import "./style.css";
import { createAudio } from "./audio";
import { GRAVITY_MS, newGame, step, type Action } from "./core/game";
import { effectFor } from "./effects";
import { createMusic } from "./music";
import { createRenderer } from "./render";

const KEY_ACTIONS: Record<string, Action> = {
  ArrowLeft: "left",
  ArrowRight: "right",
  ArrowUp: "rotate",
  ArrowDown: "softDrop",
  " ": "hardDrop",
  Enter: "restart",
  Escape: "togglePause",
};

const render = createRenderer();
const audio = createAudio();
const music = createMusic(audio);
const mutedIndicator = document.getElementById("muted");
let state = newGame(Math.random);
render(state);
showMuted();

function showMuted() {
  if (mutedIndicator) mutedIndicator.hidden = !audio.muted;
}

function dispatch(action: Action) {
  const next = step(state, action);
  if (next !== state) {
    const effect = effectFor(state, next, action);
    state = next;
    render(state);
    if (effect) audio.play(effect);
    music.sync(state.status);
  }
}

window.addEventListener("keydown", (event) => {
  audio.unlock();
  if (event.key.toLowerCase() === "m" && !event.metaKey && !event.ctrlKey && !event.altKey) {
    event.preventDefault();
    audio.toggleMute();
    showMuted();
    music.sync(state.status);
    return;
  }
  music.sync(state.status);
  const action = KEY_ACTIONS[event.key];
  if (!action) return;
  event.preventDefault();
  dispatch(action);
});

setInterval(() => dispatch("tick"), GRAVITY_MS);
