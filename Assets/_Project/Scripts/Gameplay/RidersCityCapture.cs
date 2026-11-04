using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace DallasDash.Gameplay
{
    public sealed class RidersCityCapture : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-riders-city-capture");
            if (index < 0 || index + 1 >= args.Length) return;
            var go = new GameObject("City review capture");
            go.AddComponent<RidersCityCapture>().StartCoroutine(Capture(args[index + 1]));
        }
        static IEnumerator Capture(string path)
        {
            yield return new WaitForSecondsRealtime(8);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForSecondsRealtime(3);
            Debug.Log("RIDERS_CITY_CAPTURE " + path);
            Application.Quit();
        }
    }
}
