using UnityEngine;

namespace DallasDash.Gameplay
{
    /// The brick street and its KFC art (client, 25 Nov 2026), set by the bootstrap from models/kfc-street/out.
    public partial class TrackSpawner
    {
        /// Brick facade atlases (make_brick_facades.py) in WindowAtlas brick order: loft, warehouse, buff, then the
        /// same three bricks without windows. Emission is null for the plain ones.
        public Texture2D[] facadeAlbedo, facadeNormal, facadeEmission;
        /// KFC street art (make_kfc_art.py): rooftop bulletins, the curved corner screen and the flat screen under it,
        /// the outlet's awning, stripes, tower panel and window poster, and the unbranded shops' pictograms.
        public Texture2D kfcAdDark, kfcAdRed, kfcAdPink, kfcAdBanner, kfcAwning, kfcStripes, kfcPanel, kfcPoster, shopIcons;
    }
}
