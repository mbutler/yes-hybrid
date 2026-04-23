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
        var prev = Position.Parse(fen);
        // Remember whether the Treasure was ever on the board in this game.
        // Flag-mode variants start without a 't' so the termination message
        // for a Horde-loss should NOT say "Treasure captured".
        bool treasureAtStart = prev.Count('t') > 0;

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
                var reason = DescribeTermination(result, prev, treasureAtStart);
                return new Outcome(result, reason, moves.Count, moves, fen);
            }

            // Apply the move on the engine side and read back the new FEN.
            await Engine.SetPositionAsync(fen, new[] { move }, ct);
            var after = await ReadFenAsync(ct) ?? fen;
            var afterPos = Position.Parse(after);

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

            // No ruleset-specific outcome check here.  The engine's
            // `bestmove (none)` signal on the loser's next turn is the sole
            // authority for terminal states - this works uniformly for
            // checkmate (royal king), extinction, and flag wins.
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

    /// <summary>
    /// Best-effort narration of *why* the game ended, inferred from the last
    /// known position.  The authoritative terminal signal is the engine's
    /// "bestmove (none)"; this helper just picks the most plausible spec-level
    /// cause (Treasure captured / Flag reached / Party extinct / stalemate).
    /// </summary>
    private static string DescribeTermination(GameResult result, Position last, bool treasureAtStart)
    {
        if (result == GameResult.PartyWins)
        {
            // In Treasure-as-royal variants the game ends either by the
            // Treasure being literally captured (no 't' in final FEN) or by
            // the Treasure being checkmated (still present, but no legal
            // moves).  Either way, the spec-level cause is the same so
            // we use the historic catch-all phrase.
            if (treasureAtStart) return "Treasure checkmated or captured";
            // Flag variants have no Treasure to begin with; the loss state
            // is either a flag piece on a flag square or Horde stalemate.
            return "Flag reached or Horde stalemated";
        }
        if (result == GameResult.HordeWins)
        {
            // Count living Party pieces (fresh + bloodied).
            int party = 0;
            foreach (var p in "DSCLXEYVOQ") party += last.Count(p);
            return party == 0 ? "Party extinct" : "Party has no legal response";
        }
        return "unknown";
    }

    public static string ResultTag(GameResult r) => r switch
    {
        GameResult.PartyWins => "1-0",
        GameResult.HordeWins => "0-1",
        GameResult.Draw      => "1/2-1/2",
        _                    => "*",
    };
}
