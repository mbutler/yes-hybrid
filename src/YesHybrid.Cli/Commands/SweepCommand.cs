using System.Diagnostics;
using System.Text;
using YesHybrid.Engine.Game;

namespace YesHybrid.Cli.Commands;

/// <summary>
/// Runs <see cref="MatchCommand"/> on every rule-set listed (via one or
/// more <c>--add-rules</c> flags OR discovered in <c>rulesets/</c>) and
/// produces a ranked summary:
///   - CSV at  &lt;--out&gt;/sweep-summary.csv
///   - markdown at &lt;--out&gt;/sweep-summary.md
///   - full per-match text logs at &lt;--out&gt;/match-&lt;name&gt;.log
///   - PGN archives at &lt;--out&gt;/match-&lt;name&gt;.pgn
///
///   yes-hybrid sweep --games 100 --depth 6 --parallel 4 \
///       --out reports/v2 \
///       --add-rules rulesets/baseline.json \
///       --add-rules rulesets/flag-only.json \
///       ...
/// </summary>
internal static class SweepCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var o = Options.Parse(args);
        if (o.RulesList.Count == 0)
        {
            Console.Error.WriteLine("sweep: provide at least one --add-rules <path.json>");
            return 2;
        }
        if (o.SweepOutDir is null)
        {
            o.SweepOutDir = Path.Combine("reports",
                $"sweep-{DateTime.UtcNow:yyyyMMdd-HHmmss}");
        }
        Directory.CreateDirectory(o.SweepOutDir);

        Console.WriteLine($"Sweep: {o.RulesList.Count} rule-sets x {o.Games} games  "
                         + $"(depth={o.Depth}, parallel={o.Parallel}, seed={o.Seed ?? 0xC0FFEE})");
        Console.WriteLine($"  out : {o.SweepOutDir}\n");

        var results = new List<(string Path, MatchCommand.MatchRunResult Result)>();
        var overallSw = Stopwatch.StartNew();

        for (int i = 0; i < o.RulesList.Count; i++)
        {
            var path = o.RulesList[i];
            Console.WriteLine($"[{i + 1}/{o.RulesList.Count}] {path}");

            // Per-rule-set options: fresh PGN, captured log, same global params.
            var localOpts = CloneForRuleSet(o, path);
            var logPath = Path.Combine(o.SweepOutDir, $"match-{SafeStem(path)}.log");
            using var logWriter = new StreamWriter(logPath) { AutoFlush = true };

            // Tee the per-match output to BOTH the log file and the live console.
            using var tee = new TeeTextWriter(logWriter, Console.Out);
            var result = await MatchCommand.RunForRuleSetAsync(
                localOpts, path, printHeader: true, outStream: tee);
            if (result is null)
            {
                Console.Error.WriteLine($"  FAILED on {path}; aborting sweep.");
                return 1;
            }

            results.Add((path, result));
            // Flush partial summary after every match so an aborted sweep
            // still leaves a coherent report.
            WriteSummary(o.SweepOutDir, results, overallSw.Elapsed, finished: false);
            Console.WriteLine();
        }

        overallSw.Stop();
        WriteSummary(o.SweepOutDir, results, overallSw.Elapsed, finished: true);
        Console.WriteLine($"Sweep complete in {overallSw.Elapsed.TotalMinutes:F1} min.");
        Console.WriteLine($"  summary: {Path.Combine(o.SweepOutDir, "sweep-summary.md")}");
        return 0;
    }

    // ----------------------------------------------------------------- //
    //  Summary emission                                                 //
    // ----------------------------------------------------------------- //
    private static void WriteSummary(
        string outDir,
        IReadOnlyList<(string Path, MatchCommand.MatchRunResult Result)> results,
        TimeSpan elapsed,
        bool finished)
    {
        // CSV
        var csvPath = Path.Combine(outDir, "sweep-summary.csv");
        using (var csv = new StreamWriter(csvPath))
        {
            csv.WriteLine("rank,ruleset,games,party_wins,horde_wins,unfinished,"
                        + "decisive_pct,party_share_decisive_pct,imbalance_pct,"
                        + "composite,party_ci_lo,party_ci_hi,median_plies,elapsed_sec");
            int rank = 1;
            foreach (var r in results.OrderByDescending(r => r.Result.Stats.CompositeScore))
            {
                var s = r.Result.Stats;
                var (lo, hi) = s.PartyWinRateWilson95();
                csv.WriteLine(string.Join(',', new[]
                {
                    rank++.ToString(),
                    Quote(r.Result.Rules.Name),
                    s.Total.ToString(),
                    s.PartyWins.ToString(),
                    s.HordeWins.ToString(),
                    s.Unfinished.ToString(),
                    Pct(s.DecisiveRate),
                    Pct(s.PartyShareOfDecisive),
                    Pct(s.Imbalance),
                    s.CompositeScore.ToString("F3"),
                    Pct(lo), Pct(hi),
                    s.MedianDecisivePlies.ToString(),
                    r.Result.Elapsed.TotalSeconds.ToString("F1"),
                }));
            }
        }

        // Markdown
        var mdPath = Path.Combine(outDir, "sweep-summary.md");
        using var md = new StreamWriter(mdPath);
        md.WriteLine($"# Sweep summary  \n");
        md.WriteLine($"- Rule sets evaluated: {results.Count}");
        md.WriteLine($"- Elapsed: {elapsed.TotalMinutes:F1} min");
        md.WriteLine($"- Status: {(finished ? "complete" : "in-progress")}");
        md.WriteLine();
        md.WriteLine("| Rank | Rule set | Games | P-W | H-W | Unfin | Decisive | P%(dec) | Imbal | **Composite** | Median plies |");
        md.WriteLine("|-----:|:---------|------:|----:|----:|------:|---------:|--------:|------:|--------------:|-------------:|");
        int rnk = 1;
        foreach (var r in results.OrderByDescending(r => r.Result.Stats.CompositeScore))
        {
            var s = r.Result.Stats;
            md.WriteLine($"| {rnk++} | `{r.Result.Rules.Name}` | {s.Total} | "
                       + $"{s.PartyWins} | {s.HordeWins} | {s.Unfinished} | "
                       + $"{Pct(s.DecisiveRate)}% | {Pct(s.PartyShareOfDecisive)}% | {Pct(s.Imbalance)}% | "
                       + $"**{s.CompositeScore:F3}** | {s.MedianDecisivePlies} |");
        }
        md.WriteLine();
        md.WriteLine("### Interpretation key");
        md.WriteLine("- **Composite** = (1 - Imbalance) * DecisiveRate. Range 0-1. Higher = more balanced AND more decisive.");
        md.WriteLine("- A rule set can score well by being decisive in a lopsided way (imbalance high, but if decisive=1.0 and imbalance=1.0, composite=0).");
        md.WriteLine("- **Party%(dec)** ~= 50% is the balance goal.  75%+ means Party dominates; <25% means Horde dominates.");
        md.WriteLine("- High **Unfin** = game stalls (design failure; no tempo pressure).");
    }

    // ----------------------------------------------------------------- //
    //  Helpers                                                          //
    // ----------------------------------------------------------------- //
    private static Options CloneForRuleSet(Options src, string rulesPath)
    {
        var stem = SafeStem(rulesPath);
        var copy = new Options
        {
            EnginePath = src.EnginePath,
            VariantsPath = src.VariantsPath,
            Depth = src.Depth,
            Verbose = false,
            MaxPlies = src.MaxPlies,
            Games = src.Games,
            Seed = src.Seed,
            Openings = src.Openings,
            OpeningPlies = src.OpeningPlies,
            Parallel = src.Parallel,
            RulesPath = rulesPath,
            PgnPath = src.SweepOutDir is null ? null
                    : Path.Combine(src.SweepOutDir, $"match-{stem}.pgn"),
        };
        return copy;
    }

    private static string SafeStem(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var sb = new StringBuilder();
        foreach (var ch in name)
            sb.Append(char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_');
        return sb.ToString();
    }

    private static string Pct(double v) => $"{v * 100:F1}";
    private static string Quote(string s) => s.Contains(',') ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

    /// <summary>Writer that mirrors every write to two underlying sinks.</summary>
    private sealed class TeeTextWriter : TextWriter
    {
        private readonly TextWriter _a, _b;
        public TeeTextWriter(TextWriter a, TextWriter b) { _a = a; _b = b; }
        public override System.Text.Encoding Encoding => _a.Encoding;
        public override void Write(char ch)            { _a.Write(ch); _b.Write(ch); }
        public override void Write(string? value)      { _a.Write(value); _b.Write(value); }
        public override void WriteLine(string? value)  { _a.WriteLine(value); _b.WriteLine(value); }
        public override void WriteLine()               { _a.WriteLine(); _b.WriteLine(); }
        public override void Flush()                   { _a.Flush(); _b.Flush(); }
    }
}
