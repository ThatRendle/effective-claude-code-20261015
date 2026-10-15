import { describe, expect, it } from "vitest";
import { collides, emptyBoard, HEIGHT, place, WIDTH, type Piece } from "./board";

const o = (x: number, y: number): Piece => ({ kind: "O", rotation: 0, x, y });

describe("board", () => {
  it("is 10x20 and empty", () => {
    const b = emptyBoard();
    expect(WIDTH).toBe(10);
    expect(HEIGHT).toBe(20);
    expect(b).toHaveLength(20);
    expect(b.every((row) => row.length === 10 && row.every((c) => c === null))).toBe(true);
  });

  it("does not collide in open space", () => {
    expect(collides(emptyBoard(), o(4, 0))).toBe(false);
  });

  it("collides with the left and right walls", () => {
    expect(collides(emptyBoard(), o(-1, 5))).toBe(true);
    expect(collides(emptyBoard(), o(8, 5))).toBe(false);
    expect(collides(emptyBoard(), o(9, 5))).toBe(true);
  });

  it("collides with the floor", () => {
    expect(collides(emptyBoard(), o(4, 18))).toBe(false);
    expect(collides(emptyBoard(), o(4, 19))).toBe(true);
  });

  it("collides with locked cells", () => {
    const b = place(emptyBoard(), o(4, 18));
    expect(collides(b, o(4, 16))).toBe(false);
    expect(collides(b, o(4, 17))).toBe(true);
    expect(collides(b, o(3, 18))).toBe(true);
  });

  it("place does not mutate the original board", () => {
    const b = emptyBoard();
    const placed = place(b, o(0, 0));
    expect(b[0]![0]).toBeNull();
    expect(placed[0]![0]).toBe("O");
    expect(placed[1]![1]).toBe("O");
  });
});
