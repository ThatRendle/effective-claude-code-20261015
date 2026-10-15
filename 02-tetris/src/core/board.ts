import { shapeCells, type Kind } from "./pieces";

export const WIDTH = 10;
export const HEIGHT = 20;

export type Board = readonly (readonly (Kind | null)[])[];

export interface Piece {
  kind: Kind;
  rotation: number;
  x: number;
  y: number;
}

export function emptyBoard(): Board {
  return Array.from({ length: HEIGHT }, () => Array<Kind | null>(WIDTH).fill(null));
}

export function pieceCells(piece: Piece): [number, number][] {
  return shapeCells(piece.kind, piece.rotation).map(([cx, cy]) => [piece.x + cx, piece.y + cy]);
}

export function collides(board: Board, piece: Piece): boolean {
  return pieceCells(piece).some(([x, y]) => {
    if (x < 0 || x >= WIDTH || y >= HEIGHT) return true;
    if (y < 0) return false; // above the board is free
    return board[y]![x] !== null;
  });
}

export function place(board: Board, piece: Piece): Board {
  const next = board.map((row) => [...row]);
  for (const [x, y] of pieceCells(piece)) {
    if (y >= 0 && y < HEIGHT && x >= 0 && x < WIDTH) next[y]![x] = piece.kind;
  }
  return next;
}
