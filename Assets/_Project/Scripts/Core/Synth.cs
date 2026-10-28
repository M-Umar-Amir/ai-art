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

        /// Seamless two-tone loop for the police bicycle.
        public static AudioClip Siren() => Make("siren", 1.2f, t =>
        {
            float f = t < 0.6f ? 740f : 587f;
            float phase = 2f * Mathf.PI * f * t;
            return (Mathf.Sin(phase) * 0.6f + Mathf.Sin(phase * 2f) * 0.15f) * 0.5f;
        });
    }
}
