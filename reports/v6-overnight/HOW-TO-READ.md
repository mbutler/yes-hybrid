# How to read the v6 overnight sweep

**Configuration:** 6 rule sets, **300 games each**, **depth = 8**,
**max-plies = 600** (except `shortcap` at 400), **parallel = 4**,
**seed = 20260424**.  All 6 rule sets pin `searchDepth: 8` in their JSON
so the run is depth-anchored regardless of CLI flags.  All sit on top
of v5's winner `v5-d8-minion-6` (Minion `WfcF`, rank-7 Horde reduced to
2 Minions at c7 and f7).

## What v6 exists to answer

v5 landed a single rule set in the `[40, 60]` Party%(dec) ship window:
`v5-d8-minion-6` at 44.5% Party(dec), 11.0% Imbalance, 0.513 Composite.
The *balance* side of the ship criterion is met.  The *decisiveness*
side is not - 42.3% of games still hit the 600-ply cap unfinished, far
above the 10% threshold HOW-TO-READ defined for v5.

So v6 has exactly one job: **kill the stall pathology while preserving
minion-6's balance.**  All six probes are layered on minion-6 to hold
balance roughly constant; each probe attacks one hypothesis about where
the stall comes from.

> Tightened ship criterion for v6:
> - Party%(dec) in **[40, 60]**, **and**
> - Imbalance **<= 25%**, **and**
> - Unfinished **<= 15%**.
>
> If any probe hits all three, that becomes the v1 canonical rule set
> (promoted to `variants/yeshybrid.ini`) and the starting point for
> Section 7 SPSA tuning.  If nothing hits, v7 will need a structural
> rather than tuning fix.

## The 6 rule sets

