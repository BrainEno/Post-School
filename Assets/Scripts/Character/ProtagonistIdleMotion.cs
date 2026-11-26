using UnityEngine;

namespace PostSchool.Character
{
    /// <summary>
    /// The protagonist's standing idle: a slow breath, a head that keeps turning to look at things that
    /// are not there, and a weight shift that never quite settles. The intent is someone who is present
    /// in a room that does not entirely agree with them.
    ///
    /// Purely cosmetic; it writes only local rotations on the rig and never touches movement.
    /// </summary>
    public sealed class ProtagonistIdleMotion : MonoBehaviour
    {
        [SerializeField] private float breathSeconds = 4.6f;
        [SerializeField] private float glanceSeconds = 6.8f;
        [SerializeField] private float swaySeconds = 9.5f;

        private ProtagonistRig _rig;
        private float _time;

        public void Initialize(ProtagonistRig rig)
        {
            _rig = rig;
            _time = 0f;
        }

        public void Suspend()
        {
            enabled = false;
            ResetPose();
        }

        public void Resume()
        {
            enabled = true;
        }

        private void OnDisable()
        {
            ResetPose();
        }

        private void Update()
        {
            if (_rig == null) return;

            // Unscaled time: the idle keeps breathing while the dialogue system holds timeScale at zero.
            _time += Time.unscaledDeltaTime;

            float breath = Mathf.Sin(_time / breathSeconds * Mathf.PI * 2f);
            float sway = Mathf.Sin(_time / swaySeconds * Mathf.PI * 2f);
            float glance = Mathf.Sin(_time / glanceSeconds * Mathf.PI * 2f);

            if (_rig.Visual != null)
            {
                Vector3 rest = _rig.VisualRestPosition;
                _rig.Visual.localPosition = new Vector3(rest.x, rest.y + breath * 0.004f, rest.z);
                _rig.Visual.localRotation = Quaternion.Euler(0f, 0f, sway * 0.7f);
            }

            if (_rig.HeadPivot != null)
            {
                // The head lag: it follows the sway slightly late, so the body and face disagree.
                float delayedSway = Mathf.Sin((_time - 0.35f) / swaySeconds * Mathf.PI * 2f);
                _rig.HeadPivot.localRotation = Quaternion.Euler(breath * 1.2f, glance * 11f, 1.6f + delayedSway * 1.4f);
            }

            if (_rig.LeftArmPivot != null)
            {
                _rig.LeftArmPivot.localRotation = Quaternion.Euler(0f, 0f, -8f + breath * 1.1f);
            }

            if (_rig.RightArmPivot != null)
            {
                // The right arm carries the sleeve band and hangs a little further from the body.
                _rig.RightArmPivot.localRotation = Quaternion.Euler(0f, 0f, 8f + breath * 0.9f);
            }
        }

        private void ResetPose()
        {
            if (_rig == null) return;

            if (_rig.Visual != null)
            {
                _rig.Visual.localPosition = _rig.VisualRestPosition;
                _rig.Visual.localRotation = Quaternion.identity;
            }

            if (_rig.HeadPivot != null) _rig.HeadPivot.localRotation = Quaternion.Euler(0f, 0f, 1.6f);
            if (_rig.LeftArmPivot != null) _rig.LeftArmPivot.localRotation = Quaternion.Euler(0f, 0f, -8f);
            if (_rig.RightArmPivot != null) _rig.RightArmPivot.localRotation = Quaternion.Euler(0f, 0f, 8f);
            if (_rig.LeftLegPivot != null) _rig.LeftLegPivot.localRotation = Quaternion.identity;
            if (_rig.RightLegPivot != null) _rig.RightLegPivot.localRotation = Quaternion.identity;
        }
    }
}
