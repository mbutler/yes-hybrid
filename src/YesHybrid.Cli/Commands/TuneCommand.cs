using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using YesHybrid.Engine.Game;

namespace YesHybrid.Cli.Commands;

/// <summary>
/// SPSA (Simultaneous Perturbation Stochastic Approximation) tuner for
/// variant piece values.  Reads a base rule set whose <c>pieceValueMg</c>
/// override declares the starting <c>theta_0</c>, then runs K iterations:
///
///   1. Sample Bernoulli{-1,+1} perturbation delta for each tunable piece.
///   2. Evaluate f(theta + c*delta) and f(theta - c*delta) via a full
///      <see cref="MatchCommand.RunForRuleSetAsync"/> invocation of G games each.
///   3. Gradient estimate g_i = (y+ - y-) / (2 c delta_i).
///   4. Update theta -= a * g, clipped to [10, 2000].
///   5. Log the iteration to CSV + markdown trajectory, track the globally
///      best probed theta.
///
/// The objective is balance-centric: f = (PartyShareOfDecisive - 0.5)^2 * 10000.
/// A per-eval guard-rail rejects any iteration in which either perturbed
/// rule set produced UnfinishedRate &gt; 0.25 (the update is skipped but the
/// probed points still count for "best-theta-found").
///
///   yes-hybrid tune --base-rules rulesets/v7-d8-spsa-base.json \
///       --iterations 20 --games 100 --c 40 --a 8 \
///       --seed 20260425 --parallel 4 --out reports/v7-overnight
/// </summary>
internal static class TuneCommand
{
    // Which piece letters we tune.  Minion 'm' is pinned as the scale
    // anchor (value 100).  Bloodied letters (e,y,v,o,q) are irrelevant
    // when the v1 ship runs with bloodied=false and are left untouched.
    private static readonly string[] TunableLetters = { "d", "s", "c", "l", "x", "b", "a", "u" };
    private const string PinnedLetter = "m";
    private const int PinnedValue = 100;
    private const int ClipLo = 10;
    private const int ClipHi = 2000;
    private const double UnfinGuardRail = 0.25;

