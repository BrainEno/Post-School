using System.Collections.Generic;
using NUnit.Framework;
using PostSchool.Character;
using PostSchool.Ui;
using UnityEngine;

namespace PostSchool.Tests.EditMode
{
    /// <summary>
    /// Guards the sheet's two non-code dependencies: the placeholder copy asset and the printed
    /// colour pairs. Both are cheap to break by editing, expensive to notice in the Editor.
    /// </summary>
    [TestFixture]
    public sealed class CharacterSheetContentTests
    {
        // Rec. 601 grey difference, the same measure the calibration palette tests use.
        private const float MinimumTextContrast = 0.45f;   // body copy
        private const float MinimumLargeContrast = 0.25f;  // headings, numerals, button labels
        private const float MinimumQuietContrast = 0.15f;  // rules, ladder rungs, marginalia

        private static CharacterSheetText LoadText()
        {
            TextAsset asset = Resources.Load<TextAsset>("CharacterSheet/character_sheet_text_placeholder");
            Assert.IsNotNull(asset, "Missing Assets/Resources/CharacterSheet/character_sheet_text_placeholder.json.");
            return CharacterSheetText.FromJson(asset.text, "character_sheet_text_placeholder");
        }

        [Test]
        public void PlaceholderCopyValidates()
        {
            CharacterSheetText text = LoadText();
            Assert.IsTrue(text.Validate(out string error), error);
            Assert.AreEqual("placeholder_pending_owner_text", text.status);
            Assert.IsFalse(string.IsNullOrWhiteSpace(text.draftNotice), "Unapproved copy must stay labelled on the sheet.");
        }

        [Test]
        public void EveryAttributeHasVoiceSkillsAndAllFiveRungs()
        {
            CharacterSheetText text = LoadText();
            foreach (CoreAttribute attribute in new[]
                     {
                         CoreAttribute.Physique, CoreAttribute.Psyche, CoreAttribute.Intellect,
                         CoreAttribute.Perception, CoreAttribute.Imagination,
                     })
            {
                AttributeSheetText entry = text.For(attribute);
                Assert.AreEqual(attribute.ToString(), entry.attribute);
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.voice), $"{attribute} has no voice title.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(entry.skills), $"{attribute} has no skill vocabulary.");
                Assert.AreEqual(CharacterSheetRules.SlotCount, entry.rungs.Length);
                foreach (string rung in entry.rungs)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(rung), $"{attribute} has an unnamed rung.");
                }

                // Every rung mark must stay short enough to sit under a 30px slot.
                Assert.LessOrEqual(CharacterSheetRules.DisplayWidth(entry.voice), 12, $"{attribute} voice title is too long for the row.");
            }
        }

        [Test]
        public void NarrativeLinesFitTheDescriptionCardOnceWrapped()
        {
            CharacterSheetText text = LoadText();
            const int CardColumns = 30;
            const int CardLines = 5;

            foreach (AttributeSheetText entry in text.attributes)
            {
                string wrapped = CharacterSheetRules.WrapForDisplay(entry.narrative, CardColumns);
                int lines = wrapped.Split('\n').Length;
                Assert.LessOrEqual(lines, CardLines, $"{entry.attribute} narrative needs {lines} lines at {CardColumns} columns.");
            }
        }

        [Test]
        public void UnknownOrDuplicateAttributeNamesAreRejected()
        {
            CharacterSheetText text = LoadText();
            CharacterSheetText tampered = JsonUtility.FromJson<CharacterSheetText>(JsonUtility.ToJson(text));
            tampered.attributes[0].attribute = "Charm";
            Assert.IsFalse(tampered.Validate(out string unknownError));
            StringAssert.Contains("unknown attribute", unknownError);

            CharacterSheetText repeated = JsonUtility.FromJson<CharacterSheetText>(JsonUtility.ToJson(text));
            repeated.attributes[0].attribute = repeated.attributes[1].attribute;
            Assert.IsFalse(repeated.Validate(out string duplicateError));
            StringAssert.Contains("repeats", duplicateError);

            CharacterSheetText missingStatus = JsonUtility.FromJson<CharacterSheetText>(JsonUtility.ToJson(text));
            missingStatus.status = string.Empty;
            Assert.IsFalse(missingStatus.Validate(out string statusError));
            StringAssert.Contains("status", statusError);
        }

        [Test]
        public void PrintedColourPairsStayReadable()
        {
            AssertContrast("ink on paper", CharacterSheetPalette.Ink, CharacterSheetPalette.Paper, MinimumTextContrast);
            AssertContrast("ink on lifted paper", CharacterSheetPalette.Ink, CharacterSheetPalette.PaperLift, MinimumTextContrast);
            AssertContrast("ink on shaded paper", CharacterSheetPalette.Ink, CharacterSheetPalette.PaperShade, MinimumTextContrast);
            AssertContrast("paper on ochre button", CharacterSheetPalette.Paper, CharacterSheetPalette.Ochre, MinimumLargeContrast);
            AssertContrast("ink on ochre button", CharacterSheetPalette.Ink, CharacterSheetPalette.Ochre, MinimumLargeContrast);
            AssertContrast("paper on backdrop", CharacterSheetPalette.Paper, CharacterSheetPalette.Backdrop, MinimumTextContrast);
        }

        [Test]
        public void PencilAccentsSeparateFromPaperButStayQuieterThanInk()
        {
            Color[] accents =
            {
                CharacterSheetPalette.PencilOxblood,
                CharacterSheetPalette.PencilIndigo,
                CharacterSheetPalette.PencilBlack,
                CharacterSheetPalette.PencilOlive,
                CharacterSheetPalette.PencilOchre,
            };

            HashSet<string> seen = new HashSet<string>();
            foreach (Color accent in accents)
            {
                Assert.IsTrue(seen.Add(ColorUtility.ToHtmlStringRGB(accent)), "Pencil accents should be distinguishable.");
                float onPaper = Contrast(accent, CharacterSheetPalette.Paper);
                Assert.GreaterOrEqual(onPaper, MinimumQuietContrast, $"{CharacterSheetPalette.Html(accent)} disappears on paper.");
            }

            // Indigo is the quietest accent; keep it from dissolving into the paper.
            Assert.GreaterOrEqual(
                Contrast(CharacterSheetPalette.PencilIndigo, CharacterSheetPalette.PaperShade),
                0.5f,
                "Indigo rungs must stay visible on the shaded row.");

            // Ochre is the sheet's emphasis colour and must not be mistaken for the paper.
            Assert.GreaterOrEqual(Contrast(CharacterSheetPalette.Ochre, CharacterSheetPalette.PaperShade), MinimumQuietContrast);
        }

        [Test]
        public void ContrastHelperRejectsDuplicateColours()
        {
            Assert.AreEqual(1f, Contrast(CharacterSheetPalette.Ink, CharacterSheetPalette.Ink), 0.0001f);
            Assert.Throws<System.ArgumentException>(() => CharacterSheetPalette.Hex("not-a-colour"));
            Assert.Throws<System.ArgumentException>(() => CharacterSheetPalette.Hex("12345"));
        }

        private static void AssertContrast(string label, Color foreground, Color background, float minimum)
        {
            float ratio = Contrast(foreground, background);
            Assert.GreaterOrEqual(ratio, minimum, $"{label} has contrast {ratio:0.00}, below {minimum:0.00}.");
        }

        private static float Contrast(Color first, Color second)
        {
            return Mathf.Abs(CharacterSheetPalette.Grey(first) - CharacterSheetPalette.Grey(second));
        }
    }
}
