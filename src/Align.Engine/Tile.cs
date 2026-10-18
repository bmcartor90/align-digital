using System.Collections.Generic;

namespace Align.Engine
{
    public enum TileColor { Red, Green, Blue, Black, Purple, Yellow }

    public enum TileSymbol { Diamond, Oval, Squiggle, X, Circle, Heart }

    public enum Trait { Color, Symbol, Number }

    /// <summary>One physical tile. Identity is by Id: two Red Oval 2s are different tiles.</summary>
    public sealed class Tile
    {
        public int Id { get; }
        public TileColor Color { get; }
        public TileSymbol Symbol { get; }
        public int Number { get; }

        /// <summary>Starred tiles carry one trait value no other tile has, and score a flat value.</summary>
        public bool Starred { get; }

        public Tile(int id, TileColor color, TileSymbol symbol, int number, bool starred = false)
        {
            Id = id;
            Color = color;
            Symbol = symbol;
            Number = number;
            Starred = starred;
        }

        /// <summary>Points before any premium square: printed number, or the flat starred value.</summary>
        public int BaseValue(GameRules rules) => Starred ? rules.StarredTileValue : Number;

        /// <summary>Short code, e.g. R2D = Red 2 Diamond, K3D = Black 3 Diamond.</summary>
        public string Code => $"{ColorLetter(Color)}{Number}{SymbolLetter(Symbol)}";

        public string Name => $"{Color} {Symbol} {Number}" + (Starred ? " (starred)" : "");

        public override string ToString() => Code;

        public static char ColorLetter(TileColor c)
        {
            switch (c)
            {
                case TileColor.Red: return 'R';
                case TileColor.Green: return 'G';
                case TileColor.Blue: return 'B';
                case TileColor.Black: return 'K';
                case TileColor.Purple: return 'P';
                default: return 'Y';
            }
        }

        public static char SymbolLetter(TileSymbol s)
        {
            switch (s)
            {
                case TileSymbol.Diamond: return 'D';
                case TileSymbol.Oval: return 'O';
                case TileSymbol.Squiggle: return 'S';
                case TileSymbol.X: return 'X';
                case TileSymbol.Circle: return 'C';
                default: return 'H';
            }
        }

        /// <summary>The traits every tile in the line has in common (empty = not a word).</summary>
        public static List<Trait> SharedTraits(IReadOnlyList<Tile> tiles)
        {
            var shared = new List<Trait>();
            if (tiles.Count == 0) return shared;
            bool color = true, symbol = true, number = true;
            var first = tiles[0];
            for (int i = 1; i < tiles.Count; i++)
            {
                if (tiles[i].Color != first.Color) color = false;
                if (tiles[i].Symbol != first.Symbol) symbol = false;
                if (tiles[i].Number != first.Number) number = false;
            }
            if (color) shared.Add(Trait.Color);
            if (symbol) shared.Add(Trait.Symbol);
            if (number) shared.Add(Trait.Number);
            return shared;
        }
    }

    public static class TileSet
    {
        public static readonly TileColor[] BaseColors = { TileColor.Red, TileColor.Green, TileColor.Blue };
        public static readonly TileSymbol[] BaseSymbols = { TileSymbol.Diamond, TileSymbol.Oval, TileSymbol.Squiggle };

        /// <summary>
        /// The physical set: every Red/Green/Blue x Diamond/Oval/Squiggle x 1-3 tile
        /// (rules.CopiesOfEachBaseTile copies, default 3 = 81), plus the 6 starred tiles once each.
        /// </summary>
        public static List<Tile> Create(GameRules rules)
        {
            var tiles = new List<Tile>();
            int id = 0;
            for (int copy = 0; copy < rules.CopiesOfEachBaseTile; copy++)
                foreach (var c in BaseColors)
                    foreach (var s in BaseSymbols)
                        for (int n = 1; n <= 3; n++)
                            tiles.Add(new Tile(id++, c, s, n));

            if (rules.IncludeStarredTiles)
            {
                tiles.Add(new Tile(id++, TileColor.Black, TileSymbol.Diamond, 3, true));
                tiles.Add(new Tile(id++, TileColor.Purple, TileSymbol.Oval, 1, true));
                tiles.Add(new Tile(id++, TileColor.Yellow, TileSymbol.Squiggle, 2, true));
                tiles.Add(new Tile(id++, TileColor.Blue, TileSymbol.X, 2, true));
                tiles.Add(new Tile(id++, TileColor.Green, TileSymbol.Circle, 3, true));
                tiles.Add(new Tile(id++, TileColor.Red, TileSymbol.Heart, 1, true));
            }
            return tiles;
        }
    }
}
