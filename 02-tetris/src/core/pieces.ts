export type Kind = "I" | "O" | "T" | "S" | "Z" | "J" | "L";

export type Cell = readonly [x: number, y: number];

export const KINDS: readonly Kind[] = ["I", "O", "T", "S", "Z", "J", "L"];

// Each shape is drawn in a square grid; rotation is clockwise.
const BASE: Record<Kind, readonly string[]> = {
  I: ["....", "XXXX", "....", "...."],
  O: ["XX", "XX"],
  T: [".X.", "XXX", "..."],
  S: [".XX", "XX.", "..."],
  Z: ["XX.", ".XX", "..."],
  J: ["X..", "XXX", "..."],
  L: ["..X", "XXX", "..."],
};

function cellsOf(grid: readonly string[]): Cell[] {
  const cells: Cell[] = [];
  grid.forEach((row, y) => {
    [...row].forEach((ch, x) => {
      if (ch === "X") cells.push([x, y]);
    });
  });
  return cells;
}

function rotateClockwise(grid: readonly string[]): string[] {
  const n = grid.length;
  const out: string[] = [];
  for (let y = 0; y < n; y++) {
    let row = "";
    for (let x = 0; x < n; x++) row += grid[n - 1 - x]![y];
    out.push(row);
  }
  return out;
}

function buildRotations(kind: Kind): Cell[][] {
  const rotations: Cell[][] = [];
  let grid: string[] = [...BASE[kind]];
  for (let r = 0; r < 4; r++) {
    rotations.push(cellsOf(grid));
    grid = rotateClockwise(grid);
  }
  return rotations;
}

export const SHAPES: Record<Kind, readonly (readonly Cell[])[]> = {
  I: buildRotations("I"),
  O: buildRotations("O"),
  T: buildRotations("T"),
  S: buildRotations("S"),
  Z: buildRotations("Z"),
  J: buildRotations("J"),
  L: buildRotations("L"),
};

export function shapeCells(kind: Kind, rotation: number): readonly Cell[] {
  return SHAPES[kind][((rotation % 4) + 4) % 4]!;
}

export const COLORS: Record<Kind, string> = {
  I: "#00bcd4",
  O: "#fdd835",
  T: "#9c27b0",
  S: "#4caf50",
  Z: "#f44336",
  J: "#2196f3",
  L: "#ff9800",
};
