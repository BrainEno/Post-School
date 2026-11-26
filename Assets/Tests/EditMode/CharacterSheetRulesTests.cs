using System;
using NUnit.Framework;
using PostSchool.Character;
using PostSchool.Ui;

namespace PostSchool.Tests.EditMode
{
    [TestFixture]
    public sealed class CharacterSheetRulesTests
    {
        [Test]
        public void LadderSpansTheProvisionalScale()
        {
            Assert.AreEqual(5, CharacterSheetRules.SlotCount);
            Assert.AreEqual(CharacterBuild.AttributeCap - CharacterBuild.BaseValue + 1, CharacterSheetRules.SlotCount);
        }

        [Test]
        public void FreshAttributeShowsOneFilledRungAndOnePreviewRung()
        {
            int value = CharacterBuild.BaseValue;
            int filled = 0;
            int preview = 0;
            for (int slot = 0; slot < CharacterSheetRules.SlotCount; slot++)
            {
                if (CharacterSheetRules.IsFilled(slot, value)) filled++;
                if (CharacterSheetRules.IsPreviewSlot(slot, value, CharacterBuild.FreePoints)) preview++;
            }

            Assert.AreEqual(1, filled, "A base value of 1 must fill exactly one rung, not zero.");
            Assert.AreEqual(1, preview, "Only the next reachable rung may be previewed.");
            Assert.IsTrue(CharacterSheetRules.IsFilled(0, 1));
            Assert.IsTrue(CharacterSheetRules.IsPreviewSlot(1, 1, 7));
            Assert.IsFalse(CharacterSheetRules.IsPreviewSlot(2, 1, 7), "Unreachable rungs must not be advertised.");
        }

        [Test]
        public void CappedAttributeFillsEveryRungAndOffersNoPreview()
        {
            int value = CharacterBuild.AttributeCap;
            for (int slot = 0; slot < CharacterSheetRules.SlotCount; slot++)
            {
                Assert.IsTrue(CharacterSheetRules.IsFilled(slot, value), $"Slot {slot} should be filled at the cap.");
                Assert.IsFalse(CharacterSheetRules.IsPreviewSlot(slot, value, 2));
            }

            Assert.IsTrue(CharacterSheetRules.IsAtCap(value));
            Assert.IsFalse(CharacterSheetRules.CanAllocate(value, 2), "The cap must block allocation even with points left.");
        }

        [Test]
        public void AllocationNeedsAnUnspentPoint()
        {
            Assert.IsTrue(CharacterSheetRules.CanAllocate(1, 7));
            Assert.IsFalse(CharacterSheetRules.CanAllocate(3, 0));
        }

        [Test]
        public void LevelsReadAsWordsRatherThanOnlyNumbers()
        {
            Assert.AreEqual("基础", CharacterSheetRules.DescribeLevel(1));
            Assert.AreEqual("低", CharacterSheetRules.DescribeLevel(2));
            Assert.AreEqual("中", CharacterSheetRules.DescribeLevel(3));
            Assert.AreEqual("高", CharacterSheetRules.DescribeLevel(4));
            Assert.AreEqual("极高", CharacterSheetRules.DescribeLevel(5));
            Assert.AreEqual("超出刻度", CharacterSheetRules.DescribeLevel(6));
        }

        [Test]
        public void BudgetSentenceReportsRemainingPointsHonestly()
        {
            CharacterBuild build = new CharacterBuild();
            StringAssert.Contains("7", CharacterSheetRules.DescribeBudget(build));
            Assert.IsTrue(CharacterSheetRules.HasUnspentPoints(build));

            for (int i = 0; i < CharacterBuild.FreePoints; i++)
            {
                Assert.IsTrue(build.TryAdjust(CoreAttribute.Perception, 1), $"Point {i + 1} should still fit under the cap.");
            }

            Assert.IsTrue(build.IsComplete);
            Assert.IsFalse(CharacterSheetRules.HasUnspentPoints(build));
            StringAssert.Contains("可以开始", CharacterSheetRules.DescribeBudget(build));
        }

        [Test]
        public void ThresholdPreviewMatchesTheDeterministicResolver()
        {
            CharacterBuild build = new CharacterBuild();
            build.TryAdjust(CoreAttribute.Imagination, 1);
            build.TryAdjust(CoreAttribute.Imagination, 1);

            AttributeCheckResult check = AttributeCheckResolver.Resolve(build, CoreAttribute.Imagination, 4);
            string preview = CharacterSheetRules.DescribeThreshold(CoreAttribute.Imagination, check);

            StringAssert.Contains("想象", preview);
            StringAssert.Contains("3 / 4", preview);
            StringAssert.Contains("未达到", preview);
            Assert.IsFalse(preview.Contains("达到阈值"), "A failed check must not read like a success.");
        }

        [Test]
        public void WrapKeepsEveryLineInsideTheColumnBudget()
        {
            const string Text = "【占位，待作者审定】意志、共情与情绪的回声混在一起，很难分清哪一句是自己的。";
            const int Budget = 18;

            string wrapped = CharacterSheetRules.WrapForDisplay(Text, Budget);
            string[] lines = wrapped.Split('\n');
            Assert.Greater(lines.Length, 1);
            foreach (string line in lines)
            {
                Assert.LessOrEqual(CharacterSheetRules.DisplayWidth(line), Budget, $"Line too wide: {line}");
            }

            int reflowed = 0;
            foreach (string line in lines)
            {
                reflowed += CharacterSheetRules.DisplayWidth(line);
            }

            Assert.AreEqual(CharacterSheetRules.DisplayWidth(Text), reflowed, "Wrapping must not lose or add characters.");
        }

        [Test]
        public void WrapRejectsANonPositiveBudget()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CharacterSheetRules.WrapForDisplay("文字", 0));
        }

        [Test]
        public void LabelsAreSpacedForChineseAndStopAtAsciiRuns()
        {
            StringAssert.Contains("\u2009", CharacterSheetRules.SpaceOut("角色倾向"));
            Assert.AreEqual("POST SCHOOL", CharacterSheetRules.SpaceOut("POST SCHOOL"));
            Assert.AreEqual(string.Empty, CharacterSheetRules.SpaceOut(null));
        }
    }
}
