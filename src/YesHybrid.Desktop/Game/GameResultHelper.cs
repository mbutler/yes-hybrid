using YesHybrid.Engine.Game;
using BoardPosition = YesHybrid.Engine.Game.Position;

namespace YesHybrid.Desktop;

internal static class GameResultHelper
{
    public static bool TryTerminalMessage(BoardPosition pos, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? message)
    {
        switch (pos.EvaluateOutcome())
        {
            case GameOutcome.PartyWinsByCapturingTreasure:
                message = "Party wins: Treasure captured or mated.";
                return true;
            case GameOutcome.HordeWinsByExtinction:
                message = "Horde wins: Party wiped out.";
                return true;
            case GameOutcome.Ongoing:
                message = null;
                return false;
            default:
                message = null;
                return false;
        }
    }

    public static string NoLegalMovesForSideToMoveMessage(char sideToMove) =>
        sideToMove == 'b'
            ? "Party wins: Horde has no legal move."
            : "Horde wins: Party has no legal move.";
}
