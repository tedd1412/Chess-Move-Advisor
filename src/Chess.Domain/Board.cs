using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Chess.Domain;

// This class represents the chessboard, encapsulating the state and behavior of the board (lessons: encapsulation, attributes and behaviors).
// It uses private fields for data hiding and public methods/properties for controlled access (lessons: private/protected/public access, properties).
// The class handles special moves like castling and en passant, demonstrating abstraction by focusing on essential aspects.
public sealed class Board : IBoard
{
    // Private field to store board squares (encapsulation: prevents direct manipulation).
    private readonly Piece?[] _squares = new Piece?[64]; // null means empty square.

    // Stack for move history, using composition to manage undo states (lesson: composition).
    private readonly Stack<MoveState> _history = new();

    // Public properties for board state (lessons: attributes, access modifiers).
    public Color SideToMove { get; private set; } = Color.White;
    public int HalfmoveClock { get; private set; } = 0;
    public int FullmoveNumber { get; private set; } = 1;

    // Castling rights as bitmask (abstraction: simplifies rights management).
    public int CastleRights { get; private set; } = 0b1111; // 1=K, 2=Q, 4=k, 8=q
    public const int WhiteKingSide  = 1 << 0; // 0001
    public const int WhiteQueenSide = 1 << 1; // 0010
    public const int BlackKingSide  = 1 << 2; // 0100
    public const int BlackQueenSide = 1 << 3; // 1000
    public bool CanCastleWhiteKingSide => (CastleRights & WhiteKingSide) != 0;
    public bool CanCastleWhiteQueenSide => (CastleRights & WhiteQueenSide) != 0;
    public bool CanCastleBlackKingSide => (CastleRights & BlackKingSide) != 0;
    public bool CanCastleBlackQueenSide => (CastleRights & BlackQueenSide) != 0;
    
    // En passant target square.
    public int? EnPassantSquare { get; private set; }

    // Private struct for move state (high cohesion: groups related data).
    private readonly struct MoveState(
        Piece? captured,
        int halfmove,
        int fullmove,
        Color stm,
        int castle,
        int? ep)
    {
        public Piece? Captured { get; } = captured;
        public int Halfmove { get; } = halfmove;
        public int Fullmove { get; } = fullmove;
        public Color SideToMove { get; } = stm;
        public int CastleRights { get; } = castle;
        public int? EnPassant { get; } = ep;
    }

    // Public method to get piece at square (abstraction: hides internal array).
    public Piece? GetPieceAt(int sq) => _squares[sq];

    // Method to load starting position (constructor-like initialization without explicit constructor, as .NET provides default if none defined).
    public void LoadStartPosition()
    {
        Array.Fill(_squares, null);

        // Set pawns (behavior: actions the board performs).
        for (int f = 0; f < 8; f++) { _squares[8 + f] = new Piece(PieceType.Pawn, Color.White); }
        for (int f = 0; f < 8; f++) { _squares[48 + f] = new Piece(PieceType.Pawn, Color.Black); }

        // Set back ranks.
        var back = new[] { PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen, PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook };
        for (int f = 0; f < 8; f++) _squares[f] = new Piece(back[f], Color.White);
        for (int f = 0; f < 8; f++) _squares[56 + f] = new Piece(back[f], Color.Black);

        SideToMove = Color.White;
        HalfmoveClock = 0;
        FullmoveNumber = 1;
        CastleRights = 0b1111;
        EnPassantSquare = null;
        _history.Clear();
    }

    // Method to make a move (behavior, uses polymorphism for different move types via flags).
    public void MakeMove(Move m)
    {
        Debug.Assert(m.From is >= 0 and < 64 && m.To is >= 0 and < 64);
        var moving = _squares[m.From];
        var captured = m.IsEnPassant ? _squares[EpCapturedSquare(m)] : _squares[m.To];

        _history.Push(new MoveState(captured, HalfmoveClock, FullmoveNumber, SideToMove, CastleRights, EnPassantSquare));

        _squares[m.To] = moving;
        _squares[m.From] = null;

        if (m.IsEnPassant && captured is not null)
            _squares[EpCapturedSquare(m)] = null;

        if (m.IsCastleKingSide) MoveRookForCastle(true);
        if (m.IsCastleQueenSide) MoveRookForCastle(false);

        if (m.Promotion is { } promo && moving is { } p)
            _squares[m.To] = new Piece(promo, p.Color);

        HalfmoveClock = (captured is not null || moving?.Type == PieceType.Pawn) ? 0 : HalfmoveClock + 1;

        UpdateCastleRights(m, moving, captured);
        UpdateEnPassantSquare(m, moving);

        if (SideToMove == Color.Black) FullmoveNumber++;
        SideToMove = SideToMove == Color.White ? Color.Black : Color.White;
    }

