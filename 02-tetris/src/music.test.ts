import { describe, expect, it } from "vitest";
import { MELODY, noteFrequency } from "./music";

describe("noteFrequency", () => {
  it("maps A4 to 440 Hz and A5 to 880 Hz", () => {
    expect(noteFrequency("A4")).toBeCloseTo(440);
    expect(noteFrequency("A5")).toBeCloseTo(880);
  });

  it("maps E5 to about 659.26 Hz", () => {
    expect(noteFrequency("E5")).toBeCloseTo(659.26, 1);
  });

  it("rejects malformed notes", () => {
    expect(() => noteFrequency("H4")).toThrow();
  });
});

describe("MELODY", () => {
  it("is eight bars of eight eighth notes", () => {
    expect(MELODY.reduce((sum, [, eighths]) => sum + eighths, 0)).toBe(64);
  });

  it("only uses parseable notes", () => {
    for (const [note] of MELODY) expect(noteFrequency(note)).toBeGreaterThan(0);
  });

  it("starts on E5 and ends on A4", () => {
    expect(MELODY[0]![0]).toBe("E5");
    expect(MELODY.at(-1)![0]).toBe("A4");
  });
});
