using System.Collections.Generic;
using System.Linq;
using Align.Engine;
using Xunit;

namespace Align.Engine.Tests
{
    public class EngineTests
    {
        private static readonly GameRules Rules = new GameRules();
        private static int _nextId = 1000;

        private static Tile T(string code, bool starred = false)
        {
            TileColor c = code[0] switch
            {
                'R' => TileColor.Red, 'G' => TileColor.Green, 'B' => TileColor.Blue,
                'K' => TileColor.Black, 'P' => TileColor.Purple, _ => TileColor.Yellow,
            };
            int n = code[1] - '0';
            TileSymbol s = code[2] switch
            {
                'D' => TileSymbol.Diamond, 'O' => TileSymbol.Oval, 'S' => TileSymbol.Squiggle,
                'X' => TileSymbol.X, 'C' => TileSymbol.Circle, _ => TileSymbol.Heart,
            };
            return new Tile(_nextId++, c, s, n, starred);
        }

        private static Pos P(string sq) { Assert.True(Pos.TryParse(sq, out var p)); return p; }

        private static Placement Pl(string code, string sq) => new Placement(T(code), P(sq));

        private static MoveResult Eval(Board b, params Placement[] ps) => MoveEvaluator.Evaluate(b, ps, Rules);

        // No START / touch requirements, for tests that only care about scoring.
        private static readonly GameRules Free = new GameRules { FirstPlayMustCoverStart = false, PlayMustTouchBoard = false };
        private static MoveResult EvalFree(Board b, params Placement[] ps) => MoveEvaluator.Evaluate(b, ps, Free);

        private static void Put(Board b, string code, string sq) => b.Place(T(code), P(sq));

        // ---------- tile set ----------

        [Fact]
        public void TileSet_Has81BasePlus6Starred()
        {
            var set = TileSet.Create(Rules);
            Assert.Equal(87, set.Count);
            Assert.Equal(6, set.Count(t => t.Starred));
            Assert.Equal(87, set.Select(t => t.Id).Distinct().Count());
            Assert.Equal(3, set.Count(t => t.Color == TileColor.Red && t.Symbol == TileSymbol.Oval && t.Number == 2));
        }

        [Fact]
        public void StarredTiles_EachHaveAUniqueTraitValue()
        {
            var set = TileSet.Create(Rules);
            foreach (var s in set.Where(t => t.Starred))
            {
                bool colorUnique = set.Count(t => t.Color == s.Color) == 1;
                bool symbolUnique = set.Count(t => t.Symbol == s.Symbol) == 1;
                Assert.True(colorUnique ^ symbolUnique, s.Name);
            }
        }

        [Fact]
        public void Board_LayoutMatchesPrintedBoard()
        {
            Assert.Equal(Premium.Start, Board.PremiumAt(P("F6")));
            Assert.Equal(Premium.Times2, Board.PremiumAt(P("A1")));
            Assert.Equal(Premium.Plus2, Board.PremiumAt(P("F2")));
            Assert.Equal(Premium.Times4, Board.PremiumAt(P("C3")));
            Assert.Equal(Premium.Times2, Board.PremiumAt(P("E5")));
            Assert.Equal(Premium.Plus2, Board.PremiumAt(P("B6")));
            Assert.Equal(Premium.Times4, Board.PremiumAt(P("I9")));
            Assert.Equal(Premium.Times2, Board.PremiumAt(P("K11")));
            Assert.Equal(Premium.None, Board.PremiumAt(P("D4")));
        }

        // ---------- what counts as a word ----------

        [Fact]
        public void Word_SharingOneTrait_IsLegal()
        {
            // Shares only color.
            var r = Eval(new Board(), Pl("R1D", "E6"), Pl("R2O", "F6"), Pl("R3S", "G6"));
            Assert.True(r.Legal, r.Error);
            Assert.Single(r.Words);
            Assert.Equal(new[] { Trait.Color }, r.Words[0].SharedTraits);
        }

