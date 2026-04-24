# v7i partial-brute-capture pilot — findings

## Headline

**Piece-level interpolation between "full Brute captures" and "no Brute
captures" does not work.** Both probes overshot balance dramatically
without delivering the Unfin drop that removing captures entirely
achieves.

## Results

| probe | Brute capture atom | P%(dec) | Unfin | Imbal | Median |
|---|---|---:|---:|---:|---:|
| v7f-no-leader (baseline, 300-game confirm) | `cK` (8 dirs) | 49.7% | 47.7% | 0.6% | 217 |
| v7i-d8-brute-mDcF | `cF` (4 dirs diagonal) | 81.0% | 42% | 62.1% | 222 |
| v7i-d8-brute-mDcW | `cW` (4 dirs orthogonal) | 91.0% | 22% | 82.1% | 307 |
| v7g (bug, for reference) | none | 98.9% | 7% | 97.8% | 109 |

## What this establishes

The response is **highly non-linear in capture coverage**:

- Full `cK` (8 dirs) -> 50% P%, 48% Unfin
- Half `cF` or `cW` (4 dirs) -> 81-91% P%, 22-42% Unfin
- Zero captures -> 99% P%, 7% Unfin

A 50% reduction in Brute's capture directions produces a
30-40pp balance swing toward Party but only a 5-25pp Unfin drop.
Every point of reduced Horde attack power costs 3-6x more on balance
than it gives back on decisiveness. There is no interpolation point
where balance and decisiveness both pass their ship thresholds.

Interesting split: `mDcW` (orthogonal captures only) gave better Unfin
than `mDcF` (diagonal captures only), suggesting diagonal captures are
more valuable for stall equilibria (probably because diagonal threats
force kings to move more than orthogonal ones do).

## Why this closes the piece-tuning question

After 8 v7 campaigns we have now mapped:
- Piece values (SPSA): no reliable signal
- Lurker mobility: balance lever (solved at `u:K`)
- FEN compression: balance lever, not decisiveness lever
- Horde count reduction: decisiveness lever that breaks balance
- Party count reduction: balance correction lever
- Brute mobility: neutral
- Brute capture coverage: balance lever that breaks before Unfin drops

**Every remaining piece-level lever is structurally correlated with
what we've tested.** The only untested pieces (Defender, Controller,
Striker details) are either known load-bearing (Skirmisher's
catastrophic removal) or architecturally similar to levers already
tested.

## Conclusion: ship v7f-no-leader as v1.1

The peak of the piece-level landscape is v7f-no-leader:

| metric | value | ship target |
|---|---|---|
| P%(dec) | 49.7% | [45, 55] ✅ |
| Imbalance | 0.6% | <= 20% ✅ (by 33x) |
| Unfinished | 47.7% | <= 10% stretch ❌; ~25% practical ❌ |
| Median plies (decisive) | 217 | <= 150 stretch ❌; <= 250 practical ✅ |

Balance is the best we've ever measured. Unfin is a known structural
limitation to be addressed in v2.

See `reports/v7-SUMMARY.md` for the full campaign narrative.
