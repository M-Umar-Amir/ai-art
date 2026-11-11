using UnityEngine;
using DallasDash.Core;

namespace DallasDash.Gameplay
{
    // Metre-scale street blocks, combined by TrackSpawner after construction.
    // Windows are recessed behind stone piers; rooflines vary deterministically by block.
    public static class NaturalArchitecture
    {
        public static void Build(TrackSpawner track, Transform parent, float side, float z, bool route)
        {
            int block = Mathf.FloorToInt(z / 20f);
            int variant = Mathf.Abs(block * 7 + (side > 0 ? 3 : 0)) % 5;
            int district = DashRun.District;
            // Skypark neighbours rise at least 15 m above the course roof, so there is always a wall to run.
            int floors = district == 1 ? 17 + variant : 4 + variant;
            float baseY = 0f;
            float height = floors * 3.2f + 3.6f;
            // One street line for every block: the wall-run plane (SurfaceTraversal.WallRunX) relies on it.
            float front = side * (route ? 10.8f : 8.5f);
            Color[] stoneColors = { new(.82f,.77f,.70f), new(.95f,.90f,.80f), new(.60f,.68f,.76f), new(.82f,.67f,.56f), new(.85f,.87f,.86f) };
            var stone = DashCity.Mat(track,"masonry-"+variant,stoneColors[variant]);
            if (track.masonryTexture) {
                stone.SetTexture("_BaseMap",track.masonryTexture); stone.SetFloat("_WorldTextureScale",.5f);
                stone.SetTexture("_BumpMap",track.masonryNormal); stone.SetFloat("_BumpScale",.7f);
                stone.SetTexture("_RoughnessMap",track.masonryRoughness); stone.SetFloat("_RoughnessStrength",1);
            }
            stone.SetFloat("_Smoothness",.12f); stone.SetFloat("_Metallic",0);
            var trim = DashCity.Mat(track,"limestone-trim",new Color(.59f,.56f,.49f));
            var frame = DashCity.Mat(track,"window-bronze",new Color(.075f,.085f,.09f));
            var glass = DashCity.Mat(track,"architectural-glass",new Color(.085f,.18f,.23f));
            glass.SetFloat("_Smoothness",.92f); glass.SetFloat("_Metallic",.62f);
            var warmGlass = DashCity.Mat(track,"occupied-window",new Color(1f,.65f,.3f),1.1f);
            var coolGlass = DashCity.Mat(track,"office-window",new Color(.35f,.65f,1f),.65f);
            warmGlass.SetFloat("_WindowInterior",1); coolGlass.SetFloat("_WindowInterior",1);
            var canopy = DashCity.Mat(track,"canvas-"+variant,variant%2==0 ? new Color(.22f,.26f,.18f) : new Color(.29f,.13f,.075f));
            // One ordered rotation along the street (client, 4 Nov 2026): KFC, Taco Bell, Pizza Hut, The Habit, again,
            // left then right on each block, so every brand gets the same number of signs, in sequence.
            int brandIndex = TrackSpawner.BrandAt(block * 2 + (side > 0 ? 1 : 0));
            var brand = TrackSpawner.YumBrands[brandIndex];
            if (district == 0) canopy = DashCity.Mat(track, "brandCanvas-" + brandIndex, brand.board * .8f);
            void Box(float x,float y,float pz,float sx,float sy,float sz,Material mat) =>
                DashCity.Part(parent,new Vector3(x,y,pz),new Vector3(sx,sy,sz),mat);
            // A solid inner volume provides honest shadowing behind the recessed glazing.
            Box(front+side*5.1f,baseY+height*.5f,z+10,10,height,18.8f,stone);
            float face = front-side*.04f;
            for(int floor=0;floor<floors;floor++) {
                float y=baseY+4.8f+floor*3.2f;
                Box(face-side*.09f,y-1.44f,z+10,.32f,.18f,19.1f,trim);
                for(int bay=0;bay<5;bay++) {
                    float wz=z+2.55f+bay*3.7f;
                    int occupancy=(floor*11+bay*7+variant)%7;
                    var pane=occupancy<2 ? warmGlass : occupancy==2 ? coolGlass : glass;
                    Box(face-side*.12f,y,wz,.09f,2.25f,2.45f,pane);
                    Box(face-side*.22f,y,wz,.10f,2.3f,.045f,frame);
                    Box(face-side*.22f,y,wz,.10f,.045f,2.5f,frame);
                    Box(face-side*.25f,y-1.18f,wz,.42f,.13f,2.7f,trim);
                }
            }
            // Street-level shop bays have deep overhangs and substantial end piers.
            for(int bay=0;bay<3;bay++) {
                float wz=z+4f+bay*5.8f;
                Box(face-side*.13f,baseY+1.6f,wz,.10f,2.7f,4.7f,bay%2==0 ? warmGlass : coolGlass);
                Box(face-side*.32f,baseY+1.6f,wz-2.48f,.5f,3.2f,.3f,trim);
                Box(face-side*.82f,baseY+3.05f,wz,1.75f,.15f,5.15f,canopy);
                Box(face-side*1.62f,baseY+2.86f,wz,.08f,.28f,5.15f,canopy);
            }
            Box(face-side*.16f,baseY+.23f,z+10,.5f,.46f,19.1f,trim);
            BrandShopfront(track, parent, side, z, face, brandIndex, district == 1 ? 4f : 0f);
            Box(front+side*4.9f,baseY+height+.12f,z+10,10.5f,.24f,19.35f,trim);
            // Festivals (snowy Christmas): snow lies on every roof, awning and window ledge.
            if (district == 2) {
                var snow = DashCity.Mat(track,"xmasSnow",new Color(.93f,.95f,1f),.05f);
                Box(front+side*4.9f,baseY+height+.33f,z+10,10.7f,.2f,19.5f,snow);
                for(int bay=0;bay<3;bay++) Box(face-side*.82f,baseY+3.17f,z+4f+bay*5.8f,1.8f,.1f,5.2f,snow);
                for(int floor=0;floor<floors;floor++) Box(face-side*.25f,baseY+4.8f+floor*3.2f-1.08f,z+10,.44f,.06f,19f,snow);
            }
            Box(face-side*.12f,baseY+height+.52f,z+10,.35f,.7f,19.2f,stone);
            // Chimneys and rooftop plant create a recognisable, varied silhouette.
            Box(front+side*3f,baseY+height+.8f,z+5,1.1f,1.6f,1.2f,stone);
            Box(front+side*5f,baseY+height+.55f,z+13,2.4f,1.1f,3,frame);
            FutureLayers(track,parent,side,z,front,baseY,height,variant);
        }

