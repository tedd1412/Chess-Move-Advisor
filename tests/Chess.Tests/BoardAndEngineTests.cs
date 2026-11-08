using Xunit;
using Chess.Domain;
using Chess.Engine;

namespace Chess.Tests
{
    /// <summary>
    /// Basic unit tests for Chess Position Analyser
    /// Tests focus on core OOP functionality, not UI rendering.
    /// </summary>
    public class BoardAndEngineTests
    {
        // ULO 1.1: Encapsulation and Abstraction
        [Fact]
        public void Board_Loads_Starting_Position_Correctly()
        {
            var board = new Board();
            board.LoadStartPosition();

            var piece = board.GetPieceAt(0); // a1
            Assert.NotNull(piece);
            Assert.Equal(PieceType.Rook, piece.Value.Type);
            Assert.Equal(Color.White, piece.Value.Color);

            var pieceE8 = board.GetPieceAt(60); // e8
            Assert.NotNull(pieceE8);
            Assert.Equal(PieceType.King, pieceE8.Value.Type);
            Assert.Equal(Color.Black, pieceE8.Value.Color);
        }

        // ULO 1.3: Testing method correctness
        [Fact]
        public void Board_ToFen_And_LoadFen_Should_Be_Reversible()
        {
            var board = new Board();
            board.LoadStartPosition();

            string fen = board.ToFen();
            var clone = new Board();
            clone.LoadFen(fen);

            // simple comparison: both boards should have identical FEN
            Assert.Equal(fen, clone.ToFen());
        }

        // ULO 2.1: Legal move generation
        [Fact]
        public void Pawn_Should_Have_Two_Legal_Moves_From_Start()
        {
            var board = new Board();
            board.LoadStartPosition();
            var gen = new LegalMoveGenerator();

            Span<Move> moves = stackalloc Move[256];
            int count = gen.GenerateMoves(board, moves);

            // Count moves for a2 pawn (square 8)
            int pawnMoves = 0;
            for (int i = 0; i < count; i++)
                if (moves[i].From == 8) pawnMoves++;

            Assert.Equal(2, pawnMoves); // a2→a3 and a2→a4
        }

        // ULO 4.1: Strategy pattern test (different evaluators)
        [Fact]
        public void Evaluator_Should_Give_Higher_Score_For_Material_Advantage()
        {
            var board = new Board();
            board.Clear();
            // White has queen, black has nothing
            board.SetPieceAt(0, new Piece(PieceType.Queen, Color.White));

            var eval = new MaterialEvaluator();
            int score = eval.Evaluate(board);

            Assert.True(score > 0); // White should be winning
        }

        // Engine sanity test — verifying search returns a legal move
        [Fact]
        public void AlphaBetaSearch_Should_Return_A_Move()
        {
            var board = new Board();
            board.LoadStartPosition();
            var gen = new LegalMoveGenerator();
            var eval = new MaterialEvaluator();
            var engine = new AlphaBetaSearch(gen, eval);

            var result = engine.Analyse(board, 2);

            Assert.NotEqual(default(Move), result.BestMove);
            Assert.InRange(result.Depth, 1, 2);
        }

        // Command pattern verification
        [Fact]
        public void MoveCommand_Should_Execute_And_Unexecute_Correctly()
        {
            var board = new Board();
            board.LoadStartPosition();

            // Simple move: e2→e4
            var move = new Move(12, 28);
            var cmd = new MoveCommand(board, move);

            var before = board.GetPieceAt(12);
            cmd.Execute();
            Assert.Null(board.GetPieceAt(12));
            Assert.Equal(before, board.GetPieceAt(28));

            cmd.Unexecute();
            Assert.Equal(before, board.GetPieceAt(12));
            Assert.Null(board.GetPieceAt(28));
        }
    }
}
