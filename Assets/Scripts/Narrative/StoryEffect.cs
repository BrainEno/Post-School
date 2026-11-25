using System;

namespace PostSchool.Narrative
{
    /// <summary>
    /// Parses authored effect strings such as <c>objective:textbook_missing</c> or
    /// <c>recollection:tried_to_remember</c>. Parsing is separate from application so data can be validated at load time.
    /// </summary>
    public static class StoryEffect
    {
        public const string ObjectivePrefix = "objective:";
        public const string RecollectionPrefix = "recollection:";

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

        public static void Apply(StoryState state, string effect)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Parse(effect, out StoryNamespace storyNamespace, out string flag);
            state.Set(storyNamespace, flag);
        }
    }
}
