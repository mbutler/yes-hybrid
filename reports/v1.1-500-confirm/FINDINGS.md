# v1.1 500-game confirm

## Probe

- Ruleset: `rulesets/v1.1-baseline.json`
- Variant: current canonical `variants/yeshybrid.ini`
- Board: original 12x8
- Games: 500
- Depth: 8
- Max plies: 600
- Opening book: 500 distinct 6-ply random openings
- Seed: 20260424
- Parallel: 4

## Result

```text
Party wins  :  216  (43.2%)
Horde wins  :  233  (46.6%)
Unfinished  :   51  (10.2%)
Decisive    :  89.8%
P%(dec)     :  48.1%   95% CI [43.5, 52.7]
Imbalance   :   3.8%
Median plies: 199
Composite   : 0.864
```

## Read

This is a strong v1.1 result. The original 12x8 board, as represented by the current canonical INI, appears both balanced and acceptably decisive under a larger sample.

The unfinished rate is about 10%, not the 40-50% concern from earlier v7 notes. At 500 games, the rough uncertainty on the unfinished estimate is small enough to treat the stall rate as materially below the danger zone.

Balance also holds: Party took 48.1% of decisive games, and the 95% confidence interval includes 50% comfortably.

## Recommendation

Do not pursue v2 structural changes solely to fix stalls unless human play reveals a real experiential problem. The current v1.1 board is good enough to preserve as the reference game.

The discrepancy with the earlier 300-game v7f report was investigated in `reports/v1.1-stall-rule-ab/`: explicit `nMoveRule = 0` / `nFoldRule = 0` disables stall rules and reproduces the high-Unfin behavior. v1.1 keeps chess-style stall rules as canonical gameplay rules, so this 500-game result is the final ship metric.
