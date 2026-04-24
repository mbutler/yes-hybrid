# How to read the v7c compression pilot

**Configuration:** 3 rule sets, **100 games each**, **depth = 8**,
**max-plies = 600**, **parallel = 4**, **seed = 20260429**.
All probes carry `u:K` (v7b balance winner) and disable stall
rules. The one-and-only variable is the starting-formation
distance between Horde and Party.

## What v7c exists to answer

v7b solved balance: `u:K` gives P%(dec) 52.1% and Imbalance 4.2%
without any stall rule. What it didn't solve was decisiveness:
52% of games hit the 600-ply cap without resolution.

Diagnosis: weakening Lurker removed enough tactical pressure that
games can't close naturally through the large empty middle of the
board. If pieces start closer to each other, fewer plies are
"wasted" on traversal and contact happens earlier, which should
let decisive outcomes emerge before the board locks into a
shuffle equilibrium.

## The 3 probes

All three inherit `lurker-K` (balance-preserving) and disable
stall rules. They differ only in starting FEN distance between
the armies. Baseline (v7b lurker-K) has Horde front on rank 7 and
Party on rank 1, distance = 6 ranks.

| probe | Horde front rank | Party rank | distance | descriptive |
|---|:---:|:---:|:---:|---|
| baseline (v7b) | 7 | 1 | 6 | measured: 52.1% P%, 52% Unfin |
| `v7c-d8-lurker-K-compress-5` | 6 | 1 | **5** | mild: shift Horde down 1 |
| `v7c-d8-lurker-K-compress-4` | 6 | 2 | **4** | moderate: shift Horde down 1 and Party up 1 |
| `v7c-d8-lurker-K-compress-3` | 7 | 3 | **3** | aggressive: shift Party up 2 (Horde unchanged) |

Obstacle squares (pillars on d5/i5, walls on e4/f4) are unchanged
in all three. `compress-3` puts Party pieces on rank 3, one step
away from the obstacle wall on rank 4 — so Party opens the game
with tactical contact against obstacles rather than open space.

## Ship criteria (from v7b, unchanged)

| criterion | threshold |
|---|---|
| P%(dec) | in [45, 55] |
| Imbalance | <= 20% |
| Unfinished | <= 10% |
| Median plies | <= 150 |

Any probe that meets all four graduates to a 300-game confirm. If
confirmed, that FEN becomes v1.1 and `variants/yeshybrid.ini` is
updated with the compressed startFen + `u:K` Lurker + stall rules
removed.

## Expected outcomes (priors)

| probe | prior P%(dec) | prior Unfin | notes |
|---|---:|---:|---|
| compress-5 | ~50% (symmetric shift) | ~35-45% | Mild; Horde and Party still separated by 5 ranks. Likely small improvement on decisiveness. |
| compress-4 | ~50% (both sides move equal) | ~25-35% | Moderate; the most likely to land in the ship window because it's symmetric and distance drops by 33%. |
| compress-3 | ~48-55% (Party gains tempo by moving up 2) | ~20-30% | Aggressive; real tactical unknown — Party's back rank is now Row 3 which overlaps Party's old "development zone". Strong prior this drops Unfin below 25%. |

The key thing I am watching is whether moving pieces closer breaks
balance. Distance symmetry matters: `compress-4` is a symmetric
shift (both sides step 1 toward center); `compress-3` is
asymmetric (only Party moves). The Party-only move gives Party one
free tempo, which might push P%(dec) toward 55-60%. If that
happens, we'd mix `compress-3` with a symmetric correction in v7d.

## Budget

3 probes x 100 games at depth 8, without stall rule. v7b measured
~10 min / 100-game match. Expect ~30-35 min total wall for the
sweep.

## Files this pilot will produce

- `sweep-summary.md` / `.csv` - ranked table
- `match-<name>.log` / `.pgn` - per-probe details
- `FINDINGS.md` - written after the sweep; decision on 300-game
  confirmation or v7d stacking
