# Align simulation report

200 games per configuration. Rules: hand 7, tile premiums, starred +1, board-lock end.
Bots swap their whole hand when they have no legal play (max 2 swaps in a row), then pass.

## 2 players: Greedy vs Greedy

| Measure | Value |
|---|---|
| Games finished / aborted (hit turn cap) | 200 / 0 |
| Turns per game | 20.7 (min 18, max 26) |
| Rounds per game | 10.6 (min 9, max 13) |
| Tiles on board at end (of 121 squares, 87 tiles) | 81.3 (min 71, max 87) |
| Tiles never played (bag + hands) | 5.7 (min 0, max 16) |
| Bag at end | 0.0 (min 0, max 2) |
| End was a true lock (no one could play) | 100.0% |
| Score per player | 245.6 (min 156, max 356) |
| Winning score | 258.3 (min 176, max 356) |
| Winning margin | 25.4 (min 0, max 88) |
| Ties for first | 1.0% |
| Wins by seat | P1 42.0%, P2 59.0% |
| Avg score by seat | P1 242.7, P2 248.4 |
| Avg plays by seat | P1 9.47, P2 9.05 |
| Avg first-turn score by seat | P1 14.7, P2 32.7 |
| Turn mix: play / swap / pass | 89.6% / 0.3% / 10.1% |
| Stuck turns while tiles remained in bag | 0.5% |
| Tiles per play (avg) | 4.39 |
| Tiles per play (1..7) | 1:9.1% 2:15.9% 3:12.7% 4:11.1% 5:10.6% 6:21.8% 7:18.8% |
| Words per play (avg) | 2.75 |
| Plays scoring in both directions | 67.6% |
| Points per play (avg) / best single play | 26.5 / 74 |
| Plays of 30+ points | 39.4% |
| Words by shared trait | Number 33.5%, Color 32.5%, Symbol 31.2%, Symbol+Number 1.1%, Color+Number 1.0%, Color+Symbol 0.8%, Color+Symbol+Number 0.0% |
| Premium squares covered per game | Plus2 3.2/4, Times2 6.8/8, Times4 4.0/4 |
| Starred tiles played per game | 5.6/6 |
| Bot think time per turn (avg / max) | 7.0 ms / 199 ms |

## 3 players: Greedy x3

| Measure | Value |
|---|---|
| Games finished / aborted (hit turn cap) | 200 / 0 |
| Turns per game | 21.7 (min 17, max 28) |
| Rounds per game | 7.6 (min 6, max 10) |
| Tiles on board at end (of 121 squares, 87 tiles) | 80.9 (min 71, max 87) |
| Tiles never played (bag + hands) | 6.1 (min 0, max 16) |
| Bag at end | 0.0 (min 0, max 0) |
| End was a true lock (no one could play) | 100.0% |
| Score per player | 165.3 (min 95, max 253) |
| Winning score | 182.8 (min 118, max 253) |
| Winning margin | 18.4 (min 0, max 102) |
| Ties for first | 2.0% |
| Wins by seat | P1 31.0%, P2 47.5%, P3 23.5% |
| Avg score by seat | P1 164.0, P2 172.1, P3 159.7 |
| Avg plays by seat | P1 6.48, P2 6.23, P3 5.93 |
| Avg first-turn score by seat | P1 14.7, P2 32.7, P3 30.9 |
| Turn mix: play / swap / pass | 85.8% / 0.0% / 14.2% |
| Stuck turns while tiles remained in bag | 0.0% |
| Tiles per play (avg) | 4.34 |
| Tiles per play (1..7) | 1:11.6% 2:15.7% 3:10.6% 4:11.2% 5:10.4% 6:21.0% 7:19.4% |
| Words per play (avg) | 2.75 |
| Plays scoring in both directions | 67.2% |
| Points per play (avg) / best single play | 26.6 / 83 |
| Plays of 30+ points | 40.8% |
| Words by shared trait | Number 33.5%, Symbol 32.0%, Color 31.2%, Symbol+Number 1.3%, Color+Number 1.2%, Color+Symbol 0.9%, Color+Symbol+Number 0.0% |
| Premium squares covered per game | Plus2 3.0/4, Times2 6.7/8, Times4 3.9/4 |
| Starred tiles played per game | 5.5/6 |
| Bot think time per turn (avg / max) | 6.8 ms / 186 ms |

