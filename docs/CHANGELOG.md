# Changelog

## v0.8.0 Move Ordering: Transposition Table Move

```
Order transposition table moves first.

Time Control:  inf/10+0.0
Brain Capacity: 830/1024 (81.05% full) [+6 / +0.59%]

Score of Myte 0.8.0 vs Myte 0.8.0-baseline: 113 - 33 - 35  [0.721] 181
...      Myte 0.8.0 playing White: 55 - 17 - 19  [0.709] 91
...      Myte 0.8.0 playing Black: 58 - 16 - 16  [0.733] 90
...      White vs Black: 71 - 75 - 35  [0.489] 181
Elo difference: 164.9 +/- 49.6, LOS: 100.0 %, DrawRatio: 19.3 %
SPRT: llr 2.95 (100.1%), lbound -2.94, ubound 2.94 - H1 was accepted
Elo Gain Bounds: [115.3, 214.5]
```

## v0.7.0 Inline move ranking and remove LINQ usage

```
Inline move ranking and ordering and replace LINQ usage with Array.sort().

Time Control:  inf/10+0.0
Brain Capacity: 824/1024 (80.47% full) [-16 / -1.56%]

Score of Myte 0.7.0 vs Myte 0.7.0-baseline: 227 - 140 - 96  [0.594] 463
...      Myte 0.7.0 playing White: 109 - 72 - 51  [0.580] 232
...      Myte 0.7.0 playing Black: 118 - 68 - 45  [0.608] 231
...      White vs Black: 177 - 190 - 96  [0.486] 463
Elo difference: 66.1 +/- 28.6, LOS: 100.0 %, DrawRatio: 20.7 %
SPRT: llr 2.95 (100.1%), lbound -2.94, ubound 2.94 - H1 was accepted
Elo Gain Bounds: [37.5, 94.7]
```

## v0.6.0 Optimize Evaluation
```
Change evaluation function to use bitboards and combine the white/black score accumulators.

Time Control:  inf/10+0.0
Brain Capacity: 840/1024 (82.03% full) [-54 / -5.23%]

Score of Myte 0.6.0 vs Myte 0.6.0-baseline: 103 - 27 - 37  [0.728] 167
...      Myte 0.6.0 playing White: 51 - 14 - 19  [0.720] 84
...      Myte 0.6.0 playing Black: 52 - 13 - 18  [0.735] 83
...      White vs Black: 64 - 66 - 37  [0.494] 167
Elo difference: 170.6 +/- 50.7, LOS: 100.0 %, DrawRatio: 22.2 %
SPRT: llr 2.96 (100.7%), lbound -2.94, ubound 2.94 - H1 was accepted
Elo Gain Bounds: [119.9, 221.3]
```

## v0.5.1 Reduce token usage in evaluation
```
(Regression check) Reduce token usage in evaluation and re-arrange scores to be contiguous.

Time Control:  inf/10+0.0
Brain Capacity: 894/1024 (87.30% full) [-10 / -0.98%]

Score of Myte 0.6.0 vs Myte 0.6.0-baseline: 69 - 65 - 28  [0.512] 162
...      Myte 0.6.0 playing White: 32 - 35 - 14  [0.481] 81
...      Myte 0.6.0 playing Black: 37 - 30 - 14  [0.543] 81
...      White vs Black: 62 - 72 - 28  [0.469] 162
Elo difference: 8.6 +/- 48.9, LOS: 63.5 %, DrawRatio: 17.3 %
SPRT: llr 0.0581 (2.0%), lbound -2.94, ubound 2.94
Elo Gain Bounds: [-40.3, 57.5]
```

## v0.5.0 PST Tapered Evaluation
```
Add tapered evaluation for piece-square tables.

Time Control:  inf/10+0.0
Brain Capacity: 904/1024 (88.28% full) [+64 / +6.25%]

Score of Myte 0.5.0 vs Myte 0.5.0-baseline: 228 - 133 - 54  [0.614] 415
...      Myte 0.5.0 playing White: 115 - 63 - 29  [0.626] 207
...      Myte 0.5.0 playing Black: 113 - 70 - 25  [0.603] 208
...      White vs Black: 185 - 176 - 54  [0.511] 415
Elo difference: 81.0 +/- 32.0, LOS: 100.0 %, DrawRatio: 13.0 %
SPRT: llr 2.97 (100.9%), lbound -2.94, ubound 2.94 - H1 was accepted
Elo Gain Bounds: [49.0, 113.0]
```

