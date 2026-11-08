using System;
using System.Collections.Generic;
using SplashKitSDK;
using Chess.Domain;
using Chess.Engine;

using SK = SplashKitSDK.SplashKit;
using DomainColor = Chess.Domain.Color;

namespace Chess.UI.SplashKit
{
    /// <summary>
    /// BoardView — the View component (MVC).
    /// Responsible only for rendering (displaying) the chessboard, pieces, UI panels, and highlights
    /// </summary>
    public class BoardView
    {
        // --- Layout constants (UI layout) ---
        public const int Tile = 80;              // each square size in pixels
        public const int Margin = 20;            // spacing around board
        public const int PanelW = 300;           // info panel width
        public const int BoardX = Margin;        // X offset of board
        public const int BoardY = Margin;        // Y offset of board

        // Asset manager (composition: BoardView "has" PieceAssets)
        private readonly PieceAssets _assets = new();

        /// <summary>
        /// Draws the full board and side panel.
        /// </summary>
        public void Draw(
            Window window,
            Board board,
            SearchResult? last,
            string status,
            DomainColor side,
            int? selectedSq = null,
            IReadOnlyList<int>? targets = null,
            int? dragFrom = null,
            (Piece piece, float x, float y)? dragOverlay = null,
            bool flipped = false)
        {
            SK.ClearScreen(SK.RGBColor(240, 240, 240));
            DrawBoardSquares(board, selectedSq, targets, flipped);
            DrawPieces(board, dragFrom, flipped);
            if (dragOverlay is { } overlay)
                DrawDraggingPiece(overlay.piece, overlay.x, overlay.y);
            if (last is { BestMove: { } mv } && mv.From != mv.To && mv.From >= 0 && mv.To >= 0 && mv.From < 64 && mv.To < 64)
            {
                DrawBestMoveArrow(mv, flipped);
            }

            DrawSidePanel(status, last, side);
            window.Refresh(60);

        }

        // board drawing methods

        private void DrawBoardSquares(Board board, int? selectedSq, IReadOnlyList<int>? targets, bool flipped)
        {
            for (int r = 0; r < 8; r++)
            {
                for (int f = 0; f < 8; f++)
                {
                    int sq = BoardGeometry.ToSquare(f, r, flipped);

                    float x = BoardX + f * Tile;
                    float y = BoardY + r * Tile;

                    bool isLight = BoardGeometry.IsLightSquare(sq);
                    var col = isLight ? SK.RGBColor(238, 238, 210) : SK.RGBColor(118, 150, 86);

                    // Highlight selection
                    if (selectedSq == sq)
                        col = SK.RGBColor(186, 202, 68);

                    // Highlight legal target squares
                    if (targets != null && targets.Contains(sq))
                        col = SK.RGBColor(246, 246, 105);

                    SK.FillRectangle(col, x, y, Tile, Tile);
                }
            }
        }

        private void DrawPieces(Board board, int? draggingFrom, bool flipped)
        {
            for (int sq = 0; sq < 64; sq++)
            {
                if (sq == draggingFrom) continue;
                var piece = board.GetPieceAt(sq);
                if (piece is null) continue;
                var img = _assets.Get(piece.Value);
                if (img == null) continue;

                var (col, row) = BoardGeometry.ToCell(sq, flipped);

                float x = BoardX + col * Tile;
                float y = BoardY + row * Tile;
                SK.DrawBitmap(img, x, y);
            }
        }
        private void DrawDraggingPiece(Piece piece, float mx, float my)
        {
            var img = _assets.Get(piece);
            if (img != null)
                SK.DrawBitmap(img, mx - Tile / 2f, my - Tile / 2f);
        }

