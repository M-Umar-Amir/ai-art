# Score + VO mix for the 30-minute film, rendered one section at a time (a single 30-min Mix would need ~4 GB of RAM).
# Each chunk covers [a_i, a_{i+1}) where a_i = section start - half the gap. Writes build/chunks/mix_NN.wav, then
# build/final_audio.wav (all chunks joined). Voice: the section's TTS file, the score ducked ~10 dB under speech.
import sys, os, json, subprocess, numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from audiolib import *
from scipy.io import wavfile

B = beats(); D = B['duration']; sec = B['sec']; sd = B['secDur']; NS = len(sec)
FF = os.environ.get('FFMPEG', 'ffmpeg')
A = [0.0] + [s - 0.65 for s in sec[1:]] + [D]
def norm(w): return ''.join(c for c in w.lower() if c.isalnum() or c == "'")
def SW(i, word, n=1, frac=0.5):
    c = 0
    for w, t in B['words'][i]:
        if norm(w) == word:
            c += 1
            if c == n: return sec[i] + t
    return sec[i] + sd[i] * frac

# mood per section: chord cycle, pad level, filter cut, optional arp instrument + step
MOOD = {
    0: ([(62, 'madd9'), (58, 'M7'), (65, 'add9'), (60, 'sus2')], 0.24, 1500, pluck, 0.5),
    1: ([(65, 'add9'), (62, 'm7'), (70, 'M7'), (60, 'sus4')], 0.20, 1800, kalimba, 0.75),
    2: ([(57, 'm7'), (53, 'M7'), (55, 'add9'), (52, 'sus4')], 0.20, 1400, None, 0),
    3: ([(57, 'madd9'), (53, 'M7'), (50, 'm7'), (52, 'sus4')], 0.22, 1300, marimba, 0.5),
    4: ([(55, 'add9'), (60, 'M7'), (52, 'm7'), (62, 'sus4')], 0.22, 2000, pluck, 0.66),
    5: ([(52, 'm7'), (48, 'M7'), (55, 'add9'), (50, 'sus2')], 0.22, 1200, marimba, 0.42),
    6: ([(52, 'madd9'), (57, 'm7'), (48, 'M7'), (59, 'sus4')], 0.20, 1300, None, 0),
    7: ([(58, 'add9'), (55, 'm7'), (63, 'M7'), (53, 'sus4')], 0.20, 1700, kalimba, 0.75),
    8: ([(58, 'M7'), (63, 'add9'), (55, 'm7'), (60, 'sus2')], 0.20, 1800, pluck, 0.6),
    9: ([(62, 'm7'), (58, 'M7'), (60, 'sus4'), (57, 'm7')], 0.22, 1400, marimba, 0.38),
    10: ([(50, 'madd9'), (46, 'M7'), (53, 'add9'), (48, 'sus2')], 0.22, 1500, pluck, 0.5),
    11: ([(57, 'm7'), (53, 'add9'), (60, 'M7'), (55, 'sus4')], 0.22, 1900, marimba, 0.33),
    12: ([(52, 'm7'), (48, 'M7'), (50, 'sus2'), (47, 'm7')], 0.20, 1100, None, 0),
    13: ([(57, 'madd9'), (53, 'M7'), (55, 'sus4'), (52, 'm7')], 0.22, 1300, kalimba, 0.8),
    14: ([(60, 'add9'), (65, 'M7'), (57, 'm7'), (62, 'sus4')], 0.20, 1900, pluck, 0.66),
    15: ([(54, 'madd9'), (50, 'M7'), (57, 'sus2'), (52, 'm7')], 0.20, 1300, None, 0),
    16: ([(62, 'add9'), (58, 'M7'), (65, 'add9'), (62, 'sus2')], 0.24, 1600, pluck, 0.6),
}
# sound effects on the picture's key moments: (section, word, n, kind)
FX = [(0, 'nothing', 1, 'stamp'), (0, 'galaxy', 1, 'bell'), (0, 'dead', 1, 'hit'), (0, 'called', 1, 'rise'), (0, 'seven', 1, 'bell'), (0, 'verdict', 1, 'stamp'), (0, 'untouched', 1, 'low'), (0, 'footnote', 1, 'bell'), (0, 'nineteen', 1, 'whoosh'),
      (1, 'brookhaven', 1, 'bell'), (1, 'facing', 1, 'stamp'), (1, 'asked', 2, 'hit'), (1, 'excuse', 1, 'low'), (1, 'sat', 1, 'fall'), (1, 'proceed', 1, 'clink'), (1, 'devastating', 1, 'low'), (1, 'print', 1, 'bell'), (1, 'hold', 1, 'stamp'),
      (2, 'spring', 1, 'whoosh'), (2, 'quarter', 1, 'bell'), (2, 'yukawa', 1, 'bell'), (2, 'fog', 1, 'low'), (2, 'dead', 1, 'hit'),
      (3, 'eight', 1, 'bell'), (3, 'interact', 1, 'whoosh'), (3, 'squeezed', 1, 'rise'), (3, 'sixteen', 1, 'hit'), (3, 'snaps', 1, 'stamp'), (3, 'confinement', 1, 'bell'), (3, 'proof', 1, 'stamp'),
      (4, 'both', 1, 'stamp'), (4, 'nineteen', 1, 'bell'), (4, 'asymptotic', 1, 'rise'), (4, 'nobel', 1, 'bell'), (4, 'remember', 1, 'stamp'), (4, 'higgs', 1, 'whoosh'), (4, 'gap', 1, 'hit'),
      (5, 'says', 1, 'low'), (5, 'glueball', 1, 'bell'), (5, 'twice', 1, 'hit'), (5, 'percent', 2, 'stamp'), (5, 'ninetynine', 1, 'rise'), (5, 'million', 1, 'stamp'),
      (6, 'wilson', 1, 'bell'), (6, 'dice', 1, 'clink'), (6, 'area', 1, 'bell'), (6, 'exact', 1, 'stamp'), (6, 'gap', 1, 'hit'), (6, 'theorem', 1, 'stamp'), (6, 'exist', 1, 'low'),
      (7, 'paris', 1, 'bell'), (7, 'solved', 1, 'clink'), (7, 'words', 1, 'rise'), (7, 'one', 4, 'stamp'), (7, 'trivial', 1, 'stamp'), (7, 'finished', 1, 'low'), (7, 'autumn', 1, 'rise'),
      (8, 'published', 1, 'bell'), (8, 'rigorous', 1, 'rise'), (8, 'released', 1, 'stamp'), (8, 'shepherding', 1, 'bell'), (8, 'flood', 1, 'hit'), (8, 'disclosure', 1, 'low'), (8, 'found', 1, 'rise'),
      (9, 'judgment', 1, 'bell'), (9, 'settle', 1, 'stamp'), (9, 'jury', 1, 'hit'), (9, 'eight', 1, 'rise'), (9, 'errors', 1, 'stamp'), (9, 'rejected', 1, 'stamp'), (9, 'twist', 1, 'hit'), (9, 'anxiety', 1, 'clink'), (9, 'understand', 1, 'low'),
      (10, 'footnote', 1, 'low'), (10, 'crowd', 1, 'bell'), (10, 'somewhere', 1, 'whoosh'), (10, 'quote', 1, 'hit'), (10, 'substantial', 1, 'bell'), (10, 'misleading', 1, 'stamp'), (10, 'halves', 1, 'low'),
      (11, 'evidence', 1, 'low'), (11, 'plunge', 1, 'bell'), (11, 'fail', 1, 'hit'), (11, 'particles', 1, 'bell'), (11, 'fifteenth', 1, 'clink'), (11, 'public', 2, 'stamp'),
      (12, 'more', 1, 'bell'), (12, 'understood', 1, 'low'), (12, 'open', 1, 'hit'), (12, 'somewhere', 1, 'rise'), (12, 'problem', 1, 'bell')]
