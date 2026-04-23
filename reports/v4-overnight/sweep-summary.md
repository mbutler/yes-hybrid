# Sweep summary  

- Rule sets evaluated: 7
- Elapsed: 68.7 min
- Status: complete

| Rank | Rule set | Games | P-W | H-W | Unfin | Decisive | P%(dec) | Imbal | **Composite** | Median plies |
|-----:|:---------|------:|----:|----:|------:|---------:|--------:|------:|--------------:|-------------:|
| 1 | `v4-mf-ply-cap-600` | 150 | 59 | 32 | 59 | 60.7% | 64.8% | 29.7% | **0.427** | 222 |
| 2 | `v4-mf-minion-9` | 150 | 32 | 41 | 77 | 48.7% | 43.8% | 12.3% | **0.427** | 190 |
| 3 | `v4-mf-baseline` | 150 | 46 | 31 | 73 | 51.3% | 59.7% | 19.5% | **0.413** | 200 |
| 4 | `v4-mf-striker-range` | 150 | 59 | 28 | 63 | 58.0% | 67.8% | 35.6% | **0.373** | 167 |
| 5 | `v4-mf-minion-7` | 150 | 57 | 27 | 66 | 56.0% | 67.9% | 35.7% | **0.360** | 186 |
| 6 | `v4-mf-artillery-range` | 150 | 46 | 24 | 80 | 46.7% | 65.7% | 31.4% | **0.320** | 160 |
| 7 | `v4-mf-depth-8` | 150 | 14 | 39 | 97 | 35.3% | 26.4% | 47.2% | **0.187** | 226 |

### Interpretation key
- **Composite** = (1 - Imbalance) * DecisiveRate. Range 0-1. Higher = more balanced AND more decisive.
- A rule set can score well by being decisive in a lopsided way (imbalance high, but if decisive=1.0 and imbalance=1.0, composite=0).
- **Party%(dec)** ~= 50% is the balance goal.  75%+ means Party dominates; <25% means Horde dominates.
- High **Unfin** = game stalls (design failure; no tempo pressure).
