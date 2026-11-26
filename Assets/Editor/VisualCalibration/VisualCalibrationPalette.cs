using System;
using PostSchool.Character;
using UnityEngine;

namespace PostSchool.Tools.VisualCalibration
{
    /// <summary>One reusable material in the calibration set. Procedural: no external texture or model.</summary>
    public readonly struct CalibrationMaterial
    {
        public CalibrationMaterial(string name, Color color, bool unlit, float smoothness)
        {
            Name = name;
            Color = color;
            Unlit = unlit;
            Smoothness = smoothness;
        }

        public string Name { get; }

        public Color Color { get; }

        /// <summary>True for projected or reproduced imagery, which should not respond to scene lighting.</summary>
        public bool Unlit { get; }

        public float Smoothness { get; }
    }

    /// <summary>
    /// The single shared palette for the calibration scene. Every colour is defined once here, and
    /// the contrast tests in tests/EditMode check the grey-scale separation of the important pairs.
    /// </summary>
    public static class VisualCalibrationPalette
    {
        public static class MaterialNames
        {
            public const string PaintedWall = "PaintedWall";
            public const string PaintedSideWall = "PaintedSideWall";
            public const string Board = "Board";
            public const string Concrete = "Concrete";
            public const string Paper = "Paper";
            public const string DeskTop = "DeskTop";
            public const string Oxblood = "Oxblood";
            public const string Ink = "Ink";
            public const string Ochre = "Ochre";
            public const string Projection = "Projection";
            public const string StudentSkin = "StudentSkin";
            public const string StudentHair = "StudentHair";
            public const string StudentCardigan = "StudentCardigan";
            public const string StudentOvershirt = "StudentOvershirt";
            public const string StudentTrousers = "StudentTrousers";
            public const string StudentShoes = "StudentShoes";
            public const string StudentAccent = "StudentAccent";
            public const string GridPaper = "GridPaper";
            public const string Graphite = "Graphite";
            public const string RedPen = "RedPen";
            public const string Hatch = "Hatch";
            public const string CollagePhoto = "CollagePhoto";
        }

        public static readonly Color PaintedWall = Hex("E6DCC6");
        public static readonly Color PaintedSideWall = Hex("D8CDB4");
        public static readonly Color Board = Hex("1A1F1D");
        public static readonly Color Concrete = Hex("7A7772");
        public static readonly Color Paper = Hex("EDE7D6");
        public static readonly Color DeskTop = Hex("C2A77C");
        public static readonly Color Oxblood = Hex("7A2E2A");
        public static readonly Color Ink = Hex("1A1A18");
        public static readonly Color Ochre = Hex("D99A2B");
        public static readonly Color Projection = Hex("F4D58D");
        public static readonly Color StudentSkin = StudentModelColorPalette.Skin;
        public static readonly Color StudentHair = StudentModelColorPalette.Hair;
        public static readonly Color StudentCardigan = StudentModelColorPalette.MossCardigan;
        public static readonly Color StudentOvershirt = StudentModelColorPalette.ClayOvershirt;
        public static readonly Color StudentTrousers = StudentModelColorPalette.Trousers;
        public static readonly Color StudentShoes = StudentModelColorPalette.Shoes;
        public static readonly Color StudentAccent = StudentModelColorPalette.Accent;

        // Paper-world additions (see docs/VISUAL_CALIBRATION.md, section "纸面化规则"):
        // squared paper, pencil ruling, red-pen correction marks, hatched shadow, and one cut-out photo.
        public static readonly Color GridPaper = Hex("F2F1EA");
        public static readonly Color Graphite = Hex("3A3C3E");
        public static readonly Color RedPen = Hex("C8312A");
        public static readonly Color Hatch = Hex("B5AE9E");
        public static readonly Color CollagePhoto = Hex("8A9BA8");

        public static readonly CalibrationMaterial[] Materials =
        {
            new CalibrationMaterial(MaterialNames.PaintedWall, PaintedWall, false, 0.10f),
            new CalibrationMaterial(MaterialNames.PaintedSideWall, PaintedSideWall, false, 0.10f),
            new CalibrationMaterial(MaterialNames.Board, Board, false, 0.20f),
            new CalibrationMaterial(MaterialNames.Concrete, Concrete, false, 0.15f),
            new CalibrationMaterial(MaterialNames.Paper, Paper, false, 0.05f),
            new CalibrationMaterial(MaterialNames.DeskTop, DeskTop, false, 0.25f),
            new CalibrationMaterial(MaterialNames.Oxblood, Oxblood, false, 0.20f),
            new CalibrationMaterial(MaterialNames.Ink, Ink, false, 0.30f),
            new CalibrationMaterial(MaterialNames.Ochre, Ochre, false, 0.30f),
            new CalibrationMaterial(MaterialNames.Projection, Projection, true, 0f),
            new CalibrationMaterial(MaterialNames.StudentSkin, StudentSkin, false, 0.18f),
            new CalibrationMaterial(MaterialNames.StudentHair, StudentHair, false, 0.12f),
            new CalibrationMaterial(MaterialNames.StudentCardigan, StudentCardigan, false, 0.10f),
            new CalibrationMaterial(MaterialNames.StudentOvershirt, StudentOvershirt, false, 0.10f),
            new CalibrationMaterial(MaterialNames.StudentTrousers, StudentTrousers, false, 0.05f),
            new CalibrationMaterial(MaterialNames.StudentShoes, StudentShoes, false, 0.12f),
            new CalibrationMaterial(MaterialNames.StudentAccent, StudentAccent, false, 0.12f),
            new CalibrationMaterial(MaterialNames.GridPaper, GridPaper, false, 0.05f),
            new CalibrationMaterial(MaterialNames.Graphite, Graphite, false, 0.20f),
            new CalibrationMaterial(MaterialNames.RedPen, RedPen, false, 0.30f),
            new CalibrationMaterial(MaterialNames.Hatch, Hatch, false, 0.05f),
            new CalibrationMaterial(MaterialNames.CollagePhoto, CollagePhoto, false, 0.10f),
        };

        public static Color Hex(string rgb)
        {
            if (string.IsNullOrWhiteSpace(rgb) || !ColorUtility.TryParseHtmlString("#" + rgb, out Color color))
            {
                throw new ArgumentException($"'{rgb}' is not a 6-digit RGB hex value.", nameof(rgb));
            }

            return color;
        }
    }
}
