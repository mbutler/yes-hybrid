# Sweep summary  

- Rule sets evaluated: **6 of 6 at full 300 games (complete clean sweep)**
- Elapsed across the combined run: ~155 min on old machine (baseline + first sweep attempts) + ~125 min on new machine (5 clean runs after the per-game-FSF patch)
- Status: **complete**.  All three crashed minion rule sets re-run to a clean 300/300 under the patched harness.  See `FINDINGS.md` for interpretation.

| Rank | Rule set | Games | P-W | H-W | Unfin | Decisive | P%(dec) | Imbal | **Composite** | Median plies |
|-----:|:---------|------:|----:|----:|------:|---------:|--------:|------:|--------------:|-------------:|
| 1 | `v5-d8-minion-6`      | 300 | 77 |  96 | 127 | 57.7% | **44.5%** | **11.0%** | **0.513** | 260 |
| 2 | `v5-d8-minion-7`      | 300 | 41 | 114 | 145 | 51.7% | 26.5% | 47.1% | **0.273** | 258 |
| 3 | `v5-d8-minion-9`      | 300 | 35 | 127 | 138 | 54.0% | 21.6% | 56.8% | **0.233** | 309 |
| 4 | `v5-d8-striker-range` | 300 | 32 | 118 | 150 | 50.0% | 21.3% | 57.3% | **0.213** | 248 |
| 5 | `v5-d8-lurker-range`  | 300 | 28 | 188 |  84 | 72.0% | 13.0% | 74.1% | **0.187** | 220 |
| 6 | `v5-d8-baseline`      | 300 | 18 | 122 | 160 | 46.7% | 12.9% | 74.3% | **0.120** | 294 |

### Interpretation key
- **Composite** = (1 - Imbalance) * DecisiveRate. Range 0-1. Higher = more balanced AND more decisive.
- A rule set can score well by being decisive in a lopsided way (imbalance high, but if decisive=1.0 and imbalance=1.0, composite=0).
- **Party%(dec)** ~= 50% is the balance goal.  75%+ means Party dominates; <25% means Horde dominates.
- High **Unfin** = game stalls (design failure; no tempo pressure).