    // Method to unmake a move (reversible action, command pattern inspiration).
    public void UnmakeMove(Move m)
    {
        var st = _history.Pop();

        var moved = _squares[m.To];

        if (m.Promotion is not null && moved is { } promoted)
            moved = new Piece(PieceType.Pawn, promoted.Color);

        if (m.IsCastleKingSide || m.IsCastleQueenSide)
            UnmoveRookForCastle(m.IsCastleKingSide);

        _squares[m.From] = moved;

        if (m.IsEnPassant && st.Captured is not null)
        {
            _squares[m.To] = null;
            _squares[EpCapturedSquare(m)] = st.Captured;
        }
        else
        {
            _squares[m.To] = st.Captured;
        }

        HalfmoveClock = st.Halfmove;
        FullmoveNumber = st.Fullmove;
        SideToMove = st.SideToMove;
        CastleRights = st.CastleRights;
        EnPassantSquare = st.EnPassant;
    }

    // Helper methods (private for encapsulation).
    private static int EpCapturedSquare(Move m)
    {
        int dir = m.IsWhiteMove ? -8 : +8;
        return m.To + dir;
    }

    private void MoveRookForCastle(bool kingSide)
    {
        var color = SideToMove;
        if (color == Color.White)
        {
            if (kingSide) { _squares[5] = _squares[7]; _squares[7] = null; }
            else { _squares[3] = _squares[0]; _squares[0] = null; }
        }
        else
        {
            if (kingSide) { _squares[61] = _squares[63]; _squares[63] = null; }
            else { _squares[59] = _squares[56]; _squares[56] = null; }
        }
    }

    private void UnmoveRookForCastle(bool kingSide)
    {
        var color = SideToMove;
        if (color == Color.White)
        {
            if (kingSide) { _squares[7] = _squares[5]; _squares[5] = null; }
            else { _squares[0] = _squares[3]; _squares[3] = null; }
        }
        else
        {
            if (kingSide) { _squares[63] = _squares[61]; _squares[61] = null; }
            else { _squares[56] = _squares[59]; _squares[59] = null; }
        }
    }

    private void UpdateCastleRights(Move m, Piece? moving, Piece? captured)
    {
        if (moving?.Type == PieceType.King)
        {
            if (moving.Value.Color == Color.White) CastleRights &= ~(1 | 2);
            else CastleRights &= ~(4 | 8);
        }

        if (moving?.Type == PieceType.Rook)
        {
            if (moving.Value.Color == Color.White)
            {
                if (m.From == 0) CastleRights &= ~2;
                if (m.From == 7) CastleRights &= ~1;
            }
            else
            {
                if (m.From == 56) CastleRights &= ~8;
                if (m.From == 63) CastleRights &= ~4;
            }
        }

        if (captured?.Type == PieceType.Rook)
        {
            if (captured.Value.Color == Color.White)
            {
                if (m.To == 0) CastleRights &= ~2;
                if (m.To == 7) CastleRights &= ~1;
            }
            else
            {
                if (m.To == 56) CastleRights &= ~8;
                if (m.To == 63) CastleRights &= ~4;
            }
        }
    }

    private void UpdateEnPassantSquare(Move m, Piece? moving)
    {
        EnPassantSquare = null;
        if (moving?.Type == PieceType.Pawn)
        {
            int fromRank = m.From / 8;
            int toRank = m.To / 8;
            if (Math.Abs(toRank - fromRank) == 2)
            {
                EnPassantSquare = (m.From + m.To) / 2;
            }
        }
    }

