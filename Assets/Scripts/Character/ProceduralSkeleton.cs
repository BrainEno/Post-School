using System;
using System.Collections.Generic;
using UnityEngine;

namespace PostSchool.Character
{
    /// <summary>Four bone influences for one vertex, matching Unity's skinning limit.</summary>
    [Serializable]
    public struct BoneWeight4
    {
        public int bone0;
        public int bone1;
        public int bone2;
        public int bone3;
        public float weight0;
        public float weight1;
        public float weight2;
        public float weight3;

        public static BoneWeight4 Single(int bone)
        {
            return new BoneWeight4 { bone0 = bone, weight0 = 1f };
        }

        public int Bone(int slot)
        {
            switch (slot)
            {
                case 0: return bone0;
                case 1: return bone1;
                case 2: return bone2;
                case 3: return bone3;
                default: throw new ArgumentOutOfRangeException(nameof(slot), slot, "A vertex has four influences.");
            }
        }

        public float Weight(int slot)
        {
            switch (slot)
            {
                case 0: return weight0;
                case 1: return weight1;
                case 2: return weight2;
                case 3: return weight3;
                default: throw new ArgumentOutOfRangeException(nameof(slot), slot, "A vertex has four influences.");
            }
        }

        /// <summary>Drops influences below the threshold, then renormalises to sum to one.</summary>
        public BoneWeight4 Normalized(float epsilon = 0.001f)
        {
            int[] bones = { bone0, bone1, bone2, bone3 };
            float[] weights = { weight0, weight1, weight2, weight3 };

            float total = 0f;
            for (int i = 0; i < 4; i++)
            {
                if (weights[i] < epsilon) weights[i] = 0f;
                total += weights[i];
            }

            if (total <= 0f)
            {
                return Single(bone0);
            }

            // Keep the four strongest influences, strongest first, so callers can rely on ordering.
            for (int i = 0; i < 4; i++)
            {
                for (int j = i + 1; j < 4; j++)
                {
                    if (weights[j] > weights[i])
                    {
                        (weights[i], weights[j]) = (weights[j], weights[i]);
                        (bones[i], bones[j]) = (bones[j], bones[i]);
                    }
                }
            }

            return new BoneWeight4
            {
                bone0 = bones[0],
                bone1 = bones[1],
                bone2 = bones[2],
                bone3 = bones[3],
                weight0 = weights[0] / total,
                weight1 = weights[1] / total,
                weight2 = weights[2] / total,
                weight3 = weights[3] / total,
            };
        }

        public override string ToString()
        {
            return $"({bone0}:{weight0:0.00} {bone1}:{weight1:0.00} {bone2}:{weight2:0.00} {bone3}:{weight3:0.00})";
        }
    }

    /// <summary>
    /// A bind-pose skeleton plus the linear blend skinning maths that goes with it.
    ///
    /// Every mesh vertex carries up to four bone indices and weights, so one mesh plus this rig gives a
    /// deforming figure instead of a stack of parts. The bind pose is the pose the mesh was authored in;
    /// deformation is done in binder space:
    ///
    ///   boneMatrix = binderRoot^-1 * bone.localToWorld * bindPose^-1
    ///   vertex'    = sum over influences of boneMatrix * vertex * weight
    ///
    /// This mirrors what Unity's SkinnedMeshRenderer does, so the same weights can be handed to the
    /// renderer in play mode and evaluated here in EditMode tests.
    /// </summary>
    public sealed class ProceduralSkeleton
    {
        public const int MaxInfluences = 4;

        private readonly string[] _boneNames;
        private readonly Transform[] _bones;
        private readonly Matrix4x4[] _bindInverses;
        private bool _bound;

        public ProceduralSkeleton(IReadOnlyList<string> boneNames, Transform root)
        {
            if (boneNames == null || boneNames.Count == 0)
            {
                throw new ArgumentException("A skeleton needs at least one bone.", nameof(boneNames));
            }

            if (root == null) throw new ArgumentNullException(nameof(root));

            _boneNames = new string[boneNames.Count];
            _bones = new Transform[boneNames.Count];
            _bindInverses = new Matrix4x4[boneNames.Count];

            for (int i = 0; i < boneNames.Count; i++)
            {
                _boneNames[i] = boneNames[i];
                Transform bone = FindDescendant(root, boneNames[i]);
                if (bone == null)
                {
                    throw new ArgumentException($"Skeleton bone '{boneNames[i]}' is missing under '{root.name}'.", nameof(root));
                }

                _bones[i] = bone;
            }
        }

        public int BoneCount => _bones.Length;

        public IReadOnlyList<Transform> Bones => _bones;

        public IReadOnlyList<string> BoneNames => _boneNames;

        public Transform Bone(int index)
        {
            if (index < 0 || index >= _bones.Length) throw new ArgumentOutOfRangeException(nameof(index), index, "No such bone.");
            return _bones[index];
        }

        public int IndexOf(string boneName)
        {
            return Array.IndexOf(_boneNames, boneName);
        }

        /// <summary>
        /// Records the current pose as the bind pose. Call once, right after building the model in its
        /// rest pose, and before any animation touches the bones.
        /// </summary>
        public void CaptureBindPose(Transform binderRoot)
        {
            if (binderRoot == null) throw new ArgumentNullException(nameof(binderRoot));

            Matrix4x4 worldToBinder = binderRoot.worldToLocalMatrix;
            for (int i = 0; i < _bones.Length; i++)
            {
                Matrix4x4 boneInBinder = worldToBinder * _bones[i].localToWorldMatrix;
                _bindInverses[i] = boneInBinder.inverse;
            }

            _bound = true;
        }

