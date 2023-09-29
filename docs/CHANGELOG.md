# Changelog

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