        [Fact]
        public void Word_RepeatedIdenticalTiles_IsLegal()
        {
            var r = Eval(new Board(), Pl("R2O", "E6"), Pl("R2O", "F6"), Pl("R2O", "G6"));
            Assert.True(r.Legal, r.Error);
            Assert.Equal(3, r.Words[0].SharedTraits.Count);
        }

        [Fact]
        public void Word_SharingNothing_IsIllegal()
        {
            var r = Eval(new Board(), Pl("R1D", "E6"), Pl("G2O", "F6"), Pl("B3O", "G6"));
            Assert.False(r.Legal);
        }

        [Fact]
        public void TwoTileLine_IsIllegal()
        {
            var r = Eval(new Board(), Pl("R1D", "F6"), Pl("R2D", "G6"));
            Assert.False(r.Legal);
        }

        [Fact]
        public void TwoTileLine_FormedSideways_IsIllegal()
        {
            var b = new Board();
            Put(b, "R1D", "E6"); Put(b, "R2D", "F6"); Put(b, "R3D", "G6");
            // Hanging one tile under F6 makes a 2-tile vertical line.
            var r = Eval(b, Pl("R1O", "F7"));
            Assert.False(r.Legal);
        }

        [Fact]
        public void StarredTile_JoinsWordThroughItsOtherTraits()
        {
            var kd3 = new Placement(T("K3D", true), P("G6"));
            var r = Eval(new Board(), Pl("R1D", "E6"), Pl("G2D", "F6"), kd3);
            Assert.True(r.Legal, r.Error);
            Assert.Equal(new[] { Trait.Symbol }, r.Words[0].SharedTraits);
        }

        // ---------- placement ----------

        [Fact]
        public void Play_MustBeOneRowOrColumn()
        {
            var r = Eval(new Board(), Pl("R1D", "F6"), Pl("R2D", "G7"));
            Assert.False(r.Legal);
        }

        [Fact]
        public void FirstPlay_MustCoverStart()
        {
            var r = Eval(new Board(), Pl("R1D", "A1"), Pl("R2D", "B1"), Pl("R3D", "C1"));
            Assert.False(r.Legal);
        }

        [Fact]
        public void Play_TilesNeedNotTouchEachOther()
        {
            var b = new Board();
            Put(b, "R1D", "E6"); Put(b, "R2D", "F6"); Put(b, "R3D", "G6");
            // H6 extends the word; K6 is in the same row but stands alone.
            var r = Eval(b, Pl("R1O", "H6"), Pl("B2O", "K6"));
            Assert.True(r.Legal, r.Error);
            Assert.Single(r.Words);
            Assert.Equal(1 + 2 + 3 + 1, r.Score);
        }

        [Fact]
        public void Play_AfterTheFirst_MustTouchTheBoard()
        {
            var b = new Board();
            Put(b, "R1D", "E6"); Put(b, "R2D", "F6"); Put(b, "R3D", "G6");
            var r = Eval(b, Pl("B1O", "A2"), Pl("B1O", "B2"), Pl("B1O", "C2"));
            Assert.False(r.Legal);
        }

        [Fact]
        public void Play_CanBridgeAGapThroughExistingTiles()
        {
            var b = new Board();
            Put(b, "R2O", "F6");
            // Play E6 and G6 on either side of F6: one 3-tile word.
            var r = Eval(b, Pl("R1D", "E6"), Pl("R3S", "G6"));
            Assert.True(r.Legal, r.Error);
            Assert.Single(r.Words);
            Assert.Equal(3, r.Words[0].Tiles.Count);
        }

        [Fact]
        public void Play_CannotGoOnOccupiedSquare()
        {
            var b = new Board();
            Put(b, "R2O", "F6");
            Assert.False(Eval(b, Pl("R1D", "F6")).Legal);
        }

        // ---------- scoring ----------

        [Fact]
        public void Score_IsSumOfNumbers()
        {
            var r = EvalFree(new Board(), Pl("R1D", "H8"), Pl("R2D", "I8"), Pl("R3D", "J8"));
            Assert.True(r.Legal, r.Error);
            Assert.Equal(6, r.Score);
        }

