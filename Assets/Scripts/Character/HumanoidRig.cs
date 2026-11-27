using System;
using System.Collections.Generic;
using UnityEngine;

namespace PostSchool.Character
{
    /// <summary>The bone names every procedural humanoid uses. Keep this list frozen: poses, clips and tests index by name.</summary>
    public static class HumanoidBones
    {
        public const string Hips = "Hips";
        public const string Spine = "Spine";
        public const string Chest = "Chest";
        public const string Neck = "Neck";
        public const string Head = "Head";
        public const string ShoulderLeft = "Shoulder_L";
        public const string UpperArmLeft = "UpperArm_L";
        public const string ForearmLeft = "Forearm_L";
        public const string HandLeft = "Hand_L";
        public const string ShoulderRight = "Shoulder_R";
        public const string UpperArmRight = "UpperArm_R";
        public const string ForearmRight = "Forearm_R";
        public const string HandRight = "Hand_R";
        public const string ThighLeft = "Thigh_L";
        public const string ShinLeft = "Shin_L";
        public const string FootLeft = "Foot_L";
        public const string ThighRight = "Thigh_R";
        public const string ShinRight = "Shin_R";
        public const string FootRight = "Foot_R";

        /// <summary>Root-to-leaf order. A skeleton is built in exactly this order.</summary>
        public static readonly string[] All =
        {
            Hips,
            Spine,
            Chest,
            Neck,
            Head,
            ShoulderLeft, UpperArmLeft, ForearmLeft, HandLeft,
            ShoulderRight, UpperArmRight, ForearmRight, HandRight,
            ThighLeft, ShinLeft, FootLeft,
            ThighRight, ShinRight, FootRight,
        };

        /// <summary>Parent of each bone, by name; null means the skeleton root.</summary>
        public static string ParentOf(string bone)
        {
            switch (bone)
            {
                case Hips: return null;
                case Spine: return Hips;
                case Chest: return Spine;
                case Neck: return Chest;
                case Head: return Neck;
                case ShoulderLeft:
                case ShoulderRight: return Chest;
                case UpperArmLeft:
                case UpperArmRight: return bone == UpperArmLeft ? ShoulderLeft : ShoulderRight;
                case ForearmLeft: return UpperArmLeft;
                case ForearmRight: return UpperArmRight;
                case HandLeft: return ForearmLeft;
                case HandRight: return ForearmRight;
                case ThighLeft:
                case ThighRight: return Hips;
                case ShinLeft: return ThighLeft;
                case ShinRight: return ThighRight;
                case FootLeft: return ShinLeft;
                case FootRight: return ShinRight;
                default: throw new ArgumentOutOfRangeException(nameof(bone), bone, "Unknown humanoid bone.");
            }
        }

