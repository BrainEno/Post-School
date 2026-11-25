using System;
using UnityEngine;

namespace PostSchool.Dialogue
{
    /// <summary>Node kinds accepted in dialogue data.</summary>
    public static class DialogueKinds
    {
        public const string Line = "line";
        public const string Choice = "choice";
        public const string End = "end";
    }

    /// <summary>
    /// How a line is presented. Speech is a character's words; recollection and imagination are
    /// the protagonist's subjective content and must not be read as observed events.
    /// </summary>
    public static class DialoguePresentations
    {
        public const string Speech = "speech";
        public const string Narration = "narration";
        public const string Recollection = "recollection";
        public const string Imagination = "imagination";
    }

    /// <summary>
    /// Serializable dialogue graph loaded from JSON. Text is placeholder until the owner approves it.
    /// </summary>
    [Serializable]
    public sealed class DialogueScript
    {
        public string id;
        public string status;
        public string startNode;
        public DialogueNode[] nodes;

        public static DialogueScript FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Dialogue JSON is empty. Check the TextAsset or file path that was loaded.", nameof(json));
            }

            DialogueScript script = JsonUtility.FromJson<DialogueScript>(json);
            if (script == null)
            {
                throw new ArgumentException("Dialogue JSON could not be parsed into a DialogueScript.", nameof(json));
            }

            return script;
        }
    }

    [Serializable]
    public sealed class DialogueNode
    {
        public string id;
        public string kind;
        public string presentation;
        public string speaker;
        public string text;
        public string next;
        public string[] effects;
        public DialogueChoice[] choices;
    }

    [Serializable]
    public sealed class DialogueChoice
    {
        public string text;
        public string next;
        public string[] effects;
    }
}
