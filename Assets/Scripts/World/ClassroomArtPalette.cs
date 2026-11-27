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

        // Character colours are not here: they belong to CharacterSkinPalette, next to the model they
        // colour. The room must never borrow from the cast, and the cast must never borrow from the room.

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
