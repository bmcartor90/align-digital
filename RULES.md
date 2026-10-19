# Align — rules as implemented

## Components
- 81 base tiles: Red/Green/Blue × Diamond/Oval/Squiggle × 1/2/3, 3 copies each.
- 6 starred tiles, 1 each: Black Diamond 3, Purple Oval 1, Yellow Squiggle 2, Blue X 2, Green Circle 3, Red Heart 1.
- 11×11 board. START at F6. +2: F2, B6, J6, F10. ×2: A1, K1, A11, K11, E5, G5, E7, G7. ×4: C3, I3, C9, I9.

## Ruled (Brad)
- A **word** is a line of 3+ tiles that all share at least one trait (color, symbol or number). Tiles may repeat.
- Any orthogonal line of 2 tiles is illegal. Tiles that touch must make a word of 3+.
- A play is any number of tiles in **one row or one column**. They do not have to touch each other.
- After the first play, a play must touch at least one tile already on the board.
- You score **every word** your play makes or extends. Every tile in every word scores, so a tile in two words scores twice.
- All premiums (+2, ×2, ×4) are **tile** premiums: they change that tile's value, in every word it is part of, only on the turn it is placed there. There are no word premiums.
- Starred tiles are worth their number +1, every time they are scored. A premium square applies after the +1 (B2X on ×4 = 12).
- The first play must cover START.
- Hand of 7.
- Instead of playing, you may swap any number of tiles (bag permitting). It only costs your turn.
- **Game end (board lock):** the game ends when every player passes in a row. A swap is not a pass and breaks the run. A player with no tiles and an empty bag passes automatically. Highest score wins.
- Tile value = its number.

## Open
- None. All rules currently known are implemented.
