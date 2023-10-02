using System;
using System.Collections.Generic;
using System.IO; // TODO #DEBUG
using ChessChallenge.API;
using ChessChallenge.Chess;
using Board = ChessChallenge.API.Board;
using Move = ChessChallenge.API.Move;

public class MyBot : IChessBot
{
    // Transposition table. Entries are TODO size 
    // (hash, bestMove, depth, score, nodeType)
    readonly (ulong, Move, int, int, byte)[] tt = new (ulong, Move, int, int, byte)[16777216];

    public Move Think(Board board, Timer timer)
    {
        var pvStack = new Stack<LinkedList<Move>>(); // TODO #DEBUG

        void ComputeTrainingWeights()
        {
            var inputFile = "../training_positions.txt";
            var outputFile = "../training_weights.txt";

            using (var reader = new StreamReader(inputFile))
            using (var writer = new StreamWriter(outputFile))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var pieces = line.Split('#');
                    var gameResult = pieces[0];
                    var score = pieces[1];
                    var fen = pieces[2];

                    board = Board.CreateBoardFromFEN(fen);
                    if (board.IsInCheck()) continue;

                    pvStack.Push(new LinkedList<Move>());
                    var newScore = search(0, 0, -99999999, 99999999); // Stable Qsearch
                    var pv = pvStack.Pop();

                    if (Math.Abs((float)Convert.ToDouble(score) - newScore) > 70.0) continue;

                    var b = Board.CreateBoardFromFEN(board.GetFenString());
                    foreach (var move in pv) b.MakeMove(move);

                    // b = Board.CreateBoardFromFEN("2qqkq2/pppppppp/8/8/8/1PPPPPPP/P7/2QQKQ2 w - - 0 1");

                    // Compute Coefficients
                    double phase = 0;
                    foreach (var pieceList in b.GetAllPieceLists())
                    foreach (var piece in pieceList)
                        phase += new[] { 0, 0, 1, 1, 2, 4, 0 }[(int)piece.PieceType];
                    var mgPhase = phase / 24.0;
                    var egPhase = 1.0 - mgPhase;

                    // var coefficients = new List<double>();

                    writer.Write($"{gameResult},");
                    var printComma = true;
                    foreach (var phaseWeight in new[] { mgPhase, egPhase })
                    {
                        // Material value
                        foreach (var pieceType in new[] { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen, PieceType.King })
                        {
                            var whiteCount = BitBoardUtility.PopCount(b.GetPieceBitboard(pieceType, true));
                            var blackCount = BitBoardUtility.PopCount(b.GetPieceBitboard(pieceType, false));
                            var c = whiteCount - blackCount;
                            // coefficients.Add(c * phaseWeight);
                            writer.Write($"{c * phaseWeight},");
                        }

                        // Piece-Square Tables
                        foreach (var pieceType in new[] { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen, PieceType.King })
                            for (var sq = 0; sq < 64; sq++)
                            {
                                var blackSq = new Square(sq);
                                var whiteSq = new Square(sq ^ 56);

                                var whitePiece = b.GetPiece(whiteSq);
                                var blackPiece = b.GetPiece(blackSq);

                                var c = 0;
                                if (whitePiece.PieceType == pieceType && whitePiece.IsWhite) c++;
                                if (blackPiece.PieceType == pieceType && !blackPiece.IsWhite) c--;
                                // coefficients.Add(c * phaseWeight);
                                writer.Write($"{c * phaseWeight},");

                                // if (sq % 8 == 7) writer.WriteLine();
                                // if (sq == 63) writer.WriteLine();
                            }

                        // Bishop Pair
                        {
                            var whiteBishopPair = b.GetPieceList(PieceType.Bishop, true).Count > 1;
                            var blackBishopPair = b.GetPieceList(PieceType.Bishop, false).Count > 1;
                            var c = 0;
                            if (whiteBishopPair) c++;
                            if (blackBishopPair) c--;
                            // coefficients.Add(c * phaseWeight);
                            writer.Write($"{c * phaseWeight}");
                            if (printComma)
                            {
                                printComma = false;
                                writer.Write(",");
                            }
                        }
                    }

                    // Console.WriteLine($"{gameResult}\t{score} {newScore}\t{b.GetFenString()}");
                    // foreach (var c in coefficients)
                    // writer.Write($"{c} ");
                    writer.WriteLine();

                    // Console.WriteLine();
                    // Console.WriteLine(coefficients.Count);
                    // Console.WriteLine(b.GetFenString()); 
                    // throw new Exception("Done! Early exiting via exception");
                }
            }

