using YesHybrid.Engine.Game;
using BoardPosition = YesHybrid.Engine.Game.Position;

namespace YesHybrid.Desktop;

/// <summary>Quiet-move and threefold draws using engine FEN (kept in sync with <c>variants/yeshybrid.ini</c>).</summary>
internal static class DrawRules
{
    /// <summary>Halfmove clock at which a draw is declared in the UI. 0 = off (pair with <c>nMoveRule = 0</c>); 100 = 50 full moves with <c>nMoveRule = 50</c>.</summary>
    public const int HalfmoveDrawThreshold = 0;

    /// <summary>Board + side to move + castling + en passant (fields 0–3); same as chess repetition key.</summary>
    public static string RepetitionKey(string fen)
    {
        var p = fen.Split(' ', 6, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 4) return fen.Trim();
        return string.Join(" ", p[0], p[1], p[2], p[3]);
    }

    public static bool IsHalfmoveDraw(BoardPosition pos) =>
        HalfmoveDrawThreshold > 0 && pos.HalfmoveClock >= HalfmoveDrawThreshold;
}
