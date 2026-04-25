namespace YesHybrid.Desktop;

/// <summary>Player-facing labels from <c>piece-mapping.md</c>; engine letters stay the source of truth.</summary>
internal static class PiecePresentation
{
    public static string ShortLabel(char fenChar)
    {
        if (fenChar is '.' or '*' or '#')
            return fenChar is '*' or '#' ? "▓" : "";
        return char.ToLowerInvariant(fenChar) switch
        {
            'd' or 'e' => "Gu",
            's' or 'y' => "Sc",
            'c' or 'v' => "Mg",
            'l' or 'x' => "Rg",
            'o' or 'q' => "··",
            't' => "Tr",
            'b' => "Wr",
            'a' => "Ar",
            'u' => "Fi",
            'm' => "Sp",
            _ => char.IsLetter(fenChar) ? fenChar.ToString() : "",
        };
    }

    /// <summary>Two lines: name/role, then a compact v1.1 move + capture line for learning.</summary>
    public static string? TooltipText(char ch)
    {
        if (ch is '.') return null;
        if (ch is '*')
            return "Wall (blocked terrain)\n" +
                   "Impassable; no capture; blocks movement, sliding, and landings.";
        if (ch is '#')
            return "Pillar (blocked terrain)\n" +
                   "Impassable; no capture; blocks movement, sliding, and landings.";
        if (!char.IsLetter(ch)) return null;

        var k = char.ToLowerInvariant(ch);
        var name = k switch
        {
            'd' => "Guard (Defender)",
            'e' => "Guard (Defender) — bloodied",
            's' => "Scout (Striker)",
            'y' => "Scout (Striker) — bloodied",
            'c' => "Mage (Controller)",
            'v' => "Mage (Controller) — bloodied",
            'l' => "Leader",
            'x' => "Rogue (Skirmisher)",
            'o' => "Leader — bloodied",
            'q' => "Rogue (Skirmisher) — bloodied",
            't' => "Trove (Treasure) — royal",
            'b' => "Wraith (Brute)",
            'a' => "Brute (Artillery)",
            'u' => "Fiend (Lurker)",
            'm' => "Spawn (Minion)",
            _ => "Piece",
        };

        var how = k switch
        {
            'd' => "Move 1 orthogonally. Capture 1 in any direction.",
            'e' => "Move and capture: 1 orth only (Wazir; lost all-around capture).",
            's' => "Move 1–2 in any direction. Capture 1 diagonally.",
            'y' => "Move 1 any. Capture 1 diagonally (shorter than fresh S).",
            'c' => "Grasshopper queen: on a line, over exactly one screen, land on the first square beyond; move or capture there.",
            'v' => "Grasshopper on rook lines only (no diagonals); same beyond-screen rule as Mage.",
            'l' => "Move or capture: 1 orth or 1 forward-diagonal (WfF).",
            'x' => "Move or capture: 1 any direction, or leap exactly 2 orth or diagonal.",
            'o' => "Move and capture: 1 orth only (no forward diagonal).",
            'q' => "Move or capture: 1 any direction; no 2-leap (bloodied skirmisher).",
            't' => "Move 1 orth. Cannot capture. Royal: Party wins if the Treasure is mated (or captured, tabletop).",
            'b' => "Move: leap 2 orthogonally. Capture: 1 adjacent (any direction).",
            'a' => "Move: slide up to 3 orthogonally. Capture: 1 adjacent (any direction).",
            'u' => "Move and capture: 1 in any direction (Wazir + Ferz, 8 neighbors).",
            'm' => "To empty: 1 orth (any side). To capture: 1 diagonally forward (toward Party) — you cannot step diagonally onto an empty square.",
            _ => "See RULES.md for this piece.",
        };

        return $"{name}\n{how}";
    }
}
