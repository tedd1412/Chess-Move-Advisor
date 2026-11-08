using Chess.Domain;

namespace Chess.Engine;

// Basic move generator for knights only (simple version for testing or fallback).
public sealed class SimpleMoveGenerator : IMoveGenerator
{
    private static readonly int[] KnightD = { +17, +15, +10, +6, -6, -10, -15, -17 };

    public int GenerateMoves(IBoard board, Span<Move> buffer)
    {
        int count = 0;
        for (int sq = 0; sq < 64; sq++)
        {
            var pc = board.GetPieceAt(sq);
            if (pc is null || pc.Value.Color != board.SideToMove) continue;

            if (pc.Value.Type == PieceType.Knight)
            {
                foreach (var d in KnightD)
                {
                    int to = sq + d;
                    if (to < 0 || to > 63) continue;
                    if (Math.Abs((sq % 8) - (to % 8)) > 2) continue;

                    var tgt = board.GetPieceAt(to);
                    if (tgt?.Color == pc.Value.Color) continue;

                    buffer[count++] = new Move(sq, to, null, tgt is not null);
                }
            }
        }
        return count;
    }
}