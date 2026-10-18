# Align Digital

Digital version of Align, a Mount Baker Games tile-laying territory game.

- `src/Align.Engine` — rules engine (netstandard2.1, no Unity dependencies; drops into Unity later)
- `src/Align.Console` — hot-seat console client
- `tests/Align.Engine.Tests` — xUnit tests
- `art/` — tile and board art (Board v1.0)
- `RULES.md` — rules as implemented, plus open questions

## Run
```
dotnet test
dotnet run --project src/Align.Console
dotnet run --project src/Align.Console -- --seed 42
```

In the console, squares are column letter + row number (A1 top-left, F6 = START). Tiles show as
color + number + symbol, e.g. `R2D` = Red 2 Diamond. Play with hand slot + square: `1F6 2G6 3H6`.
Preview with `? 1F6 2G6 3H6`. Type `help` for everything else.
