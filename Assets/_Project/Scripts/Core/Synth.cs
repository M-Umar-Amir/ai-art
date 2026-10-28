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

        // ---- Music: "Jingle Bells" (James Lord Pierpont, 1857: public domain) as a bright chiptune loop.
        // Pulse-wave lead, oom-pah bass, sleigh-bell shimmer on the off-beats. 16 bars at 150 bpm, ~25.6 s.

        // Melody as (semitones from C5, beats); the chorus twice, with its two endings.
        static readonly float[,] Tune =
        {
            {4,1},{4,1},{4,2}, {4,1},{4,1},{4,2}, {4,1},{7,1},{0,1.5f},{2,0.5f}, {4,4},
            {5,1},{5,1},{5,1.5f},{5,0.5f}, {5,1},{4,1},{4,1},{4,0.5f},{4,0.5f}, {4,1},{2,1},{2,1},{4,1}, {2,2},{7,2},
            {4,1},{4,1},{4,2}, {4,1},{4,1},{4,2}, {4,1},{7,1},{0,1.5f},{2,0.5f}, {4,4},
            {5,1},{5,1},{5,1.5f},{5,0.5f}, {5,1},{4,1},{4,1},{4,0.5f},{4,0.5f}, {7,1},{7,1},{5,1},{2,1}, {0,4},
        };
        // Bass root per bar (semitones from C3): C C C C F C D G | C C C C F C G C.
        static readonly int[] Roots = { 0, 0, 0, 0, 5, 0, 2, 7, 0, 0, 0, 0, 5, 0, 7, 0 };

        static float Hz(float semisFromC, float octaveC) => octaveC * Mathf.Pow(2f, semisFromC / 12f);

        public static AudioClip Music()
        {
            const float bpm = 150f, beat = 60f / bpm;
            int notes = Tune.GetLength(0);
            var starts = new float[notes];
            float acc = 0f;
            for (int i = 0; i < notes; i++) { starts[i] = acc; acc += Tune[i, 1] * beat; }
            float length = acc;
            var rng = new System.Random(11);
            float noiseLp = 0f;
            int idx = 0;
            return Make("music", length, t =>
            {
                while (idx < notes - 1 && t >= starts[idx + 1]) idx++;
                if (t < starts[idx]) idx = 0;
                float nt = t - starts[idx], nlen = Tune[idx, 1] * beat;
                float f = Hz(Tune[idx, 0], 523.25f) * (1f + 0.004f * Mathf.Sin(2f * Mathf.PI * 5.5f * nt));
                float ph = (f * nt) % 1f;
                float lead = (ph < 0.25f ? 1f : -0.33f) * 0.16f * Mathf.Min(1f, nt / 0.01f) * Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(nt / nlen)) * (nt > nlen - 0.03f ? 0.3f : 1f);

                // Oom-pah: root on beats 1 and 3, the fifth on 2 and 4, triangle wave.
                int b = (int)(t / beat);
                int bar = Mathf.Min(b / 4, Roots.Length - 1);
                float bt = t - b * beat;
                float bf = Hz(Roots[bar] + (b % 2 == 1 ? 7 : 0), 130.81f);
                float tri = 2f * Mathf.Abs(2f * ((bf * bt) % 1f) - 1f) - 1f;
                float bass = tri * 0.22f * Mathf.Exp(-bt * 5f);

                // Sleigh bells: bright noise bursts on every eighth, louder on the off-beat.
                float e = t / (beat * 0.5f);
                float et = (e - Mathf.Floor(e)) * beat * 0.5f;
                float n = (float)rng.NextDouble() * 2f - 1f;
                float hp = n - noiseLp; noiseLp += (n - noiseLp) * 0.35f;
                float bells = hp * Mathf.Exp(-et * 38f) * (((int)e % 2 == 1) ? 0.11f : 0.05f);

                // Short fade at the loop seam.
                float seam = Mathf.Clamp01(t / 0.02f) * Mathf.Clamp01((length - t) / 0.02f);
                return (lead + bass + bells) * seam;
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
