using Chess.Domain;

namespace Chess.Engine;

// Evaluator class implementing interface (polymorphism).
public sealed class HeuristicEvaluator : IEvaluator
{
    // Private static arrays for PST (encapsulation).
    private static readonly int[] Pawn = { 0, 5, 5, 0, 5, 10, 50, 0 };
    private static readonly int[] Knight = { -50, -40, -30, -30, -30, -30, -40, -50 };
    private static readonly int[] Bishop = { -20, -10, -10, -10, -10, -10, -10, -20 };
    private static readonly int[] Rook = { 0, 0, 5, 10, 10, 5, 0, 0 };
    private static readonly int[] Queen = { 0, 0, 0, 5, 5, 0, 0, 0 };
    private static readonly int[] King = { 20, 30, 10, 0, 0, 10, 30, 20 };

    public int Evaluate(IBoard board)
    {
        int score = 0;
        for (int sq = 0; sq < 64; sq++)
        {
            var p = board.GetPieceAt(sq);
            if (p is null) continue;

            int baseVal = Value(p.Value.Type);
            int posVal = SquareBonus(p.Value, sq);
            score += (p.Value.Color == Color.White ? baseVal + posVal : -(baseVal + posVal));
        }
        return score;
    }

    private static int Value(PieceType t) => t switch
    {
        PieceType.Pawn => 100,
        PieceType.Knight => 320,
        PieceType.Bishop => 330,
        PieceType.Rook => 500,
        PieceType.Queen => 900,
        _ => 20000
    };

    private static int SquareBonus(Piece p, int sq)
    {
        int rank = sq / 8;
        return p.Type switch
        {
            PieceType.Pawn => Pawn[rank],
            PieceType.Knight => Knight[rank],
            PieceType.Bishop => Bishop[rank],
            PieceType.Rook => Rook[rank],
            PieceType.Queen => Queen[rank],
            _ => King[rank],
        };
    }
}

// Note: Implements IEvaluator (polymorphism), private methods for cohesion.