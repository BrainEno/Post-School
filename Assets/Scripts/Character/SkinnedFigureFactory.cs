using System;
using System.Collections.Generic;
using UnityEngine;

namespace PostSchool.Character
{
    /// <summary>
    /// Turns a <see cref="HumanoidProportions"/> table into one continuous skinned mesh per character.
    ///
    /// Difference from the old primitive factory: a limb is a tapered tube whose rings are weighted
    /// along a bone chain, so the elbow, knee and waist bend as a surface instead of two capsules
    /// crossing. The head is a real skull shape with a jaw, the hands and feet have volume, and the
    /// coat is a separate skirt of geometry with its own weights so it swings with the hips.
    ///
    /// Every character is authored in the rest pose (the skeleton's bind pose) and then handed to a
    /// SkinnedMeshRenderer. Nothing here imports a model, a texture or a rig.
    /// </summary>
    public static class SkinnedFigureFactory
    {
        public const string MeshName = "Skinned Figure";

        /// <summary>Bone chains the mesh builder weights against, in the order the tubes are built.</summary>
        public sealed class Weights
        {
            public ProceduralSkeleton Skeleton;
            public HumanoidProportions Proportions;
        }

        /// <summary>
        /// Builds the body mesh for a proportions table. <paramref name="role"/> chooses the silhouette
        /// details (coat skirt, sleeve band, hair mass) without changing the skeleton.
        /// </summary>
        public static Mesh BuildBody(HumanoidProportions proportions, ProceduralSkeleton skeleton, FigureRole role, out BoneWeight4[] weights)
        {
            return BuildBody(proportions, skeleton, role, out weights, out _);
        }

        /// <summary>
        /// Builds the body as one mesh with submeshes in a fixed material order: skin, hair, coat, shirt.
        /// One mesh keeps the skinning and the draw call count honest; submeshes keep the palette
        /// readable instead of baking a vertex colour atlas nobody can edit.
        /// </summary>
        public static Mesh BuildBody(HumanoidProportions proportions, ProceduralSkeleton skeleton, FigureRole role, out BoneWeight4[] weights, out int submeshCount)
        {
            if (proportions == null) throw new ArgumentNullException(nameof(proportions));
            if (skeleton == null) throw new ArgumentNullException(nameof(skeleton));

            SkinnedMeshBuilder builder = new SkinnedMeshBuilder();
            WeightField field = new WeightField(skeleton);

            // Surface order follows CharacterSurface: skin, hair, coat, shirt. Submeshes are opened in
            // the order they are first written, so each part declares its own surface as it goes.
            BuildLegsSkin(builder, field, proportions);
            BuildPelvisSkin(builder, field, proportions);
            BuildArmsSkin(builder, field, proportions);
            BuildHeadSkin(builder, field, proportions);

            builder.BeginSurface(CharacterSurface.Hair);
            BuildHeadHair(builder, field, proportions, role);

            builder.BeginSurface(CharacterSurface.Coat);
            BuildTorsoCoat(builder, field, proportions);
            BuildLegsTrousers(builder, field, proportions);
            BuildArmsCoat(builder, field, proportions);
            if (role == FigureRole.Protagonist) BuildCoatSkirt(builder, field, proportions);
            if (role == FigureRole.Protagonist) BuildBag(builder, field, proportions);
            if (role == FigureRole.Protagonist) BuildSleeveBand(builder, field, proportions);

            // The last slot carries the drawn marks (face and collar). It shares the shirt material
            // because both are the figure's brightest surface, and marks must read as ink on paper.
            builder.BeginSurface(CharacterSurface.Shirt);
            BuildHeadFace(builder, field, proportions, role);
            BuildShirtDetails(builder, field, proportions, role);

            weights = builder.BuildWeights();
            submeshCount = builder.SubmeshCount;
            Mesh mesh = builder.Build(MeshName + " (" + role + ")");
            return mesh;
        }

        // ---------------------------------------------------------------- skin surfaces

