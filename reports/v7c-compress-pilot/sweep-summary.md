# Sweep summary  

- Rule sets evaluated: 3
- Elapsed: 31.4 min
- Status: complete

| Rank | Rule set | Games | P-W | H-W | Unfin | Decisive | P%(dec) | Imbal | **Composite** | Median plies |
|-----:|:---------|------:|----:|----:|------:|---------:|--------:|------:|--------------:|-------------:|
| 1 | `v7c-d8-lurker-K-compress-5` | 100 | 20 | 32 | 48 | 52.0% | 38.5% | 23.1% | **0.400** | 201 |
| 2 | `v7c-d8-lurker-K-compress-3` | 100 | 17 | 38 | 45 | 55.0% | 30.9% | 38.2% | **0.340** | 184 |
| 3 | `v7c-d8-lurker-K-compress-4` | 100 | 16 | 32 | 52 | 48.0% | 33.3% | 33.3% | **0.320** | 175 |

### Interpretation key
- **Composite** = (1 - Imbalance) * DecisiveRate. Range 0-1. Higher = more balanced AND more decisive.
- A rule set can score well by being decisive in a lopsided way (imbalance high, but if decisive=1.0 and imbalance=1.0, composite=0).
- **Party%(dec)** ~= 50% is the balance goal.  75%+ means Party dominates; <25% means Horde dominates.
- High **Unfin** = game stalls (design failure; no tempo pressure).