def fx(mx, t, kind):
    if kind == 'stamp': mx.sfx(t + 0.15, thud(0.8, 70, 0.4), 1, 0, 0.2); mx.sfx(t + 0.15, clunk(0.5), 1)
    elif kind == 'hit': mx.sfx(t, timp(49, 0.8), 1, 0, 0.3); mx.music(t, bell(mf(81), 3.5), 0.18)
    elif kind == 'bell': mx.music(t, bell(mf(int(rng.choice([79, 81, 84, 86]))), 3.5), 0.22, rng.uniform(-0.3, 0.3))
    elif kind == 'whoosh': mx.sfx(t, whoosh(1.0, True, 0.35), 1, rng.uniform(-0.4, 0.4), 0.3)
    elif kind == 'rise': mx.sfx(t - 1.6, shimmer(2.2, 0.35), 1, 0, 0.3); mx.sfx(t - 1.8, whoosh(2.0, True, 0.3), 1)
    elif kind == 'low': mx.sfx(t, thud(0.9, 41, 1.4), 1, 0, 0.3)
    elif kind == 'fall': mx.sfx(t + 1.1, thud(0.5, 90, 0.3), 1); mx.sfx(t + 1.2, click(0.5), 1)
    elif kind == 'clink': mx.sfx(t, bell(mf(96), 1.2), 0.25, 0.2); mx.sfx(t + 0.18, bell(mf(98), 0.8), 0.15, 0.25)

