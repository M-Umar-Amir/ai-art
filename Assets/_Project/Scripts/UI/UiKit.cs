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

        // Pink identity, dark city chrome and distinct reward/action colours.
        public static readonly Color Blue = new(0.055f, 0.085f, 0.14f);
        public static readonly Color Navy = new(0.025f, 0.04f, 0.12f, 0.96f);
        public static readonly Color Cream = new(1f, 0.97f, 0.9f);
        public static readonly Color Yellow = new(1f, 0.76f, 0.18f);
        public static readonly Color Red = new(1f, 0.16f, 0.28f);
        public static readonly Color Green = new(0.035f, 0.58f, 0.63f);
        public static readonly Color Pink = new(0.96f, 0.015f, 0.5f);
        public static readonly Color Ink = new(0.1f, 0.12f, 0.3f);

        static Sprite rounded, gradient, soft;
        public static Sprite Rounded => rounded ? rounded : rounded = MakeRounded(false);
        /// Same shape, lit from above: white at the top shading to grey at the bottom, so a tint becomes a gradient.
        public static Sprite Gradient => gradient ? gradient : gradient = MakeRounded(true);
        /// Blurred rounded shape for drop shadows.
        public static Sprite Soft => soft ? soft : soft = MakeSoft();

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
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.03f, 0.05f, 0.15f, 0.55f);
            return img;
        }

        /// A card that pops in whenever its screen opens.
        public static Image Card(Transform parent, Vector2 pos, Vector2 size)
        {
            // The root is invisible (it only blocks taps); children draw in order: shadow, light rim,
            // gradient face, sheen. Content added later lands on top.
            var img = Panel(parent, "Card", pos, size, new Color(0, 0, 0, 0));
            Shadow(img.transform, size, 14f);
            Panel(img.transform, "Rim", Vector2.zero, size, new Color(0.35f, 0.44f, 0.6f, 0.8f)).raycastTarget = false;
            var face = Panel(img.transform, "Face", Vector2.zero, size - new Vector2(5f, 5f), new Color(0.035f, 0.06f, 0.12f, 0.98f));
            face.sprite = Rounded;
            face.raycastTarget = false;
            var sheen = Panel(img.transform, "Sheen", new Vector2(0f, size.y * 0.5f - 60f), new Vector2(size.x - 60f, 70f), new Color(1f, 1f, 1f, 0.025f));
            sheen.raycastTarget = false;
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
            var bg = Panel(parent, icon + " Pill", pos, new Vector2(width, 100f), new Color(0.05f, 0.08f, 0.25f, 0.72f));
            bg.raycastTarget = false;
            root = bg.rectTransform;
            Icon(bg.transform, icon, new Vector2(-width * 0.5f + 48f, 2f), 58f);
            return Label(bg.transform, "", 48, new Vector2(30f, 2f), new Vector2(width - 110f, 90f), textColor);
        }

        public static Text Label(Transform parent, string text, int size, Vector2 pos, Vector2 box, Color c, TextAnchor anchor = TextAnchor.MiddleCenter, bool outline = false)
        {
            var rt = Centered(parent, "Label", pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.text = text;
            t.color = c;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
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

        public static Button Button(Transform parent, string text, Vector2 pos, Vector2 size, Color c, UnityAction onClick, int fontSize = 56, string icon = null)
        {
            var img = Panel(parent, text + " Button", pos, size, c);
            Shadow(img.transform, size, 3f);
            // Ink rim, then a darker lip under the face for a chunky, pressable toy look.
            var rim = Panel(img.transform, "Rim", Vector2.zero, size + new Vector2(2, 2), new Color(.3f,.4f,.55f,.35f));
            rim.raycastTarget = false;
            var face = Panel(img.transform, "Face", Vector2.zero, size, c);
            face.sprite = Rounded;
            face.raycastTarget = false;
            // Gloss band across the top half.
            img.color = new Color(0, 0, 0, 0); // the root only catches taps
            img.gameObject.AddComponent<ButtonBounce>();
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = face;
            var colors = b.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f);
            b.colors = colors;
            b.onClick.AddListener(onClick);
            if (icon != null)
            {
                // Icon alone is centred; with text it sits to the left of the label.
                bool alone = string.IsNullOrEmpty(text);
                float isz = size.y * 0.44f;
                float ix = alone ? 0f : -size.x * 0.5f + isz * 0.5f + 28f;
                Icon(face.transform, icon, new Vector2(ix, 4f), isz);
                if (!alone) Label(face.transform, text, fontSize, new Vector2(isz * 0.5f + 10f, 4f), size, Color.white);
            }
            else Label(face.transform, text, fontSize, new Vector2(0, 4), size, Color.white);
            return b;
        }

        public static Slider Slider(Transform parent, Vector2 pos, float width, float value, Action<float> onChange)
        {
            var root = Centered(parent, "Slider", pos, new Vector2(width, 60));
            var bg = Panel(root, "Track", Vector2.zero, new Vector2(width, 26), new Color(1, 1, 1, 0.25f));
            bg.rectTransform.anchorMin = new Vector2(0, 0.5f);
            bg.rectTransform.anchorMax = new Vector2(1, 0.5f);
            bg.rectTransform.sizeDelta = new Vector2(0, 26);

            var fillArea = Rect(root, "Fill Area", new Vector2(0, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(-30, 26));
            var fill = Panel(fillArea, "Fill", Vector2.zero, Vector2.zero, Yellow);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.sizeDelta = new Vector2(30, 0);

            var handleArea = Rect(root, "Handle Area", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-60, 0));
            var handle = Panel(handleArea, "Handle", Vector2.zero, new Vector2(64, 64), Cream);
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