    // Check methods (behavior).
    public bool IsInCheck(Color color)
    {
        int kingSq = -1;
        for (int i = 0; i < 64; i++)
        {
            var p = _squares[i];
            if (p is { Type: PieceType.King } && p.Value.Color == color) { kingSq = i; break; }
        }
        return IsSquareAttacked(kingSq, Opp(color));
    }

    public bool IsSquareAttacked(int sq, Color by)
    {
        // Knight attacks.
        foreach (var d in KnightD)
        {
            int to = sq + d;
            if (!OnBoard(to) || KnightWrap(sq, to)) continue;
            var p = _squares[to];
            if (p is { Type: PieceType.Knight } && p.Value.Color == by) return true;
        }

        // King attacks.
        foreach (var d in KingD)
        {
            int to = sq + d;
            if (!OnBoard(to) || FileWrap(sq, to)) continue;
            var p = _squares[to];
            if (p is { Type: PieceType.King } && p.Value.Color == by) return true;
        }

        // Pawn attacks.
        int dir = (by == Color.White) ? +8 : -8;
        foreach (var df in new[] { -1, +1 })
        {
            int to = sq - dir + df;
            if (!OnBoard(to) || Math.Abs((to % 8) - (sq % 8)) != 1) continue;
            var p = _squares[to];
            if (p is { Type: PieceType.Pawn } && p.Value.Color == by) return true;
        }

        // Bishop/Queen diagonal.
        foreach (var d in BishopD)
        {
            int t = sq + d;
            while (OnBoard(t) && !FileWrap(t - d, t))
            {
                var p = _squares[t];
                if (p is not null)
                {
                    if (p.Value.Color == by && (p.Value.Type == PieceType.Bishop || p.Value.Type == PieceType.Queen)) return true;
                    break;
                }
                t += d;
            }
        }

        // Rook/Queen orthogonal.
        foreach (var d in RookD)
        {
            int t = sq + d;
            while (OnBoard(t) && !FileWrap(t - d, t))
            {
                var p = _squares[t];
                if (p is not null)
                {
                    if (p.Value.Color == by && (p.Value.Type == PieceType.Rook || p.Value.Type == PieceType.Queen)) return true;
                    break;
                }
                t += d;
            }
        }
        return false;
    }

    private static Color Opp(Color c) => c == Color.White ? Color.Black : Color.White;

    private static readonly int[] KnightD = { +17, +15, +10, +6, -6, -10, -15, -17 };
    private static readonly int[] KingD = { +8, +9, +1, -7, -8, -9, -1, +7 };
    private static readonly int[] BishopD = { +9, +7, -7, -9 };
    private static readonly int[] RookD = { +8, -8, +1, -1 };

    private static bool OnBoard(int sq) => sq >= 0 && sq < 64;
    private static bool FileWrap(int a, int b) => Math.Abs((a % 8) - (b % 8)) > 1;
    private static bool KnightWrap(int a, int b) => Math.Abs((a % 8) - (b % 8)) > 2;

    // Public mutators for UI/tests (controlled access).
    public void Clear()
    {
        Array.Fill(_squares, null);
        _history.Clear();
        HalfmoveClock = 0;
        FullmoveNumber = 1;
        SideToMove = Color.White;
        CastleRights = 0b1111;
        EnPassantSquare = null;
    }

    public void SetPieceAt(int sq, Piece? piece) => _squares[sq] = piece;
    public void SetSideToMove(Color c) => SideToMove = c;

    // Clone method (creates deep copy, loose coupling by not depending on external cloning).
    public Board Clone()
    {
        var b = new Board();
        for (int i = 0; i < 64; i++) b._squares[i] = _squares[i];
        b.SideToMove = SideToMove;
        b.HalfmoveClock = HalfmoveClock;
        b.FullmoveNumber = FullmoveNumber;
        b.CastleRights = CastleRights;
        b.EnPassantSquare = EnPassantSquare;
        return b;
    }

