using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    // Nine reusable lights illuminate the nearby course; streamed segments do not create lights.
    // Emissive distant fixtures/windows remain cheap. Moonlight supplies the shadow-casting key.
    public sealed class NightLighting : MonoBehaviour
    {
        static NightLighting instance;
        readonly Light[] lights = new Light[9];
        public static void Ensure()
        {
            if (instance) return;
            instance = new GameObject("Night / practical light pool").AddComponent<NightLighting>();
        }
        void Awake()
        {
            for (int i=0;i<lights.Length;i++) {
                var go = new GameObject(i<6 ? "Street downlight" : i<8 ? "Advertising light spill" : "Soft sky bounce at rider");
                go.transform.SetParent(transform,false);
                var lamp = go.AddComponent<Light>(); lights[i] = lamp;
                lamp.type = i<6 ? LightType.Spot : LightType.Point;
                lamp.range = i<6 ? 19f : i<8 ? 16f : 10f;
                lamp.intensity = i<6 ? 28f : i<8 ? 12f : 1.4f;
                lamp.color = i<6 ? new Color(1f,.75f,.49f) : i==6 ? new Color(.12f,.65f,1f) : i==7 ? new Color(1f,.12f,.45f) : new Color(.65f,.76f,1f);
                lamp.spotAngle = 112f; lamp.innerSpotAngle = 68f;
                lamp.shadows = LightShadows.None;
                lamp.renderMode = LightRenderMode.ForcePixel;
                go.transform.rotation = Quaternion.Euler(90,0,0);
            }
        }
        void LateUpdate()
        {
            Camera view = null;
            foreach (var candidate in Camera.allCameras)
                if (candidate.enabled && (!view || candidate.depth > view.depth)) view = candidate;
            if (!view) return;
            Vector3 at = view.transform.position;
            bool city = at.x > 1000;
            // The pool rides up with the camera, so the rooftops and facades are lit like the street.
            float x = city ? 5000f : 0f, y = city ? 100f : Mathf.Max(0f, at.y - 4.2f);
            if (!city && at.x > SurfaceTraversal.FacadeX) x = SurfaceTraversal.RoofX;
            float z = Mathf.Floor(at.z/20f)*20f;
            for(int i=0;i<6;i++) {
                int row=i/2; float side=i%2==0 ? -1 : 1;
                lights[i].transform.position = new Vector3(x+side*(city ? 8.5f : 4.3f),y+5.05f,z+4+row*20);
                lights[i].enabled = PlayerProfile.Graphics>0 || i<2;
            }
            for(int i=6;i<8;i++) {
                lights[i].transform.position = new Vector3(x+(i==6 ? -6.8f : 6.8f),y+4f,z+18f);
                lights[i].enabled = PlayerProfile.Graphics>0;
                if (DashRun.District==2) lights[i].color = i==6 ? new Color(1f,.42f,.12f) : new Color(.3f,.6f,1f);
                else lights[i].color = i==6 ? new Color(.12f,.65f,1f) : new Color(1f,.12f,.45f);
            }
            // Close to the camera and above it: 6 m out it sat in Ace's face on the title and washed his hair blue-white.
            lights[8].transform.position = at+view.transform.forward*2f+Vector3.up*2.5f;
        }
    }
}