        private static void BuildLegsSkin(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            for (int side = 0; side < 2; side++)
            {
                string foot = side == 0 ? HumanoidBones.FootLeft : HumanoidBones.FootRight;
                Vector3 ankle = p.RestPosition(foot);
                BoneWeight4 footWeight = field.Single(foot);

                // Boot: a sole slab plus a rounded toe, both fully on the foot bone.
                builder.AddBox(ankle + new Vector3(0f, -0.035f, 0.045f), new Vector3(0.052f, 0.032f, 0.115f), footWeight);
                builder.AddEllipsoid(ankle + new Vector3(0f, -0.018f, 0.055f), new Vector3(0.05f, 0.038f, 0.1f), 10, 6, _ => footWeight);
            }
        }

        private static void BuildPelvisSkin(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            // Pedestal under the hips: hidden by the coat, visible if the coat is ever removed.
            Vector3 hips = p.RestPosition(HumanoidBones.Hips);
            BoneWeight4 hipsWeight = field.Single(HumanoidBones.Hips);
            builder.AddEllipsoid(hips + new Vector3(0f, -0.02f, 0f), new Vector3(0.1f, 0.08f, 0.075f), 14, 8, _ => hipsWeight);
        }

        private static void BuildArmsSkin(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                string fore = left ? HumanoidBones.ForearmLeft : HumanoidBones.ForearmRight;
                string hand = left ? HumanoidBones.HandLeft : HumanoidBones.HandRight;
                Vector3 wrist = p.RestPosition(hand);

                // Hands: palm box, thumb and a knuckle ridge. Deliberately adult-sized on both figures.
                BoneWeight4 handWeight = field.Single(hand);
                Vector3 palm = wrist + new Vector3(0f, -0.03f, 0.004f);
                builder.AddBox(palm, new Vector3(0.026f, 0.042f, 0.016f), handWeight);
                builder.AddEllipsoid(wrist + new Vector3(0f, -0.022f, 0.024f), new Vector3(0.014f, 0.026f, 0.016f), 8, 5, _ => handWeight);
                builder.AddBox(wrist + new Vector3(0f, -0.068f, 0.008f), new Vector3(0.024f, 0.012f, 0.014f), handWeight);
            }
        }

        private static void BuildHeadSkin(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            BoneWeight4 headWeight = field.Single(HumanoidBones.Head);
            Vector3 headBone = p.RestPosition(HumanoidBones.Head);
            Vector3 skullCenter = headBone + new Vector3(0f, p.headLength * 0.52f, 0.004f);

            // Skull, then a jaw shell hung slightly forward of it: the jaw is what carries age.
            builder.AddEllipsoid(skullCenter, new Vector3(p.headRadius, p.headLength * 0.5f, p.headRadius * 1.02f), 18, 12, _ => headWeight);
            builder.AddEllipsoid(headBone + new Vector3(0f, p.headLength * 0.2f, 0.024f),
                new Vector3(p.headRadius * 0.8f, p.headLength * 0.24f, p.headRadius * 0.84f), 12, 8, _ => headWeight);

            // Ears, slightly different heights: nobody's ears match.
            float earX = p.headRadius * 0.95f;
            builder.AddEllipsoid(skullCenter + new Vector3(-earX, -p.headLength * 0.05f, -0.004f), new Vector3(0.013f, 0.027f, 0.017f), 8, 5, _ => headWeight);
            builder.AddEllipsoid(skullCenter + new Vector3(earX, -p.headLength * 0.062f, -0.004f), new Vector3(0.013f, 0.027f, 0.017f), 8, 5, _ => headWeight);

            // Neck: a short tube weighted between chest and neck so it does not detach when the head turns.
            Vector3 chest = p.RestPosition(HumanoidBones.Chest);
            Vector3 neck = p.RestPosition(HumanoidBones.Neck);
            BoneWeight4 neckWeight = field.Blend(HumanoidBones.Neck, HumanoidBones.Chest, 0.75f, 0.25f);
            builder.AddTube(
                new List<Vector3> { chest + new Vector3(0f, 0.14f, 0f), neck, headBone },
                new List<float> { 0.05f, 0.045f, 0.043f },
                new List<BoneWeight4> { field.Blend(HumanoidBones.Chest, HumanoidBones.Neck, 0.6f, 0.4f), neckWeight, headWeight },
                10, capStart: true, capEnd: false);
        }

