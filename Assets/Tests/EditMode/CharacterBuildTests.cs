using NUnit.Framework;
using PostSchool.Character;

namespace PostSchool.Tests.EditMode
{
    [TestFixture]
    public sealed class CharacterBuildTests
    {
        [Test]
        public void NewBuildStartsAtFiveBasePointsWithSevenUnspent()
        {
            CharacterBuild build = new CharacterBuild();

            Assert.AreEqual(CharacterBuild.AttributeCount, 5);
            Assert.AreEqual(5, build.Total);
            Assert.AreEqual(CharacterBuild.FreePoints, build.RemainingPoints);
            Assert.IsFalse(build.IsComplete);
            Assert.IsTrue(build.IsValid(out string error), error);
            Assert.AreEqual(1, build.GetValue(CoreAttribute.Physique));
            Assert.AreEqual(1, build.GetValue(CoreAttribute.Psyche));
            Assert.AreEqual(1, build.GetValue(CoreAttribute.Intellect));
            Assert.AreEqual(1, build.GetValue(CoreAttribute.Perception));
            Assert.AreEqual(1, build.GetValue(CoreAttribute.Imagination));
        }

        [Test]
        public void PlayerCanAllocateAllSevenPointsWithoutExceedingCap()
        {
            CharacterBuild build = new CharacterBuild();
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(build.TryAdjust(CoreAttribute.Imagination, 1));
            }

            Assert.AreEqual(CharacterBuild.AttributeCap, build.GetValue(CoreAttribute.Imagination));
            Assert.IsFalse(build.TryAdjust(CoreAttribute.Imagination, 1), "The provisional cap must block a sixth point.");
            Assert.AreEqual(3, build.RemainingPoints);

            Assert.IsTrue(build.TryAdjust(CoreAttribute.Psyche, 1));
            Assert.IsTrue(build.TryAdjust(CoreAttribute.Intellect, 1));
            Assert.IsTrue(build.TryAdjust(CoreAttribute.Perception, 1));
            Assert.IsTrue(build.IsComplete);
            Assert.AreEqual(CharacterBuild.StartingTotal, build.Total);
            Assert.IsTrue(build.IsValid(out string error), error);
            Assert.IsFalse(build.TryAdjust(CoreAttribute.Physique, 1), "No eighth free point may be allocated.");
        }

        [Test]
        public void DecrementCannotGoBelowBaseAndResetReturnsAllPoints()
        {
            CharacterBuild build = new CharacterBuild();
            Assert.IsFalse(build.TryAdjust(CoreAttribute.Physique, -1));
            Assert.IsTrue(build.TryAdjust(CoreAttribute.Physique, 1));
            Assert.IsTrue(build.TryAdjust(CoreAttribute.Physique, -1));
            Assert.IsFalse(build.TryAdjust(CoreAttribute.Physique, -1));
            build.Reset();

            Assert.AreEqual(1, build.GetValue(CoreAttribute.Physique));
            Assert.AreEqual(CharacterBuild.FreePoints, build.RemainingPoints);
        }

        [Test]
        public void SnapshotIsDetachedAndRestoreValidatesBudget()
        {
            CharacterBuild original = new CharacterBuild();
            original.TryAdjust(CoreAttribute.Physique, 1);
            int[] snapshot = original.CopyValues();
            snapshot[0] = CharacterBuild.BaseValue;

            Assert.AreEqual(2, original.GetValue(CoreAttribute.Physique));

            CharacterBuild restored = new CharacterBuild();
            Assert.IsTrue(restored.TryRestoreValues(original.CopyValues(), out string restoreError), restoreError);
            Assert.AreEqual(2, restored.GetValue(CoreAttribute.Physique));

            int[] overBudget = { 5, 5, 5, 1, 1 };
            Assert.IsFalse(restored.TryRestoreValues(overBudget, out string error));
            Assert.IsNotEmpty(error);
            Assert.AreEqual(2, restored.GetValue(CoreAttribute.Physique), "Invalid restores must leave the existing build untouched.");
        }

        [Test]
        public void RestoreRejectsMissingOrOutOfRangeValues()
        {
            CharacterBuild build = new CharacterBuild();

            Assert.IsFalse(build.TryRestoreValues(null, out _));
            Assert.IsFalse(build.TryRestoreValues(new[] { 1, 1, 1, 1 }, out _));
            Assert.IsFalse(build.TryRestoreValues(new[] { 1, 1, 1, 1, 6 }, out _));
        }

        [Test]
        public void CheckResolverIsDeterministicAndReportsThreshold()
        {
            CharacterBuild build = new CharacterBuild();
            build.TryAdjust(CoreAttribute.Imagination, 1);
            build.TryAdjust(CoreAttribute.Imagination, 1);

            AttributeCheckResult pass = AttributeCheckResolver.Resolve(build, CoreAttribute.Imagination, 3);
            AttributeCheckResult fail = AttributeCheckResolver.Resolve(build, CoreAttribute.Psyche, 2);

            Assert.IsTrue(pass.Passed);
            Assert.AreEqual(3, pass.Value);
            Assert.AreEqual(3, pass.MinimumValue);
            Assert.IsFalse(fail.Passed);
            Assert.AreEqual(1, fail.Value);
            Assert.IsTrue(AttributeCheckResolver.Resolve(build, CoreAttribute.Imagination, 3).Passed);
        }
    }
}
