namespace YesHybrid.Engine.Game;

/// <summary>
/// Aggregates per-game outcomes for a single rule-set evaluation.  The
/// primary observable is <see cref="CompositeScore"/>: 1.0 when the rule
/// set is perfectly balanced AND perfectly decisive, 0.0 when either is
/// fully broken.  Use this as the objective in higher-level search (sweep,
/// tune).
/// </summary>
public sealed class MatchStats
{
    private readonly List<int> _partyPlies = new();
    private readonly List<int> _hordePlies = new();
    private readonly List<int> _unfinishedPlies = new();

    public int PartyWins => _partyPlies.Count;
    public int HordeWins => _hordePlies.Count;
    public int Unfinished => _unfinishedPlies.Count;
    public int Total => PartyWins + HordeWins + Unfinished;

    public void Record(GameLoop.Outcome o)
    {
        switch (o.Result)
        {
            case GameLoop.GameResult.PartyWins: _partyPlies.Add(o.Plies); break;
            case GameLoop.GameResult.HordeWins: _hordePlies.Add(o.Plies); break;
            default:                            _unfinishedPlies.Add(o.Plies); break;
        }
    }

    public int DecisiveGames     => PartyWins + HordeWins;
    public double DecisiveRate   => Total == 0 ? 0 : (double)DecisiveGames / Total;
    public double PartyWinRate   => Total == 0 ? 0 : (double)PartyWins / Total;
    public double HordeWinRate   => Total == 0 ? 0 : (double)HordeWins / Total;
    public double UnfinishedRate => Total == 0 ? 0 : (double)Unfinished / Total;
    /// <summary>Party's share of decisive games only.  The CI and Imbalance use this.</summary>
    public double PartyShareOfDecisive =>
        DecisiveGames == 0 ? 0.5 : (double)PartyWins / DecisiveGames;

    /// <summary>|Party% - 50%| as a fraction of 50%.  0 = perfectly balanced, 1 = one side never loses.</summary>
    public double Imbalance
    {
        get
        {
            int decisive = PartyWins + HordeWins;
            if (decisive == 0) return 1.0;
            double p = (double)PartyWins / decisive;
            return Math.Abs(p - 0.5) * 2.0;
        }
    }

    /// <summary>Composite: balance * decisiveness.  Range [0, 1], higher is better.</summary>
    public double CompositeScore => (1.0 - Imbalance) * DecisiveRate;

    public int MedianDecisivePlies
    {
        get
        {
            var all = _partyPlies.Concat(_hordePlies).ToArray();
            if (all.Length == 0) return 0;
            Array.Sort(all);
            return all[all.Length / 2];
        }
    }

    /// <summary>Wilson 95% CI half-width on Party win rate (decisive games only).</summary>
    public (double Lo, double Hi) PartyWinRateWilson95()
    {
        int decisive = PartyWins + HordeWins;
        if (decisive == 0) return (0, 1);
        const double z = 1.96;
        double p  = (double)PartyWins / decisive;
        double n  = decisive;
        double z2 = z * z;
        double c  = 1.0 / (1.0 + z2 / n);
        double centre = c * (p + z2 / (2 * n));
        double span   = c * z * Math.Sqrt(p * (1 - p) / n + z2 / (4 * n * n));
        return (Math.Max(0, centre - span), Math.Min(1, centre + span));
    }

    public string Pct(double v) => $"{v * 100,5:F1}%";

    public void PrintReport(TextWriter w, string ruleSetName)
    {
        var (lo, hi) = PartyWinRateWilson95();
        w.WriteLine();
        w.WriteLine("========================================================");
        w.WriteLine($"  Rule set    : {ruleSetName}");
        w.WriteLine($"  Games       : {Total}");
        w.WriteLine("--------------------------------------------------------");
        w.WriteLine($"  Party wins  : {PartyWins,4}  ({Pct(PartyWinRate)})");
        w.WriteLine($"  Horde wins  : {HordeWins,4}  ({Pct(HordeWinRate)})");
        w.WriteLine($"  Unfinished  : {Unfinished,4}  ({Pct(UnfinishedRate)})");
        w.WriteLine("--------------------------------------------------------");
        w.WriteLine($"  Decisive      : {Pct(DecisiveRate)}");
        w.WriteLine($"  Party(decisive): {Pct(PartyShareOfDecisive)}   95% CI [{Pct(lo)} .. {Pct(hi)}]");
        w.WriteLine($"  Imbalance     : {Pct(Imbalance)}   (0% = perfectly balanced decisive games)");
        w.WriteLine($"  Median plies  : {MedianDecisivePlies}  (decisive games)");
        w.WriteLine();
        w.WriteLine($"  COMPOSITE SCORE : {CompositeScore:F3}   (1.0 = balanced & decisive)");
        w.WriteLine("========================================================");
    }
}
