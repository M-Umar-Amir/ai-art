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

        /// The soundtrack: the cyber-synthwave loop (SynthMusic, 4 Nov 2026), cached per world.
        public static AudioClip Music(int district = 0) => SynthMusic.Get(district);

        /// Seamless two-tone loop for the police bicycle.
        public static AudioClip Siren() => Make("siren", 1.2f, t =>
        {
            float f = t < 0.6f ? 740f : 587f;
            float phase = 2f * Mathf.PI * f * t;
            return (Mathf.Sin(phase) * 0.6f + Mathf.Sin(phase * 2f) * 0.15f) * 0.5f;
        });

        /// A short spoken-vowel blip for the Riders City voice-preset picker. Six base pitches and
        /// formant weights stand in for the different voices until the real voice service exists.
        public static AudioClip Voice(int preset)
        {
            float[] pitch = { 196f, 233f, 147f, 262f, 220f, 131f };
            float[] f1 = { 700f, 850f, 520f, 760f, 620f, 480f };
            float[] f2 = { 1200f, 1500f, 900f, 1350f, 1100f, 780f };
            int i = Mathf.Clamp(preset, 0, pitch.Length - 1);
            float baseHz = pitch[i], formant1 = f1[i], formant2 = f2[i];
            return Make("voice", 0.42f, t =>
            {
                float env = Env(t, 0.42f, 0.02f);
                float vibrato = 1f + 0.012f * Mathf.Sin(2f * Mathf.PI * 5.2f * t);
                float phase = 2f * Mathf.PI * baseHz * vibrato * t;
                float glottal = Mathf.Sin(phase) + 0.45f * Mathf.Sin(phase * 2f) + 0.22f * Mathf.Sin(phase * 3f);
                float vowel = 0.55f * glottal
                    + 0.25f * Mathf.Sin(2f * Mathf.PI * formant1 * t) * 0.4f
                    + 0.14f * Mathf.Sin(2f * Mathf.PI * formant2 * t) * 0.25f;
                return vowel * 0.34f * env;
            });
        }
    }
}
