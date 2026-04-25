# The YES Hybrid

A finished v1.1 tactical board game layered on top of [Fairy-Stockfish](https://github.com/ianfab/Fairy-Stockfish). The goal is a chesslike, perfect-information tactics game with some of the role flavor of D&D combat.

For tabletop play, start with [`RULES.md`](./RULES.md). For the original design intent, see [`# PROJECT SPEC: THE YES HYBRID.md`](./%23%20PROJECT%20SPEC%3A%20THE%20YES%20HYBRID.md).

## v1.1 Status

v1.1 is the current final design. Testing is complete unless human playtesting reveals a major issue.

Final 500-game depth-8 self-play confirmation:

```text
Party wins  : 216  (43.2%)
Horde wins  : 233  (46.6%)
Unfinished  :  51  (10.2%)
P%(decisive): 48.1%   95% CI [43.5, 52.7]
Imbalance   : 3.8%
Median plies: 199
```

The design keeps chess-style stall rules as gameplay rules: 50 full moves without a capture is a draw, and threefold repetition is a draw.

## What's In Here

```text
RULES.md                    human-readable over-the-board rules
variants/yeshybrid.ini       custom Fairy-Stockfish variant (12x8, terrain, asymmetric goals)
rulesets/v1.1-baseline.json  final v1.1 validation ruleset
reports/v1.1-500-confirm/    final 500-game confirmation
scripts/download-engine.sh   downloads/builds Fairy-Stockfish into ./engine/
src/YesHybrid.Engine/        UCI client + FEN/board model + ASCII renderer
src/YesHybrid.Cli/           `yes-hybrid` command-line driver
src/YesHybrid.Desktop/       Avalonia UI (board, move list, engine modes)
```

## Quick Start

```bash
# 1. Get the engine (one-time)
./scripts/download-engine.sh

# 2. Build
dotnet build

# 3. Inspect the variant (no engine needed for this)
dotnet run --project src/YesHybrid.Cli -- info

# 4. Watch the engine play itself
dotnet run --project src/YesHybrid.Cli -- play --depth 8

# 5. Play against the engine as the Party (White)
dotnet run --project src/YesHybrid.Cli -- play --mode human-vs-engine --side white --depth 10
```

Moves are entered in long algebraic notation, for example `d1d3`. At each prompt, `yes-hybrid` asks the engine for the current legal-move list and shows the first 40.

## Running the desktop app

The GUI lives in `src/YesHybrid.Desktop`. It needs the same Fairy-Stockfish binary and `variants/yeshybrid.ini` as the CLI, so run `./scripts/download-engine.sh` first if you have not already.

```bash
dotnet run --project src/YesHybrid.Desktop
```

Run that from the **repository root** (or anywhere under it): the app looks for `engine/fairy-stockfish` and `variants/yeshybrid.ini` by walking up from the process directory, so a normal `dotnet run` from the root Just Works. The **Rules…** command expects `RULES.md` at the repo root for the best experience.

**Modes (dropdown):** Human vs engine (choose Party or Horde), human vs human on the same device, or engine vs engine at a watchable speed. Use **New game** to reset.

### macOS Apple Silicon

Fairy-Stockfish does not ship a native arm64 binary. `download-engine.sh` will print build-from-source instructions; the short version is:

```bash
git clone https://github.com/ianfab/Fairy-Stockfish.git /tmp/fsf
cd /tmp/fsf/src
make -j build ARCH=apple-silicon largeboards=yes all=yes
cp stockfish /path/to/yes-hybrid/engine/fairy-stockfish
```

## Game Summary

- Board: 12x8, files `a` through `l`, ranks `1` through `8`.
- Party / White: Defender, Striker, Controller, Skirmisher.
- Horde / Black: Treasure, four Brutes, one Artillery, two Lurkers, two Minions.
- Terrain: blocked squares at `d5`, `i5`, `e4`, and `f4`.
- Party wins by checkmating or capturing the Treasure.
- Horde wins by eliminating all Party pieces or leaving Party with no legal move.
- Draws use the chess-style quiet-move and repetition rules.

## Betza Translation Table

| Role | Sym | Used Betza | Notes |
|------|-----|------------|-------|
| Defender | D | `WcK` | Moves orthogonally, captures adjacent. |
| Striker | S | `mK2cF` | Mobile mover, diagonal-only capture. |
| Controller | C | `gQ` | Grasshopper queen. |
| Skirmisher | X | `KAD` | Adjacent plus radius-2 leaper coverage. |
| Brute | B | `mDcK` | Dabbabah movement, adjacent captures. |
| Artillery | A | `mR3cK` | Orthogonal movement up to 3, adjacent captures. |
| Lurker | U | `K` | v1.1 balanced form: one square any direction. |
| Minion | M | `WfcF` | Orthogonal mover, forward-diagonal capture. |
| Treasure | T | `mW` | Royal objective piece, move-only Wazir. |

The original spec also included the Party Leader (`L`). v1.1 removes the Leader from the starting setup for balance, though the engine definition remains in the INI for experimentation.

## CLI Commands

```text
info       print variant information
play       run a single game in the terminal
batch      run repeated self-play games to PGN
match      evaluate one ruleset over randomized openings
sweep      evaluate multiple rulesets and rank them
tune       SPSA-tune piece values for experiments
bestmove   ask the engine for one move
```

## Deferred Ideas

These are not part of v1.1:

- Multi-Capture / Bloodied pieces.
- Multi-move turns.
- Forced movement or push effects.
- 4e database mapping.

They are preserved as design hooks, but v1.1 is considered complete without them.
