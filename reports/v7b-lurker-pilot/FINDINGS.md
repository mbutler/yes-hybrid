# v7b Lurker-mobility pilot — findings

## Headline

**Balance is solved.** `v7b-d8-lurker-K` (Lurker = one-step any
direction, i.e. Wazir + Ferz) hit **P%(dec) = 52.1%, Imbalance =
4.2%** — the best-balanced configuration we have ever measured,
achieved without any stall-rule crutch.

**Decisiveness is the remaining problem.** The same probe's
**Unfin = 52%** is worse than the v6 minion-6 baseline (42.3%).
Weakening Lurker tactical power moved balance into the target
window but removed enough forcing pressure that half the games
now fail to resolve within the 600-ply harness cap.

v7b is not a ship, but it is the most *informative* sweep we have
run. We now have a **known-balanced configuration** that we can
stack a decisiveness lever on top of in v7c, rather than trying to
solve both axes at once.

## Results

| Rank | Rule set | Lurker | P%(dec) | Imbal | Unfin | Median plies | Composite |
|---:|---|---|---:|---:|---:|---:|---:|
| 1 | `v7b-d8-lurker-K` | `K`    | **52.1%** | **4.2%**  | 52.0% | 194 | **0.460** |
| 2 | `v7b-d8-lurker-fN` | `fN`  | 78.7% | 57.3% | 25.0% | 227 | 0.320 |
| 3 | `v7b-d8-lurker-WfN`| `WfN` | 15.7% | 68.6% | 49.0% | 220 | 0.160 |

For reference, the known endpoints from v5/v6:

| baseline | Lurker | P%(dec) | Imbal | Unfin | Median plies |
|---|---|---:|---:|---:|---:|
| v5 minion-6 (no stall rule) | `N`    | 44.5% | 11.0% | 42.3% | 260 |
| v6 lurker-n-plus (no stall) | `W`    | 73.0% | 46.1% | 23.3% | 215 |

## Per-probe interpretation

### `lurker-K` — the balance winner

100-game 95% CI on P%(dec) = 52.1%: roughly [42, 62] at this sample
size. The point estimate is 2.1pp above 50%; CI comfortably
includes 50%. Imbalance 4.2% is an order of magnitude better than
any prior result.

Median decisive game: 194 plies (~97 moves/side). Still longer
than chess targets but a 66-ply improvement over v5 baseline's
260. Half of decisive games finish by ply 194.

Termination breakdown (100 games):
- Treasure captured (Party): 25
- Party extinct (Horde): 20
- Party has no legal response (Horde): 3
- **Ply cap 600 (stalled): 47**

47 out of 100 games failed to resolve. That's the target of v7c.
The balance we have is real and should be preserved.

### `lurker-fN` — overshoots, forward leap too strong

