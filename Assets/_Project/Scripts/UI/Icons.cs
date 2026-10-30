using System.Collections.Generic;
using UnityEngine;

namespace DallasDash.UI
{
    /// Menu and HUD icons drawn at startup from signed-distance shapes: anti-aliased, with a dark ink
    /// outline around the whole silhouette, in the game's chunky cartoon style. No image files needed.
    public static class Icons
    {
        const int N = 128;
        static readonly Dictionary<string, Sprite> cache = new();

        // Shape layers in icon space: x, y in -1..1 (y up). Each returns a signed distance (negative inside).
        delegate float Sdf(Vector2 p);
        struct Layer { public Sdf sdf; public Color color; public Layer(Sdf s, Color c) { sdf = s; color = c; } }

        static readonly Color Ink = new(0.1f, 0.12f, 0.3f);
        static readonly Color Gold = new(1f, 0.78f, 0.16f), GoldDark = new(0.86f, 0.52f, 0.06f), GoldLight = new(1f, 0.93f, 0.55f);
        static readonly Color White = new(1f, 0.98f, 0.94f);

        public static Sprite Get(string kind)
        {
            if (cache.TryGetValue(kind, out var s)) return s;
            s = Draw(Layers(kind));
            cache[kind] = s;
            return s;
        }

        static Layer[] Layers(string kind) => kind switch
        {
            "coin" => new[]
            {
                new Layer(p => Box(p, new Vector2(0.78f,0.78f),0.13f), new Color(0.05f,0.3f,0.44f)),
                new Layer(p => Box(p, new Vector2(0.62f,0.62f),0.08f), new Color(0.03f,0.86f,1f)),
                new Layer(p => Box(p, new Vector2(0.3f,0.3f),0.035f), White),
                new Layer(p => Box(p, new Vector2(0.14f,0.14f),0.025f), new Color(0.04f,0.42f,0.62f)),
            },
            "burger" => new[]
            {
                new Layer(p => Box(p - new Vector2(0f, -0.52f), new Vector2(0.84f, 0.18f), 0.16f), new Color(0.93f, 0.6f, 0.25f)),
                new Layer(p => Box(p - new Vector2(0f, -0.22f), new Vector2(0.9f, 0.14f), 0.12f), new Color(0.45f, 0.22f, 0.1f)),
                new Layer(p => Box(p - new Vector2(0f, 0.0f), new Vector2(0.94f, 0.08f), 0.08f), new Color(0.35f, 0.78f, 0.25f)),
                new Layer(p => Box(p - new Vector2(0f, 0.1f), new Vector2(0.86f, 0.06f), 0.05f), new Color(1f, 0.82f, 0.2f)),
                new Layer(p => Bun(p - new Vector2(0f, 0.18f)), new Color(0.96f, 0.64f, 0.27f)),
                new Layer(p => Circle(p - new Vector2(-0.3f, 0.5f), 0.06f), White),
                new Layer(p => Circle(p - new Vector2(0.1f, 0.6f), 0.06f), White),
                new Layer(p => Circle(p - new Vector2(0.4f, 0.42f), 0.06f), White),
            },
            "trophy" => new[]
            {
                new Layer(p => Ring(p - new Vector2(-0.52f, 0.36f), 0.22f, 0.07f), GoldDark),
                new Layer(p => Ring(p - new Vector2(0.52f, 0.36f), 0.22f, 0.07f), GoldDark),
                new Layer(p => Mathf.Max(Circle(p - new Vector2(0f, 0.42f), 0.5f), p.y - 0.78f), Gold),
                new Layer(p => Box(p - new Vector2(0f, -0.22f), new Vector2(0.1f, 0.24f), 0.02f), GoldDark),
                new Layer(p => Box(p - new Vector2(0f, -0.62f), new Vector2(0.42f, 0.14f), 0.06f), GoldDark),
                new Layer(p => Box(p - new Vector2(-0.18f, 0.5f), new Vector2(0.06f, 0.18f), 0.06f), GoldLight),
            },
            "gear" => new[]
            {
                new Layer(p => Mathf.Max(Gear(p), -Circle(p, 0.26f)), White),
            },
            "calendar" => new[]
            {
                new Layer(p => Box(p - new Vector2(0f, -0.08f), new Vector2(0.78f, 0.7f), 0.14f), White),
                new Layer(p => Mathf.Max(Box(p - new Vector2(0f, -0.08f), new Vector2(0.78f, 0.7f), 0.14f), 0.3f - p.y), new Color(0.92f, 0.2f, 0.22f)),
                new Layer(p => Box(p - new Vector2(-0.36f, 0.66f), new Vector2(0.08f, 0.2f), 0.08f), Ink),
                new Layer(p => Box(p - new Vector2(0.36f, 0.66f), new Vector2(0.08f, 0.2f), 0.08f), Ink),
                new Layer(p => Star(p - new Vector2(0f, -0.28f), 0.3f, 0.13f), Gold),
            },
            "rider" => new[]
            {
                new Layer(p => Circle(p - new Vector2(0f, 0.36f), 0.34f), White),
                new Layer(p => Mathf.Max(Circle(p - new Vector2(0f, -0.86f), 0.78f), p.y + 0.86f - 0.78f), White),
                new Layer(p => Mathf.Max(Circle(p - new Vector2(0f, 0.52f), 0.36f), 0.56f - p.y), new Color(0.3f, 0.5f, 1f)),
                new Layer(p => Box(p - new Vector2(0f,0.4f),new Vector2(0.29f,0.09f),0.06f),new Color(0.05f,0.8f,1f)),
            },
            "boot" => new[]
            {
                new Layer(p => Box(p-new Vector2(-0.15f,0.15f),new Vector2(0.3f,0.58f),0.09f),White),
                new Layer(p => Box(p-new Vector2(0.15f,-0.4f),new Vector2(0.62f,0.24f),0.12f),White),
                new Layer(p => Box(p-new Vector2(0.15f,-0.62f),new Vector2(0.62f,0.06f),0.03f),new Color(0.1f,0.9f,1f)),
            },
            "shield" => new[]
            {
                new Layer(p => Mathf.Max(Circle(p-new Vector2(0f,0.12f),0.78f),-p.y-0.8f),new Color(0.05f,0.75f,0.9f)),
                new Layer(p => Box(p,new Vector2(0.16f,0.44f),0.04f),White),
            },
            "play" => new[] { new Layer(p => Triangle(p - new Vector2(0.08f, 0f), 0.62f) - 0.1f, White) },
            "pause" => new[]
            {
                new Layer(p => Box(p - new Vector2(-0.3f, 0f), new Vector2(0.16f, 0.6f), 0.08f), White),
                new Layer(p => Box(p - new Vector2(0.3f, 0f), new Vector2(0.16f, 0.6f), 0.08f), White),
            },
            "check" => new[]
            {
                new Layer(p => Mathf.Min(Segment(p, new Vector2(-0.6f, 0.04f), new Vector2(-0.16f, -0.44f)),
                                         Segment(p, new Vector2(-0.16f, -0.44f), new Vector2(0.64f, 0.5f))) - 0.16f, White),
            },
            "star" => new[] { new Layer(p => Star(p, 0.86f, 0.4f) - 0.04f, Gold), new Layer(p => Star(p - new Vector2(0f, 0.05f), 0.5f, 0.22f), GoldLight) },
            "lock" => new[]
            {
                new Layer(p => Ring(p - new Vector2(0f, 0.22f), 0.36f, 0.1f), new Color(0.75f, 0.78f, 0.88f)),
                new Layer(p => Box(p - new Vector2(0f, -0.3f), new Vector2(0.6f, 0.44f), 0.12f), Gold),
                new Layer(p => Circle(p - new Vector2(0f, -0.26f), 0.11f), GoldDark),
            },
            "magnet" => new[]
            {
                // Horseshoe magnet: a red U with steel tips.
                new Layer(p => Mathf.Max(Ring(p - new Vector2(0f, 0.05f), 0.5f, 0.2f), p.y - 0.05f), new Color(0.92f, 0.2f, 0.22f)),
                new Layer(p => Box(p - new Vector2(-0.5f, 0.32f), new Vector2(0.2f, 0.28f), 0.04f), new Color(0.92f, 0.2f, 0.22f)),
                new Layer(p => Box(p - new Vector2(0.5f, 0.32f), new Vector2(0.2f, 0.28f), 0.04f), new Color(0.92f, 0.2f, 0.22f)),
                new Layer(p => Box(p - new Vector2(-0.5f, 0.66f), new Vector2(0.2f, 0.12f), 0.04f), new Color(0.82f, 0.86f, 0.95f)),
                new Layer(p => Box(p - new Vector2(0.5f, 0.66f), new Vector2(0.2f, 0.12f), 0.04f), new Color(0.82f, 0.86f, 0.95f)),
            },
            "bolt" => new[] { new Layer(p => Bolt(p), Gold), new Layer(p => Bolt(p * 1.5f + new Vector2(0.06f, 0f)) / 1.5f, GoldLight) },
            "x2" => new[]
            {
                new Layer(p => Circle(p, 0.86f), new Color(0.2f, 0.75f, 0.35f)),
                // An "x" and a "2" drawn from strokes.
                new Layer(p => Mathf.Min(Segment(p, new Vector2(-0.6f, 0.22f), new Vector2(-0.24f, -0.22f)),
                                         Segment(p, new Vector2(-0.6f, -0.22f), new Vector2(-0.24f, 0.22f))) - 0.08f, White),
                new Layer(p => Mathf.Min(Mathf.Max(Ring(p - new Vector2(0.3f, 0.16f), 0.2f, 0.07f), 0.16f - p.y),
                                         Mathf.Min(Segment(p, new Vector2(0.49f, 0.12f), new Vector2(0.1f, -0.32f)),
                                                   Segment(p, new Vector2(0.1f, -0.32f), new Vector2(0.54f, -0.32f))) - 0.07f), White),
            },
            "cart" => new[]
            {
                // Shop: a shopping bag with handles.
                new Layer(p => Ring(p - new Vector2(0f, 0.3f), 0.28f, 0.07f), White),
                new Layer(p => Box(p - new Vector2(0f, -0.2f), new Vector2(0.62f, 0.52f), 0.12f), new Color(1f, 0.55f, 0.2f)),
                new Layer(p => Star(p - new Vector2(0f, -0.2f), 0.3f, 0.13f), GoldLight),
            },
            "list" => new[]
            {
                // Missions: a clipboard with ticks.
                new Layer(p => Box(p - new Vector2(0f, -0.06f), new Vector2(0.62f, 0.78f), 0.12f), White),
                new Layer(p => Box(p - new Vector2(0f, 0.72f), new Vector2(0.28f, 0.12f), 0.06f), new Color(0.6f, 0.62f, 0.75f)),
                new Layer(p => Mathf.Min(Mathf.Min(Box(p - new Vector2(0.12f, 0.36f), new Vector2(0.3f, 0.06f), 0.06f),
                                                   Box(p - new Vector2(0.12f, 0.02f), new Vector2(0.3f, 0.06f), 0.06f)),
                                         Box(p - new Vector2(0.12f, -0.32f), new Vector2(0.3f, 0.06f), 0.06f)), new Color(0.3f, 0.5f, 1f)),
                new Layer(p => Mathf.Min(Mathf.Min(Circle(p - new Vector2(-0.38f, 0.36f), 0.09f), Circle(p - new Vector2(-0.38f, 0.02f), 0.09f)),
                                         Circle(p - new Vector2(-0.38f, -0.32f), 0.09f)), new Color(0.15f, 0.62f, 0.3f)),
            },
            _ => new[] { new Layer(p => Circle(p, 0.8f), Color.magenta) },
        };

