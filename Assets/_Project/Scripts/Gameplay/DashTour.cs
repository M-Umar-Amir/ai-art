using System;
using System.Collections;
using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Visible automation of the Riders City free roam: rides to each destination with normal input.
    public sealed class DashTour : MonoBehaviour
    {
        public static bool Active { get; private set; } = Array.Exists(Environment.GetCommandLineArgs(), a => a == "-dash-tour" || a == "-riders-city-tour");
        public static Vector2 Input { get; private set; }
        public static string Caption { get; private set; } = "";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!Array.Exists(Environment.GetCommandLineArgs(), a => a == "-dash-tour" || a == "-riders-city-tour")) return;
            Active = true;
            var go = new GameObject("Riders City live bot");
            DontDestroyOnLoad(go); go.AddComponent<DashTour>();
            Application.runInBackground = true;
        }
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(2);
            var world = FindFirstObjectByType<RidersCityWorld>();
            if (!world) { Debug.LogError("CITY_BOT_FAIL: world unavailable"); yield break; }
            Debug.Log("CITY_BOT_STARTED version=RidersCity hub");
            while (true)
            {
                Caption = "CITY BOT / Exploring Riders City";
                for (int i = 0; i < RidersCityWorld.Destinations.Length; i++)
                {
                    Caption = "CITY BOT / " + RidersCityWorld.DestinationNames[i];
                    float until = Time.realtimeSinceStartup + 18;
                    while (Time.realtimeSinceStartup < until)
                    {
                        Vector3 delta = RidersCityWorld.Destinations[i] - world.Position;
                        if (delta.magnitude < 2) break;
                        Input = new Vector2(delta.x, delta.z).normalized;
                        yield return null;
                    }
                    Input = Vector2.zero;
                    Debug.Log("CITY_BOT_DESTINATION " + RidersCityWorld.DestinationNames[i]);
                    yield return new WaitForSecondsRealtime(2);
                }
                yield return new WaitForSecondsRealtime(2);
            }
        }
        void OnDestroy() { Active = false; Input = Vector2.zero; Caption = ""; }
    }
}
