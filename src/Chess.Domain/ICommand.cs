namespace Chess.Domain;

// Interface for command pattern.
public interface ICommand
{
    void Execute();
    void Unexecute();
}

// Note: Enables polymorphism for different commands, used for moves.