        static Sprite Draw(Layer[] layers)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            var px = new Color[N * N];
            const float outline = 0.09f; // ink rim around the whole silhouette, in icon units
            float aa = 2.2f / N;         // one pixel, in icon units (-1..1 across N pixels)
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                // Shrink the icon slightly so the outline fits inside the texture.
                var p = new Vector2((x + 0.5f) / N * 2f - 1f, (y + 0.5f) / N * 2f - 1f) * 1.12f;
                float union = float.MaxValue;
                var c = new Color(0, 0, 0, 0);
                foreach (var l in layers)
                {
                    float d = l.sdf(p);
                    union = Mathf.Min(union, d);
                    float a = Mathf.Clamp01(0.5f - d / aa);
                    c = Color.Lerp(c, l.color, a);
                }
                float rim = Mathf.Clamp01(0.5f - (union - outline) / aa);
                float fill = Mathf.Clamp01(0.5f - union / aa);
                // Ink everywhere inside the expanded silhouette, the painted layers on top.
                var o = Color.Lerp(new Color(Ink.r, Ink.g, Ink.b, rim), new Color(c.r, c.g, c.b, 1f), fill);
                // A soft top-left sheen on every icon gives the glossy toy feel.
                float sheen = Mathf.Clamp01(1f - Vector2.Distance(p, new Vector2(-0.35f, 0.45f)) / 0.9f) * 0.18f * fill;
                o.r += sheen; o.g += sheen; o.b += sheen;
                px[y * N + x] = o;
            }
            tex.SetPixels(px);
            tex.Apply(true);
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---------- Distance functions

