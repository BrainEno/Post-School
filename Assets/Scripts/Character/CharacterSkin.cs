using System;
using UnityEngine;

namespace PostSchool.Character
{
    /// <summary>
    /// Submesh order for every skinned figure. Fixed here so the mesh builder and the renderer's
    /// material array can never disagree.
    /// </summary>
    public enum CharacterSurface
    {
        Skin = 0,
        Hair = 1,
        Coat = 2,
        Shirt = 3,
    }

    /// <summary>Material set for one skinned character: body, hair, cloth and the single accent.</summary>
    public sealed class CharacterSkin
    {
        public CharacterSkin(Material body, Material hair, Material coat, Material shirt, Material accent)
        {
            Body = body ?? throw new ArgumentNullException(nameof(body));
            Hair = hair ?? throw new ArgumentNullException(nameof(hair));
            Coat = coat ?? throw new ArgumentNullException(nameof(coat));
            Shirt = shirt ?? throw new ArgumentNullException(nameof(shirt));
            Accent = accent ?? throw new ArgumentNullException(nameof(accent));
        }

        public Material Body { get; }
        public Material Hair { get; }
        public Material Coat { get; }
        public Material Shirt { get; }
        public Material Accent { get; }

        /// <summary>Materials in <see cref="CharacterSurface"/> order, padded with the body material.</summary>
        public Material[] ToMaterials(int submeshCount)
        {
            int count = Mathf.Max(1, submeshCount);
            Material[] materials = new Material[count];
            for (int i = 0; i < count; i++)
            {
                switch ((CharacterSurface)i)
                {
                    case CharacterSurface.Skin: materials[i] = Body; break;
                    case CharacterSurface.Hair: materials[i] = Hair; break;
                    case CharacterSurface.Coat: materials[i] = Coat; break;
                    case CharacterSurface.Shirt: materials[i] = Shirt; break;
                    default: materials[i] = Body; break;
                }
            }

            return materials;
        }
    }

    /// <summary>
    /// The palette for the two figures, chosen against the owner's paper/flat-shade references:
    ///
    ///   - figures are flat colour with almost no specular, so they read as cut paper;
    ///   - the protagonist is the only figure carrying a saturated colour (the sleeve band), which is
    ///     what the references do: one red mark in a grey drawing;
    ///   - Q is deliberately lower contrast than the protagonist and slightly closer in value to the
    ///     walls, so the classmate reads as already part of the room.
    ///
    /// Colours here are original values. The references informed the rules above, not the numbers.
    /// </summary>
    public static class CharacterSkinPalette
    {
        // The protagonist: warm coat, cold trousers, a school shirt, one red band.
        public static readonly Color ProtagonistSkin = Hex("C9A487");
        public static readonly Color ProtagonistHair = Hex("2A2730");
        public static readonly Color ProtagonistCoat = Hex("7E6A55");
        public static readonly Color ProtagonistShirt = Hex("EFE8D6");
        public static readonly Color ProtagonistAccent = Hex("B8442F");

        // Q: desaturated green-grey, pale, and a hair value two steps lighter than the protagonist's.
        public static readonly Color ClassmateSkin = Hex("C2B49C");
        public static readonly Color ClassmateHair = Hex("3B3A36");
        public static readonly Color ClassmateCoat = Hex("71806B");
        public static readonly Color ClassmateShirt = Hex("DCD8C8");
        public static readonly Color ClassmateAccent = Hex("8E5A54");

        public static Color Hex(string rgb)
        {
            if (string.IsNullOrWhiteSpace(rgb) || !ColorUtility.TryParseHtmlString("#" + rgb, out Color color))
            {
                throw new ArgumentException($"'{rgb}' is not a 6-digit RGB hex value.", nameof(rgb));
            }

            return color;
        }

        /// <summary>Rec. 601 grey value, used by the readability checks.</summary>
        public static float Grey(Color color)
        {
            return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
        }

        /// <summary>
        /// The two figures must separate from the room and from each other. The rule the references
        /// follow: no two surfaces in the same shot share a grey value within 0.06.
        /// </summary>
        public static bool Separate(Color a, Color b, float minimum = 0.06f)
        {
            return Mathf.Abs(Grey(a) - Grey(b)) >= minimum;
        }
    }
}
