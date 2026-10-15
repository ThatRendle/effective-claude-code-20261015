import type { Audio } from "./audio";
import type { GameState } from "./core/game";

/** Seconds per eighth note (about 150 bpm). */
export const EIGHTH_SECONDS = 0.2;

/** Korobeiniki (Tetris Theme A): [note, length in eighth notes]. Eight bars of eight eighths. */
export const MELODY: readonly (readonly [note: string, eighths: number])[] = [
  ["E5", 2], ["B4", 1], ["C5", 1], ["D5", 2], ["C5", 1], ["B4", 1],
  ["A4", 2], ["A4", 1], ["C5", 1], ["E5", 2], ["D5", 1], ["C5", 1],
  ["B4", 3], ["C5", 1], ["D5", 2], ["E5", 2],
  ["C5", 2], ["A4", 2], ["A4", 4],
  ["D5", 3], ["F5", 1], ["A5", 2], ["G5", 1], ["F5", 1],
  ["E5", 3], ["C5", 1], ["E5", 2], ["D5", 1], ["C5", 1],
  ["B4", 2], ["B4", 1], ["C5", 1], ["D5", 2], ["E5", 2],
  ["C5", 2], ["A4", 2], ["A4", 4],
];

const SEMITONES: Record<string, number> = { C: -9, D: -7, E: -5, F: -4, G: -2, A: 0, B: 2 };

/** Frequency in Hz of a note like "E5" (equal temperament, A4 = 440). */
export function noteFrequency(note: string): number {
  const match = /^([A-G])(\d)$/.exec(note);
  if (!match) throw new Error(`Bad note: ${note}`);
  const semitones = SEMITONES[match[1]!]! + (Number(match[2]) - 4) * 12;
  return 440 * 2 ** (semitones / 12);
}

const LOOKAHEAD_SECONDS = 0.3;
const TICK_MS = 100;
const VOLUME = 0.07;

export function createMusic(audio: Audio) {
  let timer: ReturnType<typeof setInterval> | null = null;
  let bus: GainNode | null = null;

  function start(context: AudioContext, output: GainNode) {
    const node = context.createGain();
    node.gain.value = VOLUME;
    node.connect(output);
    bus = node;

    let index = 0;
    let nextTime = context.currentTime + 0.05;

    function schedule() {
      while (nextTime < context.currentTime + LOOKAHEAD_SECONDS) {
        const [note, eighths] = MELODY[index]!;
        const duration = eighths * EIGHTH_SECONDS;
        const osc = context.createOscillator();
        const env = context.createGain();
        osc.type = "square";
        osc.frequency.value = noteFrequency(note);
        env.gain.setValueAtTime(0.0001, nextTime);
        env.gain.linearRampToValueAtTime(1, nextTime + 0.01);
        env.gain.setValueAtTime(1, nextTime + duration * 0.7);
        env.gain.linearRampToValueAtTime(0.0001, nextTime + duration * 0.95);
        osc.connect(env).connect(node);
        osc.start(nextTime);
        osc.stop(nextTime + duration);
        nextTime += duration;
        index = (index + 1) % MELODY.length;
      }
    }

    schedule();
    timer = setInterval(schedule, TICK_MS);
  }

  function stop() {
    if (timer !== null) clearInterval(timer);
    timer = null;
    // Disconnecting the bus silences notes that are already scheduled.
    bus?.disconnect();
    bus = null;
  }

  return {
    /** Play the theme iff the game is playing and not muted; otherwise stop it. */
    sync(status: GameState["status"]) {
      const { context, output } = audio;
      if (status === "playing" && !audio.muted && context && output) {
        if (timer === null) start(context, output);
      } else if (timer !== null) {
        stop();
      }
    },
  };
}