## 4 players: Greedy x4

| Measure | Value |
|---|---|
| Games finished / aborted (hit turn cap) | 200 / 0 |
| Turns per game | 22.6 (min 17, max 31) |
| Rounds per game | 6.0 (min 5, max 8) |
| Tiles on board at end (of 121 squares, 87 tiles) | 80.2 (min 72, max 86) |
| Tiles never played (bag + hands) | 6.8 (min 1, max 15) |
| Bag at end | 0.0 (min 0, max 0) |
| End was a true lock (no one could play) | 100.0% |
| Score per player | 123.7 (min 67, max 229) |
| Winning score | 143.8 (min 97, max 229) |
| Winning margin | 14.9 (min 0, max 75) |
| Ties for first | 3.5% |
| Wins by seat | P1 21.0%, P2 56.5%, P3 18.0%, P4 8.0% |
| Avg score by seat | P1 121.7, P2 136.6, P3 122.7, P4 113.7 |
| Avg plays by seat | P1 5.04, P2 4.95, P3 4.50, P4 4.22 |
| Avg first-turn score by seat | P1 14.7, P2 32.7, P3 30.9, P4 32.9 |
| Turn mix: play / swap / pass | 82.6% / 0.0% / 17.4% |
| Stuck turns while tiles remained in bag | 0.0% |
| Tiles per play (avg) | 4.29 |
| Tiles per play (1..7) | 1:12.7% 2:15.3% 3:11.7% 4:10.5% 5:10.0% 6:20.9% 7:18.9% |
| Words per play (avg) | 2.72 |
| Plays scoring in both directions | 66.1% |
| Points per play (avg) / best single play | 26.4 / 87 |
| Plays of 30+ points | 40.9% |
| Words by shared trait | Number 32.8%, Symbol 32.4%, Color 31.3%, Color+Number 1.4%, Symbol+Number 1.1%, Color+Symbol 0.9%, Color+Symbol+Number 0.0% |
| Premium squares covered per game | Plus2 3.1/4, Times2 6.5/8, Times4 3.9/4 |
| Starred tiles played per game | 5.5/6 |
| Bot think time per turn (avg / max) | 6.2 ms / 233 ms |

## 2 players: Greedy vs Random (seats alternate)

| Measure | Value |
|---|---|
| Games finished / aborted (hit turn cap) | 200 / 0 |
| Turns per game | 23.4 (min 18, max 29) |
| Rounds per game | 12.0 (min 9, max 15) |
| Tiles on board at end (of 121 squares, 87 tiles) | 79.8 (min 70, max 87) |
| Tiles never played (bag + hands) | 7.2 (min 0, max 17) |
| Bag at end | 0.0 (min 0, max 3) |
| End was a true lock (no one could play) | 100.0% |
| Score per player | 230.5 (min 111, max 385) |
| Winning score | 299.9 (min 217, max 385) |
| Winning margin | 138.7 (min 41, max 248) |
| Ties for first | 0.0% |
| Wins by seat | P1 50.5%, P2 49.5% |
| Avg score by seat | P1 232.9, P2 228.2 |
| Avg plays by seat | P1 10.75, P2 10.25 |
| Avg first-turn score by seat | P1 13.8, P2 24.3 |
| Wins by bot | Greedy 99.5% (avg 299.7 pts), Random 0.5% (avg 161.4 pts) |
| Turn mix: play / swap / pass | 89.7% / 0.4% / 9.8% |
| Stuck turns while tiles remained in bag | 0.6% |
| Tiles per play (avg) | 3.80 |
| Tiles per play (1..7) | 1:15.8% 2:19.1% 3:12.5% 4:10.4% 5:15.4% 6:17.4% 7:9.3% |
| Words per play (avg) | 2.37 |
| Plays scoring in both directions | 53.2% |
| Points per play (avg) / best single play | 22.0 / 69 |
| Plays of 30+ points | 28.2% |
| Words by shared trait | Number 34.7%, Color 31.1%, Symbol 30.5%, Color+Number 1.4%, Color+Symbol 1.2%, Symbol+Number 1.1% |
| Premium squares covered per game | Plus2 2.8/4, Times2 5.9/8, Times4 3.7/4 |
| Starred tiles played per game | 5.3/6 |
| Bot think time per turn (avg / max) | 5.5 ms / 231 ms |

