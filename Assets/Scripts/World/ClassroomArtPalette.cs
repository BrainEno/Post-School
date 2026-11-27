using System;
using UnityEngine;

namespace PostSchool.World
{
    /// <summary>
    /// The classroom's shared palette: desaturated institutional flat colour with a small set of
    /// deliberate accents. Values are chosen so the room reads in grey-scale and the protagonist
    /// (the only saturated figure) always separates from it.
    ///
    /// This is an original Unity palette informed by the owner's Where Is School references;
    /// no colour, texture or model is copied from it. See docs/CLASSROOM_ART.md.
    /// </summary>
    public static class ClassroomArtPalette
    {
        // Structural flat colour.
        public static readonly Color Floor = Hex("8E8A84");
        public static readonly Color Wall = Hex("DCD3C0");
        public static readonly Color WallLower = Hex("9A9384");
        public static readonly Color Ceiling = Hex("C4BDB0");
        public static readonly Color Wainscot = Hex("6F6A61");
        public static readonly Color Blackboard = Hex("232A26");
        public static readonly Color Chalk = Hex("E9E6DA");
        public static readonly Color Wood = Hex("B08E62");
        public static readonly Color Metal = Hex("4A4E52");
        public static readonly Color Glass = Hex("A9C0C4");
        public static readonly Color Ink = Hex("23211F");
        public static readonly Color Paper = Hex("F1EBDC");

        // Accents, reserved for narrative emphasis.
        public static readonly Color AccentRose = Hex("8E5A54");
        public static readonly Color AccentBlue = Hex("59637A");
        public static readonly Color AccentSage = Hex("71806B");
        public static readonly Color AccentOchre = Hex("C39D59");

        // What is outside the room. The dusk sky is the only warm gradient the scene has, so everything
        // that belongs to the street stays muted except the blue container.
        public static readonly Color DuskSkyHigh = Hex("5B4A63");
        public static readonly Color DuskSkyLow = Hex("E2A05C");
        public static readonly Color DuskHaze = Hex("C98A63");
        public static readonly Color Building = Hex("4A4450");
        public static readonly Color BuildingNear = Hex("3B3742");
        public static readonly Color BuildingFar = Hex("6C5F66");
        public static readonly Color WindowLit = Hex("F0C070");
        public static readonly Color Street = Hex("4E4A48");
        public static readonly Color ContainerBlue = Hex("2F6E8C");
        public static readonly Color ContainerBlueShadow = Hex("24556C");
        public static readonly Color Laundry = Hex("E6DFCB");
        public static readonly Color LaundryWarm = Hex("D9A98F");
        public static readonly Color Curtain = Hex("B8AE9A");
        public static readonly Color CurtainFold = Hex("9C9280");

        // Character colours are not here: they belong to CharacterSkinPalette, next to the model they
        // colour. The room must never borrow from the cast, and the cast must never borrow from the room.

        /// <summary>
        /// Where the sun is. The layout cannot say it (lighting is the scene's, not the room's), but both
        /// the offline proofs and the scene builder need the same direction for the long dusk shadows.
        /// </summary>
        public static readonly Vector3 SunDirection = new Vector3(-0.62f, 0.32f, 0.72f).normalized;

        public static Color Hex(string rgb)
        {
            if (string.IsNullOrWhiteSpace(rgb) || !ColorUtility.TryParseHtmlString("#" + rgb, out Color color))
            {
                throw new ArgumentException($"'{rgb}' is not a 6-digit RGB hex value.", nameof(rgb));
            }

            return color;
        }

        /// <summary>Rec. 601 grey value, used by readability checks instead of raw brightness.</summary>
        public static float Grey(Color color)
        {
            return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
        }

        public static float Contrast(Color first, Color second)
        {
            return Mathf.Abs(Grey(first) - Grey(second));
        }
    }
}
