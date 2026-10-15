import { describe, expect, it } from "vitest";
import { newGame, type GameState } from "./core/game";
import { effectFor } from "./effects";

const prev = newGame(() => 0.5);
const after = (patch: Partial<GameState>): GameState => ({ ...prev, ...patch });

describe("effectFor", () => {
  it("is silent when nothing changed", () => {
    expect(effectFor(prev, prev, "hardDrop")).toBeNull();
  });

  it("is silent for movement, rotation and soft drop without a line clear", () => {
    expect(effectFor(prev, after({}), "left")).toBeNull();
    expect(effectFor(prev, after({}), "rotate")).toBeNull();
    expect(effectFor(prev, after({}), "softDrop")).toBeNull();
    expect(effectFor(prev, after({}), "tick")).toBeNull();
  });

  it("plays the hard drop sound for a hard drop", () => {
    expect(effectFor(prev, after({}), "hardDrop")).toBe("hardDrop");
  });

  it.each([100, 300, 500])("plays the line clear sound for +%i", (gained) => {
    expect(effectFor(prev, after({ score: gained }), "hardDrop")).toBe("lineClear");
    expect(effectFor(prev, after({ score: gained }), "tick")).toBe("lineClear");
  });

  it("plays the four-line sound for +800", () => {
    expect(effectFor(prev, after({ score: 800 }), "hardDrop")).toBe("tetris");
  });

  it("plays game over once when the game ends", () => {
    const over = after({ status: "gameOver" });
    expect(effectFor(prev, over, "hardDrop")).toBe("gameOver");
    expect(effectFor(over, after({ status: "gameOver", score: 5 }), "tick")).not.toBe("gameOver");
  });

  it("is silent for pause and resume", () => {
    expect(effectFor(prev, after({ status: "paused" }), "togglePause")).toBeNull();
  });
});
