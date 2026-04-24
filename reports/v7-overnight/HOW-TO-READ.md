# How to read the v7 SPSA pilot

**Configuration:** SPSA pilot on top of the v1 canonical ship
(`variants/yeshybrid.ini` as updated in v6: minion-6 startFen, Minion
`WfcF`, `nMoveRule = 50`, `nFoldRule = 3`).  20 iterations, 100
games/eval, 40 matches total, ~50-75 min budget.

## What v7 exists to answer

The v6 winner ships at P%(dec) 44.9% - inside the v6 [40, 60] window
but just below the spec's tighter [45, 55] Section 7 SPSA target.
Closing that 5pp gap is exactly the job SPSA is for.  This run is
the pilot, not the overnight full run: we're checking that the
gradient signal is strong enough to justify a longer tune before we
commit to 4-6 hours of overnight compute.

> Pilot success means any of:
> 1. the trajectory lands P%(dec) in [48, 52] inside 20 iterations
>    ("ship it", after a 300-game confirmation match), OR
> 2. the trajectory shows a clean, monotonic-ish downward curve on
>    the objective even if not yet converged ("go overnight"), OR
> 3. the trajectory is flat-noisy and tells us `c` or `a` is wrong
>    and we need to retune hyperparams first.
>
> Pilot failure is (4) the trajectory is flat AND the individual
> parameter gradients look like white noise - in which case MG piece
> values alone aren't a strong enough lever and v7b goes after the
> lurker-mobility axis v6 already identified as the biggest unused
> balance knob.

## The SPSA loop in one picture

At each iteration `k = 0..19`, we:

1. Draw `delta_i ~ Bernoulli{-1, +1}` independently for each of the
   8 tunable piece letters (`d, s, c, l, x, b, a, u`).  Minion `m`
   is pinned at 100 as the scale anchor; Treasure is royal and not
   tuned.
2. Build `theta_plus = theta + c * delta` and `theta_minus = theta -
   c * delta`, both clipped to `[10, 2000]`.  `c = 40` (piece-value
   units), so each probe shifts every piece by +/- 40 from the
   current theta.
3. Run one match at each probed theta (100 games, depth 8,
   max-plies 600, parallel 4, inheriting stall-rule from the
   canonical .ini).  Compute the objective:

       f(theta) = (PartyShareOfDecisive - 0.5)^2 * 10000

   which is the squared pp distance from 50/50, scaled so numbers
   are easy to read.  At the v1 ship's 44.9%, `f ~= 26`.

4. Estimate the gradient with a single SPSA sample:
   `g_i = (y_plus - y_minus) / (2 * c * delta_i)`.
5. Update `theta -= a * g` with `a = 8`, clipped to the bounds.
6. **Guard-rail:** if either probed point has `UnfinishedRate > 0.25`
   (i.e. the stall rule and search depth aren't enough to keep
   games decisive at that theta), *skip the update* and log a
   "rejected" iteration.  Both probed points still count when
   tracking the globally best theta seen.

## Starting point (theta_0)

| Piece | Betza (movement) | MG value | Rationale |
|-------|------------------|---------:|-----------|
| Defender  `d` | `WcK`     |  300 | King-like short-range value |
| Striker   `s` | `mK2cF`   |  350 | Range-2 mover, bishop-class |
| Controller `c` | `gQ`     |  900 | Queen-class grasshopper |
| Leader    `l` | `WfF`     |  200 | Weak, wazir-plus-fwd-ferz |
| Skirmisher `x` | `KAD`    |  400 | Leaper ring (W+F+A+D) |
| Brute     `b` | `mDcK`    |  250 | Dabbaba-jump + K-capture |
| Artillery `a` | `mR3cK`   |  400 | Rook-3 slider + K-capture |
| Lurker    `u` | `N`       |  300 | Plain Knight |
| Minion    `m` | `W` (shipped with `fcF` extension) | **100 pinned** | Scale anchor, spec "Material Value = 10" x10 |

The exact starting values don't matter much; SPSA will move them.
They just need to span a plausible power ordering so FSF's search
starts with sensible evaluations.

## What "good" looks like per iteration

- **Best case:** `y_plus` and `y_minus` differ by tens of points
  (meaningful gradient signal), and `theta` drifts in a consistent
  direction across consecutive iterations.  This is what convergence
  to a minimum looks like.
- **OK case:** large iteration-to-iteration noise, but the running
  best `y` trends downward over the 20 iterations.  Means SPSA is
  working but our hyperparams are a bit off; overnight run with
  Spall's decay schedule should tighten it.
- **Bad case:** `y_plus ~ y_minus` every iteration (gradients are
  pure noise) AND parameter values oscillate without drift.  Means
  either `c` is too small (perturbation gets drowned by sampling
  noise) or our objective function is locally flat near `theta_0`.

## Go / no-go criteria after 20 iterations

| Outcome of pilot | Action |
|------------------|--------|
| Best probed `y < 4`  (i.e. some visited theta had P%(dec) in [48, 52]) and its Unfin < 20% | Run a 300-game confirmation match at `tune-best.json`.  If confirmed, promote to `variants/yeshybrid.ini` v1.1 and close v7. |
| Best `y` in [4, 20] AND trajectory shows clean downward trend | Commit to overnight full run: 100 iterations, 200 games/eval, Spall decay, ~4-6 hrs. |
| `y` trajectory flat-noisy (no trend) | Retune hyperparams (adjust `c` up to 60-80 or down to 20, `a` to match) and re-pilot. |
| `y` trajectory flat AND all 8 per-parameter gradients null | Declare MG piece-value SPSA insufficient; pivot to v7b (Lurker mobility axis, per v6 FINDINGS "Recommendation for v7" case 1/2/3). |

## Output files in this directory (post-pilot)

- `HOW-TO-READ.md` - this file.
- `tune-iterations.csv` - one row per iteration: delta, y-values,
  per-piece probed values, post-update theta, guard-rail flag.
  Machine-readable; use this for plotting trajectories.
- `tune-trajectory.md` - the same data in human-readable form with
  an iteration-by-iteration narrative.
- `tune-best.json` - the globally best-probed theta in deployable
  ruleset format.  Feed this directly to `yes-hybrid match` for a
  300-game confirmation run.
- `tune.log` - full match output for all 40 SPSA evaluations (no
  PGNs - those would be ~40 MB and aren't useful for tune
  diagnostics).
- `PILOT-FINDINGS.md` - written after the pilot completes;
  trajectory analysis and go/no-go recommendation.

## Budget

- 20 iterations x 2 matches x 100 games = **4000 games**.
- The v1 ship's stall-rule runs ~3 s/game at parallel 4 (measured
  in v6: 873 s / 300 games / 4 workers = 0.73 s/game-wall, or
  ~2.9 s/game-cpu).
- Estimated wall: `4000 * 3 / 4 = ~3000 s ~= 50 min`.  Budget 75
  min with margin for iteration overhead and outliers.
- Seed: `20260425` (different from v6's `20260424` so the opening
  book diverges).