    // FEN methods (serialization, abstraction).
    public string ToFen()
    {
        var parts = new List<string>(6);
        for (int r = 7; r >= 0; r--)
        {
            int empty = 0;
            var row = "";
            for (int f = 0; f < 8; f++)
            {
                var p = _squares[r * 8 + f];
                if (p is null) { empty++; continue; }
                if (empty > 0) { row += empty.ToString(); empty = 0; }
                row += PieceToFen(p.Value);
            }
            if (empty > 0) row += empty.ToString();
            parts.Add(row);
        }
        string boardFen = string.Join("/", parts);

        string stm = SideToMove == Color.White ? "w" : "b";

        string cr = "";
        if ((CastleRights & 1) != 0) cr += "K";
        if ((CastleRights & 2) != 0) cr += "Q";
        if ((CastleRights & 4) != 0) cr += "k";
        if ((CastleRights & 8) != 0) cr += "q";
        if (cr == "") cr = "-";

        string ep = EnPassantSquare is int s ? SqName(s) : "-";

        return $"{boardFen} {stm} {cr} {ep} {HalfmoveClock} {FullmoveNumber}";
    }

    public void LoadFen(string fen)
    {
        var sp = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (sp.Length < 4) throw new ArgumentException("FEN too short");
        Array.Fill(_squares, null);

        var ranks = sp[0].Split('/');
        if (ranks.Length != 8) throw new ArgumentException("FEN ranks != 8");
        for (int r = 7; r >= 0; r--)
        {
            int file = 0;
            foreach (char c in ranks[7 - r])
            {
                if (char.IsDigit(c)) { file += (c - '0'); }
                else
                {
                    _squares[r * 8 + file] = FenToPiece(c);
                    file++;
                }
            }
            if (file != 8) throw new ArgumentException("FEN row not length 8");
        }

        SideToMove = sp[1] == "w" ? Color.White : Color.Black;

        CastleRights = 0;
        if (sp[2] != "-")
        {
            if (sp[2].Contains('K')) CastleRights |= 1;
            if (sp[2].Contains('Q')) CastleRights |= 2;
            if (sp[2].Contains('k')) CastleRights |= 4;
            if (sp[2].Contains('q')) CastleRights |= 8;
        }

        EnPassantSquare = sp[3] == "-" ? null : ParseSq(sp[3]);

        HalfmoveClock = (sp.Length > 4) ? int.Parse(sp[4]) : 0;
        FullmoveNumber = (sp.Length > 5) ? int.Parse(sp[5]) : 1;

        _history.Clear();
    }

    private static string SqName(int sq)
    {
        string files = "abcdefgh";
        int f = sq % 8; int r = sq / 8;
        return $"{files[f]}{r + 1}";
    }

    private static int ParseSq(string s)
    {
        if (string.IsNullOrEmpty(s) || s.Length != 2) throw new ArgumentException($"Bad square '{s}'");
        const string files = "abcdefgh";
        int f = files.IndexOf(s[0]);
        if (f < 0) throw new ArgumentException($"Bad file '{s[0]}' in square '{s}'");
        if (!char.IsDigit(s[1])) throw new ArgumentException($"Bad rank '{s[1]}' in square '{s}'");
        int r = s[1] - '1';
        if (r < 0 || r > 7) throw new ArgumentException($"Bad rank '{s[1]}' in square '{s}'");
        return r * 8 + f;
    }

    private static char PieceToFen(Piece p)
    {
        char c = p.Type switch
        {
            PieceType.Pawn => 'p',
            PieceType.Knight => 'n',
            PieceType.Bishop => 'b',
            PieceType.Rook => 'r',
            PieceType.Queen => 'q',
            _ => 'k'
        };
        return p.Color == Color.White ? char.ToUpperInvariant(c) : c;
    }

    private static Piece FenToPiece(char c)
    {
        var color = char.IsUpper(c) ? Color.White : Color.Black;
        var t = char.ToLowerInvariant(c) switch
        {
            'p' => PieceType.Pawn,
            'n' => PieceType.Knight,
            'b' => PieceType.Bishop,
            'r' => PieceType.Rook,
            'q' => PieceType.Queen,
            'k' => PieceType.King,
            _ => throw new ArgumentException($"Bad FEN piece '{c}'")
        };
        return new Piece(t, color);
    }
}

// Note: This class applies lessons like encapsulation (private fields, public properties), abstraction (FEN handling ignores unimportant details), high cohesion (all board-related logic in one class), loose coupling (interfaces for dependencies), polymorphism (move handling via flags), and exceptions for invalid FEN.