| Rule set | Change on top of v5-d8-minion-6 | Hypothesis |
|----------|----------------------------------|------------|
| `v6-d8-minion-6-striker-range`     | Striker `mK2cF` -> `mK3cF`                                   | Stack the v5 best balance lever (minion-6, +44.5% P%) with the v5 best Party power-up (striker-range, +8.4pp at depth 8).  Striker-range doesn't touch the opening FEN, so it's the cleanest "additive Party buff."  Expected: P%(dec) drifts up from 44.5% toward 50-55%, ideally without making Imbalance worse (because we're closing the existing Horde gap, not opening a new one).  If P%(dec) overshoots above 60% we've over-buffed Party and need to dial back. |
| `v6-d8-minion-6-shortcap`          | `maxPlies` 600 -> 400                                        | Does cutting the ply cap kill stalls without rebreaking balance?  v5 lurker-range showed the truncated tail at minion-6 is mostly Horde wins, so a tighter cap should bias slightly Horde - i.e., we expect P%(dec) to drop a few pp from 44.5%.  If it stays in [40, 50] and Unfin drops below 15%, this is the cheapest possible v1 ship.  If P%(dec) collapses below 40% we've confirmed the cap was hiding the true Horde-favourability of minion-6. |
| `v6-d8-minion-placement-a7-only`   | +1 Minion at a7  (FEN: `m1mubmbu4`)                          | minion-6 has 2 Minions on c7 and f7 (3 minions on full count means c7+f7+i7 in v5-minion-7).  This swap puts the third Minion at the *opposite* corner.  Tests one half of the v5 minion-9 non-monotonicity hypothesis: does adding a Minion specifically at a7 (clogging the Horde's escape line on the Brute/Lurker side) buff Party more than adding it at i7?  If so, "where" matters more than "how many" and v6 has a placement knob to tune. |
| `v6-d8-minion-placement-j7-only`   | +1 Minion at j7  (FEN: `2mubmbu1m2`)                         | The other half of the placement experiment.  3 Minions total, but the third is at the j7 wing rather than i7 (v5-minion-7) or a7 (a7-only, above).  Three way comparison at fixed Minion count: a7-only vs minion-7 (i7) vs j7-only.  v5-minion-7 was 26.5% P%(dec); if the placement axis is real, a7-only and j7-only will diverge from it by more than the 6pp 95% CI, and from each other. |
| `v6-d8-minion-6-lurker-n-plus`     | Lurker `N` (Knight) -> `W` (Wazir, range 1)                  | NOTE: name is from v5's `Recommendation for v6` shortlist; the actual change is *reducing* Lurker mobility (per v5 FINDINGS Section 4: "minion-6 with Lurker reduced to W ... as a Party-side buff on the Horde mobility axis").  Weak prior; v5-lurker-range ruled the Lurker-mobility axis out as a balance lever in the *upward* direction (N -> NN moved Unfin, not P%(dec)).  But the symmetry argument isn't airtight: forcing Horde mobility down to a Wazir might genuinely cripple Horde formation in a way the Knight->Nightrider buff didn't help.  Expected: P%(dec) lifts modestly above 44.5%, Unfin shifts but unclear direction.  This is the lowest-confidence probe of the six. |
| `v6-d8-stall-rule`                 | Enable `nMoveRule = 50`, `nFoldRule = 3`                     | Engine-level stall fix: 50-move rule (50 full moves = 100 plies without a capture forces a draw) plus standard 3-fold repetition.  See "Stall rule mechanics" below.  Expected: a chunk of the previously-unfinished tail converts to *explicit draws* (which the harness still counts as Unfinished, so headline Unfin% may actually look worse here than minion-6); the value is in checking whether *playing under* a stall rule changes engine behaviour - does the engine fight harder for captures when it knows draws are forced?  P%(dec) and Imbalance are the metrics to watch.  If P%(dec) holds in [40, 60] and decisive games come faster (lower Median plies), the rule is working even if Unfin doesn't fall. |

## Stall rule mechanics (`v6-d8-stall-rule`)

The YES Hybrid variant.ini currently sets `nMoveRule = 0` and
`nFoldRule = 0`, i.e., disables both.  Fairy-Stockfish supports four
relevant knobs (from `src/variants.ini` documentation):

- `nMoveRule` (int, default 50) - move count for the 50/n-move rule
- `nFoldRule` (int, default 3) - move count for the n-fold repetition rule
- `nMoveRuleTypes` (PieceSet, default `p`) - pieces whose moves reset the n-move counter on irreversible moves
- `nFoldValue` (Value, default `draw`) - the result on n-fold repetition

We override only the two counts.  `nFoldValue` defaults to `draw`, so
n-fold repetitions terminate with a non-decisive outcome (counted as
Unfinished by `MatchStats`).

The YES Hybrid has no pawns, so `nMoveRuleTypes = p` (default) means
*no piece type* triggers the "irreversible move = reset" branch
through the pawn channel.  Captures still reset the counter (that's
the standard FSF behaviour for any irreversible move regardless of
piece type), so the rule effectively becomes: **"if no captures happen
for 100 plies, the game is a draw."**  At our 600-ply cap that allows
six full stall windows; we expect most ply-cap games to terminate at
ply 100-ish instead.

If this probe shows promise but isn't aggressive enough, v7 should
probe shorter values (e.g. `nMoveRule = 30` -> 60 plies without
capture).  If it shows the opposite - lots of explicit draws in
positions Party would have eventually won past ply 600 - we want to
*relax* it or move to a side-asymmetric "Party loses if no captures
in N moves."  The variant.ini has no built-in asymmetric stall
forfeit; we'd need code support in the harness for that.

## What "good" looks like

For each rule set, the question is one of three outcomes:

1. **Ship.**  P%(dec) in [40, 60] AND Imbalance <= 25% AND Unfin <= 15%.
   The first probe meeting all three becomes the v1 canonical
   `variants/yeshybrid.ini`.  Tie-breaker between multiple shippers:
   highest Composite, then lowest Imbalance, then lowest Unfin.
2. **Move the dial.**  P%(dec) in [30, 65] but at least one other
   criterion misses.  Documents direction-of-travel and feeds v7.
3. **Off the dial.**  P%(dec) outside [30, 65] OR Imbalance > 50%.
   Probe was wrong; document why and rule the lever out.

Specific predictions per rule set:

- **`minion-6-striker-range`**: most likely shipper.  Expect P%(dec) ~50-55%
  (45% baseline + ~7-9pp from striker-range gradient at depth 8),
  Imbalance ~15-20%, Unfin still ~40% (striker-range doesn't fix
  stall in v5 data).  Ships balance, fails decisiveness - which is
  exactly the same place we are with minion-6.  Useful as a SPSA
  starting point either way.
- **`minion-6-shortcap`**: probable Unfin fix, balance risk.  Expect
  Unfin ~10-15%, P%(dec) ~38-43% (slight Horde drift from cutting
  the ply tail).  If P%(dec) holds 40+, this is the easiest ship.
- **`minion-placement-a7-only`** vs **`minion-placement-j7-only`**:
  no strong prior on which dominates; ship one if it lands in window;
  otherwise this pair tells us the placement axis exists (or doesn't)
  for v7.
- **`minion-6-lurker-n-plus`**: low-confidence probe; mainly here to
  rule the lever out symmetrically.  If it surprises us by jumping
  to ~50%+ P%(dec) it's a candidate; otherwise it goes in the same
  bucket as v5-lurker-range (axis is dead).
- **`stall-rule`**: probably moves Unfin sideways (cap-stalls become
  explicit-draw stalls) but might shift Median plies dramatically
  downward and reveal whether the stall pathology is "engine refuses
  to commit" vs "no good capture moves available."  Mostly diagnostic;
  ship only if P%(dec) holds and Median plies actually drops.

## What the metrics are telling you

Same as v5; reproduced here for reference.

- **Party%(dec)** = share of decisive games won by Party (White).
  50% is perfect balance; the 300-game 95% CI half-width is roughly
  +/- 6pp.
- **Imbalance** = `|2 * P%(dec) - 1|`.  Composite multiplies
  decisiveness by `(1 - Imbalance)`, so a rule set that's decisive
  *and* balanced wins.
- **Unfinished** = games that hit max-plies (600 normally; 400 for
  `shortcap`) without a winner.  In v6 this also includes any draws
  produced by the stall rule (`stall-rule` rule set only).
- **Median plies** = median length of decisive games.  Watch this
  in `stall-rule` and `shortcap`; large drops signal the lever is
  doing something to game shape, not just to outcomes.

## Reading the output files

- `sweep-summary.md` / `.csv` - ranked table across the six rule sets.
- `match-<name>.log` - per-rule-set Match stdout, including
  per-game termination reasons.  Check this when a rule set's
  Composite is unexpected.
- `match-<name>.pgn` - the full PGN archive; useful for hand-inspection
  of unusual terminations (1-ply Treasure captures, sub-100-ply
  Party extinctions, etc.).

## Budget

- 1800 games total (6 x 300) at depth 8 / ply-cap 600 (400 for
  shortcap, which should be ~33% faster).
- v5 measured ~125 min wall clock at parallel 4 for 5 fresh rule sets.
  v6 has 6 rule sets but `shortcap` runs faster, so plan for
  ~135-150 min.  Overnight slot is plenty.
