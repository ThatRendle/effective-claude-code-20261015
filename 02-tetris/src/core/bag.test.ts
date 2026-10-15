import { describe, expect, it } from "vitest";
import { draw, newBag } from "./bag";
import { KINDS, type Kind } from "./pieces";

function seeded(seed: number) {
  let s = seed;
  return () => {
    s = (s * 1664525 + 1013904223) % 4294967296;
    return s / 4294967296;
  };
}

describe("bag", () => {
  it("contains each kind exactly once", () => {
    expect([...newBag(Math.random)].sort()).toEqual([...KINDS].sort());
  });

  it("is deterministic for a given rng", () => {
    expect(newBag(seeded(7))).toEqual(newBag(seeded(7)));
  });

  it("deals each kind once in every group of 7 from a bag boundary", () => {
    const rng = seeded(42);
    let queue: Kind[] = [];
    for (let group = 0; group < 20; group++) {
      const dealt: Kind[] = [];
      for (let i = 0; i < 7; i++) {
        const d = draw(queue, rng);
        dealt.push(d.kind);
        queue = d.queue;
      }
      expect([...dealt].sort()).toEqual([...KINDS].sort());
    }
  });
});
