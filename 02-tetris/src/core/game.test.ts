import { describe, expect, it } from "vitest";
import { emptyBoard, HEIGHT, WIDTH, type Board, type Piece } from "./board";
import { GRAVITY_MS, newGame, spawn, step, type GameState } from "./game";
import type { Kind } from "./pieces";

function seeded(seed: number) {
  let s = seed;
  return () => {
    s = (s * 1664525 + 1013904223) % 4294967296;
    return s / 4294967296;
  };
}

function state(overrides: Partial<GameState> = {}): GameState {
  return { ...newGame(seeded(1)), ...overrides };
}

function piece(kind: Kind, x: number, y: number, rotation = 0): Piece {
  return { kind, rotation, x, y };
}

/** Board where the bottom `rows` rows are full except for column `gap`. */
function stackWithGap(rows: number, gap: number): Board {
  const board = emptyBoard().map((row) => [...row]);
  for (let y = HEIGHT - rows; y < HEIGHT; y++) {
    for (let x = 0; x < WIDTH; x++) if (x !== gap) board[y]![x] = "O";
  }
  return board;
}

function filledCells(board: Board): number {
  return board.flat().filter((c) => c !== null).length;
}

describe("new game", () => {
  it("starts empty with score 0 and a falling piece at the top", () => {
    const g = newGame(seeded(3));
    expect(g.board).toHaveLength(20);
    expect(g.board.every((r) => r.length === 10)).toBe(true);
    expect(filledCells(g.board)).toBe(0);
    expect(g.score).toBe(0);
    expect(g.status).toBe("playing");
    expect(g.current.y).toBeLessThanOrEqual(0);
  });

  it("uses a constant 800 ms gravity interval", () => {
    expect(GRAVITY_MS).toBe(800);
  });
});

describe("horizontal movement", () => {
  it("moves left and right by one column", () => {
    const g = state({ current: piece("T", 4, 5) });
    expect(step(g, "left").current.x).toBe(3);
    expect(step(g, "right").current.x).toBe(5);
  });

  it("is blocked by the walls", () => {
    // T rotation 0 spans columns 0-2 of its grid
    expect(step(state({ current: piece("T", 0, 5) }), "left").current.x).toBe(0);
    expect(step(state({ current: piece("T", 7, 5) }), "right").current.x).toBe(7);
  });

  it("is blocked by locked cells", () => {
    const board = emptyBoard().map((r) => [...r]);
    board[6]![2] = "O";
    const g = state({ board, current: piece("T", 3, 5) });
    expect(step(g, "left").current.x).toBe(3);
  });
});

describe("gravity", () => {
  it("moves the piece down one row when free", () => {
    const g = state({ current: piece("T", 3, 5) });
    expect(step(g, "tick").current.y).toBe(6);
  });

  it("locks on the floor and spawns the next piece", () => {
    // T rotation 0 occupies rows y and y+1; y=18 rests on the floor
    const g = state({ current: piece("T", 3, 18), next: "L" });
    const after = step(g, "tick");
    expect(after.board[18]![4]).toBe("T");
    expect(after.board[19]![3]).toBe("T");
    expect(after.board[19]![5]).toBe("T");
    expect(after.current.kind).toBe("L");
    expect(after.current.y).toBeLessThanOrEqual(0);
  });

  it("locks on top of locked cells", () => {
    const board = emptyBoard().map((r) => [...r]);
    board[10]![4] = "O";
    const g = state({ board, current: piece("T", 3, 8) });
    // T occupies rows 8-9; moving down would put its bottom row on (4,10)
    const after = step(g, "tick");
    expect(filledCells(after.board)).toBe(1 + 4);
  });
});

