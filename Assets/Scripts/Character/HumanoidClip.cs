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

        public static IReadOnlyList<HumanoidClip> All()
        {
            return new[] { WakeFromDesk(), StandingIdle(), ClassmateIdle(), ClassmateWalk() };
        }
    }
}
