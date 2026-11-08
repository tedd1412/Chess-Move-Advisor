using System;
using Chess.Domain;

namespace Chess.Engine
{
    /// <summary>
    /// LegalMoveGenerator — generates all legal chess moves for the current position.
    /// Ensures no move leaves the king in check.
    /// </summary>
    public sealed class LegalMoveGenerator : IMoveGenerator
    {
        // Direction deltas for each piece type
        private static readonly int[] KingD   = { +8, +9, +1, -7, -8, -9, -1, +7 };
        private static readonly int[] BishopD = { +9, +7, -7, -9 };
        private static readonly int[] RookD   = { +8, -8, +1, -1 };

        // Castling is part of the rules, so it is on by default; the flag only exists for experiments.
        public bool IncludeCastling { get; set; } = true;

        public int GenerateMoves(IBoard ib, Span<Move> buffer)
        {
            var b = (Board)ib;
            int count = 0;
            Span<Move> pseudo = stackalloc Move[256];
            int pseudoCount = GeneratePseudo(b, pseudo);

            for (int i = 0; i < pseudoCount; i++)
            {
                var m = pseudo[i];
                b.MakeMove(m);
                bool legal = !b.IsInCheck(Opposite(b.SideToMove));
                b.UnmakeMove(m);

                if (legal)
                    buffer[count++] = m;
            }

            return count;
        }

        private int GeneratePseudo(Board b, Span<Move> buf)
        {
            int count = 0;
            for (int sq = 0; sq < 64; sq++)
            {
                var piece = b.GetPieceAt(sq);
                if (piece is null || piece.Value.Color != b.SideToMove)
                    continue;

                switch (piece.Value.Type)
                {
                    case PieceType.Pawn:
                        count = GeneratePawnMoves(b, sq, piece.Value, buf, count);
                        break;
                    case PieceType.Knight:
                        count = GenerateKnightMoves(b, sq, piece.Value, buf, count); // ✅ use new knight generator
                        break;
                    case PieceType.Bishop:
                        count = GenerateSlideMoves(b, sq, piece.Value, buf, count, BishopD);
                        break;
                    case PieceType.Rook:
                        count = GenerateSlideMoves(b, sq, piece.Value, buf, count, RookD);
                        break;
                    case PieceType.Queen:
                        count = GenerateSlideMoves(b, sq, piece.Value, buf, count, BishopD);
                        count = GenerateSlideMoves(b, sq, piece.Value, buf, count, RookD);
                        break;
                    case PieceType.King:
                        count = GenerateStepMoves(b, sq, piece.Value, buf, count, KingD);
                        if (IncludeCastling)
                            count = GenerateCastlingMoves(b, sq, piece.Value, buf, count);
                        break;
                }
            }
            return count;
        }

        // Knight move generator 
        private static int GenerateKnightMoves(Board b, int sq, Piece p, Span<Move> buf, int count)
        {
            int rank = sq / 8;
            int file = sq % 8;

            int[,] moves =
            {
                {+2, +1}, {+2, -1}, {-2, +1}, {-2, -1},
                {+1, +2}, {+1, -2}, {-1, +2}, {-1, -2}
            };

            for (int i = 0; i < 8; i++)
            {
                int nr = rank + moves[i, 0];
                int nf = file + moves[i, 1];
                if (nr < 0 || nr >= 8 || nf < 0 || nf >= 8)
                    continue;

                int to = nr * 8 + nf;
                var tgt = b.GetPieceAt(to);
                if (tgt is null || tgt.Value.Color != p.Color)
                    buf[count++] = new Move(sq, to, null, tgt is not null);
            }

            return count;
        }

        private static int GenerateStepMoves(Board b, int sq, Piece p, Span<Move> buf, int count, int[] deltas)
        {
            foreach (int d in deltas)
            {
                int to = sq + d;
                if (!OnBoard(to) || FileWrap(sq, to)) continue;

                var tgt = b.GetPieceAt(to);
                if (tgt is null || tgt.Value.Color != p.Color)
                    buf[count++] = new Move(sq, to, null, tgt is not null);
            }
            return count;
        }

        private static int GenerateSlideMoves(Board b, int sq, Piece p, Span<Move> buf, int count, int[] deltas)
        {
            foreach (int d in deltas)
            {
                int to = sq + d;
                while (OnBoard(to) && !FileWrap(to - d, to))
                {
                    var tgt = b.GetPieceAt(to);
                    if (tgt is null)
                        buf[count++] = new Move(sq, to);
                    else
                    {
                        if (tgt.Value.Color != p.Color)
                            buf[count++] = new Move(sq, to, null, true);
                        break;
                    }
                    to += d;
                }
            }
            return count;
        }

