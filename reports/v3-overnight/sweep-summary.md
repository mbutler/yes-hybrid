# Sweep summary  

- Rule sets evaluated: 7
- Elapsed: 64.1 min
- Status: complete

| Rank | Rule set | Games | P-W | H-W | Unfin | Decisive | P%(dec) | Imbal | **Composite** | Median plies |
|-----:|:---------|------:|----:|----:|------:|---------:|--------:|------:|--------------:|-------------:|
| 1 | `v3-mc-minion-fc` | 150 | 50 | 42 | 58 | 61.3% | 54.3% | 8.7% | **0.560** | 187 |
| 2 | `v3-mc-baseline` | 150 | 40 | 30 | 80 | 46.7% | 57.1% | 14.3% | **0.400** | 213 |
| 3 | `v3-mc-bloodied` | 150 | 57 | 26 | 67 | 55.3% | 68.7% | 37.3% | **0.347** | 198 |
| 4 | `v3-mc-denser-horde` | 150 | 25 | 51 | 74 | 50.7% | 32.9% | 34.2% | **0.333** | 205 |
| 5 | `v3-mc-shallow-flag` | 150 | 17 | 39 | 94 | 37.3% | 30.4% | 39.3% | **0.227** | 90 |
| 6 | `v3-mc-compact` | 150 | 9 | 103 | 38 | 74.7% | 8.0% | 83.9% | **0.120** | 140 |
| 7 | `v3-mc-anyflag` | 150 | 0 | 30 | 120 | 20.0% | 0.0% | 100.0% | **0.000** | 216 |

### Interpretation key
- **Composite** = (1 - Imbalance) * DecisiveRate. Range 0-1. Higher = more balanced AND more decisive.
- A rule set can score well by being decisive in a lopsided way (imbalance high, but if decisive=1.0 and imbalance=1.0, composite=0).
- **Party%(dec)** ~= 50% is the balance goal.  75%+ means Party dominates; <25% means Horde dominates.
- High **Unfin** = game stalls (design failure; no tempo pressure).
