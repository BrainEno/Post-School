using System;
using UnityEngine;

namespace PostSchool.Interaction
{
    /// <summary>
    /// Marks a prop the player can inspect. Descriptions are placeholders until the owner approves final text.
    /// </summary>
    public sealed class Inspectable : MonoBehaviour
    {
        [SerializeField] private string _propId = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField, TextArea] private string _placeholderDescription = string.Empty;

        public event Action<Inspectable> Inspected;

        public string PropId => _propId;

        public string DisplayName => _displayName;

        public string Description => _placeholderDescription;

        public void Configure(string propId, string displayName, string placeholderDescription)
        {
            if (string.IsNullOrWhiteSpace(propId))
            {
                throw new ArgumentException("Inspectable ids must be non-empty.", nameof(propId));
            }

            _propId = propId;
            _displayName = displayName ?? string.Empty;
            _placeholderDescription = placeholderDescription ?? string.Empty;
        }

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(_propId))
            {
                Debug.LogError($"Inspectable '{name}' has no propId. Set a stable id so story flags can refer to it.", this);
            }
        }

        public void Inspect()
        {
            Inspected?.Invoke(this);
        }
    }
}
