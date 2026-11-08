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

        // Optional flag to include or skip castling
        public bool IncludeCastling { get; set; } = false;

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
                if (Math.Abs(file - epFile) == 1 && epRank == rank + (p.Color == Color.White ? 1 : -1))
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

        private static int GenerateCastlingMoves(Board b, int sq, Piece p, Span<Move> buf, int count)
        {
            if (p.Color == Color.White)
            {
                if ((b.CastleRights & 1) != 0 && b.GetPieceAt(5) is null && b.GetPieceAt(6) is null)
                    buf[count++] = new Move(sq, 6, null, false, true, false);
                if ((b.CastleRights & 2) != 0 && b.GetPieceAt(1) is null && b.GetPieceAt(2) is null && b.GetPieceAt(3) is null)
                    buf[count++] = new Move(sq, 2, null, false, false, true);
            }
            else
            {
                if ((b.CastleRights & 4) != 0 && b.GetPieceAt(61) is null && b.GetPieceAt(62) is null)
                    buf[count++] = new Move(sq, 62, null, false, true, false);
                if ((b.CastleRights & 8) != 0 && b.GetPieceAt(57) is null && b.GetPieceAt(58) is null && b.GetPieceAt(59) is null)
                    buf[count++] = new Move(sq, 58, null, false, false, true);
            }
            return count;
        }

        private static bool OnBoard(int sq) => sq >= 0 && sq < 64;
        private static bool FileWrap(int a, int b) => Math.Abs((a % 8) - (b % 8)) > 1;
        private static Color Opposite(Color c) => c == Color.White ? Color.Black : Color.White;
    }
}
