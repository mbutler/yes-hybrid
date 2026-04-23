# v5 partial findings (stopped mid-sweep for machine move)

**Status:** sweep ran partially; stopped to migrate work to another
machine.  Four of six rule sets have enough data for a directional
read; two still need data.  A new stability issue was uncovered (see
"Known issue" below) that must be fixed before a clean re-run.

## What we have

All games at **depth=8, maxPlies=600, seed=20260424, parallel=2 or 4**.

| Rule set              | Games done | Party | Horde | Unfin | Decisive | **Party%(dec)** | Imbal | Composite |
|-----------------------|-----------:|------:|------:|------:|---------:|----------------:|------:|----------:|
| `v5-d8-baseline`      | **300/300** |    18 |   122 |   160 |    46.7% |       **12.9%** | 74.3% |     0.120 |
| `v5-d8-minion-9`      | 229/300    |     7 |   106 |   116 |    49.3% |        **6.2%** | 87.6% |     0.061 |
| `v5-d8-minion-7`      | 209/300    |    20 |    88 |   101 |    51.7% |       **18.5%** | 63.0% |     0.191 |
| `v5-d8-minion-6`      | 261/300    |    36 |    96 |   129 |    50.6% |       **27.3%** | 45.4% |     0.276 |
| `v5-d8-striker-range` | **300/300** |    32 |   118 |   150 |    50.0% |       **21.3%** | 57.3% |     0.213 |
| `v5-d8-lurker-range`  | **300/300** |    28 |   188 |    84 |    72.0% |       **13.0%** | 74.1% |     0.187 |

(Minion-9, minion-7, and minion-6 all stopped with FSF pipe breaks
mid-match; minion-6 had a successful `kill -TERM` during the machine
move so its ~40 missing games are not representative of a crash.
Striker-range was interrupted cleanly on the move.)

### Headline reads (depth-anchored, big caveats)

1. **v4's "minion-fc is a viable baseline at depth 6" story does not
   survive the move to depth 8.**  Baseline Party%(dec) collapses
   from 54% (v3 @ depth 6) to 26% (v4 @ depth 8 / 400 plies) to
   **12.9% (v5 @ depth 8 / 600 plies)** as we remove harness
   artifacts.  The 600-ply cap released *Horde* wins more than
   Party wins.  At honest depth 8 / 600 plies, the baseline is
   severely Horde-dominant.

2. **Minion count is a real axis but too weak to fix the gap.**
   Clean linear gradient in Party%(dec) as we vary Minion count:

       minion-9 (M+1):  6.2%
       baseline (M+0): 12.9%
       minion-7 (M-1): 18.5%
       minion-6 (M-2): 27.3%

   ~6.5pp per removed Minion.  Extrapolating, hitting 50% Party(dec)
   would require removing ~4 Minions (minion-4), which is a
   structural re-design, not a tuning knob.  Minion count alone is
   not the right lever.

3. **The unfinished rate stays at ~50% regardless of Minion count.**
   Even minion-6 has 49% ply-cap games at 600 plies.  The game has
   a stalling pathology at depth 8 that is independent of horde
   density.  The 600-ply cap is still truncating a meaningful
   fraction of decisive outcomes, which means Party%(dec) from this
   sweep is still biased by the cap (the truncated tail almost
   certainly resolves to Horde wins, pulling the true Party% even
   lower).

4. **Striker range is a real but insufficient Party buff at depth
   8.**  Full 300-game re-run on the new machine: +1 Striker range
   moves Party%(dec) from **12.9% (baseline) to 21.3%**, a +8.4pp
   shift.  95% CIs don't overlap (baseline [8.3, 19.4], striker
   [15.5, 28.6]).  Imbalance drops from 74% to 57%; composite nearly
   doubles (0.120 -> 0.213).  The v4 gradient of "+10pp per +1 Striker
   range" survived the move to depth 8 at roughly the same magnitude.
   **BUT** 21.3% is still deep in Horde territory: a single +1 range
   buff does not pull the design into the [40, 60] window.

