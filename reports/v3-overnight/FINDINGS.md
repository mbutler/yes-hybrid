# v3 overnight sweep - findings

**Configuration:** 7 rule sets, 150 games each, depth=6, max-plies=400,
parallel=4, seed=20260423.  Total: 1,050 games, 64.1 min wall-clock.
All 7 rule sets are derived from v2's winner `minion-caps` (Minion `W`
-> `WcF`), i.e. each adds exactly one axis of change on top of the
balanced v2 baseline.

## Final ranking

| Rank | Rule set | P-W | H-W | Unfin | P%(dec) | Imbal | **Composite** | Median plies |
|-----:|----------|----:|----:|------:|--------:|------:|--------------:|-------------:|
| 1 | `v3-mc-minion-fc`    | 50 | 42 |  58 | **54.3%** | **8.7%**  | **0.560** | 187 |
| 2 | `v3-mc-baseline`     | 40 | 30 |  80 |    57.1%  |   14.3%   |   0.400   | 213 |
| 3 | `v3-mc-bloodied`     | 57 | 26 |  67 |    68.7%  |   37.3%   |   0.347   | 198 |
| 4 | `v3-mc-denser-horde` | 25 | 51 |  74 |    32.9%  |   34.2%   |   0.333   | 205 |
| 5 | `v3-mc-shallow-flag` | 17 | 39 |  94 |    30.4%  |   39.3%   |   0.227   |  90 |
| 6 | `v3-mc-compact`      |  9 |103 |  38 |     8.0%  |   83.9%   |   0.120   | 140 |
| 7 | `v3-mc-anyflag`      |  0 | 30 | 120 |     0.0%  |  100.0%   |   0.000   | 216 |

## Headlines

1. **`minion-fc` is the new working baseline.** Composite **0.560** -
   a 40% jump over v2's best.  Party% 54.3% sits dead-center in the
   30-70% balance band, imbalance 8.7% is the lowest we've measured
   on any rule set.  The hypothesis that `WcF` might be "too strong"
   is confirmed: restricting Minion captures to **forward diagonals**
   both balances *and* decides the game better than full-diagonal.

2. **Option B (flag-zone, any Party piece) is dead** for v3 parameters.
   `anyflag` with `flagPiece = l c s x d` at g8 produced **zero** Party
   wins (CI [0.0%, 11.4%]).  The multi-piece flagPiece syntax works
   (verified via an engineering probe - see below), but Horde's rank 7
   wall still cannot be breached by any single Party piece at depth 6.

3. **Option A (keep Treasure) is now decisively the right call** -
   the three strongest rule sets (`minion-fc`, `baseline`, `bloodied`)
   all keep the Treasure as royal.  We can table C and proceed.

4. **Board size is NOT the balance axis.** `compact` (10x7) collapses
   to 8% Party (!) - shrinking the board gives the Horde's mass
   advantage even more purchase.  The 12x8 geometry is correct; do
   not reach for it as a balance lever.

## Hypothesis-by-hypothesis

### `minion-fc` (composite 0.560) - winner

The mechanical change: `WcF` -> `WfcF` removes the **two rear diagonal
captures** a Minion previously had.  Effects observed:

- Party decisive share: 57.1% -> 54.3% (closer to 50).
- Horde wins: 30 -> 42 (40% more Horde decisive games).
- Unfinished: 53.3% -> 38.7% (big decisiveness win).
- Median plies: 213 -> 187 (games end sooner).

**Interpretation.** Full-diagonal `cF` made Minions into omnidirectional
chokepoints: once Party crossed the Minion line, the Minions could
still attack Party pieces *from behind*.  That turned into a kind of
"sticky wall" Party had to invest time demolishing.  Removing rear
diagonals lets Party *pass through* the Minion row once it's gotten
through frontally, which frees Party to press on the Treasure - but
Party now needs to land the first hits on Minions, or Minions
counter-attack forward and remove tactical options.

The net effect is the kind of **traded-initiative** dynamic we've been
aiming at: neither side has a cheap defensive shell.

### `baseline` (composite 0.400) - reference

Same rule set as v2's `minion-caps` at 100 games; at 150 games the
composite *improves* (0.340 -> 0.400) because the CI tightens and
the Party% estimate sharpens.  Party decisive 57.1%, CI [45.5%, 68.1%].
No regressions; v2's minion-caps result is reproducible.

### `bloodied` (composite 0.347) - still a Party knob

Same conclusion as v2: Bloodied biases toward Party.  Here it moves
Party decisive from 57.1% to 68.7% (bias UP), while lifting decisiveness
from 47% to 55%.  **Bloodied is a decisiveness knob that biases Party**,
not a balance tool.  Worth keeping in reserve if post-tuning the
balanced design stalls too often, but not an axis to lead with.

### `denser-horde` (composite 0.333) - Horde flips

Two extra Minions on rank 7 (b7, k7) pushed Party decisive to 32.9%
(CI [23.4%, 44.1%]).  So **Minion count IS a live balance lever** -
baseline had 8 mid-rank Minions, 10 over-tips toward Horde.  Useful
information for magnitude tuning: if post-SPSA the balanced baseline
drifts Party-ward, adding a 9th Minion is a blunt-but-predictable
correction.

### `shallow-flag` (composite 0.227) - flag zone is playable but wrong shape

The flag variant that v2 declared unwinnable (`flagRegionWhite = g8`)
becomes *playable* at rank 6: 17 Party wins out of 150, median decisive
plies 90 (fastest in the sweep).  But Party decisive share crashed to
30.4% - the Horde defends rank 6 too cheaply.  The design lesson:

- **Flag concept works** when reachable within plausible tempo.
- **Leader as single flagPiece is still too narrow** - the Leader is
  too slow to force through even 5 ranks.
- **Rank 6 whole-row target is too ambitious for Party** - Horde
  doesn't even need to hold rank 7, it can fight mid-board.

The flag variant direction is *not* dead, but the right next probe
is probably a **mid-depth flag region (e.g. rank 5)** combined with
a **mobile flag piece (Skirmisher `KAD`)** or **multiple small flag
zones**.  See "what to do next".

### `compact` (composite 0.120) - confirmed bad direction

10x7 compressed board produced 8% Party decisive share.  The Horde
gets the full benefit of its mass advantage because Party can't
maneuver laterally.  Party pieces collide head-on and lose trades.
**Do not revisit compact geometry.**  If we ever want tighter games,
the lever is `--max-plies` (enforce a faster cut-off), not board area.

### `anyflag` (composite 0.000) - negative result

With `flagPiece = l c s x d` @ g8, Party has 5 different pieces that
could win by occupying g8.  Zero wins in 150 games.  Why the multi-
piece flag helped less than we expected:

- The Horde rank 8 wall (a b t b a) is 5 defenders on the terminal
  rank.  Five flag pieces vs five defenders is not a widening; the
  wall's density keeps pace with the added attacker options.
- All 5 listed Party pieces need to **cross 6 ranks** to reach g8,
  through 13 Horde pieces.  At depth-6 search, no single piece can
  plan a route that survives contact.
- The only way a flag zone could work is either (a) a flag **region**
  larger than the wall can cover simultaneously (not tested), or
  (b) a flag **depth** shorter than 6 ranks (`shallow-flag` tried 2
  ranks in and was Horde-dominated).

This is the second zero-Party-wins result we've seen for flag mechanics
in this codebase.  My reading is that **single-square flag + sparse
Party = unwinnable against a mass Horde** is a load-bearing conclusion,
not an implementation bug.

### Engineering probe: `flagPiece` list syntax

Before the sweep, I verified Fairy-Stockfish's handling of multi-piece
flag declarations.  Result:

- `flagPiece = l c s x d` parses without error.
- An isolated Defender on g7 finds `g7g8` with `score mate 1` at
  depth 1 (verified end-to-end).
- A non-listed piece type (Minion) does **not** trigger the flag win
  when moving to g8 - i.e. the list is exclusive, not substring.

So the zero-win result for `anyflag` is a game-dynamics verdict, not
a mechanism bug.

## What to do next (proposed v4 sweep)

**Lock `v3-mc-minion-fc` as the working baseline.**  Its composite
of 0.560 with Party 54.3% is the best balance-plus-decisiveness point
we've produced.  v4 should focus on **magnitude tuning** on this base,
not more structural hypotheses.

### v4 rule sets - derived from minion-fc

| Name | Change | Question it answers |
|------|--------|---------------------|
| `v4-mf-baseline`       | *(= minion-fc)*                  | Reference point. |
| `v4-mf-ply-cap-600`    | `--max-plies 600`                | How many of the 58 "unfinished" are decided further out? |
| `v4-mf-depth-8`        | `--depth 8`                      | Does deeper search materially change the balance point? |
| `v4-mf-minion-9`       | Add a 9th mid-rank Minion (a7)   | First magnitude probe: Party down to ~45%? |
| `v4-mf-minion-7`       | Remove one Minion (j7)           | Opposite magnitude probe: Party up to ~60%? |
| `v4-mf-striker-range`  | Striker `mK2cF` -> `mK3cF`       | One-step piece-value probe on the Party side. |
| `v4-mf-artillery-range`| Artillery `mR3cK` -> `mR2cK`    | Matching probe on the Horde side. |

**Budget:** 7 rule sets x 150 games x depth 6 ~= 70-80 min parallel-4.
(`depth-8` and `ply-cap-600` sets will be somewhat slower; if we
enlarge the ply cap, expect 90+ min total.)

### After v4

If v4 confirms a stable magnitude baseline in the 48-55% Party window
with unfin < 35%, we are ready to move to **SPSA piece-value tuning**
(spec section 7) against that fixed rule set.  That's the real
"balancing pass" the project has been driving toward.

### What about the flag/zone direction?

Park it for now.  Two sweeps (v2, v3) have shown flag-only variants
are either unwinnable or Horde-dominant at depth 6.  If we revisit,
the viable probe shape is **multiple shallow flag regions on Party's
side of mid-board**, combined with the `minion-fc` baseline - but
we should let SPSA tuning finish on option A first, so we have a
proper reference point to judge a flag variant against.

## Engineering items for v4

1. **Fix `Termination` messaging once more.**  GameLoop now reports
   "Flag reached or Horde stalemated" for non-Treasure variants,
   which is accurate but lossy - we could distinguish flag-terminal
   from Horde-stalemate by inspecting whether a flag piece sits on
   a flag square in the final FEN.  Low priority (cosmetic).
2. **Variable-board-size fix landed.**  `Position.Parse` now auto-
   detects files/ranks from the FEN.  The `compact` variant would
   previously crash the harness; that regression is closed.
3. **SPSA harness.**  Spec section 7 item, deferred since v1.  This
   is the natural next engineering push after v4 locks magnitude.
   Scope: script repeated `match` runs with varying piece-value
   offsets, minimise imbalance, output a recommended piece-value
   profile.  Estimated 2-3 days of work.
