using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    public partial class TrackSpawner
    {
        public GameObject skyparkTower;
        public const float CityGround = -40f;
        public static bool SkyparkVoid(float z) => DashRun.District == 1 && z >= 100f && Mathf.Repeat(z - 100f, 80f) < 5f;

        // The course is the roof of an occupied forty-metre tower. There is no street surface here.
        void SkyparkRoof(Transform parent, float z)
        {
            float end = z + segmentLength, at = z;
            var metal = DashCity.Mat(this, "roofMetal", new Color(.11f,.18f,.24f), .02f);
            var cyan = DashCity.Mat(this, "roofCoping", new Color(.08f,.85f,.85f), .6f);
            var garden = DashCity.Mat(this, "roofGarden", new Color(.14f,.34f,.22f));
            var stone = DashCity.Mat(this, "roofStone", new Color(.24f,.32f,.38f));
            while (at < end)
            {
                bool voidNow = SkyparkVoid(at + .01f);
                float edge = at + 1f;
                while (edge < end && SkyparkVoid(edge + .01f) == voidNow) edge += 1f;
                edge = Mathf.Min(edge, end);
                if (!voidNow)
                {
                    if (skyparkTower)
                    {
                        var tower = Instantiate(skyparkTower, new Vector3(0f, CityGround, (at + edge) * .5f), Quaternion.identity, parent);
                        tower.transform.localScale = new Vector3(1f, 1f, (edge - at) / 20f);
                        tower.name = "Playable high-rise roof / 40m above city";
                    }
                    else DashCity.Part(parent, new Vector3(0f, CityGround * .5f, (at + edge) * .5f), new Vector3(8f, -CityGround, edge-at), stone);
                    foreach (float side in new[] { -1f, 1f })
                    {
                        bool opening = side > 0f && (DashRun.RouteOpen(at) || DashRun.RouteOpen(edge));
                        if (!opening)
                        {
                            DashCity.Part(parent, new Vector3(side*4.05f,.43f,(at+edge)*.5f), new Vector3(.12f,.85f,edge-at), metal);
                            DashCity.Part(parent, new Vector3(side*4.05f,.88f,(at+edge)*.5f), new Vector3(.15f,.045f,edge-at), cyan);
                            if (edge-at > 8f)
                            {
                                DashCity.Part(parent,new Vector3(side*3.45f,.22f,at+4f),new Vector3(.65f,.44f,2.4f),stone);
                                DashCity.Part(parent,new Vector3(side*3.45f,.57f,at+4f),new Vector3(.55f,.3f,2.2f),garden);
                                if (kitTrees!=null&&kitTrees.Length>0)
                                {
                                    var tree=Instantiate(kitTrees[Mathf.Abs(Mathf.RoundToInt(at/20f))%kitTrees.Length],new Vector3(side*3.45f,.44f,at+4f),Quaternion.identity,parent);
                                    tree.transform.localScale=Vector3.one*.42f;
                                    tree.name="Rooftop terrace tree";
                                }
                            }
                        }
                    }
                    // Roof joints, skylights and edge beacons replace road markings and traffic signs.
                    for (float seam=at+4f; seam<edge; seam+=4f)
                        DashCity.Part(parent,new Vector3(0f,.009f,seam),new Vector3(7.7f,.012f,.025f),metal);
                    if (SkyparkVoid(edge+.05f))
                        DashCity.Part(parent,new Vector3(0f,.025f,edge-.12f),new Vector3(7.8f,.05f,.2f),cyan);
                }
                at = edge;
            }
            // Explicit void volumes use the same definition as the missing visual roof.
            for (float g=100f+Mathf.Max(0,Mathf.FloorToInt((z-100f)/80f))*80f;g<end;g+=80f)
                if (g>=z)
                    for (int lane=0;lane<Tuning.Lanes;lane++)
                    {
                        var gap=new GameObject("High-rise roof gap / jump");gap.transform.SetParent(transform);
                        gap.transform.position=new Vector3(Tuning.LaneX[lane],0f,g+2.5f);
                        Register(gap,TrackKind.Gap,lane,2.5f,0,0f);
                    }
        }

        void SkyparkBackdrop(Transform parent,float z)
        {
            var city = DashCity.Mat(this,"cityBelow",new Color(.035f,.055f,.09f));
            DashCity.Part(parent,new Vector3(0f,CityGround-.2f,z+10f),new Vector3(160f,.2f,segmentLength),city);
            if (dashBuildings == null || dashBuildings.Length == 0) return;
            foreach (float side in new[] {-1f,1f})
            {
                int block=Mathf.Abs(Mathf.FloorToInt(z/20f));
                if (block%2!=0) continue;
                var tower=Instantiate(dashBuildings[block%dashBuildings.Length],new Vector3(side*(22f+block%3*5f),CityGround,z+10f),Quaternion.Euler(0f,side>0f?-90f:90f,0f),parent);
                tower.transform.localScale=new Vector3(.8f+(block%3)*.12f,1.7f+(block%4)*.32f,.8f+(block%3)*.12f);
                tower.name="City skyline below rooftop course";
            }
        }

        float SpawnSkyparkRow(float z)
        {
            if (DashRun.WallZone(z)||DashRun.GravityZone(z)) { SpawnTraversalRow(z); return 0f; }
            float gapPhase = z < 80f ? 40f : Mathf.Repeat(z-100f,80f);
            if (gapPhase<15f||gapPhase>65f) return 0f;
            int open=Mathf.Clamp(lastOpen+Random.Range(-1,2),0,Tuning.Lanes-1);lastOpen=open;
            bool token = z >= nextPowerUp;
            if (token) nextPowerUp = z + Random.Range(PowerUpGap*.75f,PowerUpGap*1.25f);
            for(int lane=0;lane<Tuning.Lanes;lane++)
            {
                if (token && lane==open) { SpawnPowerUp((PowerUp)Random.Range(0,PowerUps.Count),lane,z);continue; }
                if (lane==open||Random.value<.4f) { SpawnPickupLine(lane,z);continue; }
                float x=Tuning.LaneX[lane];
                var root=new GameObject("Roof ventilation unit / jump");root.transform.SetParent(transform);root.transform.position=new Vector3(x,0,z);
                var shell=DashCity.Mat(this,"roofVentShell",new Color(.2f,.28f,.34f));
                var slat=DashCity.Mat(this,"roofVentSlat",new Color(.055f,.09f,.13f));
                DashCity.Part(root.transform,new Vector3(x,.36f,z),new Vector3(1.35f,.72f,1.5f),shell);
                for(int k=0;k<7;k++) DashCity.Part(root.transform,new Vector3(x,.725f,z-.6f+k*.2f),new Vector3(1.1f,.025f,.075f),slat);
                Register(root,TrackKind.Low,lane,.75f,0,0f);var o=live[live.Count-1];o.height=.74f;o.halfWidth=.675f;
            }
            return 0f;
        }
    }
}
