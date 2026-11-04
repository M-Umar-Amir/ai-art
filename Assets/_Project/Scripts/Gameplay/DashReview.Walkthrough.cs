using UnityEngine;

namespace DallasDash.Gameplay
{
    /// The narrated kiosk walkthroughs were retired with the kiosks (4 Nov 2026; archived in
    /// dash-riders-overhaul/_archive/kiosk-20261104). These flags remain for the city hub's input hooks.
    public partial class DashReview
    {
        public static bool WalkthroughActive { get; private set; }
        public static bool WalkingCity { get; private set; }
        public static Vector2 WalkthroughInput { get; private set; }
        public static string WalkthroughCaption { get; private set; } = "";
        void CheckWalkthroughLayout() { }
    }
}
