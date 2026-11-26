using UnityEngine;

namespace PostSchool.Core
{
    /// <summary>
    /// A simple, readable follow camera for the first prototype. It keeps a fixed offset from the
    /// target and smooths position with frame-rate-independent damping. Final framing is not decided yet.
    /// </summary>
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 6f, -8f);
        [SerializeField] private Vector3 _lookAtOffset = new Vector3(0f, 1.2f, 0f);
        [SerializeField, Min(0f)] private float _followSharpness = 8f;

        public void SetTarget(Transform target)
        {
            if (target == null)
            {
                throw new System.ArgumentNullException(nameof(target));
            }

            _target = target;
        }

        private void Start()
        {
            if (_target == null)
            {
                Debug.LogError($"FollowCamera on '{name}' has no target. Assign the player transform in the inspector or call SetTarget before Start.", this);
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 desired = _target.position + _offset;
            float blend = 1f - Mathf.Exp(-_followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, blend);
            transform.LookAt(_target.position + _lookAtOffset);
        }
    }
}
