using System;
using System.Collections.Generic;
using NUnit.Framework;
using PostSchool.Tools.VisualCalibration;
using UnityEngine;

namespace PostSchool.Tests.EditMode
{
    /// <summary>
    /// Readability guard for the calibration palette. Uses the Rec. 601 grey-scale weights, so a pair
    /// that separates only in hue (and not in value) fails here, as it would in grey-scale review.
    /// </summary>
    [TestFixture]
    public sealed class VisualCalibrationPaletteTests
    {
        private const float MinimumGreyContrast = 0.15f;

        [Test]
        public void Palette_ContainsEveryMaterialFamilyTheCalibrationNeeds()
        {
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            foreach (CalibrationMaterial material in VisualCalibrationPalette.Materials)
            {
                names.Add(material.Name);
            }

            // Painted planes, paper, ink, concrete, and projected imagery are the five families the issue asks for.
            // The paper-world additions (grid paper, graphite, red-pen, hatch, collage photo) are required too.
            CollectionAssert.IsSupersetOf(names, new[]
            {
                VisualCalibrationPalette.MaterialNames.PaintedWall,
                VisualCalibrationPalette.MaterialNames.Paper,
                VisualCalibrationPalette.MaterialNames.Ink,
                VisualCalibrationPalette.MaterialNames.Concrete,
                VisualCalibrationPalette.MaterialNames.Projection,
                VisualCalibrationPalette.MaterialNames.GridPaper,
                VisualCalibrationPalette.MaterialNames.Graphite,
                VisualCalibrationPalette.MaterialNames.RedPen,
                VisualCalibrationPalette.MaterialNames.Hatch,
                VisualCalibrationPalette.MaterialNames.CollagePhoto,
            });
        }

        [Test]
        public void MaterialNames_AreUnique()
        {
            HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
            foreach (CalibrationMaterial material in VisualCalibrationPalette.Materials)
            {
                Assert.IsTrue(names.Add(material.Name), $"Duplicate material name '{material.Name}'.");
            }
        }

        [Test]
        public void ProjectionMaterial_IsUnlit()
        {
            CalibrationMaterial projection = FindMaterial(VisualCalibrationPalette.MaterialNames.Projection);
            Assert.IsTrue(projection.Unlit, "Projected imagery should not respond to scene lighting.");
        }

        [TestCase("Ochre", "Concrete")]
        [TestCase("Ochre", "PaintedWall")]
        [TestCase("Oxblood", "Concrete")]
        [TestCase("Oxblood", "PaintedWall")]
        [TestCase("Oxblood", "Board")]
        [TestCase("Oxblood", "PaintedSideWall")]
        [TestCase("Board", "PaintedWall")]
        [TestCase("DeskTop", "Concrete")]
        [TestCase("DeskTop", "PaintedWall")]
        [TestCase("RedPen", "PaintedWall")]
        [TestCase("RedPen", "Board")]
        [TestCase("Graphite", "PaintedWall")]
        [TestCase("Graphite", "Concrete")]
        [TestCase("Graphite", "Paper")]
        [TestCase("Hatch", "Concrete")]
        public void ImportantPairs_SeparateInGreyScale(string foregroundName, string backgroundName)
        {
            Color foreground = ColorOf(foregroundName);
            Color background = ColorOf(backgroundName);

            float contrast = Mathf.Abs(Grey(foreground) - Grey(background));
            Assert.GreaterOrEqual(contrast, MinimumGreyContrast,
                $"{foregroundName} vs {backgroundName} grey-scale contrast {contrast:0.000} is below {MinimumGreyContrast}. Adjust the palette.");
        }

        [Test]
        public void Hex_RejectsMalformedValues()
        {
            Assert.Throws<ArgumentException>(() => VisualCalibrationPalette.Hex("ZZZZZZ"));
            Assert.Throws<ArgumentException>(() => VisualCalibrationPalette.Hex(string.Empty));
        }

        private static float Grey(Color color)
        {
            return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
        }

        private static CalibrationMaterial FindMaterial(string name)
        {
            foreach (CalibrationMaterial material in VisualCalibrationPalette.Materials)
            {
                if (material.Name == name)
                {
                    return material;
                }
            }

            throw new ArgumentException($"No calibration material named '{name}'. Check VisualCalibrationPalette.MaterialNames.");
        }

        private static Color ColorOf(string name)
        {
            return FindMaterial(name).Color;
        }
    }
}
