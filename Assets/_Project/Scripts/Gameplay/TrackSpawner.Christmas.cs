using System;
using System.Collections.Generic;
using DallasDash.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DallasDash.Gameplay
{
    /// Christmas hurdles for the snowy Festivals district (user, 3 Nov: "hurdles such as snowman and more
    /// Christmas related hurdles"). Real objects at real scale, each modelled from proper parts (no single-blob
    /// stand-ins), built once per session into a template that is merged to one mesh per material and pooled
    /// like every other hurdle. Hit boxes are the dimensions the builders use, so what you see is what you hit.
    /// Low hurdles stay at or under 1 m, so they are hopped; Block hurdles are steered round.
    public partial class TrackSpawner
    {
        ObstacleDef[] christmasObstacles;
        Transform christmasTemplates;

        /// In Festivals six rows in ten use a Christmas hurdle; the rest are the usual street traffic.
        ObstacleDef DistrictObstacle()
        {
            if (Core.DashRun.District == 2 && Random.value < .6f)
            {
                var set = ChristmasObstacles();
                int total = 0;
                foreach (var o in set) total += o.weight;
                int r = Random.Range(0, total);
                foreach (var o in set) { r -= o.weight; if (r < 0) return o; }
            }
            return WeightedObstacle();
        }

        ObstacleDef[] ChristmasObstacles()
        {
            if (christmasObstacles != null) return christmasObstacles;
            christmasTemplates = new GameObject("Christmas hurdle templates").transform;
            christmasTemplates.SetParent(transform, false);
            christmasObstacles = new[]
            {
                Define("snowman", "Snowman / steer round", TrackKind.Block, new Vector3(1.0f, 1.75f, 1.0f), 3, Snowman),
                Define("gift-pile", "Pile of presents / jump", TrackKind.Low, new Vector3(1.8f, 0.85f, 0.9f), 3, GiftPile),
                Define("snow-drift", "Snow drift with a sled / jump", TrackKind.Low, new Vector3(2.0f, 0.6f, 1.5f), 2, SnowDrift),
                Define("candy-cane-barrier", "Candy-cane barrier / jump", TrackKind.Low, new Vector3(2.0f, 0.95f, 0.3f), 2, CandyBarrier),
                Define("sleigh", "Parked sleigh / steer round", TrackKind.Block, new Vector3(1.9f, 1.6f, 3.0f), 2, Sleigh, heavy: true),
                Define("tree-cart", "Christmas-tree cart / steer round", TrackKind.Block, new Vector3(2.0f, 1.65f, 3.2f), 1, TreeCart, heavy: true),
            };
            return christmasObstacles;
        }

        ObstacleDef Define(string id, string name, TrackKind kind, Vector3 size, int weight, Action<Transform> build, bool heavy = false)
        {
            var def = ScriptableObject.CreateInstance<ObstacleDef>();
            def.id = id; def.displayName = name; def.kind = kind; def.size = size; def.weight = weight; def.heavy = heavy;
            var root = new GameObject("Template / " + id);
            root.transform.SetParent(christmasTemplates, false);
            root.transform.position = Vector3.zero; // parts are placed in world space at the origin
            build(root.transform);
            Combine(root);
            // The template owns its merged meshes; pooled copies share them, so no copy may destroy them.
            var lifetime = root.GetComponent<StreamedMeshLifetime>();
            if (lifetime) Destroy(lifetime);
            root.SetActive(false);
            def.prefab = root;
            return def;
        }

        Material X(string key, Color c, float glow = 0f, float smooth = .42f, float metal = .08f) => FestivalDecor.M(this, key, c, glow, smooth, metal);
        static GameObject P(Transform t, Vector3 at, Vector3 size, Material m, PrimitiveType type = PrimitiveType.Cube) => DashCity.Part(t, at, size, m, type);

        /// A classic three-ball snowman, 1.75 m to the hat: coal eyes and buttons, a carrot nose, a knitted red
        /// scarf with a tail, twig arms and a black top hat.
        void Snowman(Transform t)
        {
            var snow = X("xmasSnowman", FestivalDecor.Snow, .08f, .35f, 0f);
            var coal = X("xmasCoal", new Color(.03f, .03f, .035f), 0f, .2f);
            var carrot = X("xmasCarrot", new Color(1f, .45f, .08f), .15f);
            var scarf = X("xmasScarf", FestivalDecor.Red, .05f, .3f);
            var twig = X("xmasTwig", new Color(.25f, .15f, .07f));
            var hat = X("xmasHat", new Color(.05f, .05f, .06f), 0f, .5f);
            P(t, new Vector3(0, .38f, 0), new Vector3(.86f, .8f, .86f), snow, PrimitiveType.Sphere);
            P(t, new Vector3(0, .95f, 0), new Vector3(.64f, .6f, .64f), snow, PrimitiveType.Sphere);
            P(t, new Vector3(0, 1.38f, 0), new Vector3(.44f, .44f, .44f), snow, PrimitiveType.Sphere);
            foreach (float x in new[] { -.08f, .08f }) P(t, new Vector3(x, 1.44f, -.2f), Vector3.one * .05f, coal, PrimitiveType.Sphere);
            foreach (float y in new[] { .82f, .97f, 1.1f }) P(t, new Vector3(0, y, -.3f + Mathf.Abs(y - .97f) * .3f), Vector3.one * .06f, coal, PrimitiveType.Sphere);
            var nose = FestivalDecor.Mesh(t, FestivalDecor.Lathe("Carrot nose", new[] { (0f, .035f), (.08f, .025f), (.17f, 0f) }, 10),
                new Vector3(0, 1.38f, -.21f), Quaternion.Euler(-90f, 0, 0), Vector3.one, carrot, "Carrot nose");
            nose.name = "Carrot nose";
            P(t, new Vector3(0, 1.18f, 0), new Vector3(.5f, .09f, .5f), scarf, PrimitiveType.Cylinder);
            var tail = P(t, new Vector3(.16f, 1.02f, -.24f), new Vector3(.1f, .3f, .03f), scarf);
            tail.transform.rotation = Quaternion.Euler(0, 0, -12f);
            foreach (float s in new[] { -1f, 1f })
            {
                FestivalDecor.Member(t, new Vector3(s * .28f, 1.02f, 0), new Vector3(s * .7f, 1.32f, 0), .035f, twig);
                FestivalDecor.Member(t, new Vector3(s * .58f, 1.24f, 0), new Vector3(s * .66f, 1.42f, 0), .025f, twig);
            }
            P(t, new Vector3(0, 1.59f, 0), new Vector3(.4f, .02f, .4f), hat, PrimitiveType.Cylinder);
            P(t, new Vector3(0, 1.7f, 0), new Vector3(.26f, .11f, .26f), hat, PrimitiveType.Cylinder);
            P(t, new Vector3(0, 1.63f, 0), new Vector3(.27f, .025f, .27f), scarf, PrimitiveType.Cylinder);
        }

        /// Four wrapped presents stacked across the lane, each with ribbon bands and a bow. 0.85 m high.
        void GiftPile(Transform t)
        {
            var papers = new[] { X("xmasPaperRed", FestivalDecor.Red, .05f, .55f), X("xmasPaperGreen", FestivalDecor.Green, .05f, .55f),
                X("xmasPaperGold", new Color(.85f, .62f, .2f), .1f, .7f, .5f), X("xmasPaperWhite", new Color(.92f, .92f, .95f), .05f, .55f) };
            var ribbon = new[] { X("xmasRibbonGold", FestivalDecor.Gold, .4f, .8f, .6f), X("xmasRibbonRed", new Color(.85f, .05f, .08f), .1f, .6f) };
            (Vector3 at, Vector3 size)[] boxes = { (new Vector3(-.55f, .25f, 0), new Vector3(.62f, .5f, .62f)), (new Vector3(.12f, .3f, .05f), new Vector3(.7f, .6f, .7f)),
                (new Vector3(.72f, .2f, -.05f), new Vector3(.4f, .4f, .4f)), (new Vector3(-.2f, .72f, 0), new Vector3(.42f, .26f, .42f)) };
            for (int i = 0; i < boxes.Length; i++)
            {
                var (at, size) = boxes[i];
                var r = ribbon[i % 2 == 0 ? 0 : 1];
                P(t, at, size, papers[i % papers.Length]);
                P(t, at, new Vector3(size.x + .01f, size.y + .01f, .06f), r);
                P(t, at, new Vector3(.06f, size.y + .01f, size.z + .01f), r);
                float top = at.y + size.y * .5f;
                foreach (float s in new[] { -1f, 1f })
                {
                    var loop = P(t, at + new Vector3(s * .06f, size.y * .5f + .05f, 0), new Vector3(.12f, .08f, .04f), r);
                    loop.transform.rotation = Quaternion.Euler(0, 0, s * 30f);
                }
                P(t, new Vector3(at.x, top + .03f, at.z), new Vector3(.05f, .05f, .05f), r);
            }
        }

        /// A snow drift blown across the lane with a wooden sled stuck in it. 0.6 m high.
        void SnowDrift(Transform t)
        {
            var snow = X("xmasSnow", FestivalDecor.Snow, .05f, .55f, 0f);
            var wood = X("xmasSledWood", new Color(.55f, .3f, .14f), 0f, .4f);
            var steel = X("xmasSledSteel", new Color(.75f, .1f, .1f), 0f, .7f, .6f);
            P(t, new Vector3(0, .1f, 0), new Vector3(2.0f, .45f, 1.5f), snow, PrimitiveType.Sphere);
            P(t, new Vector3(-.35f, .2f, .15f), new Vector3(1.1f, .4f, .9f), snow, PrimitiveType.Sphere);
            P(t, new Vector3(.5f, .18f, -.2f), new Vector3(.9f, .36f, .8f), snow, PrimitiveType.Sphere);
            // Sled: two curled runners and five slats, leaning into the drift.
            var sled = new GameObject("Sled").transform;
            sled.SetParent(t, false); sled.localPosition = new Vector3(.25f, .3f, 0); sled.localRotation = Quaternion.Euler(-14f, 20f, 0);
            foreach (float x in new[] { -.22f, .22f })
            {
                FestivalDecor.Member(sled, sled.TransformPoint(new Vector3(x, 0, -.5f)), sled.TransformPoint(new Vector3(x, 0, .45f)), .03f, steel);
                FestivalDecor.Member(sled, sled.TransformPoint(new Vector3(x, 0, .45f)), sled.TransformPoint(new Vector3(x, .12f, .58f)), .03f, steel);
                FestivalDecor.Member(sled, sled.TransformPoint(new Vector3(x, .12f, .58f)), sled.TransformPoint(new Vector3(x, .2f, .5f)), .03f, steel);
            }
            for (int i = 0; i < 5; i++)
            {
                var slat = P(sled, sled.TransformPoint(new Vector3(0, .1f, -.4f + i * .2f)), new Vector3(.56f, .03f, .12f), wood);
                slat.transform.rotation = sled.rotation;
            }
        }

        /// A festive barrier: two candy-cane posts and a striped rail across the lane. 0.95 m high.
        void CandyBarrier(Transform t)
        {
            var red = X("xmasCaneRed", new Color(.85f, .05f, .08f), .1f, .7f);
            var white = X("xmasCaneWhite", new Color(.97f, .96f, .93f), .05f, .7f);
            foreach (float s in new[] { -1f, 1f })
            {
                FestivalDecor.CandyCane(this, t, new Vector3(s * .9f, 0, 0), s, .82f, .09f);
                P(t, new Vector3(s * .9f, .03f, 0), new Vector3(.36f, .06f, .3f), white);
            }
            for (int i = 0; i < 10; i++)
                P(t, new Vector3(-.8f + i * .178f, .62f, 0), new Vector3(.18f, .12f, .08f), i % 2 == 0 ? red : white);
            for (int i = 0; i < 10; i++)
                P(t, new Vector3(-.8f + i * .178f, .32f, 0), new Vector3(.18f, .1f, .07f), i % 2 == 0 ? white : red);
        }

        /// Santa's sleigh parked in the lane: a red body with gold trim, curled gold runners, a padded seat and
        /// a sack of presents in the back. 1.9 m wide, 1.6 m high, 3 m long.
        void Sleigh(Transform t)
        {
            var body = X("xmasSleighRed", new Color(.7f, .04f, .06f), .05f, .75f, .3f);
            var gold = X("xmasGold", FestivalDecor.Gold, .6f, .8f, .8f);
            var seat = X("xmasSleighSeat", new Color(.08f, .2f, .1f), 0f, .4f);
            var sack = X("xmasSack", new Color(.45f, .3f, .18f), 0f, .2f);
            foreach (float x in new[] { -.75f, .75f })
            {
                FestivalDecor.Member(t, new Vector3(x, .06f, -1.4f), new Vector3(x, .06f, 1.1f), .06f, gold);
                for (int i = 0; i < 6; i++)
                {
                    float a0 = i * Mathf.PI / 6f, a1 = (i + 1) * Mathf.PI / 6f;
                    FestivalDecor.Member(t, new Vector3(x, .06f + (1 - Mathf.Cos(a0)) * .3f, 1.1f + Mathf.Sin(a0) * .3f),
                        new Vector3(x, .06f + (1 - Mathf.Cos(a1)) * .3f, 1.1f + Mathf.Sin(a1) * .3f), .06f, gold);
                }
                foreach (float z in new[] { -1f, 0f, .9f }) FestivalDecor.Member(t, new Vector3(x, .06f, z), new Vector3(x * .9f, .4f, z), .05f, gold);
            }
            P(t, new Vector3(0, .62f, -.25f), new Vector3(1.6f, .5f, 2.3f), body);
            var nose = P(t, new Vector3(0, .9f, 1.0f), new Vector3(1.6f, .7f, .5f), body);
            nose.transform.rotation = Quaternion.Euler(-28f, 0, 0);
            foreach (float x in new[] { -.82f, .82f }) P(t, new Vector3(x, 1.0f, -.25f), new Vector3(.08f, .5f, 2.3f), body);
            P(t, new Vector3(0, 1.26f, -.25f), new Vector3(1.75f, .05f, 2.35f), gold);
            P(t, new Vector3(0, .96f, .1f), new Vector3(1.4f, .2f, .6f), seat);
            P(t, new Vector3(0, 1.2f, -.15f), new Vector3(1.4f, .5f, .14f), seat);
            P(t, new Vector3(.1f, 1.2f, -.9f), new Vector3(.9f, .8f, .75f), sack, PrimitiveType.Sphere);
            P(t, new Vector3(.1f, 1.6f, -.95f), new Vector3(.16f, .12f, .16f), gold, PrimitiveType.Cylinder);
            P(t, new Vector3(-.45f, 1.02f, -1.1f), new Vector3(.34f, .3f, .34f), X("xmasPaperGreen", FestivalDecor.Green, .05f, .55f));
            P(t, new Vector3(-.45f, 1.02f, -1.1f), new Vector3(.35f, .31f, .05f), gold);
        }

        /// A seller's cart loaded with netted Christmas trees, on spoked wheels. 2 m wide, 1.65 m high, 3.2 m long.
        void TreeCart(Transform t)
        {
            var wood = X("xmasCartWood", new Color(.4f, .24f, .12f), 0f, .3f);
            var iron = X("xmasCartIron", new Color(.08f, .08f, .09f), 0f, .5f, .6f);
            var pine = X("xmasPine", FestivalDecor.Green, 0f, .2f, 0f);
            var snow = X("xmasSnow", FestivalDecor.Snow, .05f, .55f, 0f);
            P(t, new Vector3(0, .62f, 0), new Vector3(1.8f, .1f, 3.0f), wood);
            foreach (float x in new[] { -.88f, .88f }) P(t, new Vector3(x, .8f, 0), new Vector3(.06f, .3f, 3.0f), wood);
            foreach (float x in new[] { -.95f, .95f })
                foreach (float z in new[] { -1.0f, 1.0f })
                {
                    P(t, new Vector3(x, .38f, z), new Vector3(.76f, .04f, .76f), iron, PrimitiveType.Cylinder).transform.rotation = Quaternion.Euler(0, 0, 90f);
                    for (int s = 0; s < 4; s++)
                    {
                        var spoke = P(t, new Vector3(x, .38f, z), new Vector3(.03f, .7f, .03f), wood);
                        spoke.transform.rotation = Quaternion.Euler(s * 45f, 0, 0);
                    }
                }
            FestivalDecor.Member(t, new Vector3(-.3f, .62f, 1.5f), new Vector3(-.3f, .55f, 2.2f), .06f, wood);
            FestivalDecor.Member(t, new Vector3(.3f, .62f, 1.5f), new Vector3(.3f, .55f, 2.2f), .06f, wood);
            // Three trees lying on the bed, tips toward the rider, with a dusting of snow.
            foreach (float x in new[] { -.55f, 0f, .55f })
            {
                for (int k = 0; k < 4; k++)
                {
                    float f = k / 3f;
                    var tier = FestivalDecor.Mesh(t, FestivalDecor.FirTier(), new Vector3(x, .95f + (x == 0f ? .25f : 0f), -1.2f + f * 2.1f),
                        Quaternion.Euler(-90f, 0, 0), new Vector3(Mathf.Lerp(.42f, .18f, f), .75f, Mathf.Lerp(.42f, .18f, f)), pine, "Tree on the cart");
                }
                P(t, new Vector3(x, 1.2f + (x == 0f ? .25f : 0f), -.3f), new Vector3(.4f, .05f, 1.4f), snow);
            }
        }
    }
}
