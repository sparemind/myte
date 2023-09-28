using System;
using System.Collections.Generic;
using System.Linq;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    (ulong, Move, int, int, byte)[] tt = new (ulong, Move, int, int, byte)[16777216];

    public Move Think(Board board, Timer timer)
    {
        int[] staticData =
        {
            0, //// PieceType.None
            100, // PieceType.Pawn
            300, // PieceType.Knight
            300, // PieceType.Bishop
            500, // PieceType.Rook
            900, // PieceType.Queen
            0 ///// PieceType.King
        };

        // 0 = midgame black
        // 1 = midgame white
        // 2 = endgame black
        // 3 = endgame white
        var dynamicData = new int[8192]; // 8 * 1024

        void populateData()
        {
            // initData consists of chunks of six 64-bit bitmaps, one for each PieceType/GamePhase combination.
            ulong[] initData =
            {
                0x00000000000018f2, 0x000000000000159c, 0x0000000000000794, 0x00000000000017ae, 0x0000000000001492, 0x0000000000000bec, // baselines
                0xffd2870f398deeff, 0xfff44d31ae15f9ff, 0xff31ebb3b79f9fff, 0x00413135a6070f00, 0x0086beb6a778ff00, 0xff60404858ffffff, // midgame pawn
                0x6827c6d7387c1b00, 0x49eec3c03d6ae712, 0x41dd2a547dd77d83, 0x8026ae73fce23d7c, 0xb5f92cc2e5c3feff, 0x0000503c3a3c0000, // midgame knight
                0x8dc44db6246da000, 0x9e94de63d88afbff, 0xcf63c24cefc92808, 0x1395fb0a76ac73f7, 0xc01ffbdbcf0dfeff, 0x0060043c38f20000, // midgame bishop
                0x0cc422d20ea0ccff, 0x96dc94f13491a9e0, 0x928c8cb587991d3b, 0xa1b412b941e33e33, 0x382463c2397dfccc, 0xff7afc7cfefeffff, // midgame rook
                0x01f3d928cb30ebb5, 0x010f193fe7b8a376, 0x4bfcf9b90d4f1dee, 0x0fbfd649c5ac8bf6, 0xd83c9f0e36f600e7, 0x000060f0f8f8fc18, // midgame queen
                0x9f5caba8e9a45200, 0x43dc187d8fe65134, 0xdf089882fe5faec0, 0x92d0888080000000, 0x5b28988080000000, 0xc6c7677f7fffffff, // midgame king
                0xff1c98e195fb67ff, 0xff1f972130eee6ff, 0x00ac6b86280c2200, 0x0053004184ea8f00, 0xff00000043e8ffff, 0x000000000017ff00, // endgame pawn
                0xe1e5f3ffcb517966, 0x6602172593095c99, 0x32db0431dd319619, 0x51a9932fe76add81, 0x807074e05ac63e7e, 0x0000081c2c3c0000, // endgame knight
                0xa18ede2f4a3b18f0, 0xc01725a208c609f3, 0x296ded9141a4cae1, 0x019958b6003bf614, 0xd22ea373419fffff, 0x00001c0cbe600000, // endgame bishop
                0x5c95b2352f624716, 0x06d6709a80786068, 0x4b75d0e00427a543, 0x44ebafcfc0801a41, 0x3f3e7e403f7fffbe, 0x0000013fffffffff, // endgame rook
                0x25ee61f412b5e326, 0xa0a861a4c1686aa3, 0x54971f59c38be727, 0x55093f21f5d407e3, 0x03f73f0939391bfa, 0x0000c0fefefefc1c, // endgame queen
                0xcc7bf25ff84c3c77, 0x10190f396516ef41, 0x39ab7985f45f7e3b, 0x440a9977efdf38f2, 0x1cb7b5bdbe64e6d8, 0x207c7e7e7fffffe4 // endgame king
            };


            // bitmapIdx / 6 is the PieceType, bitmapIdx % 6 is the bit index
            for (var bitmapIdx = 0; bitmapIdx < 78; bitmapIdx++)
            {
                for (int i = 0, offset = dynamicData[bitmapIdx / 6]; i < 64 && bitmapIdx % 6 == 0 && bitmapIdx / 6 != 0; i++)
                    dynamicData[bitmapIdx / 6 * 64 + i] = offset - 68;
                for (var i = 0; i < 64; i++)
                    dynamicData[bitmapIdx / 6 * 64 + i] +=
                        ChessChallenge.Chess.BitBoardUtility.ContainsSquare(initData[bitmapIdx], i) ? 1 << (bitmapIdx % 6) : 0;
            }

            // Print dynamicData in 8x8 table form
            // for (var i = 0; i < 13 * 64; i++)
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
        populateData();


        // Transposition table
        // (hash, bestMove, depth, score, nodeType)
        // var tt = new (ulong, Move, int, int, byte)[16777216];
        //
        // int adjustPliesToMate(int score, int plies, bool decrease)
        // {
        //     return score + (score > 900000 ? -plies : score < -900000 ? plies : 0) * (decrease ? 1 : -1);
        // }

        var nodes = 0;
        var stop = false;

        List<Move> generateRankedLegalMoves(bool capturesOnly)
        {
            int moveRank(Move move)
            {
                if (move.IsCapture) return ((int)move.CapturePieceType << 4) + 8 - (int)move.MovePieceType;
                return 0;
            }

            return (from move in board.GetLegalMoves(capturesOnly)
                orderby moveRank(move) descending
                select move).ToList();
        }

        int quiescenceSearch(int ply, int alpha, int beta)
        {
            // Check 3-move repetition and 50-move rule
            if (board.IsDraw()) return 0;

            dynamicData[0] = 0;
            dynamicData[1] = 0;
            foreach (var pieceList in board.GetAllPieceLists())
                dynamicData[pieceList.IsWhitePieceList ? 1 : 0] +=
                    staticData[(int)pieceList.TypeOfPieceInList] * pieceList.Count;
            var score = dynamicData[1] - dynamicData[0];
            if (!board.IsWhiteToMove) score = -score;

            if (score >= beta) return beta;
            if (score > alpha) alpha = score;

            var moves = generateRankedLegalMoves(true);

            // Is the game over?
            // if (moves.Length == 0)
            //     if (board.IsInCheck()) return ply - 10000;
            //     else return 0;

            foreach (var move in moves)
            {
                board.MakeMove(move);
                var childScore = -quiescenceSearch(ply + 1, -beta, -alpha);
                board.UndoMove(move);

                if (childScore >= beta) return beta;
                if (childScore > alpha) alpha = childScore;
            }

            return alpha;
        }

        Move bestMove = default;
        Move candidateBestMove = default;
        var limit = timer.MillisecondsRemaining / 20;

        // Negamax Alpha-Beta Pruning
        int search(int ply, int remainingDepth, int alpha, int beta)
        {
            // Check timer and exit early if past time limit
            if ((++nodes & 2047) == 0 && bestMove != default && timer.MillisecondsElapsedThisTurn > limit)
                stop = true;
            if (stop) return 0;

            if (remainingDepth == 0 || ply > 50) return quiescenceSearch(ply, alpha, beta);

            // Check 3-move repetition and 50-move rule
            if (board.IsDraw()) return 0;

            // Check transposition table
            // if (ply > 1)
            // {
            //     var (ttHash, ttBestMove, ttDepth, ttScore, ttEntryType) = tt[board.ZobristKey & 16777215];
            //     if (board.ZobristKey == ttHash)
            //         if (ttDepth >= remainingDepth)
            //         {
            //             var mateAdjusted = adjustPliesToMate(ttScore, ply, true);
            //             // TODO Change to ternary
            //             if (ttEntryType == 1) return mateAdjusted;
            //             if (ttEntryType == 2 && mateAdjusted <= alpha) return alpha;
            //             if (ttEntryType == 0 && mateAdjusted >= beta) return beta;
            //         }
            // }

            var moves = generateRankedLegalMoves(false);

            // Is the game over?
            if (moves.Count == 0)
                return board.IsInCheck() ? ply - 1000000 : 0;

            // var entryType = 2; // Upper Bound
            var bestScore = -99999999;
            foreach (var move in moves)
            {
                board.MakeMove(move);
                var childScore = -search(ply + 1, remainingDepth - 1, -beta, -alpha);
                board.UndoMove(move);

                if (childScore > bestScore)
                {
                    bestScore = childScore;
                    if (ply == 0) candidateBestMove = move;
                }

                if (childScore >= beta)
                    // entryType = 0; // Lower Bound
                    break;

                if (childScore > alpha) alpha = childScore;
                // entryType = 1; // Exact
            }

            // if (!stop)
            //     tt[board.ZobristKey & 16777215] =
            //         (board.ZobristKey, candidateBestMove, remainingDepth, adjustPliesToMate(bestScore, ply, false),
            //             entryType);

            return bestScore;
        }

        for (var depth = 1; depth < 15; depth++)
        {
            search(0, depth, -99999999, 99999999);
            if (stop) break;
            bestMove = candidateBestMove;
        }

        // TODO Fix check for no search complete
        if (bestMove == default) return board.GetLegalMoves()[0];

        return bestMove;
    }
}