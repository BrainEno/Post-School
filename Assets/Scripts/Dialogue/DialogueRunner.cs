using System;
using System.Collections.Generic;
using System.IO;
using PostSchool.Character;
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
        private readonly CharacterBuild _characterBuild;
        private readonly InsightLedger _insight;
        private readonly Dictionary<string, DialogueNode> _nodes = new Dictionary<string, DialogueNode>(StringComparer.Ordinal);
        private readonly List<DialogueHistoryEntry> _history = new List<DialogueHistoryEntry>();

        public DialogueRunner(DialogueScript script, StoryState state)
            : this(script, state, null)
        {
        }

        public DialogueRunner(DialogueScript script, StoryState state, CharacterBuild characterBuild)
            : this(script, state, characterBuild, null)
        {
        }

        /// <summary>
        /// Adds the earned-growth ledger. A runner built without one rejects insight effects at load
        /// rather than silently discarding a reward the author wrote, so a scene either pays out or
        /// fails loudly.
        /// </summary>
        public DialogueRunner(DialogueScript script, StoryState state, CharacterBuild characterBuild, InsightLedger insight)
        {
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _characterBuild = characterBuild;
            _insight = insight;

            IndexNodes();
            Validate();
        }

        public string ScriptId => _script.id;

        public DialogueNode Current { get; private set; }

        public bool IsFinished => Current != null && Current.kind == DialogueKinds.End;

        public bool HasLastAttributeCheck { get; private set; }

        public AttributeCheckResult LastAttributeCheck { get; private set; }

        public IReadOnlyList<DialogueHistoryEntry> History => _history;

        public void Begin()
        {
            _history.Clear();
            HasLastAttributeCheck = false;
            LastAttributeCheck = default;
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
            if (!string.IsNullOrWhiteSpace(choice.checkAttribute) && _characterBuild == null)
            {
                throw new InvalidOperationException($"Choice in node '{Current.id}' requires an attribute build. Pass the character build to DialogueRunner.");
            }

            _history.Add(new DialogueHistoryEntry(string.Empty, DialoguePresentations.PlayerChoice, choice.text));
            ApplyEffects(choice.effects);

            string destination = choice.next;
            HasLastAttributeCheck = false;
            if (!string.IsNullOrWhiteSpace(choice.checkAttribute))
            {
                CoreAttributeNames.TryParse(choice.checkAttribute, out CoreAttribute attribute);
                LastAttributeCheck = AttributeCheckResolver.Resolve(_characterBuild, attribute, choice.minimumValue);
                HasLastAttributeCheck = true;
                destination = LastAttributeCheck.Passed ? choice.next : choice.failureNext;
                string outcome = LastAttributeCheck.Passed ? "达到" : "未达到";
                string resultText = $"{CoreAttributeNames.DisplayName(attribute)}检定：{LastAttributeCheck.Value} / {LastAttributeCheck.MinimumValue}（{outcome}）";
                _history.Add(new DialogueHistoryEntry(string.Empty, DialoguePresentations.AttributeCheck, resultText));
            }

            Enter(destination);
        }

        private void Enter(string nodeId)
        {
            DialogueNode node = Lookup(nodeId);
            Current = node;

            ApplyEffects(node.effects);

            if (node.kind == DialogueKinds.Line || (node.kind == DialogueKinds.Choice && !string.IsNullOrWhiteSpace(node.text)))
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
                if (StoryEffect.IsInsight(effect))
                {
                    ApplyInsight(effect);
                    continue;
                }

                StoryEffect.Apply(_state, effect);
            }
        }

        /// <summary>
        /// Books an insight award against the ledger. Points are not granted here: the ledger converts
        /// whole thresholds, and the scene grants them when the imagining ends, so the player is not
        /// handed a point mid-sentence.
        /// </summary>
        private void ApplyInsight(string effect)
        {
            if (_insight == null)
            {
                throw new InvalidOperationException(
                    $"Dialogue '{_script.id}' awards '{effect}', but this runner was built without an InsightLedger.");
            }

            StoryEffect.ParseInsight(effect, out string attributeName, out int amount);
            if (!CoreAttributeNames.TryParse(attributeName, out CoreAttribute attribute))
            {
                throw new InvalidOperationException($"Dialogue '{_script.id}' awards insight in unknown attribute '{attributeName}'.");
            }

            _insight.Award(attribute, amount);
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

                bool hasCheck = !string.IsNullOrWhiteSpace(choice.checkAttribute);
                if (!hasCheck)
                {
                    if (choice.minimumValue != 0 || !string.IsNullOrWhiteSpace(choice.failureNext))
                    {
                        throw new InvalidDataException($"Choice in node '{node.id}' has check fields but no checkAttribute.");
                    }

                    continue;
                }

                if (!CoreAttributeNames.TryParse(choice.checkAttribute, out _))
                {
                    throw new InvalidDataException($"Choice in node '{node.id}' references unknown attribute '{choice.checkAttribute}'.");
                }

                if (choice.minimumValue < CharacterBuild.BaseValue || choice.minimumValue > CharacterBuild.AttributeCap)
                {
                    throw new InvalidDataException($"Choice in node '{node.id}' has check target {choice.minimumValue}; expected {CharacterBuild.BaseValue}..{CharacterBuild.AttributeCap}.");
                }

                Lookup(choice.failureNext);
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
                    if (StoryEffect.IsInsight(effect))
                    {
                        StoryEffect.ParseInsight(effect, out string attributeName, out _);
                        if (!CoreAttributeNames.TryParse(attributeName, out _))
                        {
                            throw new FormatException($"unknown attribute '{attributeName}' in insight effect");
                        }
                    }
                    else
                    {
                        StoryEffect.Parse(effect, out _, out _);
                    }
                }
                catch (FormatException error)
                {
                    throw new InvalidDataException($"Node '{nodeId}': {error.Message}", error);
                }
            }
        }
    }
}
