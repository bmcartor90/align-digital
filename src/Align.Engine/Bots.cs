using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Align.Engine
{
    public enum DecisionKind { Play, Swap, Pass }

    public sealed class BotDecision
    {
        public DecisionKind Kind { get; }
        public List<Placement> Placements { get; }
        public List<Tile> SwapTiles { get; }

        private BotDecision(DecisionKind kind, List<Placement> placements, List<Tile> swap)
        {
            Kind = kind; Placements = placements; SwapTiles = swap;
        }

        public static BotDecision Play(List<Placement> p) => new BotDecision(DecisionKind.Play, p, null);
        public static BotDecision Swap(List<Tile> t) => new BotDecision(DecisionKind.Swap, null, t);
        public static BotDecision Pass() => new BotDecision(DecisionKind.Pass, null, null);
    }

    public interface IBot
    {
        string Name { get; }
        BotDecision Decide(Game game);
    }

    /// <summary>
    /// Shared behaviour: pick a play from all legal plays; when there is none, swap the whole hand
    /// (up to MaxStuckSwaps times in a row, so a locked board can still end), then pass.
    /// </summary>
    public abstract class BotBase : IBot
    {
        protected readonly Random Rng;
        private int _stuckSwaps;

        public int MaxStuckSwaps { get; set; } = 2;
        public abstract string Name { get; }

        protected BotBase(int seed) { Rng = new Random(seed); }

        protected abstract CandidateMove Choose(Game game, List<CandidateMove> moves);

        public BotDecision Decide(Game game)
        {
            var hand = game.CurrentPlayer.Hand;
            var moves = MoveFinder.FindAll(game.Board, hand, game.Rules);
            if (moves.Count > 0)
            {
                _stuckSwaps = 0;
                return BotDecision.Play(Choose(game, moves).Placements);
            }

            if (game.Rules.AllowExchange && game.BagCount > 0 && _stuckSwaps < MaxStuckSwaps)
            {
                _stuckSwaps++;
                int n = Math.Min(hand.Count, game.BagCount);
                // Swap the lowest-value tiles first if the bag can't cover the whole hand.
                return BotDecision.Swap(hand.OrderBy(t => t.BaseValue(game.Rules)).Take(n).ToList());
            }
            return BotDecision.Pass();
        }

        protected CandidateMove Best(List<CandidateMove> moves, Func<CandidateMove, double> value)
        {
            double best = double.MinValue;
            var ties = new List<CandidateMove>();
            foreach (var m in moves)
            {
                double v = value(m);
                if (v > best + 1e-9) { best = v; ties.Clear(); ties.Add(m); }
                else if (Math.Abs(v - best) <= 1e-9) ties.Add(m);
            }
            return ties[Rng.Next(ties.Count)];
        }
    }

    /// <summary>Plays the highest-scoring move. Ties broken at random.</summary>
    public sealed class GreedyBot : BotBase
    {
        public GreedyBot(int seed = 0) : base(seed) { }
        public override string Name => "Greedy";
        protected override CandidateMove Choose(Game game, List<CandidateMove> moves) => Best(moves, m => m.Score);
    }

    /// <summary>Plays a random legal move. A baseline: any real strategy should crush it.</summary>
    public sealed class RandomBot : BotBase
    {
        public RandomBot(int seed = 0) : base(seed) { }
        public override string Name => "Random";
        protected override CandidateMove Choose(Game game, List<CandidateMove> moves) => moves[Rng.Next(moves.Count)];
    }

    /// <summary>
    /// Greedy, but values keeping good tiles: score per tile spent matters, so it doesn't dump
    /// its whole hand for a few extra points. A second style to compare against Greedy.
    /// </summary>
    public sealed class EfficientBot : BotBase
    {
        public EfficientBot(int seed = 0) : base(seed) { }
        public override string Name => "Efficient";
        protected override CandidateMove Choose(Game game, List<CandidateMove> moves) =>
            Best(moves, m => m.Score - 1.5 * m.Placements.Count);
    }

    public sealed class TurnLog
    {
        public int Player;
        public DecisionKind Kind;
        public int TilesPlaced;
        public int Score;
        public List<ScoredWord> Words;
        public List<Placement> Placements;
        public double Milliseconds;
        public int BagBefore;
    }

    public static class BotRunner
    {
        /// <summary>Plays a game to the end with one bot per seat. Returns the turn log.</summary>
        public static List<TurnLog> PlayOut(Game game, IReadOnlyList<IBot> bots, int maxTurns = 1000)
        {
            if (bots.Count != game.Players.Count) throw new ArgumentException("One bot per player.");
            var log = new List<TurnLog>();
            var sw = new Stopwatch();
            while (!game.IsOver && log.Count < maxTurns)
            {
                int seat = game.CurrentPlayerIndex;
                int bag = game.BagCount;
                sw.Restart();
                var d = bots[seat].Decide(game);
                sw.Stop();
                var entry = new TurnLog { Player = seat, Kind = d.Kind, Milliseconds = sw.Elapsed.TotalMilliseconds, BagBefore = bag };
                switch (d.Kind)
                {
                    case DecisionKind.Play:
                        var r = game.Play(d.Placements);
                        if (!r.Legal) throw new InvalidOperationException($"{bots[seat].Name} tried an illegal play: {r.Error}");
                        entry.TilesPlaced = d.Placements.Count;
                        entry.Score = r.Score;
                        entry.Words = r.Words;
                        entry.Placements = d.Placements;
                        break;
                    case DecisionKind.Swap:
                        var e = game.Exchange(d.SwapTiles);
                        if (e != null) throw new InvalidOperationException($"{bots[seat].Name} swap failed: {e}");
                        break;
                    default:
                        game.Pass();
                        break;
                }
                log.Add(entry);
            }
            return log;
        }
    }
}
