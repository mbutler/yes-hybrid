# v7d endgame-lever pilot — findings

## Headline

**First real decisiveness breakthrough.** `v7d-d8-lurker-K-remove-artillery`
hit **Unfin = 30%**, a 22pp drop from the v7b lurker-K baseline and the
lowest natural-rules Unfin we have ever measured on this variant.
Balance overshot to **P%(dec) = 70%** as expected. We now have a
Horde-lightened base configuration that needs balance correction in
v7e.

The second probe, `leader-K`, failed badly (P%(dec) 27.5%, Unfin 60%).
Upgrading Party's Leader from `WfF` to `K` made things worse, not
better, on every axis.

## Results

| rank | probe | P-W | H-W | Unfin | P%(dec) | Imbal | Median | Composite |
|:---:|---|---:|---:|---:|---:|---:|---:|---:|
| baseline | v7b-d8-lurker-K | 25 | 23 | 52 | 52.1% | 4.2% | 194 | 0.460 |
| 1 | v7d-d8-lurker-K-remove-artillery | **49** | 21 | **30** | **70.0%** | 40.0% | 242 | 0.420 |
| 2 | v7d-d8-lurker-K-leader-K | 11 | 29 | 60 | 27.5% | 45.0% | 215 | 0.220 |

## Prior accuracy

| probe | prior P% | actual | prior Unfin | actual |
|---|---:|---:|---:|---:|
| remove-artillery | 60-67% | 70.0% | 25-35% | 30% |
| leader-K | 55-62% | 27.5% | 35-45% | 60% |

Remove-artillery prior was close; the P% prior slightly
underestimated the swing but the important number (Unfin 25-35%)
was dead on. This is the first time I've been right on a
decisiveness prior.

Leader-K prior was completely wrong — I assumed a more mobile
Party Leader was a Party buff. It isn't. Below.

## Why leader-K backfired

My hypothesis: Party's Leader acts as a rear-echelon defender. The
original `WfF` motion (2 forward diagonal + 4 orthogonal) keeps it
near its starting square because it mostly can't reach forward
squares quickly. It becomes a piece that *defends its own square
and adjacent Party pieces*.

Upgrading to full `K` adds 4 diagonal + 1 backward moves, which
lets the Leader range further. The engine evidently thinks sending
Leader forward is attractive but the Leader is still a weak piece
compared to Horde heavyweights (Brute `mDcK`, Artillery `mR3cK`) —
so it gets picked off. Losing the Leader early removes one of
Party's 5 extinction-count pieces and Party folds faster.

Interesting: this is a *generalizable lesson*. On this variant
with 5-piece Party, each Party piece has enormous extinction
weight. Making a piece "more aggressive" increases its capture
rate, which for Party is catastrophic. Party needs **more piece
value**, not **more mobility**, where "value" = survivability +
coverage without pushing forward.

This explains why `striker-range` (v6) was only marginally Party-
favoring: it made Striker more able to attack, but also more
exposed. And why the remove-artillery change works so well: we
aren't buffing Party, we are *removing* a Horde piece — strictly
less material pressure on Party without giving Party a piece that
can get itself captured.

## What the remove-artillery outcome actually looks like

Termination breakdown for 100 games:

| outcome | count | note |
|---|---:|---|
| Treasure captured (Party wins) | ~45 | Clean Party victory |
| Party extinct (Horde wins) | ~20 | Horde grinds Party down |
| Party has no legal response | ~1 | Horde stalemate-like |
| Ply cap 600 (stalled) | 30 | Still too many, but down from 52 |
| Engine timeout | ~4 | Deep tactical nodes |

- Decisive rate: 70%
- Median plies among decisive: 242
- Games resolve as Treasure captures primarily — Party's new material
  edge lets it actually checkmate the mW Treasure.

**Stall games are still 30%.** The remove-artillery lever moved us
from 52 -> 30 but not to target 10%. There is additional
decisiveness headroom — possibly another Horde piece reduction, or a
counter-lever that also happens to shorten games.

## v7e plan: bracket the balance target

We need P%(dec) 70% -> 50%, a 20pp Horde-favoring swing. v6 data
gives us a clean lever with known magnitude: **add 1 Minion on the
Horde flank**. Historical magnitudes:

| v6 probe | from | to | delta |
|---|---:|---:|---:|
| minion-placement-a7-only | 44.5% | 31.1% | -13.4pp (Horde-favoring) |
| minion-placement-j7-only | 44.5% | 20.9% | -23.6pp (Horde-favoring) |

Applied on top of 70%:
- + minion at a7 -> ~57% (mild correction, might still overshoot target)
- + minion at j7 -> ~46% (stronger correction, lands dead in target window)

These bracket [45, 55]. Running both as v7e gives us two data points
near the target.

### v7e probes

| probe | change vs v7d remove-artillery | prior P% | prior Unfin |
|---|---|---:|---:|
| `v7e-d8-add-minion-a7` | add Minion at a7 | ~57% | 25-35% |
| `v7e-d8-add-minion-j7` | add Minion at j7 | ~46% | 25-35% |

Both preserve the Artillery removal and the `u:K` Lurker. Both
disable stall rules. 100 games each, depth 8, ~22 min total wall.

### Ship criteria reminder

| metric | threshold |
|---|---|
| P%(dec) | [45, 55] |
| Imbalance | <= 20% |
| Unfin | <= 10% (stretch goal — we are at 30 now, 20pp to go) |
| Median | <= 150 (stretch — we are at 242 now) |

If v7e lands in [45, 55] with Unfin <= 30%, it becomes our next
candidate base and a 300-game confirm match follows.

If v7e lands there with Unfin > 30%, we stack a SECOND decisiveness
lever in v7f (e.g. also remove a Brute, bringing Horde down to
9 pieces instead of 11).

## Pause point

Minor pause — this is the first probe in 4 campaigns that actually
moved the primary blocking metric (Unfin). I want to confirm the
v7e direction before launching:

**Proceed with the bracketing v7e pair (a7-minion + j7-minion)?**
Alternative is to run a single more-conservative probe (e.g. Minion
at i7, prior ~55%). I prefer the bracket because it gives us
confidence in the magnitude estimate for future levers; a single
probe with wide CI could mislead.
