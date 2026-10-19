using System;
using System.Collections.Generic;
using System.Linq;

namespace Align.Engine
{
    public sealed class CandidateMove
    {
        public List<Placement> Placements { get; }
        public MoveResult Result { get; }
        public int Score => Result.Score;

        internal CandidateMove(List<Placement> placements, MoveResult result)
        {
            Placements = placements;
            Result = result;
        }

        public override string ToString() => $"{string.Join(" ", Placements)} = {Score}";
    }

    /// <summary>
    /// Finds every legal play for a hand. Skips plays that include a tile sitting in no word at all
    /// (a lone tile touching nothing): those are legal but never change whether a play exists, and
    /// leaving them out keeps the search small.
    ///
    /// Works line by line (each row and column), walking the 11 squares left to right and deciding
    /// for each empty square "leave empty" or "place tile X here". Every placed tile's cross-word
    /// is fixed by the board alone, so it is checked up front; the main-line words are checked as
    /// they close. A line of 2+ tiles that already shares no trait is cut off immediately.
    /// </summary>
    public static class MoveFinder
    {
        public static List<CandidateMove> FindAll(Board board, IReadOnlyList<Tile> hand, GameRules rules, int maxMoves = 100000)
        {
            var results = new List<CandidateMove>();
            new Search(board, hand, rules, c => { results.Add(c); return results.Count < maxMoves; }).Run();
            return results;
        }

        /// <summary>True if this hand has at least one legal play on this board.</summary>
        public static bool AnyLegalPlay(Board board, IReadOnlyList<Tile> hand, GameRules rules)
        {
            bool found = false;
            new Search(board, hand, rules, c => { found = true; return false; }).Run();
            return found;
        }

        /// <summary>Bit per trait two tiles agree on: 1 color, 2 symbol, 4 number.</summary>
        internal static int Match(Tile a, Tile b) =>
            (a.Color == b.Color ? 1 : 0) | (a.Symbol == b.Symbol ? 2 : 0) | (a.Number == b.Number ? 4 : 0);

        private sealed class Kind
        {
            public Tile Sample;
            public readonly List<Tile> Tiles = new List<Tile>();
            public int Used;
        }

        private sealed class Search
        {
            // Cross status per (square, kind): 0 = illegal there, 1 = legal but in no cross word, 2 = legal and in a cross word.
            private const byte Illegal = 0, Alone = 1, InWord = 2;

            private readonly Board _board;
            private readonly GameRules _rules;
            private readonly Func<CandidateMove, bool> _emit;
            private readonly List<Kind> _kinds;
            private readonly bool _boardEmpty;

            private Pos[] _cells;
            private byte[,] _cross;
            private bool[] _touch;
            private int[] _assign;
            private bool _skipSingles; // the column pass skips 1-tile plays (the row pass already found them)

            public Search(Board board, IReadOnlyList<Tile> hand, GameRules rules, Func<CandidateMove, bool> emit)
            {
                _board = board;
                _rules = rules;
                _emit = emit;
                _boardEmpty = board.IsEmpty;
                _kinds = new List<Kind>();
                foreach (var t in hand)
                {
                    var k = _kinds.FirstOrDefault(x =>
                        x.Sample.Color == t.Color && x.Sample.Symbol == t.Symbol &&
                        x.Sample.Number == t.Number && x.Sample.Starred == t.Starred);
                    if (k == null) { k = new Kind { Sample = t }; _kinds.Add(k); }
                    k.Tiles.Add(t);
                }
            }

            public void Run()
            {
                if (_kinds.Count == 0) return;
                for (int orient = 0; orient < 2; orient++)
                {
                    _skipSingles = orient == 1;
                    for (int line = 0; line < Board.Size; line++)
                    {
                        // On an empty board only the lines through START can matter.
                        if (_boardEmpty && _rules.FirstPlayMustCoverStart &&
                            line != (orient == 0 ? Board.Start.Row : Board.Start.Col)) continue;
                        if (!SearchLine(orient, line)) return;
                    }
                }
            }

