using System.Collections.Concurrent;
using System.Diagnostics;
using YesHybrid.Engine.Game;
using YesHybrid.Engine.Uci;

namespace YesHybrid.Cli.Commands;

/// <summary>
/// Evaluate a single rule set's self-balance by playing N self-play games
/// from a randomized opening book, then reporting a composite score.
///
///   yes-hybrid match --rules rulesets/baseline.json --games 100 --depth 6 --parallel 4
///
/// The opening book is generated ONCE against the materialised variants
/// file, then shared read-only across all worker engines.  Games are
/// partitioned across <c>--parallel</c> worker tasks, each owning its own
/// Fairy-Stockfish process.
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
        var result = await RunForRuleSetAsync(o, o.RulesPath, printHeader: true);
        return result is null ? 1 : 0;
    }

    /// <summary>
    /// Reusable entry point used both by the <c>match</c> command and by
    /// the higher-level <c>sweep</c> command.  Returns null on failure.
    /// </summary>
    public static async Task<MatchRunResult?> RunForRuleSetAsync(
        Options o, string rulesPath, bool printHeader, TextWriter? outStream = null)
    {
        outStream ??= Console.Out;

        var rules = RuleSet.Load(rulesPath);
        var variantsPath = rules.MaterializeVariantsFile();
        int seed = o.Seed ?? 0xC0FFEE;
        int openingCount = o.Openings ?? o.Games;
        int openingPlies = o.OpeningPlies ?? 6;
        int parallel = Math.Max(1, o.Parallel);
        // Rule-set overrides take precedence over the global sweep params so
        // depth / ply-cap probes can coexist in one report folder.
        int effDepth    = rules.SearchDepth ?? o.Depth;
        int effMaxPlies = rules.MaxPlies    ?? o.MaxPlies;

        if (printHeader)
        {
            outStream.WriteLine($"Match: rule-set '{rules.Name}'  games={o.Games}  depth={effDepth}  "
                              + $"max-plies={effMaxPlies}  "
                              + $"openings={openingCount}x{openingPlies}plies  seed={seed}  "
                              + $"parallel={parallel}"
                              + $"{(rules.Bloodied ? "  +bloodied" : "")}");
            outStream.WriteLine($"  variants : {variantsPath}");
            outStream.WriteLine($"  startFen : {rules.EffectiveStartFen}");
        }

        // ---- Opening book: single-engine, deterministic ----------------- //
        IReadOnlyList<OpeningBook.Entry> openings;
        await using (var bookEng = await UciEngine.StartAsync(o.EnginePath, variantsPath, Variant.Name))
        {
            bookEng.Verbose = o.Verbose;
            openings = await OpeningBook.GenerateAsync(
                bookEng, rules.EffectiveStartFen, openingCount, openingPlies, seed);
        }
        if (printHeader)
            outStream.WriteLine($"  openings : {openings.Count} distinct (requested {openingCount})\n");

        // ---- Parallel game execution ------------------------------------ //
        var stats = new MatchStats();
        var statsLock = new object();
        var pgn = o.PgnPath is null ? null : PgnWriter.AppendTo(o.PgnPath);
        var pgnLock = new object();

        // Chunk the games across `parallel` workers.  Each worker owns its
        // own engine process and cycles through its slice of openings.
        var gameIds = Enumerable.Range(1, o.Games).ToList();
        var chunks = Partition(gameIds, parallel);
        var totalSw = Stopwatch.StartNew();
        int completed = 0;

        // Fresh FSF per game: mitigates the broken-pipe / bogus-mate failure
        // mode observed in v5-overnight runs at depth 8 / 600 plies after
        // ~100-200 consecutive searches on a single FSF process.  Cost is one
        // UCI handshake per game (~50 ms); acceptable vs the search budget.
        // A per-game IOException / Win32Exception / TimeoutException is
        // recorded as an "engine_crash" Unfinished outcome rather than
        // tearing down the whole match, so a single crashed worker can't
        // tank the run anymore.
        var workers = chunks.Select((chunk, wi) => Task.Run(async () =>
        {
            foreach (var gi in chunk)
            {
                var opening = openings[(gi - 1) % openings.Count];
                var sw = Stopwatch.StartNew();
                GameLoop.Outcome? outcome = null;
                string? errorTag = null;

                try
                {
                    await using var engine = await UciEngine.StartAsync(o.EnginePath, variantsPath, Variant.Name);
                    engine.Verbose = false; // never verbose inside a worker; would scramble stdout

                    var loop = new GameLoop(engine)
                    {
                        Depth = effDepth,
                        MaxPlies = effMaxPlies,
                        BloodiedEnabled = rules.Bloodied,
                    };

                    outcome = await loop.RunAsync(opening.Fen);
                }
                catch (Exception ex) when (
                    ex is IOException
                       or System.ComponentModel.Win32Exception
                       or InvalidOperationException
                       or TimeoutException)
                {
                    var firstLine = ex.Message.Split('\n', 2)[0];
                    errorTag = $"{ex.GetType().Name}: {firstLine}";
                    if (errorTag.Length > 90) errorTag = errorTag[..90];
                }
                sw.Stop();

                outcome ??= new GameLoop.Outcome(
                    Result: GameLoop.GameResult.Unfinished,
                    Termination: $"engine_crash ({errorTag})",
                    Plies: 0,
                    Moves: Array.Empty<string>(),
                    FinalFen: opening.Fen);

                lock (statsLock) stats.Record(outcome);
                int done = Interlocked.Increment(ref completed);

                var tag = GameLoop.ResultTag(outcome.Result);
                outStream.WriteLine(
                    $"  [w{wi}]  #{gi,-4} {tag,-7} {outcome.Termination,-34} "
                    + $"{outcome.Plies,4} plies  {sw.Elapsed.TotalSeconds,5:F1}s  ({done}/{o.Games})");

                if (pgn is not null)
                {
                    lock (pgnLock) pgn.Write(new PgnWriter.GameRecord(
                        StartFen: opening.Fen,
                        Moves: outcome.Moves,
                        Result: tag,
                        Termination: outcome.Termination,
                        Tags: new Dictionary<string, string>
                        {
                            ["Round"]   = gi.ToString(),
                            ["Depth"]   = effDepth.ToString(),
                            ["RuleSet"] = rules.Name,
                            ["Rules"]   = rules.Bloodied ? "bloodied" : "baseline",
                            ["Opening"] = string.Join(' ', opening.PliesPlayed),
                            ["TimeMs"]  = sw.ElapsedMilliseconds.ToString(),
                        }));
                }
            }
        })).ToList();

        try { await Task.WhenAll(workers); }
        finally { pgn?.Dispose(); }

        totalSw.Stop();
        stats.PrintReport(outStream, rules.Name);
        outStream.WriteLine($"  total time : {totalSw.Elapsed.TotalSeconds:F1}s  "
                           + $"({totalSw.Elapsed.TotalSeconds / o.Games:F2}s / game, {parallel}x)");
        if (o.PgnPath is not null)
            outStream.WriteLine($"  pgn        : {o.PgnPath}");

        return new MatchRunResult(rules, stats, totalSw.Elapsed, variantsPath);
    }

    public sealed record MatchRunResult(
        RuleSet Rules, MatchStats Stats, TimeSpan Elapsed, string MaterializedIniPath);

    private static List<List<T>> Partition<T>(IReadOnlyList<T> items, int buckets)
    {
        var result = Enumerable.Range(0, buckets).Select(_ => new List<T>()).ToList();
        for (int i = 0; i < items.Count; i++)
            result[i % buckets].Add(items[i]);
        return result;
    }
}
