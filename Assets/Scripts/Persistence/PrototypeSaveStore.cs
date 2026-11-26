using System;
using System.IO;
using System.Text;
using PostSchool.Character;
using PostSchool.Narrative;
using UnityEngine;

namespace PostSchool.Persistence
{
    [Serializable]
    public sealed class PrototypeSaveData
    {
        public int schemaVersion;
        public int[] attributeValues;
        public string[] objectiveFlags;
        public string[] recollectionFlags;
    }

    /// <summary>
    /// Versioned local snapshot for the prototype's character build and story flags.
    /// It intentionally does not claim to resume a dialogue node or scene position.
    /// </summary>
    public static class PrototypeSaveStore
    {
        public const int CurrentSchemaVersion = 1;

        public static bool TrySave(string path, CharacterBuild build, StoryState storyState, out string error)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "Save path is empty.";
                return false;
            }

            if (build == null || storyState == null)
            {
                error = "A character build and story state are required to save.";
                return false;
            }

            if (!build.IsValid(out error)) return false;

            string temporaryPath = null;
            try
            {
                PrototypeSaveData data = new PrototypeSaveData
                {
                    schemaVersion = CurrentSchemaVersion,
                    attributeValues = build.CopyValues(),
                    objectiveFlags = storyState.CopyObjectiveFlags(),
                    recollectionFlags = storyState.CopyRecollectionFlags(),
                };

                string fullPath = Path.GetFullPath(path);
                string directory = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrEmpty(directory))
                {
                    error = "Save path has no parent directory.";
                    return false;
                }

                Directory.CreateDirectory(directory);
                temporaryPath = fullPath + ".tmp-" + Guid.NewGuid().ToString("N");
                byte[] contents = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, prettyPrint: true));
                using (FileStream stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    stream.Write(contents, 0, contents.Length);
                    stream.Flush(true);
                }

                if (File.Exists(fullPath))
                {
                    File.Replace(temporaryPath, fullPath, null);
                }
                else
                {
                    File.Move(temporaryPath, fullPath);
                }

                error = string.Empty;
                return true;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is ArgumentException ||
                exception is NotSupportedException)
            {
                error = "Could not write prototype save: " + exception.Message;
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(temporaryPath) && File.Exists(temporaryPath))
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch (IOException)
                    {
                        // The reported save result remains authoritative; a leftover temp file is never loaded.
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // The reported save result remains authoritative; a leftover temp file is never loaded.
                    }
                }
            }
        }

        public static bool TryLoad(string path, CharacterBuild build, StoryState storyState, out string error)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "Save path is empty.";
                return false;
            }

            if (build == null || storyState == null)
            {
                error = "A character build and story state are required to load.";
                return false;
            }

            try
            {
                string fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath))
                {
                    error = "No prototype save exists at the configured path.";
                    return false;
                }

                string json = File.ReadAllText(fullPath, Encoding.UTF8);
                PrototypeSaveData data = JsonUtility.FromJson<PrototypeSaveData>(json);
                if (data == null)
                {
                    error = "Save data could not be parsed.";
                    return false;
                }

                if (data.schemaVersion != CurrentSchemaVersion)
                {
                    error = $"Save schema {data.schemaVersion} is not supported; expected {CurrentSchemaVersion}.";
                    return false;
                }

                CharacterBuild validatedBuild = new CharacterBuild();
                if (!validatedBuild.TryRestoreValues(data.attributeValues, out error)) return false;
                StoryState validatedStory = new StoryState();
                if (!validatedStory.TryRestoreFlags(data.objectiveFlags, data.recollectionFlags, out error)) return false;

                // Validate all input before mutating either live state object.
                if (!build.TryRestoreValues(data.attributeValues, out error)) return false;
                if (!storyState.TryRestoreFlags(data.objectiveFlags, data.recollectionFlags, out error)) return false;

                error = string.Empty;
                return true;
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is ArgumentException ||
                exception is NotSupportedException)
            {
                error = "Could not read prototype save: " + exception.Message;
                return false;
            }
        }
    }
}
