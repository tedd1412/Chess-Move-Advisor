namespace Chess.Domain;

// Record for move (immutable, value type behavior).
public readonly record struct Move(
    int From, int To,
    PieceType? Promotion = null,
    bool IsCapture = false,
    bool IsCastleKingSide = false,
    bool IsCastleQueenSide = false,
    bool IsEnPassant = false);

// Note: Encapsulates move data (attributes), immutable for safety.