# Sweep summary  

- Rule sets evaluated: 6
- Elapsed: 142.2 min
- Status: complete

| Rank | Rule set | Games | P-W | H-W | Unfin | Decisive | P%(dec) | Imbal | **Composite** | Median plies |
|-----:|:---------|------:|----:|----:|------:|---------:|--------:|------:|--------------:|-------------:|
| 1 | `v6-d8-stall-rule` | 300 | 119 | 146 | 35 | 88.3% | 44.9% | 10.2% | **0.793** | 186 |
| 2 | `v6-d8-minion-6-lurker-n-plus` | 300 | 168 | 62 | 70 | 76.7% | 73.0% | 46.1% | **0.413** | 215 |
| 3 | `v6-d8-minion-6-striker-range` | 300 | 60 | 88 | 152 | 49.3% | 40.5% | 18.9% | **0.400** | 269 |
| 4 | `v6-d8-minion-6-shortcap` | 300 | 59 | 72 | 169 | 43.7% | 45.0% | 9.9% | **0.393** | 210 |
| 5 | `v6-d8-minion-placement-a7-only` | 300 | 46 | 102 | 152 | 49.3% | 31.1% | 37.8% | **0.307** | 260 |
| 6 | `v6-d8-minion-placement-j7-only` | 300 | 34 | 129 | 137 | 54.3% | 20.9% | 58.3% | **0.227** | 314 |

### Interpretation key
- **Composite** = (1 - Imbalance) * DecisiveRate. Range 0-1. Higher = more balanced AND more decisive.
- A rule set can score well by being decisive in a lopsided way (imbalance high, but if decisive=1.0 and imbalance=1.0, composite=0).
- **Party%(dec)** ~= 50% is the balance goal.  75%+ means Party dominates; <25% means Horde dominates.
- High **Unfin** = game stalls (design failure; no tempo pressure).
