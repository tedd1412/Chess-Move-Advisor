namespace Chess.UI.SplashKit;

/// <summary>
/// The single mapping between board squares (a1 = 0 ... h8 = 63) and screen cells (column 0 is the
/// left edge, row 0 is the top). Drawing, the best-move arrow and mouse input all go through here,
/// so they cannot disagree about where a square is.
///
/// White at the bottom: a1 bottom-left, h8 top-right.
/// Black at the bottom: the board turned 180 degrees, so h8 bottom-left and a1 top-right.
/// </summary>
public static class BoardGeometry
{
    public static (int Col, int Row) ToCell(int sq, bool blackAtBottom)
    {
        int file = sq % 8, rank = sq / 8;
        return blackAtBottom ? (7 - file, rank) : (file, 7 - rank);
    }

    public static int ToSquare(int col, int row, bool blackAtBottom) =>
        blackAtBottom ? row * 8 + (7 - col) : (7 - row) * 8 + col;

    // a1 is dark, so a square is light when file + rank is odd. This is a property of the square,
    // not of the view, so the colours stay right whichever way the board faces.
    public static bool IsLightSquare(int sq) => (sq % 8 + sq / 8) % 2 == 1;
}
