using Chess.Domain;
using Chess.Engine;

namespace Chess.Tests;

/// <summary>
/// Perft ("performance test") counts every legal move sequence to a fixed depth and compares the
/// total with published reference values. Any bug in move generation, make/unmake, castling,
/// en passant or promotion shows up as a wrong count. Positions and numbers are the standard set
/// from the Chess Programming Wiki (https://www.chessprogramming.org/Perft_Results).
/// </summary>
public class PerftTests
{
    private const string Start = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private const string Kiwipete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1";
    private const string Position3 = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
    private const string Position4 = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1";
    private const string Position5 = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8";

    [Theory(DisplayName = "Perft node counts match reference values")]
    [InlineData(Start, 1, 20L)]
    [InlineData(Start, 2, 400L)]
    [InlineData(Start, 3, 8_902L)]
    [InlineData(Start, 4, 197_281L)]
    [InlineData(Kiwipete, 1, 48L)]
    [InlineData(Kiwipete, 2, 2_039L)]
    [InlineData(Kiwipete, 3, 97_862L)]
    [InlineData(Position3, 1, 14L)]
    [InlineData(Position3, 2, 191L)]
    [InlineData(Position3, 3, 2_812L)]
    [InlineData(Position3, 4, 43_238L)]
    [InlineData(Position4, 1, 6L)]
    [InlineData(Position4, 2, 264L)]
    [InlineData(Position4, 3, 9_467L)]
    [InlineData(Position5, 1, 44L)]
    [InlineData(Position5, 2, 1_486L)]
    [InlineData(Position5, 3, 62_379L)]
    public void Perft_Matches_Reference(string fen, int depth, long expected)
    {
        var board = new Board();
        board.LoadFen(fen);

        long nodes = Perft(board, new LegalMoveGenerator(), depth);

        Assert.Equal(expected, nodes);
        Assert.Equal(fen, board.ToFen()); // make/unmake must restore the position exactly
    }

    private static long Perft(Board board, LegalMoveGenerator gen, int depth)
    {
        if (depth == 0) return 1;

        Span<Move> moves = stackalloc Move[256];
        int n = gen.GenerateMoves(board, moves);
        if (depth == 1) return n;

        long total = 0;
        for (int i = 0; i < n; i++)
        {
            board.MakeMove(moves[i]);
            total += Perft(board, gen, depth - 1);
            board.UnmakeMove(moves[i]);
        }
        return total;
    }
}
