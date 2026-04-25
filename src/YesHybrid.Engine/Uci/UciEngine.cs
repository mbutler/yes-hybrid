using System.Diagnostics;
using System.Text;
using System.Threading;

namespace YesHybrid.Engine.Uci;

/// <summary>
/// Minimal UCI client for Fairy-Stockfish.  Owns the child process and
/// exposes line-oriented send / receive primitives plus a few high-level
/// helpers (<see cref="GoBestGetFenAsync"/>, <see cref="GetLegalMovesAsync(string, CancellationToken)"/>).
/// All operations are serialized: interleaving (e.g. <c>go perft 1</c> during <c>go depth</c>) corrupts state.
/// </summary>
public sealed class UciEngine : IAsyncDisposable
{
    private readonly Process _proc;
    private readonly StreamWriter _stdin;
    private readonly Queue<string> _buffer = new();
    private readonly SemaphoreSlim _io = new(1, 1);
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

        await eng.SendUnlockedAsync("uci", ct);
        await eng.WaitForAsync("uciok", TimeSpan.FromSeconds(10), ct);

        var absVariant = Path.GetFullPath(variantPath);
        await eng.SendUnlockedAsync($"setoption name VariantPath value {absVariant}", ct);
        await eng.SendUnlockedAsync($"setoption name UCI_Variant value {variantName}", ct);

        await eng.SendUnlockedAsync("isready", ct);
        await eng.WaitForAsync("readyok", TimeSpan.FromSeconds(10), ct);

        await eng.SendUnlockedAsync("ucinewgame", ct);
        await eng.SendUnlockedAsync("isready", ct);
        await eng.WaitForAsync("readyok", TimeSpan.FromSeconds(10), ct);

