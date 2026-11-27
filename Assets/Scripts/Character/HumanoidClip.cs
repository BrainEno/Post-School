using System;
using System.Collections.Generic;
using UnityEngine;

namespace PostSchool.Character
{
    /// <summary>One authored moment: where the pose is, how long it takes to get there, how to interpolate.</summary>
    [Serializable]
    public sealed class PoseKey
    {
        public float time;
        public HumanoidPose pose;

        public PoseKey(float time, HumanoidPose pose)
        {
            this.time = time;
            this.pose = pose ?? throw new ArgumentNullException(nameof(pose));
        }
    }

    /// <summary>
    /// A hand-authored clip over <see cref="HumanoidPose"/> keys. No external animation data, no
    /// Animator Controller, no avatar: the clip evaluates to a pose and the player writes it to bones.
    /// That keeps everything readable, testable and identical for every figure on the same skeleton.
    /// </summary>
    [Serializable]
    public sealed class HumanoidClip
    {
        private readonly List<PoseKey> _keys = new List<PoseKey>();

        public HumanoidClip(string name, bool loops = false)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A clip needs a name.", nameof(name));
            Name = name;
            Loops = loops;
        }

        public string Name { get; }

        public bool Loops { get; }

        public int KeyCount => _keys.Count;

        public float Duration => _keys.Count == 0 ? 0f : _keys[_keys.Count - 1].time;

        public IReadOnlyList<PoseKey> Keys => _keys;

        public HumanoidClip Add(float time, HumanoidPose pose)
        {
            if (time < 0f) throw new ArgumentOutOfRangeException(nameof(time), time, "Key times cannot be negative.");
            if (_keys.Count > 0 && time <= _keys[_keys.Count - 1].time)
            {
                throw new ArgumentException($"Key at {time:0.00}s must come after {_keys[_keys.Count - 1].time:0.00}s in clip '{Name}'.", nameof(time));
            }

            _keys.Add(new PoseKey(time, pose ?? throw new ArgumentNullException(nameof(pose))));
            return this;
        }

        /// <summary>
        /// Samples the clip. Easing is SmoothStep by default because every clip here is acting, not
        /// mechanical motion; a linear clip reads as a machine, which is wrong for both characters.
        /// </summary>
        public HumanoidPose Sample(float seconds, bool linear = false)
        {
            if (_keys.Count == 0) throw new InvalidOperationException($"Clip '{Name}' has no keys.");

            float time = Loops && Duration > 0f ? Mathf.Repeat(seconds, Duration) : Mathf.Clamp(seconds, 0f, Duration);

            if (_keys.Count == 1 || time <= _keys[0].time) return _keys[0].pose.Clone();
            if (time >= Duration) return _keys[_keys.Count - 1].pose.Clone();

            for (int i = 0; i < _keys.Count - 1; i++)
            {
                PoseKey a = _keys[i];
                PoseKey b = _keys[i + 1];
                if (time < a.time || time > b.time) continue;

                float span = b.time - a.time;
                float t = span <= 0f ? 1f : (time - a.time) / span;
                if (!linear) t = Mathf.SmoothStep(0f, 1f, t);
                return HumanoidPose.Lerp(a.pose, b.pose, t);
            }

            return _keys[_keys.Count - 1].pose.Clone();
        }

