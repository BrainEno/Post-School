using System;
using PostSchool.Character;
using UnityEngine;

namespace PostSchool.Ui
{
    /// <summary>
    /// Copy for the character sheet. Every placed line lives here so the owner can rewrite the sheet
    /// as a unit without touching view code. All shipped text is a placeholder until approved.
    /// Attribute names are written out in the JSON so the file stays editable by hand.
    /// </summary>
    [Serializable]
    public sealed class CharacterSheetText
    {
        public string id;
        public string status;
        public string header;
        public string subheader;
        public string instruction;
        public string draftNotice;
        public string confirmLabel;
        public string resetLabel;
        public string catalogueLabel;
        public string catalogueNote;
        public string budgetLabel;
        public string skillLabel;
        public string thresholdLabel;
        public AttributeSheetText[] attributes;

        /// <summary>Parses the sheet text asset and fails with an actionable error when it is unusable.</summary>
        public static CharacterSheetText FromJson(string json, string sourceName)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException($"Character sheet text '{sourceName}' is empty. Check the TextAsset at {sourceName}.json.", nameof(json));
            }

            CharacterSheetText text;
            try
            {
                text = JsonUtility.FromJson<CharacterSheetText>(json);
            }
            catch (ArgumentException jsonError)
            {
                throw new ArgumentException($"Character sheet text '{sourceName}' is not valid JSON: {jsonError.Message}", nameof(json), jsonError);
            }

            if (text == null || text.attributes == null || text.attributes.Length == 0)
            {
                throw new ArgumentException($"Character sheet text '{sourceName}' parsed without any attribute entries.", nameof(json));
            }

            if (!text.Validate(out string validationError))
            {
                throw new ArgumentException($"Character sheet text '{sourceName}' is unusable: {validationError}", nameof(json));
            }

            return text;
        }

        /// <summary>Entry for an attribute. Validation has already guaranteed one exists.</summary>
        public AttributeSheetText For(CoreAttribute attribute)
        {
            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i] != null
                    && CoreAttributeNames.TryParse(attributes[i].attribute ?? string.Empty, out CoreAttribute parsed)
                    && parsed == attribute)
                {
                    return attributes[i];
                }
            }

            throw new InvalidOperationException($"Character sheet text '{id}' has no entry for {attribute}; Validate must be called first.");
        }

        /// <summary>Every structural problem, reported together rather than rendering a half-empty sheet.</summary>
        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                error = "Character sheet text has no 'status' field; placeholder text must stay identifiable.";
                return false;
            }

            if (attributes == null || attributes.Length != CharacterBuild.AttributeCount)
            {
                error = $"Character sheet text '{id}' must contain exactly {CharacterBuild.AttributeCount} attribute entries.";
                return false;
            }

            bool[] seen = new bool[CharacterBuild.AttributeCount];
            for (int i = 0; i < attributes.Length; i++)
            {
                AttributeSheetText entry = attributes[i];
                if (entry == null)
                {
                    error = $"Character sheet text '{id}' has a null attribute entry at index {i}.";
                    return false;
                }

                if (!CoreAttributeNames.TryParse(entry.attribute ?? string.Empty, out CoreAttribute parsed))
                {
                    error = $"Character sheet text '{id}' entry {i} has unknown attribute '{entry.attribute}'.";
                    return false;
                }

                int index = (int)parsed;
                if (seen[index])
                {
                    error = $"Character sheet text '{id}' repeats attribute {parsed}.";
                    return false;
                }

                seen[index] = true;

                if (string.IsNullOrWhiteSpace(entry.voice))
                {
                    error = $"Character sheet text '{id}' entry {parsed} has no voice title.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(entry.narrative))
                {
                    error = $"Character sheet text '{id}' entry {parsed} has no narrative line.";
                    return false;
                }

                if (entry.rungs == null || entry.rungs.Length != CharacterSheetRules.SlotCount)
                {
                    error = $"Character sheet text '{id}' entry {parsed} must name all {CharacterSheetRules.SlotCount} ladder rungs.";
                    return false;
                }
            }

            for (int i = 0; i < CharacterBuild.AttributeCount; i++)
            {
                if (!seen[i])
                {
                    error = $"Character sheet text '{id}' is missing attribute {(CoreAttribute)i}.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }

    /// <summary>Per-attribute sheet copy: voice, narrative feel, skills, and the five ladder rungs.</summary>
    [Serializable]
    public sealed class AttributeSheetText
    {
        /// <summary>Enum name, e.g. "Physique". Parsed through CoreAttributeNames.TryParse.</summary>
        public string attribute = string.Empty;

        /// <summary>Short voice title printed next to the value, e.g. "身体在开口".</summary>
        public string voice = string.Empty;

        /// <summary>One line of narrative feel for the attribute.</summary>
        public string narrative = string.Empty;

        /// <summary>Provisional skill vocabulary from docs/CORE_GAMEPLAY.md.</summary>
        public string skills = string.Empty;

        /// <summary>One mark per ladder slot, from base value to the provisional cap.</summary>
        public string[] rungs = Array.Empty<string>();

        /// <summary>How the attribute behaves at its lowest value.</summary>
        public string lowNote = string.Empty;

        /// <summary>How the attribute behaves at its highest value.</summary>
        public string highNote = string.Empty;
    }
}