            private bool SearchLine(int orient, int line)
            {
                int n = Board.Size;
                _cells = new Pos[n];
                for (int i = 0; i < n; i++) _cells[i] = orient == 0 ? new Pos(line, i) : new Pos(i, line);

                // Cross words run perpendicular to the line.
                int pr = orient == 0 ? 1 : 0, pc = orient == 0 ? 0 : 1;
                _cross = new byte[n, _kinds.Count];
                _touch = new bool[n];
                bool anyOpen = false;
                for (int i = 0; i < n; i++)
                {
                    var cell = _cells[i];
                    if (_board[cell] != null) continue;
                    _touch[i] = Neighbors(cell).Any(q => _board[q] != null);

                    var before = new List<Tile>();
                    for (var q = cell.Offset(-pr, -pc); q.InBounds && _board[q] != null; q = q.Offset(-pr, -pc)) before.Add(_board[q]);
                    var after = new List<Tile>();
                    for (var q = cell.Offset(pr, pc); q.InBounds && _board[q] != null; q = q.Offset(pr, pc)) after.Add(_board[q]);
                    int len = before.Count + after.Count + 1;

                    for (int k = 0; k < _kinds.Count; k++)
                    {
                        byte st;
                        if (len == 1) st = Alone;
                        else if (len < _rules.MinWordLength) st = Illegal;
                        else
                        {
                            var t = _kinds[k].Sample;
                            int mask = 7;
                            foreach (var o in before) mask &= Match(t, o);
                            foreach (var o in after) mask &= Match(t, o);
                            st = mask != 0 ? InWord : Illegal;
                        }
                        _cross[i, k] = st;
                        if (st != Illegal) anyOpen = true;
                    }
                }
                if (!anyOpen) return true;

                _assign = new int[n];
                for (int i = 0; i < n; i++) _assign[i] = -1;
                return Dfs(0, 0, null, 0, false, false, 0, false, false);
            }

            private bool CloseOk(int runLen, int runMask, bool runHasNew, bool singleInWord)
            {
                if (!runHasNew || runLen == 0) return true;
                if (runLen == 1) return singleInWord;
                return runLen >= _rules.MinWordLength && runMask != 0;
            }

            private bool Dfs(int k, int runLen, Tile runFirst, int runMask, bool runHasNew, bool singleInWord,
                             int placed, bool touches, bool coversStart)
            {
                if (k == _cells.Length)
                {
                    if (!CloseOk(runLen, runMask, runHasNew, singleInWord)) return true;
                    if (placed == 0) return true;
                    if (_skipSingles && placed == 1) return true;
                    if (_boardEmpty ? (_rules.FirstPlayMustCoverStart && !coversStart)
                                    : (_rules.PlayMustTouchBoard && !touches)) return true;
                    return Emit();
                }

                var cell = _cells[k];
                var boardTile = _board[cell];
                if (boardTile != null)
                {
                    int mask = runLen == 0 ? 7 : runMask & Match(runFirst, boardTile);
                    if (runLen >= 1 && mask == 0) return true;
                    return Dfs(k + 1, runLen + 1, runLen == 0 ? boardTile : runFirst, mask, runHasNew, singleInWord,
                               placed, touches, coversStart);
                }

                // Leave this square empty: the current run ends here.
                if (CloseOk(runLen, runMask, runHasNew, singleInWord))
                    if (!Dfs(k + 1, 0, null, 0, false, false, placed, touches, coversStart)) return false;

                // Or place one of each kind of tile still in hand.
                for (int i = 0; i < _kinds.Count; i++)
                {
                    var kind = _kinds[i];
                    if (kind.Used >= kind.Tiles.Count) continue;
                    byte st = _cross[k, i];
                    if (st == Illegal) continue;

                    var t = kind.Sample;
                    int mask = runLen == 0 ? 7 : runMask & Match(runFirst, t);
                    if (runLen >= 1 && mask == 0) continue;

                    kind.Used++;
                    _assign[k] = i;
                    bool cont = Dfs(k + 1, runLen + 1, runLen == 0 ? t : runFirst, mask, true,
                                    runLen == 0 && st == InWord, placed + 1, touches || _touch[k],
                                    coversStart || cell == Board.Start);
                    _assign[k] = -1;
                    kind.Used--;
                    if (!cont) return false;
                }
                return true;
            }

            private bool Emit()
            {
                var placements = new List<Placement>();
                var taken = new int[_kinds.Count];
                for (int i = 0; i < _cells.Length; i++)
                {
                    int k = _assign[i];
                    if (k < 0) continue;
                    placements.Add(new Placement(_kinds[k].Tiles[taken[k]++], _cells[i]));
                }
                var result = MoveEvaluator.Evaluate(_board, placements, _rules);
                if (!result.Legal)
                    throw new InvalidOperationException(
                        $"MoveFinder produced an illegal play {string.Join(" ", placements)}: {result.Error}");
                return _emit(new CandidateMove(placements, result));
            }

            private static IEnumerable<Pos> Neighbors(Pos p)
            {
                foreach (var n in new[] { p.Offset(-1, 0), p.Offset(1, 0), p.Offset(0, -1), p.Offset(0, 1) })
                    if (n.InBounds) yield return n;
            }
        }
    }
}
