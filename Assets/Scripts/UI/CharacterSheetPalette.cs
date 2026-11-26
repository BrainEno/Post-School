using System;
using UnityEngine;

namespace PostSchool.Ui
{
    /// <summary>
    /// Colours and material cues for the character sheet. The values mirror the shared collage
    /// palette used by the visual calibration scene (paper, ink, concrete, ochre, oxblood), plus the
    /// five coloured-pencil accents that belong to the attributes. Declared here rather than in
    /// Assets/Editor because that assembly is Editor-only; see docs/CHARACTER_SHEET_UI.md.
    /// </summary>
    public static class CharacterSheetPalette
    {
        /// <summary>Desk surface behind the pasted sheet.</summary>
        public static readonly Color Backdrop = Hex("14150F");

        /// <summary>The paper the sheet is printed on.</summary>
        public static readonly Color Paper = Hex("EDE7D6");

        /// <summary>A second, older sheet under the first one; also used for alternating rows.</summary>
        public static readonly Color PaperShade = Hex("DBD2B9");

        /// <summary>Lifted paper for the selected row and the description card.</summary>
        public static readonly Color PaperLift = Hex("F6F1E2");

        public static readonly Color Ink = Hex("1A1A18");
        public static readonly Color Ochre = Hex("D99A2B");
        public static readonly Color Oxblood = Hex("7A2E2A");

        /// <summary>Coloured-pencil accents, one per attribute. Order follows CoreAttribute.</summary>
        public static readonly Color PencilOxblood = Hex("7A2E2A");
        public static readonly Color PencilIndigo = Hex("3F4A6B");
        public static readonly Color PencilBlack = Hex("1A1A18");
        public static readonly Color PencilOlive = Hex("5F6B4A");
        public static readonly Color PencilOchre = Hex("D99A2B");

        /// <summary>Translucent tape used for the collage strips.</summary>
        public static readonly Color Tape = Hex("E9DFBF");

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        /// <summary>Rec. 601 grey value, used by the readability tests instead of raw brightness.</summary>
        public static float Grey(Color color)
        {
            return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
        }

        public static Color Hex(string rgb)
        {
            if (string.IsNullOrWhiteSpace(rgb) || !ColorUtility.TryParseHtmlString("#" + rgb, out Color color))
            {
                throw new ArgumentException($"'{rgb}' is not a 6-digit RGB hex value.", nameof(rgb));
            }

            return color;
        }

        public static string Html(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }
    }
}
