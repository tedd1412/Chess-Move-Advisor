using Xunit;
using Chess.Domain;
using Chess.Engine;

namespace Chess.Tests
{
    /// <summary>
    /// Tests for the LegalMoveGenerator class to ensure all generated moves are valid under chess rules.
    /// </summary>
    public class LegalityTests
    {
        [Fact(DisplayName = "LegalMoveGenerator should not produce moves that expose own king to check")]
        public void LegalMoveGenerator_Should_Avoid_SelfCheck()
        {
            var board = new Board();
            // Minimal FEN where moving the pinned bishop would expose the king
            board.LoadFen("8/8/8/8/8/8/2B5/R3K2R w KQ - 0 1");

            var gen = new LegalMoveGenerator();
            Span<Move> moves = stackalloc Move[256];
            int count = gen.GenerateMoves(board, moves);

            for (int i = 0; i < count; i++)
            {
                board.MakeMove(moves[i]);
                bool kingInCheck = board.IsInCheck(Color.White);
                board.UnmakeMove(moves[i]);

                Assert.False(kingInCheck);
            }
        }

        [Fact(DisplayName = "GenerateMoves should include all legal knight jumps")]
        public void Knight_Should_Have_Correct_Moves()
        {
            var board = new Board();
            board.Clear();
            board.SetPieceAt(18, new Piece(PieceType.Knight, Color.White)); // c3

            var gen = new LegalMoveGenerator();
            Span<Move> moves = stackalloc Move[32];
            int count = gen.GenerateMoves(board, moves);

            // A knight in c3 should have 8 possible moves (if board empty)
            Assert.Equal(8, count);
        }

        [Fact(DisplayName = "King should only move one square in any direction")]
        public void King_Should_Move_One_Square()
        {
            var board = new Board();
            board.Clear();
            board.SetPieceAt(27, new Piece(PieceType.King, Color.White)); // d4

            var gen = new LegalMoveGenerator();
            Span<Move> moves = stackalloc Move[16];
            int count = gen.GenerateMoves(board, moves);

            Assert.Equal(8, count); // 8 adjacent squares
        }
    }
}
