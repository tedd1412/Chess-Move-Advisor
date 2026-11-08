using System;
using System.Diagnostics;
using Chess.Domain;

namespace Chess.Engine;

// Negamax alpha-beta search with iterative deepening, MVV-LVA move ordering and an optional
// quiescence search. Scores are in centipawns from the side to move's view.
public sealed class AlphaBetaSearch : ISearchEngine
{
    // A mate is scored as MateScore minus the distance in plies, so shorter mates score higher.
    public const int MateScore = 100_000;
    private const int Infinity = 1_000_000;
    private const int MaxPly = 128;

    private readonly IMoveGenerator _gen; // Dependency injection via constructor.
    private readonly IEvaluator _eval;
    private readonly bool _useQuiescence;
    private long _nodes;

    public AlphaBetaSearch(IMoveGenerator g, IEvaluator e, bool useQuiescence = true)
    {
        _gen = g;
        _eval = e;
        _useQuiescence = useQuiescence;
    }

    public static bool IsMateScore(int score) => Math.Abs(score) >= MateScore - MaxPly;

    public SearchResult Analyse(IBoard board, int maxDepth)
    {
        var b = (Board)board;
        _nodes = 0;
        var timer = Stopwatch.StartNew();

        Span<Move> moves = stackalloc Move[256];
        int count = _gen.GenerateMoves(b, moves);

        // Game already over: report checkmate or stalemate instead of searching.
        if (count == 0)
        {
            int final = b.IsInCheck(b.SideToMove) ? -MateScore : 0;
            return new SearchResult(default, final, 0, 0, timer.Elapsed.TotalSeconds);
        }

        Move bestMove = moves[0];
        int bestScore = 0;
        int depthDone = 0;

        for (int depth = 1; depth <= Math.Max(1, maxDepth); depth++)
        {
            (bestScore, bestMove) = SearchRoot(b, moves[..count], depth, bestMove);
            depthDone = depth;
            if (IsMateScore(bestScore)) break; // a forced mate will not change with more depth
        }

        timer.Stop();
        return new SearchResult(
            BestMove: bestMove,
            ScoreCp: bestScore,
            Depth: depthDone,
            Nodes: _nodes,
            Seconds: timer.Elapsed.TotalSeconds);
    }

    private (int, Move) SearchRoot(Board b, Span<Move> moves, int depth, Move previousBest)
    {
        OrderMoves(b, moves, previousBest);

        int alpha = -Infinity;
        Move bestMove = moves[0];

        for (int i = 0; i < moves.Length; i++)
        {
            b.MakeMove(moves[i]);
            int score = -AlphaBeta(b, depth - 1, 1, -Infinity, -alpha);
            b.UnmakeMove(moves[i]);

            if (score > alpha)
            {
                alpha = score;
                bestMove = moves[i];
            }
        }
        return (alpha, bestMove);
    }

    private int AlphaBeta(Board b, int depth, int ply, int alpha, int beta)
    {
        _nodes++;

        if (b.HalfmoveClock >= 100) return 0; // fifty-move rule

        // Quiescence does its own checkmate/stalemate detection, so hand over before generating.
        if ((depth <= 0 || ply >= MaxPly) && _useQuiescence)
        {
            _nodes--; // Quiesce counts this node itself
            return Quiesce(b, ply, alpha, beta);
        }

        Span<Move> buf = stackalloc Move[256];
        int n = _gen.GenerateMoves(b, buf);
        if (n == 0)
            return b.IsInCheck(b.SideToMove) ? -MateScore + ply : 0; // checkmate or stalemate

        if (depth <= 0 || ply >= MaxPly) return _eval.Evaluate(b);

        var moves = buf[..n];
        OrderMoves(b, moves, default);

        for (int i = 0; i < n; i++)
        {
            b.MakeMove(moves[i]);
            int score = -AlphaBeta(b, depth - 1, ply + 1, -beta, -alpha);
            b.UnmakeMove(moves[i]);

            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }
        return alpha;
    }

    // Keeps searching captures at the leaves so the score is not taken in the middle of an
    // exchange. When in check, every evasion is searched (standing pat would be illegal).
    private int Quiesce(Board b, int ply, int alpha, int beta)
    {
        _nodes++;

        Span<Move> buf = stackalloc Move[256];
        int n = _gen.GenerateMoves(b, buf);
        bool inCheck = b.IsInCheck(b.SideToMove);

        if (n == 0) return inCheck ? -MateScore + ply : 0;
        if (ply >= MaxPly) return _eval.Evaluate(b);

        if (!inCheck)
        {
            int stand = _eval.Evaluate(b);
            if (stand >= beta) return beta;
            if (stand > alpha) alpha = stand;
        }

        var moves = buf[..n];
        OrderMoves(b, moves, default);

        for (int i = 0; i < n; i++)
        {
            if (!inCheck && !moves[i].IsCapture && moves[i].Promotion is null) continue;

            b.MakeMove(moves[i]);
            int score = -Quiesce(b, ply + 1, -beta, -alpha);
            b.UnmakeMove(moves[i]);

            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }
        return alpha;
    }

    // Sorts in place: the hinted move first, then captures by MVV-LVA (most valuable victim,
    // least valuable attacker), then promotions, then quiet moves.
    private static void OrderMoves(Board b, Span<Move> moves, Move first)
    {
        Span<int> keys = stackalloc int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
            keys[i] = OrderKey(b, moves[i], first);

        // Insertion sort, stable and fast enough for a few dozen moves.
        for (int i = 1; i < moves.Length; i++)
        {
            var m = moves[i];
            int k = keys[i];
            int j = i - 1;
            while (j >= 0 && keys[j] < k)
            {
                moves[j + 1] = moves[j];
                keys[j + 1] = keys[j];
                j--;
            }
            moves[j + 1] = m;
            keys[j + 1] = k;
        }
    }

    private static int OrderKey(Board b, Move m, Move first)
    {
        if (m == first && first != default) return int.MaxValue;

        int key = 0;
        if (m.IsCapture)
        {
            var victim = m.IsEnPassant ? PieceType.Pawn : b.GetPieceAt(m.To)?.Type ?? PieceType.Pawn;
            var attacker = b.GetPieceAt(m.From)?.Type ?? PieceType.Pawn;
            key += 100_000 + 10 * OrderValue(victim) - OrderValue(attacker);
        }
        if (m.Promotion is { } promo) key += 50_000 + OrderValue(promo);
        return key;
    }

    private static int OrderValue(PieceType t) => t == PieceType.King ? 2_000 : MaterialEvaluator.PieceValues[(int)t];
}
