using UnityEngine;

namespace DallasDash.Gameplay
{
    /// The Dash City crowd (Quaternius CC0 bodies, models/make_people.py + bake_outfits.py): rigged people with
    /// baked everyday outfits, one of several hair pieces and real animation clips.
    [CreateAssetMenu(menuName = "Dash Riders/People catalogue", fileName = "PeopleCatalogue")]
    public class PeopleCatalogue : ScriptableObject
    {
        public Pedestrian[] bodies;
        public Material[] maleOutfits, femaleOutfits;
        public Color[] hairTints = { new(.12f, .08f, .05f), new(.25f, .15f, .08f), new(.05f, .05f, .06f), new(.55f, .4f, .2f), new(.45f, .1f, .08f) };
    }
}
