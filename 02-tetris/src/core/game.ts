import { draw, type Rng } from "./bag";
import { collides, emptyBoard, HEIGHT, place, WIDTH, type Board, type Piece } from "./board";
import type { Kind } from "./pieces";

export type Action = "left" | "right" | "rotate" | "softDrop" | "hardDrop" | "tick" | "restart" | "togglePause";

export interface GameState {
  readonly board: Board;
  readonly current: Piece;
  readonly next: Kind;
  readonly queue: readonly Kind[];
  readonly score: number;
  readonly status: "playing" | "paused" | "gameOver";
  readonly rng: Rng;
}

export const GRAVITY_MS = 800;

const LINE_SCORES = [0, 100, 300, 500, 800] as const;

export function spawn(kind: Kind): Piece {
  // Shapes live in 3x3 / 4x4 / 2x2 grids; I's cells are on row 1, so start it one row up.
  return { kind, rotation: 0, x: kind === "O" ? 4 : 3, y: kind === "I" ? -1 : 0 };
}

export function newGame(rng: Rng): GameState {
  const first = draw([], rng);
  const second = draw(first.queue, rng);
  return {
    board: emptyBoard(),
    current: spawn(first.kind),
    next: second.kind,
    queue: second.queue,
    score: 0,
    status: "playing",
    rng,
  };
}

function tryMove(state: GameState, dx: number): GameState {
  const moved = { ...state.current, x: state.current.x + dx };
  return collides(state.board, moved) ? state : { ...state, current: moved };
}

function tryRotate(state: GameState): GameState {
  const rotation = (state.current.rotation + 1) % 4;
  for (const dx of [0, -1, 1]) {
    const candidate = { ...state.current, rotation, x: state.current.x + dx };
    if (!collides(state.board, candidate)) return { ...state, current: candidate };
  }
  return state;
}

/** Moves down one row, or locks the piece if it cannot move. */
function stepDown(state: GameState): GameState {
  const moved = { ...state.current, y: state.current.y + 1 };
  return collides(state.board, moved) ? lock(state) : { ...state, current: moved };
}

function hardDrop(state: GameState): GameState {
  let piece = state.current;
  for (;;) {
    const below = { ...piece, y: piece.y + 1 };
    if (collides(state.board, below)) break;
    piece = below;
  }
  return lock({ ...state, current: piece });
}

function clearLines(board: Board): { board: Board; cleared: number } {
  const kept = board.filter((row) => row.some((cell) => cell === null));
  const cleared = HEIGHT - kept.length;
  const fresh = Array.from({ length: cleared }, () => Array<Kind | null>(WIDTH).fill(null));
  return { board: [...fresh, ...kept], cleared };
}

function lock(state: GameState): GameState {
  const placed = place(state.board, state.current);
  const { board, cleared } = clearLines(placed);
  const incoming = spawn(state.next);
  const drawn = draw(state.queue, state.rng);
  return {
    ...state,
    board,
    current: incoming,
    next: drawn.kind,
    queue: drawn.queue,
    score: state.score + LINE_SCORES[cleared]!,
    status: collides(board, incoming) ? "gameOver" : "playing",
  };
}

export function step(state: GameState, action: Action): GameState {
  if (state.status === "gameOver") {
    return action === "restart" ? newGame(state.rng) : state;
  }
  if (state.status === "paused") {
    return action === "togglePause" ? { ...state, status: "playing" } : state;
  }
  switch (action) {
    case "left":
      return tryMove(state, -1);
    case "right":
      return tryMove(state, 1);
    case "rotate":
      return tryRotate(state);
    case "softDrop":
    case "tick":
      return stepDown(state);
    case "hardDrop":
      return hardDrop(state);
    case "restart":
      return state;
    case "togglePause":
      return { ...state, status: "paused" };
  }
}