## v0.4.0 Piece Square Table Evaluation

```
Include midgame piece-square tables in evaluation.

Time Control:  inf/10+0.0
Brain Capacity: 840/1024 (82.03% full) [+70 / +6.84%]

Score of Myte 0.4.0 vs Myte 0.4.0-baseline: 92 - 11 - 13  [0.849] 116
...      Myte 0.4.0 playing White: 44 - 5 - 8  [0.842] 57
...      Myte 0.4.0 playing Black: 48 - 6 - 5  [0.856] 59
...      White vs Black: 50 - 53 - 13  [0.487] 116
Elo difference: 300.2 +/- 81.5, LOS: 100.0 %, DrawRatio: 11.2 %
SPRT: llr 2.95 (100.1%), lbound -2.94, ubound 2.94 - H1 was accepted
Elo Gain Bounds: [218.7, 381.7]
```


## v0.3.0 Transposition Table + Inlining Quiescence Search

```
Add transposition table with insertion and probing.
Merge quiescence search with standard search and control with conditions.

Time Control:  inf/10+0.0
Brain Capacity: 770/1024 (75.20% full) [+296 / +28.91%]

Score of Myte 0.3.0 vs Myte 0.3.0-baseline: 112 - 74 - 486  [0.528] 672
...      Myte 0.3.0 playing White: 91 - 33 - 211  [0.587] 335
...      Myte 0.3.0 playing Black: 21 - 41 - 275  [0.470] 337
...      White vs Black: 132 - 54 - 486  [0.558] 672
Elo difference: 19.7 +/- 13.8, LOS: 99.7 %, DrawRatio: 72.3 %
SPRT: llr 3 (101.9%), lbound -2.94, ubound 2.94 - H1 was accepted
Elo Gain Bounds: [5.9, 33.5]
```

## v0.2.0 MVV-LVA Move Ordering

```
Add Most-Valuable-Victim-Least-Valuable-Attacker move sorting to move generation.

Time Control:   inf/10+0.0
Brain Capacity: 474/1024 (46.29% full) [+78 / +7.62%]

Score of Myte 0.2.0 vs Myte 0.2.0-baseline: 26 - 2 - 45  [0.664] 73
...      Myte 0.2.0 playing White: 16 - 1 - 21  [0.697] 38
...      Myte 0.2.0 playing Black: 10 - 1 - 24  [0.629] 35
...      White vs Black: 17 - 11 - 45  [0.541] 73
Elo difference: 118.6 +/- 47.2, LOS: 100.0 %, DrawRatio: 61.6 %
SPRT: llr 2.99 (101.5%), lbound -2.94, ubound 2.94 - H1 was accepted
Elo Gain Bounds: [71.4, 165.8]
```

## v0.1.0 Alpha-Beta Quiescence Search, Basic Material Score

```
Set up a negamax alpha-beta quiescence search that calculates a basic 
100/300/300/500/900 piece material score on the fly. Use 1/20th of the
remaining clock time per turn.

Time Control:   inf/10+0.0
Brain Capacity: 396/1024 (38.67% full) [+0 / +0.00%]
Score of Myte 0.1.0 vs Myte 0.1.0-baseline: 349 - 1 - 1  [0.996] 351
...      Myte 0.1.0 playing White: 174 - 1 - 1  [0.991] 176
...      Myte 0.1.0 playing Black: 175 - 0 - 0  [1.000] 175
...      White vs Black: 174 - 176 - 1  [0.497] 351
Elo difference: 946.9 +/- nan, LOS: 100.0 %, DrawRatio: 0.3 %
SPRT: llr 12 (405.9%), lbound -2.94, ubound 2.94 - H1 was accepted
Elo Gain Bounds: [0, 0]
```