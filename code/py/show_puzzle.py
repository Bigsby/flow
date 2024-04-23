import sys, os
from typing import Dict, List, Tuple

LEFT = 1 << 0
UP = 1 << 1
RIGHT = 1 << 2
DOWN = 1 << 3
BRIDGE = 1 << 4

COLOURS = [196,21,28,226,208,51,206,88,90]
DIRECTIONS = [
    (-1, LEFT),
    (-1j, UP),
    (1, RIGHT),
    (1j, DOWN)
]

Solution = Dict[complex,int]
Colour = Tuple[int,bool,complex,complex]
Node = Tuple[Solution,List[Colour]]

class Puzzle():
    def __init__(self) -> None:
        self.name = ""
        self.subtitle = ""
        self.positions: Dict[complex,int] = {}
        self.colours: List[Tuple[complex,complex]] = []
        self.max_x = -1
        self.max_y = -1

    def add_position(self, position: complex, type:int) -> None:
        self.positions[position] = type
        self.max_x = max(self.max_x, int(position.real))
        self.max_y = max(self.max_y, int(position.imag))


    def add_colour(self, end1: complex, end2: complex) -> None:
        self.colours.append((end1, end2))


    def get_colour(self, position: complex) -> int:
        for color, (end1, end2) in enumerate(self.colours):
            if end1 == position or end2 == position:
                return color
        return -1


    def get_neighbour(self, position: complex, direction: complex) -> int:
        neighbour = position + direction
        if neighbour in self.positions:
            return self.positions[neighbour]
        return 0


    def get_colour_dot(self, colour: int) -> str:
        return f"\x1b[38;5;{COLOURS[colour]}m\u25CF\x1b[0m" if colour != -1 else "\u25E6"


    def print(self, solution: Solution = {}) -> None:
        print(self.name, self.subtitle, self.max_x, self.max_y)

        for y in range(0, self.max_y * 2 + 3):
            for x in range(0, self.max_x * 2 + 3):
                c = " "
                position = (x - 1) / 2 + ((y - 1) / 2) * 1j
                if x % 2 and y % 2: # position
                    c = self.get_colour_dot(solution[position]) if position in solution else self.get_colour_dot(self.get_colour(position))
                else: # wall
                    if not x % 2 and not y % 2:
                        c = " "
                    else:
                        if y % 2:
                            for direction, wall in [(.5, LEFT), (-.5, RIGHT)]:
                                if self.get_neighbour(position, direction) & wall == wall:
                                    c = f"\u2503"
                        elif x % 2:
                            for direction, wall in [(.5j, UP), (-.5j, DOWN)]:
                                if self.get_neighbour(position, direction) & wall == wall:
                                    c = f"\u2501"
                if position in self.positions and (self.positions[position] & BRIDGE == BRIDGE):
                    c = "\u256C"
                print(f"{c} ", end="")
            print()
    

    def get_neighbours(self, position: complex) -> List[complex]:
        neighbours: List[complex] = []
        for direction, wall in DIRECTIONS:
            if self.positions[position] & wall == wall:
                continue
            neighbour = position + direction
            if neighbour in self.positions:
                neighbours.append(neighbour)
        return neighbours
    

    def is_complete(self, colours: List[Colour]) -> bool:
        for _, complete, _, _ in colours:
            if not complete:
                return False
        return True


    def solve(self) -> Solution:
        solutions: List[Node] = []
        initial_fills: Solution = {}
        initial_colours: List[Colour] = []
        for colour, (start, end) in enumerate(self.colours):
            initial_colours.append((colour,False,start,end))
            initial_fills[start] = colour
            initial_fills[end] = colour

        solutions.append((initial_fills, initial_colours))
        while len(solutions):
            (current_fills, current_colours) = solutions.pop()
            for colour, complete, head, end in current_colours:
                if complete:
                    continue
                path_found = False
                for neighbour in self.get_neighbours(head):
                    if neighbour in current_fills and neighbour != end:
                        continue
                    
                    path_found = True
                    new_fills = dict(current_fills)
                    new_colours = list(current_colours)
                    new_fills[neighbour] = colour
                    new_colours[colour] = (colour, neighbour == end, neighbour, end)
                    if self.is_complete(new_colours):
                        return new_fills
                    solutions.append((new_fills, new_colours))
                if not path_found:
                    break
        raise Exception("Solution not found!")


def read_positions(input: str) -> Tuple[int,int]:
    if "-" in input:
        x1, x2 = input.split("-")
        return int(x1), int(x2)
    return int(input), int(input)


def read_puzzle_file(file_path: str) -> Puzzle:
    if not os.path.isfile(file_path):
        raise FileNotFoundError(file_path)
    puzzle = Puzzle()
    with open(file_path) as file:
        stage = 0
        for line in file.readlines():
            line = line.strip()
            if stage == 0:
                puzzle.name = line
                stage += 1
                continue
            if stage == 1:
                puzzle.subtitle = line
                stage += 1
                continue
            if stage == 2:
                stage += 1
                continue
            if stage == 3:
                if line:
                    splits = line.split(" ")
                    positions, type = splits[0], splits[1]
                    xs, ys = positions.split(",")
                    x1, x2 = read_positions(xs)
                    y1, y2 = read_positions(ys)
                    for x in range(x1, x2 + 1):
                        for y in range(y1, y2 + 1):
                            puzzle.add_position(x + y * 1j, int(type))
                else:
                    stage += 1
                continue
            if stage == 4 and line:
                end1, end2 = line.split()
                x1, y1 = end1.split(",")
                x2, y2 = end2.split(",")
                puzzle.add_colour(int(x1) + int(y1) * 1j, int(x2) + int(y2) *1j)
        return puzzle


def main():
    if len(sys.argv) != 2:
        raise Exception("Please, add input file path as parameter")
    
    puzzle = read_puzzle_file(sys.argv[1])
    puzzle.print()
    solution = puzzle.solve()
    # print(solution)
    puzzle.print(solution)



if __name__ == "__main__":
    main()