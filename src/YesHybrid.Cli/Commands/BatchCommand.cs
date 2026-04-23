using System.Diagnostics;
using YesHybrid.Engine.Game;
using YesHybrid.Engine.Uci;

namespace YesHybrid.Cli.Commands;

/// <summary>
/// Run N quiet self-play games from the same starting position and append
/// each one to a PGN file, printing a tally at the end.  This is the
/// workhorse for the eventual balance-search loop (see Section 7, SPSA).
/// </summary>
internal static class BatchCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var o = Options.Parse(args);
        if (o.Games < 1)
        {
            Console.Error.WriteLine("--games must be >= 1");
            return 2;
        }

        // Default to a dated PGN in ./games/ if no path given.
        var pgnPath = o.PgnPath ?? DefaultPgnPath(o.Bloodied);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(pgnPath))!);

        Console.WriteLine($"Batch: {o.Games} game(s), depth {o.Depth}, "
                          + $"rules {(o.Bloodied ? "bloodied" : "baseline")}, "
                          + $"pgn {pgnPath}");
        Console.WriteLine();

        using var pgn = PgnWriter.AppendTo(pgnPath);

        int partyWins = 0, hordeWins = 0, unfinished = 0;
        var totalSw = Stopwatch.StartNew();

        for (int g = 1; g <= o.Games; g++)
        {
            // Fresh engine per game - keeps hash tables clean and avoids
            // the chance of variant state leaking across games.
            await using var engine = await UciEngine.StartAsync(o.EnginePath, o.VariantsPath, Variant.Name);
            engine.Verbose = o.Verbose;

            var loop = new GameLoop(engine)
            {
                Depth = o.Depth,
                MaxPlies = o.MaxPlies,
                BloodiedEnabled = o.Bloodied,
                OnPly = null,
            };

            var sw = Stopwatch.StartNew();
            var outcome = await loop.RunAsync(o.Fen);
            sw.Stop();

            var tag = GameLoop.ResultTag(outcome.Result);
            switch (outcome.Result)
            {
                case GameLoop.GameResult.PartyWins: partyWins++; break;
                case GameLoop.GameResult.HordeWins: hordeWins++; break;
                default:                            unfinished++; break;
            }

            pgn.Write(new PgnWriter.GameRecord(
                StartFen: o.Fen,
                Moves: outcome.Moves,
                Result: tag,
                Termination: outcome.Termination,
                Tags: new Dictionary<string, string>
                {
                    ["Round"] = g.ToString(),
                    ["Depth"] = o.Depth.ToString(),
                    ["Rules"] = o.Bloodied ? "bloodied" : "baseline",
                    ["TimeMs"] = sw.ElapsedMilliseconds.ToString(),
                }));

            Console.WriteLine(
                $"  #{g,-4} {tag,-7} {outcome.Termination,-34} "
                + $"{outcome.Plies,4} plies  {sw.Elapsed.TotalSeconds,6:F1}s");
        }

        totalSw.Stop();
        Console.WriteLine();
        Console.WriteLine("--- Tally ---------------------------------------------");
        Console.WriteLine($"  Party wins : {partyWins}/{o.Games}  ({Pct(partyWins, o.Games)})");
        Console.WriteLine($"  Horde wins : {hordeWins}/{o.Games}  ({Pct(hordeWins, o.Games)})");
        Console.WriteLine($"  Unfinished : {unfinished}/{o.Games}  ({Pct(unfinished, o.Games)})");
        Console.WriteLine($"  Total time : {totalSw.Elapsed.TotalSeconds:F1}s  "
                          + $"({(totalSw.Elapsed.TotalSeconds / o.Games):F1}s / game)");
        Console.WriteLine($"  PGN        : {pgnPath}");
        return 0;
    }

    private static string Pct(int n, int d) => d == 0 ? "-" : $"{(100.0 * n / d):F1}%";

    private static string DefaultPgnPath(bool bloodied)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var tag = bloodied ? "bloodied" : "baseline";
        return Path.Combine("games", $"yeshybrid-{tag}-{stamp}.pgn");
    }
}
