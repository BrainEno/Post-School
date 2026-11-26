using System.IO;
using NUnit.Framework;
using PostSchool.Character;
using PostSchool.Narrative;
using PostSchool.Persistence;

namespace PostSchool.Tests.EditMode
{
    [TestFixture]
    public sealed class PrototypeSaveStoreTests
    {
        private string _directory;
        private string _savePath;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "PostSchoolSaveTests", System.Guid.NewGuid().ToString("N"));
            _savePath = Path.Combine(_directory, "prototype-save.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }

        [Test]
        public void SaveAndLoad_RoundTripsBuildAndSeparatedStoryNamespaces()
        {
            CharacterBuild sourceBuild = new CharacterBuild();
            sourceBuild.TryAdjust(CoreAttribute.Intellect, 1);
            sourceBuild.TryAdjust(CoreAttribute.Imagination, 1);
            StoryState sourceStory = new StoryState();
            sourceStory.Set(StoryNamespace.Objective, "door_open");
            sourceStory.Set(StoryNamespace.Recollection, "remembered_shape");

            Assert.IsTrue(PrototypeSaveStore.TrySave(_savePath, sourceBuild, sourceStory, out string saveError), saveError);
            StringAssert.Contains("\"schemaVersion\": 1", File.ReadAllText(_savePath));
            sourceStory.Set(StoryNamespace.Objective, "second_save");
            Assert.IsTrue(PrototypeSaveStore.TrySave(_savePath, sourceBuild, sourceStory, out saveError), saveError);

            CharacterBuild loadedBuild = new CharacterBuild();
            StoryState loadedStory = new StoryState();
            loadedStory.Set(StoryNamespace.Objective, "stale_flag");
            Assert.IsTrue(PrototypeSaveStore.TryLoad(_savePath, loadedBuild, loadedStory, out string loadError), loadError);

            Assert.AreEqual(2, loadedBuild.GetValue(CoreAttribute.Intellect));
            Assert.AreEqual(2, loadedBuild.GetValue(CoreAttribute.Imagination));
            Assert.IsTrue(loadedStory.IsSet(StoryNamespace.Objective, "door_open"));
            Assert.IsFalse(loadedStory.IsSet(StoryNamespace.Objective, "remembered_shape"));
            Assert.IsTrue(loadedStory.IsSet(StoryNamespace.Recollection, "remembered_shape"));
            Assert.IsFalse(loadedStory.IsSet(StoryNamespace.Objective, "stale_flag"));
        }

        [Test]
        public void Load_RejectsUnsupportedSchemaWithoutMutatingLiveState()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_savePath, "{\"schemaVersion\":99,\"attributeValues\":[1,1,1,1,1],\"objectiveFlags\":[],\"recollectionFlags\":[]}");
            CharacterBuild build = new CharacterBuild();
            build.TryAdjust(CoreAttribute.Physique, 1);
            StoryState state = new StoryState();
            state.Set(StoryNamespace.Objective, "keep");

            Assert.IsFalse(PrototypeSaveStore.TryLoad(_savePath, build, state, out string error));
            StringAssert.Contains("not supported", error);
            Assert.AreEqual(2, build.GetValue(CoreAttribute.Physique));
            Assert.IsTrue(state.IsSet(StoryNamespace.Objective, "keep"));
        }

        [Test]
        public void Load_RejectsInvalidSnapshotWithoutMutatingEitherState()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(_savePath, "{\"schemaVersion\":1,\"attributeValues\":[5,5,5,1,1],\"objectiveFlags\":[\"replace\"],\"recollectionFlags\":[]}");
            CharacterBuild build = new CharacterBuild();
            StoryState state = new StoryState();
            state.Set(StoryNamespace.Objective, "keep");

            Assert.IsFalse(PrototypeSaveStore.TryLoad(_savePath, build, state, out string error));
            StringAssert.Contains("exceeds", error);
            Assert.AreEqual(1, build.GetValue(CoreAttribute.Physique));
            Assert.IsTrue(state.IsSet(StoryNamespace.Objective, "keep"));
            Assert.IsFalse(state.IsSet(StoryNamespace.Objective, "replace"));
        }

        [Test]
        public void Load_MissingFileReturnsActionableFailure()
        {
            CharacterBuild build = new CharacterBuild();
            StoryState state = new StoryState();

            Assert.IsFalse(PrototypeSaveStore.TryLoad(_savePath, build, state, out string error));
            StringAssert.Contains("No prototype save", error);
        }
    }
}
