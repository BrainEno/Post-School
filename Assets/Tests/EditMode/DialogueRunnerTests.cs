using System;
using System.IO;
using NUnit.Framework;
using PostSchool.Dialogue;
using PostSchool.Narrative;
using UnityEngine;

namespace PostSchool.Tests.EditMode
{
    [TestFixture]
    public sealed class DialogueRunnerTests
    {
        private const string PlaceholderRelativePath = "Data/Dialogue/classroom_opening_placeholder.json";

        [Test]
        public void PlaceholderOpening_LoadsAndReachesEnd_SettingObjectiveFlags()
        {
            string path = Path.Combine(Application.dataPath, PlaceholderRelativePath);
            Assert.IsTrue(File.Exists(path), $"Placeholder dialogue missing at '{path}'.");

            DialogueScript script = DialogueScript.FromJson(File.ReadAllText(path));
            StoryState state = new StoryState();
            DialogueRunner runner = new DialogueRunner(script, state);

            RunToEnd(runner, choiceIndex: 1);

            Assert.IsTrue(runner.IsFinished);
            Assert.IsTrue(state.IsSet(StoryNamespace.Objective, "textbook_missing"));
            Assert.IsTrue(state.IsSet(StoryNamespace.Objective, "substitute_n_present"));
            Assert.IsTrue(state.IsSet(StoryNamespace.Objective, "opening_complete"));
            Assert.IsFalse(state.IsSet(StoryNamespace.Objective, "asked_about_class"));
        }

        [Test]
        public void Choice_AppliesOnlyTheChosenEffects()
        {
            DialogueRunner runner = CreateRunner(BranchingJson, out StoryState state);
            runner.Begin();
            Assert.AreEqual(DialogueKinds.Choice, runner.Current.kind);

            runner.Choose(1);

            Assert.IsTrue(state.IsSet(StoryNamespace.Objective, "chose_b"));
            Assert.IsFalse(state.IsSet(StoryNamespace.Objective, "chose_a"));
            Assert.AreEqual("after_b", runner.Current.id);
        }

        [Test]
        public void RecollectionAndImagination_DoNotSetObjectiveFlags()
        {
            DialogueRunner runner = CreateRunner(SubjectiveJson, out StoryState state);

            RunToEnd(runner, choiceIndex: 0);

            Assert.IsTrue(state.IsSet(StoryNamespace.Recollection, "remembered_sound"));
            Assert.AreEqual(0, state.ObjectiveFlags.Count);
            bool sawImagination = false;
            foreach (DialogueHistoryEntry entry in runner.History)
            {
                if (entry.Presentation == DialoguePresentations.Imagination)
                {
                    sawImagination = true;
                }
            }

            Assert.IsTrue(sawImagination, "History should keep imagined lines labelled as imagination.");
        }

        [Test]
        public void MissingNextNode_ThrowsWithReferencingNodeName()
        {
            const string json = @"{
              ""id"": ""broken"", ""startNode"": ""a"",
              ""nodes"": [
                { ""id"": ""a"", ""kind"": ""line"", ""presentation"": ""narration"", ""text"": ""x"", ""next"": ""missing_node"" }
              ]
            }";

            InvalidDataException error = Assert.Throws<InvalidDataException>(() => new DialogueRunner(DialogueScript.FromJson(json), new StoryState()));
            StringAssert.Contains("missing_node", error.Message);
        }

        [Test]
        public void UnknownEffectPrefix_ThrowsWithNodeName()
        {
            const string json = @"{
              ""id"": ""broken"", ""startNode"": ""a"",
              ""nodes"": [
                { ""id"": ""a"", ""kind"": ""line"", ""presentation"": ""narration"", ""text"": ""x"", ""next"": ""e"", ""effects"": [""flag_without_prefix""] },
                { ""id"": ""e"", ""kind"": ""end"", ""effects"": [] }
              ]
            }";

            InvalidDataException error = Assert.Throws<InvalidDataException>(() => new DialogueRunner(DialogueScript.FromJson(json), new StoryState()));
            StringAssert.Contains("'a'", error.Message);
        }

        [Test]
        public void ChooseOutOfRange_Throws()
        {
            DialogueRunner runner = CreateRunner(BranchingJson, out _);
            runner.Begin();

            Assert.Throws<ArgumentOutOfRangeException>(() => runner.Choose(5));
        }

        [Test]
        public void AdvanceBeforeBegin_Throws()
        {
            DialogueRunner runner = CreateRunner(SubjectiveJson, out _);

            Assert.Throws<InvalidOperationException>(() => runner.Advance());
        }

        [Test]
        public void EmptyJson_Throws()
        {
            Assert.Throws<ArgumentException>(() => DialogueScript.FromJson(string.Empty));
        }

        private static void RunToEnd(DialogueRunner runner, int choiceIndex)
        {
            runner.Begin();
            int guard = 0;
            while (!runner.IsFinished)
            {
                guard++;
                Assert.Less(guard, 100, "Dialogue did not finish; check for a loop in the placeholder graph.");

                if (runner.Current.kind == DialogueKinds.Choice)
                {
                    runner.Choose(choiceIndex);
                }
                else
                {
                    runner.Advance();
                }
            }
        }

        private static DialogueRunner CreateRunner(string json, out StoryState state)
        {
            state = new StoryState();
            return new DialogueRunner(DialogueScript.FromJson(json), state);
        }

        private const string BranchingJson = @"{
          ""id"": ""branch"", ""startNode"": ""pick"",
          ""nodes"": [
            { ""id"": ""pick"", ""kind"": ""choice"", ""presentation"": ""narration"", ""text"": ""pick"",
              ""choices"": [
                { ""text"": ""a"", ""next"": ""after_a"", ""effects"": [""objective:chose_a""] },
                { ""text"": ""b"", ""next"": ""after_b"", ""effects"": [""objective:chose_b""] }
              ] },
            { ""id"": ""after_a"", ""kind"": ""end"", ""effects"": [] },
            { ""id"": ""after_b"", ""kind"": ""end"", ""effects"": [] }
          ]
        }";

        private const string SubjectiveJson = @"{
          ""id"": ""subjective"", ""startNode"": ""recall"",
          ""nodes"": [
            { ""id"": ""recall"", ""kind"": ""line"", ""presentation"": ""recollection"", ""speaker"": ""protagonist"", ""text"": ""r"", ""next"": ""imagine"", ""effects"": [""recollection:remembered_sound""] },
            { ""id"": ""imagine"", ""kind"": ""line"", ""presentation"": ""imagination"", ""speaker"": ""protagonist"", ""text"": ""i"", ""next"": ""done"", ""effects"": [] },
            { ""id"": ""done"", ""kind"": ""end"", ""effects"": [] }
          ]
        }";
    }
}