Prior was 60-68%; actual is 78.7%. A forward-only Knight turns out
to be more Party-favoring than a full Knight because Horde loses
its defensive backward/sideways tactical coverage faster than it
loses attacking reach (attacking is naturally forward-biased for
Horde, which moves toward Party's rank 1).

Unfin = 25% is the best of the three on decisiveness, but at
Imbalance 57.3% the game is now badly broken in the other
direction. Not a candidate.

### `lurker-WfN` — surprise overshoot to Horde

Prior was 52-58%; actual is 15.7%. Horde dominates. Wazir +
forward-Knight turns out to be substantially *stronger* than full
Knight — the combination of 4 close orthogonal moves and 4 forward
leaps gives Horde both defensive solidity (Wazir blocks diagonals)
and attacking bite (forward-Knight jumps Minion lines). The
overshoot is 57pp in the opposite direction of intent. Not a
candidate.

## What the combined evidence now says

We have 5 measured points on the Lurker-mobility axis, graphed
roughly by my informal "Horde tactical power" estimate:

| Lurker | targets | approx power | P%(dec) | Unfin |
|---|---:|:---:|---:|---:|
| `W`   (Wazir)                         | 4  | 1.0 | 73.0% | 23.3% |
| `K`   (Wazir + Ferz)                  | 8  | 1.8 | **52.1%** | 52.0% |
| `fN`  (forward-Knight)                | 4  | 2.0 | 78.7% | 25.0% |
| `N`   (Knight, baseline)              | 8  | 3.0 | 44.5% | 42.3% |
| `WfN` (Wazir + forward-Knight)        | 8  | 3.5 | 15.7% | 49.0% |

Two non-monotonicities jump out:
1. `fN` (power 2.0) scores 78.7% P%, higher than `W` (power 1.0,
   73%). Forward leap is worth more to Horde than broad close
   coverage — but removing it entirely (going `W` → `fN` removes
   close and keeps leap) actually hurts Horde *less* than I
   expected.
2. `WfN` is a tactically dominant motion that Horde exploits fully;
   much more than a naive "Wazir plus forward-Knight" sum of parts
   would suggest.

These are useful priors for future piece design, but they don't
change the v7c plan: **`K` is the pick**.

## Decision: proceed to v7c

v7c should preserve `lurker-K` (balance is solved, don't risk it)
and stack one *orthogonal* lever targeting decisiveness:

### Candidate levers, with priors

1. **FEN compression** - remove one empty middle rank, moving
   Horde rank 7 into rank 6 (or Party rank 1 into rank 2). Pieces
   meet ~10 plies faster from the opening. No balance risk if
   applied symmetrically to both sides; pure tempo pressure.
   Prior: Unfin drops 15-25pp, balance holds.

2. **Horde piece-count reduction** - remove one of the 2 Lurkers
   (or one Brute) from rank 7/8. Fewer attackers means fewer
   maneuvering pieces but also weaker Horde offense. Balance
   risk: Party-favoring. Might overshoot back into 60%+ P%.

3. **Party piece buff** - extend Striker range (v5 `mK2cF` ->
   `mK3cF`) to give Party more decisive closing power. Balance
   risk: measured ~8pp Party-favoring at v5 depth 8, which would
   push from 52% to 60%. Likely overshoots.

4. **Reduce maxPlies to 400 in the variant** - *we do not do
   this.* maxPlies is a harness cutoff not a game rule; reducing
   it is identical to asserting "games over 400 plies just count
   as unfinished" which doesn't make them shorter, it just
   relabels the failure.

### Recommended v7c

Three probes at 100 games each, all on top of `lurker-K`:

| v7c-a | FEN compressed by removing rank 4 (the "4*3" rank).   | balance-preserving tempo probe |
| v7c-b | Minion count 2 -> 3 per flank, maintain `lurker-K`.   | structural density probe |
| v7c-c | FEN compressed by removing rank 2 (the "3*4*3" rank). | alternative compression test |

v6 and v5 data tell us the direct piece-count ladder stops working
below 2 minions per flank. FEN compression is an axis we have not
previously tested — it's the biggest unknown and most likely to
produce a dramatic Unfin drop.

## Summary of progress

| metric | v5 minion-6 | v6 stall-rule ship | **v7b lurker-K** | v7c target |
|---|---:|---:|---:|---:|
| P%(dec)     | 44.5% | 44.9% | **52.1%** | [45, 55] |
| Imbalance   | 11.0% | 10.2% | **4.2%**  | <= 20%   |
| Unfin       | 42.3% | 11.7% (w/ stall) | 52.0% | <= 10% (no stall) |
| Median plies | 260  | 186 | 194 | <= 150 |
| Stall-rule needed? | no | **yes** | **no** | **no** |

Balance has materially improved for the first time since v5. The
ship criterion we are missing is decisiveness under natural rules,
which is the single focused question v7c will answer.

## Pause point

Awaiting user sign-off on the proposed v7c probes (FEN
compression + Minion-density stacked on `lurker-K`).
