using Xunit;
using Chess.Domain;
using Chess.Engine;

namespace Chess.Tests
{
    /// <summary>
    /// Smoke tests for AlphaBetaSearch to confirm analysis runs correctly and returns stable results.
    /// </summary>
    public class SearchSmokeTests
    {
        [Fact(DisplayName = "AlphaBetaSearch should run at multiple depths without throwing exceptions")]
        public void AlphaBeta_Should_Run_Safely()
        {
            var board = new Board();
            board.LoadStartPosition();

            var engine = new AlphaBetaSearch(new LegalMoveGenerator(), new MaterialEvaluator());

            for (int depth = 1; depth <= 3; depth++)
            {
                var result = engine.Analyse(board, depth);
                Assert.True(result.Nodes > 0);
            }
        }

        [Fact(DisplayName = "AlphaBeta search result should remain consistent on repeated runs")]
        public void AlphaBeta_Should_Return_Consistent_Move()
        {
            var board = new Board();
            board.LoadStartPosition();

            var engine = new AlphaBetaSearch(new LegalMoveGenerator(), new MaterialEvaluator());

            var first = engine.Analyse(board, 2);
            var second = engine.Analyse(board, 2);

            Assert.Equal(first.BestMove.From, second.BestMove.From);
            Assert.Equal(first.BestMove.To, second.BestMove.To);
        }

        [Fact(DisplayName = "Search should explore at least 1000 nodes at depth 3 (performance smoke test)")]
        public void Search_Should_Reach_Minimum_Node_Count()
        {
            var board = new Board();
            board.LoadStartPosition();

            var engine = new AlphaBetaSearch(new LegalMoveGenerator(), new MaterialEvaluator());
            var result = engine.Analyse(board, 3);

            Assert.True(result.Nodes > 1000, $"Expected >1000 nodes, got {result.Nodes}");
        }
    }
}