describe("rotation", () => {
  it("rotates clockwise in open space", () => {
    const g = state({ current: piece("T", 3, 5, 0) });
    const after = step(g, "rotate");
    expect(after.current.rotation).toBe(1);
    expect(after.current.x).toBe(3);
  });

  it("wraps from rotation 3 back to 0", () => {
    const g = state({ current: piece("T", 3, 5, 3) });
    expect(step(g, "rotate").current.rotation).toBe(0);
  });

  it("kicks one column right off the left wall", () => {
    // rotation 1 at x=-1 is legal (cells in columns 0-1); rotation 2 needs column -1
    const g = state({ current: piece("T", -1, 5, 1) });
    const after = step(g, "rotate");
    expect(after.current.rotation).toBe(2);
    expect(after.current.x).toBe(0);
  });

  it("kicks one column left off the right wall", () => {
    // rotation 3 at x=8 is legal; rotation 0 needs column 10
    const g = state({ current: piece("T", 8, 5, 3) });
    const after = step(g, "rotate");
    expect(after.current.rotation).toBe(0);
    expect(after.current.x).toBe(7);
  });

  it("is ignored when in-place and both shifts collide", () => {
    const board = emptyBoard().map((r) => [...r]);
    for (const x of [3, 4, 5]) board[7]![x] = "O";
    const g = state({ board, current: piece("T", 3, 5, 0) });
    const after = step(g, "rotate");
    expect(after.current).toEqual(g.current);
  });
});

describe("soft drop", () => {
  it("moves down one row without scoring", () => {
    const g = state({ current: piece("T", 3, 5) });
    const after = step(g, "softDrop");
    expect(after.current.y).toBe(6);
    expect(after.score).toBe(0);
  });

  it("locks when the row below is blocked", () => {
    const g = state({ current: piece("T", 3, 18), next: "J" });
    const after = step(g, "softDrop");
    expect(filledCells(after.board)).toBe(4);
    expect(after.current.kind).toBe("J");
    expect(after.score).toBe(0);
  });
});

describe("hard drop", () => {
  it("lands at the lowest free position and locks at once", () => {
    const g = state({ current: piece("O", 4, 2), next: "S" });
    const after = step(g, "hardDrop");
    expect(after.board[18]![4]).toBe("O");
    expect(after.board[18]![5]).toBe("O");
    expect(after.board[19]![4]).toBe("O");
    expect(after.board[19]![5]).toBe("O");
    expect(after.current.kind).toBe("S");
    expect(after.score).toBe(0);
  });

  it("stops on top of the stack", () => {
    const board = emptyBoard().map((r) => [...r]);
    board[19]![4] = "T";
    const after = step(state({ board, current: piece("O", 4, 0) }), "hardDrop");
    expect(after.board[17]![4]).toBe("O");
    expect(after.board[18]![5]).toBe("O");
  });
});

describe("line clears and scoring", () => {
  // Vertical I (rotation 1) has its cells in grid column 2, so x=-2 puts it in column 0.
  const verticalI = (): Piece => piece("I", -2, 0, 1);

  it("scores nothing when no row completes", () => {
    const g = state({ current: piece("O", 4, 0) });
    const after = step(g, "hardDrop");
    expect(after.score).toBe(0);
    expect(filledCells(after.board)).toBe(4);
  });

  for (const [lines, points] of [
    [1, 100],
    [2, 300],
    [3, 500],
    [4, 800],
  ] as const) {
    it(`scores ${points} for ${lines} line(s)`, () => {
      const g = state({ board: stackWithGap(lines, 0), current: verticalI() });
      const after = step(g, "hardDrop");
      expect(after.score).toBe(points);
      // only the I cells above the cleared rows remain, shifted down to the floor
      expect(filledCells(after.board)).toBe(4 - lines);
      for (let i = 0; i < 4 - lines; i++) expect(after.board[HEIGHT - 1 - i]![0]).toBe("I");
    });
  }

  it("shifts rows above a cleared row down", () => {
    const board = stackWithGap(1, 0).map((r) => [...r]);
    board[18]![5] = "T"; // stray cell sitting above the row that will clear
    const g = state({ board, current: verticalI() });
    const after = step(g, "hardDrop");
    expect(after.score).toBe(100);
    expect(after.board[19]![5]).toBe("T");
    expect(after.board[18]![5]).toBeNull();
  });
});

