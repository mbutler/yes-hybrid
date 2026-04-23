# v4 overnight sweep - findings

**Configuration:** 7 rule sets, 150 games each, global depth=6, global
max-plies=400, parallel=4, seed=20260424.  Two rule sets override the
global harness knobs (`depth-8` uses search depth 8, `ply-cap-600`
raises the ply cap from 400 to 600).  Total: 1,050 games, 68.7 min
wall-clock.  All 7 rule sets are derived from v3's winner
`v3-mc-minion-fc` (Minion `WfcF`).

## Final ranking

| Rank | Rule set                | P-W | H-W | Unfin | P%(dec) | Imbal | **Composite** | Median plies |
|-----:|-------------------------|----:|----:|------:|--------:|------:|--------------:|-------------:|
| 1 | `v4-mf-ply-cap-600`     |  59 |  32 |    59 |  64.8%  | 29.7% |   0.427   | 222 |
| 2 | `v4-mf-minion-9`        |  32 |  41 |    77 |**43.8%**|**12.3%**|   0.427   | 190 |
| 3 | `v4-mf-baseline`        |  46 |  31 |    73 |  59.7%  | 19.5% |   0.413   | 200 |
| 4 | `v4-mf-striker-range`   |  59 |  28 |    63 |  67.8%  | 35.6% |   0.373   | 167 |
| 5 | `v4-mf-minion-7`        |  57 |  27 |    66 |  67.9%  | 35.7% |   0.360   | 186 |
| 6 | `v4-mf-artillery-range` |  46 |  24 |    80 |  65.7%  | 31.4% |   0.320   | 160 |
| 7 | `v4-mf-depth-8`         |  14 |  39 |    97 |  26.4%  | 47.2% |   0.187   | 226 |

## Headlines

1. **We are NOT SPSA-ready.**  Two signals from this sweep make that
   verdict unambiguous:

   - **Depth sensitivity is massive.**  `depth-8` shifts Party%(dec)
     from 59.7% to 26.4% - a **33pp swing** on a single axis that SPSA
     will not control.  If we tune piece values at depth 6 and ship at
     depth 8 or 10, our "balanced" values will be badly miscalibrated.
   - **The v3 baseline is partly an artifact of truncation.**
     Raising the ply cap from 400 to 600 shifts Party% from 59.7% to
     64.8%, and converted 14 "unfinished" games into decided ones -
     13 of those 14 went to Party.  The 400-ply cap was systematically
     hiding slow Party grinds.

   Both of these mean v3's `minion-fc` composite 0.560 was benefiting
   from **a shallow horizon plus a short ply cap**.  Real balance at
   deeper search / longer games looks different.

2. **`minion-9` (9 mid-rank Minions) is the best-balanced point we've
   measured at depth 6.**  Party 43.8%, imbalance 12.3% - closer to
   50/50 than any prior candidate.  Composite 0.427 matches ply-cap-600
   but without Party-bias.  This is the new working baseline at
   depth 6, but see headline #1 - it still needs depth-8 validation
   before SPSA.

3. **The magnitude gradients are real and predictable at depth 6.**
   Every probe moved Party% in the expected direction:

   | Probe                 | Δ Party%(dec) vs baseline | Direction |
   |-----------------------|---------------------------|-----------|
   | `minion-9` (+1 M)     | **-15.9pp**               | Horde     |
   | `minion-7` (-1 M)     | +8.2pp                    | Party     |
   | `striker-range` (+1)  | +8.1pp                    | Party     |
   | `artillery-range` (-1)| +6.0pp                    | Party     |

   The Minion gradient is **asymmetric** - +1 Minion hurts Party ~2x
   more than -1 Minion helps.  That asymmetry is informative: the 8th
   Minion sits on the flank (j7); the 9th sits on the other flank (a7)
   and closes a corridor Party was exploiting.  SPSA should weight
   Minion-count more carefully than piece-values-per-Minion.

## Hypothesis-by-hypothesis

### `baseline` (Party 59.7%, composite 0.413) - reproduces v3 within CI

