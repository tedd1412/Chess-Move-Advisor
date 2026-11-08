using System.Collections.Generic;
using Chess.Domain;

namespace Chess.Engine;

// Result of a search. ScoreCp is from the side to move's view. When the position has no legal
// moves, BestMove is default (HasBestMove is false) and ScoreCp is -MateScore (checkmate) or 0
// (stalemate).
public sealed record SearchResult(
    Move BestMove,
    int ScoreCp,
    int Depth,
    long Nodes,
    double Seconds = 0,
    IReadOnlyList<Move>? PV = null)
{
    public bool HasBestMove => BestMove != default;

    public bool IsMate => AlphaBetaSearch.IsMateScore(ScoreCp);

    // Moves (not plies) until mate: positive if the side to move mates, negative if it is mated.
    public int MateInMoves => !IsMate ? 0
        : ScoreCp > 0 ? (AlphaBetaSearch.MateScore - ScoreCp + 1) / 2
        : -(AlphaBetaSearch.MateScore + ScoreCp) / 2;
}
