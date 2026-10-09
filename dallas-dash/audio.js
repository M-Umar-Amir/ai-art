/**
 * Dallas Dash — audio, synthesised in the browser.
 *
 * No audio files, no paid generation: every sound here is an oscillator or a
 * noise burst built at runtime, so the build stays free and the whole kit is a
 * few hundred bytes of code. Porting note: all WebAudio calls live in this
 * module, so an Expo port swaps this file for expo-av and keeps the call sites.
 */

let actx = null;
let master = null;
let musicOn = false;
let musicTimer = null;
let step = 0;
let enabled = false;
let stepMs = 150;

function ac() {
  if (!actx) {
    const Ctx = window.AudioContext || window.webkitAudioContext;
    if (!Ctx) return null;
    actx = new Ctx();
    master = actx.createGain();
    master.gain.value = 0.75;
    master.connect(actx.destination);
  }
  if (actx.state === "suspended") actx.resume();
  return actx;
}

function tone(freq, dur, type, gain, slideTo) {
  if (!enabled) return;
  const c = ac();
  if (!c) return;
  const osc = c.createOscillator();
  const g = c.createGain();
  osc.type = type || "square";
  osc.frequency.setValueAtTime(freq, c.currentTime);
  if (slideTo) osc.frequency.exponentialRampToValueAtTime(slideTo, c.currentTime + dur);
  g.gain.setValueAtTime(0.0001, c.currentTime);
  g.gain.exponentialRampToValueAtTime(gain || 0.16, c.currentTime + 0.01);
  g.gain.exponentialRampToValueAtTime(0.0001, c.currentTime + dur);
  osc.connect(g);
  g.connect(master);
  osc.start();
  osc.stop(c.currentTime + dur + 0.02);
}

function noise(dur, gain, freq) {
  if (!enabled) return;
  const c = ac();
  if (!c) return;
  const frames = Math.floor(c.sampleRate * dur);
  const buf = c.createBuffer(1, frames, c.sampleRate);
  const data = buf.getChannelData(0);
  for (let i = 0; i < frames; i += 1) data[i] = (Math.random() * 2 - 1) * (1 - i / frames);
  const src = c.createBufferSource();
  src.buffer = buf;
  const filter = c.createBiquadFilter();
  filter.type = "lowpass";
  filter.frequency.value = freq || 900;
  const g = c.createGain();
  g.gain.value = gain || 0.3;
  src.connect(filter);
  filter.connect(g);
  g.connect(master);
  src.start();
}

export const audio = {
  /** Browsers only allow sound after a gesture — call this from a click. */
  unlock() {
    enabled = true;
    ac();
  },
  get on() {
    return enabled;
  },
  toggle() {
    enabled = !enabled;
    if (enabled) ac();
    if (!enabled) audio.stopMusic();
    return enabled;
  },
  jump() {
    tone(320, 0.16, "triangle", 0.18, 720);
  },
  slide() {
    noise(0.22, 0.18, 700);
  },
  lane() {
    tone(520, 0.06, "sine", 0.1, 620);
  },
  pickup() {
    tone(880, 0.09, "square", 0.12);
    tone(1320, 0.12, "square", 0.08);
  },
  coin() {
    tone(1568, 0.05, "square", 0.05, 2093);
  },
  power() {
    [523, 784, 1046].forEach((f, i) => setTimeout(() => tone(f, 0.12, "triangle", 0.16), i * 60));
  },
  crash() {
    noise(0.45, 0.42, 380);
    tone(140, 0.4, "sawtooth", 0.22, 50);
  },
  /** The beat speeds up with the run: 150 ms a step at the start, 105 at top speed. */
  tempo(speed) {
    stepMs = Math.round(150 - Math.max(0, Math.min(1, (speed - 20) / 26)) * 45);
  },
  hit() {
    noise(0.3, 0.34, 500);
    tone(120, 0.24, "sawtooth", 0.2, 60);
  },
  finish() {
    [523, 659, 784, 1046].forEach((f, i) => {
      setTimeout(() => tone(f, 0.22, "triangle", 0.16), i * 110);
    });
  },
  coupon() {
    [659, 880, 1174, 1568].forEach((f, i) => {
      setTimeout(() => tone(f, 0.3, "triangle", 0.18), i * 130);
    });
  },
  ui() {
    tone(840, 0.05, "sine", 0.1);
  },

  /**
   * A loop of plucked arpeggio over a walking bass, one step every stepMs. A
   * roadmap for the ear, not a soundtrack: it must never fight the pickups.
   */
  startMusic() {
    if (!enabled || musicOn) return;
    musicOn = true;
    const bass = [98, 98, 130.8, 98, 116.5, 116.5, 146.8, 110];
    const arp = [392, 523, 659, 523, 440, 587, 698, 587];
    const tick = () => {
      if (!musicOn) return;
      const bar = step % 8;
      tone(bass[bar], 0.32, "triangle", 0.11);
      tone(arp[bar], 0.16, "square", 0.045);
      if (step % 4 === 2) noise(0.06, 0.06, 3000);
      step += 1;
      musicTimer = setTimeout(tick, stepMs);
    };
    tick();
  },
  stopMusic() {
    musicOn = false;
    if (musicTimer) clearTimeout(musicTimer);
    musicTimer = null;
  },
};
