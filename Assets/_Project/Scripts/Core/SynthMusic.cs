using System.Collections.Generic;
using UnityEngine;

namespace DallasDash.Core
{
    /// The soundtrack (client, 4 Nov 2026: "something techy and futuristic, just like the game theme"): an original
    /// cyber-synthwave loop rendered at startup, so nothing third-party has to be licensed.
    ///
    /// 16 bars in A minor (Am - F - C - G), stereo:
    /// - four-on-the-floor kick with sidechain pumping on the pad and bass,
    /// - a rolling sixteenth-note saw bass through a resonant low-pass,
    /// - a sixteenth arpeggio with a dotted-eighth ping-pong echo,
    /// - a wide detuned-saw pad whose filter opens over the loop,
    /// - a bright lead hook in the second half, clap and noise-snare on 2 and 4, sixteenth hats, and a riser
    ///   into the loop seam.
    /// Downtown runs at 124 bpm; the Skypark shortcut is 130 bpm, a tone up, with a brighter arp.
    /// Clips are cached, so the portal teleport never re-renders music mid-run.
    public static class SynthMusic
    {
        const int Rate = 44100;
        static readonly Dictionary<int, AudioClip> cache = new();

        // Chord roots per bar (semitones from A) and chord tones (minor or major triads plus a seventh/ninth colour).
        static readonly int[] Roots = { 0, 0, -4, -4, 3, 3, -2, -2, 0, 0, -4, -4, 3, 3, -2, 2 };
        static readonly int[][] Chords =
        {
            new[] { 0, 3, 7, 10 },   // Am7
            new[] { 0, 4, 7, 11 },   // Fmaj7
            new[] { 0, 4, 7, 14 },   // Cadd9
            new[] { 0, 4, 7, 10 },   // G7 (and the lift into the loop)
        };
        static int ChordOf(int bar) => (bar / 2) % 4;
        // Lead hook, in sixteenths: (semitones from A4, length in sixteenths); -99 is a rest. 8 bars.
        static readonly int[,] Hook =
        {
            {12,2},{-99,1},{12,1},{15,2},{12,2},{19,4},{17,2},{15,2},
            {12,3},{10,1},{12,4},{-99,8},
            {8,2},{-99,1},{8,1},{12,2},{15,2},{17,4},{15,2},{12,2},
            {15,4},{14,2},{12,2},{10,8},
            {12,2},{-99,1},{12,1},{15,2},{12,2},{19,4},{22,2},{19,2},
            {17,3},{15,1},{17,4},{-99,8},
            {19,2},{17,2},{15,2},{12,2},{15,4},{14,4},
            {12,16},
        };

        public static AudioClip Get(int district)
        {
            district = district == 1 ? 1 : 0;
            if (cache.TryGetValue(district, out var clip) && clip) return clip;
            clip = Render(district);
            cache[district] = clip;
            return clip;
        }

        static float Hz(float semisFromA4) => 440f * Mathf.Pow(2f, semisFromA4 / 12f);

        static AudioClip Render(int district)
        {
            float bpm = district == 1 ? 130f : 124f;
            int key = district == 1 ? 2 : 0;
            float beat = 60f / bpm, six = beat / 4f;
            int bars = Roots.Length;
            int n = Mathf.CeilToInt(bars * 4 * beat * Rate);
            var L = new float[n];
            var R = new float[n];
            var rng = new System.Random(7);

            // Sidechain: 0.35 just after each kick, recovering over a beat.
            float Duck(float t)
            {
                float bt = t % beat;
                return 1f - 0.65f * Mathf.Exp(-bt * 9f);
            }

            // ---- Pad: three detuned saws per chord tone, stereo spread, slow filter opening over the loop.
            {
                float lpL = 0f, lpR = 0f;
                var ph = new float[24];
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate;
                    int bar = Mathf.Min((int)(t / (4 * beat)), bars - 1);
                    var chord = Chords[ChordOf(bar)];
                    float sL = 0f, sR = 0f;
                    for (int c = 0; c < chord.Length; c++)
                        for (int d = 0; d < 3; d++)
                        {
                            int k = c * 3 + d;
                            float f = Hz(Roots[bar] + key + chord[c] - 12) * (1f + (d - 1) * 0.0045f);
                            ph[k] = (ph[k] + f / Rate) % 1f;
                            float saw = ph[k] * 2f - 1f;
                            if (d == 0) sL += saw; else if (d == 2) sR += saw; else { sL += saw * 0.5f; sR += saw * 0.5f; }
                        }
                    float open = 0.035f + 0.05f * (0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * t / (bars * 4 * beat)));
                    lpL += (sL - lpL) * open; lpR += (sR - lpR) * open;
                    float g = 0.032f * Duck(t);
                    L[i] += lpL * g; R[i] += lpR * g;
                }
            }

