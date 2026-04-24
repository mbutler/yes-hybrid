# v2a 10x8 pilot findings

## Probe

- Ruleset: `rulesets/v2a-d8-10x8-same-pieces.json`
- Goal: test whether shrinking the v1.1 board from 12x8 to 10x8 reduces stalls while preserving the current balanced piece roster.
- Change: `maxFile = 10` plus a laterally compressed starting FEN.
- Held constant: v1.1 pieces, no bloodied rule, no stall rules, depth 8, 600-ply cap.

## Result

50-game pilot:

```text
Party wins  :    3  (  6.0%)
Horde wins  :   27  ( 54.0%)
Unfinished  :   20  ( 40.0%)
P%(dec)     : 10.0%   95% CI [3.5, 25.6]
Imbalance   : 80.0%
Median plies: 290
Composite   : 0.120
```

## Read

This specific 10x8 compression is not promising. It badly overcorrects toward Horde while leaving the unfinished rate high enough that the core stall problem remains.

The likely mechanism is that trimming lateral space improved Horde contact/containment more than Party conversion. Party gets fewer escape and maneuvering lanes, while Brutes and Lurkers still have enough coverage to grind down the four-piece Party.

## Recommendation

Do not run a 300-game confirm for this exact v2a. If the 10-file hypothesis is still worth exploring, the next bracket should compensate structurally for the Party rather than changing piece values: open the center, move obstacles away from Party lanes, reduce one Horde frontline body, or place Party one tempo/file closer to Treasure.
