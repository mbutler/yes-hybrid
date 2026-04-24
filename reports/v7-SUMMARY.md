# v7 campaign summary — the Yes Hybrid v1.1 final

## Final: `variants/yeshybrid.ini` v1.1

Confirmed at 500 games, depth 8, with chess-style stall rules active:

```
Party wins  :  216  ( 43.2%)
Horde wins  :  233  ( 46.6%)
Unfinished  :   51  ( 10.2%)
P%(dec)     :  48.1%   95% CI [43.5, 52.7]
Imbalance   :   3.8%
Median plies:  199
Composite   :  0.864
```

**Balance is strong.** Party won 48.1% of decisive games across a 500-game confirmation, and the 95% confidence interval comfortably includes 50%. For a non-symmetric chess variant to land here without rating adjustment is a strong result.

**Decisiveness is acceptable for v1.1.** The final design intentionally keeps chess-style stall rules: 50 full moves without a capture is a draw, and threefold repetition is a draw. With those rules active, the final unfinished rate measured 10.2%. A no-stall reference run remains archived because it showed the natural engine-vs-engine stall tail is much higher.

### The configuration

On top of the `variants/yeshybrid.ini` as it stood after v6:

1. `customPiece8 = u:K` (Lurker = Wazir + Ferz, one-step 8-direction)
   - was: `u:N` (plain Knight)
2. starting FEN removes Artillery at i8 and Leader at f1:
   - rank 8: `4abtb4` (was `4abtba3`)
   - rank 1: `3XC1SD4` (was `3XCLSD4`)
3. `nMoveRule = 50` and `nFoldRule = 3`
   - stall rules are canonical v1.1 gameplay rules

### Starting position

```
    a  b  c  d  e  f  g  t  h  i  j  k  l
  +--------------------------------------+
8 | .  .  .  .  a  b  t  b  .  .  .  .  |   Horde back
7 | .  .  m  u  b  m  b  u  .  .  .  .  |   Horde middle
6 | .  .  .  .  .  .  .  .  .  .  .  .  |
5 | .  .  .  #  .  .  .  .  #  .  .  .  |   pillars
4 | .  .  .  .  #  #  .  .  .  .  .  .  |   wall fragment
3 | .  .  .  .  .  .  .  .  .  .  .  .  |
2 | .  .  .  .  .  .  .  .  .  .  .  .  |
1 | .  .  .  X  C  .  S  D  .  .  .  .  |   Party
  +--------------------------------------+
```

4 Party pieces (X, C, S, D) face 10 Horde pieces (1 Artillery, 4 Brute, 2 Lurker, 2 Minion, 1 Treasure).

---

## The 8-campaign journey

Total: ~12 hours of experiment wall time. 28 unique rulesets tested.

### Progress chart

| campaign | what we tested | result |
|---|---|---|
| v7 | SPSA tuning on piece values MG (20 iters × 100 games) | failed — 300-game confirm regressed to 41.7% P%; piece values are noise on this objective |
| v7b | Lurker mobility bracket (N, fN, K, WfN) | ✅ **balance solved at `u:K`** (52.1% P%, 4.2% Imbal) — but Unfin 52% |
| v7c | FEN compression (distance 3/4/5) | failed — every rank of compression moved P% ~14pp toward Horde |
| v7d | Remove Horde Artillery & Leader-K upgrade | ✅ **Unfin breakthrough** at 30% (Party 70% overshoot); Leader-K showed Party buffs backfire |
| v7e | Add Horde Minion back (a7 and j7) | failed — added material restored stall geometry |
| v7f | Remove Party piece (Leader vs Skirmisher) | ✅ **balance solved in no-stall reference** (49.7%/0.6%) but Unfin remained high |
| v7g | Brute capture ablation (accidental, `mK`) | broke balance to 99% Party but Unfin crashed to 7% — showed Brute captures are the dominant Unfin lever |
| v7h | Brute mobility weak + count reduction | mobility change neutral; count -1 drove Party to 76% |
| v7i | Partial Brute captures (diag, orthog) | failed — non-linear response, overshot balance at 4-dir captures |

### The mechanism we converged on

**Balance and decisiveness are coupled through Brute capture coverage.** Any reduction of Horde's offensive capture force reduces stall equilibria (good for Unfin) but disproportionately shifts balance toward Party. The response isn't linear — going from 8-dir Brute captures to 4-dir swings P% by 30-40pp while reducing Unfin only 5-25pp. This makes piece-level tuning a dead end for simultaneously improving both axes.

The v7f-no-leader configuration represents the **unique point** in the piece-space where balance is near-perfect while retaining enough Horde material to keep games from becoming one-sided. The final v1.1 design accepts chess-style stall rules as the cleanest way to handle rare non-progress positions without reopening piece balance.

The late investigation showed that omitting `nMoveRule` / `nFoldRule` from `[theyeshybrid:chess]` inherits chess defaults rather than disabling them. v1.1 now makes those rules explicit.

### Priors vs actuals

A sobering thread across the campaigns: my priors were wrong in 6 of 8 sweeps, often badly (off by 20-40pp). The underlying reason is that piece-level priors derived from extrapolating single-lever v6 results don't stack linearly when multiple levers are combined. Every campaign after v7d required re-measurement. This is why bracketing pairs (2 probes that straddle the predicted target) were more useful than single focused probes.

---

## Why v2 is not needed now

The original v2 motivation was to reduce the no-stall unfinished rate without breaking balance. A first 10x8 board probe (`v2a`) was tested and failed badly: it favored Horde and did not reduce stalls enough. Since the final v1.1 design keeps chess-style stall rules and the 500-game confirmation is both balanced and acceptably decisive, no v2 structural change is currently recommended.

### Structural levers archived for future exploration

- **Board dimensions.** Yes Hybrid is 12 × 8. A 10-wide or 10 × 8 board would make cornering the Treasure dramatically easier and should collapse the Unfin tail. Requires a `maxFile = 10` change and a redesigned startFen.
- **Obstacle layout.** Pillars at d5/i5 and walls at e4/f4 currently help Party defense. Rearranging or reducing them could accelerate middle-game contact and reduce post-trade stalls.
- **Treasure mobility.** Currently `mW` (4 non-capturing orthogonal moves). Any change here would break v1.1's balance, but a testable structural axis.
- **Piece motion redesign.** Particularly Controller `gQ` (grasshopper queen) which can behave stall-happy in endgames; a less-escape-prone finishing piece would help.

---

## Artifacts

All v7 ruleset JSONs live in `rulesets/v7*.json`. Per-campaign reports:
- `reports/v7-overnight/` — SPSA
- `reports/v7b-lurker-pilot/` — Lurker mobility
- `reports/v7c-compress-pilot/` — FEN compression
- `reports/v7d-endgame-pilot/` — first decisiveness win
- `reports/v7e-bracket-pilot/` — balance correction attempts
- `reports/v7f-party-cut-pilot/` — **ship candidate confirmed here**
- `reports/v7g-brute-weaken/` — accidental no-captures
- `reports/v7h-brute-lever/` — partial Brute weakening
- `reports/v7i-brute-capture/` — partial capture interpolation
- `reports/v1.1-stall-rule-ab/` — proof that explicit `nMoveRule = 0` / `nFoldRule = 0` caused the old high-Unfin discrepancy
- `reports/v1.1-500-confirm/` — final v1.1 confirmation

The final shipped configuration is `variants/yeshybrid.ini` plus `rulesets/v1.1-baseline.json`.
