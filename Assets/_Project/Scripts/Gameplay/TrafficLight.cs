using UnityEngine;

namespace DallasDash.Gameplay
{
    /// The junction light the rider runs in scene 14. Lamps are lit by emission through a property block.
    public class TrafficLight : MonoBehaviour
    {
        public enum Phase { Green, Amber, Red }

        /// Each lamp has a lens on both faces of the housing, so the light reads from either side of the junction.
        public Renderer[] red, amber, green;

        static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        static readonly int Base = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock mpb;

        public void Set(Phase p)
        {
            Lamp(red, p == Phase.Red, new Color(1f, 0.15f, 0.1f));
            Lamp(amber, p == Phase.Amber, new Color(1f, 0.7f, 0.1f));
            Lamp(green, p == Phase.Green, new Color(0.2f, 1f, 0.4f));
        }

        void Lamp(Renderer[] lenses, bool on, Color c)
        {
            if (lenses == null) return;
            mpb ??= new MaterialPropertyBlock();
            mpb.SetColor(Base, on ? c : c * 0.25f);
            mpb.SetColor(Emission, on ? c * 3f : Color.black);
            foreach (var r in lenses) if (r) r.SetPropertyBlock(mpb);
        }
    }
}
