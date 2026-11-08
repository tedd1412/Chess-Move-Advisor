namespace Chess.Domain;

// Command for moves (lesson: command design pattern).
public class MoveCommand : ICommand
{
    private readonly Board _board; // Aggregation.
    private readonly Move _move;

    public MoveCommand(Board board, Move move)
    {
        _board = board;
        _move = move;
    }

    public void Execute() => _board.MakeMove(_move);
    public void Unexecute() => _board.UnmakeMove(_move);
}

// Note: Implements ICommand interface (polymorphism), high cohesion (focuses on move execution).