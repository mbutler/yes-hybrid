# How to read the v2 overnight sweep

Started: 2026-04-22, depth-6, 100 games/rule-set, parallel-4.

## The structural question we're testing

> Should we keep Treasure as a stationary piece, or replace it with a **flagged
> square** the Leader must reach?  And does adding **Minion promotion** +
> **Minion forward-capture** pull weight on top of that, or are they redundant?

Each rule set isolates one hypothesis.  Read the summary as a **diff against
`baseline`** — that's the control.

| Rule set       | Change from baseline                         | Hypothesis being tested              |
|----------------|---------------------------------------------|--------------------------------------|
| `baseline`     | -                                           | Control                              |
| `bloodied`     | Party pieces take 2 hits                    | Decisiveness knob (from earlier run) |
| `flag-only`    | Treasure removed, Leader-to-g8 wins         | Does a geometric objective cure stalls? |
| `promo-only`   | Minions promote to Brute on rank 1          | Does tempo pressure force Party to act? |
| `minion-caps`  | Minion `W` -> `WcF` (diagonal capture)      | Do Minions need to be real threats to create a trade economy? |
| `flag+promo`   | flag + promotion                            | Do these compose?                    |
| `all-in`       | flag + promo + minion-caps                  | Full v2 design                       |

## What "good" looks like

The **composite score** = `(1 - Imbalance) * DecisiveRate`.

- **Composite near 0.50+ = strong candidate.**  The rule set resolves games
  decisively AND the wins are split roughly evenly.
- **Composite near 0 = failure.**  Either the game stalls (low decisive) or
  one side dominates (high imbalance).
- **Party%(decisive) ~= 50%** is the primary goal.  30-70% is tolerable for v1;
  outside that band means we need to rebalance.
- **Unfinished > 30%** is a structural warning sign: the game lacks tempo.

## What the baseline told us yesterday (for context)

- Baseline: 44% unfinished, Party 77% of decisive wins.  Imbalanced and stall-prone.
- Bloodied: 26% unfinished, Party 92% of decisive wins.  More decisive but more lopsided.

If v2 designs land at **unfinished <= 20% AND Party%(decisive) in 40-60%**, we
have a viable foundation to start magnitude-tuning on.

## Likely outcome scenarios

1. **`flag-only` heavily favors Horde (< 30% Party)**.  Early smoke tests
   suggest this.  Reason: removing the Treasure-checkmate win condition
   leaves Party with no near-term pressure; Horde just grinds Party down
   by extinction.  _Not_ a failure of the design direction - it's why
   `promo-only` and `minion-caps` exist (to give Horde offensive role
   while Party has to move forward).

2. **`promo-only` and `minion-caps` stay lopsided toward Party but more
   decisive**.  They tighten baseline without flipping it, meaning the
   Treasure-as-piece rule was the root imbalance.

3. **`flag+promo` lands near balanced** — the ideal outcome.  Promotion
   gives Horde teeth, the flag gives Party a reachable goal, and the two
   cancel out.

4. **`all-in` overshoots one way or the other**.  Adding minion-captures
   on top of flag+promo might swing too far; data will tell.

## Files in this folder

- `sweep-summary.md`      <- rank table.  **Read this first.**
- `sweep-summary.csv`     <- same data, machine-readable.
- `match-<name>.log`      <- per-rule-set full game log.
- `match-<name>.pgn`      <- all games in PGN for replay / deeper analysis.

## Next steps after the sweep

Based on the composite ranking:

- **Winner composite >= 0.40**: lock the structural design, generate
  magnitude-tuning rule sets (Minion counts, Party piece stats), run a
  follow-up sweep over those.
- **Winner composite 0.20-0.40**: structural design is directionally right
  but weak; pick the best variant as the new baseline and add more
  structural probes (board size, starting rank density, opening-rank mix).
- **No winner above 0.20**: back to the drawing board.  We'd likely need
  to revisit the board geometry or the asymmetric material count.