            throw new Exception("Done! Early exiting via exception");
        }

        int[] staticData =
        {
            // 0, 100, 315, 324, 410, 977, 0, 124, 200, 229, 429, 687, 0
            // 0, 100, 336, 344, 436, 1039, 0, 125, 214, 242, 457, 731, 0
            // 0, 100, 338, 344, 439, 1044, 0, 126, 216, 242, 460, 735, 0
            0, 100, 335, 343, 436, 1037, 0, 125, 213, 242, 457, 730, 0

            // 0, //// PieceType.None
            // 100, // PieceType.Pawn
            // 332, // PieceType.Knight
            // 343, // PieceType.Bishop
            // 433, // PieceType.Rook
            // 1030, // PieceType.Queen
            // 0, ///// PieceType.King
            // // endgame
            // 125, // PieceType.Pawn
            // 210, // PieceType.Knight
            // 243, // PieceType.Bishop
            // 454, // PieceType.Rook
            // 725, // PieceType.Queen
            // 0 ///// PieceType.King
        };

        // 0 = midgame black
        // 1 = midgame white
        // 2 = endgame black
        // 3 = endgame white
        var dynamicData = new int[8192]; // 8 * 1024
        var killerMoves = new Move[128];
        // var counterMoves = new Move[2, 64, 64];
        var counterMoves = new Move[8192]; // 64x64x2 table
        // var history = new int[8192]; // 64x64x2 butterfly table

        // int _i = 0;

        void populateData()
        {
            // initData consists of chunks of six 64-bit bitmaps, one for each PieceType/GamePhase combination.
            ulong[] initData =
            {
                // 0x0000a841c30338f2, 0x000195e18704359c, 0x0000690047880794, 0x00017c00040017ae, 0x0000300000001492, 0x00014c0000002bec, // baselines + constants
                // 0xffd2e707398deeff, 0xfff44d31ae15f9ff, 0xff31ebb3b79f9fff, 0x00413135a6070f00, 0x0086beb6a778ff00, 0xff60404858ffffff, // midgame pawn
                // 0x6837c2d7387c1b00, 0x49eec3c03d6ae712, 0x41dd2a547dd77d83, 0x8026ae73fce23d7c, 0xb5f92cc2e5c3feff, 0x0000503c3a3c0000, // midgame knight
                // 0x89c18390666da000, 0x9a955c639a8afbff, 0xcb62c24cedc92808, 0x1795fb0a76ac73f7, 0xc01ffbdbcf0dfeff, 0x0060043c38f20000, // midgame bishop
                // 0x39d422d20ea0ccff, 0xa2dc94f13491a9e0, 0xb68c8cb587991d3b, 0x81b412b941e33e33, 0x382463c2397dfccc, 0xff7afc7cfefeffff, // midgame rook
                // 0x01f7fb28cb30ebb5, 0x010b393fe7b8a376, 0x4bfcd9b90d4f1dee, 0x0fbfd649c5ac8bf6, 0xd83c9f0e36f600e7, 0x000060f0f8f8fc18, // midgame queen
                // 0xcf5ceba8e9a45200, 0x13dc187d8fe65134, 0xdf089882fe5faec0, 0x92d0888080000000, 0x5b28988080000000, 0xc6c7677f7fffffff, // midgame king
                // 0xff1cd88185ef66ff, 0xff1f972130eae7ff, 0x00ac6b86280c2200, 0x0053004184ea8f00, 0xff00000043e8ffff, 0x000000000017ff00, // endgame pawn
                // 0xe1fdd3ffcb517966, 0x6602372593095c99, 0x32db0431dd319619, 0x51a9932fe76add81, 0x807074e05ac63e7e, 0x0000081c2c3c0000, // endgame knight
                // 0xa18ec2116a00c8f0, 0xc01725b228c6c9f3, 0x296ded8161a40ae1, 0x019958b6203bf614, 0xd22ea373619fffff, 0x00001c0c9e600000, // endgame bishop
                // 0x5895b23d2f624716, 0x02d6709280786068, 0x4f75d0e00427a543, 0x44ebafcfc0801a41, 0x3f3e7e403f7fffbe, 0x0000013fffffffff, // endgame rook
                // 0x25ee61e412b5e326, 0xa0a861a4c1686aa3, 0x54971f59c38be727, 0x55093f21f5d407e3, 0x03f73f0939391bfa, 0x0000c0fefefefc1c, // endgame queen
                // 0xec1bb25ff8cc3c77, 0x10594f396516ef41, 0x39ab7985f45f7e3b, 0x440a9977efdf38f2, 0x1cb7b5bdbe64e6d8, 0x207c7e7e7fffffe4, // endgame king
                // 0xfefaf3fffff3fefe, 0x0804050000090408, 0x0d0c080000040804, 0x0108000000000900, 0x0c040100000d0405, 0xf7f2fefffff2f2ff // pawn shield mg
                0x0000a841c30338f2, 0x000195e18704359c, 0x0000690047880794, 0x00017c00040017ae, 0x0000300000001492, 0x00014c0000002bec, // baselines + constants
                0xffd2e70f398deeff, 0xfff44d31ae15f9ff, 0xff31ebb3b79f9fff, 0x00413135a6070f00, 0x0086beb6a778ff00, 0xff60404858ffffff, // midgame pawn
                0x6837c2d7387c1b00, 0x49eec3c03d6ae712, 0x41dd2a547dd77d83, 0x8026ae73fce23d7c, 0xb5f92cc2e5c3feff, 0x0000503c3a3c0000, // midgame knight
                0x8dc18390666da000, 0x9e955c639a8afbff, 0xcf62c24cedc92808, 0x1395fb0a76ac73f7, 0xc01ffbdbcf0dfeff, 0x0060043c38f20000, // midgame bishop
                0x29d422d20ea0ccff, 0xb2dc94f13491a9e0, 0xb68c8cb587991d3b, 0x81b412b941e33e33, 0x382463c2397dfccc, 0xff7afc7cfefeffff, // midgame rook
                0x01f7fb28cb30ebb5, 0x010b393fe7b8a376, 0x4bfcd9b90d4f1dee, 0x0fbfd649c5ac8bf6, 0xd83c9f0e36f600e7, 0x000060f0f8f8fc18, // midgame queen
                0xcf5ceba8e9a45200, 0x13dc187d8fe65134, 0xdf089882fe5faec0, 0x92d0888080000000, 0x5b28988080000000, 0xc6c7677f7fffffff, // midgame king
                0xff1cd8e195fb67ff, 0xff1f972130eee6ff, 0x00ac6b86280c2200, 0x0053004184ea8f00, 0xff00000043e8ffff, 0x000000000017ff00, // endgame pawn
                0xe1fdd3ffcb517966, 0x6602372593095c99, 0x32db0431dd319619, 0x51a9932fe76add81, 0x807074e05ac63e7e, 0x0000081c2c3c0000, // endgame knight
                0xa186c2116a00c8f0, 0xc01725b228c6c9f3, 0x296ded8161a40ae1, 0x019958b6203bf614, 0xd22ea373619fffff, 0x00001c0c9e600000, // endgame bishop
                0x5895b23d2f624716, 0x02d6709280786068, 0x4f75d0e00427a543, 0x44ebafcfc0801a41, 0x3f3e7e403f7fffbe, 0x0000013fffffffff, // endgame rook
                0x25ee61e412b5e326, 0xa0a861a4c1686aa3, 0x54971f59c38be727, 0x55093f21f5d407e3, 0x03f73f0939391bfa, 0x0000c0fefefefc1c, // endgame queen
                0xec1bb25ff8cc3c77, 0x10594f396516ef41, 0x39ab7985f45f7e3b, 0x440a9977efdf38f2, 0x1cb7b5bdbe64e6d8, 0x207c7e7e7fffffe4, // endgame king
                0xfefaf3fffff3fefe, 0x0804050000090408, 0x0d0c080000040804, 0x0108000000000900, 0x0c040100000d0405, 0xf7f2fefffff2f2ff // pawn shield mg
            };


            // bitmapIdx / 6 is the PieceType, bitmapIdx % 6 is the bit index
            for (var bitmapIdx = 0; bitmapIdx < 84; bitmapIdx++)
            {
                for (int i = 0, offset = dynamicData[bitmapIdx / 6]; i < 64 && bitmapIdx % 6 == 0 && bitmapIdx / 6 != 0; i++)
                    dynamicData[bitmapIdx / 6 * 64 + i] = offset - 68;
                for (var i = 0; i < 64; i++)
                    dynamicData[bitmapIdx / 6 * 64 + i] +=
                        BitBoardUtility.ContainsSquare(initData[bitmapIdx], i) ? 1 << (bitmapIdx % 6) : 0;
            }

            // Print dynamicData in 8x8 table form
            // for (var i = 0; i < 14 * 64; i++)
            // {
            //     Console.Write(dynamicData[i]);
            //     Console.Write(' ');
            //     if (i % 8 == 7)
            //         Console.WriteLine();
            //     if (i % 64 == 63)
            //         Console.WriteLine();
            // }
        }

        // TODO add flag, don't always run
        if (dynamicData[1] == 0) populateData();

        // int adjustPliesToMate(int score, int plies, bool decrease)
        // {
        //     return score + (score > 900_000 ? -plies : score < -900_000 ? plies : 0) * (decrease ? 1 : -1);
        // }

        var nodes = 0;
        bool stop = false, // Early exit flag if over time limit
            canNMP = true; // Can try null move pruning
        Move bestMove = default, candidateBestMove = default;
        int phase, mg, eg, limit = timer.MillisecondsRemaining / 20; // TODO inline if not reused

        // ComputeTrainingWeights(); // TODO #DEBUG

        // Negamax Alpha-Beta Pruning
        int search(int ply, int remainingDepth, int alpha, int beta)
        {
            // Check timer and exit early if past time limit
            if ((++nodes & 2047) == 0 && bestMove != default && timer.MillisecondsElapsedThisTurn > limit)
                stop = true;
            if (stop) return 0; // TODO: If tokens needed, change stop flag and nodes to a hard return
            // if (bestMove != default && timer.MillisecondsElapsedThisTurn > limit) return 0;

            var inCheck = board.IsInCheck();
            if (inCheck) remainingDepth++; // Don't want to enter quiescence search if in check

            // Quiescence search is inlined to the regular search to save tokens since they're mostly identical.
            // This flag controls whether we are in the main or quiescence search stage.
            var qs = remainingDepth <= 0;

            int evaluate()
            {
                /*
                 * var pf = pawn.file
                 * var pr = pawn.rank
                 * pbb[pf] = max(pbb[pf], pr)
                 *
                 * 8-RANK
                 *
                 * var kf = board.KingSquare.File
                 * for i=kf..kf+3
                 *     mg += data[pbb[i]]
                 *     eg += data[pbb[i]+384]
                 */

                // Mobility Scoring
                // 1. Calculate enemy pawn attack span
                // 2. Calculate piece attack span 
                // 3. Subtract (enemy pawn attack span | our pieces) from piece attack span
                // 4. Count bits in the resulting attack mask
                // 5. Increment score by DATA.mobility[piece + count] * DATA.mobilityScore[piece]

                // TODO tmp
                // board = Board.CreateBoardFromFEN(
                //     //
                //     "3k4/pppppppp/8/8/8/8/P1PPPPPP/1K6 w - - 0 1"
                //     //
                // );

                phase = mg = eg = 0;
                foreach (var color in new[] { 56, 0 })
                {
                    // var pawnFiles = new int[64]; // 56+8
                    // var usMask = color == 56 ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
                    for (var pieceType = 1; pieceType < 7; pieceType++)
                    {
                        var pieceBB = board.GetPieceBitboard((PieceType)pieceType, color == 56);
                        while (pieceBB != 0)
                        {
                            phase += dynamicData[14 + pieceType]; // Phase score
                            // var squareIdx = BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBB);
                            // int file = squareIdx & 7, rank = squareIdx >> 3, pstIdx = 64 * pieceType + squareIdx ^ color;
                            var pstIdx = 64 * pieceType + BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBB) ^ color;
                            if (pieceType == 3 && pieceBB != 0)
                            {
                                // mg += 21;
                                // eg += 41;
                                mg += 23;
                                eg += 40;
                            }

                            ///// +1 to all these
                            // +13 phase values
                            // +20 max mobility half values
                            // +27 mg mobility scores
                            // +34 eg mobility scores
                            // +41 eg compressed values
                            // var pieceAttackBB = BitboardHelper.GetPieceAttacks((PieceType)pieceType, new Square(squareIdx), board, color == 56) & ~usMask;
                            // var attackCount = BitBoardUtility.PopCount(pieceAttackBB) - dynamicData[20 + pieceType];

                            // TODO wip
                            // if (pieceType == 1) {
                            //    pbb[color,file] = max(pbb[color,file], rank ^ ISWHITE_0_OR_7)
                            //    ALT: pbb[color+fiel] = max(pbb[color+file], rank ^ ISWHITE_0_OR_7)
                            // }
                            // if (pieceType == 1)
                            // pawnFiles[color + file] = Math.Max(pawnFiles[color + file], rank ^ (56 - color) / 8); // TODO >>3 ? // TODO 65^color ?

                            mg += dynamicData[pstIdx] + staticData[pieceType]; // + dynamicData[27 + pieceType] * attackCount;
                            eg += dynamicData[pstIdx + 384] + staticData[pieceType + 6]; // + dynamicData[34 + pieceType] * attackCount;
                        }
                    }

                    // TODO wip
                    // TODO save 45 tokens to use this
                    // for (_i=0;++_i<4;)
                    // var kingSide = board.GetKingSquare(color == 56).File / 4;
                    // var asdfasdf = 0;
                    // for (var i = 0; i < 4; i++)
                    // mg += dynamicData[832 + i * 8 + pawnFiles[color + kingSide * 4 + i]];


                    // Console.WriteLine($"{color == 56} {kingSide} side score {asdfasdf}");
                    // Console.Write("Index Value ");
                    // for (var i = 0; i < 8; i++)
                    //     Console.Write($"{dynamicData[896 + 0 * 8]} ");
                    // Console.WriteLine();

                    // for (var i = 0; i < 8; i++)
                    //     Console.Write($"{pawnFiles[color + i]} ");
                    // Console.WriteLine();
                    // Console.WriteLine();


                    // kingSide = kingFile/4 gives 0 or 1 for left or right side of board
                    // for i=0..4:
                    //     mg += dynamicData[offset + pbb[kingSide*4+i] ]

                    mg = -mg;
                    eg = -eg;
                }

                // Console.WriteLine("===============");
                // throw new Exception();

                return (mg * phase + eg * (24 - phase)) / (board.IsWhiteToMove ? 24 : -24);
            }


            if (ply > 0 && board.IsDraw()) return 0; // TODO: Can change IsDraw to IsRepeatedPosition? Stalemate checked below
            if (qs && (alpha = Math.Max(alpha, evaluate())) >= beta) return alpha;

            var positionHash = board.ZobristKey;
            var (ttHash, ttMove, ttDepth, ttScore, ttNodeType) = tt[positionHash & 16777215];
            if (!qs // No point in TT probe in quiescence search, too expensive (TODO check); TODO remove ^^^ and get tt_bestmove here
                && positionHash == ttHash // TT hit
                && beta - alpha == 1 // Isn't null window
                && ply > 1
                && ttDepth >= remainingDepth // Not from a shallower search
                && (ttScore >= beta ? ttNodeType > 0 : ttNodeType < 2)) // Valid node
                return ttScore;
            // var mateAdjusted = adjustPliesToMate(ttScore, ply, true);
            // var mateAdjusted = ttScore;
            // if (ttNodeType == 1) return mateAdjusted;
            // if (ttNodeType == 0 && mateAdjusted <= alpha) return alpha;
            // if (ttNodeType == 2 && mateAdjusted >= beta) return beta;

            // TODO add checkmate check for beta?
            if (!qs &&
                !inCheck &&
                (ttScore = evaluate()) - 80 * remainingDepth >= beta
                // && beta - alpha == 1 // TODO extract
                // remainingDepth < 7
               )
                return ttScore - 80 * remainingDepth;

            var reduction = 2 + remainingDepth / 6;
            if (!qs &&
                canNMP &&
                !inCheck &&
                // beta - alpha == 1 &&
                // phase != 0 && // Zugzwang check TODO evaluate() may not always run; ok?
                remainingDepth > reduction
               )
            {
                // board.Move
                board.ForceSkipTurn();
                canNMP = false;
                ttScore = -search(ply + 1, remainingDepth - 1 - reduction, -beta, 1 - beta); // TODO remove -1 and combine with reduction; >=
                canNMP = true;
                board.UndoSkipTurn();
                if (ttScore >= beta && ttScore < 900_000 && ttScore > -900_000) return beta; // TODO math.abs
            }

            // if (remainingDepth == 0 || ply > 50) return quiescenceSearch(ply, alpha, beta);

            // Check 3-move repetition and 50-move rule
            // if (board.IsDraw()) return 0;

            // TODO make non alloc?
            // board.GetLegalMovesNonAlloc();
            var moves = board.GetLegalMoves(qs);
            var moveRanks = new int[moves.Length];
            int moveIdx = 0, cmOffset = board.IsWhiteToMove ? 0 : 4096; //, rankBonus = 0;
            foreach (var move in moves)
                // move == killerMoves[ply, 1] ? 500 :
                // moveRanks[moveIdx++] = -(move == ttMove ? 50_000 :
                //     move.IsCapture ? 1_024 * (int)move.CapturePieceType - (int)move.MovePieceType :
                //     move == killerMoves[ply] ? 501 : 0);
                moveRanks[moveIdx++] = -(move == ttMove ? 50_000 :
                    move.IsCapture ? 1_024 * (int)move.CapturePieceType - (int)move.MovePieceType :
                    ((move == killerMoves[ply] ? 501 : 0) + (move == counterMoves[cmOffset + (move.RawValue & 4095)] ? 10 : 0)));
            // moveRanks[moveIdx++] = -(move == ttMove ? 100_000_000 :
            //     move.IsCapture ? 1048576 * (int)move.CapturePieceType - (int)move.MovePieceType :
            //     ((move == killerMoves[ply] ? 500_001 : 0) + (move == counterMoves[cmOffset + (move.RawValue & 4095)] ? 10 : 0) +
            //      history[cmOffset + (move.RawValue & 4095)]));

            // ((move == killerMoves[ply] ? 501 : 0) +
            // (move == counterMoves[cmOffset + (move.RawValue & 4095)] ? 10 : 0)));
            // (move == counterMoves[board.IsWhiteToMove ? 0 : 1, move.StartSquare.Index, move.TargetSquare.Index] ? 10 : 0)));
            Array.Sort(moveRanks, moves);

            // TODO: Do this check before sorting in generateRankedLegalMoves()
            // Is the game over? (If in quiet search, we don't want to return a mate score)
            if (moves.Length == 0) return qs ? alpha : inCheck ? ply - 1_000_000 : 0;

            var bestScore = -99999999;
            ttMove = default;
            ttNodeType = 0; // Upper Bound
            // for (var i = 0; i < moves.Length; i++)
            foreach (var move in moves)
            {
                // TODO: Incremental sorting with selection sort. Re-enable once more ordering heuristics are added. Remove -(...) from moveRanks
                // for (var j = i + 1; j < moves.Length; j++)
                //     if (moveRanks[j] > moveRanks[i])
                //         (moveRanks[i], moveRanks[j], moves[i], moves[j]) = (moveRanks[j], moveRanks[i], moves[j], moves[i]);
                //
                // var move = moves[i];

                board.MakeMove(move);
                // TODO LMR table
                // var lmrReduction = Math.Max(2, remainingDepth / 4) + (moveIdx + 1) / 12;
                // if (inCheck || move.IsCapture) lmrReduction = 0;

                // pvStack.Push(new LinkedList<Move>()); // TODO #DEBUG TUNING

                // Principal Variation Search
                // Reuse ttScore to save tokens 
                if (moveIdx++ == 0 // Use full window for first move (TT move)
                    || qs // Don't do PVS in quiescence search
                    || move.IsCapture // Use full window for captures
                    || remainingDepth < 2 // No point in PVS for shallow searches
                    || (ttScore = -search(ply + 1, remainingDepth - 1, -alpha - 1, -alpha)) > alpha)
                    ttScore = -search(ply + 1, remainingDepth - 1, -beta, -alpha);
                board.UndoMove(move);

                // if (ttScore <= bestScore) pvStack.Pop(); // TODO #DEBUG remove childpv if not better TUNING

                if (ttScore > bestScore)
                {
                    // var childPV = pvStack.Pop(); // TODO #DEUBG TUNING
                    // pvStack.Pop(); // TODO #DEBUG clear curr pv
                    // childPV.AddFirst(move); // TODO #DEBUG
                    // pvStack.Push(childPV); // TODO #DEBUG

                    // pv.Clear();
                    // pv.AddFirst(move);
                    // foreach (var pvMove in childPV)
                    //     pv.AddLast(pvMove);
                    // Console.WriteLine($" NEW BEST move: {move}");
                    // foreach (var m in pv)
                    //     Console.WriteLine($"\t\t{m}");

                    bestScore = ttScore;
                    ttMove = move;

                    if (ttScore > alpha)
                    {
                        alpha = ttScore;
                        ttNodeType = 1; // Exact

                        if (ply == 0) candidateBestMove = move;
                        if (ttScore >= beta)
                        {
                            // Record heuristics for quiet moves
                            if (!move.IsCapture)
                                // if (killerMoves[ply, 0] != move)
                                killerMoves[ply] = move;
                            // (killerMoves[ply, 1], killerMoves[ply, 0]) = (killerMoves[ply, 0], move);
                            // counterMoves[board.IsWhiteToMove ? 0 : 1, move.StartSquare.Index, move.TargetSquare.Index] = move;
                            // history[cmOffset + (move.RawValue & 4095)] += remainingDepth << 1;
                            counterMoves[cmOffset + (move.RawValue & 4095)] = move;
                            ttNodeType++; // (2) Lower Bound
                            break;
                        }
                    }
                }
            }

            if (!stop)
                // tt[positionHash & 16777215] =
                //     (positionHash, ttMove, remainingDepth, adjustPliesToMate(bestScore, ply, false),
                //         ttNodeType);
                tt[positionHash & 16777215] = (positionHash, ttMove, remainingDepth, bestScore, ttNodeType);

            return bestScore;
        }

        for (var depth = 1; depth < 15; depth++)
        {
            var SCORE = // TODO #DEBUG
                search(0, depth, -99999999, 99999999);
            if (stop) break;
            Console.WriteLine($"info depth {depth} score cp {SCORE} nodes {nodes}"); // TODO #DEBUG 

            bestMove = candidateBestMove;
        }

        // TODO Fix check for no search complete
        // if (bestMove == default) return board.GetLegalMoves()[0];

        return bestMove;
    }
}