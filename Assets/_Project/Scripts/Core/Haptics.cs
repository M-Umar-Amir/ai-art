using System.Runtime.InteropServices;
using UnityEngine;

namespace DallasDash.Core
{
    /// Short vibration taps: light on burger pickups and near misses, medium on a hit, heavy when busted.
    /// Unity's Handheld.Vibrate is a fixed half-second buzz, far too long for a pickup, so phones get native
    /// one-shots instead: Android's VibrationEffect with an amplitude (API 26+, our minimum), iOS's
    /// UIImpactFeedbackGenerator through Plugins/iOS/FoodIncHaptics.mm. Off in the editor and on desktop.
    /// The settings menu turns it off (PlayerProfile.Haptics).
    public static class Haptics
    {
        public enum Strength { Light, Medium, Heavy }

        /// Pickups come several a second; closer taps than this blur into one buzz, so they're dropped.
        const float MinGap = 0.07f;
        static float lastAt = -1f;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void FoodIncHapticImpact(int style);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static AndroidJavaClass effect;
        static bool androidReady, androidFailed;

        static bool InitAndroid()
        {
            if (androidReady) return true;
            if (androidFailed) return false;
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                effect = new AndroidJavaClass("android.os.VibrationEffect");
                androidReady = vibrator != null && vibrator.Call<bool>("hasVibrator");
                androidFailed = !androidReady;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[DallasDash] Haptics unavailable: " + e.Message);
                androidFailed = true;
            }
            return androidReady;
        }
#endif

        public static void Play(Strength s)
        {
            if (!PlayerProfile.Haptics || !Application.isMobilePlatform) return;
            float t = Time.unscaledTime;
            if (s == Strength.Light && t - lastAt < MinGap) return;
            lastAt = t;
#if UNITY_IOS && !UNITY_EDITOR
            FoodIncHapticImpact((int)s);
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (!InitAndroid()) return;
            long ms = s == Strength.Light ? 12 : s == Strength.Medium ? 35 : 70;
            int amplitude = s == Strength.Light ? 60 : s == Strength.Medium ? 160 : 255;
            try
            {
                using var oneShot = effect.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude);
                vibrator.Call("vibrate", oneShot);
            }
            catch (System.Exception) { }
#endif
        }

        /// Never called. Referencing Handheld.Vibrate is what makes Unity add the VIBRATE permission to
        /// the Android manifest, which the native one-shots above also need.
#if UNITY_ANDROID || UNITY_IOS
        static void KeepVibratePermission() => Handheld.Vibrate();
#endif
    }
}
