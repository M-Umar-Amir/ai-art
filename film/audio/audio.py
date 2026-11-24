# Score, ambience and effects for "Two Clocks in Sunlight". The voiceover is mixed on top in vo_mix.py.
# A quiet theme that comes home at the end; each section gets its own colour. All timing comes from src/beats.json.
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from audiolib import *

B = beats(); D = B['duration']; sec = B['sec']; sd = B['secDur']
def norm(w): return ''.join(c for c in w.lower() if c.isalnum() or c == "'")
def SW(i, word, n=1, frac=0.5):
    c = 0
    for w, t in (B.get('words') or [[]] * 8)[i]:
        if norm(w) == word:
            c += 1
            if c == n: return sec[i] + t
    return sec[i] + sd[i] * frac
END = lambda i: sec[i + 1] if i + 1 < len(sec) else D
mx = Mix(D)

def section_pads(t0, t1, prog_cycle, g, cut=1500, bar=4.0):
    t, k, prog = t0, 0, []
    while t < t1 - 0.5:
        d = min(bar, t1 - t); root, q = prog_cycle[k % len(prog_cycle)]; prog.append((t, d, root, q)); t += bar; k += 1
    mx.pads(prog, g, cut=cut, att=1.6, rel=2.2); return prog

# 0 cold open + 7 close: warm D major theme with a harp-like arpeggio, meadow and soft wind
for (a, b) in [(0.0, sec[1] - 0.5), (sec[7] - 0.6, D)]:
    p = section_pads(a + 0.3, b, [(62, 'add9'), (59, 'm7'), (67, 'M7'), (62, 'sus4')], 0.26)
    mx.arp([x for x in p if x[0] > a + 2], 4 / 6, 0.11, pluck, 66, 86, (0, 2, 4, 1, 3, 5))
    mx.bed(a, b, meadow, 0.22, fi=2, fo=2); mx.bed(a, b, wind, 0.28, fi=2, fo=2)
mx.music(SW(0, 'hundred', 1, 0.42), bell(mf(81), 4.0), 0.25); mx.music(SW(7, 'new', 1, 0.75), bell(mf(78), 5.0), 0.25); mx.music(SW(7, 'ancient', 1, 0.92), bell(mf(86), 6.0), 0.22)
# the ruler + space: thin shimmer
mx.music(SW(0, 'center', 1, 0.5), shimmer(3.0, 0.25), 0.4)
mx.sfx(SW(0, 'cross', 1, 0.6), whoosh(2.3, True, 0.35), 1, 0.4)

# 1 core: lower, warmer, a slow pulse; fusion lands on a bell + soft timpani
section_pads(sec[1] - 0.5, sec[2] - 0.4, [(57, 'madd9'), (53, 'M7'), (55, 'add9'), (57, 'sus2')], 0.24, cut=1100)
mx.bed(sec[1], sec[2], lambda d: 0.4 * lp(brown(d), 160), 0.5, fi=2, fo=1.5)
tF = SW(1, 'four', 1, 0.42)
mx.music(tF + 0.9, bell(mf(76), 4.0), 0.35); mx.sfx(tF + 0.9, timp(55, 0.5), 1, 0, 0.3)
mx.sfx(SW(1, 'equals', 1, 0.68), shimmer(2.0, 0.3), 1, 0.2)
mx.sfx(SW(1, 'instant', 1, 0.8), whoosh(0.9, True, 0.5), 1, 0.5)
tHit = SW(1, "isn't", 1, 0.97); mx.sfx(tHit, thud(0.9), 1, 0.3, 0.3); mx.sfx(tHit, click(0.6), 1, 0.3)

