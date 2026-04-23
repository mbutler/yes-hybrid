using System.Diagnostics;
using System.Text;

namespace YesHybrid.Engine.Uci;

/// <summary>
/// Minimal UCI client for Fairy-Stockfish.  Owns the child process and
/// exposes line-oriented send / receive primitives plus a few high-level
/// helpers (<see cref="GoBestMoveAsync"/>, <see cref="GetLegalMovesAsync"/>).
/// </summary>
public sealed class UciEngine : IAsyncDisposable
{
    private readonly Process _proc;
    private readonly StreamWriter _stdin;
    private readonly Queue<string> _buffer = new();
    private readonly SemaphoreSlim _bufferLock = new(1, 1);
    private readonly TaskCompletionSource _exited = new();

    public string EnginePath { get; }
    public bool Verbose { get; set; }

    private UciEngine(Process proc, string enginePath)
    {
        _proc = proc;
        _stdin = proc.StandardInput;
        EnginePath = enginePath;

        proc.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            if (Verbose) Console.Error.WriteLine($"[engine<<] {e.Data}");
            lock (_buffer) _buffer.Enqueue(e.Data);
        };
        proc.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null) return;
            Console.Error.WriteLine($"[engine!!] {e.Data}");
        };
        proc.Exited += (_, _) => _exited.TrySetResult();

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
    }

    public static async Task<UciEngine> StartAsync(string enginePath, string variantPath, string variantName, CancellationToken ct = default)
    {
        if (!File.Exists(enginePath))
            throw new FileNotFoundException(
                $"Fairy-Stockfish binary not found at '{enginePath}'.  " +
                "Run scripts/download-engine.sh (see README).", enginePath);

        var psi = new ProcessStartInfo(enginePath)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        if (!proc.Start())
            throw new InvalidOperationException("Failed to start Fairy-Stockfish process.");

        var eng = new UciEngine(proc, enginePath);

        await eng.SendAsync("uci", ct);
        await eng.WaitForAsync("uciok", TimeSpan.FromSeconds(10), ct);

        var absVariant = Path.GetFullPath(variantPath);
        await eng.SendAsync($"setoption name VariantPath value {absVariant}", ct);
        await eng.SendAsync($"setoption name UCI_Variant value {variantName}", ct);

        await eng.SendAsync("isready", ct);
        await eng.WaitForAsync("readyok", TimeSpan.FromSeconds(10), ct);

        await eng.SendAsync("ucinewgame", ct);
        await eng.SendAsync("isready", ct);
        await eng.WaitForAsync("readyok", TimeSpan.FromSeconds(10), ct);

        return eng;
    }

    public async Task SendAsync(string line, CancellationToken ct = default)
    {
        if (Verbose) Console.Error.WriteLine($"[engine>>] {line}");
        await _stdin.WriteLineAsync(line.AsMemory(), ct);
        await _stdin.FlushAsync(ct);
    }

    /// <summary>Set the position from a FEN plus an optional move list (long algebraic).</summary>
    public Task SetPositionAsync(string fen, IReadOnlyList<string>? moves = null, CancellationToken ct = default)
    {
        var sb = new StringBuilder("position fen ").Append(fen);
        if (moves is { Count: > 0 })
        {
            sb.Append(" moves");
            foreach (var m in moves) sb.Append(' ').Append(m);
        }
        return SendAsync(sb.ToString(), ct);
    }

    /// <summary>Issue a `go depth N` and return the engine's bestmove token.</summary>
    public async Task<string> GoBestMoveAsync(int depth, TimeSpan timeout, CancellationToken ct = default)
    {
        await SendAsync($"go depth {depth}", ct);
        var line = await WaitForAsync("bestmove ", timeout, ct);
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[1] : "(none)";
    }

    /// <summary>
    /// Ask Fairy-Stockfish to enumerate legal moves in the current position.
    /// Uses <c>go perft 1</c>, which prints <c>move: count</c> per legal move
    /// and finishes with a <c>Nodes searched:</c> line.  The raw <c>d</c>
    /// command does NOT emit a "Legal moves:" line for custom variants, so
    /// relying on perft output is the robust path.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetLegalMovesAsync(CancellationToken ct = default)
    {
        await DrainAsync();
        await SendAsync("go perft 1", ct);
        var moves = new List<string>();
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var line = await ReadLineAsync(TimeSpan.FromMilliseconds(200), ct);
            if (line is null) continue;
            if (line.StartsWith("Nodes searched:", StringComparison.OrdinalIgnoreCase))
                break;
            int colon = line.IndexOf(':');
            if (colon <= 0) continue;
            var mv = line[..colon].Trim();
            // UCI long-algebraic moves: "[a-l][1-8][a-l][1-8]" with optional
            // promotion suffix.  Skip any other diagnostic chatter.
            if (mv.Length >= 4 && char.IsLetter(mv[0]) && char.IsDigit(mv[1]))
                moves.Add(mv);
        }
        return moves;
    }

    /// <summary>Block until a line starting with <paramref name="prefix"/> arrives.</summary>
    public async Task<string> WaitForAsync(string prefix, TimeSpan timeout, CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var line = await ReadLineAsync(TimeSpan.FromMilliseconds(100), ct);
            if (line is null) continue;
            if (line.StartsWith(prefix, StringComparison.Ordinal)) return line;
        }
        throw new TimeoutException($"Timed out waiting for engine output starting with '{prefix}'.");
    }

    private async Task<string?> ReadLineAsync(TimeSpan poll, CancellationToken ct)
    {
        for (int i = 0; i < 5; i++)
        {
            ct.ThrowIfCancellationRequested();
            lock (_buffer)
            {
                if (_buffer.TryDequeue(out var line)) return line;
            }
            if (_proc.HasExited) return null;
            await Task.Delay(poll / 5, ct);
        }
        return null;
    }

    private Task DrainAsync()
    {
        lock (_buffer) _buffer.Clear();
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_proc.HasExited)
            {
                await SendAsync("quit");
                if (!_proc.WaitForExit(2000)) _proc.Kill(true);
            }
        }
        catch { /* swallow on dispose */ }
        _proc.Dispose();
        _stdin.Dispose();
    }
}
