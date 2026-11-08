using Chess.Domain;

namespace Chess.Engine;

// Material-only evaluator ("fast" mode). Scores are in centipawns from the side to move's view,
// which is what negamax expects.
public sealed class MaterialEvaluator : IEvaluator
{
    // P, N, B, R, Q, K (the king is never captured, so it carries no material value).
    internal static readonly int[] PieceValues = { 100, 320, 330, 500, 900, 0 };

    public int Evaluate(IBoard board)
    {
        int score = 0; // from White's view
        for (int sq = 0; sq < 64; sq++)
        {
            if (board.GetPieceAt(sq) is not { } p) continue;
            int v = PieceValues[(int)p.Type];
            score += p.Color == Color.White ? v : -v;
        }
        return board.SideToMove == Color.White ? score : -score;
    }
}
