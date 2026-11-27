using NUnit.Framework;
using PostSchool.Character;
using UnityEngine;

namespace PostSchool.Tests.EditMode
{
    /// <summary>
    /// Pins the animation contract: clips are ordered, they actually move the bones, the idle loops
    /// seamlessly, and Q's idle is deliberately less alive than the protagonist's.
    /// </summary>
    public sealed class HumanoidClipTests
    {
        private GameObject _root;
        private ProceduralSkeleton _skeleton;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Clip Test Root");
            _skeleton = HumanoidRig.Create(_root.transform, HumanoidProportions.Protagonist(), out _);
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [Test]
        public void EveryClipValidatesAgainstTheSkeleton()
        {
            foreach (HumanoidClip clip in HumanoidClips.All())
            {
                Assert.IsTrue(clip.Validate(_skeleton, out string error), $"{clip.Name}: {error}");
                Assert.Greater(clip.Duration, 0f, clip.Name);
                Assert.GreaterOrEqual(clip.KeyCount, 2, clip.Name);
            }
        }

        [Test]
        public void KeyTimesMustIncrease()
        {
            HumanoidClip clip = new HumanoidClip("Bad");
            clip.Add(0f, HumanoidPoses.Rest());
            Assert.Throws<System.ArgumentException>(() => clip.Add(0f, HumanoidPoses.Rest()));
        }

        [Test]
        public void SamplingBetweenKeysBlendsBothEnds()
        {
            HumanoidPose a = new HumanoidPose().With(HumanoidBones.Head, new Vector3(0f, 0f, 0f));
            HumanoidPose b = new HumanoidPose().With(HumanoidBones.Head, new Vector3(0f, 40f, 0f));
            HumanoidClip clip = new HumanoidClip("Blend").Add(0f, a).Add(1f, b);

            float yaw = clip.Sample(0.5f).RotationOf(HumanoidBones.Head).eulerAngles.y;
            Assert.That(yaw, Is.InRange(5f, 35f), "halfway through, the head should be partway round");
        }

        [Test]
        public void TheIdleLoopsSeamlessly()
        {
            HumanoidClip idle = HumanoidClips.StandingIdle();
            Assert.IsTrue(idle.Loops);

            Quaternion atStart = idle.Sample(0f).RotationOf(HumanoidBones.Head);
            Quaternion atEnd = idle.Sample(idle.Duration).RotationOf(HumanoidBones.Head);
            Assert.Less(Quaternion.Angle(atStart, atEnd), 1f, "last pose must match the first for a clean loop");

            // And it must be actually animating, not a still frame.
            Quaternion midway = idle.Sample(idle.Duration * 0.45f).RotationOf(HumanoidBones.Head);
            Assert.Greater(Quaternion.Angle(atStart, midway), 5f);
        }

        [Test]
        public void TheWakeClipActuallyMovesTheFigure()
        {
            HumanoidClip wake = HumanoidClips.WakeFromDesk();
            HumanoidPose asleep = wake.Sample(0f);
            HumanoidPose upright = wake.Sample(wake.Duration);

            Assert.Less(asleep.RootHeightOffset, -0.35f, "a sleeping figure is low to the desk");
            Assert.Greater(upright.RootHeightOffset, -0.05f, "a standing figure is at its full height");
            Assert.Greater(Quaternion.Angle(asleep.RotationOf(HumanoidBones.Hips), upright.RotationOf(HumanoidBones.Hips)), 40f);
        }

        [Test]
        public void ApplyingTheWakePoseMovesTheHeadFromTheDeskToStandingHeight()
        {
            HumanoidClip wake = HumanoidClips.WakeFromDesk();
            Transform head = _skeleton.Bone(_skeleton.IndexOf(HumanoidBones.Head));

            wake.Sample(0f).ApplyClean(_skeleton);
            float asleepHead = head.position.y;

            wake.Sample(wake.Duration).ApplyClean(_skeleton);
            float uprightHead = head.position.y;

            Assert.Greater(uprightHead - asleepHead, 0.45f, "the head must rise by roughly the crouch depth");
        }

        [Test]
        public void TheClassmatesIdleIsStillerThanTheProtagonists()
        {
            HumanoidClip protagonist = HumanoidClips.StandingIdle();
            HumanoidClip q = HumanoidClips.ClassmateIdle();

            float protagonistMotion = TotalHeadMotion(protagonist);
            float qMotion = TotalHeadMotion(q);

            Assert.Less(qMotion, protagonistMotion, "Q should move less than the protagonist, not more");
            Assert.Greater(qMotion, 1f, "Q is still, not frozen");
        }

