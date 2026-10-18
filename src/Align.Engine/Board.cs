using System;

namespace Align.Engine
{
    public enum Premium { None, Start, Plus2, Times2, Times4 }

    /// <summary>
    /// A board square. Row 0 is the top row, Col 0 the left column.
    /// Displayed as column letter + row number: A1 is top-left, F6 is START, K11 bottom-right.
    /// </summary>
    public readonly struct Pos : IEquatable<Pos>
    {
        public int Row { get; }
        public int Col { get; }

        public Pos(int row, int col) { Row = row; Col = col; }

        public bool InBounds => Row >= 0 && Row < Board.Size && Col >= 0 && Col < Board.Size;

        public Pos Offset(int dRow, int dCol) => new Pos(Row + dRow, Col + dCol);

        public bool Equals(Pos other) => Row == other.Row && Col == other.Col;
        public override bool Equals(object obj) => obj is Pos p && Equals(p);
        public override int GetHashCode() => Row * 31 + Col;
        public static bool operator ==(Pos a, Pos b) => a.Equals(b);
        public static bool operator !=(Pos a, Pos b) => !a.Equals(b);

        public override string ToString() => $"{(char)('A' + Col)}{Row + 1}";

        /// <summary>Parses "F6" / "f6" style coordinates. Returns false if malformed or off-board.</summary>
        public static bool TryParse(string s, out Pos pos)
        {
            pos = default;
            if (string.IsNullOrWhiteSpace(s) || s.Length < 2) return false;
            char letter = char.ToUpperInvariant(s[0]);
            if (letter < 'A' || letter >= 'A' + Board.Size) return false;
            if (!int.TryParse(s.Substring(1), out int row)) return false;
            pos = new Pos(row - 1, letter - 'A');
            return pos.InBounds;
        }
    }

    public sealed class Board
    {
        public const int Size = 11;
        public static readonly Pos Start = new Pos(5, 5);

        // Layout from Board v1.0.png. Symmetric under 90-degree rotation.
        private static readonly Premium[,] Layout = BuildLayout();

        private readonly Tile[,] _cells = new Tile[Size, Size];

        public int TileCount { get; private set; }
        public bool IsEmpty => TileCount == 0;

        public Tile this[Pos p] => _cells[p.Row, p.Col];
        public Tile this[int row, int col] => _cells[row, col];

        public static Premium PremiumAt(Pos p) => Layout[p.Row, p.Col];

        internal void Place(Tile tile, Pos p)
        {
            if (_cells[p.Row, p.Col] != null) throw new InvalidOperationException($"{p} is occupied.");
            _cells[p.Row, p.Col] = tile;
            TileCount++;
        }

        public Board Clone()
        {
            var b = new Board();
            Array.Copy(_cells, b._cells, _cells.Length);
            b.TileCount = TileCount;
            return b;
        }

        private static Premium[,] BuildLayout()
        {
            var l = new Premium[Size, Size];
            void Set(int r, int c, Premium p) => l[r, c] = p;

            Set(5, 5, Premium.Start);

            // x2 corners
            Set(0, 0, Premium.Times2); Set(0, 10, Premium.Times2);
            Set(10, 0, Premium.Times2); Set(10, 10, Premium.Times2);

            // x2 ring around START
            Set(4, 4, Premium.Times2); Set(4, 6, Premium.Times2);
            Set(6, 4, Premium.Times2); Set(6, 6, Premium.Times2);

            // x4
            Set(2, 2, Premium.Times4); Set(2, 8, Premium.Times4);
            Set(8, 2, Premium.Times4); Set(8, 8, Premium.Times4);

            // +2
            Set(1, 5, Premium.Plus2); Set(9, 5, Premium.Plus2);
            Set(5, 1, Premium.Plus2); Set(5, 9, Premium.Plus2);

            return l;
        }
    }
}
