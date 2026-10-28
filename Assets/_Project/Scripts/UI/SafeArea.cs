using UnityEngine;

namespace DallasDash.UI
{
    /// Keeps a UI root inside the screen's safe area, clear of notches, punch-hole cameras, rounded corners and
    /// the home indicator. Re-fits whenever the safe area or resolution changes (rotation, split screen).
    /// `-safe top,bottom,left,right` (pixels) fakes a phone's insets in desktop review builds, so layouts can be
    /// checked at real phone resolutions without the phone.
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        static Vector4? simulated;
        static bool parsed;

        Rect last;
        Vector2Int lastSize;

        /// The usable rectangle in screen pixels.
        public static Rect Current
        {
            get
            {
                if (!parsed)
                {
                    parsed = true;
                    var args = System.Environment.GetCommandLineArgs();
                    for (int i = 0; i < args.Length - 1; i++)
                    {
                        if (args[i] != "-safe") continue;
                        var p = args[i + 1].Split(',');
                        if (p.Length == 4 && float.TryParse(p[0], out var t) && float.TryParse(p[1], out var b)
                            && float.TryParse(p[2], out var l) && float.TryParse(p[3], out var r))
                            simulated = new Vector4(t, b, l, r);
                    }
                }
                if (simulated is { } s)
                    return new Rect(s.z, s.y, Screen.width - s.z - s.w, Screen.height - s.x - s.y);
                return Screen.safeArea;
            }
        }

        /// Wraps everything under `canvasRoot` that should stay inside the safe area in a fitted child.
        public static RectTransform Create(Transform canvasRoot)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeArea));
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvasRoot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<SafeArea>().Fit();
            return rt;
        }

        void Update()
        {
            if (Current != last || Screen.width != lastSize.x || Screen.height != lastSize.y) Fit();
        }

        void Fit()
        {
            var r = Current;
            last = r;
            lastSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            // The layouts are drawn for 1080 x 1920. Taller phones (19.5:9, 20:9) scale to width so nothing spills
            // off the sides; wider screens (tablets, 4:3) scale to height so nothing spills off the bottom.
            var scaler = GetComponentInParent<UnityEngine.UI.CanvasScaler>();
            if (scaler) scaler.matchWidthOrHeight = Screen.height * 1080f > Screen.width * 1920f ? 0f : 1f;
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(r.xMin / Screen.width, r.yMin / Screen.height);
            rt.anchorMax = new Vector2(r.xMax / Screen.width, r.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
