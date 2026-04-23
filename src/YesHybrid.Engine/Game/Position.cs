using System.Text;

namespace YesHybrid.Engine.Game;

/// <summary>
/// FEN-backed snapshot of a YES Hybrid board.  Parses extended FEN that
/// supports multi-digit run lengths (needed for boards wider than 9 files)
/// and the '*' obstacle character used for holes/walls.
/// </summary>
public sealed class Position
{
    public int Files { get; }
    public int Ranks { get; }

    /// <summary>[file, rank] indexed from (0, 0) = a1 (bottom-left, white side).</summary>
    public char[,] Squares { get; }

    public char SideToMove { get; }
    public string Castling { get; }
    public string EnPassant { get; }
    public int HalfmoveClock { get; }
    public int FullmoveNumber { get; }

    private Position(int files, int ranks, char[,] squares, char stm, string castling, string ep, int half, int full)
    {
        Files = files; Ranks = ranks; Squares = squares;
        SideToMove = stm; Castling = castling; EnPassant = ep;
        HalfmoveClock = half; FullmoveNumber = full;
    }

    public static Position Parse(string fen, int expectedFiles = 12, int expectedRanks = 8)
    {
        var parts = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4)
            throw new FormatException($"FEN must have at least 4 fields: '{fen}'.");

        var rankStrings = parts[0].Split('/');
        if (rankStrings.Length != expectedRanks)
            throw new FormatException(
                $"FEN board has {rankStrings.Length} ranks, expected {expectedRanks}.");

        var squares = new char[expectedFiles, expectedRanks];
        for (int rIdx = 0; rIdx < expectedRanks; rIdx++)
        {
            // FEN ranks listed top-down; convert to internal y where y=0 is rank 1.
            int y = expectedRanks - 1 - rIdx;
            var row = rankStrings[rIdx];
            int x = 0, i = 0;
            while (i < row.Length)
            {
                char ch = row[i];
                if (char.IsDigit(ch))
                {
                    int j = i;
                    while (j < row.Length && char.IsDigit(row[j])) j++;
                    int run = int.Parse(row[i..j]);
                    for (int k = 0; k < run; k++)
                    {
                        if (x >= expectedFiles)
                            throw new FormatException($"FEN rank '{row}' overflows {expectedFiles} files.");
                        squares[x++, y] = '.';
                    }
                    i = j;
                }
                else
                {
                    if (x >= expectedFiles)
                        throw new FormatException($"FEN rank '{row}' overflows {expectedFiles} files.");
                    squares[x++, y] = ch;
                    i++;
                }
            }
            if (x != expectedFiles)
                throw new FormatException(
                    $"FEN rank '{row}' has {x} squares, expected {expectedFiles}.");
        }

        char stm = parts[1].Length > 0 ? parts[1][0] : 'w';
        string castling = parts[2];
        string ep = parts[3];
        int half = parts.Length > 4 && int.TryParse(parts[4], out var h) ? h : 0;
        int full = parts.Length > 5 && int.TryParse(parts[5], out var f) ? f : 1;

        return new Position(expectedFiles, expectedRanks, squares, stm, castling, ep, half, full);
    }

    public string ToFen()
    {
        var sb = new StringBuilder();
        for (int rIdx = 0; rIdx < Ranks; rIdx++)
        {
            int y = Ranks - 1 - rIdx;
            int empty = 0;
            for (int x = 0; x < Files; x++)
            {
                char ch = Squares[x, y];
                if (ch == '.')
                {
                    empty++;
                }
                else
                {
                    if (empty > 0) { sb.Append(empty); empty = 0; }
                    sb.Append(ch);
                }
            }
            if (empty > 0) sb.Append(empty);
            if (rIdx < Ranks - 1) sb.Append('/');
        }
        sb.Append(' ').Append(SideToMove)
          .Append(' ').Append(Castling)
          .Append(' ').Append(EnPassant)
          .Append(' ').Append(HalfmoveClock)
          .Append(' ').Append(FullmoveNumber);
        return sb.ToString();
    }

    /// <summary>Counts pieces of a given (case-sensitive) FEN letter on the board.</summary>
    public int Count(char piece)
    {
        int n = 0;
        for (int x = 0; x < Files; x++)
            for (int y = 0; y < Ranks; y++)
                if (Squares[x, y] == piece) n++;
        return n;
    }

    /// <summary>Resolve YES-Hybrid victory state from the current FEN alone.</summary>
    public GameOutcome EvaluateOutcome()
    {
        // Party (White) loses when all of D, S, C, L, X - fresh OR Bloodied -
        // are gone from the board.  Bloodied letters are E, Y, V, O, Q
        // (see Section 6.1 / BloodiedRule.cs).
        bool partyAlive = false;
        foreach (var p in "DSCLXEYVOQ")
            if (Count(p) > 0) { partyAlive = true; break; }

        // Horde (Black) loses when the Treasure is captured (lower-case = Black in FEN).
        bool treasureAlive = Count('t') > 0;

        return (partyAlive, treasureAlive) switch
        {
            (false, _)     => GameOutcome.HordeWinsByExtinction,
            (true, false)  => GameOutcome.PartyWinsByCapturingTreasure,
            (true, true)   => GameOutcome.Ongoing,
        };
    }
}

public enum GameOutcome
{
    Ongoing,
    PartyWinsByCapturingTreasure,
    HordeWinsByExtinction,
}
