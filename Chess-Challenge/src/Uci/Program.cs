using System;
using ChessChallenge.API;
using ChessChallenge.Example;

namespace ChessChallenge.Uci;

// A barebones UCI interface for playing matches. Doesn't do much error handling and generally assumes that the UCI
// client is well-behaved and doesn't send anything out of spec. 
//
// Supported Commands:
// - uci                                                        Prints the engine info
// - isready                                                    Signals that the engine is ready
// - ucinewgame                                                 Sets up a new game
// - position (startpos | fen FEN) [moves MOVE1 MOVE2 ...]      Sets the engine to the given position
// - go [wtime TIME | btime TIME]                               Searches for the best move
// - quit                                                       Exits the program  
// - baseline                                                   (custom) Runs the baseline bot version
static class Program
{
    const string StartingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public static void Main()
    {
        var board = Board.CreateBoardFromFEN(StartingFen);
        var useBaselineBot = false;

        bool IsBaselineBot()
        {
            return useBaselineBot || Environment.GetEnvironmentVariable("BASELINE") != null;
        }

        IChessBot InitBot()
        {
            if (IsBaselineBot()) return new EvilBot();
            return new MyBot();
        }

        var bot = InitBot();
        while (true)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input)) continue;

            var tokens = input.Split(' ');
            var command = tokens[0];
            switch (command)
            {
                case "baseline":
                    useBaselineBot = true;
                    break;
                case "uci":
                    var tag = IsBaselineBot() ? "-baseline" : "";
                    Console.WriteLine($"id name Myte 0.5.0{tag}");
                    Console.WriteLine("sparemind");
                    Console.WriteLine("uciok");
                    break;
                case "isready":
                    Console.WriteLine("readyok");
                    break;
                case "ucinewgame":
                    bot = InitBot();
                    Console.WriteLine("readyok");
                    break;
                case "position":
                {
                    // Set up the initial position
                    if (tokens[1] == "startpos")
                    {
                        board = Board.CreateBoardFromFEN(StartingFen);
                        tokens = tokens[2..];
                    }
                    else if (tokens[1] == "fen")
                    {
                        var fen = string.Join(' ', tokens[2..8]);
                        board = Board.CreateBoardFromFEN(fen);
                        tokens = tokens[8..];
                    }

                    // Parse the sequence of moves, if given
                    if (tokens.Length > 0 && tokens[0] == "moves")
                        foreach (var inputMoveString in tokens[1..])
                        foreach (var move in board.GetLegalMoves())
                            if (inputMoveString == MoveToString(move))
                            {
                                board.MakeMove(move);
                                break;
                            }

                    break;
                }
                case "go":
                {
                    // Parse search args
                    var timer = new Timer(0);
                    for (var i = 1; i < tokens.Length; i++)
                        switch (tokens[i])
                        {
                            case "wtime":
                                i += 1;
                                if (board.IsWhiteToMove) timer = new Timer(int.Parse(tokens[i]));
                                break;
                            case "btime":
                                i += 1;
                                if (!board.IsWhiteToMove) timer = new Timer(int.Parse(tokens[i]));
                                break;
                        }

                    // Search and output the best move
                    var move = bot.Think(board, timer);
                    Console.WriteLine($"bestmove {MoveToString(move)}");
                    break;
                }
                case "quit":
                    return;
            }
        }
    }

    // Returns the UCI string representation of a move (e.g. 'e2e4', 'a7a8q').
    static string MoveToString(Move move)
    {
        var moveString = $"{move.StartSquare.Name}{move.TargetSquare.Name}";
        if (move.IsPromotion)
        {
            if (move.PromotionPieceType == PieceType.Queen) moveString += "q";
            else if (move.PromotionPieceType == PieceType.Rook) moveString += "r";
            else if (move.PromotionPieceType == PieceType.Knight) moveString += "n";
            else if (move.PromotionPieceType == PieceType.Bishop) moveString += "b";
        }

        return moveString;
    }
}