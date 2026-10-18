using System;
using System.Collections.Generic;
using System.Linq;

namespace Align.Engine
{
    public sealed class Player
    {
        public string Name { get; }
        public List<Tile> Hand { get; } = new List<Tile>();
        public int Score { get; internal set; }
        public Player(string name) { Name = name; }
    }

    public enum TurnKind { Play, Exchange, Pass }

    public sealed class TurnRecord
    {
        public int PlayerIndex { get; internal set; }
        public TurnKind Kind { get; internal set; }
        public List<Placement> Placements { get; internal set; }
        public List<ScoredWord> Words { get; internal set; }
        public int Score { get; internal set; }
        public int ExchangedCount { get; internal set; }
    }

    public sealed class Game
    {
        public GameRules Rules { get; }
        public Board Board { get; } = new Board();
        public IReadOnlyList<Player> Players => _players;
        public int CurrentPlayerIndex { get; private set; }
        public Player CurrentPlayer => _players[CurrentPlayerIndex];
        public int BagCount => _bag.Count;
        public bool IsOver { get; private set; }
        public string EndReason { get; private set; }
        public IReadOnlyList<TurnRecord> History => _history;

        private readonly List<Player> _players;
        private readonly List<Tile> _bag; // draw from the end
        private readonly List<TurnRecord> _history = new List<TurnRecord>();
        private readonly Random _rng;
        private int _scorelessTurns;

        /// <summary>New game with a shuffled bag. Pass a seed for a repeatable game.</summary>
        public Game(IEnumerable<string> playerNames, GameRules rules = null, int? seed = null)
            : this(playerNames, rules, (IEnumerable<Tile>)null, seed) { }

        /// <summary>
        /// New game with the bag in a fixed order (first tile in the list is drawn first).
        /// Used for tests, tutorials and replays.
        /// </summary>
        public static Game WithDrawOrder(IEnumerable<string> playerNames, GameRules rules, IEnumerable<Tile> drawOrder, int? seed = null)
            => new Game(playerNames, rules, drawOrder ?? Enumerable.Empty<Tile>(), seed);

        private Game(IEnumerable<string> playerNames, GameRules rules, IEnumerable<Tile> drawOrder, int? seed)
        {
            Rules = rules ?? new GameRules();
            _players = playerNames.Select(n => new Player(n)).ToList();
            if (_players.Count < 1) throw new ArgumentException("Need at least one player.");
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();

            if (drawOrder != null)
            {
                _bag = drawOrder.Reverse().ToList();
            }
            else
            {
                _bag = TileSet.Create(Rules);
                Shuffle(_bag);
            }

            foreach (var p in _players) Refill(p);
        }

        /// <summary>Check and score a play for the current player without committing it.</summary>
        public MoveResult Preview(IReadOnlyList<Placement> placements)
        {
            if (IsOver) return MoveResult.Fail("The game is over.");
            foreach (var p in placements)
                if (!CurrentPlayer.Hand.Contains(p.Tile))
                    return MoveResult.Fail($"{p.Tile.Code} isn't in {CurrentPlayer.Name}'s hand.");
            return MoveEvaluator.Evaluate(Board, placements, Rules);
        }

        public MoveResult Play(IReadOnlyList<Placement> placements)
        {
            var result = Preview(placements);
            if (!result.Legal) return result;

            var player = CurrentPlayer;
            foreach (var p in placements)
            {
                Board.Place(p.Tile, p.Pos);
                player.Hand.Remove(p.Tile);
            }
            player.Score += result.Score;
            Refill(player);

            _history.Add(new TurnRecord
            {
                PlayerIndex = CurrentPlayerIndex,
                Kind = TurnKind.Play,
                Placements = placements.ToList(),
                Words = result.Words,
                Score = result.Score,
            });
            _scorelessTurns = 0;

            Advance();
            return result;
        }

        /// <summary>Return tiles to the bag and draw the same number. Returns an error, or null on success.</summary>
        public string Exchange(IReadOnlyList<Tile> tiles)
        {
            if (IsOver) return "The game is over.";
            if (!Rules.AllowExchange) return "Exchanging is turned off.";
            if (tiles == null || tiles.Count == 0) return "Choose at least one tile to exchange.";
            if (tiles.Distinct().Count() != tiles.Count) return "The same tile was chosen twice.";
            foreach (var t in tiles)
                if (!CurrentPlayer.Hand.Contains(t)) return $"{t.Code} isn't in {CurrentPlayer.Name}'s hand.";
            if (_bag.Count < tiles.Count) return $"Only {_bag.Count} tiles left in the bag.";

            var player = CurrentPlayer;
            var drawn = new List<Tile>();
            for (int i = 0; i < tiles.Count; i++) drawn.Add(Draw());
            foreach (var t in tiles)
            {
                player.Hand.Remove(t);
                _bag.Add(t);
            }
            player.Hand.AddRange(drawn);
            Shuffle(_bag);

            _history.Add(new TurnRecord { PlayerIndex = CurrentPlayerIndex, Kind = TurnKind.Exchange, ExchangedCount = tiles.Count });
            _scorelessTurns = 0; // a swap is not a pass: it breaks the run of passes
            Advance();
            return null;
        }

        public void Pass()
        {
            if (IsOver) return;
            _history.Add(new TurnRecord { PlayerIndex = CurrentPlayerIndex, Kind = TurnKind.Pass });
            CountPass();
        }

        private void CountPass()
        {
            _scorelessTurns++;
            if (_scorelessTurns >= Rules.ScorelessRoundsToEnd * _players.Count)
                End("Board locked: every player passed in a row.");
            else
                Advance();
        }

        private void Advance()
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % _players.Count;
            // Out of tiles with an empty bag: nothing to do but pass.
            if (CurrentPlayer.Hand.Count == 0 && _bag.Count == 0) Pass();
        }

        private void End(string reason)
        {
            IsOver = true;
            EndReason = reason;
        }

        public IEnumerable<Player> Leaders()
        {
            int best = _players.Max(p => p.Score);
            return _players.Where(p => p.Score == best);
        }

        private void Refill(Player p)
        {
            while (p.Hand.Count < Rules.HandSize && _bag.Count > 0) p.Hand.Add(Draw());
        }

        private Tile Draw()
        {
            var t = _bag[_bag.Count - 1];
            _bag.RemoveAt(_bag.Count - 1);
            return t;
        }

        private void Shuffle(List<Tile> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var tmp = list[i]; list[i] = list[j]; list[j] = tmp;
            }
        }
    }
}
