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

        // Fewer nodes for the same depth is better (it means more pruning), so this only checks that
        // every root move is searched and that deeper searches do more work.
        [Fact(DisplayName = "Deeper searches visit more nodes, and every root move is searched")]
        public void Node_Count_Grows_With_Depth()
        {
            var board = new Board();
            board.LoadStartPosition();

            var engine = new AlphaBetaSearch(new LegalMoveGenerator(), new HeuristicEvaluator());
            long d1 = engine.Analyse(board, 1).Nodes;
            long d2 = engine.Analyse(board, 2).Nodes;
            long d3 = engine.Analyse(board, 3).Nodes;

            Assert.True(d1 >= 20, $"expected all 20 root moves searched, got {d1} nodes");
            Assert.True(d1 < d2 && d2 < d3, $"node counts {d1}, {d2}, {d3} should grow with depth");
        }
    }
}
