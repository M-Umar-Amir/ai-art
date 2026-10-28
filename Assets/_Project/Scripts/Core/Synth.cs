using UnityEngine;

namespace DallasDash.Core
{
    /// Sound effects generated at startup, so the game ships with no third-party audio to license.
    public static class Synth
    {
        const int Rate = 44100;

        static AudioClip Make(string name, float seconds, System.Func<float, float> wave)
        {
            int n = (int)(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Env(float t, float len, float attack = 0.005f) => Mathf.Min(1f, t / attack) * Mathf.Pow(1f - Mathf.Clamp01(t / len), 2f);

        /// Two-note bright chime for a burger.
        public static AudioClip Coin() => Make("coin", 0.22f, t =>
        {
            float f = t < 0.07f ? 988f : 1319f;
            return Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f * Env(t, 0.22f) + Mathf.Sin(4f * Mathf.PI * f * t) * 0.12f * Env(t, 0.12f);
        });

        public static AudioClip Sweep(float from, float to, float len, float vol) => Make("sweep", len, t =>
        {
            float k = t / len;
            float phase = 2f * Mathf.PI * (from * t + (to - from) * t * k * 0.5f);
            return (Mathf.Sin(phase) > 0 ? 1f : -1f) * 0.25f * vol * Env(t, len);
        });

        public static AudioClip Noise(float len, float vol)
        {
            var rng = new System.Random(7);
            float lp = 0f;
            return Make("noise", len, t =>
            {
                lp += ((float)rng.NextDouble() * 2f - 1f - lp) * 0.25f;
                return lp * vol * 3f * Mathf.Sin(Mathf.PI * t / len);
            });
        }

        public static AudioClip Thud()
        {
            var rng = new System.Random(3);
            return Make("thud", 0.35f, t => (Mathf.Sin(2f * Mathf.PI * (90f - 120f * t) * t) * 0.8f + ((float)rng.NextDouble() - 0.5f) * 0.4f * Env(t, 0.08f)) * Env(t, 0.35f, 0.002f));
        }

        // ---- Music: an original city-groove loop (written for the game, no seasonal theme). Pulse-wave lead,
        // walking triangle bass, closed hi-hats on the eighths with a clap on 2 and 4. 16 bars at 126 bpm, ~30 s.

        // Melody as (semitones from C5, beats); -99 is a rest. Two 8-bar phrases over C - Am - F - G.
        static readonly float[,] Tune =
        {
            {7,0.5f},{9,0.5f},{7,0.5f},{4,0.5f},{-99,0.5f},{4,0.5f},{2,0.5f},{0,0.5f}, {2,1},{4,1},{-99,1},{0,1},
            {9,0.5f},{7,0.5f},{4,0.5f},{7,0.5f},{-99,0.5f},{9,0.5f},{12,1}, {11,1},{9,1},{7,2},
            {5,0.5f},{7,0.5f},{9,0.5f},{5,0.5f},{-99,0.5f},{12,0.5f},{9,1}, {7,1},{5,1},{4,1},{2,1},
            {7,0.5f},{-99,0.5f},{7,0.5f},{9,0.5f},{11,1},{14,1}, {12,3},{-99,1},
            {12,0.5f},{11,0.5f},{9,0.5f},{7,0.5f},{-99,0.5f},{7,0.5f},{9,0.5f},{12,0.5f}, {14,1},{12,1},{9,1},{7,1},
            {9,0.5f},{12,0.5f},{16,1},{14,0.5f},{12,0.5f},{9,1}, {12,1},{9,1},{4,2},
            {5,0.5f},{9,0.5f},{12,0.5f},{9,0.5f},{5,0.5f},{9,0.5f},{12,1}, {14,1},{12,1},{11,1},{9,1},
            {7,1},{11,1},{14,1},{11,1}, {12,2},{-99,2},
        };
        // Bass root per bar (semitones from C3): C C Am Am F F G G, twice.
        static readonly int[] Roots = { 0, 0, -3, -3, 5, 5, 7, 7, 0, 0, -3, -3, 5, 5, 7, 7 };

        static float Hz(float semisFromC, float octaveC) => octaveC * Mathf.Pow(2f, semisFromC / 12f);

        public static AudioClip Music()
        {
            const float bpm = 126f, beat = 60f / bpm;
            int notes = Tune.GetLength(0);
            var starts = new float[notes];
            float acc = 0f;
            for (int i = 0; i < notes; i++) { starts[i] = acc; acc += Tune[i, 1] * beat; }
            float length = Roots.Length * 4 * beat;
            var rng = new System.Random(11);
            float noiseLp = 0f;
            int idx = 0;
            return Make("music", length, t =>
            {
                while (idx < notes - 1 && t >= starts[idx + 1]) idx++;
                if (t < starts[idx]) idx = 0;
                float lead = 0f;
                if (Tune[idx, 0] > -50f && t < acc)
                {
                    float nt = t - starts[idx], nlen = Tune[idx, 1] * beat;
                    float f = Hz(Tune[idx, 0], 523.25f) * (1f + 0.004f * Mathf.Sin(2f * Mathf.PI * 5.5f * nt));
                    float ph = (f * nt) % 1f;
                    lead = (ph < 0.25f ? 1f : -0.33f) * 0.13f * Mathf.Min(1f, nt / 0.01f) * Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(nt / nlen)) * (nt > nlen - 0.03f ? 0.3f : 1f);
                }

                // Walking bass on the eighths: root, root, octave, fifth, triangle wave with a short pluck.
                float half = beat * 0.5f;
                int e8 = (int)(t / half);
                int bar = Mathf.Min(e8 / 8, Roots.Length - 1);
                float et = t - e8 * half;
                int step = e8 % 4;
                float bf = Hz(Roots[bar] + (step == 2 ? 12 : step == 3 ? 7 : 0), 130.81f);
                float tri = 2f * Mathf.Abs(2f * ((bf * et) % 1f) - 1f) - 1f;
                float bass = tri * 0.2f * Mathf.Exp(-et * 6f);

                // Kick on every beat, closed hats on the off-eighths, a clap on 2 and 4.
                int b = (int)(t / beat);
                float bt = t - b * beat;
                float kick = Mathf.Sin(2f * Mathf.PI * (55f + 90f * Mathf.Exp(-bt * 30f)) * bt) * Mathf.Exp(-bt * 14f) * 0.28f;
                float n = (float)rng.NextDouble() * 2f - 1f;
                float hp = n - noiseLp; noiseLp += (n - noiseLp) * 0.35f;
                float hats = hp * Mathf.Exp(-et * 60f) * (e8 % 2 == 1 ? 0.09f : 0.03f);
                float clap = b % 2 == 1 ? n * Mathf.Exp(-bt * 22f) * 0.1f : 0f;

                // Short fade at the loop seam.
                float seam = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((length - t) / 0.02f);
                return (lead + bass + kick + hats + clap) * seam;
            });
        }

        /// Seamless two-tone loop for the police bicycle.
        public static AudioClip Siren() => Make("siren", 1.2f, t =>
        {
            float f = t < 0.6f ? 740f : 587f;
            float phase = 2f * Mathf.PI * f * t;
            return (Mathf.Sin(phase) * 0.6f + Mathf.Sin(phase * 2f) * 0.15f) * 0.5f;
        });
    }
}
