using UnityEngine;
using UnityEngine.InputSystem;

namespace PostSchool.Interaction
{
    /// <summary>
    /// Finds the nearest <see cref="Inspectable"/> in front of the player and inspects it on the interact button.
    /// It owns target selection only; what inspection means is decided by the Inspectable's listeners.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _reach = 1.6f;
        [SerializeField, Min(0f)] private float _radius = 0.6f;
        [SerializeField] private LayerMask _interactableMask = ~0;

        private readonly Collider[] _hits = new Collider[16];

        /// <summary>The prop currently in reach, or null. A prompt UI can read this every frame.</summary>
        public Inspectable Target { get; private set; }

        private void Update()
        {
            Target = FindTarget();

            if (Target != null && WasInteractPressed())
            {
                Target.Inspect();
            }
        }

        private Inspectable FindTarget()
        {
            Vector3 center = transform.position + transform.forward * (_reach * 0.5f) + Vector3.up * 0.5f;
            int count = Physics.OverlapSphereNonAlloc(center, _radius, _hits, _interactableMask, QueryTriggerInteraction.Collide);

            Inspectable best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Inspectable candidate = _hits[i].GetComponentInParent<Inspectable>();
                if (candidate == null)
                {
                    continue;
                }

                float distance = Vector3.SqrMagnitude(candidate.transform.position - center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        private static bool WasInteractPressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        }
    }
}