## 2 players: Greedy vs Efficient (seats alternate)

| Measure | Value |
|---|---|
| Games finished / aborted (hit turn cap) | 200 / 0 |
| Turns per game | 21.7 (min 17, max 27) |
| Rounds per game | 11.1 (min 9, max 14) |
| Tiles on board at end (of 121 squares, 87 tiles) | 81.7 (min 71, max 87) |
| Tiles never played (bag + hands) | 5.3 (min 0, max 16) |
| Bag at end | 0.0 (min 0, max 2) |
| End was a true lock (no one could play) | 100.0% |
| Score per player | 264.5 (min 157, max 400) |
| Winning score | 280.6 (min 194, max 400) |
| Winning margin | 32.3 (min 0, max 114) |
| Ties for first | 1.5% |
| Wins by seat | P1 46.5%, P2 55.0% |
| Avg score by seat | P1 261.8, P2 267.2 |
| Avg plays by seat | P1 10.07, P2 9.55 |
| Avg first-turn score by seat | P1 14.5, P2 32.4 |
| Wins by bot | Greedy 62.2% (avg 271.4 pts), Efficient 37.8% (avg 257.5 pts) |
| Turn mix: play / swap / pass | 90.3% / 0.3% / 9.4% |
| Stuck turns while tiles remained in bag | 0.3% |
| Tiles per play (avg) | 4.16 |
| Tiles per play (1..7) | 1:9.6% 2:17.7% 3:14.1% 4:12.1% 5:11.7% 6:21.4% 7:13.5% |
| Words per play (avg) | 2.78 |
| Plays scoring in both directions | 68.2% |
| Points per play (avg) / best single play | 27.0 / 84 |
| Plays of 30+ points | 40.3% |
| Words by shared trait | Color 32.9%, Number 32.8%, Symbol 31.7%, Color+Number 0.9%, Symbol+Number 0.8%, Color+Symbol 0.8%, Color+Symbol+Number 0.0% |
| Premium squares covered per game | Plus2 3.2/4, Times2 6.5/8, Times4 4.0/4 |
| Starred tiles played per game | 5.6/6 |
| Bot think time per turn (avg / max) | 7.8 ms / 208 ms |

## Sample final board (first 2-player Greedy game)

```
     A   B   C   D   E   F   G   H   I   J   K  
  1 B3D B1O B2O  .  R3O B3O G3O R2O B1O B3O R3O 
  2 B3O  .   .  G2O  .  R1O  .   .   .  G3S  .  
  3 B1D B1S R1H*G1O  .  G2O G2S G1S G3S G3C*G2S 
  4  .  B2X* .  G3O  .   .  R2S  .  B3S  .  Y2S*
  5  .  B2D  .   .  B1D B2D B1S  .  G1S G1S R3S 
  6 B2S B2S G2S R1S  .  B3D G3S G3D  .  B2S  .  
  7  .   .   .  R1S x2  G2D x2  G2O  .  R3S  .  
  8  .  R2S  .  R1O  .  G2D B3D G3D  .   .   .  
  9 G3D R3D G2D R1D R2D R1D K3D* .  B3S R1S B3S 
 10  .  R3O  .  R3D R3D B2D G1D G1D  .  G1O  .  
 11 R2O R1O R1D R2S R3S  .  G1D  .  P1O*B1O G3O 
```

Total run time: 146.9s
