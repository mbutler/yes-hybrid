# The YES Hybrid

A tactical wargame layered on top of [Fairy-Stockfish](https://github.com/ianfab/Fairy-Stockfish). The engine enforces all rules, search, and AI; this repo provides the variant definition and a C# (.NET 8) terminal driver.

See [`# PROJECT SPEC: THE YES HYBRID.md`](./%23%20PROJECT%20SPEC%3A%20THE%20YES%20HYBRID.md) for the design intent.

## What's in here

```
variants/yeshybrid.ini       custom Fairy-Stockfish variant (12x8, 10 piece types, terrain, extinction win)
scripts/download-engine.sh   downloads a Fairy-Stockfish "largeboard" binary into ./engine/
src/YesHybrid.Engine/        UCI client + FEN/board model + ASCII renderer
src/YesHybrid.Cli/           `yes-hybrid` command-line driver
```

## Quick start

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

Moves are entered in long algebraic notation (e.g. `d1d3`). At each prompt, `yes-hybrid` asks the engine for the current legal-move list and shows the first 40.

### macOS (Apple Silicon)

Fairy-Stockfish does not ship a native arm64 binary. `download-engine.sh` will print build-from-source instructions; the short version is:

```bash
git clone https://github.com/ianfab/Fairy-Stockfish.git /tmp/fsf
cd /tmp/fsf/src
make -j build ARCH=apple-silicon largeboards=yes all=yes
cp stockfish ~/Desktop/yes-hybrid/engine/fairy-stockfish
```

## What v1 covers (and what it doesn't)

Implemented per the [agreed scope](./%23%20PROJECT%20SPEC%3A%20THE%20YES%20HYBRID.md):

- 12x8 board with two pillar holes and a short wall fragment (`*` squares).
- All 10 spec pieces (D, S, C, L, X, B, A, U, M, T) defined via Betza, with each spec-vs-engine translation documented inline in [`variants/yeshybrid.ini`](./variants/yeshybrid.ini).
- Asymmetric extinction win: Party loses if D/S/C/L/X are all captured; Horde loses if T is captured.
- Engine-vs-engine and human-vs-engine play loop with ASCII board.

**Deferred for v1** (per the clarifying questions, easy to layer on later):

- Section 6 hybrid mechanics: Multi-Capture (Bloodied promotion), Multi-Move per turn, Forced Movement.
- Section 7 SPSA tuning harness.
- 4e database mapping (we use the abstract D/S/C/L/X/B/A/U/M/T symbols only).

## Betza translation table

| Role        | Sym | Spec Betza  | Used Betza | Why                                                                        |
|-------------|-----|-------------|------------|----------------------------------------------------------------------------|
| Defender    | D   | `WcK`       | `WcK`      | Valid as-is.                                                               |
| Striker     | S   | `(mK2)cF`   | `mK2cF`    | Parens aren't Betza syntax.                                                |
| Controller  | C   | `gQ`        | `gQ`       | Valid as-is (grasshopper modifier).                                        |
| Leader      | L   | `WfF`       | `WfF`      | Valid as-is.                                                               |
| Skirmisher  | X   | `K2`        | `KAD`      | `K2` is a slider; spec wants hit-and-run, so use leapers covering radius 2. |
| Brute       | B   | `W2mK`      | `mDcK`     | "Leaps 2" = Dabbabah jump; "attacks adjacent" = capture-only K.             |
| Artillery   | A   | `R3cK`      | `mR3cK`    | Slide up to 3, capture adjacent only.                                      |
| Lurker      | U   | `p[N]`      | `N`        | `[]` not Betza; `pN` rejected. v1 uses plain knight; revisit later.         |
| Minion      | M   | `W1`        | `W`        | `W` already has range 1.                                                   |
| Treasure    | T   | `W1(m)`     | `mW`       | Move-only Wazir.                                                           |

> The Lurker letter is `U`/`u` instead of `K`/`k` because `k` is reserved for the royal piece in Fairy-Stockfish (this variant has no royal piece).

## Project layout

```
src/YesHybrid.Engine/
  Uci/UciEngine.cs        subprocess UCI client (handshake, position, go, legalmoves, quit)
  Game/Variant.cs         constants (variant name, default FEN, board size)
  Game/PieceCatalog.cs    in-process mirror of the variants.ini piece roster
  Game/Position.cs        FEN parser + extinction-victory evaluator
  Game/BoardRenderer.cs   ASCII renderer
src/YesHybrid.Cli/
  Program.cs              entry point + help text
  Commands/CommonOptions.cs   long-flag parser
  Commands/InfoCommand.cs     `yes-hybrid info`
  Commands/BestMoveCommand.cs `yes-hybrid bestmove`
  Commands/PlayCommand.cs     `yes-hybrid play`
```

## Next steps (suggested)

1. Run a quick smoke test: `yes-hybrid info` (no engine needed) → `yes-hybrid play --depth 4` → look for any FEN/Betza errors.
2. If the Skirmisher / Lurker translations don't feel right in play, iterate on the Betza in `variants/yeshybrid.ini`.
3. Add Multi-Capture (Bloodied promotion) by uncommenting and filling in the `promotedPieceType` hook at the bottom of the variant file, and adding the `Db`, `Sb`, etc. piece definitions.
4. Stand up the SPSA tuning harness (Section 7) by scripting repeated `bestmove` runs and writing PGN.
