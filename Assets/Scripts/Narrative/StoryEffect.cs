using System;

namespace PostSchool.Narrative
{
    /// <summary>
    /// Parses authored effect strings such as <c>objective:textbook_missing</c>,
    /// <c>recollection:tried_to_remember</c> or <c>insight:Imagination:2</c>. Parsing is separate from
    /// application so data can be validated at load time.
    ///
    /// Insight is the third prefix and the only one that is a quantity rather than a flag: it is a
    /// reward, so it is written as <c>insight:&lt;Attribute&gt;:&lt;amount&gt;</c> and may be negative,
    /// because a scene that ends badly can forfeit work instead of granting it.
    /// </summary>
    public static class StoryEffect
    {
        public const string ObjectivePrefix = "objective:";
        public const string RecollectionPrefix = "recollection:";
        public const string InsightPrefix = "insight:";

        public static void Parse(string effect, out StoryNamespace storyNamespace, out string flag)
        {
            if (effect == null)
            {
                throw new FormatException("Story effect is null. Use an explicit entry such as 'objective:<flag>'.");
            }

            if (effect.StartsWith(ObjectivePrefix, StringComparison.Ordinal))
            {
                storyNamespace = StoryNamespace.Objective;
                flag = effect.Substring(ObjectivePrefix.Length);
            }
            else if (effect.StartsWith(RecollectionPrefix, StringComparison.Ordinal))
            {
                storyNamespace = StoryNamespace.Recollection;
                flag = effect.Substring(RecollectionPrefix.Length);
            }
            else
            {
                throw new FormatException($"Unknown story effect '{effect}'. Use '{ObjectivePrefix}<flag>' or '{RecollectionPrefix}<flag>'.");
            }

            if (string.IsNullOrWhiteSpace(flag))
            {
                throw new FormatException($"Story effect '{effect}' has no flag name after its prefix.");
            }
        }

        /// <summary>
        /// True when the effect is an insight award rather than a flag. Callers that have no ledger
        /// (a dialogue runner used only to walk a graph, for instance) can skip these instead of
        /// failing, so insight is optional everywhere it is not wanted.
        /// </summary>
        public static bool IsInsight(string effect)
        {
            return effect != null && effect.StartsWith(InsightPrefix, StringComparison.Ordinal);
        }

        /// <summary>Parses an <c>insight:Attribute:amount</c> effect. Throws on anything malformed.</summary>
        public static void ParseInsight(string effect, out string attributeName, out int amount)
        {
            if (!IsInsight(effect))
            {
                throw new FormatException($"'{effect}' is not an insight effect. Use '{InsightPrefix}<Attribute>:<amount>'.");
            }

            string[] parts = effect.Substring(InsightPrefix.Length).Split(':');
            if (parts.Length != 2)
            {
                throw new FormatException($"Insight effect '{effect}' must be '{InsightPrefix}<Attribute>:<amount>'.");
            }

            attributeName = parts[0];
            if (!int.TryParse(parts[1], out amount) || amount == 0)
            {
                throw new FormatException($"Insight effect '{effect}' has an amount that is not a non-zero integer.");
            }
        }

        public static void Apply(StoryState state, string effect)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (IsInsight(effect))
            {
                // Flags and insight are different currencies; a story state has nowhere to put points.
                throw new ArgumentException(
                    $"'{effect}' awards insight and cannot be applied to a StoryState. Apply it through an InsightLedger.",
                    nameof(effect));
            }

            Parse(effect, out StoryNamespace storyNamespace, out string flag);
            state.Set(storyNamespace, flag);
        }
    }
}
