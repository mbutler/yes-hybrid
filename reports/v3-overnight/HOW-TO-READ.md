# How to read the v3 overnight sweep

**Configuration:** 7 rule sets, 150 games each, depth=6, max-plies=400,
parallel=4, seed=20260423.  All 7 rule sets are derived from v2's winner,
`minion-caps` (Minion `W` -> `WcF`), which became the new working baseline
after the v2 sweep established it as the only balanced point in the v2
hypothesis space.  See `../v2-overnight/FINDINGS.md` for that history.

## The structural question we're testing

> Is v2's `minion-caps` already the balanced foundation we want, or is
> there a still-better point nearby?  And which of the three v2 open
> design options (A keep-Treasure / B flag-zone / C dual-objective) is
> viable in data?

v3 doesn't run option C (dual-objective) in the engine because FSF can
only terminate on one win condition at a time per side; C would require
CLI-side detection, which we defer until A vs B is resolved by data.

Each v3 rule set isolates one axis **on top of `minion-caps`**.  Read
the summary as a diff against `v3-mc-baseline`, which IS `minion-caps`
by another name and serves as the control.

| Rule set                | Change on top of minion-caps                  | Hypothesis being tested                                       |
|-------------------------|-----------------------------------------------|---------------------------------------------------------------|
| `v3-mc-baseline`        | *(= minion-caps; Treasure as royal)*          | Reference / reproducibility.                                  |
| `v3-mc-bloodied`        | + Bloodied (Party pieces take 2 hits)         | Does Bloodied tighten decisiveness now that the game is balanced? |
| `v3-mc-minion-fc`       | Minion `WcF` -> `WfcF` (forward-only cap)     | Is full-diagonal `cF` too strong?  Clawing back Horde over-buff. |
| `v3-mc-shallow-flag`    | Flag variant + flag zone = **rank 6**         | Does a shorter Leader-walk fix the "Horde walls off" v2 stall? |
| `v3-mc-anyflag`         | Flag variant + `flagPiece = l c s x d` @ g8   | Does widening *which* Party piece wins fix the unwinnable g8? |
| `v3-mc-compact`         | 10x7 board (tighter geometry, same rosters)   | Does removing sprawl force decisive play?                     |
| `v3-mc-denser-horde`    | Two extra Minions on rank 7 (b7, k7)          | Is the Horde under-armed *even after* the `cF` buff?          |

## What "good" looks like (same scoring as v2)

The **composite score** = `(1 - Imbalance) * DecisiveRate`, range 0..1.

- **Composite >= 0.50** = strong candidate.  Decisive AND balanced.
- **Composite 0.30 - 0.50** = directionally correct; needs magnitude
  tuning (piece values, SPSA) to push further.
- **Composite < 0.20** = failure for balance, for decisiveness, or both.
- **Party%(dec) ~= 50%** is the primary balance goal.  30-70% is
  tolerable.  Outside that band means we need structural changes, not
  magnitude tuning.
- **Unfinished > 30%** = structural warning sign: the game lacks tempo.

For reference, v2 winners:

- `minion-caps`   composite **0.340**, Party 57.5%, unfin 60%.  (New baseline.)
- `bloodied`      composite 0.260, Party 82.4%, unfin 26%.  Decisive but biased.
- all flag variants in v2: composite **0.000**, 0 Party wins.

## Likely outcome scenarios

1. **`v3-mc-baseline` matches v2 minion-caps (+- 5%)**.  Expected; it's
   the same rule set, just re-run at higher N.  If it doesn't match,
   something regressed in the harness.

2. **`v3-mc-bloodied` beats baseline** (lower unfin, ~50% Party).
   Bloodied was a decisiveness knob in v2; on a *balanced* base it may
   finally pull its weight without biasing.  If Party% jumps > 70%,
   Bloodied is still just a Party power-up and we discard it.

3. **`v3-mc-minion-fc` lands between baseline and the v2 baseline**
   (something like Party 65-75%, unfin 40-55%).  Mechanical prediction:
   `WfcF` removes backward diagonal captures, so Minions threaten Party
   less once Party crosses them.  If Party% > 80%, `cF` is not too
   strong - it's the right magnitude.

4. **`v3-mc-shallow-flag` produces > 0 Party wins**, unlike v2 flag-only.
   If Party 40-60% of decisive, the flag concept is viable but needs the
   right distance.  If still 0 Party wins, the Leader (`WfF`) is simply
   not mobile enough and we abandon Leader-as-flag-piece.

5. **`v3-mc-anyflag` produces high Party win %** (plausibly 80%+).
   Five flag pieces makes g8 reachable; the question is whether the
   Horde can stop them at all.  If too high, the next iteration widens
   flagRegion, narrows flagPiece, or adds a Horde "flag-guard" duty.

6. **`v3-mc-compact` is more decisive** (unfin < 30%) regardless of
   Party%.  Smaller board = pieces collide sooner.  If Party% flips
   toward Horde, that's the signal that the 12x8 board is giving Party
   too many routing options.

7. **`v3-mc-denser-horde` shifts Party% down by 10-20%** vs baseline.
   If the shift overshoots (Party < 30%), 8 middle-rank Horde pieces
   was the right number; if undershoots (Party > 50% still), the Horde
   still lacks teeth beyond mere mass.

## Files in this folder

- `sweep-summary.md` / `.csv`  - **rank table, read first**
- `HOW-TO-READ.md`             - this file
- `FINDINGS.md`                - written AFTER the sweep lands; re-frames A/B/C
- `match-<name>.log`           - per-rule-set full text log (game-by-game)
- `match-<name>.pgn`           - all games for replay

## Next steps after the sweep

- **Winner composite >= 0.45** : lock that rule set; move on to
  magnitude tuning (piece values via SPSA, spec section 7).
- **Winner composite 0.30-0.45**: structural design is close; add one
  more iteration of nearby structural probes (similar in scope to v3).
- **No winner above 0.30**     : revisit one level up - board geometry,
  Horde roster, or the asymmetric extinction/flag framework itself.

## How to reproduce this sweep

```bash
dotnet run --project src/YesHybrid.Cli -- sweep \
    --games 150 --depth 6 --parallel 4 \
    --seed 20260423 \
    --out reports/v3-overnight \
    --add-rules rulesets/v3-mc-baseline.json \
    --add-rules rulesets/v3-mc-bloodied.json \
    --add-rules rulesets/v3-mc-minion-fc.json \
    --add-rules rulesets/v3-mc-shallow-flag.json \
    --add-rules rulesets/v3-mc-anyflag.json \
    --add-rules rulesets/v3-mc-compact.json \
    --add-rules rulesets/v3-mc-denser-horde.json
```

Expected wall-clock on Apple-silicon parallel-4: ~60-75 min
(1050 games at roughly 3-5 sec/game effective).
