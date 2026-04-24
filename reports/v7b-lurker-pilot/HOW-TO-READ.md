# How to read the v7b Lurker-mobility pilot

**Configuration:** 3 rule sets, **100 games each**, **depth = 8**,
**max-plies = 600**, **parallel = 4**, **seed = 20260428**.
Stall-rule (`nMoveRule`, `nFoldRule`) explicitly disabled on all
three so metrics reflect the real variant as humans would play it,
not the automation crutch.

## What v7b exists to answer

v6 showed Lurker mobility is a **28pp-wide balance lever**:

| probe | Lurker motion | P%(dec) | Unfin | Median plies |
|---|---|---:|---:|---:|
| v6 baseline (minion-6) | `N` (Knight) | 44.5% | 42.3% | 260 |
| v6 lurker-n-plus       | `W` (Wazir)  | 73.0% | 23.3% | 215 |

Going `N` → `W` **overshot** by ~20pp. We want the notch that lands
P%(dec) in [45, 55], and we want it to also bring Unfin well
below 20% without any stall rule.

v7b is a 3-point interpolation between those endpoints.

## The 3 probes

| probe | Lurker motion | targets | power vs N | my prior P%(dec) |
|---|---|---:|:---:|---:|
| `v7b-d8-lurker-WfN` | `u:WfN` (Wazir + forward-Knight) | 4+4=8 | slightly weaker | 52-58% |
| `v7b-d8-lurker-K`   | `u:K`   (Wazir + Ferz, one-step any) | 8 | moderately weaker | 58-66% |
| `v7b-d8-lurker-fN`  | `u:fN`  (forward-Knight only) | 4 | half power | 60-68% |

Rationale:
- `WfN` keeps Knight's forward leap (the dominant tactical motion)
  but replaces backward/sideways leaps with wazir steps. Weakest
  perturbation from the N baseline, most likely to land near 50%.
- `K` gives up all leaping for full one-step omnidirectionality.
  Very different geometry — no jumps, always blockable. Should
  dramatically shorten games (no more knight forks over dense
  starting formations).
- `fN` removes half of the Knight's target set entirely, keeping
  only the 4 forward L-leaps. Weakest probe; mainly insurance that
  we bracket P%(dec) = 50% from above.

## Ship criteria (revised per the human-playability standard)

Stall rules are not part of the shipped design; they existed only
to make automated self-play terminate. The target variant must
reach decisive outcomes naturally:

| criterion | threshold | rationale |
|---|---|---|
| P%(dec)       | in [45, 55] | chess-tight balance target |
| Imbalance     | <= 20%      | (= \|2*P% - 1\|; follows from above) |
| Unfinished    | <= 10%      | natural terminations, no stall rule crutch |
| Median plies  | <= 150      | chess-length (75 moves/side or less) |

This is stricter than the v6 ship criterion on two axes (Unfin and
Median). If any probe meets all four, it goes to a 300-game
confirmation; if confirmed, it becomes v1.1 and the stall-rule
overrides are dropped from `variants/yeshybrid.ini`.

## Expected outcomes

- **Balance:** at least one of the three should land P%(dec) in
  [45, 55] given the v6-measured 28pp axis width. If none does,
  we bracket further (add a probe between whichever two span 50%).
- **Length:** Unfin should drop meaningfully from baseline's 42.3%.
  `lurker-W` already showed -19pp (to 23.3%); these "weaker than
  N but stronger than W" probes should land in 25-40% Unfin
  territory. **None of the three is guaranteed to hit Unfin <= 10%
  alone.** If balance lands but decisiveness doesn't, we stack with
  a Horde Lurker count reduction (4/side -> 2/side) in v7c. That
  will be a Phase-2 decision, not pre-committed.

## What the output files mean

- `sweep-summary.md` / `.csv` - the three probes ranked by Composite.
- `match-<name>.log` / `.pgn` - per-rule-set match details.
- `FINDINGS.md` - written after the sweep; per-probe interpretation,
  ship/stack/bracket decision, handoff to v7b-confirm or v7c.

## Budget

300 games total (3 x 100) at depth 8 / max-plies 600 / parallel 4.
v5/v6 measured ~4-5s/game wall at depth 8 without stall rule
(games run longer without the forced-draw), so estimate ~25 min/
probe, ~75-90 min total pilot wall time.
