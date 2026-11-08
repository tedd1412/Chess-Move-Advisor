using Chess.Domain;
using Chess.Engine;

namespace Chess.Engine;

// Factory pattern to create engine components.
public static class EngineFactory
{
    public static (IMoveGenerator, IEvaluator, ISearchEngine) Create(string mode)
    {
        IMoveGenerator gen = new LegalMoveGenerator(); // Aggregation: generator contained.
        IEvaluator eval;
        ISearchEngine engine;

        switch (mode.ToLower())
        {
            case "fast":
                eval = new MaterialEvaluator();
                engine = new AlphaBetaSearch(gen, eval, false);
                break;

            case "accurate":
                eval = new HeuristicEvaluator();
                engine = new AlphaBetaSearch(gen, eval, true);
                break;

            default:
                eval = new HeuristicEvaluator();
                engine = new AlphaBetaSearch(gen, eval);
                break;
        }
        return (gen, eval, engine);
    }
}