using UnityEngine;
using UnityEngine.InputSystem;

namespace PostSchool.Core
{
    /// <summary>
    /// Owns protagonist locomotion. Movement is camera-relative when an anchor is assigned.
    /// Input is read directly from the Input System devices for this first prototype; move to
    /// InputActions once the input contract is finalised.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _moveSpeed = 3f;
        [SerializeField, Min(0f)] private float _turnSpeedDegrees = 720f;
        [SerializeField] private float _gravity = -18f;
        [SerializeField] private Transform _cameraAnchor;

        private CharacterController _controller;
        private float _verticalVelocity;

        /// <summary>Set to false while dialogue or another system owns the player.</summary>
        public bool InputEnabled { get; set; } = true;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector2 input = InputEnabled ? ReadMoveInput() : Vector2.zero;

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (_cameraAnchor != null)
            {
                forward = Flatten(_cameraAnchor.forward);
                right = Flatten(_cameraAnchor.right);
            }

            Vector3 move = right * input.x + forward * input.y;
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            _verticalVelocity += _gravity * Time.deltaTime;

            Vector3 velocity = move * _moveSpeed + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);

            if (move.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, _turnSpeedDegrees * Time.deltaTime);
            }
        }

        private static Vector3 Flatten(Vector3 direction)
        {
            direction.y = 0f;
            return direction.normalized;
        }

        private static Vector2 ReadMoveInput()
        {
            Vector2 value = Vector2.zero;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    value.x -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    value.x += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    value.y -= 1f;
                }

                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    value.y += 1f;
                }
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                value += gamepad.leftStick.ReadValue();
            }

            return value;
        }
    }
}
