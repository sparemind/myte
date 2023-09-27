'''
This script encodes arbitrary score tables into compressed bitmaps that can be expanded at runtime during initialization.

Observation: For any given table, the range between the min and max values is generally less than 128 (2^7). 

We can encode the table as a series of n bitmaps, where each bitmap represents a bit value (2^i for i=0..n) and each bit 
in the bitmap represents a square. Then to recover the original value for square i, we can look at the i'th bit of each 
bitmap and add up that bitmap's 2^n value if it's set.

Observation: Although the table ranges are <128, they don't all span the same 128 integer range.

This is a problem since it means we can't just sum of the bitmap values and add the result to the absolute minimum among
all tables, as any larger values outside of that minimum + 128 wouldn't be representable. To remedy this we'll use only
6 bitmaps instead (giving 2^6 = 64 encoding range) and encode an additional score table that is used to store the
minimum values for each table to use as their "zeroes".
'''

from dataclasses import dataclass

@dataclass
class ScoreTable:
    name: str
    table: [int]
    compressed: [int]
# piece move
# piece capture
# en passant
# castling (qk)
# promotion
# combos (capture + promotion)

phases = ['midgame', 'endgame']
pieces = ['pawn', 'knight', 'bishop', 'rook', 'queen', 'king']

tables = [
    # MIDGAME pawn
    [ 
        0, 0, 0, 0, 0, 0, 0, 0,
        43, 42, 41, 46, 29, 24, 24, 30,
        17, 12, 17, 28, 29, 19, 19, -3,
        -32, -16, -16, -6, -3, -14, -12, -15,
        -43, -31, -24, -10, -15, -16, -12, -33,
        -43, -32, -35, -30, -27, -19, -1, -31,
        -47, -39, -36, -68, -57, -1, 9, -35,
        0, 0, 0, 0, 0, 0, 0, 0,
    ],
    # MIDGAME knight
    [ 
        -8, -4, -1, 0, 3, 0, 0, -8,
        -16, -9, 8, 7, 7, 8, -4, -11,
        -8, 8, 18, 15, 19, 27, 10, 6,
        -3, 11, 9, 34, 34, 58, 6, 0,
        -24, 1, 18, 11, 30, 23, 10, -10,
        -35, -16, 1, 5, 11, 6, 16, -22,
        -6, -22, -16, -5, -8, 4, -4, -5,
        -5, -37, -14, -34, -14, -12, -27, -1,
    ],
    # MIDGAME bishop
    [  
        0, 0, 0, -4, 0, 0, 0, 0,
        -16, 0, -10, -4, 0, 5, 0, -7,
        -5, 8, -1, 5, 6, 15, 11, 20,
        -6, 2, 3, 28, 16, 19, 4, -4,
        -8, 1, 11, 34, 23, 9, -4, -9,
        -1, 4, 9, 1, 0, -2, 5, 4,
        2, -6, 1, -10, 0, 10, 11, -15,
        -13, -12, -19, -19, -16, -26, -6, -3,
    ],
    # MIDGAME rook
    [  
        3, 2, 7, 12, 3, 5, 10, 10,
        -6, -4, 21, 24, 20, 18, 8, 10,
        -15, -4, 6, 11, 13, 17, 16, 5,
        -18, -7, -5, 7, 9, 9, -4, -8,
        -34, -30, -9, -4, 5, 4, 10, -14,
        -31, -21, -6, -9, -2, 7, 6, -6,
        -49, -13, -14, -6, -2, 15, -10, -32,
        -4, -6, -10, 8, 13, 15, -13, 4,
    ],
    # MIDGAME queen
    [
        -10, 3, 4, 9, 16, 4, 3, 2,
        -12, -16, 9, 18, 9, 8, 6, 16,
        -23, -7, 1, 19, 24, 32, 25, 31,
        -12, -8, 3, 10, 21, 23, 16, 16,
        -13, -9, -9, 4, 11, 12, 13, 9,
        -4, -3, -3, -4, 4, 9, 18, 2,
        -16, -16, 3, 3, 2, 2, -22, -14,
        -12, -15, -19, 1, -11, -27, -7, -11,
    ],
    # MIDGAME king
    [  
        -4, -4, 0, -4, 0, 0, 5, 4,
        0, 6, 4, 4, 1, 4, 2, 4,
        4, 8, 9, 5, 4, 3, 8, 1,
        2, 8, 7, 10, 5, 6, 6, -6,
        -1, 4, 0, 3, -1, 1, 0, -8,
        -2, -2, -3, -6, -20, -2, -3, -9,
        -4, -3, 3, -19, -39, -30, 16, 15,
        -18, 50, 6, -22, -8, -56, 34, 20,
    ],

    # ENDGAME pawn
    [  
        0,0,0,0,0,0,0,0,
        90, 104, 93, 86, 68, 85, 74, 91,
        34, 57, 44, 29, 32, 19, 20, 20,
        -4, -7, -23, -34, -40, -29, -6, -23,
        -18, -34, -34, -45, -45, -39, -24, -33,
        -31, -30, -41, -33, -36, -35, -34, -36,
        -20, -20, -27, -28, -19, -34, -25, -35,
        0,0,0,0,0,0,0,0,
    ],
    # ENDGAME knight
    [
        -8, -5, -5,0,0, -5, -5, -12,
        -13, -2,8,5,9, -5, -11, -10,
        -15,2, 26, 20, 15, 22,3, -6,
        -7,5, 22, 31,0, 18,7, -7,
        -7, -13, 21, 19, 15,9, -5, -5,
        -11, -11,0, 10,5, -5, -5, -13,
        -9, -16, -21, -10, -2,3, -1, -9,
        -13, -16, -20, -22, -10, -15, -11, -5,
    ],
    # ENDGAME bishop
    [  
        -2, -6,0, -8,3, -1, -1, -1,
        -6,4,0, -1,1,0,4,4,
        1,3, -2,1,1, 21, 10, -2,
        -4,9,8, 11,8,8, -3,8,
        -3,3, 17,9,4,3, -8, -10,
        -2, -7, 15, 21, 17, -2, -11, -3,
        -10, -5, -1,5, -14, -4, -20, -15,
        -11, -8, -24, -20, -8, -19, -6, -5,
    ],
    # ENDGAME rook
    [  
        6, 15, 11, 12, 11, 12,8, 10,
        15, 19, 15, 18, 18, 16, 13, 14,
        14, 15, 14, 12, 12, 17, 13,2,
        11, 11, 15, 11, 10, 11,2,4,
        3,4,3,4, -3, -1, -10, -24,
        2, -13, -14, -14, -15, -11, -16, -25,
        -25, -12, -15, -14, -15, -10, -24, -27,
        -18, -16, -11, -17, -21, -22, -25, -38,
    ],
    # ENDGAME queen
    [ 
        -21, -4,2, 13, 13, -4, -11, -9,
        -6, -4,9, 15, 13,4,4,2,
        -14,1,6, 19, 22, 16,7, 10,
        -5,2,5, 13, 22, 21, 11, 11,
        -7, -3,0, 17,2,8,2,0,
        -4, -7, -7, -7, -7, -8,0, -3,
        -7, -14, -14, -24, -15, -16, -18, -12,
        -10, -19, -22, -35, -23, -32, -23, -33,
    ],
    # ENDGAME king
    [  
        -34, -28, -8, -21, -12,5, 19, 16,
        -7, 14, 15,7,5, 24, 14, 10,
        4,6, 23,5,6,8, 21, -1,
        2, 16, 22, 17, 13, 24,7, -12,
        -10,0, 21, 11, 19, 18,0, -21,
        -11, -6, 10,6, 21, 13, -4, -16,
        -18, -12,8,7, 11, 13, -8, -21,
        -37, -41, -16, -20, -19, -5, -32, -40,
    ],
]

