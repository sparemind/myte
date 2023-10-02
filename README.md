# myte - A Tiny Chess Engine

Myte is a tiny C# chess bot created for the [Chess Coding Challenge](https://github.com/SebLague/Chess-Challenge) by [Sebastian Lague](https://www.youtube.com/@SebastianLague).

- [CHANGELOG.md](CHANGELOG.md) contains a history of feature changes and test results

## Features

- Engine
  - Time Control Management
- Search
  - Negamax Alpha-Beta search
  - Principal Variation Search
  - Quiescence Search
  - Iterative Deepening
  - Transposition Table
  - Check Extensions
  - Move Ordering
    - Transposition Table Moves
    - MVV-LVA
    - Killer Moves Heuristic
    - Countermove Heuristic
  - Null Move Pruning
  - Static Null Move Pruning
- Evaluation
  - Tapered Evaluation
  - Material Evaluation
  - Piece-Square Tables
  - Bishop Pairs
- Tuning
  - Texel Tuning with gradient descent
 
## Makefile Targets
```
build           Build all executables
build-gui       Build the regular chess GUI
build-uci       Build the UCI program interface

run             Run the regular chess GUI
uci             Run the bot in UCI mode
uci-baseline    Run the baseline bot in UCI mode

baseline        Copy the source code of the current bot to the baseline bot
```
