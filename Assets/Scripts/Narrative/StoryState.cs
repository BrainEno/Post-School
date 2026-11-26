using System;
using System.Collections.Generic;

namespace PostSchool.Narrative
{
    public enum StoryNamespace
    {
        /// <summary>Facts about the world that the scene and other systems may rely on.</summary>
        Objective,

        /// <summary>What the protagonist remembers or imagines. Never changes objective flags.</summary>
        Recollection,
    }

    public readonly struct StoryFlagChange
    {
        public StoryFlagChange(StoryNamespace storyNamespace, string flag)
        {
            Namespace = storyNamespace;
            Flag = flag;
        }

        public StoryNamespace Namespace { get; }

        public string Flag { get; }
    }

    /// <summary>
    /// Stores story flags in two separate namespaces. Recollection flags record subjective content
    /// and are deliberately kept apart from objective world state.
    /// </summary>
    public sealed class StoryState
    {
        private readonly HashSet<string> _objectiveFlags = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _recollectionFlags = new HashSet<string>(StringComparer.Ordinal);

        public event Action<StoryFlagChange> FlagChanged;

        public IReadOnlyCollection<string> ObjectiveFlags => _objectiveFlags;

        public IReadOnlyCollection<string> RecollectionFlags => _recollectionFlags;

        public bool IsSet(StoryNamespace storyNamespace, string flag)
        {
            return SetFor(storyNamespace).Contains(ValidateFlag(flag));
        }

        public void Set(StoryNamespace storyNamespace, string flag)
        {
            string name = ValidateFlag(flag);
            if (SetFor(storyNamespace).Add(name))
            {
                FlagChanged?.Invoke(new StoryFlagChange(storyNamespace, name));
            }
        }

        /// <summary>Clears every flag without raising change events. Intended for new-game setup only.</summary>
        public void Reset()
        {
            _objectiveFlags.Clear();
            _recollectionFlags.Clear();
        }

        public string[] CopyObjectiveFlags()
        {
            return CopySorted(_objectiveFlags);
        }

        public string[] CopyRecollectionFlags()
        {
            return CopySorted(_recollectionFlags);
        }

        /// <summary>
        /// Replaces both namespaces atomically without raising FlagChanged. Used when restoring a save;
        /// load itself is not a new gameplay event.
        /// </summary>
        public bool TryRestoreFlags(IEnumerable<string> objectiveFlags, IEnumerable<string> recollectionFlags, out string error)
        {
            if (!TryCreateFlagSet(objectiveFlags, "objective", out HashSet<string> objective, out error)) return false;
            if (!TryCreateFlagSet(recollectionFlags, "recollection", out HashSet<string> recollection, out error)) return false;

            _objectiveFlags.Clear();
            _objectiveFlags.UnionWith(objective);
            _recollectionFlags.Clear();
            _recollectionFlags.UnionWith(recollection);
            error = string.Empty;
            return true;
        }

        private static string[] CopySorted(HashSet<string> flags)
        {
            List<string> copy = new List<string>(flags);
            copy.Sort(StringComparer.Ordinal);
            return copy.ToArray();
        }

        private static bool TryCreateFlagSet(IEnumerable<string> source, string namespaceName, out HashSet<string> result, out string error)
        {
            result = new HashSet<string>(StringComparer.Ordinal);
            if (source == null)
            {
                error = $"The saved {namespaceName} flags collection is missing.";
                return false;
            }

            foreach (string flag in source)
            {
                if (string.IsNullOrWhiteSpace(flag))
                {
                    error = $"Saved {namespaceName} flag names must be non-empty.";
                    return false;
                }

                result.Add(flag);
            }

            error = string.Empty;
            return true;
        }

        private HashSet<string> SetFor(StoryNamespace storyNamespace)
        {
            switch (storyNamespace)
            {
                case StoryNamespace.Objective:
                    return _objectiveFlags;
                case StoryNamespace.Recollection:
                    return _recollectionFlags;
                default:
                    throw new ArgumentOutOfRangeException(nameof(storyNamespace), storyNamespace, "Unknown story namespace.");
            }
        }

        private static string ValidateFlag(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag))
            {
                throw new ArgumentException("Story flag names must be non-empty. Check the dialogue effect or caller that supplied this flag.", nameof(flag));
            }

            return flag;
        }
    }
}
