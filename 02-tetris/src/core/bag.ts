import { KINDS, type Kind } from "./pieces";

export type Rng = () => number;

/** Returns a freshly shuffled bag holding each kind once (Fisher-Yates). */
export function newBag(rng: Rng): Kind[] {
  const bag = [...KINDS];
  for (let i = bag.length - 1; i > 0; i--) {
    const j = Math.floor(rng() * (i + 1));
    [bag[i], bag[j]] = [bag[j]!, bag[i]!];
  }
  return bag;
}

/** Takes the next kind from the queue, refilling with a new bag when it runs out. */
export function draw(queue: readonly Kind[], rng: Rng): { kind: Kind; queue: Kind[] } {
  const q = queue.length > 0 ? [...queue] : newBag(rng);
  const kind = q.shift()!;
  return { kind, queue: q };
}
