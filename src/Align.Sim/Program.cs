using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Align.Engine;

namespace Align.Sim
{
    /// <summary>
    /// Bot-vs-bot simulator for checking that the ruleset plays.
    ///   dotnet run --project src/Align.Sim -c Release -- --games 200 --out sim-report.md
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            int games = 200;
            string outPath = "sim-report.md";
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--games") games = int.Parse(args[i + 1]);
                if (args[i] == "--out") outPath = args[i + 1];
            }

            var configs = new List<(string name, Func<int, IBot>[] seats, bool rotate)>
            {
                ("2 players: Greedy vs Greedy", new Func<int, IBot>[] { s => new GreedyBot(s), s => new GreedyBot(s + 1) }, false),
                ("3 players: Greedy x3", new Func<int, IBot>[] { s => new GreedyBot(s), s => new GreedyBot(s + 1), s => new GreedyBot(s + 2) }, false),
                ("4 players: Greedy x4", new Func<int, IBot>[] { s => new GreedyBot(s), s => new GreedyBot(s + 1), s => new GreedyBot(s + 2), s => new GreedyBot(s + 3) }, false),
                ("2 players: Greedy vs Random (seats alternate)", new Func<int, IBot>[] { s => new GreedyBot(s), s => new RandomBot(s + 1) }, true),
                ("2 players: Greedy vs Efficient (seats alternate)", new Func<int, IBot>[] { s => new GreedyBot(s), s => new EfficientBot(s + 1) }, true),
            };

            var sb = new StringBuilder();
            sb.AppendLine("# Align simulation report");
            sb.AppendLine();
            sb.AppendLine($"{games} games per configuration. Rules: hand {new GameRules().HandSize}, tile premiums, starred +1, board-lock end.");
            sb.AppendLine("Bots swap their whole hand when they have no legal play (max 2 swaps in a row), then pass.");
            sb.AppendLine();

            var total = Stopwatch.StartNew();
            string sampleBoard = null;
            foreach (var cfg in configs)
            {
                var st = new Stats(cfg.seats.Length);
                for (int g = 0; g < games; g++)
                {
                    int seed = 1000 + g;
                    var names = Enumerable.Range(0, cfg.seats.Length).Select(i => $"P{i + 1}").ToList();
                    // Seat order: when rotating, odd games put the second bot type first.
                    var factories = cfg.seats.ToList();
                    bool flipped = cfg.rotate && g % 2 == 1;
                    if (flipped) factories.Reverse();
                    var bots = factories.Select((f, i) => f(seed * 10 + i)).ToList();
                    var game = new Game(names, new GameRules(), seed);
                    var log = BotRunner.PlayOut(game, bots);
                    st.Add(game, log, bots.Select(b => b.Name).ToList());
                    if (sampleBoard == null) sampleBoard = RenderBoard(game);
                }
                st.Write(sb, cfg.name);
            }

            sb.AppendLine("## Sample final board (first 2-player Greedy game)");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.Append(sampleBoard);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine($"Total run time: {total.Elapsed.TotalSeconds:F1}s");

            File.WriteAllText(outPath, sb.ToString());
            Console.WriteLine(sb.ToString());
            return 0;
        }

        internal static string RenderBoard(Game game)
        {
            var sb = new StringBuilder();
            sb.Append("    ");
            for (int c = 0; c < Board.Size; c++) sb.Append($" {(char)('A' + c)}  ");
            sb.AppendLine();
            for (int r = 0; r < Board.Size; r++)
            {
                sb.Append($"{r + 1,3} ");
                for (int c = 0; c < Board.Size; c++)
                {
                    var t = game.Board[r, c];
                    if (t != null) sb.Append(t.Code + (t.Starred ? "*" : " "));
                    else
                    {
                        switch (Board.PremiumAt(new Pos(r, c)))
                        {
                            case Premium.Start: sb.Append("[S] "); break;
                            case Premium.Plus2: sb.Append("+2  "); break;
                            case Premium.Times2: sb.Append("x2  "); break;
                            case Premium.Times4: sb.Append("x4  "); break;
                            default: sb.Append(" .  "); break;
                        }
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }

    internal sealed class Stats
    {
        private readonly int _players;
        private int _games, _aborted, _ties;
        private readonly List<int> _turns = new List<int>();
        private readonly List<int> _rounds = new List<int>();
        private readonly List<int> _boardTiles = new List<int>();
        private readonly List<int> _tilesLeft = new List<int>();
        private readonly List<int> _bagLeft = new List<int>();
        private readonly List<double> _allScores = new List<double>();
        private readonly List<double> _winnerScores = new List<double>();
        private readonly List<double> _margins = new List<double>();
        private readonly int[] _seatWins;
        private readonly Dictionary<string, double> _botWins = new Dictionary<string, double>();
        private readonly Dictionary<string, int> _botGames = new Dictionary<string, int>();
        private readonly Dictionary<string, List<double>> _botScores = new Dictionary<string, List<double>>();
        private int _plays, _swaps, _passes, _tilesPlaced, _wordsScored, _pointsScored, _maxPlay, _bigPlays;
        private int _stuckWithTilesInBag;
        private readonly int[] _tilesPerPlay = new int[8];
        private readonly Dictionary<string, int> _traitMix = new Dictionary<string, int>();
        private readonly Dictionary<Premium, int> _premiumsCovered = new Dictionary<Premium, int>();
        private int _starredPlaced, _lockedTrue, _lockedFalse;
        private double _ms; private int _decisions; private double _maxMs;
        private int _crossPlays;

        public Stats(int players) { _players = players; _seatWins = new int[players]; }

        public void Add(Game game, List<TurnLog> log, List<string> botNames)
        {
            _games++;
            if (!game.IsOver) { _aborted++; return; }

            _turns.Add(log.Count);
            _rounds.Add((int)Math.Ceiling(log.Count / (double)_players));
            _boardTiles.Add(game.Board.TileCount);
            _bagLeft.Add(game.BagCount);
            _tilesLeft.Add(game.BagCount + game.Players.Sum(p => p.Hand.Count));

            var scores = game.Players.Select(p => p.Score).ToList();
            _allScores.AddRange(scores.Select(s => (double)s));
            var sorted = scores.OrderByDescending(s => s).ToList();
            _winnerScores.Add(sorted[0]);
            _margins.Add(sorted[0] - sorted[1]);
            var leaders = Enumerable.Range(0, _players).Where(i => scores[i] == sorted[0]).ToList();
            if (leaders.Count > 1) _ties++;
            foreach (var i in leaders) _seatWins[i]++;

            for (int i = 0; i < _players; i++)
            {
                var n = botNames[i];
                if (!_botGames.ContainsKey(n)) { _botGames[n] = 0; _botWins[n] = 0; _botScores[n] = new List<double>(); }
                _botGames[n]++;
                _botScores[n].Add(scores[i]);
                if (leaders.Contains(i)) _botWins[n] += 1.0 / leaders.Count;
            }

            // Was the end a true lock (nobody could play) or just everyone giving up?
            if (game.IsBoardLockedForEveryone()) _lockedTrue++; else _lockedFalse++;

            foreach (var t in log)
            {
                _ms += t.Milliseconds; _decisions++; _maxMs = Math.Max(_maxMs, t.Milliseconds);
                switch (t.Kind)
                {
                    case DecisionKind.Play:
                        _plays++;
                        _tilesPlaced += t.TilesPlaced;
                        _tilesPerPlay[Math.Min(7, t.TilesPlaced)]++;
                        _wordsScored += t.Words.Count;
                        _pointsScored += t.Score;
                        _maxPlay = Math.Max(_maxPlay, t.Score);
                        if (t.Score >= 30) _bigPlays++;
                        if (t.Words.Select(w => w.Orientation).Distinct().Count() > 1) _crossPlays++;
                        foreach (var w in t.Words)
                        {
                            var key = string.Join("+", w.SharedTraits);
                            _traitMix[key] = _traitMix.TryGetValue(key, out int c) ? c + 1 : 1;
                        }
                        break;
                    case DecisionKind.Swap:
                        _swaps++;
                        if (t.BagBefore > 0) _stuckWithTilesInBag++;
                        break;
                    default:
                        _passes++;
                        if (t.BagBefore > 0) _stuckWithTilesInBag++;
                        break;
                }
            }

            for (int r = 0; r < Board.Size; r++)
                for (int c = 0; c < Board.Size; c++)
                {
                    var tile = game.Board[r, c];
                    if (tile == null) continue;
                    if (tile.Starred) _starredPlaced++;
                    var p = Board.PremiumAt(new Pos(r, c));
                    if (p != Premium.None) _premiumsCovered[p] = _premiumsCovered.TryGetValue(p, out int n) ? n + 1 : 1;
                }
        }

        private static string Range(List<int> xs) => xs.Count == 0 ? "-" : $"{xs.Average():F1} (min {xs.Min()}, max {xs.Max()})";
        private static string Range(List<double> xs) => xs.Count == 0 ? "-" : $"{xs.Average():F1} (min {xs.Min():F0}, max {xs.Max():F0})";
        private static string Pct(double a, double b) => b == 0 ? "-" : $"{100.0 * a / b:F1}%";

        public void Write(StringBuilder sb, string title)
        {
            int done = _games - _aborted;
            int turns = _plays + _swaps + _passes;
            sb.AppendLine($"## {title}");
            sb.AppendLine();
            sb.AppendLine("| Measure | Value |");
            sb.AppendLine("|---|---|");
            sb.AppendLine($"| Games finished / aborted (hit turn cap) | {done} / {_aborted} |");
            sb.AppendLine($"| Turns per game | {Range(_turns)} |");
            sb.AppendLine($"| Rounds per game | {Range(_rounds)} |");
            sb.AppendLine($"| Tiles on board at end (of 121 squares, 87 tiles) | {Range(_boardTiles)} |");
            sb.AppendLine($"| Tiles never played (bag + hands) | {Range(_tilesLeft)} |");
            sb.AppendLine($"| Bag at end | {Range(_bagLeft)} |");
            sb.AppendLine($"| End was a true lock (no one could play) | {Pct(_lockedTrue, done)} |");
            sb.AppendLine($"| Score per player | {Range(_allScores)} |");
            sb.AppendLine($"| Winning score | {Range(_winnerScores)} |");
            sb.AppendLine($"| Winning margin | {Range(_margins)} |");
            sb.AppendLine($"| Ties for first | {Pct(_ties, done)} |");
            sb.AppendLine($"| Wins by seat | {string.Join(", ", _seatWins.Select((w, i) => $"P{i + 1} {Pct(w, done)}"))} |");
            if (_botGames.Count > 1)
                sb.AppendLine($"| Wins by bot | {string.Join(", ", _botGames.Keys.Select(k => $"{k} {Pct(_botWins[k], done)} (avg {_botScores[k].Average():F1} pts)"))} |");
            sb.AppendLine($"| Turn mix: play / swap / pass | {Pct(_plays, turns)} / {Pct(_swaps, turns)} / {Pct(_passes, turns)} |");
            sb.AppendLine($"| Stuck turns while tiles remained in bag | {Pct(_stuckWithTilesInBag, turns)} |");
            sb.AppendLine($"| Tiles per play (avg) | {(_plays == 0 ? 0 : _tilesPlaced / (double)_plays):F2} |");
            sb.AppendLine($"| Tiles per play (1..7) | {string.Join(" ", Enumerable.Range(1, 7).Select(i => $"{i}:{Pct(_tilesPerPlay[i], _plays)}"))} |");
            sb.AppendLine($"| Words per play (avg) | {(_plays == 0 ? 0 : _wordsScored / (double)_plays):F2} |");
            sb.AppendLine($"| Plays scoring in both directions | {Pct(_crossPlays, _plays)} |");
            sb.AppendLine($"| Points per play (avg) / best single play | {(_plays == 0 ? 0 : _pointsScored / (double)_plays):F1} / {_maxPlay} |");
            sb.AppendLine($"| Plays of 30+ points | {Pct(_bigPlays, _plays)} |");
            var traitTotal = _traitMix.Values.Sum();
            sb.AppendLine($"| Words by shared trait | {string.Join(", ", _traitMix.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} {Pct(kv.Value, traitTotal)}"))} |");
            sb.AppendLine($"| Premium squares covered per game | {string.Join(", ", new[] { Premium.Plus2, Premium.Times2, Premium.Times4 }.Select(p => $"{p} {(_premiumsCovered.TryGetValue(p, out int n) ? n / (double)done : 0):F1}/{(p == Premium.Times2 ? 8 : 4)}"))} |");
            sb.AppendLine($"| Starred tiles played per game | {(done == 0 ? 0 : _starredPlaced / (double)done):F1}/6 |");
            sb.AppendLine($"| Bot think time per turn (avg / max) | {(_decisions == 0 ? 0 : _ms / _decisions):F1} ms / {_maxMs:F0} ms |");
            sb.AppendLine();
        }
    }
}
