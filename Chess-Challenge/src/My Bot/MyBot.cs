using System;
using ChessChallenge.API;

public class MyBot : IChessBot
{
    // Transposition table. Entries are TODO size 
    // (hash, bestMove, depth, score, nodeType)
    readonly (ulong, Move, int, int, byte)[] tt = new (ulong, Move, int, int, byte)[16777216];

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
        var killerMoves = new Move[128];
        // var counterMoves = new Move[2, 64, 64];
        // var counterMoves = new Move[8192]; // 64x64x2 table
        // var history = new int[4096]; // 64x64 butterfly table

        void populateData()
        {
            // initData consists of chunks of six 64-bit bitmaps, one for each PieceType/GamePhase combination.
            ulong[] initData =
            {
                0x00000000000198f2, 0x000000000002159c, 0x0000000000040794, 0x00000000000017ae, 0x0000000000001492, 0x0000000000000bec, // baselines + constants
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

        int adjustPliesToMate(int score, int plies, bool decrease)
        {
            return score + (score > 900_000 ? -plies : score < -900_000 ? plies : 0) * (decrease ? 1 : -1);
        }

        var nodes = 0;
        var stop = false; // Early exit flag if over time limit
        // canNMP = true; // Can try null move pruning
        Move bestMove = default, candidateBestMove = default;
        var limit = timer.MillisecondsRemaining / 20; // TODO inline if not reused

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
                int mg = 0, eg = 0, phase = 0;
                foreach (var color in new[] { true, false })
                {
                    for (var pieceType = 1; pieceType < 7; pieceType++)
                    {
                        var pieceBB = board.GetPieceBitboard((PieceType)pieceType, color);
                        while (pieceBB != 0)
                        {
                            phase += dynamicData[13 + pieceType]; // Phase score
                            var squareIdx = 64 * pieceType + BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBB) ^ (color ? 56 : 0);
                            mg += dynamicData[squareIdx] + staticData[pieceType];
                            eg += dynamicData[squareIdx + 384] + staticData[pieceType];
                        }
                    }

                    mg = -mg;
                    eg = -eg;
                }

                return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
            }


            if (ply > 0 && board.IsDraw()) return 0; // TODO: Can change IsDraw to IsRepeatedPosition? Stalemate checked below
            if (qs && (alpha = Math.Max(alpha, evaluate())) >= beta) return alpha;

            var positionHash = board.ZobristKey;
            var (ttHash, ttMove, ttDepth, ttScore, ttNodeType) = tt[positionHash & 16777215];
            if (!qs // No point in TT probe in quiescence search, too expensive (TODO check); TODO remove ^^^ and get tt_bestmove here
                && beta - alpha == 1 // Isn't null window
                && positionHash == ttHash // TT hit
                && ply > 1
                && ttDepth >= remainingDepth) // Not from a shallower search
            {
                var mateAdjusted = adjustPliesToMate(ttScore, ply, true);
                if (ttNodeType == 1) return mateAdjusted;
                if (ttNodeType == 0 && mateAdjusted <= alpha) return alpha;
                if (ttNodeType == 2 && mateAdjusted >= beta) return beta;
            }

            // TODO add checkmate check for beta?
            if (!qs &&
                !inCheck &&
                (ttScore = evaluate()) - 80 * remainingDepth >= beta
                // beta - alpha == 1 && // TODO extract
                // remainingDepth < 7
               )
                return ttScore - 80 * remainingDepth;

            // var reduction = 2 + remainingDepth / 6;
            // if (!qs &&
            //     canNMP &&
            //     !inCheck &&
            //     beta - alpha == 1 &&
            //     remainingDepth > reduction
            //     // TODO add zugzwang check
            //    )
            // {
            //     // board.Move
            //     board.TrySkipTurn();
            //     canNMP = false;
            //     ttScore = -search(ply + 1, remainingDepth - 1 - reduction, -beta, 1 - beta);
            //     canNMP = true;
            //     board.UndoSkipTurn();
            //     if (ttScore >= beta && ttScore < 900_000 && ttScore > -900_000) return beta;
            // }

            // if (remainingDepth == 0 || ply > 50) return quiescenceSearch(ply, alpha, beta);

            // Check 3-move repetition and 50-move rule
            // if (board.IsDraw()) return 0;

            // TODO make non alloc?
            var moves = board.GetLegalMoves(qs);
            var moveRanks = new int[moves.Length];
            var moveIdx = 0; //, cmOffset = board.IsWhiteToMove ? 0 : 4096; //, rankBonus = 0;
            foreach (var move in moves)
                // move == killerMoves[ply, 1] ? 500 :
                moveRanks[moveIdx++] = -(move == ttMove ? 50_000 :
                    move.IsCapture ? 1_024 * (int)move.CapturePieceType - (int)move.MovePieceType :
                    move == killerMoves[ply] ? 501 : 0);
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
                // Reuse ttScore to save tokens 
                ttScore = -search(ply + 1, remainingDepth - 1, -beta, -alpha);
                board.UndoMove(move);

                if (ttScore > bestScore)
                {
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
                            // counterMoves[cmOffset + (move.RawValue & 4095)] = move;
                            ttNodeType++; // (2) Lower Bound
                            break;
                        }
                    }
                }
            }

            if (!stop)
                tt[positionHash & 16777215] =
                    (positionHash, ttMove, remainingDepth, adjustPliesToMate(bestScore, ply, false),
                        ttNodeType);

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