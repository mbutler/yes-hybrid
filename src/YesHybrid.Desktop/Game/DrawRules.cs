using YesHybrid.Engine.Game;
using BoardPosition = YesHybrid.Engine.Game.Position;

namespace YesHybrid.Desktop;

/// <summary>50-move and threefold (same as RULES.md quiet / repetition) using engine FEN.</summary>
internal static class DrawRules
{
    /// <summary>Chess halfmove clock: 100 halfmoves = 50 full moves without the relevant reset.</summary>
    public const int HalfmoveDrawThreshold = 100;

    /// <summary>Board + side to move + castling + en passant (fields 0–3); same as chess repetition key.</summary>
    public static string RepetitionKey(string fen)
    {
        var p = fen.Split(' ', 6, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 4) return fen.Trim();
        return string.Join(" ", p[0], p[1], p[2], p[3]);
    }

    public static bool IsHalfmoveDraw(BoardPosition pos) => pos.HalfmoveClock >= HalfmoveDrawThreshold;
}