            // ---- Bass: sixteenth saw + sub sine, octave bounce, resonant-ish low-pass with a per-note envelope.
            {
                float lp = 0f, bp = 0f, phase = 0f, sub = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate;
                    int s = (int)(t / six);
                    int bar = Mathf.Min(s / 16, bars - 1);
                    float st = t - s * six;
                    int step = s % 16;
                    // Off the kick on the first sixteenth: the classic rolling pattern.
                    bool rest = step % 4 == 0;
                    int oct = step % 4 == 2 ? 12 : 0;
                    float f = Hz(Roots[bar] + key - 36 + oct);
                    phase = (phase + f / Rate) % 1f;
                    sub = (sub + f * 0.5f / Rate) % 1f;
                    float env = rest ? 0f : Mathf.Exp(-st * 18f);
                    float cutoff = 0.04f + 0.22f * env;
                    float x = (phase * 2f - 1f) * 0.8f + Mathf.Sin(2f * Mathf.PI * sub) * 0.6f;
                    // Two-pole state-variable-style filter with a little resonance.
                    lp += cutoff * bp; float hp = x - lp - 0.6f * bp; bp += cutoff * hp;
                    float v = lp * (rest ? 0f : Mathf.Min(1f, st / 0.003f)) * (0.55f + 0.45f * env) * 0.16f * Duck(t);
                    L[i] += v; R[i] += v;
                }
            }

            // ---- Arp: sixteenths over the chord (up-down, two octaves), pulse wave, dotted-eighth ping-pong echo.
            {
                var arp = new float[n];
                float phase = 0f, lp = 0f;
                int[] order = { 0, 1, 2, 3, 2, 1, 0, 1 };
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate;
                    int s = (int)(t / six);
                    int bar = Mathf.Min(s / 16, bars - 1);
                    var chord = Chords[ChordOf(bar)];
                    float st = t - s * six;
                    int tone = order[s % order.Length];
                    int up = (s / 8) % 2 == 1 ? 12 : 0;
                    float f = Hz(Roots[bar] + key + chord[tone] + up);
                    phase = (phase + f / Rate) % 1f;
                    float duty = district == 1 ? 0.25f : 0.4f;
                    float x = phase < duty ? 1f : -1f;
                    float env = Mathf.Exp(-st * (district == 1 ? 16f : 22f));
                    lp += (x - lp) * (0.08f + 0.25f * env);
                    // The arp enters after two bars.
                    arp[i] = bar < 2 ? 0f : lp * env * 0.07f;
                }
                int delay = Mathf.RoundToInt(six * 3f * Rate);
                for (int i = 0; i < n; i++)
                {
                    float dry = arp[i];
                    float e1 = i >= delay ? arp[i - delay] * 0.45f : 0f;
                    float e2 = i >= 2 * delay ? arp[i - 2 * delay] * 0.22f : 0f;
                    L[i] += dry + e1; R[i] += dry * 0.8f + e2 + e1 * 0.3f;
                }
            }

            // ---- Lead hook in the second half (bars 8-15): supersaw with vibrato and a soft echo.
            {
                int notes = Hook.GetLength(0);
                var lead = new float[n];
                float p1 = 0f, p2 = 0f, lp = 0f;
                float start = 8 * 4 * beat;
                int idx = 0; float noteStart = start;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate;
                    if (t < start) continue;
                    while (idx < notes && t >= noteStart + Hook[idx, 1] * six) { noteStart += Hook[idx, 1] * six; idx++; }
                    if (idx >= notes) break;
                    if (Hook[idx, 0] < -50) { lp *= 0.995f; lead[i] = lp * 0.0f; continue; }
                    float nt = t - noteStart, nlen = Hook[idx, 1] * six;
                    float vib = 1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 5.2f * nt) * Mathf.Clamp01(nt / 0.15f);
                    float f = Hz(Hook[idx, 0] + key) * vib;
                    p1 = (p1 + f / Rate) % 1f; p2 = (p2 + f * 1.007f / Rate) % 1f;
                    float x = (p1 + p2 - 1f);
                    lp += (x - lp) * 0.22f;
                    float env = Mathf.Min(1f, nt / 0.01f) * (nt > nlen - 0.02f ? Mathf.Clamp01((nlen - nt) / 0.02f) : 1f);
                    lead[i] = lp * env * 0.075f;
                }
                int delay = Mathf.RoundToInt(beat * 0.75f * Rate);
                for (int i = 0; i < n; i++)
                {
                    float e = i >= delay ? lead[i - delay] * 0.3f : 0f;
                    L[i] += lead[i] + e * 0.6f; R[i] += lead[i] + e;
                }
            }

            // ---- Drums: kick on every beat, clap + noise snare on 2 and 4, sixteenth hats (open on the offbeat).
            {
                float hpPrev = 0f, hpOut = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)Rate;
                    int b = (int)(t / beat);
                    float bt = t - b * beat;
                    int s = (int)(t / six);
                    float st = t - s * six;
                    int bar = b / 4;
                    float kick = Mathf.Sin(2f * Mathf.PI * (48f * bt + 160f * (1f - Mathf.Exp(-bt * 32f)) / 32f)) * Mathf.Exp(-bt * 7f) * 0.42f
                                 + Mathf.Exp(-bt * 400f) * 0.12f;
                    float noise = (float)rng.NextDouble() * 2f - 1f;
                    hpOut = 0.92f * (hpOut + noise - hpPrev); hpPrev = noise;
                    bool backbeat = b % 2 == 1;
                    float clap = backbeat ? noise * (Mathf.Exp(-bt * 30f) + 0.5f * Mathf.Exp(-Mathf.Max(0f, bt - 0.012f) * 30f) * (bt > 0.012f ? 1f : 0f)) * 0.12f
                                          + noise * Mathf.Exp(-bt * 9f) * 0.035f : 0f;
                    bool open = s % 4 == 2;
                    float hat = hpOut * Mathf.Exp(-st * (open ? 18f : 70f)) * (open ? 0.06f : s % 2 == 1 ? 0.035f : 0.02f);
                    // The intro bar keeps only the hats; fills drop the kick for the last beat of every 8th bar.
                    bool fill = bar % 8 == 7 && b % 4 == 3;
                    float drums = (bar == 0 ? 0f : fill ? 0f : kick) + (bar == 0 ? 0f : clap) + hat;
                    if (fill) drums += noise * Mathf.Exp(-st * 25f) * 0.05f; // snare roll into the next section
                    L[i] += drums + hat * 0.3f; R[i] += drums - hat * 0.3f;
                }
            }

            // ---- Riser into the loop seam: filtered noise sweeping up over the last bar.
            {
                float lp = 0f, riseStart = (bars - 1) * 4 * beat, total = bars * 4 * beat;
                for (int i = Mathf.FloorToInt(riseStart * Rate); i < n; i++)
                {
                    float t = i / (float)Rate;
                    float k = Mathf.Clamp01((t - riseStart) / (total - riseStart));
                    float noise = (float)rng.NextDouble() * 2f - 1f;
                    lp += (noise - lp) * (0.02f + 0.5f * k * k);
                    float v = lp * k * k * 0.06f;
                    L[i] += v; R[i] += v;
                }
            }

            // Soft-clip master, short seam fades, interleave.
            var data = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                float seam = Mathf.Clamp01(i / (0.01f * Rate)) * Mathf.Clamp01((n - i) / (0.01f * Rate));
                data[2 * i] = Soft(L[i] * 1.25f) * seam;
                data[2 * i + 1] = Soft(R[i] * 1.25f) * seam;
            }
            var clip = AudioClip.Create(district == 1 ? "music-skypark" : "music-downtown", n, 2, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Soft(float x) => x / (1f + Mathf.Abs(x)) * 1.1f;
    }
}
