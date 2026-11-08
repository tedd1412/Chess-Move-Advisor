using Chess.Domain;
using Chess.Engine;

namespace Chess.Tests;

/// <summary>
/// One test per bug fixed in the November 2026 audit, so none of them can come back unnoticed.
/// </summary>
public class RegressionTests
{
    private static List<Move> Legal(Board b)
    {
        Span<Move> buf = stackalloc Move[256];
        int n = new LegalMoveGenerator().GenerateMoves(b, buf);
        return buf[..n].ToArray().ToList();
    }

    private static Board FromFen(string fen)
    {
        var b = new Board();
        b.LoadFen(fen);
        return b;
    }

    // ---- Rules ----

    [Fact(DisplayName = "Black en passant removes the white pawn and unmake restores it")]
    public void Black_EnPassant_RoundTrip()
    {
        const string fen = "4k3/8/8/8/3Pp3/8/8/4K3 b - d3 0 1";
        var b = FromFen(fen);
        var ep = Legal(b).Single(m => m.IsEnPassant);
        Assert.Equal(19, ep.To); // e4xd3

        b.MakeMove(ep);
        Assert.Null(b.GetPieceAt(27)); // d4 pawn captured
        b.UnmakeMove(ep);
        Assert.Equal(fen, b.ToFen());
    }

    [Fact(DisplayName = "Castling moves the rook, and unmake puts both pieces back")]
    public void Castling_RoundTrip_Both_Colours()
    {
        const string fen = "r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1";
        var b = FromFen(fen);
        var castles = Legal(b).Where(m => m.IsCastleKingSide || m.IsCastleQueenSide).ToList();
        Assert.Equal(2, castles.Count);

        var oo = castles.Single(m => m.IsCastleKingSide);
        b.MakeMove(oo);
        Assert.Equal(new Piece(PieceType.Rook, Color.White), b.GetPieceAt(5)); // f1
        Assert.Equal(new Piece(PieceType.King, Color.White), b.GetPieceAt(6)); // g1

        var blackOoo = Legal(b).Single(m => m.IsCastleQueenSide);
        b.MakeMove(blackOoo);
        Assert.Equal(new Piece(PieceType.Rook, Color.Black), b.GetPieceAt(59)); // d8

        b.UnmakeMove(blackOoo);
        b.UnmakeMove(oo);
        Assert.Equal(fen, b.ToFen());
    }

    [Theory(DisplayName = "No castling out of, through, or into check")]
    [InlineData("4k3/8/8/8/8/8/8/R3K2R w KQ - 0 1", 2)]   // free to castle both ways
    [InlineData("4k3/8/8/8/8/8/4r3/R3K2R w KQ - 0 1", 0)] // in check from e2
    [InlineData("4k3/8/8/8/8/8/5r2/R3K2R w KQ - 0 1", 1)] // f1 attacked: only O-O-O
    [InlineData("4k3/8/8/8/8/8/6r1/R3K2R w KQ - 0 1", 1)] // g1 attacked: only O-O-O
    [InlineData("4k3/8/8/8/8/8/1r6/R3K2R w KQ - 0 1", 2)] // b1 attacked is allowed (king does not cross it)
    public void Castling_Respects_Attacked_Squares(string fen, int expectedCastles)
    {
        var castles = Legal(FromFen(fen)).Count(m => m.IsCastleKingSide || m.IsCastleQueenSide);
        Assert.Equal(expectedCastles, castles);
    }

    [Fact(DisplayName = "Castling needs the king and rook on their home squares")]
    public void Castling_Needs_Pieces_At_Home()
    {
        // Rights say KQ, but the king is on d1 and there is no rook on a1.
        var b = FromFen("4k3/8/8/8/8/8/8/3K3R w KQ - 0 1");
        Assert.DoesNotContain(Legal(b), m => m.IsCastleKingSide || m.IsCastleQueenSide);
    }

    [Fact(DisplayName = "A cleared board has no castling rights and no king in check")]
    public void Cleared_Board_Is_Neutral()
    {
        var b = new Board();
        b.LoadStartPosition();
        b.Clear();
        Assert.Equal(0, b.CastleRights);
        Assert.False(b.IsInCheck(Color.White));
        Assert.Equal("8/8/8/8/8/8/8/8 w - - 0 1", b.ToFen());
    }

