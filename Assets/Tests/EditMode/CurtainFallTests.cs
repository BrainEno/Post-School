using NUnit.Framework;
using PostSchool.Character;
using PostSchool.World;
using UnityEngine;

namespace PostSchool.Tests.EditMode
{
    /// <summary>
    /// The curtain scene's rules, tested without a scene. These are the decisions the whole scene hangs
    /// on: which attribute a fall is checked against, what looking first costs, and what a pass and a
    /// failure each look like from the outside.
    /// </summary>
    public sealed class CurtainFallTests
    {
        /// <summary>
        /// Builds a legal character: five attributes start at 1 and there are seven free points. The
        /// tests care about the two that matter here, so the rest is spent to make room (or ignored).
        /// </summary>
        private static CharacterBuild BuildWith(int physique, int perception)
        {
            CharacterBuild build = new CharacterBuild();
            SetTo(build, CoreAttribute.Physique, physique);
            SetTo(build, CoreAttribute.Perception, perception);
            Assert.IsTrue(build.IsValid(out string error), error);
            return build;
        }

        private static void SetTo(CharacterBuild build, CoreAttribute attribute, int value)
        {
            // Find a build that reaches this value, keeping the point budget legal. Attributes cannot be
            // moved between each other, so the target has to fit the budget: 1 + 7 = 8 points total.
            int[] values = build.CopyValues();
            int spent = 0;
            foreach (int value in values) spent += value - CharacterBuild.BaseValue;

            while (build.GetValue(attribute) < value)
            {
                spent++;
                Assert.LessOrEqual(spent, CharacterBuild.FreePoints,
                    $"raising {attribute} to {value} needs more than the {CharacterBuild.FreePoints} starting points");
                build.TryAdjust(attribute, 1);
            }
        }

        [Test]
        public void TheFallIsCheckedAgainstPhysiqueBecauseThereIsNoAgility()
        {
            Assert.AreEqual(CoreAttribute.Physique, CurtainFallRules.FallAttribute);
            Assert.AreEqual(CoreAttribute.Perception, CurtainFallRules.ObserveAttribute);
            Assert.AreEqual("体魄", CoreAttributeNames.DisplayName(CurtainFallRules.FallAttribute));
        }

        [Test]
        public void LookingFirstMakesTheFallEasier()
        {
            Assert.Less(
                CurtainFallRules.InformedFallTarget,
                CurtainFallRules.BlindFallTarget,
                "an informed fall must be easier than a blind one, or looking has no point");
        }

        [Test]
        public void StrongBodyPassesTheBlindFall()
        {
            CharacterBuild build = BuildWith(physique: 5, perception: 1);
            AttributeCheckResult fall = CurtainFallRules.ResolveFall(build, observed: false);

            Assert.IsTrue(fall.Passed);
            Assert.AreEqual(FallOutcome.LandedOnContainer, CurtainFallRules.ResolveFallOutcome(fall, fainted: false));
        }

        [Test]
        public void WeakBodyCrashesOnTheContainerRatherThanMissingIt()
        {
            CharacterBuild build = BuildWith(physique: 1, perception: 1);
            AttributeCheckResult fall = CurtainFallRules.ResolveFall(build, observed: false);

            Assert.IsFalse(fall.Passed);
            Assert.AreEqual(FallOutcome.CrashedOnContainer, CurtainFallRules.ResolveFallOutcome(fall, fainted: false));
        }

        [Test]
        public void ObservingCanTurnAFailureIntoALanding()
        {
            // Physique 3 is short of the blind target but enough once the container has been spotted.
            CharacterBuild build = BuildWith(physique: 3, perception: 2);

            Assert.IsFalse(CurtainFallRules.ResolveFall(build, observed: false).Passed, "3 should fail a blind fall");
            Assert.IsTrue(CurtainFallRules.ResolveFall(build, observed: true).Passed, "3 should land after looking");
        }

        [Test]
        public void LookingDownCostsMoreTheMoreYouSee()
        {
            Assert.Less(CurtainFallRules.FaintChanceFor(1), CurtainFallRules.FaintChanceFor(5),
                "a perceptive protagonist sees more of the drop and loses their legs more often");

            for (int value = 1; value <= 5; value++)
            {
                Assert.That(CurtainFallRules.FaintChanceFor(value), Is.InRange(1, 60), "a fainting chance must stay a chance");
            }
        }

        [Test]
        public void TheFaintRollIsAPlainThresholdOnTheChance()
        {
            // The rule is a less-than against a percentage, so both edges are testable with a fixed roll.
            int perception = 2;
            int chance = CurtainFallRules.FaintChanceFor(perception);

            Assert.IsTrue(CurtainFallRules.Faints(perception, (min, max) => chance - 1),
                "a roll just under the chance faints");
            Assert.IsFalse(CurtainFallRules.Faints(perception, (min, max) => chance),
                "a roll exactly on the chance does not");
        }

        [Test]
        public void LookingNeverMakesTheBlindFallHarder()
        {
            // Whatever the build, the informed target is the easier of the two.
            for (int physique = CharacterBuild.BaseValue; physique <= CharacterBuild.AttributeCap; physique++)
            {
                CharacterBuild build = BuildWith(physique, perception: CharacterBuild.BaseValue);
                int blind = CurtainFallRules.ResolveFall(build, observed: false).MinimumValue;
                int informed = CurtainFallRules.ResolveFall(build, observed: true).MinimumValue;
                Assert.LessOrEqual(informed, blind);
            }
        }

        [Test]
        public void AFaintStillLandsOnTheContainerButNotOnItsFeet()
        {
            CharacterBuild build = BuildWith(physique: 5, perception: 1);
            AttributeCheckResult fall = CurtainFallRules.ResolveFall(build, observed: true);

            Assert.IsTrue(fall.Passed);
            Assert.AreEqual(FallOutcome.CrashedOnContainer, CurtainFallRules.ResolveFallOutcome(fall, fainted: true),
                "a body that faints mid-step still falls onto the lid, just badly");
        }

        [Test]
        public void TheDropIsMeasuredFromTheRoomToTheContainerLid()
        {
            TextAsset asset = Resources.Load<TextAsset>("World/classroom_layout");
            Assert.IsNotNull(asset, "the classroom layout must be a Resources asset");
            ClassroomLayout layout = ClassroomLayout.FromJson(asset.text, "test");

            float drop = CurtainFallRules.DropMetres(layout);
            Assert.That(drop, Is.InRange(1f, 12f), "a fall has to be long enough to hurt and short enough to survive");
            Assert.AreEqual(layout.fall.ContainerTopY, CurtainFallRules.LandingPosition(layout).y, 0.001f);
        }

        [Test]
        public void TheContainerIsOutsideTheRoomAndBelowTheDoor()
        {
            TextAsset asset = Resources.Load<TextAsset>("World/classroom_layout");
            ClassroomLayout layout = ClassroomLayout.FromJson(asset.text, "test");

            Assert.Less(layout.fall.containerCenterZ, layout.room.MinZ, "the container must be beyond the missing wall");
            Assert.Less(layout.fall.ContainerTopY, layout.room.height, "the lid must be below the room, or it is not a fall");
        }
    }
}