        [Fact]
        public void Score_CrossingTileScoresInBothWords()
        {
            // Two vertical words already down, in columns D and H (rows 2-4).
            var b = new Board();
            Put(b, "G1O", "D2"); Put(b, "G1O", "D3"); Put(b, "G1O", "D4");
            Put(b, "B1S", "H2"); Put(b, "B1S", "H3"); Put(b, "B1S", "H4");

            // Horizontal play on row 5: D5..H5 extends both columns (all share number 1).
            var r = Eval(b,
                Pl("G1D", "D5"), Pl("R1D", "E5"), Pl("R1D", "F5"), Pl("R1D", "G5"), Pl("B1D", "H5"));
            Assert.True(r.Legal, r.Error);
            Assert.Equal(3, r.Words.Count);
            // Row word: D5..H5 = five 1s, but E5 and G5 are x2 squares -> 1+2+1+2+1 = 7.
            // Column D: 1+1+1+1 = 4. Column H: 1+1+1+1 = 4. D5 and H5 score in two words each.
            Assert.Equal(15, r.Score);
        }

        [Fact]
        public void Score_PremiumAppliesToNewTileInEveryWordItIsIn()
        {
            var b = new Board();
            // Column E rows 2-4 and row 5 F..G give E5 (x2) two words when filled.
            Put(b, "R3D", "E2"); Put(b, "R3D", "E3"); Put(b, "R3D", "E4");
            Put(b, "B3O", "F5"); Put(b, "B3O", "G5");
            var r = Eval(b, Pl("R3O", "E5"));
            Assert.True(r.Legal, r.Error);
            // Column: 3+3+3+(3*2)=15. Row: (3*2)+3+3=12.
            Assert.Equal(27, r.Score);
        }

        [Fact]
        public void Score_PremiumOnlyCountsTheTurnTheTileIsPlaced()
        {
            var b = new Board();
            Put(b, "R3D", "C3"); // sits on x4, placed earlier
            var r = Eval(b, Pl("R1D", "D3"), Pl("R1D", "E3"));
            Assert.True(r.Legal, r.Error);
            Assert.Equal(5, r.Score); // 3 + 1 + 1, no x4
        }

        [Fact]
        public void Score_StarredTileIsWorthNumberPlusOne()
        {
            var heart = new Placement(T("R1H", true), P("J8")); // printed 1, scores 1+1
            var r = EvalFree(new Board(), Pl("R1D", "H8"), Pl("R1D", "I8"), heart);
            Assert.True(r.Legal, r.Error);
            Assert.Equal(4, r.Score);
        }

        [Fact]
        public void Score_StarredTileKeepsItsBonusWhenScoredAgainLater()
        {
            var b = new Board();
            b.Place(T("K3D", true), P("H8"));
            Put(b, "R1D", "I8"); Put(b, "R1D", "J8");
            var r = Eval(b, Pl("R1D", "K8"));
            Assert.True(r.Legal, r.Error);
            Assert.Equal((3 + 1) + 1 + 1 + 1, r.Score);
        }

        [Fact]
        public void Score_StarredTileOnPremiumSquare_PremiumApplies()
        {
            // Ruled: the +1 is added first, then the premium multiplies it.
            var x = new Placement(T("B2X", true), P("C3")); // x4
            var r = EvalFree(new Board(), x, Pl("B1D", "D3"), Pl("B1D", "E3"));
            Assert.True(r.Legal, r.Error);
            Assert.Equal((2 + 1) * 4 + 1 + 1, r.Score);
        }

        // ---------- game flow ----------

        private static List<Tile> Bag(params string[] codes) => codes.Select(c => T(c)).ToList();

        [Fact]
        public void Game_DealsHands_AndRefillsAfterPlay()
        {
            var bag = Bag("R1D", "R2D", "R3D", "B1O", "B2O", "B3O",
                          "G1S", "G2S", "G3S", "G1D", "G2D", "G3D",
                          "R1O", "R2O", "R3O");
            var g = Game.WithDrawOrder(new[] { "A", "B" }, new GameRules { HandSize = 6 }, bag);
            Assert.Equal(6, g.Players[0].Hand.Count);
            Assert.Equal(6, g.Players[1].Hand.Count);
            Assert.Equal(3, g.BagCount);

            var h = g.CurrentPlayer.Hand;
            var r = g.Play(new[] { new Placement(h[0], P("E6")), new Placement(h[1], P("F6")), new Placement(h[2], P("G6")) });
            Assert.True(r.Legal, r.Error);
            Assert.Equal(6, g.Players[0].Score);
            Assert.Equal(6, g.Players[0].Hand.Count);
            Assert.Equal(0, g.BagCount);
            Assert.Equal(1, g.CurrentPlayerIndex);
        }