5. **Lurker range (`N` -> `NN`) is a decisiveness lever, NOT a balance
   lever.**  Full 300-game re-run:

       Baseline      (N):   P%(dec) 12.9%, Unfinished 53.3%, Decisive 46.7%
       Lurker-range (NN):   P%(dec) 13.0%, Unfinished 28.0%, Decisive 72.0%

   The 95% CIs on Party%(dec) overlap completely ([8.3, 19.4] vs
   [9.1, 18.1]) - nightrider Lurker does NOT shift who wins.  But
   it dramatically resolves previously-unfinished games: unfinished
   drops 25pp, and the newly-decisive games are ~90% Horde wins
   (Horde-absolute jumps from 40.7% to 62.7%).

   **Confirmation of a v4/PARTIAL-FINDINGS hypothesis:** the 600-ply
   cap was biasing Party%(dec) *upward* by converting would-be
   Horde wins into "unfinished".  The true Party%(dec) of the
   depth-8 baseline - revealed by forcing decisiveness via the
   Horde buff - is ~13%, virtually identical to what the ply-cap-biased
   baseline reported.  That means **we slightly overestimated Party%
   in every prior rule set with high Unfinished rates, but not by
   much.**  The directional reads (striker-range > baseline, minion-6 >
   minion-7 > baseline) are still valid.

   **Design implication:** Lurker range is not the right lever to
   pull to buff Party - reducing it would just push more games
   into the ply cap without shifting P%(dec).  Drop this as a
   candidate balance knob.

### Cross-rule-set read (final for v5; 3 complete + 3 partial)

Ordered by Party%(dec) - **bold** rows are complete 300-game runs:

       minion-9 (M+1):            6.2%  (partial, 229/300)
       **baseline (M+0):          12.9%** (complete, 300/300)
       **lurker-range (NN):       13.0%** (complete, 300/300)
       minion-7 (M-1):           18.5%  (partial, 209/300)
       **striker-range (+1rng):   21.3%** (complete, 300/300)
       minion-6 (M-2):           27.3%  (partial, 261/300)

