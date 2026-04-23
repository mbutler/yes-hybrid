using YesHybrid.Engine.Uci;

namespace YesHybrid.Engine.Game;

/// <summary>
/// Produces a set of starting FENs by playing <c>k</c> uniform-random legal
/// plies from the base position, <c>n</c> times.  Seeded; reproducible.
///
/// Why we need this: the engine is deterministic at fixed depth, so N games
/// from the same FEN are N copies of the same game.  The opening book adds
/// just enough variance (per-game, not per-ply) for self-balance statistics
/// to be meaningful without destabilising engine play after the opening.
/// </summary>
public static class OpeningBook
{
    public sealed record Entry(string Fen, IReadOnlyList<string> PliesPlayed);

    public static async Task<IReadOnlyList<Entry>> GenerateAsync(
        UciEngine engine,
        string startFen,
        int count,
        int pliesPerOpening,
        int seed,
        CancellationToken ct = default)
    {
        if (count <= 0) throw new ArgumentException("count must be >= 1", nameof(count));
        if (pliesPerOpening < 0) throw new ArgumentException("plies must be >= 0", nameof(pliesPerOpening));

        var rng = new Random(seed);
        var seen = new HashSet<string>();
        var result = new List<Entry>(count);

        // Best-effort: try to produce distinct openings, but if the random
        // walk keeps colliding (small board / forced positions) we accept
        // duplicates after a bounded number of retries.
        int attempts = 0, retriesPerSlot = Math.Max(4, pliesPerOpening * 2);

        while (result.Count < count && attempts++ < count * retriesPerSlot)
        {
            ct.ThrowIfCancellationRequested();

            var moves = new List<string>();
            for (int p = 0; p < pliesPerOpening; p++)
            {
                await engine.SetPositionAsync(startFen, moves, ct);
                var legal = await engine.GetLegalMovesAsync(ct);
                if (legal.Count == 0) break; // mate/stalemate; short opening is fine
                var pick = legal[rng.Next(legal.Count)];
                moves.Add(pick);
            }

            await engine.SetPositionAsync(startFen, moves, ct);
            var fen = await ReadFenAsync(engine, ct) ?? startFen;

            if (seen.Add(fen))
                result.Add(new Entry(fen, moves));
        }

        // Fallback: pad with the base FEN if we couldn't find enough distinct
        // openings (tiny --openings-plies, extremely tight positions, etc.).
        while (result.Count < count)
            result.Add(new Entry(startFen, Array.Empty<string>()));

        return result;
    }

    private static async Task<string?> ReadFenAsync(UciEngine engine, CancellationToken ct)
    {
        await engine.SendAsync("d", ct);
        await engine.SendAsync("isready", ct);
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(3);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var line = await engine.WaitForAsync("Fen:", TimeSpan.FromMilliseconds(300), ct);
                return line["Fen:".Length..].Trim();
            }
            catch (TimeoutException) { }
        }
        return null;
    }
}
