using System;
using System.Collections.Generic;
using System.IO;
using PostSchool.Narrative;

namespace PostSchool.Dialogue
{
    public readonly struct DialogueHistoryEntry
    {
        public DialogueHistoryEntry(string speaker, string presentation, string text)
        {
            Speaker = speaker;
            Presentation = presentation;
            Text = text;
        }

        public string Speaker { get; }

        public string Presentation { get; }

        public string Text { get; }
    }

    /// <summary>
    /// Walks a validated dialogue graph and applies its story effects. It owns no UI; a presenter reads
    /// <see cref="Current"/> and calls <see cref="Advance"/> or <see cref="Choose"/>.
    /// </summary>
    public sealed class DialogueRunner
    {
        private static readonly HashSet<string> ValidKinds = new HashSet<string>(StringComparer.Ordinal)
        {
            DialogueKinds.Line,
            DialogueKinds.Choice,
            DialogueKinds.End,
        };

        private static readonly HashSet<string> ValidPresentations = new HashSet<string>(StringComparer.Ordinal)
        {
            DialoguePresentations.Speech,
            DialoguePresentations.Narration,
            DialoguePresentations.Recollection,
            DialoguePresentations.Imagination,
        };

        private readonly DialogueScript _script;
        private readonly StoryState _state;
        private readonly Dictionary<string, DialogueNode> _nodes = new Dictionary<string, DialogueNode>(StringComparer.Ordinal);
        private readonly List<DialogueHistoryEntry> _history = new List<DialogueHistoryEntry>();

        public DialogueRunner(DialogueScript script, StoryState state)
        {
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _state = state ?? throw new ArgumentNullException(nameof(state));

            IndexNodes();
            Validate();
        }

        public string ScriptId => _script.id;

        public DialogueNode Current { get; private set; }

        public bool IsFinished => Current != null && Current.kind == DialogueKinds.End;

        public IReadOnlyList<DialogueHistoryEntry> History => _history;

        public void Begin()
        {
            _history.Clear();
            Current = null;
            Enter(_script.startNode);
        }

        public void Advance()
        {
            RequireCurrent(DialogueKinds.Line);
            Enter(Current.next);
        }

        public void Choose(int index)
        {
            RequireCurrent(DialogueKinds.Choice);

            DialogueChoice[] choices = Current.choices;
            if (index < 0 || index >= choices.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Node '{Current.id}' offers {choices.Length} choices.");
            }

            DialogueChoice choice = choices[index];
            _history.Add(new DialogueHistoryEntry(string.Empty, DialoguePresentations.Speech, choice.text));
            ApplyEffects(choice.effects);
            Enter(choice.next);
        }

        private void Enter(string nodeId)
        {
            DialogueNode node = Lookup(nodeId);
            Current = node;

            ApplyEffects(node.effects);

            if (node.kind == DialogueKinds.Line)
            {
                _history.Add(new DialogueHistoryEntry(node.speaker, node.presentation, node.text));
            }
        }

        private DialogueNode Lookup(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || !_nodes.TryGetValue(nodeId, out DialogueNode node))
            {
                throw new InvalidDataException($"Dialogue '{_script.id}' references missing node '{nodeId}'. Check the 'next' or 'startNode' field.");
            }

            return node;
        }

        private void ApplyEffects(string[] effects)
        {
            if (effects == null)
            {
                return;
            }

            foreach (string effect in effects)
            {
                StoryEffect.Apply(_state, effect);
            }
        }

        private void RequireCurrent(string expectedKind)
        {
            if (Current == null)
            {
                throw new InvalidOperationException("Dialogue has not started. Call Begin() first.");
            }

            if (Current.kind != expectedKind)
            {
                throw new InvalidOperationException($"Node '{Current.id}' is '{Current.kind}', not '{expectedKind}'.");
            }
        }

        private void IndexNodes()
        {
            if (_script.nodes == null)
            {
                throw new InvalidDataException($"Dialogue '{_script.id}' has no nodes array.");
            }

            foreach (DialogueNode node in _script.nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.id))
                {
                    throw new InvalidDataException($"Dialogue '{_script.id}' contains a node without an id.");
                }

                if (_nodes.ContainsKey(node.id))
                {
                    throw new InvalidDataException($"Dialogue '{_script.id}' defines node '{node.id}' more than once.");
                }

                _nodes.Add(node.id, node);
            }
        }

        private void Validate()
        {
            if (string.IsNullOrEmpty(_script.startNode))
            {
                throw new InvalidDataException($"Dialogue '{_script.id}' has no startNode.");
            }

            Lookup(_script.startNode);

            foreach (DialogueNode node in _script.nodes)
            {
                if (!ValidKinds.Contains(node.kind))
                {
                    throw new InvalidDataException($"Node '{node.id}' has unknown kind '{node.kind}'. Use line, choice, or end.");
                }

                if (node.kind != DialogueKinds.End && !ValidPresentations.Contains(node.presentation))
                {
                    throw new InvalidDataException($"Node '{node.id}' has unknown presentation '{node.presentation}'.");
                }

                ValidateEffects(node.id, node.effects);

                switch (node.kind)
                {
                    case DialogueKinds.Line:
                        Lookup(node.next);
                        break;

                    case DialogueKinds.Choice:
                        ValidateChoices(node);
                        break;
                }
            }
        }

        private void ValidateChoices(DialogueNode node)
        {
            if (node.choices == null || node.choices.Length == 0)
            {
                throw new InvalidDataException($"Choice node '{node.id}' has no choices.");
            }

            foreach (DialogueChoice choice in node.choices)
            {
                if (choice == null || string.IsNullOrWhiteSpace(choice.text))
                {
                    throw new InvalidDataException($"Choice node '{node.id}' contains a choice with no text.");
                }

                ValidateEffects(node.id, choice.effects);
                Lookup(choice.next);
            }
        }

        private static void ValidateEffects(string nodeId, string[] effects)
        {
            if (effects == null)
            {
                return;
            }

            foreach (string effect in effects)
            {
                try
                {
                    StoryEffect.Parse(effect, out _, out _);
                }
                catch (FormatException error)
                {
                    throw new InvalidDataException($"Node '{nodeId}': {error.Message}", error);
                }
            }
        }
    }
}
