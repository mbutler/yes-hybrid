using System.Text;

namespace YesHybrid.Engine.Game;

/// <summary>ASCII renderer for a YES Hybrid <see cref="Position"/>.</summary>
public static class BoardRenderer
{
    public static string Render(Position p, bool useColor = false)
    {
        var sb = new StringBuilder();

        // Top file labels (a..l for 12 files)
        sb.Append("    ");
        for (int x = 0; x < p.Files; x++)
            sb.Append(' ').Append(FileLetter(x)).Append(' ');
        sb.AppendLine();

        sb.Append("    ");
        sb.Append(new string('-', p.Files * 3));
        sb.AppendLine();

        for (int rIdx = 0; rIdx < p.Ranks; rIdx++)
        {
            int y = p.Ranks - 1 - rIdx;
            sb.Append($" {y + 1,2} |");
            for (int x = 0; x < p.Files; x++)
            {
                char ch = p.Squares[x, y];
                string token = ch switch
                {
                    '.' => " . ",
                    PieceCatalog.Hole => " # ",
                    _ => $" {ch} ",
                };
                sb.Append(token);
            }
            sb.Append("| ").Append(y + 1);
            sb.AppendLine();
        }

        sb.Append("    ");
        sb.Append(new string('-', p.Files * 3));
        sb.AppendLine();

        sb.Append("    ");
        for (int x = 0; x < p.Files; x++)
            sb.Append(' ').Append(FileLetter(x)).Append(' ');
        sb.AppendLine();

        sb.AppendLine();
        sb.Append($"  Side to move: {(p.SideToMove == 'w' ? "White (Party)" : "Black (Horde)")}    ");
        sb.AppendLine($"Move {p.FullmoveNumber}");

        sb.AppendLine();
        sb.AppendLine("  Legend:  # = hole/wall    UPPER = Party (White)    lower = Horde (Black)");
        sb.AppendLine("  Party:   D Defender  S Striker  C Controller  L Leader  X Skirmisher");
        sb.AppendLine("           E Defender* Y Striker* V Controller* O Leader* Q Skirmisher*  (* = bloodied)");
        sb.AppendLine("  Horde:   B Brute     A Artillery U Lurker      M Minion  T Treasure");
        return sb.ToString();
    }

    public static char FileLetter(int x) => (char)('a' + x);
    public static string SquareName(int x, int y) => $"{FileLetter(x)}{y + 1}";
}
