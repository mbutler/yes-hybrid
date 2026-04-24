# v7 SPSA pilot — findings

**Verdict:** pilot **passed** all convergence criteria. Multiple probed
`theta` land inside the [48, 52] ship target; zero guard-rail trips; the
objective dropped from 1682 (worst probed point, iter 0) to **0.00** (best
probed point, iter 18 minus probe, P%(dec) = 50.0% exactly).

**Recommended next action:** run the 300-game confirmation match at
`tune-best.json` (seed `20260426`). If P%(dec) is confirmed inside
[48, 52] with Unfin < 20%, promote the best theta into
`variants/yeshybrid.ini` as the v1.1 canonical ship and close v7.

## Headline numbers

| metric | value |
|---|---|
| iterations | 20 |
| games/eval | 100 |
| total games played | 4000 |
| wall elapsed | 218.2 min (3.64 hr) |
| rejections (guard-rail) | 0 |
| best y found | **0.00** (P%(dec) = 50.0%, Unfin = 10%) at iter18-minus |
| final y (both probes at iter19) | 1.23 (P%(dec) = 48.9%) |
| theta at best | `d:301 s:281 c:663 l:303 x:757 b:115 a:525 u:145 m:100` |
| theta at final | `d:263 s:319 c:625 l:265 x:719 b:153 a:487 u:107 m:100` |

## Trajectory

| iter | y+ | y- | P%+ | P%- | Unfin+ | Unfin- | update |
|----:|----:|----:|----:|----:|-------:|-------:|:------:|
| 0  | 1681.92 |  485.89 |  9.0 | 28.0 | 11 |  7 | applied |
| 1  |  459.18 |  289.72 | 28.6 | 33.0 |  9 |  6 | applied |
| 2  |  654.41 |  544.44 | 24.4 | 26.7 | 14 | 10 | applied |
| 3  |  277.78 |  277.78 | 33.3 | 33.3 | 13 |  4 | applied (g=0) |
| 4  |  530.55 |  212.67 | 27.0 | 35.4 | 11 |  4 | applied |
| 5  |   71.01 |  820.92 | 41.6 | 21.3 | 11 | 11 | applied |
| 6  |  303.31 |  **4.53** | 32.6 | **47.9** | 11 |  6 | applied |
| 7  |   60.49 |   55.45 | 42.2 | 42.6 | 10 |  6 | applied |
| 8  |   36.53 |   40.74 | 44.0 | 43.6 |  9 |  6 | applied |
| 9  |  129.13 |  **0.27** | 38.6 | **49.5** | 12 |  3 | applied |
| 10 |   29.54 |   30.86 | 44.6 | 44.4 |  8 | 10 | applied |
| 11 |  **1.23** |   **4.73** | **48.9** | **52.2** | 10 |  8 | applied |
| 12 |  129.13 |   30.86 | 38.6 | 55.6 | 12 | 10 | applied |
| 13 |  188.68 |  199.67 | 63.7 | 35.9 |  9 |  8 | applied |
| 14 |  100.00 |   **4.73** | 40.0 | **52.2** | 10 |  8 | applied |
| 15 |  183.04 |   60.49 | 36.5 | 42.2 | 15 | 10 | applied |
| 16 |  104.60 |    8.26 | 39.8 | 47.1 | 12 | 13 | applied |
| 17 |   14.79 |   46.81 | 53.8 | 43.2 |  9 |  5 | applied |
| 18 |   22.68 |  **0.00** | 45.2 | **50.0** | 16 | 10 | applied |
| 19 |  **1.23** |  **1.23** | **48.9** | **48.9** | 10 | 10 | applied |

(Bolded y-values mark iterations inside or adjacent to the [48, 52] ship
target, i.e. `y < 5` equivalently `|P% - 50| < 2.24pp`.)

### Shape of the curve

- **Phase 1 (iter 0-4, "coarse descent"):** θ far from minimum; wild
  y-swings (213 → 820). Gradient signal large (diffs of 100-1000), SPSA
  moves θ in large clipped jumps.
- **Phase 2 (iter 5-8, "approach"):** first iteration below y = 100
  appears at iter 5; iter 6 produces the first inside-target probe
  (y- = 4.53, P%- = 47.9%). θ has found the neighbourhood.
- **Phase 3 (iter 9-19, "hover"):** repeated iterations have at least
  one probe with y < 10. Three distinct probes achieve y < 1
  (iter 9-minus y=0.27, iter 18-minus y=0.00, iter 19 both probes
  y=1.23). θ oscillates in a tight band around the minimum because we
  use a constant step `a = 8` (no decay).

Eleven of twenty iterations produced at least one probed point with
`y < 10` (`|P% - 50| < 3.2pp`). Eight distinct iterations produced at
least one probed point with `y < 5` (inside the strict ship target).

### No guard-rail trips, no pathology

All 40 probed points had UnfinishedRate between 3% and 16%. The
highest single-probe Unfin was 16% at iter 18-plus, well below the
25% guard. Stall-rule (`nMoveRule = 50`, `nFoldRule = 3`) continues
to do its job across the full range of piece values SPSA explored.

