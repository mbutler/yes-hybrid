# v7c FEN-compression pilot — findings

## Headline

**All three compression probes failed on balance, and none meaningfully improved decisiveness.** FEN compression is not the right lever for this variant. My prior was wrong.

## Results

| rank | probe | distance | P-W | H-W | Unfin | P%(dec) | Imbal | Median | Composite |
|:---:|---|:---:|---:|---:|---:|---:|---:|---:|---:|
| baseline | v7b-d8-lurker-K | 6 | 25 | 23 | 52 | **52.1%** | **4.2%** | 194 | 0.460 |
| 1 | v7c-d8-lurker-K-compress-5 | 5 | 20 | 32 | 48 | 38.5% | 23.1% | 201 | 0.400 |
| 2 | v7c-d8-lurker-K-compress-3 | 3 | 17 | 38 | 45 | 30.9% | 38.2% | 184 | 0.340 |
| 3 | v7c-d8-lurker-K-compress-4 | 4 | 16 | 32 | 52 | 33.3% | 33.3% | 175 | 0.320 |

## What went wrong with the prior

The v7b writeup assumed "compression = both armies meet faster = decisive resolution forms earlier." Armies *do* meet faster, but that turns out to hurt Party disproportionately:

- **Party has 5 pieces starting on rank 1.** Horde has 9 pieces on ranks 7-8.
- When the armies are 6 ranks apart, Party has 3-5 full moves to develop pieces, reach obstacles that funnel Horde attack lanes, and set up defensive formations. The obstacle squares on d5/i5/e4/f4 are effectively Party's friend.
- When the armies are 3-5 ranks apart, Party doesn't get setup time. Horde piles into contact with a 9-vs-5 material edge, and numerical superiority wins the early exchanges.
- Horde wins rose from 23 → 32/38. Party wins fell from 25 → 20/17/16. **The Unfin rate barely moved** (52 → 48/52/45) because stalls happen after the initial exchange in a reduced-material middlegame — and that middlegame is the same regardless of how fast the first trades occurred.

So compression reshuffles *when* trades happen, not *whether* the endgame converges.

## What this tells us about the actual problem

The stall is not caused by "armies too far apart to engage." It's caused by a **post-trade equilibrium** where the remaining material cannot force a conclusion on a 12 × 8 board. Indicative pattern from the PGNs: decisive games end by ply ~200 (median 175-201), and the stalled games are games where 400+ plies are spent shuffling surviving pieces without progress.

Decisiveness is therefore an *endgame* problem, not an *opening tempo* problem.

## Where we actually stand across all v7 work

| attempt | what it was | verdict |
|---|---|---|
| v7 SPSA (piece value MG tuning) | 20 iterations, 100 games/eval, 200 pts | regressed to P%(dec) ~42% on confirm; bandwidth too small to signal |
| v7b Lurker mobility (`K`/`fN`/`WfN`) | 3 probes × 100 | **K solved balance** (52.1%/4.2%); fN/WfN badly overshot |
| v7c FEN compression (dist 5/4/3) | 3 probes × 100 | all failed balance; Unfin unmoved |

One meaningful improvement (balance solved) out of three tuning campaigns. The goal of shortening stalled-endgame games under natural rules has not moved at all.

## Why I want to pause before picking v7d

Repeated tuning attempts without improvement cost real time. I have been wrong twice in a row on priors (SPSA gradient, compression tempo). Before I guess at a v7d lever I should be more honest about what the data says vs. what I'm assuming.

### What the combined data actually supports

1. **Lurker mobility is the dominant balance lever** (4 measured points spanning 15-78% P%(dec)). It's not a decisiveness lever — every setting still produces 25-52% Unfin.
2. **Minion count/placement is a secondary balance lever** with moderate decisiveness effect (v6 minion-6 Unfin = 42%; v6 single-flank minions had Unfin 50-55%).
3. **FEN geometry (tested once, here)** is not a balance-preserving tempo lever — it's another balance lever, apparently stronger than Lurker per rank (14pp/rank).
4. **Piece MG values** (tested via SPSA) are a low-signal lever on balance. No evidence they affect decisiveness.

### What we have NOT tested

- Party piece *mobility* upgrades (Leader `WfF` → larger motion, Skirmisher `KAD` → something bigger, Defender `WcK` → something bigger). This is the closest analog to "give Party more closing power" that doesn't change balance by adding material.
- Horde Treasure motion (`mW`, very slow). A more mobile Treasure makes Horde harder to checkmate but also moves differently in stall conditions.
- Horde piece-count reductions (remove one Brute/Artillery). Party-favoring balance shift with unknown Unfin effect.
- Board geometry (obstacle layout, width). The obstacles help Party; adding or removing obstacles could change endgame topology dramatically.

### Candidate v7d levers, honestly priced

| lever | balance effect prior | decisiveness prior | confidence |
|---|---|---|---|
| Leader buff (`WfF` → `KAD` / `K` / `WfFD`) | Party-favoring, +8-20pp | potentially large Unfin drop (stronger king-analog finishes endgames) | low |
| Remove 1 Horde Artillery | Party-favoring, +10-15pp | moderate, fewer stalling pieces | medium |
| Remove 1 Horde Brute | Party-favoring, +10-15pp | moderate | medium |
| Treasure motion `mW` → `mK` | Horde-favoring, unknown magnitude | probably Unfin *up*, not down (more escape options) | medium |
| Remove obstacles | unknown, could break either way | unknown, probably Unfin down | low |
| Shrink board to 10 files | unknown, likely Party-favoring (easier corner) | likely Unfin down | low |

None of these have clean priors. Most are one-rank-at-a-time structural experiments like v7b/v7c were.

## Proposal for v7d — 2-probe minimum-commitment run

Rather than another 3-probe sweep guessing priors, I want to run **2 focused probes** where each directly tests a hypothesis and one of them has a good prior:

**Probe A: `v7d-reduce-horde-brute`** - Remove one Brute from the v7b-lurker-K starting FEN. Tests the "fewer Horde pieces = faster endgame convergence + shift balance back toward Party" hypothesis. Prior: P%(dec) 60-65% (Party overshoots), Unfin 25-35%. If Unfin drops substantially, we then look for a partial-strength counter-balance; if not, we abandon the "Horde count" lever.

**Probe B: `v7d-leader-upgrade-K`** - Upgrade Party Leader `WfF` → `K` (add back-diagonal + sideways). Leader becomes king-like. Tests whether Party needs a more powerful finishing piece to close endgames. Prior: P%(dec) 55-65% (Party-favoring), Unfin unknown.

Both probes on top of v7b `lurker-K` with stall rules off. 100 games × depth 8 × ~11 min each = ~25 min total. If *either* produces Unfin < 30% while keeping P%(dec) in [40, 60], we follow up with a balance-correction probe in v7e.

## Pause point

I have written no v7d ruleset JSONs yet. I want your read on this before we commit to another sweep:

1. **Are you OK with 2-probe focused experiments** instead of 3-way sweeps? I think the variance of a 100-game run is large enough that 2 probes produces nearly as much signal at lower cost.
2. **Of the candidate levers above, any priors I'm under- or over-weighting?** In particular I'm nervous the obstacle/geometry tests are too unstructured.
3. **Are we OK with "Party-favoring balance shift + decisiveness gain, then correct balance back"** as the v7d-v7e recipe? That's what the data suggests we should do — stop trying to preserve lurker-K's balance exactly and accept we'll need a second probe to correct.

If these aren't the right questions, say so and I'll rework.
