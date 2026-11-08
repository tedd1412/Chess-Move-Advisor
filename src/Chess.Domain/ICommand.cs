namespace Chess.Domain;

// Interface for command pattern (lesson: command design pattern for reversible actions).
public interface ICommand
{
    void Execute();
    void Unexecute();
}

// Note: Enables polymorphism for different commands, used for moves.