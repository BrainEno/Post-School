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

        /// <summary>
        /// The two shells a head is made of, in the frame the marks are drawn in: the skull ellipsoid
        /// and the jaw ellipsoid hung slightly forward and low. Returned as centre + radius pairs
        /// because that is the only thing a caller needs to put a mark on the skin.
        /// </summary>
        private static void HeadShells(HumanoidProportions p, out Vector3 skullCenter, out Vector3 skullRadius,
                                       out Vector3 jawCenter, out Vector3 jawRadius)
        {
            Vector3 headBone = p.RestPosition(HumanoidBones.Head);
            skullCenter = headBone + new Vector3(0f, p.headLength * 0.52f, 0.004f);
            skullRadius = new Vector3(p.headRadius, p.headLength * 0.5f, p.headRadius * 1.02f);
            jawCenter = headBone + new Vector3(0f, p.headLength * 0.2f, 0.024f);
            jawRadius = new Vector3(p.headRadius * 0.8f, p.headLength * 0.24f, p.headRadius * 0.84f);
        }

        /// <summary>
        /// The front-most point of skin at a given height on the face and a given distance to the side.
        ///
        /// The face is drawn, not sculpted: every mark is a flat quad laid over an ellipsoid, and the
        /// ellipsoid bulges toward the camera by up to 1.02 head-radii. A quad placed at a fixed z is
        /// therefore *behind* the chin and mouth, and those marks render as nothing at all --- the
        /// symptom is a blank lower face, not an error. Marks are placed through this method so they
        /// sit on the surface they are drawn on.
        ///
        /// Returns false when the point is off the head entirely, which is caller error worth knowing
        /// about rather than silently clamping.
        /// </summary>
        private static bool TryFaceSurface(HumanoidProportions p, float y, float x, out Vector3 surface)
        {
            HeadShells(p, out Vector3 skullCenter, out Vector3 skullRadius,
                       out Vector3 jawCenter, out Vector3 jawRadius);

            bool found = false;
            float best = 0f;
            for (int shell = 0; shell < 2; shell++)
            {
                Vector3 centre = shell == 0 ? skullCenter : jawCenter;
                Vector3 radius = shell == 0 ? skullRadius : jawRadius;
                float u = (x - centre.x) / radius.x;
                float v = (y - centre.y) / radius.y;
                float inside = 1f - u * u - v * v;
                if (inside <= 0f) continue;

                float z = centre.z + radius.z * Mathf.Sqrt(inside);
                if (!found || z > best)
                {
                    best = z;
                    found = true;
                }
            }

            surface = new Vector3(x, y, best);
            return found;
        }

        /// <summary>
        /// A flat mark stuck to the face at (x, y), clear of the skin by <paramref name="lift"/> metres.
        ///
        /// The quad keeps its own half-extents and is pushed to the skin's z at its centre, then lifted.
        /// A tall mark (the protagonist's seam runs a third of a head-length) also has to clear the skin
        /// at its own top edge, because the head curves toward the camera as it rises; that extra lift
        /// is measured rather than guessed.
        /// </summary>
        private static void AddFaceMark(SkinnedMeshBuilder builder, HumanoidProportions p, float x, float dy,
                                        float halfWidth, float halfHeight, BoneWeight4 weight, float lift = 0.0004f)
        {
            HeadShells(p, out Vector3 skullCenter, out _, out _, out _);
            float y = skullCenter.y + dy;

            if (!TryFaceSurface(p, y, x, out Vector3 surface)) return;

            // Clear the curve at the top and bottom edges of the quad, not only at its centre.
            float clearance = 0f;
            for (int edge = -1; edge <= 1; edge += 2)
            {
                if (!TryFaceSurface(p, y + edge * halfHeight, x, out Vector3 edgeSurface)) continue;
                clearance = Mathf.Max(clearance, edgeSurface.z - surface.z);
            }

            Vector3 centre = new Vector3(x, y, surface.z + clearance + lift);
            builder.AddQuad(centre, Vector3.right, Vector3.up, halfWidth, halfHeight, weight);
        }


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
            BuildTorsoCoat(builder, field, proportions, role);
            BuildLegsTrousers(builder, field, proportions);
            BuildArmsCoat(builder, field, proportions);
            BuildClothDetails(builder, field, proportions, role);

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
            // N's is a cropped bob: the shell is the same idea but pulled back off the brow and cut
            // above the jaw, so the skull, the ears and the jaw all stay visible. A bob that covers the
            // ears reads as a helmet, which is the failure mode a bare ellipsoid shell falls into.
            bool bob = role == FigureRole.TeacherN;
            if (bob)
            {
                // The bob's shell is pulled back off the brow, but it still has to come over the
                // crown: at 0.44 head-lengths of vertical radius it stopped 1 cm below the top of the
                // skull and the head read as bald from every angle. 0.47 with the same lift clears the
                // crown by 2.5 cm, which is a haircut rather than a helmet.
                builder.AddEllipsoid(hairCenter + new Vector3(0f, 0f, -p.headRadius * 0.035f),
                    new Vector3(p.headRadius * 1.1f, p.headLength * 0.47f, p.headRadius * 1.12f), 18, 10, _ => headWeight);
            }
            else
            {
                builder.AddEllipsoid(hairCenter, new Vector3(p.headRadius * 1.09f, p.headLength * 0.47f, p.headRadius * 1.1f), 18, 10, _ => headWeight);
            }

            // Fringe: two slabs at the brow, one longer than the other. The asymmetry is the point.
            builder.AddQuad(skullCenter + new Vector3(0.014f, p.headLength * 0.28f, p.headRadius * 0.95f), Vector3.right, Vector3.up, 0.054f, 0.026f, headWeight, 0.002f);
            builder.AddQuad(skullCenter + new Vector3(-0.032f, p.headLength * 0.33f, p.headRadius * 0.92f), Vector3.right, Vector3.up, 0.038f, 0.034f, headWeight, 0.002f);

            if (bob)
            {
                // The cut: a straight line of hair across the back of the jaw, plus a side panel each
                // side that stops above the ear. Two cut edges are what make it read as scissors rather
                // than as growth.
                builder.AddQuad(hairCenter + new Vector3(0f, -p.headLength * 0.30f, -p.headRadius * 0.96f),
                    Vector3.right, Vector3.up, p.headRadius * 0.82f, 0.006f, headWeight, 0.001f);
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1f : 1f;
                    builder.AddQuad(hairCenter + new Vector3(sign * p.headRadius * 0.93f, -p.headLength * 0.02f, -p.headRadius * 0.18f),
                        Vector3.forward, Vector3.up, p.headRadius * 0.42f, 0.005f, headWeight, 0.001f);
                }
            }

            if (role == FigureRole.TeacherB)
            {
                BuildTeacherBHair(builder, p, headWeight, skullCenter, hairCenter);
            }
            else if (role == FigureRole.Protagonist)
            {
                // The hair is the first thing anyone says about him: badly in need of a cut, and pushed
                // back with a hand rather than combed. Two spikes were not enough to read as that ---
                // they looked like a crown. Five masses at different angles, three of them breaking the
                // silhouette and two lying flat on the crown, give a shape that is wrong from every side
                // instead of wrong from one.
                //
                // The angle is the point: each slab is rotated differently, so no two edges are parallel
                // and the outline never resolves into a haircut.
                builder.AddOrientedBox(hairCenter + new Vector3(0.058f, p.headLength * 0.235f, -0.012f),
                    new Vector3(0.017f, 0.052f, 0.015f), Quaternion.Euler(8f, 24f, -14f), headWeight);
                builder.AddOrientedBox(hairCenter + new Vector3(-0.034f, p.headLength * 0.275f, -0.028f),
                    new Vector3(0.015f, 0.044f, 0.014f), Quaternion.Euler(-10f, -18f, 12f), headWeight);
                builder.AddOrientedBox(hairCenter + new Vector3(0.004f, p.headLength * 0.305f, 0.030f),
                    new Vector3(0.030f, 0.030f, 0.012f), Quaternion.Euler(-22f, 6f, 3f), headWeight);

                // Two that lie down over the crown and the nape, so it reads as hair with volume rather
                // than as spikes growing out of a skull.
                builder.AddOrientedBox(hairCenter + new Vector3(0.012f, p.headLength * 0.085f, -p.headRadius * 0.86f),
                    new Vector3(0.052f, 0.012f, 0.020f), Quaternion.Euler(14f, 0f, -6f), headWeight);
                builder.AddQuad(skullCenter + new Vector3(-0.010f, p.headLength * 0.325f, p.headRadius * 0.62f),
                    Vector3.right, Vector3.up, p.headRadius * 0.72f, 0.016f, headWeight, 0.002f);
            }
            else if (role == FigureRole.ClassmateQ)
            {
                // A buzz cut: the shell is pulled in tight against the skull and then cut off square at
                // the top, so there is no volume anywhere. The previous version drew one flat quad at
                // the brow and read as combed hair; a crew cut is the absence of a shape, which is why
                // it has to be made by shrinking the shell rather than by drawing anything on it.
                builder.AddQuad(skullCenter + new Vector3(0f, p.headLength * 0.335f, p.headRadius * 0.60f),
                    Vector3.right, Vector3.up, p.headRadius * 1.02f, 0.010f, headWeight, 0.0015f);
                builder.AddQuad(skullCenter + new Vector3(0f, p.headLength * 0.355f, -p.headRadius * 0.25f),
                    Vector3.right, Vector3.up, p.headRadius * 1.05f, 0.008f, headWeight, 0.0015f);
            }
        }

        private static void BuildHeadFace(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p, FigureRole role)
        {
            BoneWeight4 headWeight = field.Single(HumanoidBones.Head);
            float eyeY = p.headLength * 0.06f;

            // The face is drawn, not sculpted: flat marks carried as geometry so they survive posing.
            // Every mark is placed through AddFaceMark, which finds the skin under it --- the skull and
            // jaw are ellipsoids and their front surface is further forward than the mark height used
            // to assume, so a mark at a fixed z is buried from the mouth down.
            if (role == FigureRole.Protagonist)
            {
                // One eye a slit, the other a hollow. Two different ages sharing one face.
                AddFaceMark(builder, p, -0.032f, eyeY, 0.022f, 0.004f, headWeight);
                HeadShells(p, out Vector3 protagonistSkull, out _, out _, out _);
                if (TryFaceSurface(p, protagonistSkull.y + eyeY, 0.034f, out Vector3 hollowAt))
                {
                    // The hollow is set a little into the skin on purpose: it reads as a socket, not a lens.
                    builder.AddEllipsoid(hollowAt + new Vector3(0f, 0f, -0.004f),
                        new Vector3(0.016f, 0.012f, 0.008f), 10, 6, _ => headWeight);
                }

                // The seam, and a second line that does not meet it.
                AddFaceMark(builder, p, 0.004f, -p.headLength * 0.02f, 0.0035f, p.headLength * 0.33f, headWeight);
                AddFaceMark(builder, p, 0.021f, -p.headLength * 0.17f, 0.0028f, p.headLength * 0.13f, headWeight);

                // Mouth: a short line, drawn low so the face reads as long.
                AddFaceMark(builder, p, 0.002f, -p.headLength * 0.24f, 0.019f, 0.003f, headWeight);
            }
            else if (role == FigureRole.ClassmateQ)
            {
                // Q's face is almost blank: two level marks and a mouth that is exactly horizontal.
                AddFaceMark(builder, p, -0.03f, eyeY, 0.018f, 0.003f, headWeight);
                AddFaceMark(builder, p, 0.03f, eyeY, 0.018f, 0.003f, headWeight);
                AddFaceMark(builder, p, 0f, -p.headLength * 0.2f, 0.017f, 0.0025f, headWeight);
            }
            else if (role == FigureRole.TeacherB)
            {
                BuildTeacherBFace(builder, p, headWeight);
            }
            else
            {
                BuildTeacherFace(builder, p, headWeight);
            }
        }

        /// <summary>
        /// N's face. The standard this house holds faces to is that a resting expression is carried by
        /// shape — a brow line, a lid, a mouth corner, a jaw — and not by a blank surface with two
        /// marks on it. She is the first figure here to get all four:
        ///
        ///   - brows lower than the hairline and asymmetric by 1.5 mm, one of them pulled in;
        ///   - eyes slightly narrowed, with a lower lid under each, because she is tired;
        ///   - a mouth whose right corner turns down 2 mm — the "am I actually in charge here" mouth;
        ///   - the two soft creases from the nose to the mouth corners that make an adult read as adult
        ///     without drawing a single wrinkle.
        ///
        /// Measured against the model standard's face bands (chin 0 to crown 1): eye line 0.52 of head
        /// length, which is inside the 0.47-0.53 band; nose base 0.315, inside 0.30-0.36; mouth line
        /// 0.24, below the nose; each eye 0.40 of head radius wide, 0.20 of the face. Those are the
        /// numbers the proof sheet's C panel draws the dashed bands against, so if one of them is
        /// changed here the sheet will show the mark outside its band.
        /// </summary>
        private static void BuildTeacherFace(SkinnedMeshBuilder builder, HumanoidProportions p, BoneWeight4 headWeight)
        {
            float browY = p.headLength * 0.145f;
            float eyeX = p.headRadius * 0.365f;
            float eyeHalfWidth = p.headRadius * 0.20f;

            // The skull centre is 0.52 head-lengths above the head bone and the chin is the bottom of
            // the jaw shell, so an eye line at 0.52 of face height sits 0.02 head-lengths above centre.
            float eyeY = p.headLength * 0.02f;

            // Every mark goes through AddFaceMark: it finds the skull or jaw surface under the mark and
            // lifts the quad clear of it. Her marks are the ones that needed it most --- the mouth sits
            // 10 mm behind the jaw shell on a flat plane, so before this her face read as brows and
            // nothing else.
            AddFaceMark(builder, p, -eyeX, browY, eyeHalfWidth * 0.95f, 0.0026f, headWeight);
            AddFaceMark(builder, p, eyeX - 0.0015f, browY + 0.001f, eyeHalfWidth * 0.9f, 0.0026f, headWeight);

            // Eyes: a lens and a lower lid. The lid is the tiredness; the lens is the attention.
            for (int side = 0; side < 2; side++)
            {
                float x = side == 0 ? -eyeX : eyeX;
                HeadShells(p, out Vector3 skullCenter, out _, out _, out _);
                if (TryFaceSurface(p, skullCenter.y + eyeY, x, out Vector3 lensAt))
                {
                    builder.AddEllipsoid(lensAt + new Vector3(0f, 0f, 0.0006f),
                        new Vector3(eyeHalfWidth, p.headLength * 0.019f, 0.006f), 12, 6, _ => headWeight);
                }

                AddFaceMark(builder, p, x, eyeY - p.headLength * 0.031f, eyeHalfWidth * 0.92f, 0.0014f, headWeight);
            }

            // Nose: a soft base mark, not a wedge. Only what a soft pencil would leave.
            AddFaceMark(builder, p, 0.002f, -p.headLength * 0.185f, p.headRadius * 0.115f, 0.0022f, headWeight);

            // Mouth, set 2 mm low on the left so the whole line tilts down toward one corner.
            AddFaceMark(builder, p, 0f, -p.headLength * 0.26f, p.headRadius * 0.20f, 0.0028f, headWeight);
            AddFaceMark(builder, p, p.headRadius * 0.20f, -p.headLength * 0.268f, 0.0022f, 0.0045f, headWeight);

            // The two creases from the nose wings down past the mouth corners.
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                AddFaceMark(builder, p, sign * p.headRadius * 0.22f, -p.headLength * 0.225f, 0.0012f,
                    p.headLength * 0.055f, headWeight);
            }
        }


        /// <summary>
        /// B's hair: white, coiled into a bun at the back of the head.
        ///
        /// A teacher's bun is a compact torus of hair sitting on the occiput, and it reads as a bun
        /// rather than a growth only if it is a separate mass with a visible edge against the scalp. So
        /// there are three pieces: a shell cropped close to the skull (she is not a bob; the hair is
        /// pulled back), the coil itself, and a hairpin through it.
        ///
        /// The white is the palette's job. What is done here is the shape and the two loose strands,
        /// because a bun without strays reads as a helmet with a knob on it.
        /// </summary>
        private static void BuildTeacherBHair(SkinnedMeshBuilder builder, HumanoidProportions p,
                                              BoneWeight4 headWeight, Vector3 skullCenter, Vector3 hairCenter)
        {
            // Pulled back and tight: a shell that follows the skull closely, wider only at the back.
            builder.AddEllipsoid(hairCenter + new Vector3(0f, 0f, -p.headRadius * 0.05f),
                new Vector3(p.headRadius * 1.06f, p.headLength * 0.46f, p.headRadius * 1.06f), 18, 10, _ => headWeight);

            // The coil: low on the occiput, not on top of the crown. An ellipsoid squashed on z so it
            // sits against the back of the head instead of balancing on it.
            Vector3 bun = hairCenter + new Vector3(0f, -p.headLength * 0.10f, -p.headRadius * 1.02f);
            builder.AddEllipsoid(bun, new Vector3(p.headRadius * 0.62f, p.headLength * 0.17f, p.headRadius * 0.5f), 14, 8, _ => headWeight);

            // The hairpin, through the coil at a slight angle. Two thin boxes rather than one, so it
            // reads as a pin and not as a ridge.
            builder.AddOrientedBox(bun + new Vector3(0f, 0f, -p.headRadius * 0.18f),
                new Vector3(p.headRadius * 0.70f, 0.0016f, 0.0016f),
                Quaternion.Euler(0f, 0f, 12f), headWeight);

            // Two strays, one each side, falling in front of the ears. The only loose thing on her.
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                builder.AddQuad(skullCenter + new Vector3(sign * p.headRadius * 0.74f, -p.headLength * 0.16f, p.headRadius * 0.56f),
                    Vector3.right, Vector3.up, 0.0032f, p.headLength * 0.13f, headWeight, 0.0004f);
            }

            // A widow's peak, because the hairline is where a pulled-back bun becomes a face.
            builder.AddQuad(skullCenter + new Vector3(0f, p.headLength * 0.30f, p.headRadius * 0.90f),
                Vector3.right, Vector3.up, p.headRadius * 0.52f, 0.007f, headWeight, 0.001f);
        }

        /// <summary>
        /// B's face. The brief for her is a bird, and every mark here is a decision toward that:
        ///
        ///   - a hooked nose, built as three ellipsoids marching down and forward, so the profile has a
        ///     ridge and a point rather than a wedge. This is the largest single feature on her face and
        ///     the reason she reads as a bird instead of as a generic old woman;
        ///   - eyes that are small and close together (0.30 of head radius from centre, against N's
        ///     0.365), set behind spectacles that have a brow shade --- a hat brim over the lens;
        ///   - the spectacles are geometry, two rings and a bridge, because a drawn mark would not
        ///     read as glass sitting off the face, and the shade above each lens is the thing the
        ///     protagonist actually remembers about her;
        ///   - a mouth pulled into a line narrower than the jaw, with the corners already turned down.
        ///
        /// Every flat mark goes through AddFaceMark so it sits on the skin. The spectacles and the nose
        /// do not: they are volumes in front of the face, and placing them on the surface would flatten
        /// them back into it.
        /// </summary>
        private static void BuildTeacherBFace(SkinnedMeshBuilder builder, HumanoidProportions p, BoneWeight4 headWeight)
        {
            HeadShells(p, out Vector3 skullCenter, out _, out _, out _);

            float eyeX = p.headRadius * 0.30f;          // close together
            float eyeY = p.headLength * 0.03f;
            float browY = p.headLength * 0.125f;

            // --- the nose, in three parts: ridge, tip, hook. Built before the marks so the marks sit
            // on the face and the nose sits in front of them.
            float noseTop = skullCenter.y - p.headLength * 0.02f;
            if (TryFaceSurface(p, noseTop, 0f, out Vector3 browSurface))
            {
                // Ridge: rises off the face at the bridge and grows forward as it descends.
                builder.AddEllipsoid(browSurface + new Vector3(0f, -p.headLength * 0.055f, 0.004f),
                    new Vector3(0.0075f, p.headLength * 0.055f, 0.012f), 8, 6, _ => headWeight);

                // Tip: the furthest-forward point, below the ridge.
                Vector3 tip = browSurface + new Vector3(0f, -p.headLength * 0.115f, 0.020f);
                builder.AddEllipsoid(tip, new Vector3(0.0085f, 0.0095f, 0.0115f), 8, 6, _ => headWeight);

                // The hook: a small mass that hangs *down* from the tip, which is what makes the profile
                // read as a hooked nose rather than a pointed one.
                builder.AddEllipsoid(tip + new Vector3(0f, -0.0115f, 0.0035f),
                    new Vector3(0.0062f, 0.0082f, 0.0075f), 8, 6, _ => headWeight);
            }

            // --- spectacles: a ring per eye, a bridge, and the brow shade above each lens.
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                float x = sign * eyeX;
                if (!TryFaceSurface(p, skullCenter.y + eyeY, x, out Vector3 lensSurface)) continue;

                // The lens is a shallow ellipsoid standing 12 mm off the face; the frame is drawn as a
                // ring of eight small boxes around it, which reads as a rim at any angle.
                Vector3 lens = lensSurface + new Vector3(0f, 0f, 0.012f);
                float rimRadius = p.headRadius * 0.30f;
                for (int segment = 0; segment < 8; segment++)
                {
                    float angle = segment * Mathf.PI * 0.25f;
                    Vector3 offset = new Vector3(Mathf.Cos(angle) * rimRadius, Mathf.Sin(angle) * rimRadius * 0.82f, 0f);
                    builder.AddBox(lens + offset, new Vector3(0.0022f, 0.0022f, 0.0018f), headWeight);
                }

                // The shade: a flat plate above the lens, angled forward. In the description it is a hat
                // brim lying over the top of the glasses, and it is the reason her eyes are hard to see.
                builder.AddOrientedBox(lens + new Vector3(0f, rimRadius * 0.95f, 0.004f),
                    new Vector3(rimRadius * 1.12f, 0.0012f, 0.009f),
                    Quaternion.Euler(-24f, 0f, 0f), headWeight);

                // Eyes: small, and narrowed by a lower lid. Behind the glass, so no lens geometry.
                AddFaceMark(builder, p, x, eyeY, p.headRadius * 0.155f, 0.0022f, headWeight);
                AddFaceMark(builder, p, x, eyeY - p.headLength * 0.026f, p.headRadius * 0.145f, 0.0012f, headWeight);
            }

            // The bridge of the spectacles, between the two rings.
            HeadShells(p, out skullCenter, out _, out _, out _);
            builder.AddBox(skullCenter + new Vector3(0f, eyeY, p.headRadius * 0.99f + 0.012f),
                new Vector3(eyeX - p.headRadius * 0.28f, 0.0016f, 0.0016f), headWeight);

            // Brows: high, thin, and drawn as two short strokes that do not meet in the middle.
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                AddFaceMark(builder, p, sign * p.headRadius * 0.34f, browY, p.headRadius * 0.17f, 0.0018f, headWeight);
            }

            // The mouth: narrow, level, both corners down. Not a scowl --- a habit.
            AddFaceMark(builder, p, 0f, -p.headLength * 0.265f, p.headRadius * 0.155f, 0.0024f, headWeight);
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                AddFaceMark(builder, p, sign * p.headRadius * 0.155f, -p.headLength * 0.276f, 0.0018f, 0.0038f, headWeight);
            }

            // Two deep creases, longer than N's: the folds either side of a mouth that has been set this
            // way for forty years.
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                AddFaceMark(builder, p, sign * p.headRadius * 0.30f, -p.headLength * 0.205f, 0.0014f,
                    p.headLength * 0.075f, headWeight);
            }
        }


        // -------------------------------------------------------------- coat / trousers

        private static void BuildTorsoCoat(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p, FigureRole role)
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
            // The torso rings: hips, waist, chest, above the chest, neck. The students wear loose
            // school clothes, so their torso is nearly a cylinder. N's knit cardigan hangs off narrow
            // shoulders and opens out over the hips, so her rings pinch at the waist and flare at the
            // hem — the pear silhouette the teacher is supposed to have, made of five numbers instead
            // of a scaled copy of the protagonist.
            bool cardigan = role == FigureRole.TeacherN;
            bool stocky = role == FigureRole.ClassmateQ;

            // Q is the heavy one, and the weight is carried where a teenage boy who does not move much
            // carries it: at the waist and the chest, not the hips. So his rings are widest in the middle
            // rather than flaring at the hem like N's cardigan --- a barrel, against her pear. This is
            // the difference between "overweight" and "wearing a bigger coat", and it is one list.
            List<float> radii = cardigan
                ? new List<float> { 0.134f, 0.104f, 0.116f, 0.104f, 0.056f }
                : stocky
                    ? new List<float> { 0.124f, 0.136f, 0.142f, 0.120f, 0.062f }
                    : new List<float> { 0.112f, 0.116f, 0.126f, 0.112f, 0.06f };
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
            // N's caps are smaller: a cardigan shoulder sits on the joint, it does not pad it.
            float capRadius = cardigan ? 0.046f : 0.056f;
            for (int side = 0; side < 2; side++)
            {
                string shoulder = side == 0 ? HumanoidBones.ShoulderLeft : HumanoidBones.ShoulderRight;
                Vector3 at = p.RestPosition(shoulder);
                BoneWeight4 weight = field.Blend(shoulder, HumanoidBones.Chest, 0.75f, 0.25f);
                builder.AddEllipsoid(at + new Vector3(0f, 0.004f, 0f), new Vector3(capRadius, capRadius * 0.9f, capRadius), 10, 6, _ => weight);
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
                // Both arms used to blend their top ring against Shoulder_Left. On the right arm that
                // pinned the sleeve to the wrong clavicle, so raising the right shoulder dragged the
                // left sleeve's cap with it and the right shoulder pinched.
                string shoulder = left ? HumanoidBones.ShoulderLeft : HumanoidBones.ShoulderRight;

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
                    field.Blend(upper, shoulder, 0.8f, 0.2f),
                    field.Single(upper),
                    field.Blend(upper, fore, 0.5f, 0.5f),
                    field.Single(fore),
                    field.Blend(fore, hand, 0.7f, 0.3f),
                };

                builder.AddTube(spine, radii, ringWeights, 10, capStart: true, capEnd: true);
            }
        }

        /// <summary>
        /// The cloth that is not a torso and not a sleeve: who wears a coat tail, who carries a bag,
        /// who has a sleeve band. One switch so a fourth figure is a case, not a new if in three places.
        /// </summary>
        private static void BuildClothDetails(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p, FigureRole role)
        {
            switch (role)
            {
                case FigureRole.Protagonist:
                    BuildCoatSkirt(builder, field, p);
                    BuildBag(builder, field, p);
                    BuildSleeveBand(builder, field, p);
                    BuildSuitJacket(builder, field, p);
                    break;
                case FigureRole.TeacherN:
                    BuildCardiganHem(builder, field, p);
                    BuildCardiganCuffs(builder, field, p);
                    break;
                case FigureRole.TeacherB:
                    BuildTeacherBSkirt(builder, field, p);
                    BuildTeacherBCuffs(builder, field, p);
                    break;
                case FigureRole.ClassmateQ:
                    BuildQStomach(builder, field, p);
                    break;
            }
        }

        /// <summary>
        /// Q's stomach. The torso rings make him wide, but width alone reads as a loose shirt; what makes
        /// a heavy person heavy is that the mass is in front and lower than the chest, and that it moves
        /// with the spine rather than with the shoulders. One ellipsoid, weighted mostly to the spine, so
        /// when he leans in to needle the protagonist it leans with him.
        /// </summary>
        private static void BuildQStomach(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            Vector3 spine = p.RestPosition(HumanoidBones.Spine);
            BoneWeight4 weight = field.Blend(HumanoidBones.Spine, HumanoidBones.Hips, 0.75f, 0.25f);

            builder.AddEllipsoid(spine + new Vector3(0f, -0.055f, p.chestDepth * 0.42f),
                new Vector3(0.108f, 0.088f, 0.070f), 14, 8, _ => weight);
        }

        /// <summary>
        /// B's skirt. A teacher's skirt is the one garment in this cast that is a shape rather than a
        /// tube, so it is built as a separate flared panel hanging from the waist: three rings, widening
        /// downward, stopping below the knee. The silhouette it gives her is the second half of the
        /// bird --- narrow shoulders above, a widening wedge below.
        /// </summary>
        private static void BuildTeacherBSkirt(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            Vector3 hips = p.RestPosition(HumanoidBones.Hips);
            Vector3 knee = p.RestPosition(HumanoidBones.ShinLeft);
            float length = (hips.y - knee.y) + 0.075f;

            List<Vector3> spine = new List<Vector3>
            {
                hips + new Vector3(0f, 0.04f, 0f),
                hips + new Vector3(0f, -length * 0.52f, 0f),
                hips + new Vector3(0f, -length, 0f),
            };
            List<float> radii = new List<float> { 0.112f, 0.132f, 0.156f };
            List<BoneWeight4> weights = new List<BoneWeight4>
            {
                field.Blend(HumanoidBones.Hips, HumanoidBones.Spine, 0.7f, 0.3f),
                field.Single(HumanoidBones.Hips),
                field.Blend(HumanoidBones.Hips, HumanoidBones.ThighLeft, 0.85f, 0.15f),
            };

            builder.AddTube(spine, radii, weights, 20, capStart: false, capEnd: false, squashX: 1f, squashZ: 0.82f);

            // A hem line, drawn as one band just inside the bottom ring: it stops the flare from reading
            // as a cone with no bottom.
            builder.AddTube(
                new List<Vector3> { hips + new Vector3(0f, -length + 0.012f, 0f), hips + new Vector3(0f, -length + 0.006f, 0f) },
                new List<float> { 0.157f, 0.157f },
                new List<BoneWeight4> { field.Single(HumanoidBones.Hips), field.Single(HumanoidBones.Hips) },
                20, capStart: false, capEnd: false, squashX: 1f, squashZ: 0.82f);
        }

        /// <summary>
        /// B's cuffs: narrow, buttoned, and set at the wrist. She is dressed properly, which is part of
        /// why she is frightening --- the figure in the imagination keeps the details of a person who
        /// takes her job seriously.
        /// </summary>
        private static void BuildTeacherBCuffs(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                string fore = left ? HumanoidBones.ForearmLeft : HumanoidBones.ForearmRight;
                string hand = left ? HumanoidBones.HandLeft : HumanoidBones.HandRight;

                Vector3 elbow = p.RestPosition(fore);
                Vector3 wrist = p.RestPosition(hand);
                Vector3 cuffStart = Vector3.Lerp(elbow, wrist, 0.80f);

                BoneWeight4 weight = field.Blend(fore, hand, 0.65f, 0.35f);
                builder.AddTube(
                    new List<Vector3> { cuffStart, wrist },
                    new List<float> { 0.041f, 0.038f },
                    new List<BoneWeight4> { weight, weight },
                    10, capStart: false, capEnd: false);
            }
        }

        /// <summary>
        /// The cardigan, below the belt: a hem-weighted panel that flares and stops at the top of the
        /// thigh, a knitted rib drawn across it, and the ribbed band running up the front where the
        /// cardigan is open over the shirt. It hangs from the hips with a little spine so it moves when
        /// she leans on the podium, and it deliberately does not reach the knee — a cardigan, not a coat.
        /// </summary>
        private static void BuildCardiganHem(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            Vector3 hips = p.RestPosition(HumanoidBones.Hips);
            Vector3 spine = p.RestPosition(HumanoidBones.Spine);
            Vector3 hem = hips + new Vector3(0f, -0.135f, 0f);

            List<Vector3> skirt = new List<Vector3> { hips + new Vector3(0f, 0.045f, 0f), hips, hem };
            List<float> radii = new List<float> { 0.134f, 0.140f, 0.132f };
            List<BoneWeight4> weights = new List<BoneWeight4>
            {
                field.Blend(HumanoidBones.Hips, HumanoidBones.Spine, 0.6f, 0.4f),
                field.Single(HumanoidBones.Hips),
                field.Blend(HumanoidBones.Hips, HumanoidBones.ThighLeft, 0.9f, 0.1f),
            };

            builder.AddTube(skirt, radii, weights, 18, capStart: false, capEnd: false, squashX: 1f, squashZ: 0.8f);

            // The front opening. Two bands used to run straight down in parallel, which reads as a
            // placket on a closed coat. A cardigan is open in a V: the two edges splay apart as they
            // rise so the shirt shows between them at the collar and closes again at the waist.
            BoneWeight4 bandWeight = field.Single(HumanoidBones.Spine);
            float depth = p.chestDepth + 0.008f;
            // The lean is the wrong way round if it opens downwards: with leanSign = +1 the edges
            // converge at the collar and splay at the waist, which is a V standing on its point and reads
            // as a tailcoat. Only -1 is a cardigan.
            //
            // The band geometry is solved rather than eyeballed, because two things about it are easy to
            // get wrong and neither shows up as an error: where the two edges meet, and whether they
            // cross below it. The centre and half-length here put the inner-bottom corner of each band
            // exactly at (0, -0.029) --- the meeting point --- and the outer-top corner at the neckline
            // (y = 0.135). Because both inner edges run to the same point and stop there, the bands
            // cannot cross; a slightly longer band would poke through the other side, which is what the
            // first version did.
            const float veeAngle = 13f;                 // degrees each edge leans off vertical
            const float leanSign = -1f;
            const float bandHalfWidth = 0.009f;
            const float bandHalfLength = 0.0862f;
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                float radians = veeAngle * Mathf.Deg2Rad * sign * leanSign;
                Vector3 across = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
                Vector3 along = new Vector3(-Mathf.Sin(radians), Mathf.Cos(radians), 0f);

                Vector3 bandCentre = spine + new Vector3(sign * 0.0282f, 0.0530f, depth);
                builder.AddQuad(bandCentre, across, along, bandHalfWidth, bandHalfLength, bandWeight, 0.001f);
            }

            // The shirt showing through the opening, drawn as three bands that fill the gap and stop
            // short of the cardigan edges. A single quad cannot follow a V that tapers from 77 mm to
            // nothing: it either pokes through the cardigan at the bottom or leaves a margin at the top
            // that makes the whole thing read as a bib patch. Each of these is sized to the gap at its
            // own bottom edge, which is its narrowest point, so it never crosses the edges.
            BoneWeight4 shirtWeight = field.Single(HumanoidBones.Chest);
            (float y, float halfWidth)[] shirtBands =
            {
                (0.030f, 0.0096f),   // narrowest gap over the band 0.0233 m, less 2 mm each side
                (0.052f, 0.0147f),   // 0.0334 m
                (0.074f, 0.0198f),   // 0.0436 m
            };
            foreach ((float y, float halfWidth) in shirtBands)
            {
                builder.AddQuad(spine + new Vector3(0f, y, depth - 0.002f), Vector3.right, Vector3.up,
                    halfWidth, 0.0108f, shirtWeight);
            }

            // Knit ribs: three shallow ridges across the back of the hem, where a cardigan pulls.
            BoneWeight4 hemWeight = field.Blend(HumanoidBones.Hips, HumanoidBones.ThighLeft, 0.9f, 0.1f);
            for (int rib = 0; rib < 3; rib++)
            {
                float y = -0.055f - rib * 0.028f;
                float radius = 0.140f - rib * 0.004f;
                builder.AddQuad(hips + new Vector3(0f, y, -radius * 0.8f), Vector3.right, Vector3.up, radius * 0.9f, 0.005f, hemWeight, 0.001f);
            }
        }

        /// <summary>
        /// Rolled cuffs at both wrists: a short, fatter tube over the sleeve, so the cardigan reads as
        /// something with a hem instead of one extrusion from shoulder to wrist.
        /// </summary>
        private static void BuildCardiganCuffs(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                string fore = left ? HumanoidBones.ForearmLeft : HumanoidBones.ForearmRight;
                string hand = left ? HumanoidBones.HandLeft : HumanoidBones.HandRight;

                Vector3 elbow = p.RestPosition(fore);
                Vector3 wrist = p.RestPosition(hand);
                Vector3 a = Vector3.Lerp(elbow, wrist, 0.86f);
                Vector3 b = Vector3.Lerp(elbow, wrist, 0.97f);
                BoneWeight4 weight = field.Blend(fore, hand, 0.8f, 0.2f);

                builder.AddTube(new List<Vector3> { a, b }, new List<float> { 0.039f, 0.038f }, new List<BoneWeight4> { weight, weight }, 10, capStart: false, capEnd: false);
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

        /// <summary>
        /// The jacket he is wearing, which is a suit jacket that does not fit him.
        ///
        /// A suit is not a coat with different colours: it is open down the front, has lapels that fold
        /// back, and is cut to a waist. The register wanted here is the one where an adult's clothes read
        /// as evidence --- the jacket is too good for a schoolboy and too loose for the body in it, which
        /// is the same thing the story is saying about him.
        ///
        /// So: the skirt already gives the length; these pieces give the opening and the fold. Two front
        /// panels with a gap between them (through which the shirt shows), two lapels laid over them at
        /// an angle, and a cuff at each wrist. Every piece is measured off the torso tube it lies on, so
        /// none of it floats.
        /// </summary>
        private static void BuildSuitJacket(SkinnedMeshBuilder builder, WeightField field, HumanoidProportions p)
        {
            Vector3 spine = p.RestPosition(HumanoidBones.Spine);
            Vector3 chest = p.RestPosition(HumanoidBones.Chest);
            float depth = p.chestDepth + 0.010f;
            BoneWeight4 chestWeight = field.Single(HumanoidBones.Chest);

            // The two front panels. They stop short of the centre line: the gap between them is the
            // opening, and it is a fixed 46 mm because a jacket's button stance does not move with the
            // shoulders.
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                // Wider at the top (over the chest) than at the bottom, because a jacket is cut, not a
                // rectangle pinned on.
                for (int band = 0; band < 2; band++)
                {
                    float y = band == 0 ? 0.030f : 0.115f;
                    float halfWidth = band == 0 ? 0.052f : 0.040f;
                    builder.AddQuad(spine + new Vector3(sign * (0.023f + halfWidth), y, depth),
                        Vector3.right, Vector3.up, halfWidth, 0.044f, chestWeight, 0.0008f);
                }
            }

            // Lapels: folded back, so they sit at an angle and stand proud of the panel under them. The
            // rotation is what makes them read as a fold rather than as a stripe.
            for (int side = 0; side < 2; side++)
            {
                float sign = side == 0 ? -1f : 1f;
                builder.AddOrientedBox(chest + new Vector3(sign * 0.056f, 0.012f, depth + 0.004f),
                    new Vector3(0.036f, 0.0016f, 0.030f), Quaternion.Euler(0f, 0f, sign * -26f), chestWeight);
            }

            // Cuffs: the jacket's sleeve ends above the wrist, so there is a band where the shirt shows.
            for (int side = 0; side < 2; side++)
            {
                bool left = side == 0;
                string fore = left ? HumanoidBones.ForearmLeft : HumanoidBones.ForearmRight;
                string hand = left ? HumanoidBones.HandLeft : HumanoidBones.HandRight;
                Vector3 elbow = p.RestPosition(fore);
                Vector3 wrist = p.RestPosition(hand);
                BoneWeight4 weight = field.Blend(fore, hand, 0.7f, 0.3f);

                builder.AddTube(
                    new List<Vector3> { Vector3.Lerp(elbow, wrist, 0.86f), Vector3.Lerp(elbow, wrist, 0.97f) },
                    new List<float> { 0.048f, 0.046f },
                    new List<BoneWeight4> { weight, weight },
                    10, capStart: false, capEnd: false);
            }
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
            else if (role == FigureRole.ClassmateQ)
            {
                // Q: a high, buttoned collar with no gap. Nothing showing.
                builder.AddQuad(chest + new Vector3(0f, 0.085f, depth + 0.004f), Vector3.right, Vector3.up, 0.052f, 0.026f, chestWeight);
            }
            else
            {
                // N: two collar points standing away from the throat and a placket between them, with
                // the cardigan left open over all of it. This is the brightest cloth in the room and it
                // is the thing that marks her out as staff, so it gets the whole front of the sternum.
                builder.AddQuad(chest + new Vector3(-0.030f, 0.068f, depth + 0.005f), Vector3.right, Vector3.up, 0.030f, 0.034f, chestWeight);
                builder.AddQuad(chest + new Vector3(0.030f, 0.068f, depth + 0.005f), Vector3.right, Vector3.up, 0.030f, 0.034f, chestWeight);
                builder.AddQuad(chest + new Vector3(0f, -0.008f, depth + 0.004f), Vector3.right, Vector3.up, 0.017f, 0.088f, chestWeight);
                // Two buttons, spaced by hand rather than evenly: nothing on this figure is machine.
                builder.AddEllipsoid(chest + new Vector3(0.002f, -0.030f, depth + 0.006f), new Vector3(0.005f, 0.005f, 0.004f), 8, 4, _ => chestWeight);
                builder.AddEllipsoid(chest + new Vector3(-0.001f, -0.062f, depth + 0.006f), new Vector3(0.005f, 0.005f, 0.004f), 8, 4, _ => chestWeight);
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

    /// <summary>Which silhouette details to build. The skeleton is identical for all of them.</summary>
    public enum FigureRole
    {
        Protagonist,
        ClassmateQ,

        /// <summary>N: the biology substitute. Narrow shoulders, a sage cardigan, a cropped bob.</summary>
        TeacherN,

        /// <summary>
        /// B: the biology teacher as the protagonist imagines her, built out of Q's description. Only
        /// ever appears inside the imagined lesson, so her geometry is allowed to be wrong in ways the
        /// other figures are not --- she is a recollection, not a person in the room.
        /// </summary>
        TeacherB,
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