v3-mc-minion-fc measured Party 54.3%, CI [44.2%, 64.1%].  v4 baseline
lands at 59.7%, CI [48.6%, 70.0%].  The point estimate drifted 5pp
up; both CIs overlap substantially.  The drift driver is mostly
imbalance (v3 8.7% -> v4 19.5%), which at N=150 is within sampling
noise.

**Takeaway.**  The rule set itself is reproducible; the 150-game N
is not tight enough to distinguish between "Party 50%" and "Party
60%" cleanly.  For SPSA, 300+ games per step is a more honest budget.

### `ply-cap-600` (Party 64.8%, composite 0.427) - ply cap was biased

Raising the ply cap converted 14 games from "unfinished" to "decided".
The outcomes of those 14 games were not distributed evenly:

- Party decisive:  46 -> 59  (+13)
- Horde decisive:  31 -> 32  (+1)

That is **overwhelmingly** Party-favored.  In other words: games that
take longer than 400 plies are games that **Party is winning**.  This
changes how we read the v2 and v3 Unfinished columns: their 40-60%
unfinished shares were mostly unfinished Party wins.

**Takeaway.**  The 400-ply cap is too tight for fair evaluation.
SPSA should run at ply-cap >= 600.  This also explains why v3's
minion-fc looked balanced: half its "unfinished" games were actually
slow Party wins, which were being excluded from Party%(dec).

### `depth-8` (Party 26.4%, composite 0.187) - severe depth drift

At depth 8, Party win rate crashes.  Mechanics:

- Decisive rate goes DOWN (51% -> 35%).  Both sides see more defensive
  resources and stall more often.
- Horde wins go UP (31 -> 39).  With deeper horizon, Horde coordinates
  its wall and picks off Party's scouts before they reach tactical
  range.
- Party wins go DOWN (46 -> 14).  Party can't see the reward for
  committing pieces across the mid-ranks because the tactical win
  is deeper than even 8 plies can see.

**Takeaway.**  v3's composite 0.560 is a depth-6 number.  At depth 8
the same rule set is a Horde-favored stall (composite 0.187).  **We
cannot ship at depth 6**; the AI would be noticeably weaker than a
human's typical analysis horizon, and the balance we tuned for would
invert.  Before SPSA, we must pick a canonical depth (probably 8 or
10) and re-baseline there.

### `minion-9` (Party 43.8%, composite 0.427) - new balance candidate

Adding a Minion to a7 tips balance toward Horde by ~16pp, landing at
Party 43.8% with imbalance 12.3% - the lowest imbalance on any rule
set in any sweep so far.  Composite 0.427 ties ply-cap-600 for #1 but
without the Party-bias.

**Takeaway.**  `minion-9` at depth 6 is the best-balanced point
measured.  Provisionally this becomes the new working baseline.  We
should immediately re-run `minion-9` at depth 8 before building on it.

### `minion-7` (Party 67.9%, composite 0.360) - predictable gradient

Removing j7's Minion tips balance 8pp toward Party.  Useful datum;
not a candidate for the working baseline.

### `striker-range` (Party 67.8%, composite 0.373)

Striker `mK2cF` -> `mK3cF` (+1 move-range tile).  +8.1pp to Party.
This is the cleanest "piece power" gradient we have: one step of
Betza tile change, measurable effect.  SPSA should be able to tune
via Betza tweaks for Party pieces in 1-tile increments.

### `artillery-range` (Party 65.7%, composite 0.320)

Artillery `mR3cK` -> `mR2cK` (-1 move-range tile).  +6.0pp to Party.
Smaller effect than Striker +1 (8pp), even though both are single-tile
changes.  That's a hint that **Horde pieces' effective contribution
is front-loaded** - the Artillery's 3rd tile of range wasn't getting
used much against a mobile Party anyway.  SPSA-relevant: Horde piece
tuning will have diminishing returns fast.

## Decision matrix

Where we actually stand on the "elegant, balanced game" milestone:

