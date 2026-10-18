# Align — rules as implemented

## Components
- 81 base tiles: Red/Green/Blue × Diamond/Oval/Squiggle × 1/2/3, 3 copies each.
- 6 starred tiles, 1 each: Black Diamond 3, Purple Oval 1, Yellow Squiggle 2, Blue X 2, Green Circle 3, Red Heart 1.
- 11×11 board. START at F6. +2: F2, B6, J6, F10. ×2: A1, K1, A11, K11, E5, G5, E7, G7. ×4: C3, I3, C9, I9.

## Ruled (Brad)
- A **word** is a line of 3+ tiles that all share at least one trait (color, symbol or number). Tiles may repeat.
- Any orthogonal line of 2 tiles is illegal. Tiles that touch must make a word of 3+.
- A play is any number of tiles in **one row or one column**. They do not have to touch each other.
- You score **every word** your play makes or extends. Every tile in every word scores, so a tile in two words scores twice.
- Premium squares change a tile's value only on the turn it is placed there.
- Starred tiles score 2 every time they are scored (not their printed number).
- Tile value = its number.

## Placeholder defaults (change in `GameRules.cs`)
| Setting | Default | Open question |
|---|---|---|
| `HandSize` | 6 | Real hand size? |
| `FirstPlayMustCoverStart` | true | Does the first play have to cover START? |
| `PlayMustTouchBoard` | false | After the first play, must a play touch the board, or can you start a new island anywhere? |
| `AllowExchange` | true | Can you swap tiles instead of playing? |
| `ScorelessRoundsToEnd` | 2 | End condition (unknown). Currently: bag empty and a player plays out, or 2 full rounds of only passes/swaps. |
| Starred tile on a premium | premium applies to the flat 2 | Does a starred tile on ×4 score 8, or always 2? |
| +2 / ×2 / ×4 | applies to the tile's value | Confirm these are tile premiums, not word premiums. |
