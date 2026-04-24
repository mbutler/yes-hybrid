# Sweep summary  

- Rule sets evaluated: 3
- Elapsed: 31.5 min
- Status: complete

| Rank | Rule set | Games | P-W | H-W | Unfin | Decisive | P%(dec) | Imbal | **Composite** | Median plies |
|-----:|:---------|------:|----:|----:|------:|---------:|--------:|------:|--------------:|-------------:|
| 1 | `v7b-d8-lurker-K` | 100 | 25 | 23 | 52 | 48.0% | 52.1% | 4.2% | **0.460** | 194 |
| 2 | `v7b-d8-lurker-fN` | 100 | 59 | 16 | 25 | 75.0% | 78.7% | 57.3% | **0.320** | 227 |
| 3 | `v7b-d8-lurker-WfN` | 100 | 8 | 43 | 49 | 51.0% | 15.7% | 68.6% | **0.160** | 220 |

### Interpretation key
- **Composite** = (1 - Imbalance) * DecisiveRate. Range 0-1. Higher = more balanced AND more decisive.
- A rule set can score well by being decisive in a lopsided way (imbalance high, but if decisive=1.0 and imbalance=1.0, composite=0).
- **Party%(dec)** ~= 50% is the balance goal.  75%+ means Party dominates; <25% means Horde dominates.
- High **Unfin** = game stalls (design failure; no tempo pressure).