        /// <summary>The matrices a SkinnedMeshRenderer's bones array expects, in bind order.</summary>
        public Matrix4x4[] BindPoses(Transform binderRoot)
        {
            if (binderRoot == null) throw new ArgumentNullException(nameof(binderRoot));

            Matrix4x4 worldToBinder = binderRoot.worldToLocalMatrix;
            Matrix4x4[] poses = new Matrix4x4[_bones.Length];
            for (int i = 0; i < _bones.Length; i++)
            {
                poses[i] = worldToBinder * _bones[i].localToWorldMatrix;
            }

            return poses;
        }

        /// <summary>The bone's current pose relative to its bind pose, in binder space.</summary>
        public Matrix4x4 BoneMatrix(int boneIndex, Transform binderRoot)
        {
            if (!_bound) throw new InvalidOperationException("CaptureBindPose must run before skinning.");
            if (boneIndex < 0 || boneIndex >= _bones.Length) throw new ArgumentOutOfRangeException(nameof(boneIndex), boneIndex, "No such bone.");
            if (binderRoot == null) throw new ArgumentNullException(nameof(binderRoot));

            Matrix4x4 boneInBinder = binderRoot.worldToLocalMatrix * _bones[boneIndex].localToWorldMatrix;
            return boneInBinder * _bindInverses[boneIndex];
        }

        /// <summary>
        /// Skins bind-pose vertices in place (binder space). Returns a new array; the input is untouched.
        /// </summary>
        public Vector3[] Skin(Vector3[] bindVertices, BoneWeight4[] weights, Transform binderRoot)
        {
            if (bindVertices == null) throw new ArgumentNullException(nameof(bindVertices));
            if (weights == null) throw new ArgumentNullException(nameof(weights));
            if (binderRoot == null) throw new ArgumentNullException(nameof(binderRoot));
            if (!_bound) throw new InvalidOperationException("CaptureBindPose must run before skinning.");
            if (weights.Length != bindVertices.Length)
            {
                throw new ArgumentException($"Expected {bindVertices.Length} weights, got {weights.Length}.", nameof(weights));
            }

            Matrix4x4[] matrices = new Matrix4x4[_bones.Length];
            for (int i = 0; i < _bones.Length; i++)
            {
                matrices[i] = BoneMatrix(i, binderRoot);
            }

            Vector3[] skinned = new Vector3[bindVertices.Length];
            for (int v = 0; v < bindVertices.Length; v++)
            {
                BoneWeight4 weight = weights[v];
                Vector3 accumulated = Vector3.zero;
                float total = 0f;
                for (int i = 0; i < MaxInfluences; i++)
                {
                    float influence = weight.Weight(i);
                    if (influence <= 0f) continue;

                    int bone = weight.Bone(i);
                    accumulated += matrices[bone].MultiplyPoint3x4(bindVertices[v]) * influence;
                    total += influence;
                }

                skinned[v] = total > 0f ? accumulated / total : bindVertices[v];
            }

            return skinned;
        }

        /// <summary>
        /// Deforms a mesh in place using its stored bind vertices, for EditMode verification.
        /// Play mode uses the SkinnedMeshRenderer instead; this is the same maths.
        /// </summary>
        public void ApplyToMesh(Mesh mesh, Vector3[] bindVertices, BoneWeight4[] weights, Transform binderRoot)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            mesh.vertices = Skin(bindVertices, weights, binderRoot);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }

        /// <summary>
        /// Distance-weighted influence from a point to a bone segment, used by the mesh builder to blend
        /// skin across a joint instead of leaving a crease.
        /// </summary>
        public static float SegmentInfluence(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd, float falloff)
        {
            if (falloff <= 0f) throw new ArgumentOutOfRangeException(nameof(falloff), falloff, "Falloff must be positive.");

            Vector3 ab = segmentEnd - segmentStart;
            float lengthSquared = ab.sqrMagnitude;
            float t = lengthSquared <= 1e-8f ? 0f : Mathf.Clamp01(Vector3.Dot(point - segmentStart, ab) / lengthSquared);
            float distance = Vector3.Distance(point, segmentStart + ab * t);
            return 1f / (1f + Mathf.Pow(distance / falloff, 4f));
        }

        /// <summary>
        /// True when every vertex has weights summing to one and no zero-influence vertex. The mesh
        /// builder can bake a bad weight by accident; this is the check that catches it.
        /// </summary>
        public static bool WeightsAreValid(BoneWeight4[] weights, int boneCount, out string error)
        {
            if (weights == null || weights.Length == 0)
            {
                error = "No weights.";
                return false;
            }

            for (int v = 0; v < weights.Length; v++)
            {
                float total = 0f;
                for (int i = 0; i < MaxInfluences; i++)
                {
                    int bone = weights[v].Bone(i);
                    if (bone < 0 || bone >= boneCount)
                    {
                        error = $"Vertex {v} references bone {bone} of {boneCount}.";
                        return false;
                    }

                    float weight = weights[v].Weight(i);
                    if (weight < 0f)
                    {
                        error = $"Vertex {v} has a negative weight at slot {i}.";
                        return false;
                    }

                    total += weight;
                }

                if (Mathf.Abs(total - 1f) > 0.01f)
                {
                    error = $"Vertex {v} weights sum to {total:0.000}, not 1.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDescendant(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }
    }
}
