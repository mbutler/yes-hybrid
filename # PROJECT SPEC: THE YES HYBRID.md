# PROJECT SPEC: THE YES HYBRID
## A Tactical Wargame & Chess Engine Synthesis

### 1. Overview
The **YES Hybrid** is a deterministic, objective-based tactical board game. It uses **Fairy-Stockfish** as the logic/AI core to enforce rules and provide high-level tactical opposition. The goal is to merge the role-based tactical depth of D&D 4th Edition with the rigorous, perfect-information state-space of Chess.

### 2. Core Architecture
- **Engine:** Fairy-Stockfish (multivariant-compatible).
- **Interface:** Universal Chess Interface (UCI).
- **State Representation:** Modified FEN (Forsyth-Edwards Notation).
- **Configuration:** `variants.ini` file for custom piece and board logic.

### 3. Board Geometry
- **Dimensions:** 12x8 (Standard) up to 14x14.
- **Orientation:** Vertical progression. Side A (Party) at bottom; Side B (Horde) at top.
- **Static Terrain:** - `holeSquares`: Impassable squares (Pillars, Pits).
    - `wall`: Pieces that cannot be moved or captured, acting as barriers.

### 4. Victory Conditions (Asymmetric)
The game does not use Checkmate. It uses the `extinction` win condition logic.
- **The Party (White):** - **Goal:** Capture the "Treasure" piece.
    - **Failure:** All 5 Party pieces are removed from the board.
- **The Horde (Black):** - **Goal:** Capture all 5 Party pieces.
    - **Failure:** The "Treasure" piece is captured.

### 5. Piece Abstractions (Role-Based)
All pieces are defined via **Betza Notation** to separate movement from attacking.

#### Party Archetypes (Side A)
| Role | Symbol | Betza Logic | Description |
| :--- | :--- | :--- | :--- |
| **Defender** | D | `WcK` | Moves 1 sq orth; attacks 1 sq any. High durability. |
| **Striker** | S | `(mK2)cF` | Moves 2 sq; attacks diagonally only. High mobility. |
| **Controller** | C | `gQ` | Grasshopper logic. Jumps over hurdles to move/attack. |
| **Leader** | L | `WfF` | Buff piece. Mediocre stats, high strategic value. |
| **Skirmisher** | X | `K2` | Moves/Attacks in 2 sq radius. Hit-and-run. |

#### Horde Archetypes (Side B)
| Role | Symbol | Betza Logic | Description |
| :--- | :--- | :--- | :--- |
| **Brute** | B | `W2mK` | Leaps 2 sq to move; attacks adjacent. The roadblock. |
| **Artillery** | A | `R3cK` | Slides 3 sq; attacks adjacent only. The sniper. |
| **Lurker** | K | `p[N]` | Teleport/Leap attack. Threatens backlines. |
| **Minion** | M | `W1` | Basic fodder. Material Value = 10. |
| **Treasure** | T | `W1(m)` | Passive goal. Moves but cannot attack. |

### 6. Hybrid Mechanics
- **Multi-Capture (Durability):** Using the `promote` and `gating` flags. High-value pieces do not die on first capture; they promote to a "Bloodied" version of themselves (lower value/speed).
- **Multi-Move:** The game can be configured for `movesPerTurn > 1` to simulate a "Round" of combat.
- **Forced Movement:** Simulated via Gating. If Piece A captures Piece B, Piece B is re-spawned (slid) to an adjacent square instead of being removed.

### 7. Playtesting & Balancing Protocol
- **Brute Force Simulation:** Run 100,000 self-play games at Depth 10.
- **SPSA Tuning:** Automated adjustment of `pieceValueMg` and `pieceValueEg` to ensure the win/loss ratio for Party vs. Horde stays within 45%-55%.
- **Strategic Depth Check:** Monitor the "Average Depth" required for the engine to find winning lines. If Depth is too low, increase piece complexity.

### 8. Implementation Steps for Cursor
1. Define the `[TheYesHybrid]` entry in `variants.ini`.
2. Map 4e database entities to the defined symbols (D, S, C, etc.).
3. Generate the starting FEN based on the combat encounter.
4. Call `fairy-stockfish` and parse the best move.