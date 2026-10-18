using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Align.Engine;

namespace Align.ConsoleClient
{
    /// <summary>Hot-seat console client. Run: dotnet run --project src/Align.Console [-- --seed 42]</summary>
    public static class Program
    {
        private static readonly Regex PlaceToken = new Regex(@"^(\d+)([A-Ka-k]\d{1,2})$");

        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            int? seed = null;
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "--seed" && int.TryParse(args[i + 1], out int s)) seed = s;

            Console.WriteLine("ALIGN");
            int count = AskInt("Players (2-4): ", 2, 4);
            var names = new List<string>();
            for (int i = 1; i <= count; i++)
            {
                Console.Write($"Name for player {i} [Player {i}]: ");
                var n = Console.ReadLine()?.Trim();
                names.Add(string.IsNullOrEmpty(n) ? $"Player {i}" : n);
            }

            var game = new Game(names, new GameRules(), seed);
            PrintHelp();

            while (!game.IsOver)
            {
                PrintBoard(game);
                PrintStatus(game);
                Console.Write($"{game.CurrentPlayer.Name}> ");
                var line = Console.ReadLine();
                if (line == null) return;
                line = line.Trim();
                if (line.Length == 0) continue;

                var parts = line.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                var cmd = parts[0].ToLowerInvariant();
                var rest = parts.Skip(1).ToArray();

                // A line that starts with a placement token is a play.
                if (PlaceToken.IsMatch(parts[0])) { cmd = "play"; rest = parts; }

                switch (cmd)
                {
                    case "play":
                    case "p":
                    case "preview":
                    case "?":
                        {
                            var placements = ParsePlacements(game, rest, out string err);
                            if (placements == null) { Warn(err); break; }
                            bool commit = cmd == "play" || cmd == "p";
                            var result = commit ? game.Play(placements) : game.Preview(placements);
                            if (!result.Legal) { Warn(result.Error); break; }
                            Console.WriteLine(commit ? "Played." : "Preview (not played):");
                            PrintWords(result);
                            break;
                        }
                    case "swap":
                    case "x":
                        {
                            var tiles = ParseSlots(game, rest, out string err);
                            if (tiles == null) { Warn(err); break; }
                            var e = game.Exchange(tiles);
                            if (e != null) Warn(e); else Console.WriteLine($"Exchanged {tiles.Count}.");
                            break;
                        }
                    case "pass":
                        game.Pass();
                        break;
                    case "tiles":
                        PrintTileKey();
                        break;
                    case "help":
                    case "h":
                        PrintHelp();
                        break;
                    case "quit":
                    case "q":
                        return;
                    default:
                        Warn("Unknown command. Type help.");
                        break;
                }
            }