    public static async Task<int> RunAsync(string[] args)
    {
        var o = Options.Parse(args);
        if (o.BaseRulesPath is null)
        {
            Console.Error.WriteLine("tune: --base-rules <path.json> is required");
            return 2;
        }
        if (o.SweepOutDir is null)
        {
            o.SweepOutDir = Path.Combine("reports",
                $"tune-{DateTime.UtcNow:yyyyMMdd-HHmmss}");
        }
        Directory.CreateDirectory(o.SweepOutDir);

        int iterations = o.Iterations ?? 20;
        double c = o.SpsaC ?? 40.0;
        double a = o.SpsaA ?? 8.0;
        int gamesPerEval = Math.Max(1, o.Games);
        int parallel = Math.Max(1, o.Parallel);
        int baseSeed = o.Seed ?? 0xC0FFEE;

        var baseRules = RuleSet.Load(o.BaseRulesPath);
        var theta = ParseTheta(baseRules);

        Console.WriteLine($"Tune (SPSA): base='{baseRules.Name}'  iterations={iterations}  "
                         + $"games/eval={gamesPerEval}  c={c}  a={a}  parallel={parallel}  "
                         + $"seed0={baseSeed}");
        Console.WriteLine($"  out    : {o.SweepOutDir}");
        Console.WriteLine($"  theta0 : {FormatTheta(theta)}");
        Console.WriteLine();

        var rng = new Random(baseSeed);
        var totalSw = Stopwatch.StartNew();

        // ---- Output scaffolding ----------------------------------------- //
        var csvPath = Path.Combine(o.SweepOutDir, "tune-iterations.csv");
        var trajPath = Path.Combine(o.SweepOutDir, "tune-trajectory.md");
        var tuneLogPath = Path.Combine(o.SweepOutDir, "tune.log");

        using var csv = new StreamWriter(csvPath) { AutoFlush = true };
        using var traj = new StreamWriter(trajPath) { AutoFlush = true };
        using var tuneLog = new StreamWriter(tuneLogPath) { AutoFlush = true };

        WriteCsvHeader(csv);
        WriteTrajectoryHeader(traj, baseRules, iterations, gamesPerEval, c, a, baseSeed, theta);

        double bestY = double.MaxValue;
        Dictionary<string, int> bestTheta = new(theta);
        string bestLabel = "theta_0";
        int rejections = 0;

        for (int k = 0; k < iterations; k++)
        {
            var iterSw = Stopwatch.StartNew();

            // 1. Sample Bernoulli perturbation.
            var delta = TunableLetters.ToDictionary(
                letter => letter,
                _ => rng.NextDouble() < 0.5 ? -1 : +1);

            // 2. Build theta_plus, theta_minus (with clipping).
            var thetaPlus  = Perturb(theta, delta, +c);
            var thetaMinus = Perturb(theta, delta, -c);

            // 3. Evaluate both points.  Different per-iteration seeds so the
            //    opening books don't collide across iterations while staying
            //    paired within a single iteration.
            int iterSeed = baseSeed + k * 1000;
            var (yPlus,  statsPlus)  = await EvaluateAsync(o, baseRules, thetaPlus,
                $"iter{k:D2}-plus",  gamesPerEval, parallel, iterSeed + 1, tuneLog);
            var (yMinus, statsMinus) = await EvaluateAsync(o, baseRules, thetaMinus,
                $"iter{k:D2}-minus", gamesPerEval, parallel, iterSeed + 2, tuneLog);

            // 4. Guard rail: skip update if either probed point is too stall-heavy.
            bool guardTripped = statsPlus.UnfinishedRate > UnfinGuardRail
                             || statsMinus.UnfinishedRate > UnfinGuardRail;

            var updated = new Dictionary<string, int>(theta);
            if (!guardTripped)
            {
                foreach (var letter in TunableLetters)
                {
                    double g = (yPlus - yMinus) / (2.0 * c * delta[letter]);
                    double moved = theta[letter] - a * g;
                    updated[letter] = Clip((int)Math.Round(moved));
                }
                theta = updated;
            }
            else
            {
                rejections++;
            }

            // 5. Track best probed theta globally.
            if (yPlus < bestY)
            {
                bestY = yPlus;
                bestTheta = new Dictionary<string, int>(thetaPlus);
                bestLabel = $"iter{k:D2}-plus";
            }
            if (yMinus < bestY)
            {
                bestY = yMinus;
                bestTheta = new Dictionary<string, int>(thetaMinus);
                bestLabel = $"iter{k:D2}-minus";
            }

            iterSw.Stop();

            // 6. Log.
            WriteCsvRow(csv, k, delta, yPlus, yMinus, statsPlus, statsMinus,
                        thetaPlus, thetaMinus, theta, guardTripped);
            WriteTrajectorySection(traj, k, delta, thetaPlus, thetaMinus, theta,
                                   yPlus, yMinus, statsPlus, statsMinus,
                                   guardTripped, iterSw.Elapsed);

            Console.WriteLine(
                $"[{k:D2}/{iterations}]  y+ {yPlus,7:F2}  y- {yMinus,7:F2}  "
                + $"P%+ {statsPlus.PartyShareOfDecisive*100,5:F1}  "
                + $"P%- {statsMinus.PartyShareOfDecisive*100,5:F1}  "
                + $"Unfin {statsPlus.UnfinishedRate*100,4:F1}/{statsMinus.UnfinishedRate*100,4:F1}  "
                + $"{(guardTripped ? "[GUARD]" : "       ")}  "
                + $"elapsed {iterSw.Elapsed.TotalSeconds,5:F1}s    "
                + $"theta_new = [{FormatThetaCompact(theta)}]");
        }

        totalSw.Stop();

        // ---- Final artefacts ------------------------------------------- //
        WriteTrajectoryFooter(traj, iterations, rejections, bestY, bestLabel, bestTheta, theta, totalSw.Elapsed);
        var bestRulesPath = WriteBestRuleset(o.SweepOutDir!, baseRules, bestTheta, bestY, bestLabel);

        Console.WriteLine();
        Console.WriteLine($"Tune complete in {totalSw.Elapsed.TotalMinutes:F1} min.");
        Console.WriteLine($"  iterations : {iterations}   rejections: {rejections}");
        Console.WriteLine($"  best y     : {bestY:F2}   at {bestLabel}");
        Console.WriteLine($"  best theta : {FormatTheta(bestTheta)}");
        Console.WriteLine($"  best rules : {bestRulesPath}");
        Console.WriteLine($"  csv        : {csvPath}");
        Console.WriteLine($"  trajectory : {trajPath}");
        return 0;
    }

