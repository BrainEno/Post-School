using System;
using UnityEngine;

namespace PostSchool.Character
{
    /// <summary>The five owner-approved primary attributes used by the first character build.</summary>
    public enum CoreAttribute
    {
        Physique,
        Psyche,
        Intellect,
        Perception,
        Imagination,
    }

    public static class CoreAttributeNames
    {
        public static string DisplayName(CoreAttribute attribute)
        {
            switch (attribute)
            {
                case CoreAttribute.Physique: return "体魄";
                case CoreAttribute.Psyche: return "精神";
                case CoreAttribute.Intellect: return "智性";
                case CoreAttribute.Perception: return "感知";
                case CoreAttribute.Imagination: return "想象";
                default: throw new ArgumentOutOfRangeException(nameof(attribute), attribute, "Unknown core attribute.");
            }
        }

        public static bool TryParse(string value, out CoreAttribute attribute)
        {
            return Enum.TryParse(value, ignoreCase: false, out attribute) && Enum.IsDefined(typeof(CoreAttribute), attribute);
        }
    }

    /// <summary>
    /// Mutable starting build. A new character has five base points and seven unspent points;
    /// the model owns all cap, floor, and budget validation independently of the UI.
    /// </summary>
    [Serializable]
    public sealed class CharacterBuild
    {
        public const int BaseValue = 1;
        public const int FreePoints = 7;
        public const int AttributeCap = 5;
        public const int AttributeCount = 5;
        public const int StartingTotal = AttributeCount * BaseValue + FreePoints;

        [SerializeField] private int[] _values;

        public CharacterBuild()
        {
            _values = new int[AttributeCount];
            for (int i = 0; i < _values.Length; i++)
            {
                _values[i] = BaseValue;
            }
        }

        public int RemainingPoints => StartingTotal - Total;

        public int Total
        {
            get
            {
                int total = 0;
                for (int i = 0; i < _values.Length; i++)
                {
                    total += _values[i];
                }

                return total;
            }
        }

        public bool IsComplete => RemainingPoints == 0;

        public int GetValue(CoreAttribute attribute)
        {
            return _values[GetIndex(attribute)];
        }

        /// <summary>Apply exactly one point in either direction; invalid edits leave state unchanged.</summary>
        public bool TryAdjust(CoreAttribute attribute, int delta)
        {
            if (delta != -1 && delta != 1)
            {
                return false;
            }

            int index;
            try
            {
                index = GetIndex(attribute);
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }

            int current = _values[index];
            if (delta > 0 && (RemainingPoints <= 0 || current >= AttributeCap))
            {
                return false;
            }

            if (delta < 0 && current <= BaseValue)
            {
                return false;
            }

            _values[index] = current + delta;
            return true;
        }

        public int[] CopyValues()
        {
            if (!IsValid(out string error))
            {
                throw new InvalidOperationException("Cannot snapshot an invalid character build: " + error);
            }

            return (int[])_values.Clone();
        }

        public bool TryRestoreValues(int[] values, out string error)
        {
            if (values == null || values.Length != AttributeCount)
            {
                error = "A saved character build must contain exactly five attribute values.";
                return false;
            }

            int total = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] < BaseValue || values[i] > AttributeCap)
                {
                    error = $"Saved attribute at index {i} must be between {BaseValue} and {AttributeCap}.";
                    return false;
                }

                total += values[i];
            }

            if (total > StartingTotal)
            {
                error = $"Saved character total {total} exceeds the starting budget {StartingTotal}.";
                return false;
            }

            _values = (int[])values.Clone();
            error = string.Empty;
            return true;
        }

        public void Reset()
        {
            for (int i = 0; i < _values.Length; i++)
            {
                _values[i] = BaseValue;
            }
        }

        public bool IsValid(out string error)
        {
            if (_values == null || _values.Length != AttributeCount)
            {
                error = "A character build must contain exactly five attribute values.";
                return false;
            }

            for (int i = 0; i < _values.Length; i++)
            {
                if (_values[i] < BaseValue || _values[i] > AttributeCap)
                {
                    error = $"Attribute at index {i} must be between {BaseValue} and {AttributeCap}.";
                    return false;
                }
            }

            if (Total > StartingTotal)
            {
                error = $"Character total {Total} exceeds the starting budget {StartingTotal}.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static int GetIndex(CoreAttribute attribute)
        {
            if (!Enum.IsDefined(typeof(CoreAttribute), attribute))
            {
                throw new ArgumentOutOfRangeException(nameof(attribute), attribute, "Unknown core attribute.");
            }

            return (int)attribute;
        }
    }

    /// <summary>A deterministic, repeatable attribute threshold result; this prototype uses no random roll.</summary>
    public readonly struct AttributeCheckResult
    {
        public AttributeCheckResult(CoreAttribute attribute, int value, int minimumValue)
        {
            Attribute = attribute;
            Value = value;
            MinimumValue = minimumValue;
        }

        public CoreAttribute Attribute { get; }

        public int Value { get; }

        public int MinimumValue { get; }

        public bool Passed => Value >= MinimumValue;
    }

    public static class AttributeCheckResolver
    {
        public static AttributeCheckResult Resolve(CharacterBuild build, CoreAttribute attribute, int minimumValue)
        {
            if (build == null)
            {
                throw new ArgumentNullException(nameof(build));
            }

            if (minimumValue < CharacterBuild.BaseValue || minimumValue > CharacterBuild.AttributeCap)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumValue), minimumValue, "Check target must fit within the provisional attribute scale.");
            }

            return new AttributeCheckResult(attribute, build.GetValue(attribute), minimumValue);
        }
    }
}