        return eng;
    }

    private async Task SendUnlockedAsync(string line, CancellationToken ct = default)
    {
        if (Verbose) Console.Error.WriteLine($"[engine>>] {line}");
        await _stdin.WriteLineAsync(line.AsMemory(), ct);
        await _stdin.FlushAsync(ct);
    }

    /// <summary>Set the position from a FEN plus an optional move list (long algebraic).</summary>
    public async Task SetPositionAsync(string fen, IReadOnlyList<string>? moves = null, CancellationToken ct = default)
    {
        await _io.WaitAsync(ct);
        try
        {
            DrainUnlocked();
            var sb = new StringBuilder("position fen ").Append(fen);
            if (moves is { Count: > 0 })
            {
                sb.Append(" moves");
                foreach (var m in moves) sb.Append(' ').Append(m);
            }
            await SendUnlockedAsync(sb.ToString(), ct);
        }
        finally
        {
            _io.Release();
        }
    }

    /// <summary>Search from <paramref name="fen"/> and return the best UCI (does not call <c>position</c> to apply; engine state is left at start FEN for the next UCI op).</summary>
    public async Task<string> GoSearchBestMoveOnlyAsync(string fen, int depth, TimeSpan timeout, CancellationToken ct = default)
    {
        await _io.WaitAsync(ct);
        try
        {
            DrainUnlocked();
            await SendUnlockedAsync($"position fen {fen}", ct);
            await SendUnlockedAsync($"go depth {depth}", ct);
            var line = await WaitForAsync("bestmove ", timeout, ct);
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 ? parts[1] : "(none)";
        }
        finally
        {
            _io.Release();
        }
    }

    /// <summary>After <c>position fen {startFen} [moves ...]</c>, run <c>go perft 1</c> (one locked transaction).</summary>
    public async Task<IReadOnlyList<string>> GetLegalMovesFromSetupAsync(
        string startFen, IReadOnlyList<string>? moves, CancellationToken ct = default)
    {
        await _io.WaitAsync(ct);
        try
        {
            DrainUnlocked();
            var sb = new StringBuilder("position fen ").Append(startFen);
            if (moves is { Count: > 0 })
            {
                sb.Append(" moves");
                foreach (var m in moves) sb.Append(' ').Append(m);
            }
            await SendUnlockedAsync(sb.ToString(), ct);
            await SendUnlockedAsync("go perft 1", ct);
            return await ReadPerftMoveListAsync(ct);
        }
        finally
        {
            _io.Release();
        }
    }

    /// <summary>Search from <paramref name="fen"/>, then apply the best UCI and return the new FEN. When there is no move, returns (none) and <c>newFen: null</c> (one locked transaction).</summary>
    public async Task<(string move, string? newFen)> GoBestGetFenAsync(string fen, int depth, TimeSpan timeout, CancellationToken ct = default)
    {
        await _io.WaitAsync(ct);
        try
        {
            DrainUnlocked();
            await SendUnlockedAsync($"position fen {fen}", ct);
            await SendUnlockedAsync($"go depth {depth}", ct);
            var line = await WaitForAsync("bestmove ", timeout, ct);
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var move = parts.Length >= 2 ? parts[1] : "(none)";
            if (move is "(none)" or "0000")
                return (move, null);
            // Deeper searches may still queue "info" after bestmove; drop before the next position + "d".
            DrainUnlocked();
            await SendUnlockedAsync($"position fen {fen} moves {move}", ct);
            var next = await GetCurrentFenCoreAsync(ct);
            return (move, next);
        }
        finally
        {
            _io.Release();
        }
    }

    /// <summary>Apply a UCI line from the current FEN and return the FEN the engine has after the move (one locked transaction).</summary>
    public async Task<string?> SetPositionGetFenAsync(string fen, IReadOnlyList<string> moves, CancellationToken ct = default)
    {
        await _io.WaitAsync(ct);
        try
        {
            DrainUnlocked();
            var sb = new StringBuilder("position fen ").Append(fen);
            if (moves is { Count: > 0 })
            {
                sb.Append(" moves");
                foreach (var m in moves) sb.Append(' ').Append(m);
            }
            await SendUnlockedAsync(sb.ToString(), ct);
            return await GetCurrentFenCoreAsync(ct, drainBufferFirst: true);
        }
        finally
        {
            _io.Release();
        }
    }

    private async Task<string?> GetCurrentFenCoreAsync(CancellationToken ct, bool drainBufferFirst = true)
    {
        if (drainBufferFirst)
            DrainUnlocked();
        // Stale "info" / partial lines from a prior "go" can hide the Fen: line; clear before a fresh "d".
        await SendUnlockedAsync("d", ct);
        await SendUnlockedAsync("isready", ct);
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            var line = await ReadLineAsync(TimeSpan.FromMilliseconds(300), ct);
            if (line is null) continue;
            if (TryStripFenLine(line, out var fen)) return fen;
        }
        return null;
    }

    private static bool TryStripFenLine(string line, out string fen)
    {
        fen = "";
        var t = line.Trim();
        if (t.Length < 5) return false;
        if (!t.StartsWith("Fen", StringComparison.OrdinalIgnoreCase) || t[3] != ':') return false;
        fen = t[4..].Trim();
        return fen.Length >= 3;
    }

    /// <summary>Read the FEN after the last <see cref="SetPositionAsync"/> in this process (must not be interleaved; prefer <see cref="SetPositionGetFenAsync"/>).</summary>
    public async Task<string?> GetCurrentFenAsync(CancellationToken ct = default)
    {
        await _io.WaitAsync(ct);
        try
        {
            return await GetCurrentFenCoreAsync(ct, drainBufferFirst: true);
        }
        finally
        {
            _io.Release();
        }
    }

    /// <summary>Replay from <paramref name="startFen"/> + moves, <c>go perft 1</c>, then <c>d</c> under one lock. Board + legals always match; avoids drift between <c>Session.Fen</c> and the plies the engine is replaying.</summary>
    public async Task<(string? Fen, IReadOnlyList<string> Legals)> GetFenAndLegalMovesFromSetupAsync(
        string startFen, IReadOnlyList<string>? moves, CancellationToken ct = default)
    {
        await _io.WaitAsync(ct);
        try
        {
            DrainUnlocked();
            var sb = new StringBuilder("position fen ").Append(startFen);
            if (moves is { Count: > 0 })
            {
                sb.Append(" moves");
                foreach (var m in moves) sb.Append(' ').Append(m);
            }
            await SendUnlockedAsync(sb.ToString(), ct);
            await SendUnlockedAsync("go perft 1", ct);
            var legals = await ReadPerftMoveListAsync(ct);
            var fen = await GetCurrentFenCoreAsync(ct, drainBufferFirst: false);
            return (fen, legals);
        }
        finally
        {
            _io.Release();
        }
    }

    /// <summary>Enumerate legals: sets <c>position fen ...</c> and runs <c>go perft 1</c> under a single engine lock (required for correct output).</summary>
    public async Task<IReadOnlyList<string>> GetLegalMovesAsync(string fen, CancellationToken ct = default)
    {
        await _io.WaitAsync(ct);
        try
        {
            DrainUnlocked();
            await SendUnlockedAsync($"position fen {fen}", ct);
            await SendUnlockedAsync("go perft 1", ct);
            return await ReadPerftMoveListAsync(ct);
        }
        finally
        {
            _io.Release();
        }
    }

    private async Task<IReadOnlyList<string>> ReadPerftMoveListAsync(CancellationToken ct)
    {
        var moves = new List<string>();
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var line = await ReadLineAsync(TimeSpan.FromMilliseconds(200), ct);
            if (line is null) continue;
            if (line.StartsWith("Nodes searched:", StringComparison.OrdinalIgnoreCase))
                break;
            var mv = TryParsePerftMoveLine(line, out var token) ? token : null;
            if (mv is not null) moves.Add(mv);
        }
        return moves;
    }

    /// <summary>Perft lines look like "c7c6: 1" or in some builds "c7c6 1".</summary>
    private static bool TryParsePerftMoveLine(string line, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? move)
    {
        move = null;
        int colon = line.IndexOf(':');
        if (colon > 0)
        {
            var mv = line[..colon].Trim();
            if (IsUciFromToPrefix(mv)) { move = mv; return true; }
        }
        // Fallback: "c7c6 1" or "c7c6 1 (something)"
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && IsUciFromToPrefix(parts[0]))
        {
            move = parts[0];
            return true;
        }
        return false;
    }

    private static bool IsUciFromToPrefix(string mv) =>
        mv.Length >= 4
        && char.IsLetter(mv[0])
        && char.IsDigit(mv[1])
        && char.IsLetter(mv[2])
        && char.IsDigit(mv[3]);

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
        // Bounded spin-wait: poll interval split so we don't return null while data is in flight.
        for (int i = 0; i < 25; i++)
        {
            ct.ThrowIfCancellationRequested();
            lock (_buffer)
            {
                if (_buffer.TryDequeue(out var line)) return line;
            }
            if (_proc.HasExited) return null;
            await Task.Delay(poll / 25, ct);
        }
        return null;
    }

    private void DrainUnlocked()
    {
        lock (_buffer) _buffer.Clear();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_proc.HasExited)
            {
                await _io.WaitAsync(5000);
                try
                {
                    await SendUnlockedAsync("quit");
                }
                catch { /* ignore */ }
                finally
                {
                    _io.Release();
                }
                if (!_proc.WaitForExit(2000)) _proc.Kill(true);
            }
        }
        catch { /* swallow on dispose */ }
        _io.Dispose();
        _proc.Dispose();
        _stdin.Dispose();
    }
}
