# How to read the v4 overnight sweep

**Configuration:** 7 rule sets, 150 games each, global depth=6, global
max-plies=400, parallel=4, seed=20260424.  Two rule sets override the
global harness knobs (`depth-8` and `ply-cap-600`).  All 7 rule sets
are derived from v3's winner `v3-mc-minion-fc` (Minion `WfcF` - the
forward-only-capture variant), which tested at composite 0.560 / Party
54.3% / imbalance 8.7% in the v3 sweep.

## The structural question we're testing

> `minion-fc` lands on a balanced point at depth-6/150 games.  Is that
> point **robust** to harness knobs (deeper search, longer games) and
> does it respond **predictably** to small magnitude tweaks (Minion
> count +/- 1, Striker range +1, Artillery range -1)?

If yes on both, we are SPSA-ready: we have a balanced rule set that
moves in sensible ways to small parameter nudges, which is exactly the
precondition for automated piece-value tuning.

v4 does **not** test new structural mechanics.  The v3 sweep already
told us to park flag / compact / dual-objective directions.  v4 is a
"does this baseline actually hold up" check, plus first-gradient
readings on two piece-value-adjacent axes.

| Rule set                 | Change on top of minion-fc                       | Hypothesis being tested                                  |
|--------------------------|--------------------------------------------------|----------------------------------------------------------|
| `v4-mf-baseline`         | *(= minion-fc)*                                  | Reproducibility of v3 winner at a new seed.              |
| `v4-mf-ply-cap-600`      | `maxPlies: 600` (from 400)                       | How many of the 39% "unfinished" games finish in 400-600? |
| `v4-mf-depth-8`          | `searchDepth: 8` (from 6)                        | Does deeper search change the balance point?  If Party%(dec) drifts >= 10pp, depth-6 results are not a reliable baseline for SPSA. |
| `v4-mf-minion-9`         | Add a 9th mid-rank Minion at a7                  | First magnitude probe: how much does +1 Minion tip the scale? |
| `v4-mf-minion-7`         | Remove a Minion at j7                            | Symmetric probe: how much does -1 Minion tip Party-ward? |
| `v4-mf-striker-range`    | Striker `mK2cF` -> `mK3cF`                       | One-step Party power-up: does +1 range for S change the balance?  Acts as a proxy for "what does +1 centipawn on Striker do". |
| `v4-mf-artillery-range`  | Artillery `mR3cK` -> `mR2cK`                     | Matching Horde nerf: does -1 range for A change the balance?  Proxy for "what does -1 centipawn on Artillery do". |

## What "good" looks like

- **Baseline reproduces within CI** of v3's 54.3% / composite 0.560.
  At 150 games the 95% CI width is roughly +/- 10pp; if Party%(dec) lands
  in [44, 64] we're reproducing.  Outside that, something drifted.
- **ply-cap-600 resolves most of the 58 unfinished games** from v3.
  If most of those 58 games finish decisively and match the same
  Party-share as the already-decided games, the 400-ply cap isn't
  cutting off one side systematically.  If Party%(dec) *shifts*,
  the ply cap was biasing our data.
- **depth-8 stays within +/- 10pp of baseline Party%(dec)**.
  This is the strongest claim we can make about "depth-6 data
  generalises".  If depth-8 diverges, it doesn't invalidate v3 but
  it means SPSA needs to tune at the depth we'll ship.
- **minion-9 pushes Party% DOWN by 8-15pp; minion-7 pushes it UP by
  8-15pp**.  Two symmetric, well-behaved gradient readings are the
  ideal SPSA precondition.  If either is flat (<= 3pp) or inverted,
  Minion count is not the lever we thought.
- **striker-range and artillery-range each move Party% by 5-15pp**
  in their expected direction.  These are the first "piece value"
  gradient readings; if they behave, SPSA on piece values is worth
  running.  If they saturate (huge move, >= 20pp) or flat-line, we
  need different levers.

## Likely outcome scenarios

1. **Best case (SPSA-ready).**  Baseline matches v3; depth-8 is within
   10pp; minion-9 and minion-7 are roughly symmetric around baseline;
   striker and artillery probes each shift Party% 5-15pp in the
   predicted direction.  Next step: write the SPSA harness.

2. **Depth drift.**  depth-8 shifts Party% by 15-25pp.  Not a
   catastrophe - we pick a canonical depth (probably 8) and rerun
   key magnitude probes at that depth before SPSA.  Costs one more
   v4.5 mini-sweep.

3. **Saturated gradients.**  Minion 9/7 or Striker/Artillery swing
   Party% >= 20pp.  The game is fragile at this level; magnitude-tuning
   via SPSA will be twitchy.  We back off to smaller changes (e.g.
   only change one piece value at a time, at 20% steps) and run
   more games per step.

4. **Flat gradients.**  A probe moves Party% < 3pp.  That piece's
   parameters are not a useful lever for balance; skip in SPSA.

5. **Reproducibility miss.**  If `v4-mf-baseline` doesn't land within
   CI of v3's result, investigate the difference (seed? build? minor
   FSF flag?) before moving forward.

## Files in this folder

- `sweep-summary.md` / `.csv`  - **rank table, read first**
- `HOW-TO-READ.md`             - this file
- `FINDINGS.md`                - written AFTER the sweep; verdict on SPSA-readiness
- `match-<name>.log`           - per-rule-set full text log
- `match-<name>.pgn`           - all games for replay

## Next steps after the sweep

- **Best case hits**        : start writing the SPSA harness
                              (spec section 7) against `minion-fc`.
- **Depth-drift case**      : rerun the magnitude probes at the
                              canonical depth chosen, then SPSA.
- **Saturated-gradients**   : reduce probe magnitudes and re-run
                              a smaller v4.5 sweep.
- **Flat-gradients case**   : identify which pieces have useful
                              value gradients, prune the SPSA search
                              space accordingly.

## How to reproduce this sweep

```bash
dotnet run --project src/YesHybrid.Cli -c Release -- sweep \
    --games 150 --depth 6 --parallel 4 \
    --seed 20260424 \
    --out reports/v4-overnight \
    --add-rules rulesets/v4-mf-baseline.json \
    --add-rules rulesets/v4-mf-ply-cap-600.json \
    --add-rules rulesets/v4-mf-depth-8.json \
    --add-rules rulesets/v4-mf-minion-9.json \
    --add-rules rulesets/v4-mf-minion-7.json \
    --add-rules rulesets/v4-mf-striker-range.json \
    --add-rules rulesets/v4-mf-artillery-range.json
```

Expected wall-clock on Apple-silicon parallel-4: ~70-90 min.
(`depth-8` is roughly 2x per-game cost; `ply-cap-600` only slightly
slower than baseline because most games still resolve under 400.)
