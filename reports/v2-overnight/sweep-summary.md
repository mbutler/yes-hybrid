# Sweep summary  

- Rule sets evaluated: 7
- Elapsed: 35.9 min
- Status: complete

| Rank | Rule set | Games | P-W | H-W | Unfin | Decisive | P%(dec) | Imbal | **Composite** | Median plies |
|-----:|:---------|------:|----:|----:|------:|---------:|--------:|------:|--------------:|-------------:|
| 1 | `minion-caps` | 100 | 23 | 17 | 60 | 40.0% | 57.5% | 15.0% | **0.340** | 181 |
| 2 | `bloodied` | 100 | 61 | 13 | 26 | 74.0% | 82.4% | 64.9% | **0.260** | 159 |
| 3 | `baseline` | 100 | 39 | 8 | 53 | 47.0% | 83.0% | 66.0% | **0.160** | 157 |
| 4 | `promo-only` | 100 | 39 | 8 | 53 | 47.0% | 83.0% | 66.0% | **0.160** | 157 |
| 5 | `flag-only` | 100 | 0 | 10 | 90 | 10.0% | 0.0% | 100.0% | **0.000** | 178 |
| 6 | `flag+promo` | 100 | 0 | 10 | 90 | 10.0% | 0.0% | 100.0% | **0.000** | 178 |
| 7 | `all-in` | 100 | 0 | 16 | 84 | 16.0% | 0.0% | 100.0% | **0.000** | 164 |

### Interpretation key
- **Composite** = (1 - Imbalance) * DecisiveRate. Range 0-1. Higher = more balanced AND more decisive.
- A rule set can score well by being decisive in a lopsided way (imbalance high, but if decisive=1.0 and imbalance=1.0, composite=0).
- **Party%(dec)** ~= 50% is the balance goal.  75%+ means Party dominates; <25% means Horde dominates.
- High **Unfin** = game stalls (design failure; no tempo pressure).
