namespace Align.Engine
{
    /// <summary>
    /// Every rule that is a number or a toggle lives here, so open design questions
    /// are a settings change rather than a code change. Defaults marked GUESS are
    /// placeholders waiting on a ruling (see RULES.md).
    /// </summary>
    public sealed class GameRules
    {
        /// <summary>Copies of each of the 27 base tiles. Ruled: 3.</summary>
        public int CopiesOfEachBaseTile { get; set; } = 3;

        /// <summary>Starred tiles, 1 of each. Ruled: included.</summary>
        public bool IncludeStarredTiles { get; set; } = true;

        /// <summary>Starred tiles always score this, every time they are scored. Ruled: 2.</summary>
        public int StarredTileValue { get; set; } = 2;

        /// <summary>Minimum tiles in a word. Any 2-tile line is illegal. Ruled: 3.</summary>
        public int MinWordLength { get; set; } = 3;

        /// <summary>Tiles in hand. GUESS: 6.</summary>
        public int HandSize { get; set; } = 6;

        /// <summary>The first play of the game must cover START. GUESS: true.</summary>
        public bool FirstPlayMustCoverStart { get; set; } = true;

        /// <summary>
        /// After the first play, at least one placed tile must touch a tile already on the board.
        /// The placed tiles don't have to touch each other. Ruled: true.
        /// </summary>
        public bool PlayMustTouchBoard { get; set; } = true;

        /// <summary>Players may exchange tiles instead of playing. GUESS: true.</summary>
        public bool AllowExchange { get; set; } = true;

        /// <summary>
        /// Placeholder end condition until the real one is recalled: the game ends when the bag
        /// is empty and a player empties their hand, or after this many full rounds in which
        /// every turn was a pass or exchange.
        /// </summary>
        public int ScorelessRoundsToEnd { get; set; } = 2;
    }
}
