using System.Collections.Generic;
using System.Linq;

namespace Align.Engine
{
    public sealed class Placement
    {
        public Tile Tile { get; }
        public Pos Pos { get; }
        public Placement(Tile tile, Pos pos) { Tile = tile; Pos = pos; }
        public override string ToString() => $"{Tile.Code}@{Pos}";
    }

    public enum Orientation { Horizontal, Vertical }

    public sealed class ScoredWord
    {
        public Orientation Orientation { get; internal set; }
        public List<Pos> Positions { get; } = new List<Pos>();
        public List<Tile> Tiles { get; } = new List<Tile>();
        public List<Trait> SharedTraits { get; internal set; }

        /// <summary>Points per tile in this word, in order, after premiums.</summary>
        public List<int> TilePoints { get; } = new List<int>();

        public int Points => TilePoints.Sum();

        public override string ToString() =>
            $"{string.Join("-", Tiles.Select(t => t.Code))} ({string.Join("+", SharedTraits)}) = {Points}";
    }

    public sealed class MoveResult
    {
        public bool Legal { get; internal set; }
        public string Error { get; internal set; }
        public List<ScoredWord> Words { get; } = new List<ScoredWord>();
        public int Score => Words.Sum(w => w.Points);

        internal static MoveResult Fail(string error) => new MoveResult { Legal = false, Error = error };
    }

    /// <summary>
    /// Pure rules check + scoring for a set of placements against a board. Doesn't know about
    /// hands or turns (Game does that), so the UI and a future AI can call it freely.
    /// </summary>
    public static class MoveEvaluator
    {
        public static MoveResult Evaluate(Board board, IReadOnlyList<Placement> placements, GameRules rules)
        {
            if (placements == null || placements.Count == 0)
                return MoveResult.Fail("Place at least one tile.");

            var newTiles = new Dictionary<Pos, Tile>();
            var tileIds = new HashSet<int>();
            foreach (var p in placements)
            {
                if (!p.Pos.InBounds) return MoveResult.Fail($"{p.Pos} is off the board.");
                if (board[p.Pos] != null) return MoveResult.Fail($"{p.Pos} is already occupied.");
                if (newTiles.ContainsKey(p.Pos)) return MoveResult.Fail($"Two tiles placed on {p.Pos}.");
                if (!tileIds.Add(p.Tile.Id)) return MoveResult.Fail($"{p.Tile.Code} placed twice.");
                newTiles[p.Pos] = p.Tile;
            }

            // One row or one column. Gaps are allowed.
            bool sameRow = placements.All(p => p.Pos.Row == placements[0].Pos.Row);
            bool sameCol = placements.All(p => p.Pos.Col == placements[0].Pos.Col);
            if (!sameRow && !sameCol)
                return MoveResult.Fail("All tiles in a play must be in one row or one column.");

            if (board.IsEmpty)
            {
                if (rules.FirstPlayMustCoverStart && !newTiles.ContainsKey(Board.Start))
                    return MoveResult.Fail($"The first play must cover START ({Board.Start}).");
            }
            else if (rules.PlayMustTouchBoard)
            {
                bool touches = placements.Any(p => Neighbors(p.Pos).Any(n => board[n] != null));
                if (!touches) return MoveResult.Fail("A play must touch a tile already on the board.");
            }

            Tile At(Pos q) => newTiles.TryGetValue(q, out var t) ? t : board[q];

            // Every line through a new tile must be a single tile or a legal word.
            var result = new MoveResult { Legal = true };
            var seen = new HashSet<(Pos, Orientation)>();
            foreach (var p in placements)
            {
                foreach (var orient in new[] { Orientation.Horizontal, Orientation.Vertical })
                {
                    int dr = orient == Orientation.Vertical ? 1 : 0;
                    int dc = orient == Orientation.Horizontal ? 1 : 0;

                    var start = p.Pos;
                    while (true)
                    {
                        var prev = start.Offset(-dr, -dc);
                        if (!prev.InBounds || At(prev) == null) break;
                        start = prev;
                    }
                    if (!seen.Add((start, orient))) continue;

                    var word = new ScoredWord { Orientation = orient };
                    for (var q = start; q.InBounds && At(q) != null; q = q.Offset(dr, dc))
                    {
                        word.Positions.Add(q);
                        word.Tiles.Add(At(q));
                    }
                    if (word.Tiles.Count == 1) continue;

                    string line = string.Join("-", word.Tiles.Select(t => t.Code));
                    if (word.Tiles.Count < rules.MinWordLength)
                        return MoveResult.Fail(
                            $"{line} ({word.Positions[0]}) is only {word.Tiles.Count} tiles; touching tiles must form a word of {rules.MinWordLength}+.");

                    word.SharedTraits = Tile.SharedTraits(word.Tiles);
                    if (word.SharedTraits.Count == 0)
                        return MoveResult.Fail($"{line} ({word.Positions[0]}) doesn't share a color, symbol, or number.");

                    for (int i = 0; i < word.Tiles.Count; i++)
                    {
                        var tile = word.Tiles[i];
                        var q = word.Positions[i];
                        word.TilePoints.Add(newTiles.ContainsKey(q)
                            ? ApplyPremium(tile.BaseValue(rules), Board.PremiumAt(q))
                            : tile.BaseValue(rules));
                    }
                    result.Words.Add(word);
                }
            }
            return result;
        }

        /// <summary>Premiums change a tile's value only on the turn it is placed there.</summary>
        public static int ApplyPremium(int value, Premium premium)
        {
            switch (premium)
            {
                case Premium.Plus2: return value + 2;
                case Premium.Times2: return value * 2;
                case Premium.Times4: return value * 4;
                default: return value;
            }
        }

        private static IEnumerable<Pos> Neighbors(Pos p)
        {
            foreach (var n in new[] { p.Offset(-1, 0), p.Offset(1, 0), p.Offset(0, -1), p.Offset(0, 1) })
                if (n.InBounds) yield return n;
        }
    }
}
