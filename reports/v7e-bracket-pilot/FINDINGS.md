# v7e balance-bracket pilot — findings

## Headline

**Both bracket probes missed the target window and regressed on
decisiveness.** Adding a Horde Minion on top of the v7d remove-artillery
base corrected balance by only 3-8pp (not the 13-24pp v6 predicted)
and pushed Unfin from 30% back to 40-47%.

## Results

| rank | probe | P-W | H-W | Unfin | P%(dec) | Imbal | Median | Composite |
|:---:|---|---:|---:|---:|---:|---:|---:|---:|
| v7d base | v7d-d8-lurker-K-remove-artillery | 49 | 21 | **30** | 70.0% | 40.0% | 242 | 0.420 |
| 1 | v7e-d8-remove-a-add-m-a7 | 33 | 20 | 47 | 62.3% | 24.5% | 189 | 0.400 |
| 2 | v7e-d8-remove-a-add-m-j7 | 40 | 20 | 40 | 66.7% | 33.3% | 249 | 0.400 |

## Priors vs actuals

| probe | prior P% | actual | prior Unfin | actual |
|---|---:|---:|---:|---:|
| a7-minion | ~57% | 62.3% | 25-35% | 47% |
| j7-minion | ~46% | 66.7% | 25-35% | 40% |

The j7 prior was off by 21pp. My v6-based linear extrapolation —
"+1 Minion at j7 subtracts 24pp Party" — is wrong on top of the
remove-artillery base. That extrapolation assumed the Horde's
attacking environment was fixed; in fact, with 1 fewer Artillery
supporting the Minions, each additional Minion is worth much less
as an attacker.

## What we actually learned — the real mechanism

Comparing all 3 data points against baseline (v7b lurker-K, 11 Horde
pieces):

| configuration | Horde pieces | Unfin | P%(dec) |
|---|---:|---:|---:|
| v7b lurker-K (baseline) | 11 | 52% | 52.1% |
| v7d remove-artillery | 10 | 30% | 70.0% |
| v7e remove-a + m-a7 | 11 | 47% | 62.3% |
| v7e remove-a + m-j7 | 11 | 40% | 66.7% |

**Unfin correlates strongly with total Horde material count, not
with balance.** 10 pieces -> 30% stalls. 11 pieces -> 40-52% stalls.

This is a **major mechanistic finding**. The stall isn't caused by
any specific piece type or position — it is caused by there being
enough surviving post-trade material that neither side can force a
conclusion. The v7d artillery removal worked because it cut total
material; adding *any* piece back re-enables the stall equilibrium.

**Implication for balance correction:** we cannot use "add Horde
piece" as a balance-correction lever because every Horde add undoes
the decisiveness gain. The correct approach is to reduce *Party*
material — this will:
1. Shift balance Horde-ward (Party loses material advantage)
2. Further lower total board material (Unfin preserved or improved)
3. Not re-enable the stall geometry

This is a **structural win** — the direction of travel is "keep
cutting material until both goals land." We have been trying to
solve balance and decisiveness with orthogonal levers. The new
hypothesis is that they are the same lever applied to different
sides.

## v7f plan: cut Party material

Party has 5 pieces: D (Defender, WcK), C (Controller, gQ), L (Leader,
WfF), S (Striker, mK2cF), X (Skirmisher, KAD). Approximate value
rank (from SPSA-derived priors): C > X > S > D > L.

Bracket the balance correction by removing two different Party
pieces and seeing which one lands in [45, 55]:

| probe | change vs v7d | piece removed | prior P% | prior Unfin |
|---|---|---|---:|---:|
| `v7f-remove-leader` | Also remove L from d1 (weakest Party piece) | Leader | 58-65% | 22-30% |
| `v7f-remove-skirmisher` | Also remove X from d1 position (stronger Party piece) | Skirmisher | 45-55% | 22-30% |

Starting FEN construction:
- Base (v7d remove-artillery): `3XCLSD4` on rank 1
- Remove Leader: `3XC1SD4` (X, C, ., S, D)
- Remove Skirmisher: `4CLSD4` (., C, L, S, D)

Both probes preserve: `u:K` Lurker, stall rules off, Artillery
already removed from i8. 100 games × depth 8 × 2 probes = ~22 min.

### Expected outcomes

If the "total material" hypothesis is right, BOTH probes should
preserve Unfin near 30% (maybe even lower, 20-25%). One of them
should land in [45, 55] P%(dec). If both miss on balance,
remove-leader AND remove-skirmisher at once may overshoot, but the
bracketing gives us a linear estimate for v7g.

If *either* probe hits [45, 55] with Unfin <= 30%, that configuration
becomes our first ship candidate under natural rules and we run a
300-game confirmation match.

### Fallback

If Unfin DOESN'T stay low (material hypothesis is wrong), we
pivot again — likely to board geometry (smaller board, different
obstacles). Much bigger change, will pause for user sign-off.

## Commit point

Launching v7f now. This is the most principled move I can make
from the data. The mechanism hypothesis ("Unfin tracks total
material") is strongly supported by three data points (52/30/40-47%
Unfin at 11/10/11 pieces), though still only three points.
