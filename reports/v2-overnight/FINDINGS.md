# v2 overnight sweep — findings

**Configuration:** 7 rule sets, 100 games each, depth=6, max-plies=300,
parallel=4, seed=20260422.  Total: 700 games, 35.9 min wall-clock.

## Final ranking

| Rank | Rule set | P-W | H-W | Unfin | P%(dec) | Imbal | **Composite** |
|-----:|----------|----:|----:|------:|--------:|------:|--------------:|
|  1 | `minion-caps` | 23 | 17 | 60 | **57.5%** | **15.0%** | **0.340** |
|  2 | `bloodied`    | 61 | 13 | 26 | 82.4% | 64.9% | 0.260 |
|  3 | `baseline`    | 39 |  8 | 53 | 83.0% | 66.0% | 0.160 |
|  4 | `promo-only`  | 39 |  8 | 53 | 83.0% | 66.0% | 0.160 |
|  5 | `flag-only`   |  0 | 10 | 90 | 0.0%  | 100%  | 0.000 |
|  6 | `flag+promo`  |  0 | 10 | 90 | 0.0%  | 100%  | 0.000 |
|  7 | `all-in`      |  0 | 16 | 84 | 0.0%  | 100%  | 0.000 |

## The three structural hypotheses, tested

### 1. "Minions need to be real threats" — *strongly confirmed*

The single change from `W` to `WcF` (Minion gains diagonal capture) is the
clear standout.  Every metric improved:

- Imbalance dropped from **66% to 15%** — the largest swing in the sweep.
- Party-share-of-decisive went from 83% to **57.5%**.  95% CI [42.2%, 71.5%].
- Horde wins jumped from 8 to 17 (+110%) — the Horde is now actually winning.
- Decisive rate slipped slightly (47% -> 40%), median plies rose (157 -> 181):
  games got longer because both sides are fighting instead of Party bullying.

**Interpretation:** the baseline's central imbalance was not Party's
firepower but the **Horde's passivity**.  Minions with no forward threat
are just furniture Party routes around; Minions with `cF` create no-go
zones, forcing Party to engage them on disadvantageous terms.  That
friction *is* the tactical game we want.

### 2. "A flag square unlocks the game" — *falsified as implemented*

All three flag variants (`flag-only`, `flag+promo`, `all-in`) produce
**zero** Party wins across 300 games.  The Leader simply cannot reach g8
through 6 ranks of Horde defense within 300 plies at depth-6.

Why this fails:

- The Leader (`WfF`) is not mobile enough to force passage.
- Horde defense on ranks 7-8 is too dense — at least 8 pieces between the
  Party rank and g8.
- Without tempo pressure on the Horde, the Horde can just form a wall and
  wait; Party can't break it.

**This is a design lesson, not a dead end.**  The flag concept is fine,
but "Leader to g8" is the wrong flag.  Viable variants to test next:

- `flagPiece = l c s x d` — any Party piece wins (if FSF supports multi).
- `flagRegionWhite = g8 h8 f8 g7 h7 f7` — flag *zone* instead of single square.
- `flagRegionWhite = rank 6` — closer flag, Party only needs to reach midboard.
- Flag combined with `minion-caps` so the Horde can't just static-defend.

### 3. "Minion promotion adds tempo" — *falsified at this search depth*

`promo-only` produced **literally identical** stats to `baseline` (39/8/53,
157 median plies).  The depth-6 engine never pushes a Minion far enough to
promote, so the rule never fires.

**Interpretation:** promotion only works if the engine can *see* the reward.
Two ways to fix:

- Promote after a much shorter walk (e.g., `promotionRegionBlack = *3` or *4).
- Raise search depth to 10-12 so the engine plans longer sequences.

For the current harness budget, promotion is a **deep-engine feature** and
shouldn't be relied on at depth-6.

### 4. Bonus: `bloodied` is still only a decisiveness knob

Its composite (0.260) beats baseline but it still runs 82% Party.
Bloodied makes the game *resolve* more often, not more *fairly*.  It's a
knob to stack **on top of** a balanced design, not a balance tool itself.

## What to do next (proposed v3 sweep)

**Lock `minion-caps` as the new working baseline.**  Build v3 around it.

### v3 rule sets — all derived from `minion-caps`

| Name | Change on top of minion-caps | Question it answers |
|------|------------------------------|---------------------|
| `v3-mc-baseline`     | *(= minion-caps, for comparison)*         | Reference point            |
| `v3-mc-bloodied`     | + Bloodied                                | Does bloodied now tighten a balanced game? |
| `v3-mc-minion-fc`    | Minion uses `WfcF` (forward-only capture) | Is full `cF` too strong?   |
| `v3-mc-shallow-flag` | + flag at `rank 6`, Leader-to-flag        | Is the flag playable at shorter distance? |
| `v3-mc-anyflag`      | + flag at g8, any Party piece wins (if FSF supports) | Does widening flagPiece fix the unwinnable problem? |
| `v3-mc-compact`      | + board 10x7 (smaller)                    | Does removing sprawling space force decisive play? |
| `v3-mc-denser-horde` | + extra Minion on rank 7 (more defense)   | Is the Horde currently under-armed despite the WcF buff? |

**Budget:** 7 rule sets × 150 games × depth 6 ≈ 50-60 min parallel-4.

### Open structural question for the user

The v2 data forces a concrete design decision about **how the game ends
for Party**.  Three options, in increasing boldness:

- **A) Keep Treasure as royal piece (our current working baseline).**
  Party wins by checkmate of the stationary Treasure.  Simple; the data
  shows `minion-caps` already balances it meaningfully.  Smallest conceptual
  change from today's game.
- **B) Flag *zone*, any Party piece.**  Treasure-square becomes a 3x3
  flag zone on ranks 7-8; the first Party piece to enter wins.  This
  answers your "why is Treasure a piece?" question cleanly.  Risk: we saw
  single-square flag is too hard; widening should help but we need to test.
- **C) Dual-objective hybrid.**  Keep the Treasure as a piece AND add a
  flag zone.  Party wins by EITHER capturing the Treasure OR reaching the
  flag zone.  The Horde has to defend both objectives, which *structurally*
  solves the "Horde just walls off" problem we saw in v2 flag-only.
  Richest design space; also most complex.

**My recommendation:** run the v3 sweep above to get data on (A)
optimised and (B) widened, then make the A-vs-B-vs-C call from evidence.

## Engineering items for v3

1. **Verify if FSF's `flagPiece` accepts a list** (`l c s x d`).  If yes,
   `v3-mc-anyflag` becomes trivially implementable.  If no, we need an
   alternate approach (e.g., multiple flag regions per piece, or CLI-side
   flag detection).
2. **Fix `Termination` messaging.**  Today's logs say "Treasure checkmated
   or captured" and "Party has no legal response" even for flag variants.
   The game loop should emit the correct cause based on whether a flag
   was reached.
3. **Reconsider `promo-only` hypothesis** at depth 10-12.  Not now; too
   expensive per game.  Revisit once v3 lands a balanced baseline.
