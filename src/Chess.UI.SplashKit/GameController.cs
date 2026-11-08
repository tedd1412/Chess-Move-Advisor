using System;
using System.Collections.Generic;
using Chess.Domain;
using Chess.Engine;
using SplashKitSDK;

using SK = SplashKitSDK.SplashKit;
using DomainColor = Chess.Domain.Color;

namespace Chess.UI.SplashKit
{
    /// <summary>
    /// GameController — part of MVC architecture (Controller).
    /// Handles user input, updates model (Board), triggers engine analysis,
    /// and communicates results to the View (BoardView).
    /// </summary>
    public sealed class GameController
    {
        // Model (Domain Layer)
        private readonly Board _board = new();

        // Engine Components (created via Factory pattern)
        private readonly IMoveGenerator _generator;
        private readonly IEvaluator _evaluator;
        private readonly ISearchEngine _engine;

        // View (UI Layer)
        private readonly BoardView _view = new();
        private Window _window = null!;

        // UI State
        private SearchResult? _lastSearch;
        private string _status = "Click a piece to select. Drag or click to move.";
        private DomainColor _sideToMove = DomainColor.White;
        private int _searchDepth = 3;

        // Selection & dragging
        private int? _selectedSquare = null;
        private readonly List<Move> _legalFromSelected = new();
        private bool _isDragging = false;
        private bool _flipped = false; // board orientation
        private Piece? _dragPiece = null;
        private int? _dragFromSquare = null;

        // Options
        private bool _autoAnalyse = false;

        /// <summary>
        /// Constructor — uses Factory Pattern to build engine components.
        /// Factory centralizes object creation and supports easy configuration changes.
        /// </summary>
        public GameController(string engineMode = "accurate")
        {
            _generator = new SimpleMoveGenerator();     // temporarily use working generator
            (_generator , _evaluator, _engine) = EngineFactory.Create(engineMode);
        }

        /// <summary>
        /// Main game loop — event-driven (polls SplashKit events).
        /// </summary>
        public void Run()
        {
            _window = new Window(
                "Chess Position Analyser",
                BoardView.BoardX + 8 * BoardView.Tile + BoardView.Margin + BoardView.PanelW + BoardView.Margin,
                BoardView.BoardY + 8 * BoardView.Tile + BoardView.Margin
            );

            _board.LoadStartPosition(); // initial position

            while (!SK.WindowCloseRequested(_window))
            {
                SK.ProcessEvents();
                HandleInput();

                // Render highlights and dragging overlay
                IReadOnlyList<int>? targets = null;
                if (_selectedSquare is int && _legalFromSelected.Count > 0)
                {
                    var list = new List<int>(_legalFromSelected.Count);
                    foreach (var m in _legalFromSelected) list.Add(m.To);
                    targets = list;
                }

                (Piece piece, float x, float y)? draggingOverlay = null;
                if (_isDragging && _dragPiece is { } dp)
                    draggingOverlay = (dp, SK.MouseX(), SK.MouseY());

                _view.Draw(
                    _window, _board, _lastSearch, _status, _sideToMove,
                    _selectedSquare, targets, _dragFromSquare, draggingOverlay, _flipped
                );
            }

            _window.Close();
        }

        // Input handling

        private void HandleInput()
        {
            // Left click: select or start dragging
            if (SK.MouseDown(MouseButton.LeftButton) && !_isDragging)
            {
                var (sq, valid) = BoardView.SquareFromMouse(_flipped);
                if (valid)
                {
                    var piece = _board.GetPieceAt(sq);
                    if (piece is { } p && p.Color == _sideToMove)
                    {
                        SelectSquare(sq);
                        _isDragging = true;
                        _dragPiece = p;
                        _dragFromSquare = sq;
                    }
                    else if (_selectedSquare is int sel)
                    {
                        TryMove(sel, sq);
                    }
                    else ClearSelection();
                }
            }

            // Mouse release: drop piece if dragging
            if (SK.MouseUp(MouseButton.LeftButton))
            {
                if (_isDragging && _selectedSquare is int sel)
                {
                    var (sq, valid) = BoardView.SquareFromMouse();
                    if (valid) TryMove(sel, sq);
                }
                _isDragging = false;
                _dragPiece = null;
                _dragFromSquare = null;
            }

            // Right click: clear
            if (SK.MouseClicked(MouseButton.RightButton)) ClearSelection();

            // Keyboard shortcuts
            HandleKeys();
        }

