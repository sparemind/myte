import os
import chess
import chess.pgn

def load_positions(directory: str):
    positions = 0
    files = 0
    for filename in os.listdir(directory):
        if filename.endswith(".pgn"):
            with open(directory + filename, "r") as file:
                files += 1
                while True:
                    game = chess.pgn.read_game(file)
                    if game is None:
                        break
                    
                    board = game.board()
                    move_index = 0
                    
                    game_result = game.headers['Result']
                    if game_result == '1-0':
                        game_result = 1
                    elif game_result == '0-1':
                        game_result = 0
                    else:
                        game_result = 0.5
                    
                    for node in game.mainline():
                        move_index += 1
                        board.push(node.move)
                        
                        if move_index < 8: # Ignore opening book
                            continue
                        score = node.comment.split()
                        if len(score)==0: # Ignore positions without scores
                            continue
                        score = score[0]
                        if 'M' in score: # Ignore mating scores
                            continue
                        score = score.split('/')[0].replace('+', '')
                        if 'White' in score or 'Black' in score or 'Draw' in score or 's' in score:
                            continue
                        score = float(score) * 100
                        fen = board.fen()
                        print(f'{game_result}#{score}#{fen}')
                        positions += 1

def main():
    positions = load_positions("pgns/")
    
if __name__ == "__main__":
    main()
