using System;
using UnityEngine;

namespace PostSchool.Character
{
    /// <summary>
    /// Plays <see cref="HumanoidClip"/>s on a <see cref="SkinnedFigure"/>.
    ///
    /// This replaces the old "rotate five pivots" approach: it writes all nineteen bones from a sampled
    /// pose, cross-fades between clips, and runs on unscaled time so the opening still animates while
    /// the dialogue system holds Time.timeScale at zero.
    ///
    /// It deliberately does not use Unity's Animator/AnimatorOverrideController. These clips are a few
    /// kB of authored rotations; an Animator asset per character would add binary assets, an avatar
    /// and an import pipeline for no gain at this scale.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HumanoidAnimator : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _blendSeconds = 0.25f;

        private SkinnedFigure _figure;
        private HumanoidClip _current;
        private float _currentTime;
        private bool _playing;

        private HumanoidClip _previous;
        private HumanoidPose _previousPose;
        private float _blendElapsed;

        /// <summary>Raised once when a non-looping clip finishes.</summary>
        public event Action<HumanoidClip> ClipFinished;

        public HumanoidClip CurrentClip => _current;

        public bool IsPlaying => _playing;

        public void Initialize(SkinnedFigure figure)
        {
            _figure = figure ?? throw new ArgumentNullException(nameof(figure));
            CaptureBaseHeight();
        }

        /// <summary>Starts a clip from time zero, cross-fading from whatever is on screen.</summary>
        public void Play(HumanoidClip clip, float startTime = 0f)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            if (_figure == null) throw new InvalidOperationException("Initialize must run before Play.");

            if (_current != null)
            {
                _previous = _current;
                _previousPose = _current.Sample(_currentTime, linear: false);
                _blendElapsed = 0f;
            }

            _current = clip;
            _currentTime = Mathf.Max(0f, startTime);
            _playing = true;
            Evaluate();
        }

        /// <summary>Stops playback and leaves the figure on its current pose.</summary>
        public void Stop()
        {
            _playing = false;
            _previous = null;
        }

        /// <summary>Applies a single pose immediately, with no clip. Used for posed shots and tests.</summary>
        public void ApplyPose(HumanoidPose pose)
        {
            if (pose == null) throw new ArgumentNullException(nameof(pose));
            if (_figure == null) throw new InvalidOperationException("Initialize must run before ApplyPose.");

            _playing = false;
            _current = null;
            _previous = null;

            if (!_baseHeightCaptured) CaptureBaseHeight();

            pose.ApplyClean(_figure.Skeleton);
            ApplyRootOffset(pose);
        }

        /// <summary>
        /// Moves the figure by the pose's root offset, measured from where it was placed. Poses may move
        /// forward as well as up (lying on a desk), so the baseline position is a full vector, not a height.
        /// </summary>
        private void ApplyRootOffset(HumanoidPose pose)
        {
            _figure.Root.localPosition = BasePosition + pose.RootOffset;
        }

        /// <summary>The point the figure was placed at, before any pose offset is applied.</summary>
        /// <summary>Y of <see cref="BasePosition"/>, for callers that only care about height.</summary>
        public float BaseHeight => BasePosition.y;

        /// <summary>
        /// Where the figure was placed before it was animated. Captured once from the transform, so a
        /// pose offset is always measured from the authored position and never accumulates.
        /// </summary>
        public void CaptureBaseHeight()
        {
            BasePosition = _figure != null ? _figure.Root.localPosition : Vector3.zero;
            _baseHeightCaptured = true;
        }

        /// <summary>The figure's authored position; poses offset from here.</summary>
        public Vector3 BasePosition { get; private set; }

        private bool _baseHeightCaptured;

        private void Update()
        {
            if (!_playing || _current == null || _figure == null) return;

            if (!_baseHeightCaptured) CaptureBaseHeight();
            _currentTime += Time.unscaledDeltaTime;
            if (_blendElapsed < _blendSeconds) _blendElapsed += Time.unscaledDeltaTime;

            if (!_current.Loops && _currentTime >= _current.Duration)
            {
                _currentTime = _current.Duration;
                _playing = false;
                Evaluate();
                ClipFinished?.Invoke(_current);
                return;
            }

            Evaluate();
        }

        private void Evaluate()
        {
            if (_current == null || _figure == null) return;

            HumanoidPose pose = _current.Sample(_currentTime);
            if (_previous != null && _blendElapsed < _blendSeconds)
            {
                float t = _blendSeconds <= 0f ? 1f : Mathf.Clamp01(_blendElapsed / _blendSeconds);
                pose = HumanoidPose.Lerp(_previousPose, pose, Mathf.SmoothStep(0f, 1f, t));
            }
            else
            {
                _previous = null;
            }

            pose.ApplyClean(_figure.Skeleton);
            ApplyRootOffset(pose);
        }
    }
}