        private void HandleKeys()
        {
            // Board controls
            // Swap board orientation (for analysis or player perspective)
            if (SK.KeyTyped(KeyCode.XKey))
            {
                _flipped = !_flipped;
                _status = _flipped ? "View flipped: Black bottom." : "View flipped: White bottom.";
            }

            if (SK.KeyTyped(KeyCode.SKey)) { _board.LoadStartPosition(); Reset(); _status = "Start position."; }
            if (SK.KeyTyped(KeyCode.CKey)) { _board.Clear(); Reset(); _status = "Board cleared."; }

            // Side toggle
            if (SK.KeyTyped(KeyCode.WKey))
            {
                _sideToMove = DomainColor.White; 
                _board.SetSideToMove(DomainColor.White);
                _status = "Side to move: White.";
            }
            if (SK.KeyTyped(KeyCode.BKey))
            {
                _sideToMove = DomainColor.Black;
                _board.SetSideToMove(DomainColor.Black);
                _status = "Side to move: Black.";
            }

            // Search depth
            for (int i = 1; i <= 5; i++)
            {
                if (SK.KeyTyped(KeyCode.Num0Key + i))
                {
                    _searchDepth = i;
                    _status = $"Depth set to {i}.";
                }
            }

            // Toggle auto-analysis
            if (SK.KeyTyped(KeyCode.TKey))
            {
                _autoAnalyse = !_autoAnalyse;
                _status = $"Auto-analyse: {(_autoAnalyse ? "ON" : "OFF")}.";
            }

            // Manual analysis
            if (SK.KeyTyped(KeyCode.AKey))
            {
                if (_isDragging) { _status = "Finish the move before analysing."; return; }

                var clone = _board.Clone(); // Command pattern ensures clone safety
                var res = _engine.Analyse(clone, _searchDepth);
                _lastSearch = res;
                _status += $" | Best move: {Sq(res.BestMove.From)}→{Sq(res.BestMove.To)} ({(res.ScoreCp >= 0 ? "+" : "")}{res.ScoreCp}).";
            }

            // FEN utilities
            if (SK.KeyTyped(KeyCode.FKey)) { Console.WriteLine($"FEN: {_board.ToFen()}"); _status = "FEN printed."; }
            if (SK.KeyTyped(KeyCode.IKey)) { LoadFenFromInput(); }

            // Exit
            if (SK.KeyTyped(KeyCode.EscapeKey)) Environment.Exit(0);
        }

        // Move handling

        private void SelectSquare(int sq)
        {
            _selectedSquare = sq;
            _legalFromSelected.Clear();

            Span<Move> buf = stackalloc Move[256];
            int n = _generator.GenerateMoves(_board, buf);
            for (int i = 0; i < n; i++)
                if (buf[i].From == sq)
                    _legalFromSelected.Add(buf[i]);

            _status = _legalFromSelected.Count == 0
                ? $"No legal moves from {Sq(sq)}."
                : $"Selected {Sq(sq)} ({_legalFromSelected.Count} moves).";
        }

        private bool TryMove(int from, int to)
        {
            Move? chosen = null;

            foreach (var m in _legalFromSelected)
            {
                if (m.From == from && m.To == to)
                {
                    chosen = chosen == null || m.Promotion == PieceType.Queen ? m : chosen;
                }
            }

            if (chosen is not Move move)
            {
                _status = "Illegal move.";
                return false;
            }

            _board.MakeMove(move);
            _status = $"Moved {Sq(move.From)}→{Sq(move.To)}.";
            ClearSelection();
            _sideToMove = _board.SideToMove;

            if (_autoAnalyse)
            {
                var clone = _board.Clone();
                var res = _engine.Analyse(clone, _searchDepth);
                _lastSearch = res;
                _status += $" Best reply: {Sq(res.BestMove.From)}→{Sq(res.BestMove.To)} ({(res.ScoreCp >= 0 ? "+" : "")}{res.ScoreCp}).";
            }

            return true;
        }

    //Utilities

        private void ClearSelection()
        {
            _selectedSquare = null;
            _legalFromSelected.Clear();
            _isDragging = false;
            _dragPiece = null;
            _dragFromSquare = null;
        }

        private void Reset()
        {
            ClearSelection();
            _lastSearch = null;
        }

        private void LoadFenFromInput()
        {
            _status = "Type/paste FEN then press Enter.";
            _view.Draw(_window, _board, null, _status, _sideToMove);

            string fen = ReadLineFromKeyboard();
            if (!string.IsNullOrWhiteSpace(fen))
            {
                try { _board.LoadFen(fen.Trim()); Reset(); _status = "FEN loaded."; }
                catch (Exception ex) { Console.WriteLine(ex); _status = "Invalid FEN."; }
            }
            else _status = "FEN input cancelled.";
        }

        private string ReadLineFromKeyboard()
        {
            var chars = new List<char>(256);

            while (true)
            {
                SK.ProcessEvents();
                if (SK.KeyTyped(KeyCode.ReturnKey)) break;
                if (SK.KeyTyped(KeyCode.EscapeKey)) return "";

                for (KeyCode k = KeyCode.AKey; k <= KeyCode.ZKey; k++)
                    if (SK.KeyTyped(k)) chars.Add((char)('a' + (int)(k - KeyCode.AKey)));

                if (SK.KeyTyped(KeyCode.SpaceKey)) chars.Add(' ');
                if (SK.KeyTyped(KeyCode.SlashKey)) chars.Add('/');
                if (SK.KeyTyped(KeyCode.MinusKey)) chars.Add('-');
                for (int i = 0; i <= 9; i++)
                    if (SK.KeyTyped(KeyCode.Num0Key + i)) chars.Add((char)('0' + i));

                if (SK.KeyTyped(KeyCode.BackspaceKey) && chars.Count > 0)
                    chars.RemoveAt(chars.Count - 1);

                _status = "FEN> " + new string(chars.ToArray());
                _view.Draw(_window, _board, null, _status, _sideToMove);
            }

            return new string(chars.ToArray());
        }

        private static string Sq(int sq)
        {
            string files = "abcdefgh";
            int f = sq % 8, r = sq / 8;
            return $"{files[f]}{r + 1}";
        }
    }
}
