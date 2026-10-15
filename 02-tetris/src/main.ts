import "./style.css";
import { GRAVITY_MS, newGame, step, type Action } from "./core/game";
import { createRenderer } from "./render";

const KEY_ACTIONS: Record<string, Action> = {
  ArrowLeft: "left",
  ArrowRight: "right",
  ArrowUp: "rotate",
  ArrowDown: "softDrop",
  " ": "hardDrop",
  Enter: "restart",
};

const render = createRenderer();
let state = newGame(Math.random);
render(state);

function dispatch(action: Action) {
  const next = step(state, action);
  if (next !== state) {
    state = next;
    render(state);
  }
}

window.addEventListener("keydown", (event) => {
  const action = KEY_ACTIONS[event.key];
  if (!action) return;
  event.preventDefault();
  dispatch(action);
});

setInterval(() => dispatch("tick"), GRAVITY_MS);