    // ---- Evaluation ----

    [Fact(DisplayName = "Piece-square tables are oriented: kings like their castled squares")]
    public void Pst_Orientation()
    {
        Assert.Equal(30, HeuristicEvaluator.SquareBonus(new Piece(PieceType.King, Color.White), 6));  // g1
        Assert.Equal(30, HeuristicEvaluator.SquareBonus(new Piece(PieceType.King, Color.Black), 62)); // g8
        Assert.Equal(50, HeuristicEvaluator.SquareBonus(new Piece(PieceType.Pawn, Color.White), 48)); // a7
        Assert.Equal(50, HeuristicEvaluator.SquareBonus(new Piece(PieceType.Pawn, Color.Black), 8));  // a2
    }

    [Theory(DisplayName = "Evaluators score from the side to move and treat colours symmetrically")]
    [InlineData("fast")]
    [InlineData("accurate")]
    public void Evaluator_Is_Symmetric(string mode)
    {
        var (_, eval, _) = EngineFactory.Create(mode);

        // White is a knight up with White to move, and the colour-flipped position with Black to move.
        int whiteUp = eval.Evaluate(FromFen("4k3/pppp4/8/8/8/2N5/PPPP4/4K3 w - - 0 1"));
        int blackUp = eval.Evaluate(FromFen("4k3/pppp4/2n5/8/8/8/PPPP4/4K3 b - - 0 1"));

        Assert.True(whiteUp > 250, $"expected a knight's advantage, got {whiteUp}");
        Assert.Equal(whiteUp, blackUp);
    }

    // ---- Search ----

    [Theory(DisplayName = "Search finds mate in one")]
    [InlineData("fast")]
    [InlineData("accurate")]
    public void Finds_Mate_In_One(string mode)
    {
        var (_, _, engine) = EngineFactory.Create(mode);
        var res = engine.Analyse(FromFen("6k1/5ppp/8/8/8/8/8/R5K1 w - - 0 1"), 3);

        Assert.Equal(new Move(0, 56), new Move(res.BestMove.From, res.BestMove.To)); // Ra1-a8#
        Assert.True(res.IsMate);
        Assert.Equal(1, res.MateInMoves);
    }

    [Fact(DisplayName = "Search reports checkmate and stalemate instead of a fake move")]
    public void Reports_Game_Over()
    {
        var (_, _, engine) = EngineFactory.Create("accurate");

        var mated = engine.Analyse(FromFen("R5k1/5ppp/8/8/8/8/8/6K1 b - - 0 1"), 3);
        Assert.False(mated.HasBestMove);
        Assert.True(mated.IsMate);
        Assert.True(mated.ScoreCp < 0);

        var stalemate = engine.Analyse(FromFen("7k/5Q2/6K1/8/8/8/8/8 b - - 0 1"), 3);
        Assert.False(stalemate.HasBestMove);
        Assert.False(stalemate.IsMate);
        Assert.Equal(0, stalemate.ScoreCp);
    }

    [Theory(DisplayName = "Black, as well as White, takes a hanging queen")]
    [InlineData("fast")]
    [InlineData("accurate")]
    public void Black_Takes_Free_Queen(string mode)
    {
        var (_, _, engine) = EngineFactory.Create(mode);
        var res = engine.Analyse(FromFen("3rk3/8/8/8/3Q4/8/8/7K b - - 0 1"), 2);

        Assert.Equal(59, res.BestMove.From); // Rd8
        Assert.Equal(27, res.BestMove.To);   // xd4
        Assert.True(res.ScoreCp > 0);
    }

    [Fact(DisplayName = "Fifty-move rule: the search scores a dead position as a draw")]
    public void Fifty_Move_Rule_Is_Draw()
    {
        var (_, _, engine) = EngineFactory.Create("accurate");
        // White is a rook up, but the halfmove clock is at 99, so every quiet move ends in a draw.
        var res = engine.Analyse(FromFen("4k3/8/8/8/8/8/8/R3K3 w - - 99 80"), 2);
        Assert.Equal(0, res.ScoreCp);
    }
}
