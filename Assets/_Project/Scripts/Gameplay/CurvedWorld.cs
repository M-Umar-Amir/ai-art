using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Drives the curved-world bend in FoodInc/ToonCurved: the street drops away over the horizon and
    /// sways gently left and right as the run goes on. Visual only; hit tests stay on the straight track.
    [ExecuteAlways]
    public class CurvedWorld : MonoBehaviour
    {
        public float down = 0.0009f;
        public float sway = 0.0007f;
        /// Distance (m) for one full left-right-left sway.
        public float swayPeriod = 900f;

        static readonly int CurveParams = Shader.PropertyToID("_CurveParams");

        void Update()
        {
            float d = Application.isPlaying && GameManager.Instance ? GameManager.Instance.Distance : 0f;
            float side = Mathf.Sin(d / swayPeriod * Mathf.PI * 2f) * sway;
            Shader.SetGlobalVector(CurveParams, new Vector4(down, side, 0f, 0f));
        }
    }
}
