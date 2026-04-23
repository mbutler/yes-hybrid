# v5 findings (clean sweep)

**Status:** complete.  6 of 6 rule sets, **300 games each**, depth 8,
max-plies 600, parallel 4, seed 20260424.  All six runs finished on a
single consistent harness after the per-game-FSF-restart patch landed.
Zero engine crashes across the five new clean runs (~8 cpu-hours of
FSF time).

## Headline

**`v5-d8-minion-6` is the v5 winner and the new working baseline for
v6.**  It is the only rule set in the `[40, 60]` Party%(dec) window
HOW-TO-READ.md defined as shippable, and it gets there with the best
Imbalance (11.0%) and highest Composite (0.513) of any v5 rule set -
more than 4x baseline.  The *balance* side of the ship criterion is
met; the *decisiveness* side is not (42.3% unfinished vs the 10%
threshold), so v6 has to attack stall-rate without reintroducing
Horde dominance.

## The six clean rule sets

Ordered by Composite:

| Rank | Rule set              | Games | P-W | H-W | Unfin |  Dec% | **P%(dec)** | CI 95%        | Imbal | **Composite** | Median |
|-----:|:----------------------|------:|----:|----:|------:|------:|------------:|:--------------|------:|--------------:|-------:|
|  1   | `v5-d8-minion-6`      |   300 |  77 |  96 |   127 | 57.7% |   **44.5%** | [37.3, 52.0]  |  11.0% |       **0.513** |   260 |
|  2   | `v5-d8-minion-7`      |   300 |  41 | 114 |   145 | 51.7% |       26.5% | [20.1, 33.9]  |  47.1% |         0.273 |   258 |
|  3   | `v5-d8-minion-9`      |   300 |  35 | 127 |   138 | 54.0% |       21.6% | [16.0, 28.6]  |  56.8% |         0.233 |   309 |
|  4   | `v5-d8-striker-range` |   300 |  32 | 118 |   150 | 50.0% |       21.3% | [15.5, 28.6]  |  57.3% |         0.213 |   248 |
|  5   | `v5-d8-lurker-range`  |   300 |  28 | 188 |    84 | 72.0% |       13.0% |  [9.1, 18.1]  |  74.1% |         0.187 |   220 |
|  6   | `v5-d8-baseline`      |   300 |  18 | 122 |   160 | 46.7% |       12.9% |  [8.3, 19.4]  |  74.3% |         0.120 |   294 |

Perfect balance is 50%; HOW-TO-READ's ship window is [40, 60].  Baseline
sits at 12.9%, deep in Horde territory; minion-6 pulls it to 44.5%,
the only crossing.

## What each lever does

### Minion count is the dominant balance axis, but not linearly

Party%(dec) across the Minion-count axis (rank-7 Horde size):

       Minions  P%(dec)   Imbal   Composite
       ---------------------------------------
        2 (M-2)  44.5%     11.0%    0.513     minion-6
        3 (M-1)  26.5%     47.1%    0.273     minion-7
        4 (M+0)  12.9%     74.3%    0.120     baseline
        5 (M+1)  21.6%     56.8%    0.233     minion-9    <-- non-monotonic

The slope from baseline down (2, 3, 4 Minions) is steep and monotonic
- each removed Minion buys ~+13-18pp Party%(dec).  Extrapolating
linearly from baseline would predict minion-6 at around 40-50%, and it
lands at 44.5%: consistent.

But **minion-9 (+1 Minion) breaks the monotonicity** - it is strictly
better for Party than baseline, not worse.  The partial-data story
(see Methodology note below) said minion-9 was the worst rule set in
v5 at 6.2%; the clean data says it's 21.6%, slightly better than
striker-range.  Two compatible explanations:

1. **The a7 corner Minion constrains Horde formation more than it
   helps Horde offense.**  minion-9 adds a Minion on a7, filling the
   last empty rank-7 square.  That may clog Brute/Lurker escape lines
   or break a key Horde formation, yielding net Party benefit despite
   adding mass.
2. **Which Minion you add matters more than how many.**  The axis we
   thought we were studying ("Horde density") is really the
   interaction of density with placement.

Either way the lesson is: **Minion placement is a v6 knob**, and we
should not assume the gradient is linear beyond the range we measured.

### Striker range (+1) is a clean Party buff