        public bool Validate(ProceduralSkeleton skeleton, out string error)
        {
            if (_keys.Count == 0)
            {
                error = $"Clip '{Name}' has no keys.";
                return false;
            }

            foreach (PoseKey key in _keys)
            {
                if (!key.pose.Validate(skeleton, out string poseError))
                {
                    error = $"Clip '{Name}' at {key.time:0.00}s: {poseError}";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }

    /// <summary>
    /// The prototype's clip library. Both characters play the same clips; their proportions do the
    /// differentiating. Timings are authored to read at a 3 m third-person camera distance.
    /// </summary>
    public static class HumanoidClips
    {
        public const string WakeName = "WakeFromDesk";
        public const string IdleName = "StandingIdle";
        public const string QIdleName = "ClassmateIdle";
        public const string WalkName = "WalkCycle";
        public const string FallName = "FallFromTheCurtain";
        public const string CrashName = "CrashLanding";

        /// <summary>
        /// The opening: asleep, head lifts, weight comes back over the feet, they stand and stop
        /// slightly off-balance. About 3.4 s, which is long enough to read as waking up and short
        /// enough not to make the player wait.
        /// </summary>
        public static HumanoidClip WakeFromDesk()
        {
            return new HumanoidClip(WakeName)
                .Add(0f, HumanoidPoses.AsleepOnDesk())
                .Add(0.45f, HumanoidPoses.AsleepOnDesk())
                .Add(1.15f, HumanoidPoses.LiftingHead())
                .Add(1.95f, HumanoidPoses.StandingUp())
                .Add(3.1f, HumanoidPoses.StandingUnsettled())
                .Add(3.4f, HumanoidPoses.StandingUnsettled());
        }

        /// <summary>
        /// A 6 s loop: breath, a head that turns to look at something off to one side, and a weight
        /// shift that never resolves. Loops seamlessly because the first and last poses match.
        /// </summary>
        public static HumanoidClip StandingIdle()
        {
            HumanoidPose a = HumanoidPoses.StandingUnsettled();
            HumanoidPose b = a.Clone().With(HumanoidBones.Head, new Vector3(-2f, 14f, 4f)).With(HumanoidBones.Chest, new Vector3(0f, 0f, 2f));
            HumanoidPose c = a.Clone().With(HumanoidBones.Head, new Vector3(1f, -16f, 2f)).With(HumanoidBones.Neck, new Vector3(3f, -6f, 0f));
            HumanoidPose d = a.Clone().With(HumanoidBones.Hips, new Vector3(0f, 0f, 2.4f)).With(HumanoidBones.Spine, new Vector3(2f, 0f, -1.6f));

            return new HumanoidClip(IdleName, loops: true)
                .Add(0f, a)
                .Add(1.6f, b)
                .Add(3.1f, d)
                .Add(4.4f, c)
                .Add(5.5f, a.Clone())
                .Add(6f, a);
        }

        /// <summary>
        /// Q's loop is much smaller than the protagonist's: the head turns in steps, not in a sweep,
        /// and the body does not breathe. Shorter cycle, so the stillness is noticeable.
        /// </summary>
        public static HumanoidClip ClassmateIdle()
        {
            HumanoidPose rest = HumanoidPoses.ClassmateStill();
            HumanoidPose turned = rest.Clone().With(HumanoidBones.Head, new Vector3(0f, 22f, 0f)).With(HumanoidBones.Neck, new Vector3(0f, 5f, 0f));
            HumanoidPose other = rest.Clone().With(HumanoidBones.Head, new Vector3(0f, -24f, 0f));

            return new HumanoidClip(QIdleName, loops: true)
                .Add(0f, rest)
                .Add(2.2f, rest)
                .Add(2.35f, turned)
                .Add(4.6f, turned)
                .Add(4.75f, other)
                .Add(6.9f, other)
                .Add(7.05f, rest);
        }

        /// <summary>
        /// A 1.1 s walk cycle: contact, pass, contact, pass, back to the first contact so it loops. The
        /// root does not travel; locomotion is the caller's job, the clip only owns the legs.
        /// </summary>
        public static HumanoidClip ClassmateWalk()
        {
            HumanoidPose contactLeft = HumanoidPoses.WalkContactLeft();

            return new HumanoidClip(WalkName, loops: true)
                .Add(0f, contactLeft)
                .Add(0.275f, HumanoidPoses.WalkPassRight())
                .Add(0.55f, HumanoidPoses.WalkContactRight())
                .Add(0.825f, HumanoidPoses.WalkPassLeft())
                .Add(1.1f, contactLeft);
        }

        /// <summary>
        /// The fall, from stepping through the curtain to the container's lid. Three beats: the step
        /// out (nothing under the foot), the drop (arms up, legs trailing, the body twisting to look
        /// down at what it is falling onto), and the landing brace.
        ///
        /// The clip deliberately carries <b>no world travel</b>. Root offsets in this rig are pose-space
        /// (they lower a hips, they do not move a building), so a clip that walked itself six metres down
        /// would also have to be re-anchored by whoever plays it — and the moment two systems both move
        /// the body, the fall lands at twice the distance. The caller moves the transform; this moves the
        /// body inside it. <paramref name="dropMetres"/> only sets how long the drop reads.
        /// </summary>
        public static HumanoidClip FallFromTheCurtain(float dropMetres)
        {
            if (dropMetres <= 0f) throw new ArgumentOutOfRangeException(nameof(dropMetres), dropMetres, "A fall needs a drop.");

            HumanoidPose step = new HumanoidPose()
                .WithRootOffset(0f, 0f, 0.34f)
                .With(HumanoidBones.Hips, new Vector3(-2f, 0f, 1f))
                .With(HumanoidBones.Spine, new Vector3(6f, 0f, 0f))
                .With(HumanoidBones.Neck, new Vector3(-6f, 0f, 0f))
                .With(HumanoidBones.Head, new Vector3(8f, 6f, 2f))
                .With(HumanoidBones.ThighLeft, new Vector3(-34f, 0f, 3f))
                .With(HumanoidBones.ShinLeft, new Vector3(10f, 0f, 0f))
                .With(HumanoidBones.ThighRight, new Vector3(14f, 0f, -3f))
                .With(HumanoidBones.ShinRight, new Vector3(18f, 0f, 0f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(18f, 0f, 12f))
                .With(HumanoidBones.UpperArmRight, new Vector3(18f, 0f, -12f));

            // Falling: a body does not flail, it hardens. Arms up, legs trailing, spine arched back.
            HumanoidPose falling = new HumanoidPose()
                .WithRootOffset(0f, -0.10f, 0.42f)
                .With(HumanoidBones.Hips, new Vector3(-14f, 0f, 0f))
                .With(HumanoidBones.Spine, new Vector3(-10f, 0f, 0f))
                .With(HumanoidBones.Chest, new Vector3(-8f, 0f, 0f))
                .With(HumanoidBones.Neck, new Vector3(10f, 0f, 0f))
                .With(HumanoidBones.Head, new Vector3(16f, 14f, 0f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(-46f, 0f, 62f))
                .With(HumanoidBones.ForearmLeft, new Vector3(-30f, 0f, 0f))
                .With(HumanoidBones.UpperArmRight, new Vector3(-52f, 0f, -58f))
                .With(HumanoidBones.ForearmRight, new Vector3(-24f, 0f, 0f))
                .With(HumanoidBones.ThighLeft, new Vector3(8f, 0f, 6f))
                .With(HumanoidBones.ShinLeft, new Vector3(28f, 0f, 0f))
                .With(HumanoidBones.ThighRight, new Vector3(-16f, 0f, -6f))
                .With(HumanoidBones.ShinRight, new Vector3(46f, 0f, 0f));

            // The brace, then settle: the arrival is one pose, the recovery another.
            HumanoidPose brace = HumanoidPoses.LandingBrace();
            HumanoidPose settle = brace.Clone()
                .WithRootOffset(0f, -0.28f, 0.02f)
                .With(HumanoidBones.Spine, new Vector3(8f, 0f, 0f))
                .With(HumanoidBones.ThighLeft, new Vector3(-26f, 0f, 6f))
                .With(HumanoidBones.ThighRight, new Vector3(-14f, 0f, -5f))
                .With(HumanoidBones.ShinLeft, new Vector3(42f, 0f, 0f))
                .With(HumanoidBones.ShinRight, new Vector3(26f, 0f, 0f))
                .With(HumanoidBones.Head, new Vector3(-8f, 30f, 2f));

            // The impact key sits later the further there is to fall: 0.62 s at one metre, ~1.1 s at six.
            float impact = 0.62f + Mathf.Clamp(dropMetres - 1f, 0f, 8f) * 0.075f;
            return new HumanoidClip(FallName)
                .Add(0f, step)
                .Add(0.18f, falling)
                .Add(impact, HumanoidPoses.LandingBrace())
                .Add(impact + 0.22f, HumanoidPoses.LandingBrace())
                .Add(impact + 0.62f, settle)
                .Add(impact + 1.1f, HumanoidPoses.StandingUnsettled());
        }

        /// <summary>
        /// The failed version: the same drop, but the last third is a body arriving wrong. The keys are
        /// deliberately late (the arms never get up in time) and the final pose holds, because nothing
        /// here gets back on its feet by itself.
        /// </summary>
        public static HumanoidClip CrashLanding(float dropMetres)
        {
            if (dropMetres <= 0f) throw new ArgumentOutOfRangeException(nameof(dropMetres), dropMetres, "A fall needs a drop.");

            HumanoidPose falling = new HumanoidPose()
                .WithRootOffset(0f, -0.08f, 0.30f)
                .With(HumanoidBones.Hips, new Vector3(-6f, 0f, -3f))
                .With(HumanoidBones.Spine, new Vector3(14f, 0f, 0f))
                .With(HumanoidBones.Neck, new Vector3(-6f, 0f, 0f))
                .With(HumanoidBones.Head, new Vector3(-14f, 10f, 6f))
                .With(HumanoidBones.UpperArmLeft, new Vector3(-20f, 0f, 40f))
                .With(HumanoidBones.UpperArmRight, new Vector3(-14f, 0f, -30f))
                .With(HumanoidBones.ThighLeft, new Vector3(-30f, 0f, 8f))
                .With(HumanoidBones.ShinLeft, new Vector3(40f, 0f, 0f))
                .With(HumanoidBones.ThighRight, new Vector3(-10f, 0f, -8f))
                .With(HumanoidBones.ShinRight, new Vector3(30f, 0f, 0f));

            float impact = 0.30f + Mathf.Clamp(dropMetres - 1f, 0f, 8f) * 0.09f;
            return new HumanoidClip(CrashName)
                .Add(0f, falling)
                .Add(impact * 0.55f, falling)
                .Add(impact, HumanoidPoses.CrashOnContainer())
                .Add(impact + 0.26f, HumanoidPoses.CrashOnContainer())
                .Add(impact + 1.9f, HumanoidPoses.CrashOnContainer());
        }

        public static IReadOnlyList<HumanoidClip> All()
        {
            return new[]
            {
                WakeFromDesk(), StandingIdle(), ClassmateIdle(), ClassmateWalk(),
                FallFromTheCurtain(6.4f), CrashLanding(6.4f),
            };
        }
    }
}