        private void DrawSidePanel(string status, SearchResult? last, DomainColor side)
        {
            int panelX = BoardX + 8 * Tile + Margin;
            SK.FillRectangle(SK.RGBColor(230, 230, 230), panelX, BoardY, PanelW, 8 * Tile);

            // Title and info
            DrawPanelText($"Side to move: {side}", panelX + 10, BoardY + 10);
            DrawPanelText($"Status: {status}", panelX + 10, BoardY + 40);

            if (last is { } res)
            {
                DrawPanelText($"Last analysis:", panelX + 10, BoardY + 80);
                DrawPanelText($"Depth: {res.Depth}", panelX + 10, BoardY + 110);
                DrawPanelText($"Score (side to move): {GameController.FormatScore(res)}", panelX + 10, BoardY + 140);
                DrawPanelText(res.HasBestMove ? $"Best move: {Sq(res.BestMove.From)}→{Sq(res.BestMove.To)}" : "Best move: none (game over)", panelX + 10, BoardY + 170);
                DrawPanelText($"Nodes: {res.Nodes}", panelX + 10, BoardY + 200);
            }
            int helpY = BoardY + 240;
            DrawPanelText("=== Controls ===", panelX + 10, helpY);
            DrawPanelText("S: Start position", panelX + 10, helpY + 20);
            DrawPanelText("C: Clear board", panelX + 10, helpY + 40);
            DrawPanelText("W/B: Set side to move", panelX + 10, helpY + 60);
            DrawPanelText("T: Toggle auto-analyse", panelX + 10, helpY + 80);
            DrawPanelText("A: Analyse manually", panelX + 10, helpY + 100);
            DrawPanelText("F: Show FEN", panelX + 10, helpY + 120);
            DrawPanelText("I: Input FEN", panelX + 10, helpY + 140);
            DrawPanelText("X: Swap board side", panelX + 10, helpY + 160);
            DrawPanelText("Esc: Quit", panelX + 10, helpY + 180);
        }
        // Best move arrow
        private void DrawBestMoveArrow(Move move, bool flipped)
        {
            (float fx, float fy) = SquareCenter(move.From, flipped);
            (float tx, float ty) = SquareCenter(move.To, flipped);

            // Draw line (arrow shaft)
            SK.DrawLine(
                SK.RGBAColor(255, 0, 0, 150), // semi-transparent red
                fx, fy,
                tx, ty
            );

            // Draw arrow head
            float angle = (float)Math.Atan2(ty - fy, tx - fx);
            float size = 15f;
            float ax1 = tx - (float)Math.Cos(angle - 0.3f) * size;
            float ay1 = ty - (float)Math.Sin(angle - 0.3f) * size;
            float ax2 = tx - (float)Math.Cos(angle + 0.3f) * size;
            float ay2 = ty - (float)Math.Sin(angle + 0.3f) * size;

            SK.FillTriangle(
                SK.RGBAColor(255, 0, 0, 150),
                tx, ty,
                ax1, ay1,
                ax2, ay2
            );
        }
        // utilities

        public static (int, bool) SquareFromMouse(bool flipped = false)
        {
            // SplashKit returns mouse positions as float → cast to int for board math
            float mx = SK.MouseX();
            float my = SK.MouseY();
            if (mx >= BoardX && mx < BoardX + 8 * Tile &&
                my >= BoardY && my < BoardY + 8 * Tile)
            {
                int col = (int)((mx - BoardX) / Tile);
                int row = (int)((my - BoardY) / Tile);
                return (BoardGeometry.ToSquare(col, row, flipped), true);
            }
            return (0, false);
        }
        public static (float, float) SquareCenter(int sq, bool flipped)
        {
            if (sq < 0 || sq >= 64) return (0, 0);
            var (col, row) = BoardGeometry.ToCell(sq, flipped);
            float x = BoardX + col * Tile + Tile / 2f;
            float y = BoardY + row * Tile + Tile / 2f;
            return (x, y);
        }

        public static string Sq(int sq)
        {
            if (sq < 0 || sq >= 64) return "?";
            string files = "abcdefgh";
            int f = sq % 8;
            int r = sq / 8;
            return $"{files[f]}{r + 1}";
        }

        public void DrawPanelText(string text, int x, int y)
        {
            SK.DrawText(text, SK.ColorBlack(), "Arial", 14, x, y);
        }
    }

    // Asset manager

    /// <summary>
    /// PieceAssets — loads and caches piece images.
    /// Demonstrates Encapsulation and Cohesion (keeps image handling separate).
    /// </summary>
    public class PieceAssets
    {
        private readonly Dictionary<string, Bitmap> _cache = new();

        public PieceAssets()
        {
            LoadAssets();
        }

        private void LoadAssets()
        {
            // The build copies images/assets/** next to the executable, so resolve from there rather
            // than from the drive root or the current working directory.
            string baseDir = System.IO.Path.Combine(AppContext.BaseDirectory, "images", "assets", "pieces");

            string[] names =
            {
                "kin-whi", "que-whi", "roo-whi", "bis-whi", "kni-whi", "paw-whi",
                "kin-bla", "que-bla", "roo-bla", "bis-bla", "kni-bla", "paw-bla"
            };

            foreach (string name in names)
            {
                string path = System.IO.Path.Combine(baseDir, $"{name}.png");
                if (!System.IO.File.Exists(path))
                {
                    Console.WriteLine($"[WARN] Missing asset: {path}");
                    continue;
                }
                // No catch-all here: if the SplashKit native library is missing, that error should
                // surface as itself, not be reported as a missing image.
                _cache[name] = SK.LoadBitmap($"piece_{name}", path);
            }

        }

        /// <summary>
        /// Gets bitmap for a given piece, using normalized name key.
        /// </summary>
        public Bitmap? Get(Piece piece)
        {
            string key = Normalize(piece.Type.ToString(), piece.Color);
            if (_cache.TryGetValue(key, out var img))
                return img;

            Console.WriteLine($"[WARN] No image for {key}");
            return null;
        }

        /// <summary>
        /// Normalizes type/color into cache key like 'kin-whi'.
        /// Always returns a value (fix for compile error).
        /// </summary>
        private string Normalize(string type, DomainColor color)
        {
            string t = type[..3].ToLower();
            string c = color == DomainColor.White ? "whi" : "bla";
            return $"{t}-{c}";
        }
    }
}


