# Chess Position Analyser

A desktop chess board with a built-in engine. Set up a position by playing moves (or by typing a
FEN string), and the engine searches it with alpha-beta pruning and suggests the best move with
its score.

**Stack:** C# · .NET 9 · SplashKit (graphics and input) · xUnit

**Highlights**

- Full legal move generation, including special moves (covered by `SpecialMovesTests.cs`).
- Alpha-beta search with iterative deepening and quiescence search, up to depth 5.
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

### Run the tests

From the repository root (the folder with `ChessAnalyser.sln`):

```bash
dotnet test
```

The tests cover board set-up and FEN, move legality, special moves, and a quick search to check
the engine returns a move.

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
| **I** | Type a FEN string, then press Enter (Esc cancels); see the limitation below |
| **X** | Flip the board |
| **Esc** | Quit |

**Reading the analysis.** The panel shows the depth searched, the score in centipawns (100 =
one pawn), the best move (for example `e2→e4`) and the number of
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

## Project structure

```
ChessAnalyser.sln
src/
  Chess.Domain/          Board, Move, MoveCommand, piece types and board interfaces
  Chess.Engine/          move generators, evaluators, AlphaBetaSearch, EngineFactory
  Chess.UI.SplashKit/    Program.cs, GameController, BoardView, piece images, SplashKit wrapper
tests/
  Chess.Tests/           xUnit tests: board and engine, legality, special moves, search
```

## Known limitations

- Depth is capped at 5, and deeper searches get slow because there is no transposition table and
  move ordering is minimal.
- No clock, game history panel or undo button in the interface.
- FEN input from the keyboard only types lowercase letters, digits, `/`, `-` and spaces, so
  positions with White pieces (uppercase in FEN) cannot be entered this way yet, and pasting is not
  supported.

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