        public static bool IsLeft(string bone)
        {
            return bone.EndsWith("_L", StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// A humanoid skeleton described as data: a joint per bone, relative to its parent, in metres,
    /// with the figure standing at the origin and its feet on y = 0.
    ///
    /// The proportions are the character. Two presets that differ only in numbers are what make the
    /// protagonist read as an adult-young hybrid and Q read as something that was never a person:
    /// the skeleton is shared, so the same animation always fits both.
    /// </summary>
    [Serializable]
    public sealed class HumanoidProportions
    {
        /// <summary>Total standing height, crown to floor.</summary>
        public float height = 1.72f;

        /// <summary>Height of the ankle: the Foot bones sit here and the sole is this far below.</summary>
        public float ankleHeight = 0.07f;

        /// <summary>
        /// The pelvis. Every other bone is the joint at the start of its own segment, so the thigh
        /// bone hangs a short way below this (the hip joint proper), not at the knee.
        /// </summary>
        public Vector3 hips = new Vector3(0f, 0.94f, 0f);
        public Vector3 spine = new Vector3(0f, 0.16f, 0f);
        public Vector3 chest = new Vector3(0f, 0.17f, 0f);
        public Vector3 neck = new Vector3(0f, 0.15f, 0f);
        public Vector3 head = new Vector3(0f, 0.06f, 0f);

        public Vector3 shoulder = new Vector3(0.058f, 0.1f, 0f);
        public Vector3 upperArm = new Vector3(0.145f, 0f, 0f);
        public Vector3 forearm = new Vector3(0f, -0.28f, 0f);
        public Vector3 hand = new Vector3(0f, -0.25f, 0f);

        public Vector3 thigh = new Vector3(0.086f, -0.07f, 0f);
        public Vector3 shin = new Vector3(0f, -0.41f, 0f);

        /// <summary>Knee to ankle. The foot bone is the ankle joint; the sole hangs ankleHeight below it.</summary>
        public Vector3 foot = new Vector3(0f, -0.39f, 0f);

        /// <summary>Chin-to-crown length. This is the unit a "7 heads tall" figure is measured in.</summary>
        public float headLength = 0.245f;

        /// <summary>Half-width of the skull, front to back and side to side.</summary>
        public float headRadius = 0.088f;

        /// <summary>Depth of the ribcage, used by the mesh and by the silhouette checks.</summary>
        public float chestDepth = 0.115f;

        /// <summary>
        /// The protagonist: 7 heads tall, which is deliberately between a child (about 6) and an adult
        /// (about 7.5). Narrow shoulders (0.116 m between shoulder joints) on an adult ribcage, long
        /// adult forearms, and a head that is slightly too large for the shoulders it sits on.
        /// </summary>
        public static HumanoidProportions Protagonist()
        {
            return new HumanoidProportions
            {
                height = 1.72f,
                ankleHeight = 0.07f,
                hips = new Vector3(0f, 0.94f, 0f),
                spine = new Vector3(0f, 0.16f, 0f),
                chest = new Vector3(0f, 0.17f, 0f),
                neck = new Vector3(0f, 0.15f, 0f),
                head = new Vector3(0f, 0.06f, 0f),
                shoulder = new Vector3(0.058f, 0.10f, 0f),
                upperArm = new Vector3(0.145f, 0f, 0f),
                forearm = new Vector3(0f, -0.28f, 0f),
                hand = new Vector3(0f, -0.25f, 0f),
                thigh = new Vector3(0.086f, -0.07f, 0f),
                shin = new Vector3(0f, -0.41f, 0f),
                foot = new Vector3(0f, -0.39f, 0f),
                headLength = 0.245f,
                headRadius = 0.088f,
                chestDepth = 0.115f,
            };
        }

        /// <summary>
        /// Q, the classmate: 6.8 heads, a longer neck, thinner forearms and a smaller skull. The
        /// differences from the protagonist are small on purpose. Q should read as a person who is
        /// almost correctly proportioned, because the wrongness should be in the face and the stillness,
        /// not in a cartoon silhouette.
        /// </summary>
        public static HumanoidProportions Classmate()
        {
            return new HumanoidProportions
            {
                height = 1.615f,
                ankleHeight = 0.065f,
                hips = new Vector3(0f, 0.86f, 0f),
                spine = new Vector3(0f, 0.15f, 0f),
                chest = new Vector3(0f, 0.16f, 0f),
                neck = new Vector3(0f, 0.15f, 0f),
                head = new Vector3(0f, 0.06f, 0f),
                shoulder = new Vector3(0.052f, 0.09f, 0f),
                upperArm = new Vector3(0.128f, 0f, 0f),
                forearm = new Vector3(0f, -0.255f, 0f),
                hand = new Vector3(0f, -0.225f, 0f),
                thigh = new Vector3(0.078f, -0.06f, 0f),
                shin = new Vector3(0f, -0.375f, 0f),
                foot = new Vector3(0f, -0.36f, 0f),
                headLength = 0.235f,
                headRadius = 0.081f,
                chestDepth = 0.105f,
            };
        }

        /// <summary>How many head-lengths the figure is, crown to floor.</summary>
        public float HeadsTall => height / headLength;

        /// <summary>
        /// Local position, relative to the parent bone, for one bone. The skeleton is built from this
        /// table and nothing else, so changing a proportion moves the mesh and the bones together.
        /// </summary>
        public Vector3 LocalPosition(string bone)
        {
            switch (bone)
            {
                case HumanoidBones.Hips: return hips;
                case HumanoidBones.Spine: return spine;
                case HumanoidBones.Chest: return chest;
                case HumanoidBones.Neck: return neck;
                case HumanoidBones.Head: return head;
                case HumanoidBones.ShoulderLeft: return new Vector3(-shoulder.x, shoulder.y, shoulder.z);
                case HumanoidBones.ShoulderRight: return shoulder;
                case HumanoidBones.UpperArmLeft: return new Vector3(-upperArm.x, upperArm.y, upperArm.z);
                case HumanoidBones.UpperArmRight: return upperArm;
                case HumanoidBones.ForearmLeft:
                case HumanoidBones.ForearmRight: return forearm;
                case HumanoidBones.HandLeft:
                case HumanoidBones.HandRight: return hand;
                case HumanoidBones.ThighLeft: return new Vector3(-thigh.x, thigh.y, thigh.z);
                case HumanoidBones.ThighRight: return thigh;
                case HumanoidBones.ShinLeft:
                case HumanoidBones.ShinRight: return shin;
                case HumanoidBones.FootLeft:
                case HumanoidBones.FootRight: return foot;
                default: throw new ArgumentOutOfRangeException(nameof(bone), bone, "Unknown humanoid bone.");
            }
        }

        /// <summary>Absolute position of a bone in the rest pose, walking up the chain. Used by mesh building.</summary>
        public Vector3 RestPosition(string bone)
        {
            Vector3 position = Vector3.zero;
            string current = bone;
            int guard = 0;
            while (current != null)
            {
                position += LocalPosition(current);
                current = HumanoidBones.ParentOf(current);
                if (++guard > 32) throw new InvalidOperationException("Bone hierarchy has a cycle.");
            }

            return position;
        }

        /// <summary>
        /// Checks the proportion table is physically sane before anything builds from it: feet on the
        /// floor, a crown at the declared height, shoulders above hips, limbs hanging downward.
        /// </summary>
        public bool Validate(out string error)
        {
            if (height < 1.2f || height > 2.2f)
            {
                error = $"Figure height {height} m is outside the 1.2-2.2 m range this rig supports.";
                return false;
            }

            float ankle = RestPosition(HumanoidBones.FootLeft).y;
            if (Mathf.Abs(ankle - ankleHeight) > 0.02f)
            {
                error = $"Ankle sits at y = {ankle:0.000} m but ankleHeight is {ankleHeight:0.000} m.";
                return false;
            }

            float crown = RestPosition(HumanoidBones.Head).y + headLength;
            if (Mathf.Abs(crown - height) > 0.05f)
            {
                error = $"Head top is at {crown:0.000} m but the declared height is {height:0.000} m.";
                return false;
            }

            float shoulderY = RestPosition(HumanoidBones.UpperArmLeft).y;
            float hipY = RestPosition(HumanoidBones.ThighLeft).y;
            if (shoulderY <= hipY)
            {
                error = "Shoulders must sit above the hips.";
                return false;
            }

            if (forearm.y >= 0f || shin.y >= 0f || hand.y >= 0f)
            {
                error = "Arm and leg bones must hang downward (negative y offsets) in the rest pose.";
                return false;
            }

            float hipJoint = RestPosition(HumanoidBones.ThighLeft).y;
            float knee = RestPosition(HumanoidBones.ShinLeft).y;
            if (!(ankle < knee && knee < hipJoint))
            {
                error = $"Leg joints out of order: ankle {ankle:0.000} / knee {knee:0.000} / hip {hipJoint:0.000}.";
                return false;
            }

            if (foot.y >= 0f)
            {
                error = "The foot bone must hang downward from the knee (the foot bone is the ankle joint).";
                return false;
            }

            if (headLength <= 0f || headRadius <= 0f || chestDepth <= 0f)
            {
                error = "Head and chest dimensions must be positive.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>Distance between the two shoulder joints.</summary>
        public float ShoulderWidth => shoulder.x * 2f + upperArm.x * 2f;
    }

    /// <summary>Instantiates the bone transforms for a proportion table and hands back the skeleton.</summary>
    public static class HumanoidRig
    {
        public const string RootName = "Skeleton";

        /// <summary>
        /// Builds the bone hierarchy under a new "Skeleton" object. Bones start at identity rotation;
        /// the bind pose is therefore the rest pose, which is what the mesh is authored in.
        /// </summary>
        public static ProceduralSkeleton Create(Transform parent, HumanoidProportions proportions, out Transform skeletonRoot)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (proportions == null) throw new ArgumentNullException(nameof(proportions));
            if (!proportions.Validate(out string error))
            {
                throw new ArgumentException("Invalid proportions: " + error, nameof(proportions));
            }

            skeletonRoot = new GameObject(RootName).transform;
            skeletonRoot.SetParent(parent, false);
            skeletonRoot.localPosition = Vector3.zero;
            skeletonRoot.localRotation = Quaternion.identity;

            Dictionary<string, Transform> bones = new Dictionary<string, Transform>(HumanoidBones.All.Length);
            foreach (string boneName in HumanoidBones.All)
            {
                Transform bone = new GameObject(boneName).transform;
                string parentName = HumanoidBones.ParentOf(boneName);
                bone.SetParent(parentName == null ? skeletonRoot : bones[parentName], false);
                bone.localPosition = proportions.LocalPosition(boneName);
                bone.localRotation = Quaternion.identity;
                bone.localScale = Vector3.one;
                bones[boneName] = bone;
            }

            return new ProceduralSkeleton(HumanoidBones.All, skeletonRoot);
        }

        /// <summary>Puts every bone back to the rest rotation, so a pose can be applied from a known state.</summary>
        public static void ResetToRest(ProceduralSkeleton skeleton)
        {
            if (skeleton == null) throw new ArgumentNullException(nameof(skeleton));
            for (int i = 0; i < skeleton.BoneCount; i++)
            {
                skeleton.Bone(i).localRotation = Quaternion.identity;
            }
        }
    }
}