        private static int GeneratePawnMoves(Board b, int sq, Piece p, Span<Move> buf, int count)
        {
            int dir = (p.Color == Color.White) ? +8 : -8;
            int rank = sq / 8;
            int file = sq % 8;

            // Single push
            int one = sq + dir;
            if (OnBoard(one) && b.GetPieceAt(one) is null)
            {
                count = AddPawnMove(buf, count, sq, one, p, false);
                // Double push
                bool start = (p.Color == Color.White && rank == 1) || (p.Color == Color.Black && rank == 6);
                int two = sq + 2 * dir;
                if (start && b.GetPieceAt(two) is null)
                    buf[count++] = new Move(sq, two);
            }

            // Captures
            foreach (int df in new[] { -1, +1 })
            {
                int toFile = file + df;
                if (toFile < 0 || toFile > 7) continue;
                int to = sq + dir + df;
                if (!OnBoard(to)) continue;

                var tgt = b.GetPieceAt(to);
                if (tgt is { } t && t.Color != p.Color)
                    count = AddPawnMove(buf, count, sq, to, p, true);
            }

            // En passant
            if (b.EnPassantSquare is int ep)
            {
                int epFile = ep % 8;
                int epRank = ep / 8;
                int victimSq = ep - dir; // the pawn that just made the double push
                bool victimThere = OnBoard(victimSq)
                    && b.GetPieceAt(victimSq) is { Type: PieceType.Pawn } v && v.Color != p.Color;
                if (Math.Abs(file - epFile) == 1 && epRank == rank + (p.Color == Color.White ? 1 : -1)
                    && b.GetPieceAt(ep) is null && victimThere)
                    buf[count++] = new Move(sq, ep, null, true, false, false, true);
            }

            return count;
        }

        private static int AddPawnMove(Span<Move> buf, int count, int from, int to, Piece p, bool capture)
        {
            int toRank = to / 8;
            bool promo = (p.Color == Color.White && toRank == 7) || (p.Color == Color.Black && toRank == 0);
            if (promo)
            {
                foreach (var pr in new[] { PieceType.Queen, PieceType.Rook, PieceType.Bishop, PieceType.Knight })
                    buf[count++] = new Move(from, to, pr, capture);
            }
            else buf[count++] = new Move(from, to, null, capture);
            return count;
        }

        // Castling rules: the right must still exist, king and rook on their home squares, the squares
        // between them empty, and the king may not be in check or pass through an attacked square.
        // (Landing in check is rejected later by the normal legality filter.)
        private static int GenerateCastlingMoves(Board b, int sq, Piece p, Span<Move> buf, int count)
        {
            bool white = p.Color == Color.White;
            int home = white ? 4 : 60;
            if (sq != home) return count;

            var enemy = Opposite(p.Color);
            if (b.IsSquareAttacked(home, enemy)) return count;

            int kingSideRight = white ? Board.WhiteKingSide : Board.BlackKingSide;
            int queenSideRight = white ? Board.WhiteQueenSide : Board.BlackQueenSide;

            // King side: rook on h-file, f and g empty, f not attacked.
            if ((b.CastleRights & kingSideRight) != 0
                && IsOwnRook(b, home + 3, p.Color)
                && b.GetPieceAt(home + 1) is null && b.GetPieceAt(home + 2) is null
                && !b.IsSquareAttacked(home + 1, enemy))
                buf[count++] = new Move(sq, home + 2, null, false, true, false);

            // Queen side: rook on a-file, b, c and d empty, d not attacked.
            if ((b.CastleRights & queenSideRight) != 0
                && IsOwnRook(b, home - 4, p.Color)
                && b.GetPieceAt(home - 1) is null && b.GetPieceAt(home - 2) is null && b.GetPieceAt(home - 3) is null
                && !b.IsSquareAttacked(home - 1, enemy))
                buf[count++] = new Move(sq, home - 2, null, false, false, true);

            return count;
        }

        private static bool IsOwnRook(Board b, int sq, Color c) =>
            b.GetPieceAt(sq) is { Type: PieceType.Rook } r && r.Color == c;

        private static bool OnBoard(int sq) => sq >= 0 && sq < 64;
        private static bool FileWrap(int a, int b) => Math.Abs((a % 8) - (b % 8)) > 1;
        private static Color Opposite(Color c) => c == Color.White ? Color.Black : Color.White;
    }
}