            PrintBoard(game);
            Console.WriteLine($"Game over: {game.EndReason}");
            foreach (var p in game.Players.OrderByDescending(p => p.Score))
                Console.WriteLine($"  {p.Name}: {p.Score}");
            Console.WriteLine($"Winner: {string.Join(" & ", game.Leaders().Select(p => p.Name))}");
        }

        private static List<Placement> ParsePlacements(Game game, string[] tokens, out string error)
        {
            error = null;
            if (tokens.Length == 0) { error = "Usage: play 1F6 2G6 3H6  (hand slot + square)"; return null; }
            var hand = game.CurrentPlayer.Hand;
            var list = new List<Placement>();
            foreach (var tok in tokens)
            {
                var m = PlaceToken.Match(tok);
                if (!m.Success) { error = $"Can't read '{tok}'. Use slot+square, like 2G6."; return null; }
                int slot = int.Parse(m.Groups[1].Value);
                if (slot < 1 || slot > hand.Count) { error = $"No tile in slot {slot}."; return null; }
                if (!Pos.TryParse(m.Groups[2].Value, out var pos)) { error = $"{m.Groups[2].Value} isn't on the board."; return null; }
                list.Add(new Placement(hand[slot - 1], pos));
            }
            return list;
        }

        private static List<Tile> ParseSlots(Game game, string[] tokens, out string error)
        {
            error = null;
            var hand = game.CurrentPlayer.Hand;
            var list = new List<Tile>();
            if (tokens.Length == 0) { error = "Usage: swap 1 3 5  (hand slots)"; return null; }
            foreach (var tok in tokens)
            {
                if (!int.TryParse(tok, out int slot) || slot < 1 || slot > hand.Count)
                { error = $"No tile in slot '{tok}'."; return null; }
                list.Add(hand[slot - 1]);
            }
            return list;
        }

        private static void PrintBoard(Game game)
        {
            Console.WriteLine();
            Console.Write("    ");
            for (int c = 0; c < Board.Size; c++) Console.Write($" {(char)('A' + c)}  ");
            Console.WriteLine();
            for (int r = 0; r < Board.Size; r++)
            {
                Console.Write($"{r + 1,3} ");
                for (int c = 0; c < Board.Size; c++)
                {
                    var t = game.Board[r, c];
                    if (t != null) WriteTile(t);
                    else WritePremium(Board.PremiumAt(new Pos(r, c)));
                }
                Console.WriteLine();
            }
        }

        private static void WriteTile(Tile t)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ColorFor(t.Color);
            Console.Write(t.Code + (t.Starred ? "*" : " "));
            Console.ForegroundColor = old;
        }

        private static void WritePremium(Premium p)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            switch (p)
            {
                case Premium.Start: Console.Write("[S] "); break;
                case Premium.Plus2: Console.Write("+2  "); break;
                case Premium.Times2: Console.Write("x2  "); break;
                case Premium.Times4: Console.Write("x4  "); break;
                default: Console.Write(" .  "); break;
            }
            Console.ForegroundColor = old;
        }

        private static ConsoleColor ColorFor(TileColor c)
        {
            switch (c)
            {
                case TileColor.Red: return ConsoleColor.Red;
                case TileColor.Green: return ConsoleColor.Green;
                case TileColor.Blue: return ConsoleColor.Cyan;
                case TileColor.Black: return ConsoleColor.Gray;
                case TileColor.Purple: return ConsoleColor.Magenta;
                default: return ConsoleColor.Yellow;
            }
        }

        private static void PrintStatus(Game game)
        {
            Console.WriteLine();
            Console.WriteLine("Scores: " + string.Join("   ", game.Players.Select(p => $"{p.Name} {p.Score}")) + $"   | Bag: {game.BagCount}");
            Console.Write("Hand:   ");
            var hand = game.CurrentPlayer.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                Console.Write($"{i + 1}:");
                WriteTile(hand[i]);
                Console.Write(" ");
            }
            Console.WriteLine();
        }

        private static void PrintWords(MoveResult r)
        {
            if (r.Words.Count == 0) Console.WriteLine("  No words formed. 0 points.");
            foreach (var w in r.Words) Console.WriteLine($"  {w}");
            Console.WriteLine($"  Total: {r.Score}");
        }

        private static void PrintHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Squares are column letter + row number (A1 top-left, F6 = START).");
            Console.WriteLine("Tiles are color+number+symbol: R2D = Red 2 Diamond. * = starred tile (+1 pt).");
            Console.WriteLine("  1F6 2G6 3H6     play hand slots 1,2,3 on F6,G6,H6 (or: play 1F6 ...)");
            Console.WriteLine("  ? 1F6 2G6 3H6   preview legality and score without playing");
            Console.WriteLine("  swap 1 4        exchange hand slots 1 and 4");
            Console.WriteLine("  pass | tiles | help | quit");
        }

        private static void PrintTileKey()
        {
            Console.WriteLine("Colors: R Red, G Green, B Blue, K Black*, P Purple*, Y Yellow*");
            Console.WriteLine("Symbols: D Diamond, O Oval, S Squiggle, X X*, C Circle*, H Heart*");
        }

        private static void Warn(string msg)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  " + msg);
            Console.ForegroundColor = old;
        }

        private static int AskInt(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = Console.ReadLine();
                if (s == null) Environment.Exit(0);
                if (int.TryParse(s.Trim(), out int v) && v >= min && v <= max) return v;
            }
        }
    }
}
