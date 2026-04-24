namespace YesHybrid.Engine.Game;

/// <summary>
/// Mirrors the customPiece definitions in <c>variants/yeshybrid.ini</c>.
/// Used purely for display/inspection; the engine remains the source of truth.
/// </summary>
public static class PieceCatalog
{
    public enum Side { White, Black, Neutral }

    public sealed record PieceDef(char Letter, string Name, string Role, Side Side, string Betza);

    public const char Hole = '*';

    public static readonly IReadOnlyList<PieceDef> All =
    [
        new('D', "Defender",   "Party",  Side.White, "WcK"),
        new('S', "Striker",    "Party",  Side.White, "mK2cF"),
        new('C', "Controller", "Party",  Side.White, "gQ"),
        new('L', "Leader",     "Party",  Side.White, "WfF"),
        new('X', "Skirmisher", "Party",  Side.White, "KAD"),
        new('B', "Brute",      "Horde",  Side.Black, "mDcK"),
        new('A', "Artillery",  "Horde",  Side.Black, "mR3cK"),
        new('U', "Lurker",     "Horde",  Side.Black, "K"),
        new('M', "Minion",     "Horde",  Side.Black, "WfcF"),
        new('T', "Treasure",   "Horde",  Side.Black, "mW"),
        // Bloodied Party pieces (spec Section 6.1; only visible when --bloodied is on).
        new('E', "Defender*",   "Party (bloodied)", Side.White, "W"),
        new('Y', "Striker*",    "Party (bloodied)", Side.White, "mKcF"),
        new('V', "Controller*", "Party (bloodied)", Side.White, "gR"),
        new('O', "Leader*",     "Party (bloodied)", Side.White, "W"),
        new('Q', "Skirmisher*", "Party (bloodied)", Side.White, "K"),
    ];

    public static PieceDef? Find(char fenChar)
    {
        var upper = char.ToUpperInvariant(fenChar);
        foreach (var p in All)
            if (p.Letter == upper) return p;
        return null;
    }

    public static Side SideOf(char fenChar) =>
        fenChar == Hole ? Side.Neutral
        : char.IsUpper(fenChar) ? Side.White
        : Side.Black;
}
