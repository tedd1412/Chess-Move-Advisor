using Chess.Domain;

namespace Chess.Engine;

// Hand-tuned evaluator ("accurate" mode): material plus piece-square tables (PSTs), the classic
// "simplified evaluation function" values. This is the baseline the learned evaluator will be
// measured against. Scores are in centipawns from the side to move's view (negamax convention).
public sealed class HeuristicEvaluator : IEvaluator
{
    // Each table is written the way a diagram is read from White's side: the first row is rank 8
    // (a8..h8) and the last row is rank 1 (a1..h1). Board squares use a1 = 0, so a White piece
    // looks up its square mirrored vertically, and a Black piece looks up its square directly.
    private static readonly int[] PawnTable = {
         0,  0,  0,  0,  0,  0,  0,  0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,
         5,  5, 10, 25, 25, 10,  5,  5,
         0,  0,  0, 20, 20,  0,  0,  0,
         5, -5,-10,  0,  0,-10, -5,  5,
         5, 10, 10,-20,-20, 10, 10,  5,
         0,  0,  0,  0,  0,  0,  0,  0,
    };
    private static readonly int[] KnightTable = {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,
        -30,  0, 15, 20, 20, 15,  0,-30,
        -30,  5, 10, 15, 15, 10,  5,-30,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50,
    };
    private static readonly int[] BishopTable = {
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10,  5,  0,  0,  0,  0,  5,-10,
        -20,-10,-10,-10,-10,-10,-10,-20,
    };
    private static readonly int[] RookTable = {
          0,  0,  0,  0,  0,  0,  0,  0,
          5, 10, 10, 10, 10, 10, 10,  5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
          0,  0,  0,  5,  5,  0,  0,  0,
    };
    private static readonly int[] QueenTable = {
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5,  5,  5,  5,  0,-10,
         -5,  0,  5,  5,  5,  5,  0, -5,
          0,  0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20,
    };
    private static readonly int[] KingTable = {
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -10,-20,-20,-20,-20,-20,-20,-10,
         20, 20,  0,  0,  0,  0, 20, 20,
         20, 30, 10,  0,  0, 10, 30, 20,
    };

    private const int TempoBonus = 10;

    public int Evaluate(IBoard board)
    {
        int score = 0; // from White's view
        for (int sq = 0; sq < 64; sq++)
        {
            if (board.GetPieceAt(sq) is not { } p) continue;
            int s = MaterialEvaluator.PieceValues[(int)p.Type] + SquareBonus(p, sq);
            score += p.Color == Color.White ? s : -s;
        }

        int stmScore = board.SideToMove == Color.White ? score : -score;
        return stmScore + TempoBonus;
    }

    // Exposed for tests: the PST bonus a piece gets on a square.
    internal static int SquareBonus(Piece p, int sq)
    {
        int idx = p.Color == Color.White ? MirrorRank(sq) : sq;
        return p.Type switch
        {
            PieceType.Pawn => PawnTable[idx],
            PieceType.Knight => KnightTable[idx],
            PieceType.Bishop => BishopTable[idx],
            PieceType.Rook => RookTable[idx],
            PieceType.Queen => QueenTable[idx],
            _ => KingTable[idx],
        };
    }

    private static int MirrorRank(int sq) => (7 - sq / 8) * 8 + sq % 8;
}
