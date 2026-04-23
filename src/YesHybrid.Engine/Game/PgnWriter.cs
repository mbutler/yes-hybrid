using System.Text;

namespace YesHybrid.Engine.Game;

/// <summary>
/// Emits extended PGN records for YES Hybrid games.  Uses UCI long-algebraic
/// move notation (e.g. <c>g1i3</c>) rather than SAN, because SAN is not
/// defined for our custom piece symbols.  Moves are still grouped into
/// full-move turns so standard PGN tooling can parse the file.
/// </summary>
public sealed class PgnWriter : IDisposable
{
    public sealed record GameRecord(
        string StartFen,
        IReadOnlyList<string> Moves,
        string Result,            // "1-0" | "0-1" | "1/2-1/2" | "*"
        string Termination,       // human-readable game-end reason
        IReadOnlyDictionary<string, string> Tags);

    private readonly TextWriter _writer;
    private readonly bool _ownsWriter;
    public int GamesWritten { get; private set; }

    public PgnWriter(TextWriter writer, bool ownsWriter = false)
    {
        _writer = writer;
        _ownsWriter = ownsWriter;
    }

    public static PgnWriter AppendTo(string path) =>
        new(new StreamWriter(path, append: true) { AutoFlush = true }, ownsWriter: true);

    public void Write(GameRecord g)
    {
        // --- Seven Tag Roster (PGN-required, plus our extensions) ----------
        var tags = new List<(string Key, string Value)>
        {
            ("Event",    Get(g.Tags, "Event", "YES Hybrid self-play")),
            ("Site",     Get(g.Tags, "Site",  "local")),
            ("Date",     Get(g.Tags, "Date",  DateTime.UtcNow.ToString("yyyy.MM.dd"))),
            ("Round",    Get(g.Tags, "Round", (GamesWritten + 1).ToString())),
            ("White",    Get(g.Tags, "White", "Fairy-Stockfish (Party)")),
            ("Black",    Get(g.Tags, "Black", "Fairy-Stockfish (Horde)")),
            ("Result",   g.Result),
            ("Variant",  Variant.Name),
            ("SetUp",    "1"),
            ("FEN",      g.StartFen),
            ("Termination", g.Termination),
            ("PlyCount", g.Moves.Count.ToString()),
        };
        foreach (var kv in g.Tags)
        {
            if (!tags.Any(t => t.Key == kv.Key))
                tags.Add((kv.Key, kv.Value));
        }

        foreach (var (k, v) in tags)
            _writer.WriteLine($"[{k} \"{EscapeTag(v)}\"]");

        _writer.WriteLine();

        // --- Move text -----------------------------------------------------
        if (g.Moves.Count > 0)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < g.Moves.Count; i++)
            {
                if (i % 2 == 0)
                {
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append((i / 2) + 1).Append('.');
                }
                sb.Append(' ').Append(g.Moves[i]);
                if (sb.Length > 78) { _writer.WriteLine(sb.ToString()); sb.Clear(); }
            }
            if (sb.Length > 0) { sb.Append(' ').Append(g.Result); _writer.WriteLine(sb.ToString()); }
            else _writer.WriteLine(g.Result);
        }
        else
        {
            _writer.WriteLine(g.Result);
        }

        _writer.WriteLine();
        GamesWritten++;
    }

    public void Dispose()
    {
        if (_ownsWriter) _writer.Dispose();
    }

    private static string Get(IReadOnlyDictionary<string, string> d, string key, string fallback)
        => d.TryGetValue(key, out var v) ? v : fallback;

    private static string EscapeTag(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
