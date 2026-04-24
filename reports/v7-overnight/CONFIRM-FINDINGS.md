# v7 confirmation match — findings

**Verdict:** `tune-best.json` (iter18-minus θ) does **not** confirm.
At a 300-game sample size, the point estimate P%(dec) = 44.8% is
outside the [48, 52] ship target. This is not a failure of SPSA —
it's a textbook demonstration of selection bias from tracking
"best single probed point" across 40 noisy 100-game evaluations.

**Recommended action:** do **not** promote iter18-minus. Choose one
of:
1. Confirm the **final SPSA iterate** (`d:263 s:319 c:625 l:265
   x:719 b:153 a:487 u:107`) with another 300-game match — this is
   the SPSA-canonical "ship candidate" (Polyak-Ruppert would
   average more iterates; final iterate is the cheap proxy).
2. Accept the v6 canonical (no explicit piece values, P%(dec) =
   44.9%) as v1 final and pivot v7b to a non-material lever
   (Lurker mobility axis per v6 FINDINGS).
3. Commit to overnight SPSA with decay schedule (iteration budget
   100+, games/eval 200+) to actually drive the point estimate
   into [48, 52] with CI support.

## What the confirmation says

| metric | pilot iter18-minus (100 games) | 300-game confirmation | delta |
|---|---:|---:|---:|
| Party wins | — | 124 (41.3%) | |
| Horde wins | — | 153 (51.0%) | |
| Unfinished | 10% | 7.7% | -2.3pp |
| P%(dec)     | 50.0% | **44.8%** | **-5.2pp** |
| 95% CI on P%(dec) | ~[40.0, 60.0] (100-game CI is wide) | **[39.0, 50.7]** | |
| Imbalance   | 0.0% | 10.5% | +10.5pp |
| Median plies (decisive) | 200 | 187 | |
| Decisive rate | 90% | 92.3% | +2.3pp |
| Composite   | — | 0.827 | |
| Wall elapsed | — | 806.5s (13.4 min) | |

The confirmation's 95% CI upper bound (50.7%) barely overlaps the
ship target lower bound (48%). In hypothesis-test terms: we can't
reject `P = 50%` at 95% confidence, but we also can't accept it;
the point estimate says the true value is ~45%.

## Why this happened (selection bias)

At each SPSA iteration we ran two 100-game evaluations. With 100
games, the 95% CI on a decisive P%(dec) ~ 45-50% has a half-width
of ~10pp. Across 40 probed points, it's nearly certain at least
one samples to P%(dec) ≥ 50% by chance alone — even if every
probed θ has the same "true" P%(dec) ~ 45%. That's exactly what
happened: iter18-minus sampled 45/90 Party decisive wins (50.0%),
but the true value at that θ, as the 300-game confirm reveals, is
around 44.8%.

The fix **is not** more SPSA iterations at 100 games/eval (that
amplifies the selection); it's either:
- larger games/eval (200+) so each probe's CI half-width is ≤7pp,
  making selection bias proportionally smaller, OR
- use the **final iterate** or **Polyak-Ruppert average** rather
  than best-probed-point as the ship candidate, since these are
  unbiased estimators of SPSA's fixed point.

## Comparison: where we actually are

| canonical variant | P%(dec) | Unfin | Imbalance | 95% CI | source |
|---|---:|---:|---:|---|---|
| v6 stall-rule (no MG override) | 44.9% | 11.7% | 10.2% | [~38.9, ~50.9] | v6 sweep, 300 games |
| v7 tune-best iter18-minus      | 44.8% | 7.7%  | 10.5% | [39.0, 50.7]    | this confirmation, 300 games |

These are **statistically indistinguishable**. Within confirmation
noise, SPSA reproduced the v6 baseline but did not improve on it.
That's an important signal: either
- (a) material values are a shallow lever and the true
  convergence basin is at ~45% P%(dec), with no θ inside [48, 52]
  actually achievable for this variant, OR
