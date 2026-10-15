import type { Effect } from "./effects";

const MUTE_KEY = "tetris.muted";

function loadMuted(): boolean {
  try {
    return localStorage.getItem(MUTE_KEY) === "1";
  } catch {
    return false;
  }
}

function saveMuted(muted: boolean) {
  try {
    localStorage.setItem(MUTE_KEY, muted ? "1" : "0");
  } catch {
    // Storage unavailable: the setting just won't persist.
  }
}

type Note = readonly [freq: number, start: number, duration: number];

const SEQUENCES: Record<Effect, { wave: OscillatorType; gain: number; notes: readonly Note[] }> = {
  hardDrop: { wave: "triangle", gain: 0.4, notes: [[90, 0, 0.14]] },
  lineClear: {
    wave: "square",
    gain: 0.12,
    notes: [
      [523.25, 0, 0.08],
      [659.25, 0.07, 0.08],
      [783.99, 0.14, 0.08],
      [1046.5, 0.21, 0.2],
    ],
  },
  tetris: {
    wave: "square",
    gain: 0.14,
    notes: [
      [523.25, 0, 0.09],
      [659.25, 0.08, 0.09],
      [783.99, 0.16, 0.09],
      [1046.5, 0.24, 0.09],
      [1318.51, 0.32, 0.09],
      [1567.98, 0.4, 0.09],
      [2093, 0.48, 0.45],
    ],
  },
  gameOver: {
    wave: "sawtooth",
    gain: 0.12,
    notes: [
      [392, 0, 0.2],
      [329.63, 0.22, 0.2],
      [261.63, 0.44, 0.2],
      [196, 0.66, 0.6],
    ],
  },
};

export interface Audio {
  /** Create/resume the AudioContext. Must be called from a user gesture. */
  unlock(): void;
  /** Null until unlock() has been called, or if Web Audio is unavailable. */
  readonly context: AudioContext | null;
  /** Node that music and effects connect to; silent while muted. */
  readonly output: GainNode | null;
  readonly muted: boolean;
  toggleMute(): boolean;
  play(effect: Effect): void;
}

export function createAudio(): Audio {
  let context: AudioContext | null = null;
  let output: GainNode | null = null;
  let muted = loadMuted();

  function applyMute() {
    if (output) output.gain.value = muted ? 0 : 1;
  }

  return {
    unlock() {
      if (!context) {
        if (typeof AudioContext === "undefined") return;
        context = new AudioContext();
        output = context.createGain();
        output.connect(context.destination);
        applyMute();
      }
      if (context.state === "suspended") void context.resume();
    },
    get context() {
      return context;
    },
    get output() {
      return output;
    },
    get muted() {
      return muted;
    },
    toggleMute() {
      muted = !muted;
      saveMuted(muted);
      applyMute();
      return muted;
    },
    play(effect) {
      if (!context || !output || muted) return;
      const { wave, gain, notes } = SEQUENCES[effect];
      const now = context.currentTime;
      for (const [freq, start, duration] of notes) {
        const osc = context.createOscillator();
        const env = context.createGain();
        osc.type = wave;
        osc.frequency.setValueAtTime(freq, now + start);
        if (effect === "hardDrop") osc.frequency.exponentialRampToValueAtTime(40, now + start + duration);
        env.gain.setValueAtTime(0.0001, now + start);
        env.gain.linearRampToValueAtTime(gain, now + start + 0.01);
        env.gain.exponentialRampToValueAtTime(0.0001, now + start + duration);
        osc.connect(env).connect(output);
        osc.start(now + start);
        osc.stop(now + start + duration + 0.02);
      }
    },
  };
}