def load(path):
    raw = subprocess.run([FF, '-v', 'error', '-i', path, '-ac', '1', '-ar', str(SR), '-f', 'f32le', '-'], capture_output=True, check=True).stdout
    return np.frombuffer(raw, np.float32).astype(np.float64)

os.makedirs('build/chunks', exist_ok=True)
only = [int(x) for x in sys.argv[1:]] or range(NS)
for i in only:
    a, b = A[i], A[i + 1]; L = b - a; mx = Mix(L)
    chords, g, cut, inst, step_ = MOOD[i]
    prog, t, k = [], 0.4, 0
    while t < L - 1.0: d = min(4.8, L - t); r, q = chords[k % len(chords)]; prog.append((t, d, r, q)); t += 4.8; k += 1
    mx.pads(prog, g, cut=cut, att=1.8, rel=2.4)
    if inst: mx.arp([p for p in prog if p[0] > 1.5], step_, 0.07, inst, 64, 84, (0, 2, 1, 3, 2, 4))
    mx.bassline(prog, 0.12)
    for (s, w, n, kind) in FX:
        if s == i: fx(mx, SW(s, w, n) - a, kind)
    mx.write(f'build/chunks/score_{i:02d}.wav', fade_out=0.6)
    # voice on top, score ducked under speech
    sr, score = wavfile.read(f'build/chunks/score_{i:02d}.wav'); score = score.astype(np.float64).T / 32768.0
    N = score.shape[1]; vo = np.zeros(N); s = load(B['voFiles'][i]); o = int((sec[i] - a) * SR); vo[o:o + len(s)] += s[:max(0, N - o)]
    vo = sosfilt(butter(2, 70, 'high', fs=SR, output='sos'), vo)
    env = sosfilt(butter(1, 3, 'low', fs=SR, output='sos'), np.abs(vo)); env = np.clip(env / (np.percentile(env[env > 1e-4], 90) + 1e-9), 0, 1)
    duck = 10 ** (-10 * env / 20)
    vo_rms = np.sqrt(np.mean(vo[np.abs(vo) > 1e-3] ** 2)); sc_rms = np.sqrt(np.mean(score ** 2)) + 1e-9
    score = score / sc_rms * vo_rms * 10 ** (-13 / 20)          # music bed ~13 dB under the narration
    mix = score * duck + vo[None, :]
    fi = int(0.05 * SR); mix[:, :fi] *= np.linspace(0, 1, fi); mix[:, -fi:] *= np.linspace(1, 0, fi)
    wavfile.write(f'build/chunks/mix_{i:02d}.wav', SR, (np.clip(mix, -1, 1).T * 30000).astype(np.int16))
    print('chunk', i, round(L, 1), 's', flush=True)

if not sys.argv[1:]:
    with open('build/chunks/list.txt', 'w') as f:
        for i in range(NS): f.write(f"file '{os.path.abspath(f'build/chunks/mix_{i:02d}.wav')}'\n")
    subprocess.run([FF, '-y', '-loglevel', 'error', '-f', 'concat', '-safe', '0', '-i', 'build/chunks/list.txt', '-c', 'copy', 'build/final_audio.wav'], check=True)
    print('wrote build/final_audio.wav')