        private static void BuildHeadHair(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p, FigureRole role)
        {
            BoneWeight4 headWeight = field.Single(HumanoidBones.Head);
            Vector3 headBone = p.RestPosition(HumanoidBones.Head);
            Vector3 skullCenter = headBone + new Vector3(0f, p.headLength * 0.52f, 0.004f);
            Vector3 hairCenter = skullCenter + new Vector3(0f, p.headLength * 0.07f, -0.006f);

            // Hair shell, flatter and wider than the skull so the outline reads as a cut shape.
            builder.AddEllipsoid(hairCenter, new Vector3(p.headRadius * 1.09f, p.headLength * 0.47f, p.headRadius * 1.1f), 18, 10, _ => headWeight);

            // Fringe: two slabs at the brow, one longer than the other. The asymmetry is the point.
            builder.AddQuad(skullCenter + new Vector3(0.014f, p.headLength * 0.28f, p.headRadius * 0.95f), Vector3.right, Vector3.up, 0.054f, 0.026f, headWeight, 0.002f);
            builder.AddQuad(skullCenter + new Vector3(-0.032f, p.headLength * 0.33f, p.headRadius * 0.92f), Vector3.right, Vector3.up, 0.038f, 0.034f, headWeight, 0.002f);

            if (role == FigureRole.Protagonist)
            {
                // Two spikes: the wrong direction on purpose, the one thing that makes the head shape
                // recognisable from behind.
                builder.AddBox(hairCenter + new Vector3(0.062f, p.headLength * 0.24f, -0.01f), new Vector3(0.016f, 0.05f, 0.016f), headWeight);
                builder.AddBox(hairCenter + new Vector3(-0.03f, p.headLength * 0.28f, -0.03f), new Vector3(0.014f, 0.04f, 0.014f), headWeight);
            }
            else
            {
                // Q's hair is combed flat and too neat: it is the only tidy thing about them.
                builder.AddQuad(skullCenter + new Vector3(0f, p.headLength * 0.34f, p.headRadius * 0.7f), Vector3.right, Vector3.up, 0.06f, 0.02f, headWeight, 0.003f);
            }
        }

        private static void BuildHeadFace(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p, FigureRole role)
        {
            BoneWeight4 headWeight = field.Single(HumanoidBones.Head);
            Vector3 skullCenter = p.RestPosition(HumanoidBones.Head) + new Vector3(0f, p.headLength * 0.52f, 0.004f);
            float faceZ = p.headRadius * 0.985f;
            float eyeY = p.headLength * 0.06f;

            // The face is drawn, not sculpted: flat marks carried as geometry so they survive posing.
            if (role == FigureRole.Protagonist)
            {
                // One eye a slit, the other a hollow. Two different ages sharing one face.
                builder.AddQuad(skullCenter + new Vector3(-0.032f, eyeY, faceZ), Vector3.right, Vector3.up, 0.022f, 0.004f, headWeight, 0.001f);
                builder.AddEllipsoid(skullCenter + new Vector3(0.034f, eyeY, faceZ - 0.004f), new Vector3(0.016f, 0.012f, 0.008f), 10, 6, _ => headWeight);

                // The seam, and a second line that does not meet it.
                builder.AddQuad(skullCenter + new Vector3(0.004f, -p.headLength * 0.02f, faceZ), Vector3.right, Vector3.up, 0.0035f, p.headLength * 0.33f, headWeight, 0.001f);
                builder.AddQuad(skullCenter + new Vector3(0.021f, -p.headLength * 0.17f, faceZ), Vector3.right, Vector3.up, 0.0028f, p.headLength * 0.13f, headWeight, 0.001f);

                // Mouth: a short line, drawn low so the face reads as long.
                builder.AddQuad(skullCenter + new Vector3(0.002f, -p.headLength * 0.24f, faceZ * 0.99f), Vector3.right, Vector3.up, 0.019f, 0.003f, headWeight, 0.001f);
            }
            else
            {
                // Q's face is almost blank: two level marks and a mouth that is exactly horizontal.
                builder.AddQuad(skullCenter + new Vector3(-0.03f, eyeY, faceZ), Vector3.right, Vector3.up, 0.018f, 0.003f, headWeight, 0.001f);
                builder.AddQuad(skullCenter + new Vector3(0.03f, eyeY, faceZ), Vector3.right, Vector3.up, 0.018f, 0.003f, headWeight, 0.001f);
                builder.AddQuad(skullCenter + new Vector3(0f, -p.headLength * 0.2f, faceZ * 0.99f), Vector3.right, Vector3.up, 0.017f, 0.0025f, headWeight, 0.001f);
            }
        }

