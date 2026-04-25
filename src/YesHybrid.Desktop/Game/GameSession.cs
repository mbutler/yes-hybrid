using YesHybrid.Engine.Game;
using YesHybrid.Engine.Uci;
using BoardPosition = YesHybrid.Engine.Game.Position;

namespace YesHybrid.Desktop;

internal enum DesktopPlayMode
{
    HumanVsEngine,
    HumanVsHuman,
}

/// <summary>Which side the human controls in <see cref="DesktopPlayMode.HumanVsEngine"/> (Party = White).</summary>
internal enum DesktopHumanSide
{
    Party,
    Horde,
}

/// <summary>Owning FEN, move list, and engine interactions for the desktop board.</summary>
internal sealed class GameSession
{
    public UciEngine Engine { get; }
    public string Fen { get; private set; } = Variant.DefaultStartFen;
    public List<string> MoveUci { get; } = new();
    public DesktopPlayMode Mode { get; set; } = DesktopPlayMode.HumanVsEngine;
    public DesktopHumanSide HumanSide { get; set; } = DesktopHumanSide.Party;
    public int EngineSearchDepth { get; set; } = 6;

    private readonly Dictionary<string, int> _repetitionCounts = new();

    public GameSession(UciEngine engine) => Engine = engine;

    /// <summary>After every committed FEN (including the start position after <see cref="NewGameAsync"/>).</summary>
    public void RegisterPositionForRepetition()
    {
        var k = DrawRules.RepetitionKey(Fen);
        _repetitionCounts[k] = _repetitionCounts.GetValueOrDefault(k) + 1;
    }

    public bool IsTripleRepetitionDraw =>
        _repetitionCounts.GetValueOrDefault(DrawRules.RepetitionKey(Fen)) >= 3;

    public void ClearDrawHash()
    {
        _repetitionCounts.Clear();
    }

    public BoardPosition GetPosition() => BoardPosition.Parse(Fen, Variant.Files, Variant.Ranks);

    public async Task NewGameAsync(CancellationToken ct = default)
    {
        MoveUci.Clear();
        ClearDrawHash();
        Fen = Variant.DefaultStartFen;
        await Engine.SetPositionAsync(Fen, null, ct);
        RegisterPositionForRepetition();
    }

    /// <summary>Legals + <see cref="Fen"/> from one engine replay of <see cref="MoveUci"/> (avoids drift between stored FEN and the move list).</summary>
    public async Task<IReadOnlyList<string>> GetLegalMovesAuthoritativeAsync(CancellationToken ct = default)
    {
        var (fen, legals) = await Engine.GetFenAndLegalMovesFromSetupAsync(Variant.DefaultStartFen, MoveUci, ct);
        if (fen is null)
        {
            if (MoveUci.Count == 0)
                fen = Variant.DefaultStartFen;
            else
            {
                fen = await Engine.SetPositionGetFenAsync(Variant.DefaultStartFen, MoveUci, ct)
                    ?? await Engine.GetCurrentFenAsync(ct);
            }
        }
        if (fen is null)
            throw new InvalidOperationException("Fairy-Stockfish did not return a FEN; cannot sync the board with legal moves.");
        Fen = fen;
        return legals;
    }

    public async Task ApplyUserMoveUciAsync(string uci, CancellationToken ct = default)
    {
        var next = await Engine.SetPositionGetFenAsync(Fen, new[] { uci }, ct);
        if (next is null)
            next = await Engine.GetCurrentFenAsync(ct);
        if (next is null)
            throw new InvalidOperationException("Fairy-Stockfish did not return a FEN after the move; the in-memory and engine boards may be out of sync.");
        MoveUci.Add(uci);
        Fen = next;
        RegisterPositionForRepetition();
    }

    /// <summary>Engine plays the current side; returns the UCI move, or <c>(none)</c> if the engine reports no move.</summary>
    public async Task<string> EngineBestMoveAsync(CancellationToken ct = default)
    {
        var (move, newFen) = await Engine.GoBestGetFenAsync(Fen, EngineSearchDepth, TimeSpan.FromMinutes(1), ct);
        if (move is "(none)" or "0000")
            return move;
        if (newFen is null)
            newFen = await Engine.GetCurrentFenAsync(ct);
        if (newFen is null)
            return move;
        MoveUci.Add(move);
        Fen = newFen;
        RegisterPositionForRepetition();
        return move;
    }
}
