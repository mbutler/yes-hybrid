using YesHybrid.Engine.Uci;

namespace YesHybrid.Engine.Game;

/// <summary>
/// One self-contained YES Hybrid game driven by a single UCI engine process.
/// Used by both the interactive `play` command and the batch self-play
/// tournament runner.  Keeps the engine's position pinned to an absolute FEN
/// each ply so rule overlays (Bloodied, etc.) can rewrite the board without
/// desyncing the UCI move-list machinery.
/// </summary>
public sealed class GameLoop
{
    public UciEngine Engine { get; }
    public int Depth { get; init; } = 6;
    public int MaxPlies { get; init; } = 400;
    public TimeSpan MoveTimeout { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>If true, Party pieces take two hits (spec section 6.1).</summary>
    public bool BloodiedEnabled { get; init; }

    /// <summary>Callback fired after every applied ply (for rendering, logging, etc.).</summary>
    public Action<PlyInfo>? OnPly { get; init; }

    public GameLoop(UciEngine engine) { Engine = engine; }

    public sealed record PlyInfo(int PlyIndex, string Fen, string Move, bool BloodiedTriggered, char BloodiedFrom);

    public sealed record Outcome(
        GameResult Result,
        string Termination,
        int Plies,
        IReadOnlyList<string> Moves,
        string FinalFen);

    public enum GameResult { PartyWins, HordeWins, Draw, Unfinished }

    public async Task<Outcome> RunAsync(string startFen, CancellationToken ct = default)
    {
        var moves = new List<string>();
        var fen = startFen;
        var prev = Position.Parse(fen, Variant.Files, Variant.Ranks);

        for (int ply = 0; ply < MaxPlies; ply++)
        {
            ct.ThrowIfCancellationRequested();

            await Engine.SetPositionAsync(fen, Array.Empty<string>(), ct);

            string move;
            try
            {
                move = await Engine.GoBestMoveAsync(Depth, MoveTimeout, ct);
            }
            catch (TimeoutException)
            {
                return new Outcome(GameResult.Unfinished, "engine timeout", moves.Count, moves, fen);
            }

            if (move is "(none)" or "0000")
            {
                // Engine reports no legal move for the side to move -> they lose.
                var loser = prev.SideToMove;
                var result = loser == 'w' ? GameResult.HordeWins : GameResult.PartyWins;
                var reason = loser == 'w'
                    ? "Party has no legal response"
                    : "Treasure checkmated or captured";
                return new Outcome(result, reason, moves.Count, moves, fen);
            }

            // Apply the move on the engine side and read back the new FEN.
            await Engine.SetPositionAsync(fen, new[] { move }, ct);
            var after = await ReadFenAsync(ct) ?? fen;
            var afterPos = Position.Parse(after, Variant.Files, Variant.Ranks);

            bool triggered = false;
            char bloodiedFrom = '\0';
            if (BloodiedEnabled)
            {
                var r = BloodiedRule.Apply(prev, afterPos, move);
                if (r.Triggered)
                {
                    afterPos = r.NewPosition;
                    after = afterPos.ToFen();
                    triggered = true;
                    bloodiedFrom = r.BloodiedFrom;
                }
            }

            moves.Add(move);
            fen = after;
            prev = afterPos;

            OnPly?.Invoke(new PlyInfo(ply + 1, fen, move, triggered, bloodiedFrom));

            // Quick outcome check on material, independent of engine verdict.
            var material = prev.EvaluateOutcome();
            if (material == GameOutcome.PartyWinsByCapturingTreasure)
                return new Outcome(GameResult.PartyWins, "Treasure captured", moves.Count, moves, fen);
            if (material == GameOutcome.HordeWinsByExtinction)
                return new Outcome(GameResult.HordeWins, "Party extinct", moves.Count, moves, fen);
        }

        return new Outcome(GameResult.Unfinished, $"ply cap ({MaxPlies})", moves.Count, moves, fen);
    }

    private async Task<string?> ReadFenAsync(CancellationToken ct)
    {
        await Engine.SendAsync("d", ct);
        await Engine.SendAsync("isready", ct);
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(3);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var line = await Engine.WaitForAsync("Fen:", TimeSpan.FromMilliseconds(400), ct);
                return line["Fen:".Length..].Trim();
            }
            catch (TimeoutException) { }
        }
        return null;
    }

    public static string ResultTag(GameResult r) => r switch
    {
        GameResult.PartyWins => "1-0",
        GameResult.HordeWins => "0-1",
        GameResult.Draw      => "1/2-1/2",
        _                    => "*",
    };
}