        /// Yum! Brands shopfront (client, 4 Nov 2026: official logos, not text): lit logo panels along the fascia
        /// above the shop bays, and a square blade sign standing out from the facade, facing down the street, so the
        /// logo reads from the chase camera long before the rider passes it.
        static void BrandShopfront(TrackSpawner track, Transform parent, float side, float z, float face, int brandIndex, float lift)
        {
            var frame = DashCity.Mat(track, "brandFrame", new Color(.06f, .065f, .08f));
            if (lift <= 0f)
            {
                DashCity.Part(parent, new Vector3(face - side * .2f, 3.75f, z + 10f), new Vector3(.16f, .9f, 15f), frame);
                var along = Quaternion.Euler(0f, side > 0 ? 90f : -90f, 0f);
                foreach (float wz in new[] { z + 4.5f, z + 10f, z + 15.5f })
                    BrandVisibility.Face(track, parent, brandIndex, "wide", new Vector3(face - side * .3f, 3.75f, wz), along, 2.5f, .78f);
            }
            float bx = face - side * 1.45f, by = 6.3f + lift, bz = z + 2.2f;
            DashCity.Part(parent, new Vector3(face - side * .45f, by + 1.3f, bz), new Vector3(1.1f, .14f, .14f), frame);
            DashCity.Part(parent, new Vector3(bx, by, bz), new Vector3(2.3f, 2.3f, .2f), frame);
            BrandVisibility.Face(track, parent, brandIndex, "square", new Vector3(bx, by, bz - .105f), Quaternion.identity, 2.18f, 2.18f);
            BrandVisibility.Face(track, parent, brandIndex, "square", new Vector3(bx, by, bz + .105f), Quaternion.Euler(0f, 180f, 0f), 2.18f, 2.18f);
        }

        static void FutureLayers(TrackSpawner track,Transform parent,float side,float z,float front,float baseY,float height,int variant)
        {
            int block=Mathf.Abs(Mathf.FloorToInt(z/20f));
            var graphite=DashCity.Mat(track,"terraceGraphite",new Color(.045f,.06f,.08f));
            var cyan=DashCity.Mat(track,"architecturalCyan",new Color(.05f,.55f,.9f),2f);
            var magenta=DashCity.Mat(track,"architecturalMagenta",new Color(.8f,.05f,.3f),1.4f);
            float terraceY=DashRun.District==1 ? 3.2f : 7f+variant*.8f;
            DashCity.Part(parent,new Vector3(front-side*.85f,terraceY,z+11),new Vector3(2.1f,.3f,12f),graphite);
            DashCity.Part(parent,new Vector3(front-side*1.86f,terraceY+.9f,z+11),new Vector3(.065f,.06f,12f),cyan);
            for(int i=0;i<5;i++)
                DashCity.Part(parent,new Vector3(front-side*1.86f,terraceY+.45f,z+6+i*2.5f),new Vector3(.055f,.9f,.055f),graphite);
            if(track.dashPlanter) {
                var planter=Object.Instantiate(track.dashPlanter,new Vector3(front-side*.7f,terraceY+.16f,z+7),Quaternion.identity,parent);
                planter.name="Elevated planted terrace";
            }
            // Illuminated advertising is mounted on architecture, with brackets and a deep housing.
            if(block%2!=(side>0?1:0)) return;
            int index=TrackSpawner.BrandAt(Mathf.FloorToInt(z/20f)); // one billboard per block, alternating sides, brands in order
            // Mounted clear of the wall-run line (5.2 m, the screen is 4.3 m tall).
            float y=DashRun.District==1 ? 10f : 12f;
            var screen=new GameObject("Architectural digital billboard").transform;
            screen.SetParent(parent,false); screen.position=new Vector3(front-side*1.4f,y,z+10);
            screen.rotation=Quaternion.Euler(0,side>0 ? -125 : 125,0);
            var housing=DashCity.Part(screen,Vector3.zero,new Vector3(6.5f,4.3f,.28f),graphite);
            housing.transform.localPosition=Vector3.zero; housing.transform.localRotation=Quaternion.identity;
            // The ad face: the brand's official logo on a lit board.
            BrandVisibility.Face(track,screen,index,"board",screen.TransformPoint(new Vector3(0,0,-.16f)),screen.rotation,6.1f,3.85f);
            foreach(float ry in new[]{-2.1f,2.1f}) {
                var trim=DashCity.Part(screen,Vector3.zero,new Vector3(6.5f,.045f,.08f),side<0 ? cyan : magenta);
                trim.transform.localPosition=new Vector3(0,ry,-.17f); trim.transform.localRotation=Quaternion.identity;
            }
        }
    }
}