- (b) SPSA hasn't converged yet and needs a longer run with a
  decay schedule to get below the basin floor we've seen.

We can discriminate (a) from (b) cheaply by confirming the **final
SPSA iterate** separately. If it also lands at ~45%, (a) is
strongly supported and v7b should pivot away from MG tuning. If it
lands in [48, 52], the pilot actually did find a new basin and
iter18-minus was just a mis-probe.

## Second confirmation: the final SPSA iterate

θ = `d:263 s:319 c:625 l:265 x:719 b:153 a:487 u:107 m:100`,
seed `20260427`, 300 games, ~15 min.

| metric | value |
|---|---:|
| Party wins | 111 (37.0%) |
| Horde wins | 155 (51.7%) |
| Unfinished | 34 (11.3%) |
| P%(dec)    | **41.7%** |
| 95% CI on P%(dec) | [36.0, 47.7] |
| Imbalance  | 16.5% |
| Median plies | 210 |
| Composite  | 0.740 |

This is **worse** than both iter18-minus (44.8%) and the v6
baseline (44.9%). The final SPSA iterate actually regressed
balance by ~3pp relative to leaving material values to FSF's
auto-derivation.

## Combined verdict across three 300-game samples

| config | P%(dec) | 95% CI | Imbalance | Unfin |
|---|---:|---|---:|---:|
| v6 baseline (no MG override) | 44.9% | ~[38.9, 50.9] | 10.2% | 11.7% |
| iter18-minus (SPSA best-probed) | 44.8% | [39.0, 50.7] | 10.5% | 7.7% |
| final SPSA iterate | 41.7% | [36.0, 47.7] | 16.5% | 11.3% |
| **pooled (SPSA-basin mean)** | **43.25%** | **[39, 47] approx** | - | - |

The pooled two-point mean of SPSA-at-basin thetas is 43.25%,
roughly 1.7pp **worse** than the baseline. Material-value SPSA
has been given a fair test (40 probes + 2 confirmations + 2600
games of basin data) and **does not move balance into [48, 52]**
for this variant. If anything, moving away from FSF's auto-derived
defaults costs us ~2pp of balance.

**Conclusion: MG piece-value tuning is a dead lever. Close v7 as
complete-but-negative, pivot to v7b on a non-material axis.**

## Recommended v7b scope

Per v6 FINDINGS "Recommendation for v7" case-2 and case-3 and the
current evidence:

1. **Lurker mobility axis.** v6's `minion-6-lurker-n-plus` probe
   tested Lurker = Knight (the default). The obvious next probes
   reduce or redirect Lurker's power:
   - `u:WfF`   (Wazir + forward-ferz, like Leader; same file)
   - `u:FfN`   (Ferz plus forward-Knight; half-power)
   - `u:fNH`   (forward-knight + forward-threeleaper; asymmetric)

   With Lurker weakened, Horde's two Lurkers per flank lose their
   tactical dominance. Target: shift P%(dec) from 45% to 50%.

2. **Minion count / placement on rank 7.** v6's `minion-placement-*`
   probes showed minion density is the biggest balance knob. We
   shipped rank-7 with 2 minions (vs v0's 4). Try 3-minion variants
   with different gaps: `m.mubmbum.m`, `m.mubmbumm..`, etc. These
   are discrete micro-moves we haven't exhausted.

3. **Search depth asymmetry.** If SPSA-MG can't help, check
   whether the ~45% basin is a SEARCH artefact (Horde engine gets
   deeper tactical signal per game). Run d=10 vs d=8 on the v6
   baseline. Cost ~40 min for 100 games.

Priority: **option 1** (Lurker mobility) because v6 flagged it as
the biggest unused balance knob AND it's the cleanest probe axis
(one override per ruleset).

## Pause point

Awaiting user decision on v7b scope and whether to close v7
formally (write `FINDINGS.md` for this directory and commit) before
starting v7b, or roll directly into v7b probe design.