        [Test]
        public void TheWalkCycleAlternatesLegsAndLoops()
        {
            HumanoidClip walk = HumanoidClips.ClassmateWalk();
            Assert.IsTrue(walk.Validate(_skeleton, out string error), error);

            // At the first contact the left leg is forward of the right; half a cycle later it is the
            // other way round. That is the whole definition of a walk cycle.
            string thigh = HumanoidBones.ThighLeft;
            string thighRight = HumanoidBones.ThighRight;
            float leftForwardAtStart = walk.Sample(0f).RotationOf(thigh).eulerAngles.x;
            float rightForwardAtStart = walk.Sample(0f).RotationOf(thighRight).eulerAngles.x;
            float leftForwardAtHalf = walk.Sample(walk.Duration * 0.5f).RotationOf(thigh).eulerAngles.x;

            Assert.Less(Normalise(leftForwardAtStart), Normalise(rightForwardAtStart), "left leg leads at contact");
            Assert.Greater(Normalise(leftForwardAtHalf), Normalise(rightForwardAtStart), "and trails half a cycle later");

            // Loops: the last key must be the first key.
            Assert.Less(Quaternion.Angle(walk.Sample(0f).RotationOf(thigh), walk.Sample(walk.Duration).RotationOf(thigh)), 0.5f);
        }

        private static float Normalise(float degrees)
        {
            return degrees > 180f ? degrees - 360f : degrees;
        }

        [Test]
        public void PosesRejectUnknownBones()
        {
            HumanoidPose pose = new HumanoidPose().With("Tail", Vector3.zero);
            Assert.IsFalse(pose.Validate(_skeleton, out string error));
            StringAssert.Contains("Tail", error);
        }

        [Test]
        public void EveryWakeKeyKeepsTheSolesOnTheFloor()
        {
            // Grounding is the easiest thing to lose when a pose is edited: a foot 3 cm in the air or
            // 5 cm through the floor is instantly visible and never fails on its own.
            HumanoidClip wake = HumanoidClips.WakeFromDesk();
            Transform left = _skeleton.Bone(_skeleton.IndexOf(HumanoidBones.FootLeft));
            Transform right = _skeleton.Bone(_skeleton.IndexOf(HumanoidBones.FootRight));
            float ankleHeight = HumanoidProportions.Protagonist().ankleHeight;

            foreach (PoseKey key in wake.Keys)
            {
                key.pose.ApplyClean(_skeleton);
                float lowest = Mathf.Min(left.position.y, right.position.y);
                Assert.That(lowest, Is.InRange(ankleHeight - 0.035f, ankleHeight + 0.035f),
                    $"key at {key.time:0.00}s puts the lowest ankle at {lowest:0.000} m");
            }
        }

        [Test]
        public void EveryWalkKeyKeepsAtLeastOneSoleOnTheFloor()
        {
            HumanoidClip walk = HumanoidClips.ClassmateWalk();
            Transform left = _skeleton.Bone(_skeleton.IndexOf(HumanoidBones.FootLeft));
            Transform right = _skeleton.Bone(_skeleton.IndexOf(HumanoidBones.FootRight));
            float ankleHeight = HumanoidProportions.Protagonist().ankleHeight;

            foreach (PoseKey key in walk.Keys)
            {
                key.pose.ApplyClean(_skeleton);
                float lowest = Mathf.Min(left.position.y, right.position.y);
                Assert.That(lowest, Is.InRange(ankleHeight - 0.03f, ankleHeight + 0.03f),
                    $"walk key at {key.time:0.00}s: no foot is planted (lowest {lowest:0.000} m)");
            }
        }

        [Test]
        public void ApplyingACleanPoseClearsLeftoverRotations()
        {
            _skeleton.Bone(_skeleton.IndexOf(HumanoidBones.Spine)).localRotation = Quaternion.Euler(40f, 0f, 0f);
            HumanoidPoses.Rest().ApplyClean(_skeleton);

            Quaternion spine = _skeleton.Bone(_skeleton.IndexOf(HumanoidBones.Spine)).localRotation;
            Assert.Less(Quaternion.Angle(spine, Quaternion.identity), 0.01f);
        }

        /// <summary>Total head rotation change sampled across a clip; a crude "how alive is this" score.</summary>
        private static float TotalHeadMotion(HumanoidClip clip)
        {
            float total = 0f;
            Quaternion previous = clip.Sample(0f).RotationOf(HumanoidBones.Head);
            const int steps = 24;
            for (int i = 1; i <= steps; i++)
            {
                Quaternion current = clip.Sample(clip.Duration * i / steps).RotationOf(HumanoidBones.Head);
                total += Quaternion.Angle(previous, current);
                previous = current;
            }

            return total;
        }
    }
}
