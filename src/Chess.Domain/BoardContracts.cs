namespace Chess.Domain;

// Interface for board (lessons: interfaces for polymorphism and flexible interactions).
public interface IBoard
{
    Color SideToMove { get; }
    int HalfmoveClock { get; }
    int FullmoveNumber { get; }

    Piece? GetPieceAt(int square0to63);
    void MakeMove(Move m);
    void UnmakeMove(Move m);
}

// Interfaces enable polymorphism, allowing different board implementations if needed, and loose coupling.