import { describe, expect, it } from "vitest";
import { COLORS, KINDS, SHAPES, shapeCells } from "./pieces";

describe("pieces", () => {
  it("has the seven standard kinds", () => {
    expect([...KINDS].sort()).toEqual(["I", "J", "L", "O", "S", "T", "Z"]);
  });

  it("has 4 cells in every rotation of every kind", () => {
    for (const kind of KINDS) {
      expect(SHAPES[kind]).toHaveLength(4);
      for (let r = 0; r < 4; r++) {
        expect(shapeCells(kind, r)).toHaveLength(4);
      }
    }
  });

  it("rotates clockwise: T pointing up becomes pointing right", () => {
    // rotation 0: ".X." / "XXX"; rotation 1: stem points right
    const cells = shapeCells("T", 1).map(([x, y]) => `${x},${y}`).sort();
    expect(cells).toEqual(["1,0", "1,1", "1,2", "2,1"]);
  });

  it("wraps rotation indexes", () => {
    expect(shapeCells("L", 4)).toEqual(shapeCells("L", 0));
    expect(shapeCells("L", -1)).toEqual(shapeCells("L", 3));
  });

  it("gives each kind a distinct colour", () => {
    expect(new Set(KINDS.map((k) => COLORS[k])).size).toBe(7);
  });
});
