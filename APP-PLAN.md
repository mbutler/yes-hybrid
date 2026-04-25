# Standalone App Plan

This document is the starting point for a future playable standalone version of the tuned v1.1 game. The app should preserve the finished rules exactly and add only presentation, usability, packaging, and player-facing naming.

## Core Principle

Do not change the tuned game while building the app.

The following must remain the source of truth:

- Rules and movement: `variants/yeshybrid.ini`
- Internal symbols: `D S C X B A U M T`
- Starting FEN: `4abtb4/2mubmbu4/12/3*4*3/4**6/12/12/3XC1SD4 w - - 0 1`
- Engine variant id: `theyeshybrid`
- Validation ruleset: `rulesets/v1.1-baseline.json`

Player-facing names, icons, colors, art, sound, and app branding can change freely as long as the internal symbols and Betza definitions do not.

## Player-Facing Naming

Use `piece-mapping.md` as the presentation mapping for the playable app:

| Symbol | Internal Role | Player Name |
|---|---|---|
| `D` | Defender | Guard |
| `S` | Striker | Scout |
| `C` | Controller | Mage |
| `X` | Skirmisher | Rogue |
| `T` | Treasure | Trove |
| `B` | Brute | Wraith |
| `A` | Artillery | Brute |
| `U` | Lurker | Fiend |
| `M` | Minion | Spawn |
| `#` / `*` | Blocked terrain | Blocked |

The UI should show player names, while debug panels, saved games, logs, and engine calls may continue to use symbols.

## Recommended Repo Shape

Keep the playable app in this repository beside the existing engine and CLI:

```text
src/YesHybrid.Engine/      shared engine/rules/FEN/UCI library
src/YesHybrid.Cli/         developer CLI, testing, matches, tuning
src/YesHybrid.Desktop/     future standalone app
variants/                  shipped Fairy-Stockfish variant files
RULES.md                   tabletop rules
piece-mapping.md           player-facing names
APP-PLAN.md                app architecture plan
reports/                   validation archive
rulesets/                  validation and experiment configs
```

The public app can have a new product name without renaming `YesHybrid.Engine` or the repo immediately.

## Suggested Technology

Use Avalonia for the first desktop app.

Reasons:

- Cross-platform desktop UI.
- Native .NET, so it can reference `YesHybrid.Engine` directly.
- No server required.
- Fairy-Stockfish can run as a local bundled process.

## MVP Scope

The first playable version should be intentionally small:

- 12x8 clickable board.
- Render blocked terrain and pieces.
- Use player-facing piece names from `piece-mapping.md`.
- Highlight legal moves for the selected piece.
- Human vs engine, Party side first.
- Optional human vs human local mode.
- Show move history.
- Show result: Party win, Horde win, draw, or unfinished.
- Show a rules/help panel sourced from `RULES.md`.

Do not include online multiplayer, accounts, matchmaking, animations, or rule editing in the MVP.

## Engine Integration

The app should reuse the existing UCI/Fairy-Stockfish integration:

- Start bundled Fairy-Stockfish.
- Set `VariantPath` to the bundled `variants/yeshybrid.ini`.
- Set `UCI_Variant` to `theyeshybrid`.
- Use current FEN as app state.
- Ask the engine for legal moves with `go perft 1`.
- Apply a player move by sending `position fen <fen> moves <move>`.
- Read back the resulting FEN from `d`.
- Ask the engine for AI moves with `go depth N`.

The current CLI `play` loop proves the flow works; the app should extract the "one move at a time" pieces into a reusable session service rather than copy terminal UI code.

## App Services To Add Later

Likely useful shared app-layer services:

- `GameSession`: owns current FEN, move history, side to move, result state.
- `LegalMoveService`: maps selected square to legal destination squares.
- `EngineOpponent`: chooses engine moves at a selected difficulty.
- `PiecePresentation`: maps internal symbols to display names/icons/colors.
- `RulesTextProvider`: loads `RULES.md` for the help view.
- `SaveGameService`: stores FEN plus move history.

These can live in the desktop project first. Move them into a shared app library only if another frontend appears.

## Difficulty Levels

Initial engine difficulty can be depth-based:

- Easy: depth 2-4, optional random choice among legal moves.
- Normal: depth 6.
- Hard: depth 8.
- Expert: depth 10+.

Do not tune rules or piece values per difficulty. Difficulty should affect engine strength only.

## Packaging Notes

The app will need bundled Fairy-Stockfish binaries per platform:

- macOS arm64
- macOS x64 if desired
- Windows x64
- Linux x64

The app should fail gracefully if the engine binary is missing and show a clear setup message.

## Non-Goals

- Do not rename internal symbols.
- Do not change Betza definitions.
- Do not change starting position.
- Do not add alternate rulesets to the player app.
- Do not expose tuning/sweep commands in the player UI.
- Do not build online multiplayer before local play is solid.

## First Implementation Step

When ready to start building, create `src/YesHybrid.Desktop/` as an Avalonia app that references `src/YesHybrid.Engine/`.

The first milestone is not a polished game. It is a board that can:

1. Load the v1.1 start position.
2. Select a piece.
3. Show legal destinations.
4. Apply one human move.
5. Ask Fairy-Stockfish for one engine reply.
6. Render the updated board.
