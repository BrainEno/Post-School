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
    /// The palette for the three figures, chosen against the owner's paper/flat-shade references:
    ///
    ///   - figures are flat colour with almost no specular, so they read as cut paper;
    ///   - the protagonist is the only student carrying a saturated colour (the sleeve band), which is
    ///     what the references do: one red mark in a grey drawing;
    ///   - Q is deliberately lower contrast than the protagonist and slightly closer in value to the
    ///     walls, so the classmate reads as already part of the room;
    ///   - N is the bright, cold value key: the lightest skin and the lightest cloth in the cast, with
    ///     a sage cardigan far enough above Q's olive coat (0.645 vs 0.475 grey) that the two do not
    ///     collapse into one green silhouette when they stand in the same shot.
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

        // N, the substitute teacher. She is the third value key in the cast and the lightest of the
        // three: the protagonist is dark (coat 0.430 grey), Q is mid (0.475), N is bright and cold.
        // The cardigan is a sage knit lifted clear of Q's olive coat so the two green-greys never read
        // as the same garment at a distance, and the hair is the one place she is not dark: 0.300
        // against the protagonist's 0.160 and Q's 0.227, which is what keeps a short dark bob from
        // turning into the same silhouette as the two students' heads.
        public static readonly Color TeacherSkin = Hex("D9CDBE");
        public static readonly Color TeacherHair = Hex("4A4C56");
        public static readonly Color TeacherCardigan = Hex("9BAC97");
        public static readonly Color TeacherShirt = Hex("FAF7EE");

        /// <summary>
        /// Chalk dust on a navy register cover. Nothing in the mesh binds the accent slot yet (neither
        /// protagonist nor Q does either), so this is the colour the marker pens and the register will
        /// take when they get geometry, not a surface you can see today.
        /// </summary>
        public static readonly Color TeacherAccent = Hex("7E8B9C");

        /// <summary>
        /// B, as the imagination builds her: white hair, a dark skirt and cardigan, and skin paler than
        /// anyone else in the room because she is not lit --- she is recalled.
        ///
        /// The coat is the darkest surface in the cast on purpose. She has to separate from N's sage
        /// cardigan at a glance, since the two of them are the only adults and the player meets them
        /// within a few minutes of each other. The accent is the one warm colour on her, and it goes on
        /// the hairpin: the single place the imagination bothered to invent a detail nobody described.
        /// </summary>
        public static readonly Color BiologyTeacherSkin = Hex("D6CBC0");
        public static readonly Color BiologyTeacherHair = Hex("E4E1D8");
        public static readonly Color BiologyTeacherCoat = Hex("3E3B3F");
        public static readonly Color BiologyTeacherShirt = Hex("CFC9BC");
        public static readonly Color BiologyTeacherAccent = Hex("8C5B3C");

        /// <summary>
        /// One figure's five colours. Callers that used to write <c>role == Protagonist ? a : b</c>
        /// take this instead, so adding a fourth figure is a case here and nowhere else.
        /// </summary>
        public readonly struct FigurePalette
        {
            public FigurePalette(Color skin, Color hair, Color coat, Color shirt, Color accent)
            {
                Skin = skin;
                Hair = hair;
                Coat = coat;
                Shirt = shirt;
                Accent = accent;
            }

            public Color Skin { get; }
            public Color Hair { get; }
            public Color Coat { get; }
            public Color Shirt { get; }
            public Color Accent { get; }
        }

        /// <summary>The colour set for a silhouette. Every <see cref="FigureRole"/> must be answered here.</summary>
        public static FigurePalette ForRole(FigureRole role)
        {
            switch (role)
            {
                case FigureRole.Protagonist:
                    return new FigurePalette(ProtagonistSkin, ProtagonistHair, ProtagonistCoat, ProtagonistShirt, ProtagonistAccent);
                case FigureRole.ClassmateQ:
                    return new FigurePalette(ClassmateSkin, ClassmateHair, ClassmateCoat, ClassmateShirt, ClassmateAccent);
                case FigureRole.TeacherN:
                    return new FigurePalette(TeacherSkin, TeacherHair, TeacherCardigan, TeacherShirt, TeacherAccent);
                case FigureRole.TeacherB:
                    return new FigurePalette(BiologyTeacherSkin, BiologyTeacherHair, BiologyTeacherCoat,
                        BiologyTeacherShirt, BiologyTeacherAccent);
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "No palette for this figure role.");
            }
        }

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