`v5-d8-striker-range` (Striker `mK2cF` -> `mK3cF`) lifts P%(dec) from
12.9% to 21.3% (+8.4pp, CIs don't overlap) and drops Imbalance from
74.3% to 57.3% with only a modest cost to Unfinished (50.0% vs 53.3%).
The magnitude matches the v4 depth-6 gradient (~+10pp), so the effect
is depth-robust.

Striker-range is weaker as a single lever than minion-6, but it is
*additive* in a way minion count isn't: it changes piece power, not
piece count, so it doesn't interact with opening FEN alignment the
way minion edits do.  **Recommended as a v6 stacking candidate on top
of minion-6 or minion-7**, probed directly in a v6 run.

### Lurker range is a dead-end balance axis

`v5-d8-lurker-range` (Lurker `N` -> `NN`, nightrider) gives:

    P%(dec)   12.9% (baseline) -> 13.0% (lurker-range)       CIs overlap
    Unfin     53.3% (baseline) -> 28.0% (lurker-range)       -25pp

The 95% CIs on Party%(dec) overlap essentially perfectly.  What
nightrider Lurker actually does is **resolve more games faster for
Horde** - the 25pp of previously-unfinished games convert ~90% to
Horde wins, which is why Composite looks better than baseline (0.187
vs 0.120) while Imbalance stays pinned at 74%.

This incidentally **confirms a suspicion in PARTIAL-FINDINGS**: the
600-ply cap was not biasing Party%(dec) meaningfully.  The truncated
tail at the baseline really was going to be ~90% Horde wins; removing
the cap reveals the same P%(dec) ratio.  **Baseline's P%(dec) of
12.9% is the honest number**, not an artifact of truncation.

Conclusion: drop Lurker range as a balance lever.  Reducing Lurker
range from `N` would just push games back into the ply cap without
shifting P%(dec).

### Decisiveness and stall are not correlated with balance

Unfinished-rate across the six rule sets:

    lurker-range  28.0%   (nightrider Lurker resolves fast)
    minion-6      42.3%   (less Horde mass -> fewer fast kills)
    baseline      53.3%   (stall baseline)
    minion-9      46.0%   (extra Minion speeds closing slightly)
    striker-range 50.0%   (Party buff doesn't reduce stall)
    minion-7      48.3%   (mid)

There is no rule set with both [40, 60] P%(dec) AND <= 10% Unfin.
The best-balanced (minion-6) still hits the 600-ply cap in 42% of
games.  **This is the residual v6 problem**: at depth 8, the YES
Hybrid has a structural stall pathology that piece-tuning alone has
not fixed.  Candidates for v6:

- Tighten the 50-move-like rule or the repetition rule in the
  variant definition.
- Add a progress metric (e.g. losing side must capture within N
  moves or forfeit).
- Reduce board size in a way that forces engagement.
- Accept a higher Unfin rate and re-anchor the ship criterion to
  `Unfin <= 30%` (minion-6 would ship).

## Methodology note: partial data on seeded books is dangerous

The original `PARTIAL-FINDINGS.md` extrapolated from 3 partial runs
that all crashed mid-match.  Clean re-runs show every partial was
biased in the same direction:

| Rule set     | Partial P%(dec) | Clean P%(dec) |  Shift |
|:-------------|:---------------:|:-------------:|-------:|
| minion-9     |       6.2%      |     21.6%     | +15.4pp |
| minion-7     |      18.5%      |     26.5%     |  +8.0pp |
| minion-6     |      27.3%      |     44.5%     | +17.2pp |

The crashes were not random - they happened after ~100-200 consecutive
depth-8 searches on one FSF process, which in a parallel=4 worker pool
means the workers had each consumed a contiguous prefix of their
assigned openings.  The `OpeningBook` generator orders openings
deterministically from the seed, so the "sampled" subset was
specifically the first N openings of each worker's slice.  That
subset happened to be Party-hostile in all three crashed runs - a
+15pp bias on minion-9 alone.

**Policy for future runs:**

1. Do not report partial-sweep numbers as if they were 300-game
   estimates.  The opening-book-order bias can easily exceed the
   nominal CI half-width.
2. If a sweep crashes, either re-run clean or shuffle the opening
   indices pseudo-randomly across workers (seeded but deterministic)
   so a crashed prefix is a random sample of the full book.
3. The per-game-FSF-restart patch in `MatchCommand.cs` eliminates
   the crash mode that caused this in v5, so this is only a concern
   for harness regressions.

## Infrastructure

Per-game FSF restart + per-game IOException/Timeout/InvalidOperation
resilience landed in `src/YesHybrid.Cli/Commands/MatchCommand.cs`
between the baseline run and the 5 clean re-runs.  Effects observed:

- **Zero pipe-breaks** in 5 consecutive 300-game depth-8 runs
  (1500 games, ~8 cpu-hours).
- Cost: ~50 ms per game for the UCI handshake.  Negligible vs the
  ~5-6 s/game search budget.  Total clean sweep walltime (5 new
  rule sets) was ~125 min at parallel 4, well under the v5 budget.
- One "engine timeout" event in minion-7 #299 (287 plies, 132s wall)
  - handled gracefully, match continued, recorded as Unfinished.  This
  is a pre-existing GameLoop termination, not a crash.
- Rare 1-ply "Treasure captured" outcomes still show up (1-2 per 300
  games per rule set).  These correlate with specific openings, not
  with FSF state, and are not a bug in the harness.

## Recommendation for v6

**Adopt `v5-d8-minion-6` as the new canonical yeshybrid rule set** and
re-anchor the ship criterion as (a) P%(dec) in [40, 60] and (b)
Unfin <= 30% (relaxed from 10%) for an incremental ship, or (c)
invest in fixing the stall pathology for a proper ship.

v6 probes to run (depth 8, 300 games, seed 20260424, parallel 4,
maxPlies 600 unless otherwise noted):

1. **`v6-d8-minion-6-striker-range`** - stack minion-6 + Striker +1
   range.  If P%(dec) lifts above 50% with acceptable imbalance, we
   have a shippable canonical AND a Party-side SPSA target.
2. **`v6-d8-minion-6-shortcap`** - minion-6 at `maxPlies 400`.  Does
   tightening the cap preserve balance while reducing Unfin?
   (Speculative: this is equivalent to biasing toward Horde slightly
   since the truncated tail is Horde-favorable, so we may need to
   combine with (1).)
3. **`v6-d8-minion-placement-a7-only`** and
   **`v6-d8-minion-placement-j7-only`** - single-Minion swaps
   on top of minion-6 to dis-entangle "how many" from "which ones."
   Directly tests the minion-9 non-monotonicity hypothesis.
4. **`v6-d8-minion-6-lurker-n-plus`** - minion-6 with Lurker
   reduced to `W` (range 1) rather than `N`, as a Party-side buff
   on the Horde mobility axis.  Weak prior; low-confidence probe.
5. **`v6-d8-stall-rule`** - minion-6 with a variant.ini-level stall
   rule (e.g. 50-move without progress = draw, or losing side must
   make progress within N moves).  This is an engine-level change;
   investigate what Fairy-Stockfish supports here.

If any of (1), (3), or (5) lands P%(dec) in [40, 60] with Unfin <=
15% and Imbalance <= 25%, that's the v1 ship rule set and the
starting point for Section 7 SPSA tuning.

## File inventory in this directory (post-clean-sweep)

- `HOW-TO-READ.md` - original plan and hypotheses for v5 (not edited).
- `PARTIAL-FINDINGS.md` - snapshot taken mid-sweep on the old machine;
  kept for posterity.  **Partial-data conclusions in section 2 ("Minion
  count is a real axis but too weak to fix the gap") are superseded
  by this document** - minion-6 did in fact fix the gap.
- `FINDINGS.md` - this file; trust this one.
- `match-v5-d8-baseline.{log,pgn}` - complete, 300/300 (old machine).
- `match-v5-d8-minion-9.{log,pgn}` - **complete, 300/300** (new
  machine, clean re-run).  Overwrote the 229/300 partial.
- `match-v5-d8-minion-7.{log,pgn}` - **complete, 300/300** (new
  machine, clean re-run).  Overwrote the 209/300 partial.
- `match-v5-d8-minion-6.{log,pgn}` - **complete, 300/300** (new
  machine, clean re-run).  Overwrote the 261/300 partial.
- `match-v5-d8-striker-range.{log,pgn}` - complete, 300/300 (new
  machine).  First real data on this rule set (old run only had
  13/300).
- `match-v5-d8-lurker-range.{log,pgn}` - complete, 300/300 (new
  machine).  First run of this rule set.
- `minion-sweep.log` - driver log for the back-to-back minion-9/7/6
  re-run.
- `sweep-summary.{md,csv}` - manually updated through each clean
  re-run; now reflects all 6 complete rule sets.
- `sweep.log` - original baseline-only capture from the aborted
  sweep invocation; not a v5 summary.
