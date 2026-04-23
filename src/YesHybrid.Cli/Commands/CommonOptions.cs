using YesHybrid.Engine.Game;

namespace YesHybrid.Cli.Commands;

/// <summary>Tiny long-option parser. Sufficient for our handful of flags.</summary>
internal sealed class Options
{
    public string EnginePath { get; set; } = DefaultEnginePath();
    public string VariantsPath { get; set; } = "variants/yeshybrid.ini";
    public string Fen { get; set; } = Variant.DefaultStartFen;
    public int Depth { get; set; } = 8;
    public bool Verbose { get; set; }

    public string Mode { get; set; } = "engine-vs-engine";
    public string HumanSide { get; set; } = "white";
    public int MaxPlies { get; set; } = 400;
    public int PauseMs { get; set; } = 250;

    public bool Bloodied { get; set; }
    public string? PgnPath { get; set; }
    public bool Quiet { get; set; }
    public int Games { get; set; } = 1;
    public int? Seed { get; set; }

    public string? RulesPath { get; set; }
    public int? Openings { get; set; }
    public int? OpeningPlies { get; set; }

    public static Options Parse(string[] args)
    {
        var o = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            string Next() => i + 1 < args.Length ? args[++i]
                : throw new ArgumentException($"missing value after {a}");
            switch (a)
            {
                case "--engine":      o.EnginePath = Next(); break;
                case "--variants":    o.VariantsPath = Next(); break;
                case "--fen":         o.Fen = Next(); break;
                case "--depth":       o.Depth = int.Parse(Next()); break;
                case "--verbose":     o.Verbose = true; break;
                case "--mode":        o.Mode = Next(); break;
                case "--side":        o.HumanSide = Next().ToLowerInvariant(); break;
                case "--max-plies":   o.MaxPlies = int.Parse(Next()); break;
                case "--pause":       o.PauseMs = int.Parse(Next()); break;
                case "--bloodied":    o.Bloodied = true; break;
                case "--pgn":         o.PgnPath = Next(); break;
                case "--quiet":       o.Quiet = true; break;
                case "--games":       o.Games = int.Parse(Next()); break;
                case "--seed":        o.Seed = int.Parse(Next()); break;
                case "--rules":       o.RulesPath = Next(); break;
                case "--openings":    o.Openings = int.Parse(Next()); break;
                case "--opening-plies": o.OpeningPlies = int.Parse(Next()); break;
                default:
                    throw new ArgumentException($"unknown option: {a}");
            }
        }
        return o;
    }

    private static string DefaultEnginePath()
    {
        var candidates = new[]
        {
            "engine/fairy-stockfish",
            "engine/fairy-stockfish.exe",
            "./fairy-stockfish",
        };
        foreach (var c in candidates)
            if (File.Exists(c)) return c;
        return candidates[0];
    }
}
