# How to read the v5 overnight sweep

**Configuration:** 6 rule sets, **300 games each**, **depth=8**,
**max-plies=600**, parallel=4, seed=20260424.  All 6 rule sets pin
`searchDepth: 8` and `maxPlies: 600` in their JSON so the run is
depth-anchored regardless of the CLI flags.  All are derived from v3's
winner `minion-fc` (Minion `WfcF`).

## What v5 exists to answer

v4 exposed two failures of our depth-6 baseline:

1. **Depth sensitivity:** going from depth 6 to depth 8 swung Party%(dec)
   by about 33pp (v4-mf-baseline 59.7% -> v4-mf-depth-8 26.4%).  The
   depth-6 balance point was an artifact of shallow search, not a real
   equilibrium.
2. **Ply-cap bias:** the 400-ply cap was hiding Party wins.  Raising it
   to 600 recovered additional Party-won games that were being rounded
   off as "unfinished."

Both failures are fixable by fixing the harness.  v5 does exactly that
and then re-probes the design space from a depth-anchored baseline.

> We now ask: at depth 8 / ply-cap 600, where is the real balance
> point?  Does the `minion-fc` family cross 50% Party(dec) somewhere
> between 6 and 8 Minions, or do we need a structural buff (Striker
> range, Lurker range) instead?

If v5 lands a rule set in the 40-60% Party(dec) window with imbalance
<= 20% and unfinished <= 10%, we have a **shippable depth-anchored
baseline** and the next step is SPSA on piece values.  If nothing lands
in that window, we need a different balance knob (and v5 will have
told us which direction to push).

## The 6 rule sets

| Rule set | Change on top of minion-fc | Hypothesis |
|----------|----------------------------|-----------|
| `v5-d8-baseline`        | *(= minion-fc)*                                    | Anchor. What is true Party%(dec) when depth and ply cap are honest?  v4's depth-8 probe said 26.4% at 150 games; does 300 games confirm Horde dominance? |
| `v5-d8-minion-9`        | +1 Minion at a7 (9-piece rank-7 Horde)             | Gradient check in the Horde-buff direction.  We expect Party%(dec) to move further *down* from baseline.  If it doesn't, there's a non-linearity in the Minion axis at depth 8. |
| `v5-d8-minion-7`        | -1 Minion at j7 (7-piece rank-7 Horde)             | Primary Horde-nerf candidate.  v4's depth-6 run showed -1 Minion moves Party by about +14pp; if that gradient holds at depth 8 we'd expect around 40% here, which is close to balanced.  Smoke run (2 games) already split 1-1. |
| `v5-d8-minion-6`        | -2 Minions (drop i7 and j7) (6-piece rank-7 Horde) | Aggressive Horde nerf.  Insurance in case minion-7 is still too Horde-heavy.  If this overshoots to 60%+ Party, we've bracketed the balance point between 6 and 7 Minions. |
| `v5-d8-striker-range`   | Striker `mK2cF` -> `mK3cF`                         | Range-based Party buff.  v4 showed +1 Striker range moves Party +10pp at depth 6 with Imbalance almost unchanged; does that gradient survive depth 8 on top of a depth-anchored baseline? |
| `v5-d8-lurker-range`    | Lurker `N` -> `NN` (Nightrider / double knight)    | New axis: the Lurker has been a plain Knight since v1.  Giving it nightrider range is a meaningful Horde buff; expected to push Party% further down.  Probes whether Lurker mobility is a balance lever we've been leaving on the floor. |

## What "good" looks like

- **`v5-d8-minion-7` Party%(dec) in [40, 60]** with imbalance <= 20%
  and unfinished <= 10%: **ship it**.  That becomes the new canonical
  `yeshybrid` rule set and SPSA target.
- **`v5-d8-minion-6` in [40, 60]**: ship minion-6 as the canonical; note
  that the balance point sits at a surprisingly small Horde (design
  implication: piece values are dominated by structural minion count).
- **Minion 6 and 7 both land on the same side of 50%** (e.g. both <40%
  or both >60%): the Minion-count axis alone isn't enough at depth 8
  and we'll combine it with Striker/Lurker range in v6.
- **Striker-range delivers in [40, 60]**: we have a second knob; may
  prefer it if minion-7 is worse because range changes don't alter the
  opening-book alignment with the current FEN.
- **Lurker-range tips Party to <20%**: confirms Lurker mobility is a
  strong Horde lever and documents the direction for future tuning
  (reduce Lurker range to buff Party).

## What the metrics are telling you

- **Party%(dec)** = share of decisive games won by Party (White).
  50% is perfect balance; the 300-game 95% CI width is roughly +/- 6pp.
- **Imbalance** = |2 * Party%(dec) - 1|, how one-sided decisive games
  are.  Composite multiplies decisiveness by (1 - imbalance), so a
  rule set that's decisive *and* balanced wins.
- **Unfinished** = games that hit max-plies (600 for v5) without a
  winner.  We expect this to drop vs. v4 at 600-cap because we're
  also going deeper.  If a rule set still has >20% unfinished at
  depth 8 / 600 plies, the game has a stalling pathology and should
  be rejected regardless of Party%.
- **Median plies** = median length of decisive games.  At depth 8 we
  expect decisive games to be *longer* than at depth 6 (stronger play
  from both sides) but not anywhere near the 600-ply cap.  If they
  cluster near the cap, unfinished-rate tells the real story.

## Reading the output files

- `sweep-summary.md` / `.csv` - ranked table across the six rule sets.
- `match-<name>.log` - per-rule-set Match stdout, including
  per-game termination reasons.  This is the file to check when a
  rule set's composite is unexpected: does it match v5 smoke behavior,
  or are we seeing a new kind of termination?
- `match-<name>.pgn` - the full PGN archive; useful if we need to
  hand-inspect games that ended unusually (e.g. Party extinction in
  <200 plies, or a 600-ply draw that should have been decisive).

## Budget

- 1800 games total (6 x 300) at depth 8 / ply-cap 600.
- Smoke run observed 13.07s / game at parallel 4; real sweep is
  order-of-magnitude 1800 * 13 / 4 = ~100 min wall clock, modulo
  rule sets that hit the ply cap more often.  v4 FINDINGS estimated
  130-160 min; plan for an overnight window.