        [Fact]
        public void Game_RejectsTilesNotInHand()
        {
            var g = new Game(new[] { "A", "B" }, null, 1);
            var foreign = g.Players[1].Hand[0];
            var r = g.Play(new[] { new Placement(foreign, Board.Start) });
            Assert.False(r.Legal);
            Assert.Equal(0, g.CurrentPlayerIndex);
        }

        [Fact]
        public void Game_IllegalPlay_ChangesNothing()
        {
            var g = new Game(new[] { "A", "B" }, null, 1);
            var h = g.CurrentPlayer.Hand;
            var r = g.Play(new[] { new Placement(h[0], P("A1")) }); // misses START
            Assert.False(r.Legal);
            Assert.True(g.Board.IsEmpty);
            Assert.Equal(7, h.Count);
        }

        [Fact]
        public void Game_Exchange_KeepsHandSizeAndPassesTurn()
        {
            var g = new Game(new[] { "A", "B" }, null, 7);
            var bagBefore = g.BagCount;
            var old = g.CurrentPlayer.Hand.Take(2).ToList();
            Assert.Null(g.Exchange(old));
            Assert.Equal(7, g.Players[0].Hand.Count);
            Assert.Equal(bagBefore, g.BagCount);
            Assert.Equal(1, g.CurrentPlayerIndex);
        }

        [Fact]
        public void Game_EndsWhenEveryonePassesInARow()
        {
            var g = new Game(new[] { "A", "B", "C" }, null, 3);
            g.Pass(); g.Pass();
            Assert.False(g.IsOver);
            g.Pass();
            Assert.True(g.IsOver);
        }

        [Fact]
        public void Game_SwapIsNotAPass_AndBreaksTheRun()
        {
            var g = new Game(new[] { "A", "B" }, null, 3);
            g.Pass();
            Assert.Null(g.Exchange(g.CurrentPlayer.Hand.Take(2).ToList()));
            g.Pass();
            Assert.False(g.IsOver);
            g.Pass();
            Assert.True(g.IsOver);
        }

        [Fact]
        public void Game_SwapAnyNumber_UpToWholeHand()
        {
            var g = new Game(new[] { "A", "B" }, null, 5);
            Assert.Null(g.Exchange(g.CurrentPlayer.Hand.ToList()));
            Assert.Equal(7, g.Players[0].Hand.Count);
        }

        [Fact]
        public void Game_PlayerOutOfTilesWithEmptyBag_PassesAutomatically()
        {
            // A gets 3 tiles, B gets 3 tiles, bag then empty.
            var rules = new GameRules { HandSize = 3 };
            var bag = Bag("R1D", "R2D", "R3D", "B1O", "B2O", "B3O");
            var g = Game.WithDrawOrder(new[] { "A", "B" }, rules, bag);
            var h = g.CurrentPlayer.Hand;
            Assert.True(g.Play(new[] { new Placement(h[0], P("E6")), new Placement(h[1], P("F6")), new Placement(h[2], P("G6")) }).Legal);
            Assert.Equal(1, g.CurrentPlayerIndex);
            g.Pass(); // B passes, A auto-passes (no tiles) -> everyone passed
            Assert.True(g.IsOver);
        }

        [Fact]
        public void Game_SeededGamesAreRepeatable()
        {
            var a = new Game(new[] { "A", "B" }, null, 42);
            var b = new Game(new[] { "A", "B" }, null, 42);
            Assert.Equal(a.Players[0].Hand.Select(t => t.Id), b.Players[0].Hand.Select(t => t.Id));
        }
    }
}
