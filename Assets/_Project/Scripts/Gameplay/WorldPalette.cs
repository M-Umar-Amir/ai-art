using UnityEngine;

namespace DallasDash.Gameplay
{
    public static class WorldPalette
    {
        public static readonly Color WarmWhite = new(1f, .86f, .62f);
        public static bool IsCoolNeon(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            return saturation > .4f && value > .35f && ((hue > .43f && hue < .61f) || (hue > .69f && hue < .96f));
        }
        public static Color Surface(Color color) => color;
    }
}
