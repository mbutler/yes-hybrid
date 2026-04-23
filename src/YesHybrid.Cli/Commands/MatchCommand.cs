using System.Diagnostics;
using YesHybrid.Engine.Game;
using YesHybrid.Engine.Uci;

namespace YesHybrid.Cli.Commands;

/// <summary>
/// Evaluate a single rule set's self-balance by playing N self-play games
/// from a randomized opening book, then reporting a composite score.  This
/// is the atomic unit of every higher-level search (sweep, tune).
///
///   yes-hybrid match --rules rulesets/baseline.json --games 100 --depth 6
/// </summary>
internal static class MatchCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var o = Options.Parse(args);
        if (o.RulesPath is null)
        {
            Console.Error.WriteLine("match: --rules <path.json> is required");
            return 2;
        }

        var rules = RuleSet.Load(o.RulesPath);
        var variantsPath = rules.MaterializeVariantsFile();
        int seed = o.Seed ?? 0xC0FFEE;
        int openingCount = o.Openings ?? o.Games;
        int openingPlies = o.OpeningPlies ?? 6;

        Console.WriteLine($"Match: rule-set '{rules.Name}'  games={o.Games}  depth={o.Depth}  "
                          + $"openings={openingCount}x{openingPlies}plies  seed={seed}"
                          + $"{(rules.Bloodied ? "  +bloodied" : "")}");
        Console.WriteLine($"  variants : {variantsPath}");
        Console.WriteLine($"  startFen : {rules.EffectiveStartFen}");

        // ---- Generate the opening book against this variant ------------- //
        IReadOnlyList<OpeningBook.Entry> openings;
        await using (var bookEng = await UciEngine.StartAsync(o.EnginePath, variantsPath, Variant.Name))
        {
            bookEng.Verbose = o.Verbose;
            openings = await OpeningBook.GenerateAsync(
                bookEng, rules.EffectiveStartFen, openingCount, openingPlies, seed);
        }
        Console.WriteLine($"  openings : {openings.Count} distinct (requested {openingCount})");
        Console.WriteLine();

        // ---- Play N games, one engine per game -------------------------- //
        var stats = new MatchStats();
        PgnWriter? pgn = o.PgnPath is null ? null : PgnWriter.AppendTo(o.PgnPath);
        var totalSw = Stopwatch.StartNew();

        try
        {
            for (int g = 1; g <= o.Games; g++)
            {
                var opening = openings[(g - 1) % openings.Count];

                await using var engine = await UciEngine.StartAsync(o.EnginePath, variantsPath, Variant.Name);
                engine.Verbose = o.Verbose;

                var loop = new GameLoop(engine)
                {
                    Depth = o.Depth,
                    MaxPlies = o.MaxPlies,
                    BloodiedEnabled = rules.Bloodied,
                };

                var sw = Stopwatch.StartNew();
                var outcome = await loop.RunAsync(opening.Fen);
                sw.Stop();

                stats.Record(outcome);
                var tag = GameLoop.ResultTag(outcome.Result);
                Console.WriteLine(
                    $"  #{g,-4} {tag,-7} {outcome.Termination,-34} "
                    + $"{outcome.Plies,4} plies  {sw.Elapsed.TotalSeconds,5:F1}s  "
                    + $"open=[{string.Join(' ', opening.PliesPlayed)}]");

                pgn?.Write(new PgnWriter.GameRecord(
                    StartFen: opening.Fen,
                    Moves: outcome.Moves,
                    Result: tag,
                    Termination: outcome.Termination,
                    Tags: new Dictionary<string, string>
                    {
                        ["Round"]      = g.ToString(),
                        ["Depth"]      = o.Depth.ToString(),
                        ["RuleSet"]    = rules.Name,
                        ["Rules"]      = rules.Bloodied ? "bloodied" : "baseline",
                        ["Opening"]    = string.Join(' ', opening.PliesPlayed),
                        ["TimeMs"]     = sw.ElapsedMilliseconds.ToString(),
                    }));
            }
        }
        finally
        {
            pgn?.Dispose();
        }

        totalSw.Stop();
        stats.PrintReport(Console.Out, rules.Name);
        Console.WriteLine($"  total time : {totalSw.Elapsed.TotalSeconds:F1}s  "
                          + $"({totalSw.Elapsed.TotalSeconds / o.Games:F2}s / game)");
        if (o.PgnPath is not null)
            Console.WriteLine($"  pgn        : {o.PgnPath}");

        return 0;
    }
}
