# Chess Position Analyser

A desktop chess board with a built-in engine. Set up a position by playing moves (or by typing a
FEN string), and the engine searches it with alpha-beta pruning and suggests the best move with
its score.

**Stack:** C# · .NET 9 · SplashKit (graphics and input) · xUnit

**Highlights**

- Full legal move generation, including castling, en passant and promotion, verified with
  perft against the standard reference positions (`PerftTests.cs`).
- Alpha-beta search with iterative deepening, MVV-LVA move ordering and quiescence search, up to
  depth 5. Detects checkmate (reported as "mate in N"), stalemate and the fifty-move rule.
- Two evaluators behind one interface: material only (*fast*) or material plus piece-square
  tables (*accurate*).
- Layered design: domain model, engine and UI are separate projects, and the engine can be
  tested without any graphics.

## Quick start

### What you need

- **.NET 9 SDK**: check with `dotnet --version`.
- **SplashKit**: install it for your operating system by following the guide at
  [splashkit.io](https://splashkit.io). The C# wrapper (`SplashKitSDK.dll`) is already in
  `src/Chess.UI.SplashKit/libs/`, but it needs the SplashKit library installed on your machine.

### Run

```bash
git clone https://github.com/<your-username>/<this-repo>.git
cd <this-repo>/src/Chess.UI.SplashKit
dotnet run
```

**On Windows**, SplashKit's library is built with MSYS2's UCRT64 toolchain. If
`C:\msys64\mingw64\bin` came first on `PATH`, Windows would load an incompatible `libstdc++` and
fail with `DllNotFoundException ... The specified procedure could not be found`. To avoid that, the
app puts `C:\msys64\ucrt64\bin` first on its own `PATH` at startup (`NativeLibrarySetup.cs`), so
plain `dotnet run` works. If MSYS2 is not in `C:\msys64`, set `MSYS2_ROOT` to its folder.

### Run the tests

From the repository root (the folder with `ChessAnalyser.sln`):

```bash
dotnet test
```

The tests cover board set-up and FEN, perft node counts on five reference positions, special
moves, evaluator symmetry, and search behaviour (finding mate in one, reporting checkmate and
stalemate, taking a hanging piece with either colour).

## How to use it

**Moving pieces.** Click a piece of the side to move to select it; its legal destination squares
are highlighted. Then click a destination, or drag the piece there. Right-click clears the
selection. Illegal moves are refused with a message in the status panel.

**Keyboard controls** (also listed in the panel on the right of the window):

| Key | Action |
|---|---|
| **A** | Analyse the current position: best move, score and nodes searched |
| **T** | Toggle auto-analyse after every move |
| **1**–**5** | Set the search depth (default 3) |
| **S** | Reset to the start position |
| **C** | Clear the board |
| **W** / **B** | Set the side to move: White / Black |
| **F** | Print the position as FEN to the terminal |
| **I** | Type a FEN string (hold Shift for White's uppercase pieces), then press Enter (Esc cancels) |
| **X** | Flip the board |
| **Esc** | Quit |

**Reading the analysis.** The panel shows the depth searched, the score in centipawns (100 =
one pawn) from the point of view of the side to move, or "mate in N" when a forced mate is found,
the best move (for example `e2→e4`) and the number of
positions searched.

**Try it:** play a few opening moves (for example `e2→e4`, then `e7→e5`), set depth 4 with **4**,
then press **A**. Press **F** to print the position as FEN in the terminal.

## How it works

```
Chess.UI.SplashKit   GameController (input, game loop) ─► BoardView (drawing)
        │
        ▼
Chess.Engine         EngineFactory ─► LegalMoveGenerator · HeuristicEvaluator / MaterialEvaluator
                                       · AlphaBetaSearch
        │
        ▼
Chess.Domain         Board · Move · MoveCommand · piece types
```

- **Model–view–controller.** `Board` (in `Chess.Domain`) holds the position, `BoardView` draws
  it, and `GameController` turns mouse and keyboard input into changes to the board.
- **Interfaces for the engine.** `IMoveGenerator`, `IEvaluator` and `ISearchEngine` mean the
  search does not depend on any particular evaluator, so evaluators can be swapped or tested on
  their own.
- **Factory.** `EngineFactory.Create("fast" | "accurate")` builds a matching set of generator,
  evaluator and search engine in one place.
- **Command.** Moves are applied through `MoveCommand`, so they can be undone, and the engine
  always analyses a clone of the board, never the one on screen.
- **Search.** `AlphaBetaSearch` deepens one ply at a time up to the chosen depth and prunes
  branches that cannot change the result. In *accurate* mode a quiescence search keeps going
  through captures at the end of a line, so the score is not taken in the middle of an exchange.
  Moves are searched captures-first (most valuable victim, least valuable attacker), and the best
  move from the previous depth is tried first.
- **Evaluation.** Scores are always from the side to move's point of view, as negamax expects.
  The *accurate* evaluator adds piece-square tables to material, so for example a knight in the
  centre is worth more than one on the rim.

## Project structure

```
ChessAnalyser.sln
src/
  Chess.Domain/          Board, Move, MoveCommand, piece types and board interfaces
  Chess.Engine/          move generators, evaluators, AlphaBetaSearch, EngineFactory
  Chess.UI.SplashKit/    Program.cs, GameController, BoardView, piece images, SplashKit wrapper
tests/
  Chess.Tests/           xUnit tests: perft, legality, special moves, evaluation, search
```

## Known limitations

- Depth is capped at 5. Deeper searches get slow because there is no transposition table and the
  move generator checks legality by making each move and scanning for the king.
- Draw by threefold repetition is not detected (it needs position hashing); stalemate and the
  fifty-move rule are.
- Promotion in the interface always chooses a queen.
- No clock, game history panel or undo button in the interface.
- FEN input cannot be pasted; it has to be typed.

## What I learned

<!-- TED: starting points drawn from the code. Rewrite them in your own words and keep only what
     you can explain in an interview. Delete this comment when done. -->

- **Object-oriented design on a real problem.** Interfaces, encapsulation and polymorphism stop
  being textbook terms when you need to swap an evaluator without touching the search.
- **Design patterns where they earn their place.** MVC to keep drawing apart from game rules,
  Factory to build engine configurations, and Command so moves can be undone safely.
- **Game-tree search.** Minimax with alpha-beta pruning, iterative deepening, and why a
  quiescence search is needed to avoid misleading scores.
- **Testing rules-heavy code.** Castling, en passant and promotion are easy to get subtly wrong,
  so they each have their own unit tests.

## Credits

<!-- TED: if this was a solo project, say so; otherwise name collaborators. -->

Built with [SplashKit](https://splashkit.io) for graphics and input.