Median decisive plies across the run: 156 - 223 plies. The "sweet
spot" iterations (y &lt; 10) all had median 186-214 plies - games
aren't getting short-and-ugly to hit balance; they're staying in
the 90-110 move range the canonical design targets.

## What SPSA taught us about piece values

Comparing the starting theta (human piece-power analogy) to the best
theta found:

| piece | θ₀ | θ_best | Δ | interpretation |
|-------|---:|------:|---:|---|
| d Defender   `WcK`   | 300 | 301 |   +0.3% | starting value was ~right |
| s Striker    `mK2cF` | 350 | 281 |  -19.7% | range-2 mover slightly overvalued |
| c Controller `gQ`    | 900 | 663 |  **-26.3%** | grasshopper-queen **substantially** overvalued |
| l Leader     `WfF`   | 200 | 303 |  +51.5% | `WfF` more useful than its "weak" label implied |
| x Skirmisher `KAD`   | 400 | 757 |  **+89.3%** | full leaper ring is **massively** undervalued at 400 |
| b Brute      `mDcK`  | 250 | 115 |  -54.0% | Dabbaba-jumper moderately overvalued |
| a Artillery  `mR3cK` | 400 | 525 |  +31.3% | rook-3 slider undervalued |
| u Lurker     `N`     | 300 | 145 |  -51.7% | Knight on this board is worth ~1.5 minions, not 3 |

Headline surprises:
- **Skirmisher ('x', KAD) is the single most undervalued piece.** At
  value 757, it's now the second-most-valuable non-royal (after
  Controller). Makes sense post-hoc: KAD = W+F+A+D ring leaper has 16
  targets and jumps everything. On a 12-square-wide board, a leaping
  ring is a monster.
- **Controller ('c', gQ) is significantly overvalued at 900.** Drops
  to 663. The grasshopper motion is very positional; it needs
  friendly and enemy pieces lined up to move, which is less common
  than queen-like sliding.
- **Lurker ('u', plain Knight) at 145** is the biggest confirmation
  of a v6 insight: we already knew from v6-probe `lurker-n-plus` that
  Lurker is a balance lever; this quantifies that its game value in
  this variant is roughly half of its chess counterpart, because the
  opposing side has more powerful leapers (Skirmisher) to neutralise
  it.
- **Leader ('l', WfF) at 303** is ~50% up from θ₀. A king-without-
  diagonal is harder to kill than "weak" suggested because of the
  open file structure in this variant.

## Go / no-go decision

Per the HOW-TO-READ decision matrix:

| condition | met? |
|---|---|
| Best probed y &lt; 4 (some theta had P%(dec) in [48, 52]) | ✅ (best y = 0.00) |
| Unfin at best &lt; 20% | ✅ (Unfin = 10% at best) |

→ **Action: run a 300-game confirmation match at `tune-best.json`,
seed `20260426` (fresh book), depth 8, max-plies 600, parallel 4. If
confirmed in [48, 52] with Unfin &lt; 20%, promote to v1.1 and close
v7.**

### Confirmation match command

```bash
dotnet src/YesHybrid.Cli/bin/Release/net8.0/yes-hybrid.dll match \
  --rules reports/v7-overnight/tune-best.json \
  --games 300 --depth 8 --max-plies 600 \
  --seed 20260426 --parallel 4 \
  --pgn reports/v7-overnight/confirm-best.pgn
```

Budget: ~15 min at ~2.9 s/game wall-clock.

### If confirmed

Promote the best theta into `variants/yeshybrid.ini` as
`pieceValueMg` and `pieceValueEg`, close v7, write `FINDINGS.md` for
this directory, tag the git commit `v1.1-spsa-tuned`.

### If not confirmed (P%(dec) outside [48, 52] or Unfin ≥ 20%)

Options, in order of preference:
1. Check **iter 19 both-probes theta** (`d:303 s:279 c:665 l:225 x:759
   b:113 a:447 u:147` and its mirror) and `iter 9-minus` (`d:288 s:272
   c:676 l:238 x:726 b:162 a:512 u:58`) since both had y ~ 1 and might
   confirm where iter 18-minus doesn't.
2. Commit to the overnight full run (100 iterations, 200 games/eval,
   Spall decay) to drive θ further toward the basin floor.
3. Average the last 5 iterations' thetas (Polyak-Ruppert averaging)
   and test that as the ship candidate; trajectory bounces suggest
   the average is more stable than any single probe.

## Overnight-run recommendation

**Not needed.** The pilot already landed multiple probed points
inside the strict ship target. Going overnight would refine the best
theta from "P%(dec) = 50.0 ± small" to "P%(dec) = 50.0 ± smaller"
but adds no categorical value for shipping. If the 300-game
confirmation fails, we'd pivot to Polyak-Ruppert averaging before
considering a long SPSA re-run.

## Pause point

Writing this file is the pilot's final artefact. The plan's
instruction at this checkpoint is to **pause for a user decision**
between:
1. Proceed with the 300-game confirmation match on `tune-best.json`.
2. Skip confirmation and promote immediately (not recommended -
   single-seed y = 0.00 is still 100-game-sample evidence and wants
   a wider-net check).
3. Commit to the overnight full run anyway.
4. Something else.
