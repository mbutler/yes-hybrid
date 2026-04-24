# v6 findings (sweep complete)

**Status:** complete.  6 of 6 rule sets, **300 games each**, depth 8,
max-plies 600 (400 for `shortcap`), parallel 4, seed 20260424, single
142-min sweep with zero engine crashes (FSF restart-per-game from v5
held up).

## Headline

**`v6-d8-stall-rule` ships.**  It is the **only rule set that meets
all three v6 ship criteria** (P%(dec) in [40, 60], Imbalance <= 25%,
Unfin <= 15%), and it does so with the strongest Composite of the
v3-v6 era (0.793, vs v5-d8-minion-6's prior best 0.513).

What "shipping" means concretely:

- The v5 winner `v5-d8-minion-6` (rank-7 Horde reduced to 2 Minions
  at c7/f7, Minion `WfcF`) is now the canonical opening setup, and
- the standard chess `nMoveRule = 50` plus `nFoldRule = 3` are
  enabled for stall control,

both promoted into `variants/yeshybrid.ini` (was: 8-Minion v0 layout,
Minion `W`, no n-move/repetition rules).  This is the v1 of YES Hybrid
and the starting point for Section 7 SPSA tuning.

## All six rule sets, ordered by Composite

| Rank | Rule set                          | Games | P-W | H-W | Unfin |  Dec% | **P%(dec)** | CI 95%        | Imbal  | **Composite** | Median |
|-----:|:----------------------------------|------:|----:|----:|------:|------:|------------:|:--------------|-------:|--------------:|-------:|
|  1   | `v6-d8-stall-rule`                |   300 | 119 | 146 |    35 | 88.3% |   **44.9%** | [39.0, 50.9]  |  10.2% |     **0.793** |   186  |
|  2   | `v6-d8-minion-6-lurker-n-plus`    |   300 | 168 |  62 |    70 | 76.7% |       73.0% | [67.0, 78.4]  |  46.1% |       0.413   |   215  |
|  3   | `v6-d8-minion-6-striker-range`    |   300 |  60 |  88 |   152 | 49.3% |       40.5% | [33.0, 48.6]  |  18.9% |       0.400   |   269  |
|  4   | `v6-d8-minion-6-shortcap`         |   300 |  59 |  72 |   169 | 43.7% |       45.0% | [36.8, 53.6]  |   9.9% |       0.393   |   210  |
|  5   | `v6-d8-minion-placement-a7-only`  |   300 |  46 | 102 |   152 | 49.3% |       31.1% | [24.2, 38.9]  |  37.8% |       0.307   |   260  |
|  6   | `v6-d8-minion-placement-j7-only`  |   300 |  34 | 129 |   137 | 54.3% |       20.9% | [15.3, 27.7]  |  58.3% |       0.227   |   314  |

For reference, the v5 winner `v5-d8-minion-6`: P%(dec) 44.5%, CI [37.3,
52.0], Imbal 11.0%, Composite 0.513, Median 260, Unfin 42.3%.

## What each lever did

### Stall rule: the unexpected ship (probe 6)

`v6-d8-stall-rule` was filed in HOW-TO-READ as a "mostly diagnostic"
probe.  It is the v1 ship.

| Metric                | v5-d8-minion-6 | v6-d8-stall-rule | Delta            |
|-----------------------|---------------:|-----------------:|------------------|
| P%(dec)               |          44.5% |            44.9% | +0.4pp           |
| Imbalance             |          11.0% |            10.2% | -0.8pp           |
| Decisive rate         |          57.7% |            88.3% | **+30.6pp**      |
| Unfinished rate       |          42.3% |            11.7% | **-30.6pp**      |
| Median plies          |            260 |              186 | **-74 plies**    |
| Composite             |          0.513 |            0.793 | **+0.280**       |

The 50-move rule (with `nMoveRuleTypes` defaulting to `p` and the
variant having no pawns - so only captures reset the counter) does
exactly what we wanted: at our 600-ply cap, the rule fires once for
every 100 captureless plies, which is enough to convert most of the
ply-cap tail into terminations.  The terminations are split between
*explicit n-move-rule draws* (still scored as Unfinished by the
harness; that's the residual 11.7%) and *forced engine commitments*
(captures the engine would have skipped under no-rule pressure but
plays under threat of a forced draw, which produce the +30.6pp of new
decisive games).

Critically, the new decisive games are **balanced in the same
proportion as minion-6's original decisive games** (45% vs 44%) - the
rule didn't bias who wins, it just compressed who-wins-when-the-game-
actually-resolves into a sharper signal.  That's the ideal behaviour:
it's a diagnostic stall fix, not a strategic intervention.

### Lurker mobility: the strongest balance lever ever found, but it overshoots (probe 5)

`v6-d8-minion-6-lurker-n-plus` (Lurker `N` -> `W`, range-1 wazir)
moved P%(dec) from 44.5% to **73.0%**.  This is by a wide margin the
biggest single-knob effect any v3-v6 probe has produced.

Why it works:

- The Horde's offensive engine is the rank-7 cluster of Brutes,
  Lurkers, and Artillery developing through the rank 4-6 corridor
  toward the Party home.  The Lurker as a Knight is the only Horde
  piece with leaper range >1; it is the *only* way Horde gets fast
  outflanking pressure on the Party's flat-W Minion screen.
- Reducing Lurker to `W` removes that outflank entirely.  Horde
  development collapses to a pawn-grade shuffle and Party's Striker
  + Skirmisher + Artillery start scoring extinction kills as soon as
  they get into range.

Why it doesn't ship by itself:

- 73.0% is too far past 50%.  Imbalance 46.1% > 25% ship limit.
- The shape is "Party crushes a crippled Horde," not "balanced game."

What it's good for:

- **A second balance knob with known direction and magnitude.**  We
  have minion-count (Horde-side mass) and now Lurker-mobility
  (Horde-side reach).  v7+ can blend them: e.g. `v7-d8-stall-rule +
  lurker-W + restore-1-Minion` (one extra Minion at i7 or j7 to give
  Horde back some mass), which by the placement-axis data should pull
  P%(dec) from 73% back into [50, 60].
- **A SPSA Party-side power-up reserve.**  If SPSA on the canonical
  ship destabilizes balance toward Horde, partially restricting
  Lurker (something between `N` and `W` - perhaps `fW` + leap-2
  versions, or `mNcW`) is the obvious knob to pull.

### Placement matters at fixed Minion count (probes 3 and 4)

Three 3-Minion configurations, all c7+f7 fixed, varying the third
Minion:

| Third Minion location | Rule set                       | P%(dec) | CI 95%       |
|-----------------------|--------------------------------|--------:|--------------|
| a7  (corner, far side) | `minion-placement-a7-only`    |   31.1% | [24.2, 38.9] |
| i7  (mid, near Horde core) | `v5-d8-minion-7`           |   26.5% | [20.1, 33.9] |
| j7  (Horde-strong wing) | `minion-placement-j7-only`   |   20.9% | [15.3, 27.7] |

a7-only and j7-only CIs barely overlap (38.9 vs 27.7 wing-points
that are also 4.4pp apart), and the *ordering* is monotonic in
"distance from the Horde's offensive cluster."  Hypothesis 1 from
v5 FINDINGS is confirmed: **a7 constrains Horde formation; i7
sits inside the cluster; j7 reinforces it.**  The "how many Minions"
axis is really an interaction of mass with placement.

Practical implication for v7+ tuning:

- If we ever need to add Horde mass (e.g. to compensate for a Party
  buff that overshoots), prefer i7 over j7 over a7 in that order
  for Horde-side, or the reverse for Party-side.
- The placement axis is a real lever, ~10pp wide at fixed Minion
  count.  It is not a *primary* shipping lever (no placement
  candidate alone is in [40, 60]), but it is a useful **fine-tuning
  knob**.

### Striker range did not stack on minion-6 (probe 1)

| Setup                                    | P%(dec) | Delta vs baseline |
|------------------------------------------|--------:|-------------------|
| `v5-d8-baseline` (4 Minions, mK2cF)      |   12.9% | -                 |
| `v5-d8-striker-range` (4 Min, mK3cF)     |   21.3% | +8.4pp            |
| `v5-d8-minion-6` (2 Min, mK2cF)          |   44.5% | +31.6pp           |
| `v6-d8-minion-6-striker-range` (2 Min, mK3cF) | 40.5% | -4.0pp from minion-6 |

The striker-range gradient that was clean in v5 (+8.4pp on top of the
4-Minion baseline) **does not survive on top of minion-6**.  P%(dec)
actually drifts slightly downward (40.5% vs 44.5%) and Imbalance gets
slightly worse (18.9% vs 11.0%).  CIs overlap, so this could be noise
in either direction, but it definitely does *not* additively stack.

A reasonable interpretation: striker range buffs Party by giving
Striker reach to capture rank-7 Minions before they screen the
Horde's deeper pieces.  When there are only 2 Minions left (minion-6
config), there's nothing to reach for - Striker can already engage
the Horde's interior pieces from its current range.  The buff is
"wasted" on a thinned Horde.

This rules out striker-range as a v7 stacking candidate on the
canonical ship.  Save it for a v7 SPSA exploration where Horde mass
is restored.

### Shortcap moves Unfin in the wrong direction (probe 2)

| Setup                          | maxPlies | P%(dec) | Unfin  |
|--------------------------------|---------:|--------:|-------:|
| `v5-d8-minion-6`               |     600  |   44.5% |  42.3% |
| `v6-d8-minion-6-shortcap`      |     400  |   45.0% |  56.3% |

P%(dec) is unchanged within noise; Unfin went *up* by 14pp.  This
**falsifies the v5 hypothesis** that the truncated tail at 600 was
biased Horde-favorable.  The truncation had been roughly balanced
all along; tightening it just truncates earlier, which moves the
same number of resolved-late games into Unfinished and produces a
worse Unfin number, not a better one.

Combined with the stall-rule's success, the lesson is clear: the
right way to attack stalls is **terminate them earlier with an
explicit rule** (so they're truncated to a clean draw judgment),
not **truncate them earlier with a tighter cap** (which just moves
the truncation point without changing what's inside the truncated
tail).

## What went into `variants/yeshybrid.ini` (v1 ship)

Diff from v0 (= the file as it stood through v3-v5):

- `customPiece9 = m:W`         ->  `customPiece9 = m:WfcF`
  (the v3 minion-fc upgrade: Minion gains forward-diagonal capture)
- `startFen = 4abtba3/2mubmbumm2/12/3*4*3/4**6/12/12/3XCLSD4 w - - 0 1`
  ->  `startFen = 4abtba3/2mubmbu4/12/3*4*3/4**6/12/12/3XCLSD4 w - - 0 1`
  (the v5 minion-6 reduction: drop the i7 and j7 Minions)
- `nMoveRule = 0`              ->  `nMoveRule = 50`
- `nFoldRule = 0`              ->  `nFoldRule = 3`
  (the v6 stall fix: 50 full moves without a capture is a draw,
  3-fold repetition is a draw)

Existing `rulesets/v6-d8-stall-rule.json` continues to specify these
overrides explicitly so the v6 sweep is reproducible without depending
on the .ini change; from v7 forward, ruleset JSONs that don't override
these keys will inherit the new defaults from the canonical .ini.

`rulesets/baseline.json` was historically a "no overrides" file
pointing at the canonical .ini; it now produces the v1 ship setup
unmodified.  Smoke verified.

## Recommendation for v7

The v1 canonical now ships at 44.9% P%(dec).  The slight Horde tilt
is well within the [40, 60] window, so v7's job is **not** to move
balance further; it's to **start SPSA tuning piece values** on this
configuration.  Section 7 of the spec.

If/when SPSA destabilizes balance:

1. **If Horde dominance creeps back above 60% (Party% drops below
   40%):**  The strongest Party buff in the catalog is Lurker `N` ->
   `W`.  It overshoots at full strength; partial reductions (e.g.
   `mNcW` or `fmWfceF`) are the obvious knobs.  Probe a v7 sweep with
   2-3 partial-Lurker variants stacked on the canonical ship.
2. **If Party dominance creeps above 60%:**  Restore Horde mass at
   i7 first (cheapest re-buff per the placement data), j7 second.
   Adding the j7 Minion alone moves P%(dec) by ~24pp (from 44.5%
   minion-6 to 20.9% j7-only); adding i7 moves it ~18pp.
3. **If decisiveness collapses again (Unfin starts climbing):** The
   stall rule is the stall rule; it can't be tightened much further
   without distorting strategy (50 -> 30 might be tolerable; below
   30 starts truncating real maneuvering).  The right escalation is
   re-examining whether Lurker mobility or Striker range produce
   the captures that resolve the stalled middlegames - both showed
   in v5/v6 that they affect Median plies as well as outcome.

Probes that should NOT happen in v7:

- Striker range on the canonical setup (no marginal balance effect
  on top of minion-6, ruled out by v6 probe 1).
- Tighter ply caps (moves Unfin in the wrong direction, ruled out
  by v6 probe 2).
- Adding Minions at j7 specifically (worst placement, ruled out by
  v6 probes 3 & 4).

## Methodology / infrastructure notes

- Zero engine crashes across all 1800 games (~95 cpu-min of FSF
  time).  Per-game-FSF-restart from v5 + IOException resilience
  in `MatchCommand.cs` continues to be sufficient.
- Total wall time 142 min at parallel 4, vs HOW-TO-READ's 135-150
  estimate.  No surprises.
- One artifact worth noting in the PGN: a handful of "Treasure
  checkmated or captured in 1 ply" outcomes appear early in
  `striker-range` and `placement-j7-only` (1-3 per 300 games).
  These are the same opening-FEN-meets-mK3cF / clustered-Minion
  pathology seen in v5; they're real game outcomes, not engine
  bugs, and they drag P%(dec) up by perhaps 0.5pp at most.  Not a
  signal worth correcting for.
- Sweep produced six clean PGN files; full match logs preserved.
  See file inventory below.

## File inventory in this directory

- `HOW-TO-READ.md` - original plan and hypotheses for v6 (not edited
  post-sweep).
- `FINDINGS.md` - this file.
- `match-v6-d8-minion-6-striker-range.{log,pgn}` - 300/300 complete.
- `match-v6-d8-minion-6-shortcap.{log,pgn}` - 300/300 complete.
- `match-v6-d8-minion-placement-a7-only.{log,pgn}` - 300/300 complete.
- `match-v6-d8-minion-placement-j7-only.{log,pgn}` - 300/300 complete.
- `match-v6-d8-minion-6-lurker-n-plus.{log,pgn}` - 300/300 complete.
- `match-v6-d8-stall-rule.{log,pgn}` - 300/300 complete.  **This is
  the v1 ship rule set.**
- `sweep.log` - driver log for the full v6 sweep, 142 min.
- `sweep-summary.{md,csv}` - ranked table across all 6 rule sets.