describe("preview and spawning", () => {
  it("promotes the preview piece and draws a new preview", () => {
    const g = newGame(seeded(9));
    const expectedNext = g.queue[0]!;
    const after = step(g, "hardDrop");
    expect(after.current.kind).toBe(g.next);
    expect(after.next).toBe(expectedNext);
  });

  it("spawns pieces from a 7-bag", () => {
    let g = newGame(seeded(11));
    const seen: Kind[] = [g.current.kind, g.next];
    // each hard drop on the same columns would stack up; use a fresh board each time
    for (let i = 0; i < 5; i++) {
      g = { ...step(g, "hardDrop"), board: emptyBoard() };
      seen.push(g.next);
    }
    expect(new Set(seen).size).toBe(7);
  });
});

describe("game over", () => {
  function nearlyFull(): GameState {
    const board = emptyBoard().map((r) => [...r]);
    for (const y of [0, 1]) for (let x = 0; x < WIDTH - 1; x++) board[y]![x] = "O";
    return state({ board, current: piece("O", 4, 5), next: "T" });
  }

  it("ends the game when the next spawn is blocked", () => {
    const after = step(nearlyFull(), "hardDrop");
    expect(after.status).toBe("gameOver");
  });

  it("ignores movement, rotation, drop and gravity after game over", () => {
    const over = step(nearlyFull(), "hardDrop");
    for (const action of ["left", "right", "rotate", "softDrop", "hardDrop", "tick"] as const) {
      expect(step(over, action)).toEqual(over);
    }
  });

  it("keeps the final score", () => {
    const over = { ...step(nearlyFull(), "hardDrop"), score: 1200 };
    expect(step(over, "tick").score).toBe(1200);
  });
});

describe("pause", () => {
  const playing = () => state({ current: piece("T", 3, 5), score: 300 });

  it("pauses during play without changing board, piece or score", () => {
    const g = playing();
    const paused = step(g, "togglePause");
    expect(paused.status).toBe("paused");
    expect(paused.board).toEqual(g.board);
    expect(paused.current).toEqual(g.current);
    expect(paused.score).toBe(g.score);
  });

  it("ignores gravity while paused", () => {
    const paused = step(playing(), "togglePause");
    expect(step(paused, "tick")).toEqual(paused);
  });

  it("ignores movement, rotation and drop input while paused", () => {
    const paused = step(playing(), "togglePause");
    for (const action of ["left", "right", "rotate", "softDrop", "hardDrop"] as const) {
      expect(step(paused, action)).toEqual(paused);
    }
  });

  it("resumes from the same board, piece and score", () => {
    const g = playing();
    const resumed = step(step(g, "togglePause"), "togglePause");
    expect(resumed).toEqual(g);
  });

  it("does nothing after game over", () => {
    const board = emptyBoard().map((r) => [...r]);
    for (const y of [0, 1]) for (let x = 0; x < WIDTH - 1; x++) board[y]![x] = "O";
    const over = step(state({ board, current: piece("O", 4, 5), next: "T" }), "hardDrop");
    expect(over.status).toBe("gameOver");
    expect(step(over, "togglePause")).toBe(over);
  });

  it("stays paused and unchanged on restart", () => {
    const paused = step(playing(), "togglePause");
    expect(step(paused, "restart")).toBe(paused);
  });
});

describe("restart", () => {
  it("starts a new empty game with score 0 after game over", () => {
    const over = { ...step(nearlyFull(), "hardDrop"), score: 900 };
    const fresh = step(over, "restart");
    expect(fresh.status).toBe("playing");
    expect(fresh.score).toBe(0);
    expect(filledCells(fresh.board)).toBe(0);
  });

  it("does nothing during play", () => {
    const g = state({ current: spawn("T") });
    expect(step(g, "restart")).toBe(g);
  });

  function nearlyFull(): GameState {
    const board = emptyBoard().map((r) => [...r]);
    for (const y of [0, 1]) for (let x = 0; x < WIDTH - 1; x++) board[y]![x] = "O";
    return state({ board, current: piece("O", 4, 5), next: "T" });
  }
});
