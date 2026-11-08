using Chess.UI.SplashKit;

namespace Chess.Tests;

/// <summary>
/// The screen mapping used for drawing squares, pieces, the best-move arrow and mouse input.
/// </summary>
public class BoardGeometryTests
{
    private const int A1 = 0, H1 = 7, A8 = 56, H8 = 63, E2 = 12, E4 = 28;

    [Fact(DisplayName = "White at the bottom: a1 bottom-left, h8 top-right")]
    public void White_View_Corners()
    {
        Assert.Equal((0, 7), BoardGeometry.ToCell(A1, blackAtBottom: false));
        Assert.Equal((7, 7), BoardGeometry.ToCell(H1, blackAtBottom: false));
        Assert.Equal((0, 0), BoardGeometry.ToCell(A8, blackAtBottom: false));
        Assert.Equal((7, 0), BoardGeometry.ToCell(H8, blackAtBottom: false));
    }

    [Fact(DisplayName = "Black at the bottom is the board turned 180 degrees")]
    public void Black_View_Is_A_Rotation()
    {
        Assert.Equal((0, 7), BoardGeometry.ToCell(H8, blackAtBottom: true));
        Assert.Equal((7, 0), BoardGeometry.ToCell(A1, blackAtBottom: true));
        for (int sq = 0; sq < 64; sq++)
        {
            var (c, r) = BoardGeometry.ToCell(sq, false);
            Assert.Equal((7 - c, 7 - r), BoardGeometry.ToCell(sq, true));
        }
    }

    [Theory(DisplayName = "Screen cell and square convert back and forth in both views")]
    [InlineData(false)]
    [InlineData(true)]
    public void Mapping_Round_Trips(bool blackAtBottom)
    {
        var seen = new HashSet<int>();
        for (int sq = 0; sq < 64; sq++)
        {
            var (c, r) = BoardGeometry.ToCell(sq, blackAtBottom);
            Assert.InRange(c, 0, 7);
            Assert.InRange(r, 0, 7);
            Assert.Equal(sq, BoardGeometry.ToSquare(c, r, blackAtBottom));
            seen.Add(r * 8 + c);
        }
        Assert.Equal(64, seen.Count); // every square gets its own cell
    }

    [Fact(DisplayName = "An arrow for e2-e4 points up for White and down for Black")]
    public void Arrow_Direction_Follows_View()
    {
        var (_, fromRowW) = BoardGeometry.ToCell(E2, false);
        var (_, toRowW) = BoardGeometry.ToCell(E4, false);
        var (_, fromRowB) = BoardGeometry.ToCell(E2, true);
        var (_, toRowB) = BoardGeometry.ToCell(E4, true);

        Assert.True(toRowW < fromRowW); // towards the top of the screen
        Assert.True(toRowB > fromRowB); // towards the bottom
    }

    [Fact(DisplayName = "Square colours: a1 and h8 dark, h1 and a8 light")]
    public void Square_Colours()
    {
        Assert.False(BoardGeometry.IsLightSquare(A1));
        Assert.False(BoardGeometry.IsLightSquare(H8));
        Assert.True(BoardGeometry.IsLightSquare(H1));
        Assert.True(BoardGeometry.IsLightSquare(A8));
    }
}