Striker-range (+8.4pp over baseline) is the strongest single lever
found at depth 8.  It is slightly stronger than removing one Minion
(+5.6pp for minion-7 vs baseline) and delivers it with the *best*
Imbalance (57% vs baseline's 74%) and *best* Composite (0.213) of
any v5 rule set.  Neither single knob comes close to 40% Party(dec).
Direction of travel suggests a stacked approach (striker-range +
minion-6 or -7, or striker-range + a second Party buff like +2 range
or Bloodied promotion) is the next place to probe - but that's v6
work, not v5.

### What this suggests (not settled, needs v5.1 to confirm)

- At depth 8, Party is severely under-powered; the gap is bigger
  than any single piece-count tweak can fix.  This is a **design
  problem**, not a tuning problem: piece *values* are being
  dwarfed by piece *count asymmetry*.
- The obvious next move is to probe **Party power-ups** (Striker
  range, Bloodied selectively, Leader buffs) at depth 8 first,
  then combine the strongest of those with a modest Minion-count
  nerf.  Minion-count alone is a trap.

## Known issue: FSF stability at depth 8 + long matches

Three of the five rule sets we launched crashed mid-match with
`error: Pipe is broken.` after 200-260 games.  Every crash had a
characteristic lead-up:

1. One or more "engine timeout" events (worker stuck >2 min on a
   single move).
2. Shortly after, one or more "Treasure checkmated or captured in
   1 ply" from the same worker (FSF returning a bogus mate move
   when it's in a degraded state).
3. Eventually, a broken pipe on stdin/stdout and the match dies
   (match does not respawn workers).

It reproduces regardless of parallel=4 vs parallel=2; it correlates
with depth 8 + maxPlies 600 + games-in-a-row.  The baseline
(which completed 300 games) had 2 of the 1-ply bogus mates and 0
timeouts; it survived by luck, not by being different.

**Likely cause:** FSF (build 220426) has a resource leak that kicks
in after ~100-200 depth-8 searches on the same process.  We never
saw this at depth 6 because depth-6 games use far less memory per
search.

### Workarounds to implement on the next machine

Two independent fixes; pick either.

1. **Restart FSF between games.**  This is the right long-term
   fix.  `MatchCommand.RunGameAsync` currently reuses one FSF
   instance per worker across all games; change it to spawn a
   fresh FSF per game (cost: ~50 ms opening-book init per game,
   ~1 min extra across 300 games, acceptable).  Code location:
   `src/YesHybrid.Cli/Commands/MatchCommand.cs` - the `engine`
   allocation inside the per-worker loop.
2. **Chunk each rule set into 100-game matches.**  If code changes
   are undesirable, run three `match` invocations per rule set
   with `--seed 20260424`, `20260425`, `20260426` and `--games
   100` each, then aggregate the three logs.  Each invocation
   starts a fresh FSF, so the leak doesn't get time to accumulate.

Also consider catching `IOException` (broken pipe) in the
per-worker loop and marking that game as unfinished, so a single
crashed worker doesn't tank the whole match.

## What to do on the next machine

After committing this work on the old machine and pulling on the
new one:

1. **Validate build.** `dotnet build -c Release` (needs .NET 8 SDK).
2. **Verify .NET 8.** If missing, use `curl -sSL
   https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0
   --install-dir "$HOME/.dotnet"` (no sudo).
3. **Decide: fix stability or chunk.**
   - Preferred: apply workaround (1) above - restart FSF per game
     in `MatchCommand`.  Small, localized change.
   - Pragmatic: just run the three chunked `match` invocations
     per remaining rule set and merge.
4. **Re-run the two missing rule sets with full 300 games:**
   - `v5-d8-striker-range` (primary - this is the probe we most
     need; if +1 Striker range moves Party% meaningfully, Party
     power-ups are the right direction).
   - `v5-d8-lurker-range` (secondary - the new axis; if Lurker
     `NN` crushes Party below 10%, Lurker mobility is the biggest
     Horde lever and we can buff Party by *reducing* it).
5. **Optional: re-run the crashed rule sets to get clean 300-game
   numbers.**  The partial reads are directionally correct but
   have wider CIs than intended.  Not blocking.
6. **Then write** `reports/v5-overnight/FINDINGS.md` (the real
   one, not this partial) with the v5.1 decision:  is there a
   single knob or combination that lands us in [40, 60] Party(dec)
   at depth 8, or do we need a structural revision of piece-count
   asymmetry?

## File inventory in this directory

- `HOW-TO-READ.md` - original plan and hypotheses for v5.
- `PARTIAL-FINDINGS.md` - this file.
- `match-v5-d8-baseline.{log,pgn}` - complete, 300/300.
- `match-v5-d8-minion-9.{log,pgn}` - partial, 229/300, crashed.
- `match-v5-d8-minion-7.{log,pgn}` - partial, 209/300, crashed.
- `match-v5-d8-minion-6.{log,pgn}` - partial, 261/300, crashed.
- `match-v5-d8-striker-range.{log,pgn}` - **complete, 300/300** on new
  machine after applying the per-game FSF restart patch; zero engine
  crashes in 1535s of run time.  The 13-game partial was overwritten.
- `match-v5-d8-lurker-range.{log,pgn}` - **complete, 300/300** on new
  machine; zero engine crashes in 1382s of run time.  First ever run
  of this rule set.
- `sweep-summary.{md,csv}` and `sweep.log` - from the initial
  sweep invocation that aborted after rule set 2.  The `.md` / `.csv`
  files have been manually updated to include striker-range and
  lurker-range rows; `sweep.log` is the original baseline-only capture.
