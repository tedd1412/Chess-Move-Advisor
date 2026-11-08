using Chess.Domain;

namespace Chess.Engine;

public sealed class MaterialEvaluator : IEvaluator
{
    // Material values in centipawns (P, N, B, R, Q, K=0 since not captured)
    private static readonly int[] V = { 100, 320, 330, 500, 900, 0 };

    // Piece-Square Tables (PST) for each piece type, from white's perspective.
    // These are arrays of 64 values, one for each square, to encourage good positioning.
    private static readonly int[] PST_P = {
         0,  0,  0,  0,  0,  0,  0,  0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,
         5,  5, 10, 25, 25, 10,  5,  5,
         0,  0,  0, 20, 20,  0,  0,  0,
         5, -5,-10,  0,  0,-10, -5,  5,
         5, 10, 10,-20,-20, 10, 10,  5,
         0,  0,  0,  0,  0,  0,  0,  0,
    };
    private static readonly int[] PST_N = {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -30,  5, 10, 15, 15, 10,  5,-30,
        -30,  0, 15, 20, 20, 15,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50,
    };
    private static readonly int[] PST_B = {
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  5,  0,  0,  0,  0,  5,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -20,-10,-10,-10,-10,-10,-10,-20,
    };
    private static readonly int[] PST_R = {
          0,  0,  5, 10, 10,  5,  0,  0,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
          5, 10, 10, 10, 10, 10, 10,  5,
          0,  0,  0,  0,  0,  0,  0,  0,
    };
    private static readonly int[] PST_Q = {
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -10,  5,  5,  5,  5,  5,  0,-10,
         -5,  0,  5,  5,  5,  5,  0, -5,
          0,  0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20,
    };
    private static readonly int[] PST_K = {
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -10,-20,-20,-20,-20,-20,-20,-10,
         20, 20,  0,  0,  0,  0, 20, 20,
         20, 30, 10,  0,  0, 10, 30, 20,
    };

    public int Evaluate(IBoard ib)
    {
        var board = (Board)ib;
        int score = 0;

        for (int sq = 0; sq < 64; sq++)
        {
            var p = board.GetPieceAt(sq);
            if (p is null) continue;

            int mat = V[(int)p.Value.Type];
            int pst = PSTValue(p.Value.Type, p.Value.Color, sq);
            int s = mat + pst;
            score += (p.Value.Color == Color.White) ? s : -s;
        }

        // Small tempo bonus for side to move.
        score += (board.SideToMove == Color.White) ? 10 : -10;

        // Return from side-to-move's perspective (for negamax).
        return (board.SideToMove == Color.White) ? score : -score;
    }

    private static int PSTValue(PieceType t, Color c, int sq)
    {
        int idx = (c == Color.White) ? sq : Mirror(sq);
        return t switch
        {
            PieceType.Pawn => PST_P[idx],
            PieceType.Knight => PST_N[idx],
            PieceType.Bishop => PST_B[idx],
            PieceType.Rook => PST_R[idx],
            PieceType.Queen => PST_Q[idx],
            _ => PST_K[idx],
        };
    }

    private static int Mirror(int sq)
    {
        int f = sq % 8; int r = sq / 8;
        int mr = 7 - r;
        return mr * 8 + f;
    }
}