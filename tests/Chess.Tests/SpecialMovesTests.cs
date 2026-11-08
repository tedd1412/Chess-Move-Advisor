using Xunit;
using Chess.Domain;
using Chess.Engine;

namespace Chess.Tests
{
    /// <summary>
    /// Tests for special chess rules: castling, en passant, and promotion.
    /// </summary>
    public class SpecialMovesTests
    {
        [Fact(DisplayName = "Castling rights should be lost after moving the king")]
        public void King_Move_Should_Remove_Castling_Rights()
        {
            var board = new Board();
            board.LoadStartPosition();

            var move = new Move(4, 5); // e1 -> f1
            board.MakeMove(move);

            Assert.False(board.CanCastleWhiteKingSide);
            Assert.False(board.CanCastleWhiteQueenSide);
        }

        [Fact(DisplayName = "Castling should be available in starting position")]
        public void Castling_Should_Be_Allowed_Initially()
        {
            var board = new Board();
            board.LoadStartPosition();

            Assert.True(board.CanCastleWhiteKingSide);
            Assert.True(board.CanCastleWhiteQueenSide);
            Assert.True(board.CanCastleBlackKingSide);
            Assert.True(board.CanCastleBlackQueenSide);
        }

        [Fact(DisplayName = "Promotion should replace pawn with queen")]
        public void Promotion_Should_Replace_Pawn()
        {
            var board = new Board();
            board.Clear();
            board.SetPieceAt(48, new Piece(PieceType.Pawn, Color.White)); // a7

            var move = new Move(48, 56, PieceType.Queen);
            board.MakeMove(move);

            var piece = board.GetPieceAt(56);
            Assert.Equal(PieceType.Queen, piece?.Type);
            Assert.Equal(Color.White, piece?.Color);
        }

        [Fact(DisplayName = "En passant capture should remove the captured pawn correctly")]
        public void EnPassant_Should_Remove_Captured_Pawn()
        {
            var board = new Board();
            const string fen = "4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 2"; // White pawn e5, black pawn d5 just pushed
            board.LoadFen(fen);

            var move = new Move(36, 43, null, true, false, false, true); // e5xd6 en passant
            board.MakeMove(move);

            Assert.Null(board.GetPieceAt(35)); // captured pawn on d5 is gone
            Assert.Equal(new Piece(PieceType.Pawn, Color.White), board.GetPieceAt(43));

            board.UnmakeMove(move);
            Assert.Equal(fen, board.ToFen());
        }
    }
}
