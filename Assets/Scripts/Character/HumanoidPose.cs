using System;
using System.Collections.Generic;
using UnityEngine;

namespace PostSchool.Character
{
    /// <summary>
    /// A pose is a bone -> local rotation table plus a root height offset. Everything is in local bone
    /// space, so the same pose fits any figure sharing the skeleton: the protagonist and Q use
    /// identical clips and still read as different people, because their proportions differ.
    /// </summary>
    [Serializable]
    public sealed class HumanoidPose
    {
        public Dictionary<string, Quaternion> Rotations = new Dictionary<string, Quaternion>(StringComparer.Ordinal);

        /// <summary>
        /// Offset of the whole figure from where it was placed, in metres. Vertical is crouch/lift;
        /// forward is what lying across a desk needs, and it is why this is a vector: a figure whose
        /// origin is at the feet cannot lie down by rotating its hips, because the legs hang from them.
        /// </summary>
        public Vector3 RootOffset;

        /// <summary>Vertical part of <see cref="RootOffset"/>, kept because most poses only move vertically.</summary>
        public float RootHeightOffset => RootOffset.y;

        public HumanoidPose Clone()
        {
            HumanoidPose copy = new HumanoidPose { RootOffset = RootOffset };
            foreach (KeyValuePair<string, Quaternion> pair in Rotations)
            {
                copy.Rotations[pair.Key] = pair.Value;
            }

            return copy;
        }

        public HumanoidPose With(string bone, Quaternion rotation)
        {
            Rotations[bone] = rotation;
            return this;
        }

        public HumanoidPose With(string bone, Vector3 eulerDegrees)
        {
            Rotations[bone] = Quaternion.Euler(eulerDegrees);
            return this;
        }

        public HumanoidPose WithRootHeight(float offset)
        {
            RootOffset = new Vector3(RootOffset.x, offset, RootOffset.z);
            return this;
        }

        /// <summary>Moves the whole figure; use this when a pose has to leave the spot it stands on.</summary>
        public HumanoidPose WithRootOffset(float right, float up, float forward)
        {
            RootOffset = new Vector3(right, up, forward);
            return this;
        }

        public Quaternion RotationOf(string bone)
        {
            return Rotations.TryGetValue(bone, out Quaternion rotation) ? rotation : Quaternion.identity;
        }

