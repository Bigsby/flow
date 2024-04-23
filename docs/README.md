# Colours

0 - Red (196)
1 - Blue (21)
2 - Green (28)
3 - Yellow (226)
4 - Orange (214)
5 - Cyan (51)
6 - Pink (206)
7 - Brown (88)
8 - Purple (90)

196,21,28,226,214,51,206,88,90

# Puzzles

- Line 1: name
- Line 2: subtitle 
- Line 3: empty
- Line 4 until empty line: possible paths
    - format: x[-x1],y[-y1] t
        - x: position
        - y: position
        - optional x1, y1: continuous positions with same walls
        - t: position walls - sum of existing walls and if it is bridge:
            - 1: left
            - 2: up
            - 4: right
            - 8: down
            - 16: bridge
- Line n: empty
- Line n+1 until end: colours ends
    - format: x1,y1[|x3,y3] x2,y2[|x4,y4]
        - x1,y1 and x2,y2: color ends
        - optional x3,y3 and x4,y4: other color connection


