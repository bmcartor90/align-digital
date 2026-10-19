using System.Collections.Generic;
using System.Linq;
using Align.Engine;
using Xunit;

namespace Align.Engine.Tests
{
    public class BotTests
    {
        private static Game MidGame(int seed, int turns)
        {
            var g = new Game(new[] { "A", "B" }, new GameRules(), seed);
            var bots = new IBot[] { new GreedyBot(seed), new GreedyBot(seed + 1) };
            for (int i = 0; i < turns && !g.IsOver; i++)
            {
                var d = bots[g.CurrentPlayerIndex].Decide(g);
                if (d.Kind == DecisionKind.Play) Assert.True(g.Play(d.Placements).Legal);
                else if (d.Kind == DecisionKind.Swap) Assert.Null(g.Exchange(d.SwapTiles));
                else g.Pass();
            }
            return g;
        }

        /// <summary>Every legal play with no lone tiles, found the slow way.</summary>
        private static HashSet<string> BruteForce(Board board, List<Tile> hand, GameRules rules)
        {
            var found = new HashSet<string>();
            for (int orient = 0; orient < 2; orient++)
                for (int line = 0; line < Board.Size; line++)
                {
                    var cells = Enumerable.Range(0, Board.Size)
                        .Select(i => orient == 0 ? new Pos(line, i) : new Pos(i, line))
                        .Where(p => board[p] == null).ToList();
                    for (int k = 1; k <= hand.Count; k++)
                        foreach (var squares in Combinations(cells, k))
                            foreach (var tiles in Permutations(hand, k))
                            {
                                var ps = squares.Zip(tiles, (p, t) => new Placement(t, p)).ToList();
                                var r = MoveEvaluator.Evaluate(board, ps, rules);
                                if (!r.Legal) continue;
                                bool lone = ps.Any(p => !r.Words.Any(w => w.Positions.Contains(p.Pos)));
                                if (lone) continue;
                                found.Add(Key(ps));
                            }
                }
            return found;
        }

        private static string Key(IEnumerable<Placement> ps) =>
            string.Join(" ", ps.Select(p => $"{p.Tile.Code}{(p.Tile.Starred ? "*" : "")}@{p.Pos}").OrderBy(s => s));

        private static IEnumerable<List<T>> Combinations<T>(List<T> items, int k, int start = 0)
        {
            if (k == 0) { yield return new List<T>(); yield break; }
            for (int i = start; i <= items.Count - k; i++)
                foreach (var rest in Combinations(items, k - 1, i + 1))
                {
                    rest.Insert(0, items[i]);
                    yield return rest;
                }
        }

        private static IEnumerable<List<T>> Permutations<T>(List<T> items, int k)
        {
            if (k == 0) { yield return new List<T>(); yield break; }
            for (int i = 0; i < items.Count; i++)
            {
                var rest = items.Where((_, j) => j != i).ToList();
                foreach (var p in Permutations(rest, k - 1))
                {
                    p.Insert(0, items[i]);
                    yield return p;
                }
            }
        }

        [Theory]
        [InlineData(11, 0)]
        [InlineData(12, 3)]
        [InlineData(13, 6)]
        [InlineData(14, 10)]
        [InlineData(15, 16)]
        public void MoveFinder_MatchesBruteForce(int seed, int turns)
        {
            MoveFinder.Verify = true; // also checks every found play's score against MoveEvaluator
            var g = MidGame(seed, turns);
            var rules = g.Rules;
            var hand = g.CurrentPlayer.Hand.Take(4).ToList();

            var fast = MoveFinder.FindAll(g.Board, hand, rules).Select(m => Key(m.Placements)).ToHashSet();
            var slow = BruteForce(g.Board, hand, rules);

            Assert.True(slow.SetEquals(fast),
                $"Missing: {string.Join(" | ", slow.Except(fast).Take(5))}  Extra: {string.Join(" | ", fast.Except(slow).Take(5))}");
            Assert.Equal(slow.Count > 0, MoveFinder.AnyLegalPlay(g.Board, hand, rules));
        }

        [Fact]
        public void MoveFinder_FirstMoveCoversStart()
        {
            var g = new Game(new[] { "A", "B" }, new GameRules(), 5);
            var moves = MoveFinder.FindAll(g.Board, g.CurrentPlayer.Hand, g.Rules);
            Assert.All(moves, m => Assert.Contains(m.Placements, p => p.Pos == Board.Start));
        }

        [Fact]
        public void MoveFinder_NoPlayWhenNothingFits()
        {
            // A word of Red 1 Diamonds across row 6; hand of tiles that share nothing with it can't touch it.
            var g = Game.WithDrawOrder(new[] { "A" }, new GameRules { HandSize = 3 },
                new[] { new Tile(1, TileColor.Red, TileSymbol.Diamond, 1), new Tile(2, TileColor.Red, TileSymbol.Diamond, 1), new Tile(3, TileColor.Red, TileSymbol.Diamond, 1),
                        new Tile(4, TileColor.Blue, TileSymbol.Oval, 2), new Tile(5, TileColor.Green, TileSymbol.Squiggle, 3), new Tile(6, TileColor.Blue, TileSymbol.Squiggle, 3) });
            var h = g.CurrentPlayer.Hand.ToList();
            Assert.True(g.Play(new[] { new Placement(h[0], new Pos(5, 4)), new Placement(h[1], new Pos(5, 5)), new Placement(h[2], new Pos(5, 6)) }).Legal);
            // Hand is now B2O, G3S, B3S: nothing matches Red/Diamond/1, and no 3 of them alone can touch the board.
            Assert.False(g.CurrentPlayerHasLegalPlay());
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void BotGames_FinishAndStayConsistent(int players)
        {
            for (int seed = 0; seed < 8; seed++)
            {
                var names = Enumerable.Range(0, players).Select(i => $"P{i}").ToList();
                var g = new Game(names, new GameRules(), seed);
                var bots = Enumerable.Range(0, players).Select(i => (IBot)new GreedyBot(seed * 10 + i)).ToList();
                var log = BotRunner.PlayOut(g, bots);

                Assert.True(g.IsOver, $"seed {seed} didn't finish in {log.Count} turns");
                for (int p = 0; p < players; p++)
                    Assert.Equal(log.Where(t => t.Player == p).Sum(t => t.Score), g.Players[p].Score);
                Assert.Equal(log.Sum(t => t.TilesPlaced), g.Board.TileCount);
                Assert.Equal(87, g.Board.TileCount + g.BagCount + g.Players.Sum(x => x.Hand.Count));
            }
        }

        [Fact]
        public void GreedyBeatsRandom()
        {
            int greedyWins = 0;
            for (int seed = 0; seed < 20; seed++)
            {
                var g = new Game(new[] { "G", "R" }, new GameRules(), seed);
                IBot[] bots = seed % 2 == 0
                    ? new IBot[] { new GreedyBot(seed), new RandomBot(seed) }
                    : new IBot[] { new RandomBot(seed), new GreedyBot(seed) };
                BotRunner.PlayOut(g, bots);
                int gi = seed % 2 == 0 ? 0 : 1;
                if (g.Players[gi].Score > g.Players[1 - gi].Score) greedyWins++;
            }
            Assert.True(greedyWins >= 14, $"Greedy only won {greedyWins}/20");
        }
    }
}
