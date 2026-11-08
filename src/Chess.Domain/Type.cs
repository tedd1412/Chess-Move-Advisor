namespace Chess.Domain;

// Enums for types (abstraction).
public enum Color { White, Black }
public enum PieceType { Pawn, Knight, Bishop, Rook, Queen, King }

// Record for piece.
public readonly record struct Piece(PieceType Type, Color Color)
{
    public bool IsNone => false;
}

// Note: Enums provide clear, type-safe values.