# 2 pinball: soft marimba ticks on the walk's collisions (about two a second, randomly panned), then the √N chart theme
section_pads(sec[2] - 0.4, sec[3] - 0.6, [(57, 'm7'), (53, 'M7'), (50, 'madd9'), (52, 'sus4')], 0.22, cut=1300)
t = sec[2] + 0.6
while t < SW(2, 'physicists', 1, 0.4): mx.music(t, marimba(mf(int(rng.choice([69, 72, 74, 76, 79]))), 0.5), 0.12, rng.uniform(-0.7, 0.7)); t += 1 / 2.4
for w, f in [('hundred', 0.6), ('thousand', 0.66), ('million', 0.72)]: mx.music(SW(2, w, 1, f), bell(mf(79), 2.5), 0.2)
mx.sfx(SW(2, 'scale', 1, 0.75), whoosh(1.4, False, 0.35), 1, -0.2)

# 3 snail: light, playful kalimba over birds; then the relay and the timeline, a calm pad
section_pads(sec[3] - 0.6, sec[4] - 0.5, [(65, 'add9'), (62, 'm7'), (70, 'M7'), (65, 'sus2')], 0.2)
mx.bed(sec[3] - 0.6, SW(3, "isn't", 1, 0.3), meadow, 0.25, fi=1.5, fo=1.0)
for k, tt_ in enumerate(np.arange(sec[3] + 1, SW(3, "isn't", 1, 0.3), 0.75)): mx.music(tt_, kalimba(mf([72, 77, 81, 77, 74, 79][k % 6])), 0.12, 0.3 * ((-1) ** k))
for k in range(4): mx.sfx(sec[3] + 2 + k * 3.3, chirp(0.3), 1, rng.uniform(-0.8, 0.8))
t = SW(3, "isn't", 1, 0.3) + 0.8
for k in range(6): mx.music(t + k * 1.25, pluck(mf([84, 81, 79, 76, 74, 72][k]), 1.5), 0.14, -0.5 + k * 0.2)

# 4 boiling zone: low rumble and bubbling
section_pads(sec[4] - 0.5, sec[5] - 0.5, [(50, 'm7'), (48, 'M7'), (45, 'madd9'), (50, 'sus2')], 0.22, cut=900)
mx.bed(sec[4], sec[5], lambda d: rumble(d, 0.6), 0.45, fi=1.5, fo=1.5)
for k in range(int(sd[4] * 2.5)): mx.sfx(sec[4] + k * 0.4 + rng.uniform(0, 0.2), pop(0.12), 1, rng.uniform(-0.8, 0.8))

# 5 escape: it opens up; a rising sweep as the light flies free, a long held chord across space
section_pads(sec[5] - 0.5, sec[6] - 0.5, [(62, 'add9'), (67, 'add9'), (64, 'sus4'), (62, 'M7')], 0.26, cut=2200)
mx.sfx(SW(5, 'nothing', 1, 0.45), whoosh(3.5, True, 0.4), 1, 0.3, 0.3); mx.music(SW(5, 'leave', 1, 0.75), bell(mf(81), 3.0), 0.22); mx.music(SW(5, 'everything', 1, 0.88), bell(mf(86), 3.0), 0.22)

# 6 neutrinos: mysterious; glassy shimmers, night crickets, a soft ring for the detector
section_pads(sec[6] - 0.5, sec[7] - 0.6, [(56, 'madd9'), (52, 'M7'), (59, 'sus2'), (54, 'm7')], 0.22, cut=1400)
mx.sfx(SW(6, 'neutrinos', 1, 0.22), shimmer(3.0, 0.3, 90, 104), 1, -0.3)
mx.bed(SW(6, 'sixty', 1, 0.42), SW(6, 'detectors', 1, 0.6), night, 0.35, fi=1, fo=1)
mx.music(SW(6, 'detectors', 1, 0.6) + 1.6, bell(mf(88), 3.0), 0.25)
mx.sfx(SW(6, 'switched', 1, 0.82), whoosh(1.5, False, 0.3), 1, 0)

mx.write(sys.argv[1] if len(sys.argv) > 1 else 'build/score.wav', fade_out=3.0)
