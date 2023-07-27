using ChessChallenge.API;

public class MyBot : IChessBot
{
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
        var dynamicData = new int[1024];

        var nodes = 0;
        var stop = false;

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

            var moves = board.GetLegalMoves(true);

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

            var moves = board.GetLegalMoves();

            // Is the game over?
            if (moves.Length == 0)
                return board.IsInCheck() ? ply - 10000 : 0;

            var bestScore = -999999;
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

                if (childScore >= beta) break;
                if (childScore > alpha) alpha = childScore;
            }

            return bestScore;
        }

        for (var depth = 1; depth < 8; depth++)
        {
            search(0, depth, -999999, 999999);
            if (stop) break;
            bestMove = candidateBestMove;
        }

        return bestMove;
    }
}