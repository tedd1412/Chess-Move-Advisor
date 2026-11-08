using Chess.Domain;

namespace Chess.Engine;

// Interfaces for engine components.
public interface IMoveGenerator
{
    int GenerateMoves(IBoard board, Span<Move> buffer);
}

public interface IEvaluator
{
    int Evaluate(IBoard board);
}

public interface ISearchEngine
{
    SearchResult Analyse(IBoard board, int depth);
}
