# How to read the v7d endgame-lever pilot

**Configuration:** 2 rule sets, 100 games each, depth 8, max-plies 600,
parallel 4, seed 20260430. Both share `u:K` Lurker (balance winner from
v7b) and disable stall rules.

## Why v7d exists

v7c proved the stall is an **endgame** phenomenon, not an opening-tempo
phenomenon. Games that resolve do so by ply ~200; games that stall
spend 400+ plies shuffling surviving material that cannot force
conclusion. Moving armies closer (compression) didn't help.

v7d tests two orthogonal mechanisms for making the post-trade endgame
converge:

1. **Less material to grind through** (remove a Horde heavy piece)
2. **Stronger finishing piece** (upgrade Party Leader to full king
   mobility so it can actually corner the Treasure)

## The two probes

| probe | change vs v7b lurker-K | mechanism |
|---|---|---|
| `v7d-d8-lurker-K-remove-artillery` | Remove Artillery at i8 from starting FEN | Reduces Horde material by ~9% (1 of 11); removes Horde's longest-range non-royal attacker |
| `v7d-d8-lurker-K-leader-K` | Leader `WfF` -> `K` (customPiece4 override) | Party's Leader becomes a full king-analog piece; now has the mobility to chase and constrict the Treasure |

Baseline remains v7b-d8-lurker-K: P%(dec) 52.1%, Imbalance 4.2%, Unfin
52.0%, Median 194.

## Priors (stated, testable)

| probe | P%(dec) prior | Unfin prior | Median prior |
|---|:---:|:---:|:---:|
| remove-artillery | 60-67% | 25-35% | 160-190 |
| leader-K | 55-62% | 35-45% | 180-200 |

The key threshold is **Unfin < 40%**. If neither probe clears it,
piece-level changes are not sufficient to fix decisiveness on a 12x8
board and the next step is board geometry (obstacle layout, file
count).

## Decision rules

### If *both* probes show Unfin drop AND stay within [40, 70] P%(dec)

Whichever has lower Unfin becomes the v7e base. v7e runs **2 counter-
balance probes** that pull P%(dec) back toward 50 while preserving the
Unfin gain. Candidates: Striker range back to 1 (Horde-favoring), or
Minion count +1 (Horde-favoring).

### If *only one* probe shows Unfin drop

That one becomes the v7e base regardless of P%(dec) overshoot; v7e
handles balance correction.

### If *neither* shows Unfin drop (Unfin >= 40%)

Piece-level changes are a dead lever for decisiveness. Pivot to v7e
geometry: remove obstacle squares, or shrink board to 10 files in a
new variant definition. Much bigger changes; will need user sign-off.

## Budget

~22 min wall for the pilot.