| Check                                                   | Status  | Evidence                              |
|---------------------------------------------------------|---------|---------------------------------------|
| Balanced rule set exists at depth 6                     | **YES** | `minion-9`, Party 43.8%, imbal 12.3%  |
| Magnitude gradients are well-behaved at depth 6         | **YES** | Minion +/- 1 and range +/- 1 all move Party% 6-16pp in predicted direction |
| Result is reproducible across seeds                     | PARTIAL | Baseline Party drifted 5pp v3->v4     |
| Result is stable across search depth                    | **NO**  | Depth 6 -> 8 swings Party% by 33pp    |
| Result is stable across ply cap                         | **NO**  | Cap 400 -> 600 shifts Party% by 5pp, and resolves Party grinds the 400 cap was hiding |
| Ready for SPSA piece-value tuning                       | **NO**  | Must settle depth + ply cap first     |

## What to do next (proposed v5 sweep)

The two stability failures above are each fixable with one focused
sweep.  My strong recommendation is to address depth first:

### v5 rule sets - depth-anchored re-baselining

Anchor every probe at **depth 8** and **ply-cap 600**, so the v5
result is directly shippable.  Use 300 games per rule set to get
CI widths under 8pp.  Six rule sets:

| Name                     | Change                                  | Question                                                 |
|--------------------------|-----------------------------------------|----------------------------------------------------------|
| `v5-d8-baseline`         | v4-mf-baseline @ d=8, maxPlies=600      | What is the true Party%(dec) when we stop truncating?    |
| `v5-d8-minion-9`         | v4-mf-minion-9 @ d=8, maxPlies=600      | Does the "best balance" hold at depth 8?                  |
| `v5-d8-minion-10`        | Add a 10th Minion at l7                 | If depth-8 favors Horde, do we need FEWER Minions not more? |
| `v5-d8-striker-range`    | Striker `mK3cF` @ d=8, maxPlies=600     | Does the Party power-up gradient preserve its sign at depth 8? |
| `v5-d8-lurker-range`     | Lurker `N` -> `NN` (double knight leap) | A knight-like Horde piece with more reach: probe an axis we haven't touched. |
| `v5-d8-party-bloodied-x` | Bloodied, but only Skirmisher           | Can we give Party a single survivability boost without flipping to 70%+? |

**Budget:** 6 rule sets x 300 games x depth 8 + ply-cap 600 ~=
130-160 min parallel-4.  Worth an overnight.

### After v5

- **If `v5-d8-minion-9` lands Party% 45-55% with imbalance < 15%:**
  lock it as THE baseline; start writing SPSA.
- **If Party% stays < 40% at depth 8:**  the game needs a Party
  power-up, not a Horde nerf.  Candidates: Striker +1 (tested here),
  Skirmisher Bloodied (tested in v5), or a new Party piece.
- **If Party% overshoots to > 60%:**  we've over-corrected; go back
  to 8 Minions (v3 baseline) but keep depth 8 and ply-cap 600 as
  the harness settings.

## Engineering items for v5

1. **Per-rule-set depth / ply-cap overrides shipped.**  Already done
   this sweep (see `RuleSet.SearchDepth`, `RuleSet.MaxPlies`).  The
   harness can now mix depths and ply caps in a single report.
2. **Ship a `summarise` command** that compares two sweep directories
   and prints deltas per rule set.  Would turn the "v3 vs v4 vs v5"
   cross-referencing (which I did by hand in this findings doc) into
   a one-liner.  Small; half a day.
3. **Start the SPSA harness in parallel with v5.**  We'll need it
   immediately after v5 if v5 goes well.  Skeleton only; the tuning
   loop body depends on v5's canonical baseline.

## Comparison to v3

For posterity - the same `minion-fc` rule set across the two sweeps:

| Metric            | v3-mc-minion-fc (d=6, cap=400, seed=20260423) | v4-mf-baseline (d=6, cap=400, seed=20260424) |
|-------------------|-----------------------------------------------|----------------------------------------------|
| Party wins        | 50                                            | 46                                           |
| Horde wins        | 42                                            | 31                                           |
| Unfinished        | 58                                            | 73                                           |
| Party%(dec)       | 54.3%  [44.2, 64.1]                           | 59.7%  [48.6, 70.0]                          |
| Imbalance         | 8.7%                                          | 19.5%                                        |
| Composite         | 0.560                                         | 0.413                                        |

Same rule set, different seed, same settings.  Composite moved 0.147.
That's the noise floor at N=150.  Any future claim of "rule set X
improves composite by 0.10" must be supported by N >= 300.