    // ----------------------------------------------------------------- //
    //  Per-evaluation: write temp ruleset JSON, run match, return y     //
    // ----------------------------------------------------------------- //
    private static async Task<(double Y, MatchStats Stats)> EvaluateAsync(
        Options outer, RuleSet baseRules, Dictionary<string, int> probedTheta,
        string label, int games, int parallel, int seed, TextWriter tuneLog)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "yes-hybrid-tune");
        Directory.CreateDirectory(tempDir);
        var tempPath = Path.Combine(tempDir,
            $"{Safe(baseRules.Name)}-{label}-{Guid.NewGuid():N}.json");

        var newOverrides = new Dictionary<string, string>(baseRules.Overrides);
        var mgStr = FormatTheta(probedTheta);
        newOverrides["pieceValueMg"] = mgStr;
        newOverrides["pieceValueEg"] = mgStr;

        var wrapper = new RuleSetJson
        {
            name        = $"{baseRules.Name}-{label}",
            variantsIni = baseRules.VariantsIni,
            startFen    = baseRules.StartFen,
            flags       = baseRules.Flags,
            overrides   = newOverrides,
            searchDepth = baseRules.SearchDepth,
            maxPlies    = baseRules.MaxPlies,
        };
        var json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });
        File.WriteAllText(tempPath, json);

        // Build a local Options for this eval.  No PGN (saves 4-8MB per
        // iteration); no verbose (would scramble tune console output).
        var localOpts = new Options
        {
            EnginePath   = outer.EnginePath,
            VariantsPath = outer.VariantsPath,
            Depth        = outer.Depth,
            Verbose      = false,
            MaxPlies     = outer.MaxPlies,
            Games        = games,
            Seed         = seed,
            Openings     = outer.Openings,
            OpeningPlies = outer.OpeningPlies,
            Parallel     = parallel,
            RulesPath    = tempPath,
            PgnPath      = null,
        };

        var result = await MatchCommand.RunForRuleSetAsync(
            localOpts, tempPath, printHeader: true, outStream: tuneLog);
        if (result is null)
            throw new InvalidOperationException($"tune: match eval failed for {label}");

        double y = Objective(result.Stats);
        return (y, result.Stats);
    }

    private static double Objective(MatchStats s)
    {
        double p = s.PartyShareOfDecisive;
        double dev = p - 0.5;
        return dev * dev * 10000.0;
    }

    // ----------------------------------------------------------------- //
    //  theta parsing / formatting / clipping                            //
    // ----------------------------------------------------------------- //
    private static Dictionary<string, int> ParseTheta(RuleSet rules)
    {
        if (!rules.Overrides.TryGetValue("pieceValueMg", out var raw))
            throw new FormatException(
                "tune: base ruleset must provide 'pieceValueMg' override as starting theta");
        var theta = new Dictionary<string, int>();
        foreach (var part in raw.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var bits = part.Split(':');
            if (bits.Length != 2)
                throw new FormatException($"tune: malformed pieceValueMg token '{part}'");
            theta[bits[0]] = int.Parse(bits[1], CultureInfo.InvariantCulture);
        }
        foreach (var l in TunableLetters)
            if (!theta.ContainsKey(l))
                throw new FormatException($"tune: starting theta missing tunable piece '{l}'");
        if (!theta.TryGetValue(PinnedLetter, out var pinnedSeen) || pinnedSeen != PinnedValue)
            throw new FormatException(
                $"tune: starting theta must pin '{PinnedLetter}:{PinnedValue}' as scale anchor");
        return theta;
    }

    private static Dictionary<string, int> Perturb(
        Dictionary<string, int> theta, Dictionary<string, int> delta, double c)
    {
        var result = new Dictionary<string, int>(theta);
        foreach (var letter in TunableLetters)
            result[letter] = Clip((int)Math.Round(theta[letter] + c * delta[letter]));
        return result;
    }

    private static int Clip(int v) => Math.Clamp(v, ClipLo, ClipHi);

    private static string FormatTheta(Dictionary<string, int> theta)
    {
        var sb = new StringBuilder();
        foreach (var letter in TunableLetters)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(letter).Append(':').Append(theta[letter].ToString(CultureInfo.InvariantCulture));
        }
        sb.Append(' ').Append(PinnedLetter).Append(':').Append(PinnedValue);
        return sb.ToString();
    }

    private static string FormatThetaCompact(Dictionary<string, int> theta) =>
        string.Join(",", TunableLetters.Select(l => theta[l].ToString()));

    // ----------------------------------------------------------------- //
    //  Output writers                                                    //
    // ----------------------------------------------------------------- //
    private static void WriteCsvHeader(StreamWriter csv)
    {
        var thetaCols = TunableLetters.Select(l => $"theta_{l}").ToArray();
        var deltaCols = TunableLetters.Select(l => $"delta_{l}").ToArray();
        var plusCols  = TunableLetters.Select(l => $"plus_{l}").ToArray();
        var minusCols = TunableLetters.Select(l => $"minus_{l}").ToArray();

        csv.WriteLine("iter," +
            string.Join(',', deltaCols) + "," +
            "y_plus,y_minus,p_plus,p_minus,unfin_plus,unfin_minus," +
            "dec_plus,dec_minus,median_plus,median_minus," +
            "guard_tripped," +
            string.Join(',', plusCols) + "," +
            string.Join(',', minusCols) + "," +
            string.Join(',', thetaCols));
    }

    private static void WriteCsvRow(
        StreamWriter csv, int k,
        Dictionary<string, int> delta,
        double yPlus, double yMinus,
        MatchStats sPlus, MatchStats sMinus,
        Dictionary<string, int> thetaPlus, Dictionary<string, int> thetaMinus,
        Dictionary<string, int> thetaAfter, bool guard)
    {
        var parts = new List<string> { k.ToString() };
        parts.AddRange(TunableLetters.Select(l => delta[l].ToString()));
        parts.Add(yPlus.ToString("F4",  CultureInfo.InvariantCulture));
        parts.Add(yMinus.ToString("F4", CultureInfo.InvariantCulture));
        parts.Add((sPlus.PartyShareOfDecisive).ToString("F4", CultureInfo.InvariantCulture));
        parts.Add((sMinus.PartyShareOfDecisive).ToString("F4", CultureInfo.InvariantCulture));
        parts.Add((sPlus.UnfinishedRate).ToString("F4", CultureInfo.InvariantCulture));
        parts.Add((sMinus.UnfinishedRate).ToString("F4", CultureInfo.InvariantCulture));
        parts.Add((sPlus.DecisiveRate).ToString("F4", CultureInfo.InvariantCulture));
        parts.Add((sMinus.DecisiveRate).ToString("F4", CultureInfo.InvariantCulture));
        parts.Add(sPlus.MedianDecisivePlies.ToString());
        parts.Add(sMinus.MedianDecisivePlies.ToString());
        parts.Add(guard ? "1" : "0");
        parts.AddRange(TunableLetters.Select(l => thetaPlus[l].ToString()));
        parts.AddRange(TunableLetters.Select(l => thetaMinus[l].ToString()));
        parts.AddRange(TunableLetters.Select(l => thetaAfter[l].ToString()));
        csv.WriteLine(string.Join(',', parts));
    }

    private static void WriteTrajectoryHeader(
        StreamWriter traj, RuleSet baseRules, int iterations, int games,
        double c, double a, int seed, Dictionary<string, int> theta0)
    {
        traj.WriteLine("# SPSA trajectory");
        traj.WriteLine();
        traj.WriteLine($"- base ruleset : `{baseRules.Name}`");
        traj.WriteLine($"- iterations   : {iterations}");
        traj.WriteLine($"- games/eval   : {games}");
        traj.WriteLine($"- c (perturb)  : {c}");
        traj.WriteLine($"- a (gain)     : {a}");
        traj.WriteLine($"- seed0        : {seed}");
        traj.WriteLine($"- tunable      : {string.Join(",", TunableLetters)}");
        traj.WriteLine($"- pinned       : {PinnedLetter}={PinnedValue}");
        traj.WriteLine($"- bounds       : [{ClipLo}, {ClipHi}]");
        traj.WriteLine($"- guard-rail   : reject iteration if Unfin > {UnfinGuardRail*100:F0}%");
        traj.WriteLine($"- objective    : f(theta) = (PartyShareOfDecisive - 0.5)^2 * 10000");
        traj.WriteLine();
        traj.WriteLine($"theta_0 = `{FormatTheta(theta0)}`");
        traj.WriteLine();
    }

    private static void WriteTrajectorySection(
        StreamWriter traj, int k,
        Dictionary<string, int> delta,
        Dictionary<string, int> thetaPlus, Dictionary<string, int> thetaMinus, Dictionary<string, int> thetaAfter,
        double yPlus, double yMinus,
        MatchStats sPlus, MatchStats sMinus,
        bool guard, TimeSpan elapsed)
    {
        traj.WriteLine($"## Iteration {k}");
        traj.WriteLine();
        traj.WriteLine($"- delta: `[{string.Join(",", TunableLetters.Select(l => delta[l] > 0 ? "+1" : "-1"))}]`  "
                     + $"(for letters {string.Join(",", TunableLetters)})");
        traj.WriteLine($"- theta+ `{FormatTheta(thetaPlus)}` -> y+ = {yPlus:F2}  "
                     + $"(P%={sPlus.PartyShareOfDecisive*100:F1}%, Unfin={sPlus.UnfinishedRate*100:F1}%, "
                     + $"Dec={sPlus.DecisiveRate*100:F1}%, Median plies={sPlus.MedianDecisivePlies})");
        traj.WriteLine($"- theta- `{FormatTheta(thetaMinus)}` -> y- = {yMinus:F2}  "
                     + $"(P%={sMinus.PartyShareOfDecisive*100:F1}%, Unfin={sMinus.UnfinishedRate*100:F1}%, "
                     + $"Dec={sMinus.DecisiveRate*100:F1}%, Median plies={sMinus.MedianDecisivePlies})");
        if (guard)
            traj.WriteLine($"- **guard-rail tripped** (Unfin > {UnfinGuardRail*100:F0}%); theta unchanged.");
        traj.WriteLine($"- theta_after: `{FormatTheta(thetaAfter)}`");
        traj.WriteLine($"- iteration elapsed: {elapsed.TotalSeconds:F1}s");
        traj.WriteLine();
    }

    private static void WriteTrajectoryFooter(
        StreamWriter traj, int iterations, int rejections,
        double bestY, string bestLabel, Dictionary<string, int> bestTheta,
        Dictionary<string, int> finalTheta, TimeSpan elapsed)
    {
        traj.WriteLine("## Summary");
        traj.WriteLine();
        traj.WriteLine($"- iterations    : {iterations}");
        traj.WriteLine($"- rejections    : {rejections}  (guard-rail trips)");
        traj.WriteLine($"- elapsed       : {elapsed.TotalMinutes:F1} min");
        traj.WriteLine($"- best y        : {bestY:F2}  ({Math.Sqrt(bestY)/100 + 0.5:F3} / {0.5 - Math.Sqrt(bestY)/100:F3} P%(dec) distance from 50%)");
        traj.WriteLine($"- best at       : {bestLabel}");
        traj.WriteLine($"- best theta    : `{FormatTheta(bestTheta)}`");
        traj.WriteLine($"- final theta   : `{FormatTheta(finalTheta)}`");
    }

    private static string WriteBestRuleset(
        string outDir, RuleSet baseRules, Dictionary<string, int> bestTheta,
        double bestY, string bestLabel)
    {
        var newOverrides = new Dictionary<string, string>(baseRules.Overrides);
        var mgStr = FormatTheta(bestTheta);
        newOverrides["pieceValueMg"] = mgStr;
        newOverrides["pieceValueEg"] = mgStr;

        var wrapper = new RuleSetJson
        {
            name        = $"{baseRules.Name}-best",
            variantsIni = baseRules.VariantsIni,
            startFen    = baseRules.StartFen,
            flags       = baseRules.Flags,
            overrides   = newOverrides,
            searchDepth = baseRules.SearchDepth,
            maxPlies    = baseRules.MaxPlies,
        };
        var json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });
        var path = Path.Combine(outDir, "tune-best.json");
        // Add a machine-readable provenance comment as top-line JSON stays valid
        // (System.Text.Json supports line comments when ReadCommentHandling=Skip,
        // which RuleSet.Load uses).
        var prelude = $"// Generated by yes-hybrid tune; best-y={bestY:F2} at {bestLabel}\n";
        File.WriteAllText(path, prelude + json);
        return path;
    }

    private static string Safe(string s)
    {
        var sb = new StringBuilder();
        foreach (var ch in s)
            sb.Append(char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_');
        return sb.ToString();
    }

    // Plain POCO for JSON round-trip (RuleSet has init-only setters; this
    // avoids dragging its serialization surface around).
    private sealed class RuleSetJson
    {
        public string name { get; set; } = "unnamed";
        public string variantsIni { get; set; } = "variants/yeshybrid.ini";
        public string? startFen { get; set; }
        public Dictionary<string, bool> flags { get; set; } = new();
        public Dictionary<string, string> overrides { get; set; } = new();
        public int? searchDepth { get; set; }
        public int? maxPlies { get; set; }
    }
}
