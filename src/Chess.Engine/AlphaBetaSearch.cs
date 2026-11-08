using System;
using System.Diagnostics;
using Chess.Domain;

namespace Chess.Engine;

// Search engine class (implements interface for polymorphism).
public sealed class AlphaBetaSearch : ISearchEngine
{
    private readonly IMoveGenerator _gen; // Dependency injection via constructor.
    private readonly IEvaluator _eval;
    private readonly bool _useQuiescence;
    private long _nodes;

    // Constructor initializes object.
    public AlphaBetaSearch(IMoveGenerator g, IEvaluator e, bool useQuiescence = true)
    {
        _gen = g;
        _eval = e;
        _useQuiescence = useQuiescence;
    }

    public SearchResult Analyse(IBoard board, int maxDepth)
    {
        _nodes = 0;
        Move bestMove = default;
        int bestScore = 0;
        var timer = Stopwatch.StartNew();

        for (int depth = 1; depth <= maxDepth; depth++)
        {
            (bestScore, bestMove) = SearchRoot((Board)board, depth);
        }

        timer.Stop();
        return new SearchResult(
        BestMove: bestMove,
        ScoreCp: bestScore,
        Depth: maxDepth,
        Nodes: _nodes,
        Seconds: timer.Elapsed.TotalSeconds
        );
    }

    // Private methods for search (encapsulation).
    private (int, Move) SearchRoot(Board b, int depth)
    {
        Span<Move> moves = stackalloc Move[256];
        int count = _gen.GenerateMoves(b, moves);
        Array.Sort(moves[..count].ToArray(), (a, c) => (c.IsCapture ? 1 : 0) - (a.IsCapture ? 1 : 0));

        int best = int.MinValue;
        Move bestMove = moves[0];

        for (int i = 0; i < count; i++)
        {
            b.MakeMove(moves[i]);
            int score = -AlphaBeta(b, depth - 1, -1000000, 1000000);
            b.UnmakeMove(moves[i]);

            if (score > best)
            {
                best = score;
                bestMove = moves[i];
            }
        }
        return (best, bestMove);
    }

    private int AlphaBeta(Board b, int depth, int alpha, int beta)
    {
        _nodes++;
        if (depth <= 0)
            return _useQuiescence ? Quiesce(b, alpha, beta) : _eval.Evaluate(b);

        Span<Move> buf = stackalloc Move[256];
        int n = _gen.GenerateMoves(b, buf);
        if (n == 0) return _eval.Evaluate(b);

        for (int i = 0; i < n; i++)
        {
            b.MakeMove(buf[i]);
            int score = -AlphaBeta(b, depth - 1, -beta, -alpha);
            b.UnmakeMove(buf[i]);

            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }
        return alpha;
    }

    private int Quiesce(Board b, int alpha, int beta)
    {
        int stand = _eval.Evaluate(b);
        if (stand >= beta) return beta;
        if (alpha < stand) alpha = stand;

        Span<Move> buf = stackalloc Move[256];
        int n = _gen.GenerateMoves(b, buf);

        for (int i = 0; i < n; i++)
        {
            if (!buf[i].IsCapture) continue;
            b.MakeMove(buf[i]);
            int score = -Quiesce(b, -beta, -alpha);
            b.UnmakeMove(buf[i]);

            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }
        return alpha;
    }
}

// Note: Implements ISearchEngine (interface), constructor injection (dependency), high cohesion (search logic), polymorphism via evaluator.