        // -------------------------------------------------------------- coat / trousers

        private static void BuildTorsoCoat(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            Vector3 hips = p.RestPosition(HumanoidBones.Hips);
            Vector3 spine = p.RestPosition(HumanoidBones.Spine);
            Vector3 chest = p.RestPosition(HumanoidBones.Chest);
            Vector3 neck = p.RestPosition(HumanoidBones.Neck);
            float depth = p.chestDepth;

            List<Vector3> spineLine = new List<Vector3>
            {
                hips + new Vector3(0f, 0.06f, 0f),
                spine,
                chest,
                Vector3.Lerp(chest, neck, 0.5f),
                neck,
            };
            List<float> radii = new List<float> { 0.112f, 0.116f, 0.126f, 0.112f, 0.06f };
            List<BoneWeight4> ringWeights = new List<BoneWeight4>
            {
                field.Blend(HumanoidBones.Hips, HumanoidBones.Spine, 0.7f, 0.3f),
                field.Single(HumanoidBones.Spine),
                field.Blend(HumanoidBones.Spine, HumanoidBones.Chest, 0.35f, 0.65f),
                field.Single(HumanoidBones.Chest),
                field.Blend(HumanoidBones.Chest, HumanoidBones.Neck, 0.6f, 0.4f),
            };

            // Squashed on z: a ribcage is wider than it is deep.
            builder.AddTube(spineLine, radii, ringWeights, 16, capStart: true, capEnd: true, squashX: 1f, squashZ: depth / 0.115f);

            // Shoulder caps, weighted to the clavicles so the sleeves never separate from the body.
            for (int side = 0; side < 2; side++)
            {
                string shoulder = side == 0 ? HumanoidBones.ShoulderLeft : HumanoidBones.ShoulderRight;
                Vector3 at = p.RestPosition(shoulder);
                BoneWeight4 weight = field.Blend(shoulder, HumanoidBones.Chest, 0.75f, 0.25f);
                builder.AddEllipsoid(at + new Vector3(0f, 0.004f, 0f), new Vector3(0.056f, 0.05f, 0.056f), 10, 6, _ => weight);
            }
        }

