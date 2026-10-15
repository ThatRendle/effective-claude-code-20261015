import type { Action, GameState } from "./core/game";

export type Effect = "hardDrop" | "lineClear" | "tetris" | "gameOver";

/**
 * Which sound effect (if any) the transition prev -> next should trigger.
 * Line clears are inferred from the score delta: 800 is a four-line clear,
 * 100/300/500 are 1-3 rows, and nothing else awards points.
 */
export function effectFor(prev: GameState, next: GameState, action: Action): Effect | null {
  if (next === prev) return null;
  if (prev.status !== "gameOver" && next.status === "gameOver") return "gameOver";
  const gained = next.score - prev.score;
  if (gained === 800) return "tetris";
  if (gained > 0) return "lineClear";
  if (action === "hardDrop") return "hardDrop";
  return null;
}