        static float Circle(Vector2 p, float r) => p.magnitude - r;

        static float Ring(Vector2 p, float r, float w) => Mathf.Abs(p.magnitude - r) - w;

        static float Box(Vector2 p, Vector2 half, float round)
        {
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + new Vector2(round, round);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - round;
        }

        static float Segment(Vector2 p, Vector2 a, Vector2 b)
        {
            var pa = p - a;
            var ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        /// Dome of the top bun: an upper half-ellipse with a flat base.
        static float Bun(Vector2 p)
        {
            var q = new Vector2(p.x / 0.9f, p.y / 0.62f);
            float d = (q.magnitude - 1f) * 0.62f;
            return Mathf.Max(d, -p.y - 0.02f);
        }

        static float Triangle(Vector2 p, float r)
        {
            // Equilateral triangle pointing right.
            const float k = 1.7320508f;
            p = new Vector2(p.y, p.x);
            p.x = Mathf.Abs(p.x) - r;
            p.y += r / k;
            if (p.x + k * p.y > 0f) p = new Vector2(p.x - k * p.y, -k * p.x - p.y) / 2f;
            p.x -= Mathf.Clamp(p.x, -2f * r, 0f);
            return -p.magnitude * Mathf.Sign(p.y);
        }

        static float Star(Vector2 p, float outer, float inner)
        {
            // Exact distance to a five-point star polygon.
            float best = float.MaxValue;
            bool inside = false;
            for (int i = 0; i < 10; i++)
            {
                Vector2 a = StarPoint(i, outer, inner), b = StarPoint(i + 1, outer, inner);
                best = Mathf.Min(best, Segment(p, a, b));
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside ? -best : best;
        }

        static Vector2 StarPoint(int i, float outer, float inner)
        {
            float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float r = i % 2 == 0 ? outer : inner;
            return new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
        }

        /// Lightning bolt: a zig-zag polygon.
        static readonly Vector2[] BoltPts =
        {
            new(0.18f, 0.9f), new(-0.5f, -0.02f), new(-0.02f, -0.02f), new(-0.22f, -0.9f),
            new(0.52f, 0.12f), new(0.04f, 0.12f), new(0.3f, 0.9f),
        };

        static float Bolt(Vector2 p)
        {
            float best = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = BoltPts.Length - 1; i < BoltPts.Length; j = i++)
            {
                Vector2 a = BoltPts[j], b = BoltPts[i];
                best = Mathf.Min(best, Segment(p, a, b));
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside ? -best : best;
        }

                static float Gear(Vector2 p)
        {
            float ang = Mathf.Atan2(p.y, p.x);
            // Eight square-ish teeth around a disc.
            float tooth = Mathf.Clamp01((Mathf.Cos(ang * 8f) - 0.1f) * 3f);
            return p.magnitude - (0.6f + 0.2f * tooth);
        }
    }
}
