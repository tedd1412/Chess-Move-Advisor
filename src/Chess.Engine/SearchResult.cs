using System.Collections.Generic;
using Chess.Domain;

namespace Chess.Engine;

// Record for search results (immutable, value-like).
public sealed record SearchResult(
    Move BestMove,
    int ScoreCp,
    int Depth,
    long Nodes,
    double Seconds = 0,
    IReadOnlyList<Move>? PV = null);