namespace YesHybrid.Engine.Game;

/// <summary>
/// Spec Section 6.1 "Multi-Capture": Party pieces don't die on the first
/// capture attempt - they promote in place to a Bloodied version (weaker
/// stats) and the attacker is repelled back to its origin square.
///
/// Implemented as a post-move FEN rewrite: after the engine resolves a ply,
/// we diff against the previous position; if a non-Bloodied Party piece just
/// disappeared, we roll back the attacker and drop the Bloodied analogue on
/// the captured square.  Side-to-move still flips (the attacker spent its
/// action swinging).  On subsequent hits, the Bloodied piece captures
/// normally.
/// </summary>
public static class BloodiedRule
{
    /// <summary>Fresh Party piece (uppercase FEN char) -> Bloodied counterpart.</summary>
    private static readonly IReadOnlyDictionary<char, char> FreshToBloodied = new Dictionary<char, char>
    {
        ['D'] = 'E',
        ['S'] = 'Y',
        ['C'] = 'V',
        ['L'] = 'O',
        ['X'] = 'Q',
    };

    public static readonly IReadOnlySet<char> FreshParty = new HashSet<char>("DSCLX");
    public static readonly IReadOnlySet<char> BloodiedParty = new HashSet<char>("EYVOQ");

    public static bool IsFreshParty(char fenChar) => FreshParty.Contains(fenChar);
    public static bool IsBloodied(char fenChar) => BloodiedParty.Contains(fenChar);
    public static char Bloody(char freshChar) => FreshToBloodied[freshChar];

    public record Result(Position NewPosition, bool Triggered, char BloodiedFrom);

    /// <summary>
    /// Given the board state before and after a single engine move (in UCI
    /// long-algebraic, e.g. "g1i3") and the move itself, return the corrected
    /// position.  If a fresh Party piece was captured, the return indicates
    /// Triggered=true and the attacker is rolled back.
    /// </summary>
    public static Result Apply(Position before, Position after, string uciMove)
    {
        if (uciMove.Length < 4)
            return new Result(after, false, '\0');

        var (fx, fy) = ParseSquare(uciMove[..2], before.Files);
        var (tx, ty) = ParseSquare(uciMove.Substring(2, 2), before.Files);

        char attacker = before.Squares[fx, fy];
        char victim   = before.Squares[tx, ty];

        // Only intervene when a fresh Party piece was captured.
        if (!IsFreshParty(victim)) return new Result(after, false, '\0');

        // Sanity check: was this actually a capture?  (Target had a piece, and
        // the attacker is White - Party pieces are upper-case in FEN.)
        if (attacker == '.' || attacker == PieceCatalog.Hole) return new Result(after, false, '\0');
        if (!char.IsLower(attacker))
        {
            // Attacker is also uppercase (=White) - shouldn't capture its own
            // Party piece; bail.
            return new Result(after, false, '\0');
        }

        // Rewrite: attacker back to origin, Bloodied defender on target.
        var rewritten = RewriteCapture(after, fx, fy, tx, ty, attacker, Bloody(victim));
        return new Result(rewritten, true, victim);
    }

    private static Position RewriteCapture(
        Position after, int fx, int fy, int tx, int ty, char attacker, char bloodied)
    {
        var squares = (char[,])after.Squares.Clone();
        squares[fx, fy] = attacker;   // attacker rebound
        squares[tx, ty] = bloodied;   // bloodied defender holds the square

        var fen = SerializeBoard(squares, after.Files, after.Ranks);
        fen += $" {after.SideToMove} {after.Castling} {after.EnPassant} {after.HalfmoveClock} {after.FullmoveNumber}";
        return Position.Parse(fen, after.Files, after.Ranks);
    }

    private static (int x, int y) ParseSquare(string alg, int files)
    {
        char fileCh = alg[0];
        int x = fileCh - 'a';
        int y = int.Parse(alg[1..]) - 1;
        if (x < 0 || x >= files || y < 0)
            throw new FormatException($"Bad UCI square '{alg}'.");
        return (x, y);
    }

    private static string SerializeBoard(char[,] sq, int files, int ranks)
    {
        var sb = new System.Text.StringBuilder();
        for (int rIdx = 0; rIdx < ranks; rIdx++)
        {
            int y = ranks - 1 - rIdx;
            int empty = 0;
            for (int x = 0; x < files; x++)
            {
                char ch = sq[x, y];
                if (ch == '.') { empty++; continue; }
                if (empty > 0) { sb.Append(empty); empty = 0; }
                sb.Append(ch);
            }
            if (empty > 0) sb.Append(empty);
            if (rIdx < ranks - 1) sb.Append('/');
        }
        return sb.ToString();
    }
}
