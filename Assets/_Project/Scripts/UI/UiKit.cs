using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DallasDash.UI
{
    /// Small runtime builder for the menus, so every screen is code and needs no hand-made prefabs.
    /// Rounded panels and buttons use one generated 9-sliced sprite.
    public static class UiKit
    {
        public static Font Font;

        // Dash Riders identity: courier orange for actions, Bolt magenta for danger, express-lane cyan
        // for the fourth lane, Nova purple for parkour, deep indigo chrome behind all of it.
        // Enhanced with more vibrant, mobile-friendly colors inspired by Subway Surfers
        public static readonly Color Blue = new(0.12f, 0.15f, 0.32f);
        public static readonly Color Navy = new(0.045f, 0.055f, 0.14f, 0.96f);
        public static readonly Color Cream = new(1f, 0.98f, 0.95f);
        public static readonly Color Yellow = new(1f, 0.72f, 0.15f);
        public static readonly Color Red = new(.94f, .22f, .15f);
        public static readonly Color Green = new(.28f, .62f, .38f);
        public static readonly Color Pink = new(1f, 0.42f, 0.12f);
        public static readonly Color Ink = new(0.06f, 0.07f, 0.18f);
        /// The fourth lane and every express/rooftop marker.
        public static readonly Color Cyan = new(.45f, .82f, .94f);
        /// Nova's parkour line and the crew ribbons.
        public static readonly Color Purple = new(.68f, .42f, .94f);
        public static readonly Color Surface = new(.075f, .095f, .18f);
        public static readonly Color Muted = new(.65f, .72f, .82f);
        /// Additional colors for enhanced UI
        public static readonly Color Gold = new(1f, 0.85f, 0.3f);
        public static readonly Color Orange = new(1f, 0.55f, 0.2f);
        public static readonly Color Teal = new(0.2f, 0.8f, 0.75f);
        public static readonly Color Magenta = new(0.95f, 0.3f, 0.6f);

        static Sprite rounded, gradient, soft, disc, glow, gradient2D;
        public static Sprite Disc => disc ? disc : disc = MakeDisc();
        public static Sprite Glow => glow ? glow : glow = MakeGlow();
        public static Sprite Gradient2D => gradient2D ? gradient2D : gradient2D = MakeGradient2D();
        static Sprite MakeDisc()
        {
            var texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < 96; y++) for (int x = 0; x < 96; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(47.5f, 47.5f)) / 47.5f;
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.SmoothStep(0, 1, 1 - distance)));
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 96, 96), new Vector2(.5f, .5f));
        }
        public static Sprite Rounded => rounded ? rounded : rounded = MakeRounded(false);
        /// Same shape, lit from above: white at the top shading to grey at the bottom, so a tint becomes a gradient.
        public static Sprite Gradient => gradient ? gradient : gradient = MakeRounded(true);
        /// Blurred rounded shape for drop shadows.
        public static Sprite Soft => soft ? soft : soft = MakeSoft();
        /// Radial glow for emphasis effects
        static Sprite MakeGlow()
        {
            const int n = 128;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 63.5f;
                float a = Mathf.SmoothStep(1f, 0f, distance);
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }
        /// 2D gradient from top-left to bottom-right
        static Sprite MakeGradient2D()
        {
            const int n = 64;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (n - 1f);
                float v = y / (n - 1f);
                float brightness = Mathf.Lerp(0.85f, 1f, (u + v) * 0.5f);
                t.SetPixel(x, y, new Color(brightness, brightness, brightness, 1f));
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
        }

        static Sprite MakeSoft()
        {
            const int n = 96, r = 40;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(r - x, x - (n - 1 - r)));
                float dy = Mathf.Max(0, Mathf.Max(r - y, y - (n - 1 - r)));
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((r - Mathf.Sqrt(dx * dx + dy * dy)) / 26f));
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        static Sprite MakeRounded(bool shaded)
        {
            const int n = 64, r = 20;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(r - x, x - (n - 1 - r)));
                float dy = Mathf.Max(0, Mathf.Max(r - y, y - (n - 1 - r)));
                float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                float v = shaded ? Mathf.Lerp(0.74f, 1f, y / (n - 1f)) : 1f;
                t.SetPixel(x, y, new Color(v, v, v, a));
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos = default, Vector2 size = default)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Centered(Transform parent, string name, Vector2 pos, Vector2 size) =>
            Rect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);

        public static Image Panel(Transform parent, string name, Vector2 pos, Vector2 size, Color c)
        {
            var img = Centered(parent, name, pos, size).gameObject.AddComponent<Image>();
            img.sprite = Rounded;
            img.type = Image.Type.Sliced;
            img.color = c;
            return img;
        }

        /// Full-screen dim layer that also blocks taps reaching the game behind it.
        public static Image Shade(Transform parent, string name)
        {
            var rt = Rect(parent, name, Vector2.zero, Vector2.one);
            rt.sizeDelta = new Vector2(300, 900);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(.012f, .018f, .04f, .88f);
            return img;
        }

        /// A card that pops in whenever its screen opens.
        public static Image Card(Transform parent, Vector2 pos, Vector2 size)
        {
            // The root is invisible (it only blocks taps); children draw in order: shadow, light rim,
            // gradient face, sheen. Content added later lands on top.
            var img = Panel(parent, "Card", pos, size, new Color(0, 0, 0, 0));
            Shadow(img.transform, size, 8f);
            Panel(img.transform, "Rim", Vector2.zero, size, new Color(.35f, .42f, .58f, .75f)).raycastTarget = false;
            var face = Panel(img.transform, "Face", Vector2.zero, size - new Vector2(3f, 3f), new Color(.035f, .05f, .095f, 1));
            face.sprite = Gradient2D;
            face.raycastTarget = false;
            var sheen = Panel(img.transform, "Sheen", new Vector2(0f, size.y * .5f - 20f), new Vector2(72, 3), Cyan);
            sheen.raycastTarget = false;
            img.gameObject.AddComponent<PanelPop>();
            return img;
        }

        /// Enhanced card with decorative elements and glow
        public static Image FancyCard(Transform parent, Vector2 pos, Vector2 size, Color accentColor)
        {
            var img = Panel(parent, "FancyCard", pos, size, new Color(0, 0, 0, 0));
            Shadow(img.transform, size, 12f);
            // Glowing rim with accent color
            var glow = Centered(img.transform, "Glow", Vector2.zero, size + new Vector2(8, 8)).gameObject.AddComponent<Image>();
            glow.sprite = Glow;
            glow.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.15f);
            glow.raycastTarget = false;
            // Add subtle glow animation
            var glowEffect = glow.gameObject.AddComponent<VisualEffects>();
            glowEffect.effect = VisualEffects.EffectType.PulseGlow;
            glowEffect.intensity = 0.5f;
            glowEffect.speed = 0.8f;
            glowEffect.glowColor = accentColor;

            Panel(img.transform, "Rim", Vector2.zero, size, new Color(.4f, .48f, .65f, .85f)).raycastTarget = false;
            var face = Panel(img.transform, "Face", Vector2.zero, size - new Vector2(4f, 4f), new Color(.04f, .06f, .11f, 1));
            face.sprite = Gradient2D;
            face.raycastTarget = false;
            // Corner accents
            float cornerSize = 24f;
            var corners = new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) };
            foreach (var corner in corners)
            {
                var cornerEl = Panel(img.transform, "Corner", corner * (size * 0.5f - new Vector2(cornerSize, cornerSize)), new Vector2(cornerSize * 2, cornerSize * 2), accentColor);
                cornerEl.raycastTarget = false;
            }
            var sheen = Panel(img.transform, "Sheen", new Vector2(0f, size.y * .5f - 24f), new Vector2(88, 4), accentColor);
            sheen.raycastTarget = false;
            // Add shimmer effect to sheen
            var sheenEffect = sheen.gameObject.AddComponent<VisualEffects>();
            sheenEffect.effect = VisualEffects.EffectType.Shimmer;
            sheenEffect.intensity = 0.7f;
            sheenEffect.speed = 1.2f;
            sheenEffect.glowColor = accentColor;

            img.gameObject.AddComponent<PanelPop>();
            return img;
        }

        /// Soft drop shadow under a panel, sitting a little below it.
        public static Image Shadow(Transform owner, Vector2 size, float drop)
        {
            var sh = Centered(owner, "Shadow", new Vector2(0f, -drop), size + new Vector2(40f, 40f)).gameObject.AddComponent<Image>();
            sh.sprite = Soft;
            sh.type = Image.Type.Sliced;
            sh.color = new Color(0.02f, 0.03f, 0.12f, 0.45f);
            sh.raycastTarget = false;
            sh.transform.SetAsFirstSibling();
            return sh;
        }

        public static Image Icon(Transform parent, string kind, Vector2 pos, float size)
        {
            var img = Centered(parent, kind + " Icon", pos, new Vector2(size, size)).gameObject.AddComponent<Image>();
            img.sprite = Icons.Get(kind);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// Rounded translucent pill with an icon on the left and a value label: coin and score counters.
        public static Text Pill(Transform parent, string icon, Vector2 pos, float width, Color textColor, out RectTransform root)
        {
            var bg = Panel(parent, icon + " Pill", pos, new Vector2(width, 80f), new Color(.035f, .05f, .1f, .9f));
            bg.raycastTarget = false;
            root = bg.rectTransform;
            Icon(bg.transform, icon, new Vector2(-width * .5f + 42f, 0), 44f);
            return Label(bg.transform, "", 40, new Vector2(25, 0), new Vector2(width - 100, 70), textColor);
        }

        public static Text Label(Transform parent, string text, int size, Vector2 pos, Vector2 box, Color c, TextAnchor anchor = TextAnchor.MiddleCenter, bool outline = false)
        {
            var rt = Centered(parent, "Label", pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            if (size >= 64 && parent.name != "Logo" && parent.name != "Ribbon") size = Mathf.Min(size, 64);
            t.fontSize = size;
            t.text = text;
            t.color = c;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Min(size, Mathf.Max(18, Mathf.RoundToInt(size * .75f)));
            t.resizeTextMaxSize = size;
            t.lineSpacing = 1.08f;
            t.raycastTarget = false;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(Ink.r, Ink.g, Ink.b, 0.95f);
                float w = size >= 80 ? 2f : size >= 48 ? 1.5f : 0.6f;
                o.effectDistance = new Vector2(w, -w);
                if (size >= 48)
                {
                    var sh = rt.gameObject.AddComponent<Shadow>();
                    sh.effectColor = new Color(0.02f, 0.03f, 0.12f, 0.55f);
                    sh.effectDistance = new Vector2(0f, -w * 2.2f);
                }
            }
            return t;
        }

        /// Card heading with a short accent rule underneath, so every screen shares one title style.
        public static Text Header(Transform parent, string title, float y, Color accent, int size = 78, float width = 940)
        {
            var label = Label(parent, title, size, new Vector2(0, y), new Vector2(width, 112), Cream, TextAnchor.MiddleCenter, true);
            // Label caps big text at 64 for body copy; headings opt out so they stay prominent.
            label.fontSize = size;
            label.resizeTextMaxSize = size;
            label.resizeTextMinSize = Mathf.Max(30, Mathf.RoundToInt(size * .6f));
            var rule = Panel(parent, "Header accent", new Vector2(0, y - Mathf.RoundToInt(size * .78f)), new Vector2(190, 7), accent);
            rule.raycastTarget = false;
            return label;
        }

        /// Close button in the corner of a card.
        public static Button Close(Transform parent, Vector2 pos, Action onClose)
        {
            return Button(parent, "X", pos, new Vector2(115, 115), Red, () => onClose?.Invoke(), 60);
        }

        public static Button Button(Transform parent, string text, Vector2 pos, Vector2 size, Color c, UnityAction onClick, int fontSize = 56, string icon = null)
        {
            bool close = text == "X";
            if (close) { c = Surface; text = "×"; fontSize = 38; }
            else if (c == Green) c = new Color(.035f, .25f, .22f);
            else if (c == Red) c = new Color(.32f, .045f, .13f);
            else if (c == Cyan) c = new Color(.035f, .24f, .3f);
            fontSize = Mathf.Min(fontSize, 44);
            var img = Panel(parent, text + " Button", pos, size, c);
            Shadow(img.transform, size, 2f);
            // Ink rim, then a darker lip under the face for a chunky, pressable toy look.
            var rim = Panel(img.transform, "Rim", Vector2.zero, size + new Vector2(2, 2), new Color(.3f,.4f,.55f,.35f));
            rim.raycastTarget = false;
            var face = Panel(img.transform, "Face", Vector2.zero, size, c);
            face.sprite = Rounded;
            face.raycastTarget = false;
            // A soft gloss band along the top gives every button the same lit, toy-like surface.
            var gloss = Panel(face.transform, "Gloss", new Vector2(0f, size.y * .24f), new Vector2(size.x * .84f, Mathf.Max(6f, size.y * .22f)), new Color(1f, 1f, 1f, .1f));
            gloss.raycastTarget = false;
            // Gloss band across the top half.
            img.color = new Color(0, 0, 0, 0); // the root only catches taps
            img.gameObject.AddComponent<ButtonBounce>();
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = face;
            var colors = b.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f);
            colors.disabledColor = new Color(.45f, .5f, .6f, .55f);
            colors.fadeDuration = .1f;
            b.colors = colors;
            b.onClick.AddListener(onClick);
            if (close)
            {
                foreach (float angle in new[] { -45f, 45f })
                {
                    var stroke = Panel(face.transform, "Close stroke", Vector2.zero, new Vector2(30, 3), Color.white);
                    stroke.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
                    stroke.raycastTarget = false;
                }
            }
            else if (icon != null)
            {
                // Icon alone is centred; with text it sits to the left of the label.
                bool alone = string.IsNullOrEmpty(text);
                float isz = size.y * 0.44f;
                float ix = alone ? 0f : -size.x * 0.5f + isz * 0.5f + 28f;
                var well = Panel(face.transform, "Icon well", new Vector2(ix, 4f), new Vector2(isz * 1.18f, isz * 1.18f), new Color(0f, 0f, 0f, .18f));
                well.raycastTarget = false;
                Icon(face.transform, icon, new Vector2(ix, 4f), isz);
                if (!alone) Label(face.transform, text, fontSize, new Vector2(isz * .5f + 10f, 0), new Vector2(size.x - isz - 60, size.y - 12), Color.white);
            }
            else Label(face.transform, text, fontSize, Vector2.zero, size - new Vector2(24, 12), Color.white);
            img.gameObject.AddComponent<ButtonVisualState>();
            return b;
        }

        public static Slider Slider(Transform parent, Vector2 pos, float width, float value, Action<float> onChange)
        {
            var root = Centered(parent, "Slider", pos, new Vector2(width, 88));
            var bg = Panel(root, "Track", Vector2.zero, new Vector2(width, 8), new Color(1, 1, 1, .18f));
            bg.rectTransform.anchorMin = new Vector2(0, 0.5f);
            bg.rectTransform.anchorMax = new Vector2(1, 0.5f);
            bg.rectTransform.sizeDelta = new Vector2(0, 8);

            var fillArea = Rect(root, "Fill Area", new Vector2(0, .5f), new Vector2(1, .5f), Vector2.zero, new Vector2(-44, 8));
            var fill = Panel(fillArea, "Fill", Vector2.zero, Vector2.zero, Cyan);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.sizeDelta = new Vector2(44, 0);

            var handleArea = Rect(root, "Handle Area", new Vector2(0, .5f), new Vector2(1, .5f), Vector2.zero, new Vector2(-44, 48));
            var handle = Panel(handleArea, "Handle", Vector2.zero, new Vector2(44, 0), Cream);
            handle.rectTransform.anchorMin = handle.rectTransform.anchorMax = new Vector2(0, 0.5f);

            var s = root.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.minValue = 0;
            s.maxValue = 1;
            s.value = value;
            s.onValueChanged.AddListener(v => onChange(v));
            return s;
        }
    }
}
