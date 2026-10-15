import { HEIGHT, pieceCells, WIDTH } from "./core/board";
import type { GameState } from "./core/game";
import { COLORS, shapeCells, type Kind } from "./core/pieces";

const GRID_COLOR = "#1a1d27";

function context(id: string): { canvas: HTMLCanvasElement; ctx: CanvasRenderingContext2D } {
  const canvas = document.getElementById(id);
  if (!(canvas instanceof HTMLCanvasElement)) throw new Error(`#${id} is not a canvas`);
  const ctx = canvas.getContext("2d");
  if (!ctx) throw new Error("2d canvas context unavailable");
  return { canvas, ctx };
}

function text(id: string): HTMLElement {
  const el = document.getElementById(id);
  if (!el) throw new Error(`#${id} not found`);
  return el;
}

function drawCell(ctx: CanvasRenderingContext2D, x: number, y: number, size: number, kind: Kind) {
  ctx.fillStyle = COLORS[kind];
  ctx.fillRect(x * size, y * size, size, size);
  ctx.strokeStyle = "rgb(0 0 0 / 0.35)";
  ctx.lineWidth = 2;
  ctx.strokeRect(x * size + 1, y * size + 1, size - 2, size - 2);
}

export function createRenderer() {
  const board = context("board");
  const preview = context("preview");
  const score = text("score");
  const paused = text("paused");
  const gameOver = text("gameover");
  const finalScore = text("final-score");

  const cell = board.canvas.width / WIDTH;

  function drawBoard(state: GameState) {
    const { ctx, canvas } = board;
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    ctx.strokeStyle = GRID_COLOR;
    ctx.lineWidth = 1;
    for (let x = 1; x < WIDTH; x++) {
      ctx.beginPath();
      ctx.moveTo(x * cell + 0.5, 0);
      ctx.lineTo(x * cell + 0.5, canvas.height);
      ctx.stroke();
    }
    for (let y = 1; y < HEIGHT; y++) {
      ctx.beginPath();
      ctx.moveTo(0, y * cell + 0.5);
      ctx.lineTo(canvas.width, y * cell + 0.5);
      ctx.stroke();
    }

    state.board.forEach((row, y) =>
      row.forEach((kind, x) => {
        if (kind) drawCell(ctx, x, y, cell, kind);
      }),
    );

    if (state.status !== "gameOver") {
      for (const [x, y] of pieceCells(state.current)) {
        if (y >= 0) drawCell(ctx, x, y, cell, state.current.kind);
      }
    }
  }

  function drawPreview(kind: Kind) {
    const { ctx, canvas } = preview;
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    const size = 24;
    const cells = shapeCells(kind, 0);
    const xs = cells.map(([x]) => x);
    const ys = cells.map(([, y]) => y);
    const minX = Math.min(...xs);
    const minY = Math.min(...ys);
    const w = Math.max(...xs) - minX + 1;
    const h = Math.max(...ys) - minY + 1;
    const offsetX = (canvas.width / size - w) / 2 - minX;
    const offsetY = (canvas.height / size - h) / 2 - minY;
    for (const [x, y] of cells) drawCell(ctx, x + offsetX, y + offsetY, size, kind);
  }

  return function render(state: GameState) {
    drawBoard(state);
    drawPreview(state.next);
    score.textContent = String(state.score);
    paused.hidden = state.status !== "paused";
    gameOver.hidden = state.status !== "gameOver";
    finalScore.textContent = String(state.score);
  };
}