# Rescales the values in a table to fit within a given range. The range is compress equally from both ends.
def rescale_table(table: [int], range_limit: int) -> [int]:
    min_val = min(table)
    max_val = max(table)
    diff = max_val - min_val
    rescale_range = diff
    
    if rescale_range > range_limit:
        compress_amount = (rescale_range - range_limit) // 2
        rescale_range = range_limit
        
        new_min = min_val + compress_amount
        table = [int((x-min_val)/diff * rescale_range + new_min) for x in table]
    return table


def generate_bitmaps(table: [int], range_limit: int) -> [int]:
    min_val = min(table)
    max_val = max(table)
    diff = max_val - min_val
    
    table = [x-min_val for x in table]
    print(min_val, max_val, diff, table[:8])
    
    bitmaps = [0] * 6
    for squareIdx, value in enumerate(table):
        for i in range(5,-1,-1):
            if value >= 2**i:
                value -= 2**i
                bitmaps[i] |= 1 << squareIdx
                
#     for i, bitmap in enumerate(bitmaps):
#         print(f"  {2**i}\t{bitmap:064b} {bitmap}")
    
    return (bitmaps, min_val)
    
    
def main():
    absolute_min = min([min(table) for table in tables])
#     absolute_max = max([max(table) for table in tables])
#     absolute_diff = absolute_max - absolute_min
#     print(absolute_min, absolute_max, absolute_diff)
#     return

    i = 0
    final_bitmaps = []
    final_min_vals = []
    for table in tables:
        piece = pieces[i%6]
        phase = phases[i//6]
        
        print(phase, piece)
        rescaled_table = rescale_table(table, 64)
        if rescaled_table != table:
            print(f"WARNING: table {i} rescaled from {max(table)-min(table)} to {max(rescaled_table)-min(rescaled_table)}")
            
        (bitmaps, min_val) = generate_bitmaps(rescaled_table, 64)
        final_bitmaps.extend(bitmaps)
        final_min_vals.append(min_val)
        i+=1

    final_min_vals = [x-absolute_min for x in final_min_vals] # Account for abs min being hardcoded
    final_min_vals = [0] + final_min_vals # prepend absolute_min (which is normalized to 0)
    print('final_min_vals', [x+0 for x in final_min_vals], '(includes baseline table min value)') 
    
    (bitmaps, min_val) = generate_bitmaps(final_min_vals, 64)
    print('ABSOLUTE MIN:', absolute_min) 
    final_bitmaps = bitmaps + final_bitmaps
#     final_min_vals = [min_val] + final_min_vals 
    
    print('final_min_vals', [x+0 for x in final_min_vals], '(includes baseline table min value)') 

    for i, bitmap in enumerate(bitmaps):
        print(f"  {2**i}\t{bitmap:064b} {bitmap}")
    
    dynamic_data = [0] * (1+12)*64
#     dynamic_data[0] = -68
    for bitmapIdx in range((1+12)*6):
        if bitmapIdx % 6 == 0 and bitmapIdx // 6 != 0:
            offset = dynamic_data[bitmapIdx//6] # Only want to exec once for each 0..6 bitmap chunk
            for i in range(64):
                dynamic_data[bitmapIdx // 6 * 64 + i] = offset -68
        for i in range(64):
            dynamic_data[bitmapIdx // 6 * 64 + i] += 1 << (bitmapIdx % 6) if final_bitmaps[bitmapIdx] & (1 << i) else 0
        
    j=0
    for idx in range(0, len(dynamic_data), 8):
        print(dynamic_data[idx:idx+8])
        j += 1
        if j % 8 == 0:
            print()
            
    names = ['baselines']
    names += [f'{phase} {piece}' for phase in phases for piece in pieces]
    for i, bitmap in enumerate(final_bitmaps):
        print(f'{bitmap}, ', end='')
        if i % 6 == 5:
            print('//', names[i//6])

if __name__ == "__main__":
    main()