# The YES Hybrid v1.1 Rules

The YES Hybrid is an asymmetric tactical board game for two players. One player controls the Party, a small band of heroes trying to capture the Treasure. The other controls the Horde, a larger defending force trying to wipe the Party out.

These rules describe the v1.1 tabletop game. The computer implementation in this repository uses Fairy-Stockfish to enforce the same board, pieces, and victory conditions.

## Components

- A 12 by 8 square board.
- Four Party pieces: Defender, Striker, Controller, Skirmisher.
- Ten Horde pieces: Treasure, four Brutes, one Artillery, two Lurkers, two Minions.
- Four blocked terrain squares.
- Optional counters for the quiet-move rule.

Use any tokens you like. The letters below match the engine notation.

## Board

Files run from `a` through `l`. Ranks run from `1` through `8`. The Party starts at the bottom on rank 1. The Horde starts near the top on ranks 7 and 8.

Blocked terrain squares cannot be entered, crossed by sliders, or captured:

- Pillars: `d5`, `i5`
- Wall: `e4`, `f4`

## Setup

Place the pieces as follows:

```text
    a  b  c  d  e  f  g  h  i  j  k  l
  +--------------------------------------+
8 | .  .  .  .  A  B  T  B  .  .  .  . |
7 | .  .  M  U  B  M  B  U  .  .  .  . |
6 | .  .  .  .  .  .  .  .  .  .  .  . |
5 | .  .  .  #  .  .  .  .  #  .  .  . |
4 | .  .  .  .  #  #  .  .  .  .  .  . |
3 | .  .  .  .  .  .  .  .  .  .  .  . |
2 | .  .  .  .  .  .  .  .  .  .  .  . |
1 | .  .  .  X  C  .  S  D  .  .  .  . |
  +--------------------------------------+
```

White / Party moves first.

## Goal

The Party wins by checkmating or capturing the Treasure.

The Horde wins if all Party pieces are removed from the board, or if the Party has no legal move.

If neither side can make progress, the game can also end by the quiet-move rule below.

## Turn Structure

Players alternate turns. On your turn, move one of your pieces according to its movement rules.

Most pieces have different rules for moving to an empty square and capturing an enemy piece. A piece may only capture onto a square occupied by an enemy piece. A piece may not move onto a friendly piece or blocked terrain.

## Quiet-Move Rule

The YES Hybrid v1.1 uses the standard chess-style stall rules:

- If 50 full moves pass with no capture, the game is a draw.
- If the same position repeats three times, the game is a draw.

For tabletop play, use a simple counter. Reset it to 0 after any capture. After every pair of turns with no capture, increase it by 1. If it reaches 50, the game is drawn.

## Piece Movement

Movement words:

- Orthogonal means up, down, left, or right.
- Diagonal means along a diagonal.
- Adjacent means one square away.
- Leap means jump directly to the destination; intervening squares do not matter.
- Slide means move any allowed distance along a line, but not through occupied or blocked squares.

### Party Pieces

**Defender (`D`)**

- Move: one square orthogonally.
- Capture: one square in any direction.
- Role: tough front-line guard.

**Striker (`S`)**

- Move: one or two squares in any direction.
- Capture: one square diagonally.
- Role: mobile attacker.

**Controller (`C`)**

- Move or capture: grasshopper queen.
- Choose an orthogonal or diagonal line. There must be exactly one occupied square somewhere along that line. The Controller jumps over that occupied square and lands on the first square immediately beyond it.
- If the landing square is empty, this is a move. If it contains an enemy piece, this is a capture. If it contains a friendly piece or blocked terrain, the move is illegal.
- Role: tactical piece that uses other pieces as launch points.

**Skirmisher (`X`)**

- Move or capture: one square in any direction, or leap exactly two squares orthogonally or diagonally.
- Role: hit-and-run piece with broad local coverage.

### Horde Pieces

**Treasure (`T`)**

- Move: one square orthogonally.
- Capture: cannot capture.
- Role: objective piece. If the Treasure is checkmated or captured, the Party wins.

**Brute (`B`)**

- Move: leap exactly two squares orthogonally.
- Capture: one square in any direction.
- Role: roadblock and close-range threat.

**Artillery (`A`)**

- Move: slide up to three squares orthogonally.
- Capture: one square in any direction.
- Role: mobile defender that repositions along lanes.

**Lurker (`U`)**

- Move or capture: one square in any direction.
- Role: flexible close guard. Earlier versions used a knight-like Lurker; v1.1 uses this simpler balanced form.

**Minion (`M`)**

- Move: one square orthogonally.
- Capture: one square diagonally forward toward the Party side.
- Role: basic Horde body.

## Tabletop Notes

The engine treats the Treasure as a royal piece, so "capturing the Treasure" is often represented as checkmate: the Treasure is under unavoidable attack and has no legal escape. For tabletop play, use the same idea. If the Treasure is attacked and the Horde cannot make any legal move that leaves it safe, the Party wins.

The game is asymmetric by design. The Party has fewer, more specialized pieces. The Horde has more bodies and wins by eliminating the Party.

## v1.1 Balance Result

The final v1.1 rules were validated with a 500-game engine self-play confirmation at depth 8:

```text
Party wins  : 216
Horde wins  : 233
Unfinished  :  51
P%(decisive): 48.1%
```

That is close enough to even for the current design goal. The game is considered finished at v1.1 unless human playtesting reveals a major problem.
