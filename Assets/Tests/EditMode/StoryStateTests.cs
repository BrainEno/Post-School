using System;
using System.Collections.Generic;
using NUnit.Framework;
using PostSchool.Narrative;

namespace PostSchool.Tests.EditMode
{
    [TestFixture]
    public sealed class StoryStateTests
    {
        [Test]
        public void Set_SameFlagTwice_RaisesChangeOnce()
        {
            StoryState state = new StoryState();
            List<StoryFlagChange> changes = new List<StoryFlagChange>();
            state.FlagChanged += change => changes.Add(change);

            state.Set(StoryNamespace.Objective, "textbook_missing");
            state.Set(StoryNamespace.Objective, "textbook_missing");

            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual("textbook_missing", changes[0].Flag);
        }

        [Test]
        public void Namespaces_AreIndependent()
        {
            StoryState state = new StoryState();

            state.Set(StoryNamespace.Recollection, "tried_to_remember");

            Assert.IsTrue(state.IsSet(StoryNamespace.Recollection, "tried_to_remember"));
            Assert.IsFalse(state.IsSet(StoryNamespace.Objective, "tried_to_remember"));
        }

        [Test]
        public void EmptyFlagName_Throws()
        {
            StoryState state = new StoryState();

            Assert.Throws<ArgumentException>(() => state.Set(StoryNamespace.Objective, "   "));
        }

        [Test]
        public void Reset_ClearsBothNamespacesWithoutEvents()
        {
            StoryState state = new StoryState();
            state.Set(StoryNamespace.Objective, "a");
            state.Set(StoryNamespace.Recollection, "b");
            int events = 0;
            state.FlagChanged += _ => events++;

            state.Reset();

            Assert.AreEqual(0, state.ObjectiveFlags.Count);
            Assert.AreEqual(0, state.RecollectionFlags.Count);
            Assert.AreEqual(0, events);
        }

        [Test]
        public void RestoreReplacesNamespacesWithoutEventsAndCopiesInStableOrder()
        {
            StoryState state = new StoryState();
            state.Set(StoryNamespace.Objective, "old");
            int events = 0;
            state.FlagChanged += _ => events++;

            Assert.IsTrue(state.TryRestoreFlags(new[] { "zeta", "alpha" }, new[] { "memory" }, out string error), error);

            CollectionAssert.AreEqual(new[] { "alpha", "zeta" }, state.CopyObjectiveFlags());
            CollectionAssert.AreEqual(new[] { "memory" }, state.CopyRecollectionFlags());
            Assert.IsFalse(state.IsSet(StoryNamespace.Objective, "old"));
            Assert.AreEqual(0, events, "Loading state must not impersonate gameplay flag changes.");
        }

        [Test]
        public void RestoreRejectsInvalidFlagsAtomically()
        {
            StoryState state = new StoryState();
            state.Set(StoryNamespace.Objective, "keep_me");

            Assert.IsFalse(state.TryRestoreFlags(new[] { "replace_me" }, new[] { " " }, out string error));
            Assert.IsNotEmpty(error);
            Assert.IsTrue(state.IsSet(StoryNamespace.Objective, "keep_me"));
            Assert.IsFalse(state.IsSet(StoryNamespace.Objective, "replace_me"));
        }

        [Test]
        public void StoryEffect_ParsesBothPrefixes()
        {
            StoryEffect.Parse("objective:textbook_missing", out StoryNamespace objective, out string objectiveFlag);
            StoryEffect.Parse("recollection:tried_to_remember", out StoryNamespace recollection, out string recollectionFlag);

            Assert.AreEqual(StoryNamespace.Objective, objective);
            Assert.AreEqual("textbook_missing", objectiveFlag);
            Assert.AreEqual(StoryNamespace.Recollection, recollection);
            Assert.AreEqual("tried_to_remember", recollectionFlag);
        }

        [Test]
        public void StoryEffect_RejectsUnknownPrefixAndEmptyFlag()
        {
            Assert.Throws<FormatException>(() => StoryEffect.Parse("memory:x", out _, out _));
            Assert.Throws<FormatException>(() => StoryEffect.Parse("objective:", out _, out _));
        }
    }
}