        /// <summary>Blends two poses; bones missing from one side fall back to the other's rest rotation.</summary>
        public static HumanoidPose Lerp(HumanoidPose from, HumanoidPose to, float t)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));

            float clamped = Mathf.Clamp01(t);
            HumanoidPose result = new HumanoidPose
            {
                RootOffset = Vector3.Lerp(from.RootOffset, to.RootOffset, clamped),
            };

            HashSet<string> bones = new HashSet<string>(from.Rotations.Keys, StringComparer.Ordinal);
            bones.UnionWith(to.Rotations.Keys);
            foreach (string bone in bones)
            {
                result.Rotations[bone] = Quaternion.Slerp(from.RotationOf(bone), to.RotationOf(bone), clamped);
            }

            return result;
        }

        /// <summary>Applies the pose to a skeleton, writing only the bones the pose mentions.</summary>
        public void Apply(ProceduralSkeleton skeleton)
        {
            if (skeleton == null) throw new ArgumentNullException(nameof(skeleton));

            for (int i = 0; i < skeleton.BoneCount; i++)
            {
                string name = skeleton.BoneNames[i];
                if (Rotations.TryGetValue(name, out Quaternion rotation))
                {
                    skeleton.Bone(i).localRotation = rotation;
                }
            }
        }

        /// <summary>Applies the pose after resetting every bone, so leftover rotations never leak.</summary>
        public void ApplyClean(ProceduralSkeleton skeleton)
        {
            HumanoidRig.ResetToRest(skeleton);
            Apply(skeleton);
        }

        /// <summary>
        /// A pose is only usable if it names bones that exist. Typos in a clip would otherwise be
        /// silent, so the builder validates every key against the skeleton.
        /// </summary>
        public bool Validate(ProceduralSkeleton skeleton, out string error)
        {
            if (skeleton == null)
            {
                error = "No skeleton.";
                return false;
            }

            foreach (string bone in Rotations.Keys)
            {
                if (skeleton.IndexOf(bone) < 0)
                {
                    error = $"Pose names bone '{bone}', which is not in the skeleton.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// The authored poses. These are the acting notes for both characters: what the protagonist is
    /// doing before the player takes over, and how Q stands, waits and looks.
    /// </summary>
    public static class HumanoidPoses
    {
        /// <summary>Neutral standing, arms slightly out because the coat is thick.</summary>
        public static HumanoidPose Rest()
        {
            return new HumanoidPose()
                .With(HumanoidBones.Head, new Vector3(0f, 0f, 1.6f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(2f, 0f, 6f))
                .With(HumanoidBones.UpperArmRight, new Vector3(2f, 0f, -6f))
                .With(HumanoidBones.ForearmLeft, new Vector3(-8f, 0f, 0f))
                .With(HumanoidBones.ForearmRight, new Vector3(-8f, 0f, 0f));
        }

        /// <summary>
        /// Asleep face-down on the wake desk. The figure stands on its own origin, so lying down is a
        /// <b>root translation</b> plus a fold at the spine: hips stay back over the chair, the torso
        /// goes forward and down until the chest rests on the desk top (0.82 m), the head turns to one
        /// side so the seam faces the camera, one forearm is under the head.
        ///
        /// Zeros are not guesses: the forward/vertical numbers come from solving the forward kinematics
        /// for "chest just off the desk edge, head on the desk top at 0.89 m, hands hanging".
        /// </summary>
        public static HumanoidPose AsleepOnDesk()
        {
            return new HumanoidPose()
                .WithRootOffset(0f, -0.37f, 0.15f)
                .With(HumanoidBones.Hips, new Vector3(0f, 0f, 4f))
                .With(HumanoidBones.Spine, new Vector3(45f, 0f, 0f))
                .With(HumanoidBones.Chest, new Vector3(20f, -4f, 0f))
                .With(HumanoidBones.Neck, new Vector3(50f, 0f, 0f))
                .With(HumanoidBones.Head, new Vector3(30f, 22f, 8f))
                .With(HumanoidBones.ShoulderLeft, new Vector3(0f, 0f, -6f))
                .With(HumanoidBones.ShoulderRight, new Vector3(0f, 0f, 6f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(-60f, 0f, 18f))
                .With(HumanoidBones.UpperArmRight, new Vector3(-60f, 0f, -14f))
                .With(HumanoidBones.ForearmLeft, new Vector3(-40f, 0f, 0f))
                .With(HumanoidBones.ForearmRight, new Vector3(-40f, 0f, 0f))
                .With(HumanoidBones.HandLeft, new Vector3(10f, 0f, 0f))
                .With(HumanoidBones.HandRight, new Vector3(14f, 0f, 0f))
                .With(HumanoidBones.ThighLeft, new Vector3(-88f, 0f, 4f))
                .With(HumanoidBones.ThighRight, new Vector3(-84f, 0f, -3f))
                .With(HumanoidBones.ShinLeft, new Vector3(78f, 0f, 0f))
                .With(HumanoidBones.ShinRight, new Vector3(76f, 0f, 0f))
                .With(HumanoidBones.FootLeft, new Vector3(-10f, 0f, 0f))
                .With(HumanoidBones.FootRight, new Vector3(-8f, 0f, 0f));
        }

        /// <summary>
        /// Head up off the arms, one elbow taking the weight, the torso still folded onto the desk.
        /// The whole body rises about 7 cm, which is what "lifting your head" actually costs.
        /// </summary>
        public static HumanoidPose LiftingHead()
        {
            return new HumanoidPose()
                .WithRootOffset(0f, -0.43f, 0.10f)
                .With(HumanoidBones.Hips, new Vector3(-4f, 0f, 3f))
                .With(HumanoidBones.Spine, new Vector3(60f, 0f, 0f))
                .With(HumanoidBones.Chest, new Vector3(14f, -6f, 0f))
                .With(HumanoidBones.Neck, new Vector3(-20f, 0f, 0f))
                .With(HumanoidBones.Head, new Vector3(18f, 14f, 6f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(-30f, 0f, 18f))
                .With(HumanoidBones.UpperArmRight, new Vector3(-34f, 0f, -14f))
                .With(HumanoidBones.ForearmLeft, new Vector3(-60f, 0f, 0f))
                .With(HumanoidBones.ForearmRight, new Vector3(-66f, 0f, 0f))
                .With(HumanoidBones.ThighLeft, new Vector3(-70f, 0f, 4f))
                .With(HumanoidBones.ThighRight, new Vector3(-68f, 0f, -3f))
                .With(HumanoidBones.ShinLeft, new Vector3(30f, 0f, 0f))
                .With(HumanoidBones.ShinRight, new Vector3(28f, 0f, 0f))
                .With(HumanoidBones.FootLeft, new Vector3(-14f, 0f, 0f))
                .With(HumanoidBones.FootRight, new Vector3(-12f, 0f, 0f));
        }

        /// <summary>
        /// Halfway up: thighs still reaching forward, knees under the body, one hand forward for balance.
        /// A deep squat, not a stand, so the clip has somewhere to travel from.
        /// </summary>
        public static HumanoidPose StandingUp()
        {
            return new HumanoidPose()
                .WithRootOffset(0f, -0.155f, 0.02f)
                .With(HumanoidBones.Hips, new Vector3(-14f, 0f, 3f))
                .With(HumanoidBones.Spine, new Vector3(12f, 0f, 0f))
                .With(HumanoidBones.Chest, new Vector3(8f, 0f, 0f))
                .With(HumanoidBones.Neck, new Vector3(-10f, 0f, 0f))
                .With(HumanoidBones.Head, new Vector3(6f, -8f, 3f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(4f, 0f, 14f))
                .With(HumanoidBones.UpperArmRight, new Vector3(-30f, 0f, -12f))
                .With(HumanoidBones.ForearmLeft, new Vector3(-24f, 0f, 0f))
                .With(HumanoidBones.ForearmRight, new Vector3(-52f, 0f, 0f))
                .With(HumanoidBones.ThighLeft, new Vector3(-40f, 0f, 3f))
                .With(HumanoidBones.ThighRight, new Vector3(-36f, 0f, -3f))
                .With(HumanoidBones.ShinLeft, new Vector3(62f, 0f, 0f))
                .With(HumanoidBones.ShinRight, new Vector3(58f, 0f, 0f))
                .With(HumanoidBones.FootLeft, new Vector3(-4f, 0f, 0f))
                .With(HumanoidBones.FootRight, new Vector3(-2f, 0f, 0f));
        }

        /// <summary>
        /// Upright but not settled: shoulders off level, head turned a little too far, the right hand
        /// never quite closing. This is the pose the idle breathes around.
        /// </summary>
        public static HumanoidPose StandingUnsettled()
        {
            return new HumanoidPose()
                .With(HumanoidBones.Hips, new Vector3(0f, 0f, 1.2f))
                .With(HumanoidBones.Spine, new Vector3(1.5f, 0f, -1f))
                .With(HumanoidBones.Chest, new Vector3(-1f, 0f, 1.2f))
                .With(HumanoidBones.Neck, new Vector3(2f, -4f, 0f))
                .With(HumanoidBones.Head, new Vector3(-1f, -7f, 3.2f))
                .With(HumanoidBones.ShoulderLeft, new Vector3(0f, 0f, -3f))
                .With(HumanoidBones.ShoulderRight, new Vector3(0f, 0f, 1.5f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(-3f, 0f, 7.5f))
                .With(HumanoidBones.UpperArmRight, new Vector3(2f, 0f, -5f))
                .With(HumanoidBones.ForearmLeft, new Vector3(-14f, 0f, 0f))
                .With(HumanoidBones.ForearmRight, new Vector3(-9f, 0f, 0f))
                .With(HumanoidBones.HandLeft, new Vector3(0f, 0f, -4f))
                .With(HumanoidBones.HandRight, new Vector3(6f, 0f, 2f))
                .With(HumanoidBones.ThighLeft, new Vector3(-2f, 0f, 2f))
                .With(HumanoidBones.ThighRight, new Vector3(3f, 0f, -1.5f))
                .With(HumanoidBones.ShinLeft, new Vector3(3f, 0f, 0f))
                .With(HumanoidBones.ShinRight, new Vector3(1f, 0f, 0f));
        }

        /// <summary>
        /// One half of a walk cycle: left leg forward and straight, right leg back and bent, the torso
        /// counter-rotated, the arms opposing the legs. The head stays level, because a walk is below
        /// the neck.
        /// </summary>
        public static HumanoidPose WalkContactLeft()
        {
            return new HumanoidPose()
                .WithRootHeight(-0.049f)
                .With(HumanoidBones.Hips, new Vector3(0f, 3f, 0f))
                .With(HumanoidBones.Spine, new Vector3(3f, -3f, 0f))
                .With(HumanoidBones.Chest, new Vector3(1f, -5f, 0f))
                .With(HumanoidBones.Neck, new Vector3(-2f, 2f, 0f))
                .With(HumanoidBones.Head, new Vector3(-1f, 4f, 0.5f))
                .With(HumanoidBones.ThighLeft, new Vector3(-22f, 0f, 2f))
                .With(HumanoidBones.ShinLeft, new Vector3(4f, 0f, 0f))
                .With(HumanoidBones.FootLeft, new Vector3(10f, 0f, 0f))
                .With(HumanoidBones.ThighRight, new Vector3(12f, 0f, -2f))
                .With(HumanoidBones.ShinRight, new Vector3(16f, 0f, 0f))
                .With(HumanoidBones.FootRight, new Vector3(-40f, 0f, 0f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(14f, 0f, 4f))
                .With(HumanoidBones.ForearmLeft, new Vector3(-10f, 0f, 0f))
                .With(HumanoidBones.UpperArmRight, new Vector3(-16f, 0f, -4f))
                .With(HumanoidBones.ForearmRight, new Vector3(-16f, 0f, 0f));
        }

        /// <summary>Passing position: the right leg swings through under the body, the body at its highest.</summary>
        public static HumanoidPose WalkPassRight()
        {
            return new HumanoidPose()
                .WithRootHeight(-0.003f)
                .With(HumanoidBones.Hips, new Vector3(0f, 0f, -1.2f))
                .With(HumanoidBones.Spine, new Vector3(3f, 0f, 0.6f))
                .With(HumanoidBones.Chest, new Vector3(1f, 0f, 0f))
                .With(HumanoidBones.Neck, new Vector3(-2f, 0f, 0f))
                .With(HumanoidBones.Head, new Vector3(-1f, -1f, -0.4f))
                .With(HumanoidBones.ThighLeft, new Vector3(-4f, 0f, 2f))
                .With(HumanoidBones.ShinLeft, new Vector3(2f, 0f, 0f))
                .With(HumanoidBones.FootLeft, new Vector3(0f, 0f, 0f))
                .With(HumanoidBones.ThighRight, new Vector3(-6f, 0f, -2f))
                .With(HumanoidBones.ShinRight, new Vector3(42f, 0f, 0f))
                .With(HumanoidBones.FootRight, new Vector3(-18f, 0f, 0f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(2f, 0f, 4f))
                .With(HumanoidBones.ForearmLeft, new Vector3(-12f, 0f, 0f))
                .With(HumanoidBones.UpperArmRight, new Vector3(-2f, 0f, -4f))
                .With(HumanoidBones.ForearmRight, new Vector3(-12f, 0f, 0f));
        }

        /// <summary>The mirror of <see cref="WalkContactLeft"/>: right leg forward. Names are the same bones.</summary>
        public static HumanoidPose WalkContactRight()
        {
            return new HumanoidPose()
                .WithRootHeight(-0.049f)
                .With(HumanoidBones.Hips, new Vector3(0f, -3f, 0f))
                .With(HumanoidBones.Spine, new Vector3(3f, 3f, 0f))
                .With(HumanoidBones.Chest, new Vector3(1f, 5f, 0f))
                .With(HumanoidBones.Neck, new Vector3(-2f, -2f, 0f))
                .With(HumanoidBones.Head, new Vector3(-1f, -4f, -0.5f))
                .With(HumanoidBones.ThighRight, new Vector3(-22f, 0f, -2f))
                .With(HumanoidBones.ShinRight, new Vector3(4f, 0f, 0f))
                .With(HumanoidBones.FootRight, new Vector3(10f, 0f, 0f))
                .With(HumanoidBones.ThighLeft, new Vector3(12f, 0f, 2f))
                .With(HumanoidBones.ShinLeft, new Vector3(16f, 0f, 0f))
                .With(HumanoidBones.FootLeft, new Vector3(-40f, 0f, 0f))
                .With(HumanoidBones.UpperArmRight, new Vector3(14f, 0f, -4f))
                .With(HumanoidBones.ForearmRight, new Vector3(-10f, 0f, 0f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(-16f, 0f, 4f))
                .With(HumanoidBones.ForearmLeft, new Vector3(-16f, 0f, 0f));
        }

        /// <summary>The mirror of <see cref="WalkPassRight"/>: the left leg swings through.</summary>
        public static HumanoidPose WalkPassLeft()
        {
            return new HumanoidPose()
                .WithRootHeight(-0.003f)
                .With(HumanoidBones.Hips, new Vector3(0f, 0f, 1.2f))
                .With(HumanoidBones.Spine, new Vector3(3f, 0f, -0.6f))
                .With(HumanoidBones.Chest, new Vector3(1f, 0f, 0f))
                .With(HumanoidBones.Neck, new Vector3(-2f, 0f, 0f))
                .With(HumanoidBones.Head, new Vector3(-1f, 1f, 0.4f))
                .With(HumanoidBones.ThighRight, new Vector3(-4f, 0f, -2f))
                .With(HumanoidBones.ShinRight, new Vector3(2f, 0f, 0f))
                .With(HumanoidBones.FootRight, new Vector3(0f, 0f, 0f))
                .With(HumanoidBones.ThighLeft, new Vector3(-6f, 0f, 2f))
                .With(HumanoidBones.ShinLeft, new Vector3(42f, 0f, 0f))
                .With(HumanoidBones.FootLeft, new Vector3(-18f, 0f, 0f))
                .With(HumanoidBones.UpperArmRight, new Vector3(2f, 0f, -4f))
                .With(HumanoidBones.ForearmRight, new Vector3(-12f, 0f, 0f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(-2f, 0f, 4f))
                .With(HumanoidBones.ForearmLeft, new Vector3(-12f, 0f, 0f));
        }

        /// <summary>
        /// Landed on the container: knees deep, one hand down on the steel, head already turning to see
        /// where the drop came from. This is the pose a successful body check buys — a landing you walk
        /// out of. Root offset puts the soles on the container's lid.
        /// </summary>
        public static HumanoidPose LandingBrace()
        {
            return new HumanoidPose()
                .WithRootOffset(0f, -0.52f, 0.06f)
                .With(HumanoidBones.Hips, new Vector3(6f, 0f, 2f))
                .With(HumanoidBones.Spine, new Vector3(20f, 0f, 0f))
                .With(HumanoidBones.Chest, new Vector3(10f, 4f, 0f))
                .With(HumanoidBones.Neck, new Vector3(-16f, 0f, 0f))
                .With(HumanoidBones.Head, new Vector3(-22f, 24f, 4f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(-34f, 0f, 26f))
                .With(HumanoidBones.ForearmLeft, new Vector3(-58f, 0f, 0f))
                .With(HumanoidBones.UpperArmRight, new Vector3(-64f, 0f, -10f))
                .With(HumanoidBones.ForearmRight, new Vector3(-24f, 0f, 0f))
                .With(HumanoidBones.HandRight, new Vector3(-28f, 0f, 0f))
                .With(HumanoidBones.ThighLeft, new Vector3(-72f, 0f, 9f))
                .With(HumanoidBones.ThighRight, new Vector3(-58f, 0f, -7f))
                .With(HumanoidBones.ShinLeft, new Vector3(96f, 0f, 0f))
                .With(HumanoidBones.ShinRight, new Vector3(78f, 0f, 0f))
                .With(HumanoidBones.FootLeft, new Vector3(-24f, 0f, 0f))
                .With(HumanoidBones.FootRight, new Vector3(-20f, 0f, 0f));
        }

        /// <summary>
        /// Landed badly: face down on the lid, legs still hanging over the edge, one arm folded under
        /// the body, neck taking the last of the fall. The pose has to say "hurt" without a sound effect,
        /// so the asymmetry is violent: shoulders twisted, one knee bent and one straight, head down.
        /// </summary>
        public static HumanoidPose CrashOnContainer()
        {
            return new HumanoidPose()
                .WithRootOffset(0f, -0.44f, 0.18f)
                .With(HumanoidBones.Hips, new Vector3(4f, 0f, -6f))
                .With(HumanoidBones.Spine, new Vector3(74f, 6f, 0f))
                .With(HumanoidBones.Chest, new Vector3(16f, -10f, 4f))
                .With(HumanoidBones.Neck, new Vector3(-34f, 0f, 0f))
                .With(HumanoidBones.Head, new Vector3(-16f, -46f, 10f))
                .With(HumanoidBones.ShoulderLeft, new Vector3(0f, 0f, -10f))
                .With(HumanoidBones.ShoulderRight, new Vector3(0f, 0f, 6f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(-84f, 0f, 34f))
                .With(HumanoidBones.ForearmLeft, new Vector3(-96f, 0f, 0f))
                .With(HumanoidBones.UpperArmRight, new Vector3(-38f, 0f, -22f))
                .With(HumanoidBones.ForearmRight, new Vector3(-70f, 0f, 0f))
                .With(HumanoidBones.HandLeft, new Vector3(-20f, 0f, 0f))
                .With(HumanoidBones.HandRight, new Vector3(14f, 0f, 0f))
                .With(HumanoidBones.ThighLeft, new Vector3(-84f, 0f, 12f))
                .With(HumanoidBones.ShinLeft, new Vector3(52f, 0f, 0f))
                .With(HumanoidBones.ThighRight, new Vector3(-12f, 0f, -8f))
                .With(HumanoidBones.ShinRight, new Vector3(22f, 0f, 0f))
                .With(HumanoidBones.FootLeft, new Vector3(-16f, 0f, 0f))
                .With(HumanoidBones.FootRight, new Vector3(-6f, 0f, 0f));
        }

        /// <summary>
        /// N at the podium, three seconds into a class she is not holding. Arms forward and low because
        /// the podium top is at 1.02 m and the hands rest on it; the spine tips forward 4 degrees; the
        /// head is already turned a little toward the door, the way a substitute keeps checking whether
        /// anyone else is coming. This is the base the podium loop breathes around.
        /// </summary>
        public static HumanoidPose TeacherAtPodium()
        {
            // Root at zero on purpose: her feet are on the floor and the podium's near edge is 0.44 m
            // in front of them (stand z 10.74, podium back face z 10.30). The wrist lands 0.442 m in
            // front of the spine, which is exactly that edge, so the pose only has to lean. A negative
            // root offset here would sink her boots into the floor for nothing.
            return new HumanoidPose()
                .WithRootOffset(0f, 0f, 0f)
                .With(HumanoidBones.Hips, new Vector3(2f, 0f, 0.8f))
                .With(HumanoidBones.Spine, new Vector3(4f, 0f, 0.6f))
                .With(HumanoidBones.Chest, new Vector3(2f, 0f, -0.4f))
                .With(HumanoidBones.Neck, new Vector3(6f, -5f, 0f))
                .With(HumanoidBones.Head, new Vector3(4f, -6f, 1.4f))
                .With(HumanoidBones.ShoulderLeft, new Vector3(0f, 0f, -2f))
                .With(HumanoidBones.ShoulderRight, new Vector3(0f, 0f, 2f))
                // Arms solved, not eyeballed: upper arm -52 / forearm -60 puts the wrist at 1.164 m and
                // 0.44 m in front of the spine, so the heel of the hand rests on the podium top (1.02 m
                // surface, 1.09 m with the cap) instead of hovering over it or sinking into it.
                .With(HumanoidBones.UpperArmLeft, new Vector3(-50f, 0f, 9f))
                .With(HumanoidBones.UpperArmRight, new Vector3(-52f, 0f, -11f))
                .With(HumanoidBones.ForearmLeft, new Vector3(-58f, 0f, 0f))
                .With(HumanoidBones.ForearmRight, new Vector3(-60f, 0f, 0f))
                .With(HumanoidBones.HandLeft, new Vector3(-10f, 0f, 0f))
                .With(HumanoidBones.HandRight, new Vector3(-10f, 0f, 0f))
                .With(HumanoidBones.ThighLeft, new Vector3(-3f, 0f, 1.5f))
                .With(HumanoidBones.ThighRight, new Vector3(1f, 0f, -1.5f))
                .With(HumanoidBones.ShinLeft, new Vector3(2f, 0f, 0f))
                .With(HumanoidBones.ShinRight, new Vector3(1f, 0f, 0f));
        }

        /// <summary>Q's resting stance: too straight, arms flat to the body, head level and unblinking.</summary>
        public static HumanoidPose ClassmateStill()
        {
            return new HumanoidPose()
                .With(HumanoidBones.Head, new Vector3(0f, 0f, 0f))
                .With(HumanoidBones.Neck, new Vector3(0f, 0f, 0f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(0f, 0f, 1.5f))
                .With(HumanoidBones.UpperArmRight, new Vector3(0f, 0f, -1.5f))
                .With(HumanoidBones.ForearmLeft, new Vector3(-2f, 0f, 0f))
                .With(HumanoidBones.ForearmRight, new Vector3(-2f, 0f, 0f));
        }
    }
}