        private static void BuildLegsTrousers(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                string thigh = left ? HumanoidBones.ThighLeft : HumanoidBones.ThighRight;
                string shin = left ? HumanoidBones.ShinLeft : HumanoidBones.ShinRight;
                string foot = left ? HumanoidBones.FootLeft : HumanoidBones.FootRight;

                Vector3 hip = p.RestPosition(thigh);
                Vector3 knee = p.RestPosition(shin);
                Vector3 ankle = p.RestPosition(foot);

                // One tube from hip to ankle with rings at the knee: this is the whole point of the
                // rebuild. The knee bends as a surface instead of two cylinders crossing over.
                List<Vector3> spine = new List<Vector3>
                {
                    hip,
                    Vector3.Lerp(hip, knee, 0.45f),
                    knee,
                    Vector3.Lerp(knee, ankle, 0.55f),
                    ankle + new Vector3(0f, 0.02f, 0f),
                };
                List<float> radii = new List<float> { 0.074f, 0.065f, 0.055f, 0.048f, 0.044f };
                List<BoneWeight4> ringWeights = new List<BoneWeight4>
                {
                    field.Blend(thigh, HumanoidBones.Hips, 0.85f, 0.15f),
                    field.Single(thigh),
                    field.Blend(thigh, shin, 0.5f, 0.5f),
                    field.Single(shin),
                    field.Blend(shin, foot, 0.75f, 0.25f),
                };

                builder.AddTube(spine, radii, ringWeights, 12, capStart: true, capEnd: true);
            }
        }

        private static void BuildArmsCoat(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                string upper = left ? HumanoidBones.UpperArmLeft : HumanoidBones.UpperArmRight;
                string fore = left ? HumanoidBones.ForearmLeft : HumanoidBones.ForearmRight;
                string hand = left ? HumanoidBones.HandLeft : HumanoidBones.HandRight;

                Vector3 shoulderJoint = p.RestPosition(upper);
                Vector3 elbow = p.RestPosition(fore);
                Vector3 wrist = p.RestPosition(hand);

                List<Vector3> spine = new List<Vector3>
                {
                    shoulderJoint,
                    Vector3.Lerp(shoulderJoint, elbow, 0.5f),
                    elbow,
                    Vector3.Lerp(elbow, wrist, 0.55f),
                    Vector3.Lerp(elbow, wrist, 0.94f),
                };
                List<float> radii = new List<float> { 0.05f, 0.045f, 0.041f, 0.036f, 0.031f };
                List<BoneWeight4> ringWeights = new List<BoneWeight4>
                {
                    field.Blend(upper, HumanoidBones.ShoulderLeft, 0.8f, 0.2f),
                    field.Single(upper),
                    field.Blend(upper, fore, 0.5f, 0.5f),
                    field.Single(fore),
                    field.Blend(fore, hand, 0.7f, 0.3f),
                };

                builder.AddTube(spine, radii, ringWeights, 10, capStart: true, capEnd: true);
            }
        }

        private static void BuildCoatSkirt(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            // Waist to below the knee, hips-weighted with a little thigh so it swings when walking.
            Vector3 waist = p.RestPosition(HumanoidBones.Spine);
            Vector3 hem = p.RestPosition(HumanoidBones.Hips) + new Vector3(0f, -0.31f, 0f);

            List<Vector3> spine = new List<Vector3> { waist, Vector3.Lerp(waist, hem, 0.55f), hem };
            List<float> radii = new List<float> { 0.132f, 0.152f, 0.168f };
            List<BoneWeight4> weights = new List<BoneWeight4>
            {
                field.Blend(HumanoidBones.Spine, HumanoidBones.Hips, 0.55f, 0.45f),
                field.Single(HumanoidBones.Hips),
                field.Blend(HumanoidBones.Hips, HumanoidBones.ThighLeft, 0.85f, 0.15f),
            };

            builder.AddTube(spine, radii, weights, 18, capStart: false, capEnd: false, squashX: 1f, squashZ: 0.82f);
        }

        private static void BuildBag(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            // The bag is a separate object in the world; here it is hips-weighted geometry so the
            // silhouette carries it without a second renderer.
            Vector3 hips = p.RestPosition(HumanoidBones.Hips);
            BoneWeight4 hipsWeight = field.Single(HumanoidBones.Hips);
            Vector3 bag = hips + new Vector3(-0.152f, -0.07f, -0.05f);

            builder.AddBox(bag, new Vector3(0.072f, 0.082f, 0.042f), hipsWeight);
            builder.AddQuad(bag + new Vector3(0f, 0.058f, 0.044f), Vector3.right, Vector3.up, 0.07f, 0.028f, hipsWeight, 0.001f);
            builder.AddBox(bag + new Vector3(0f, 0.088f, 0f), new Vector3(0.008f, 0.055f, 0.04f), hipsWeight);
        }

        private static void BuildSleeveBand(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            Vector3 shoulder = p.RestPosition(HumanoidBones.UpperArmRight);
            Vector3 elbow = p.RestPosition(HumanoidBones.ForearmRight);
            Vector3 band = Vector3.Lerp(shoulder, elbow, 0.38f);
            BoneWeight4 weight = field.Single(HumanoidBones.UpperArmRight);
            builder.AddEllipsoid(band, new Vector3(0.048f, 0.013f, 0.048f), 12, 4, _ => weight);
        }

        private static void BuildShirtDetails(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p, FigureRole role)
        {
            Vector3 chest = p.RestPosition(HumanoidBones.Chest);
            float depth = p.chestDepth;
            BoneWeight4 chestWeight = field.Single(HumanoidBones.Chest);

            // Collar points and a placket. Only the protagonist wears a school shirt under an adult coat.
            if (role == FigureRole.Protagonist)
            {
                builder.AddQuad(chest + new Vector3(0f, 0.02f, depth + 0.004f), Vector3.right, Vector3.up, 0.036f, 0.12f, chestWeight);
                builder.AddQuad(chest + new Vector3(-0.05f, 0.072f, depth + 0.003f), Vector3.right, Vector3.up, 0.046f, 0.03f, chestWeight);
                builder.AddQuad(chest + new Vector3(0.05f, 0.072f, depth + 0.003f), Vector3.right, Vector3.up, 0.046f, 0.03f, chestWeight);
            }
            else
            {
                // Q: a high, buttoned collar with no gap. Nothing showing.
                builder.AddQuad(chest + new Vector3(0f, 0.085f, depth + 0.004f), Vector3.right, Vector3.up, 0.052f, 0.026f, chestWeight);
            }
        }

        /// <summary>
        /// Weight lookups by bone name. Kept separate so the mesh builder reads as shape, not index math.
        /// </summary>
        private sealed class WeightField
        {
            private readonly ProceduralSkeleton _skeleton;

            public WeightField(ProceduralSkeleton skeleton)
            {
                _skeleton = skeleton ?? throw new ArgumentNullException(nameof(skeleton));
            }

            public BoneWeight4 Single(string bone)
            {
                int index = Index(bone);
                return BoneWeight4.Single(index).Normalized();
            }

            /// <summary>Two influences with explicit weights, renormalised so they always sum to one.</summary>
            public BoneWeight4 Blend(string dominant, string secondary, float dominantWeight, float secondaryWeight)
            {
                BoneWeight4 weight = new BoneWeight4
                {
                    bone0 = Index(dominant),
                    bone1 = Index(secondary),
                    weight0 = dominantWeight,
                    weight1 = secondaryWeight,
                };

                return weight.Normalized();
            }

            private int Index(string bone)
            {
                int index = _skeleton.IndexOf(bone);
                if (index < 0)
                {
                    throw new ArgumentException($"Bone '{bone}' is not in the skeleton.", nameof(bone));
                }

                return index;
            }
        }
    }

    /// <summary>Which silhouette details to build. The skeleton is identical for both.</summary>
    public enum FigureRole
    {
        Protagonist,
        ClassmateQ,
    }

    /// <summary>A built figure: bone hierarchy, skinned mesh and the renderer that poses it.</summary>
    public sealed class SkinnedFigure
    {
        internal SkinnedFigure(Transform root, Transform skeletonRoot, ProceduralSkeleton skeleton, SkinnedMeshRenderer renderer, Mesh mesh, BoneWeight4[] weights, Vector3[] bindVertices)
        {
            Root = root;
            SkeletonRoot = skeletonRoot;
            Skeleton = skeleton;
            Renderer = renderer;
            Mesh = mesh;
            Weights = weights;
            BindVertices = bindVertices;
        }

        public Transform Root { get; }
        public Transform SkeletonRoot { get; }
        public ProceduralSkeleton Skeleton { get; }
        public SkinnedMeshRenderer Renderer { get; }
        public Mesh Mesh { get; }

        /// <summary>Per-vertex bone weights, in the same order as the mesh vertices.</summary>
        public BoneWeight4[] Weights { get; }

        /// <summary>The mesh's rest-pose vertices; used by tests and by EditMode deformation checks.</summary>
        public Vector3[] BindVertices { get; }

        public Transform Bone(string name)
        {
            return Skeleton.Bone(Skeleton.IndexOf(name));
        }

        /// <summary>
        /// Destroys the mesh created at runtime. The GameObject tree is destroyed by the scene owner.
        /// </summary>
        public void DisposeMesh()
        {
            if (Mesh == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(Mesh);
            else UnityEngine.Object.DestroyImmediate(Mesh);
        }
    }

    /// <summary>
    /// Builds a complete skinned character: proportions -> bone hierarchy -> one skinned mesh ->
    /// SkinnedMeshRenderer, with the bind pose captured between the two.
    /// </summary>
    public static class SkinnedCharacterBuilder
    {
        public const string RootPrefix = "Skinned ";

        /// <summary>Builds the figure under a new root at the given world position and facing.</summary>
        public static SkinnedFigure Create(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            float yawDegrees,
            HumanoidProportions proportions,
            FigureRole role,
            CharacterSkin skin)
        {
            if (string.IsNullOrWhiteSpace(objectName)) throw new ArgumentException("A character needs a name.", nameof(objectName));
            if (proportions == null) throw new ArgumentNullException(nameof(proportions));
            if (skin == null) throw new ArgumentNullException(nameof(skin));

            GameObject root = new GameObject(RootPrefix + objectName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);

            // The figure's own origin is at the feet, which is also y = 0 in the proportion table.
            Transform figure = new GameObject("Figure").transform;
            figure.SetParent(root.transform, false);

            ProceduralSkeleton skeleton = HumanoidRig.Create(figure, proportions, out Transform skeletonRoot);

            Mesh mesh = SkinnedFigureFactory.BuildBody(proportions, skeleton, role, out BoneWeight4[] weights);
            Vector3[] bindVertices = mesh.vertices;

            GameObject meshObject = new GameObject("Body");
            meshObject.transform.SetParent(figure, false);
            SkinnedMeshRenderer renderer = meshObject.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;

            // Submesh order is fixed by CharacterSkinMaterialSlot. A skin with fewer materials reuses
            // the body material for the missing slots rather than rendering nothing.
            renderer.sharedMaterials = skin.ToMaterials(renderer.sharedMesh.subMeshCount);
            renderer.rootBone = skeletonRoot;
            renderer.bones = ToArray(skeleton.Bones);
            renderer.updateWhenOffscreen = true;

            // Bind poses must be captured in the rest pose, before anything animates.
            skeleton.CaptureBindPose(meshObject.transform);
            renderer.sharedMesh.bindposes = skeleton.BindPoses(meshObject.transform);
            renderer.sharedMesh.boneWeights = ToUnityWeights(weights);

            return new SkinnedFigure(root.transform, skeletonRoot, skeleton, renderer, mesh, weights, bindVertices);
        }

        private static Transform[] ToArray(IReadOnlyList<Transform> bones)
        {
            Transform[] array = new Transform[bones.Count];
            for (int i = 0; i < bones.Count; i++) array[i] = bones[i];
            return array;
        }

        private static BoneWeight[] ToUnityWeights(BoneWeight4[] weights)
        {
            BoneWeight[] unity = new BoneWeight[weights.Length];
            for (int i = 0; i < weights.Length; i++)
            {
                BoneWeight4 w = weights[i];
                unity[i] = new BoneWeight
                {
                    boneIndex0 = w.bone0,
                    boneIndex1 = w.bone1,
                    boneIndex2 = w.bone2,
                    boneIndex3 = w.bone3,
                    weight0 = w.weight0,
                    weight1 = w.weight1,
                    weight2 = w.weight2,
                    weight3 = w.weight3,
                };
            }

            return unity;
        }
    }
}
