using System;
using PostSchool.Character;
using UnityEngine;

namespace PostSchool.World
{
    /// <summary>
    /// What the protagonist commits to at the curtain. The rules are pure functions of the character
    /// build and the layout so they can be tested without a scene, and so the scene cannot quietly
    /// decide something the designer did not write down.
    /// </summary>
    public enum CurtainChoice
    {
        /// <summary>Pull it back and actually look at what is below.</summary>
        Observe,

        /// <summary>Walk through it without looking.</summary>
        StepThrough,

        /// <summary>Leave it closed and turn back into the room.</summary>
        Leave,
    }

    /// <summary>How the drop ends.</summary>
    public enum FallOutcome
    {
        /// <summary>Landed on the container, upright. The good fall.</summary>
        LandedOnContainer,

        /// <summary>Landed on the container, but wrong. Face down on the lid.</summary>
        CrashedOnContainer,

        /// <summary>
        /// Not produced by <see cref="ResolveFallOutcome"/> in this build. Kept as the name for a future
        /// "you stepped out somewhere other than the doorway" case; the scene treats it as a failure that
        /// never plays a landing.
        /// </summary>
        MissedAndFellToStreet,
    }

    /// <summary>
    /// The numbers behind the curtain scene, in one place. Read it as the design document:
    ///
    /// * Looking first is a <b>Perception</b> check, not a dice roll. Passing it lowers the difficulty
    ///   of the body check, because you now know where the container is. It also carries a real risk:
    ///   you cannot help looking down, and looking down is what makes your legs unreliable.
    /// * Stepping through without looking is a <b>Physique</b> check, because with nothing to aim at the
    ///   only thing that saves you is the body's own reaction.
    /// * There is no third attribute in play. Agility is not one of the five; the closest thing to it is
    ///   Physique, so that is what a fall is checked against.
    /// </summary>
    public static class CurtainFallRules
    {
        /// <summary>The attribute that decides a fall. Documented as "the closest thing to a reflex".</summary>
        public const CoreAttribute FallAttribute = CoreAttribute.Physique;

        /// <summary>The attribute that decides what you can see from a doorway at dusk.</summary>
        public const CoreAttribute ObserveAttribute = CoreAttribute.Perception;

        /// <summary>Perception needed to read where the container is before stepping out.</summary>
        public const int ObserveTarget = 2;

        /// <summary>Body check needed to land well when you have not looked.</summary>
        public const int BlindFallTarget = 4;

        /// <summary>Body check needed to land well when you have looked and know where the lid is.</summary>
        public const int InformedFallTarget = 3;

        /// <summary>
        /// The chance, in percent, that looking down first costs the protagonist their footing. This is
        /// the "观察之后有概率降低直接衰弱的风险" part: looking is safer for the landing and riskier for
        /// the descent, and the build decides which side of that you end up on.
        /// </summary>
        public const int FaintChancePercent = 35;

        /// <summary>
        /// True when looking down is likely to end the scene instead: deeper Perception means seeing more
        /// of the drop, and seeing more of the drop is what makes the body refuse.
        /// </summary>
        public static int FaintChanceFor(int perception)
        {
            // Perception 1..5 maps to 8%..48%: a reader notices the drop and pays for noticing.
            int clamped = Mathf.Clamp(perception, CharacterBuild.BaseValue, CharacterBuild.AttributeCap);
            return 8 + (clamped - 1) * 10;
        }

        /// <summary>
        /// Whether the protagonist sees enough of the street to know where the container is. This is the
        /// observation check, and its result is what the informed fall target represents.
        /// </summary>
        public static AttributeCheckResult Observe(CharacterBuild build)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            return AttributeCheckResolver.Resolve(build, ObserveAttribute, ObserveTarget);
        }

        /// <summary>The body check for the fall itself, against the right difficulty.</summary>
        public static AttributeCheckResult ResolveFall(CharacterBuild build, bool observed)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));
            int target = observed ? InformedFallTarget : BlindFallTarget;
            return AttributeCheckResolver.Resolve(build, FallAttribute, target);
        }

        /// <summary>
        /// The final result. <paramref name="fainted"/> is decided by the caller with
        /// <see cref="FaintChanceFor"/>; a fainted protagonist never gets to make the body check.
        /// </summary>
        public static FallOutcome ResolveFallOutcome(AttributeCheckResult fall, bool fainted)
        {
            // A faint mid-step is still a body going roughly where it was aimed: it lands on the lid, just
            // not on its feet. A failed body check is the one that can put you past the container.
            if (fainted) return FallOutcome.CrashedOnContainer;
            return fall.Passed ? FallOutcome.LandedOnContainer : FallOutcome.CrashedOnContainer;
        }

        /// <summary>
        /// The roll for the faint. Kept as a delegate so the scene can inject a deterministic sequence in
        /// tests and the prototype can use Unity's own random.
        /// </summary>
        public static bool Faints(int perception, Func<int, int, int> rollPercent)
        {
            if (rollPercent == null) throw new ArgumentNullException(nameof(rollPercent));
            return rollPercent(0, 100) < FaintChanceFor(perception);
        }

        /// <summary>How far the drop is, in metres, for the animation to travel.</summary>
        public static float DropMetres(ClassroomLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            return Mathf.Max(0.5f, layout.room.height - layout.fall.ContainerTopY);
        }

        /// <summary>
        /// Where the protagonist's root should end up if they land on the container: the lid, in the
        /// layout's own coordinates. Used by the scene and by the tests alike.
        /// </summary>
        public static Vector3 LandingPosition(ClassroomLayout layout)
        {
            FallZone fall = layout.fall;
            return new Vector3(fall.containerCenterX, fall.ContainerTopY, fall.containerCenterZ + fall.containerLength * 0.18f);
        }

        /// <summary>A one-line summary for the prototype's status line. Chinese, like the rest of the UI.</summary>
        public static string Describe(AttributeCheckResult check)
        {
            string name = CoreAttributeNames.DisplayName(check.Attribute);
            return $"{name} 检定：{check.Value} / {check.MinimumValue} · {(check.Passed ? "达到" : "未达到")}";
        }
    }
}
