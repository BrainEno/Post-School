using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PostSchool.Character;
using UnityEngine;

namespace PostSchool.Tests.EditMode
{
    /// <summary>
    /// Proves the skinning actually deforms: build a figure, bend one joint, and assert the vertices
    /// moved the way a skinned surface must (near the joint more than far from it, nothing moving on
    /// the other side).
    /// </summary>
    public sealed class SkinnedCharacterTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            _root = null;
        }

        private SkinnedFigure Build(HumanoidProportions proportions, FigureRole role, out HumanoidProportions used)
        {
            _root = new GameObject("Skinned Test Root");
            used = proportions;
            Material material = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"));
            CharacterSkin skin = new CharacterSkin(material, material, material, material, material);
            return SkinnedCharacterBuilder.Create("Test", _root.transform, Vector3.zero, 0f, proportions, role, skin);
        }

        [Test]
        public void FigureBuildsWithMeshWeightsAndNineteenBones()
        {
            SkinnedFigure figure = Build(HumanoidProportions.Protagonist(), FigureRole.Protagonist, out _);

            Assert.IsNotNull(figure.Mesh);
            Assert.Greater(figure.Mesh.vertexCount, 500, "a skinned body should be more than a handful of primitives");
            Assert.AreEqual(figure.Mesh.vertexCount, figure.Weights.Length);
            Assert.AreEqual(19, figure.Renderer.bones.Length);
            Assert.AreEqual(figure.Mesh.vertexCount, figure.Renderer.sharedMesh.boneWeights.Length);
        }

        [Test]
        public void WeightsSumToOneForEveryVertex()
        {
            SkinnedFigure figure = Build(HumanoidProportions.Protagonist(), FigureRole.Protagonist, out _);
            Assert.IsTrue(ProceduralSkeleton.WeightsAreValid(figure.Weights, figure.Skeleton.BoneCount, out string error), error);
        }

        [Test]
        public void MeshHasOneSubmeshPerSurfaceAndEnoughTrianglesPerPart()
        {
            SkinnedFigure figure = Build(HumanoidProportions.Protagonist(), FigureRole.Protagonist, out _);
            int submeshes = Mathf.Max(1, figure.Mesh.subMeshCount);

            Assert.GreaterOrEqual(submeshes, 2, "skin/hair/coat/shirt should not collapse into one surface");

            int triangles = 0;
            for (int i = 0; i < submeshes; i++) triangles += figure.Mesh.GetTriangles(i).Length;
            Assert.Greater(triangles / 3, 800);
        }

        [Test]
        public void BendingTheForearmMovesTheWristSideAndNotTheOtherArm()
        {
            SkinnedFigure figure = Build(HumanoidProportions.Protagonist(), FigureRole.Protagonist, out _);

            ProceduralSkeleton skeleton = figure.Skeleton;
            skeleton.CaptureBindPose(figure.Renderer.transform);
            Vector3[] bind = figure.BindVertices;

            // Rest-space landmarks must be read before anything rotates.
            Vector3 rightHandBind = figure.Bone(HumanoidBones.HandRight).position;
            Vector3 leftHandBind = figure.Bone(HumanoidBones.HandLeft).position;

            figure.Bone(HumanoidBones.ForearmRight).localRotation = Quaternion.Euler(0f, 0f, -90f);
            Vector3[] posed = skeleton.Skin(bind, figure.Weights, figure.Renderer.transform);

            // The right hand is weighted to the right forearm/hand, so it must move a lot.
            float rightDisplacement = MaxDisplacementNear(bind, posed, rightHandBind, 0.09f);
            Assert.Greater(rightDisplacement, 0.05f, "bending the elbow must move the right hand");

            // The left hand is on the other chain and must stay put.
            float leftDisplacement = MaxDisplacementNear(bind, posed, leftHandBind, 0.09f);
            Assert.Less(leftDisplacement, 0.002f, "the left arm must not be affected by the right elbow");

            Object.DestroyImmediate(figure.Mesh);
        }

        [Test]
        public void BendingTheKneeKeepsTheSoleOnTheThighSideOfTheJoint()
        {
            SkinnedFigure figure = Build(HumanoidProportions.Protagonist(), FigureRole.Protagonist, out _);
            ProceduralSkeleton skeleton = figure.Skeleton;
            skeleton.CaptureBindPose(figure.Renderer.transform);

            Vector3[] bind = figure.BindVertices;
            Vector3 kneePoint = figure.Bone(HumanoidBones.ShinRight).position;
            Vector3 anklePoint = figure.Bone(HumanoidBones.FootRight).position;

            figure.Bone(HumanoidBones.ShinRight).localRotation = Quaternion.Euler(-70f, 0f, 0f);
            Vector3[] posed = skeleton.Skin(bind, figure.Weights, figure.Renderer.transform);

            float atKnee = MaxDisplacementNear(bind, posed, kneePoint, 0.06f);
            float atAnkle = MaxDisplacementNear(bind, posed, anklePoint, 0.08f);

            Assert.Greater(atAnkle, atKnee, "a bent knee must displace the ankle more than the knee");
            Object.DestroyImmediate(figure.Mesh);
        }

        [Test]
        public void OneClipFitsBothFiguresOnTheSharedSkeleton()
        {
            HumanoidClip clip = HumanoidClips.StandingIdle();

            SkinnedFigure protagonist = Build(HumanoidProportions.Protagonist(), FigureRole.Protagonist, out _);
            Assert.IsTrue(clip.Validate(protagonist.Skeleton, out string error), error);
            Object.DestroyImmediate(protagonist.Mesh);
            Object.DestroyImmediate(_root);
            _root = null;

            SkinnedFigure q = Build(HumanoidProportions.Classmate(), FigureRole.ClassmateQ, out _);
            Assert.IsTrue(clip.Validate(q.Skeleton, out string qError), qError, "one clip must fit both figures");

            // The same pose, applied to both skeletons, must produce different absolute positions:
            // that difference is the characterisation, and it has to come from proportions only.
            HumanoidPose pose = clip.Sample(1.6f);
            pose.ApplyClean(q.Skeleton);
            float qHead = q.Skeleton.Bone(q.Skeleton.IndexOf(HumanoidBones.Head)).position.y;
            Object.DestroyImmediate(q.Mesh);

            Assert.Greater(qHead, 1.2f);
            Assert.Less(qHead, 1.45f, "Q is shorter than the protagonist under an identical pose");
        }

        [Test]
        public void TheTeacherIsNotACloneOfTheClassmate()
        {
            // Three figures, three silhouettes. If N were built by copying Q and repainting her, these
            // two meshes would have the same vertex count and the same shoulder width. They must not:
            // the point of a fourth build path is that the shape is hers.
            SkinnedFigure q = Build(HumanoidProportions.Classmate(), FigureRole.ClassmateQ, out _);
            int qVertices = q.Mesh.vertexCount;
            float qShoulder = HumanoidProportions.Classmate().ShoulderWidth;
            Object.DestroyImmediate(q.Mesh);
            Object.DestroyImmediate(_root);
            _root = null;

            SkinnedFigure n = Build(HumanoidProportions.TeacherN(), FigureRole.TeacherN, out _);
            int nVertices = n.Mesh.vertexCount;

            Assert.IsTrue(n.Skeleton.BoneCount == 19);
            Assert.AreNotEqual(qVertices, nVertices, "N's cardigan hem, cuffs and bob are extra geometry");
            Assert.Less(HumanoidProportions.TeacherN().ShoulderWidth, qShoulder);
            Assert.Greater(n.Mesh.bounds.size.y, q.Mesh.bounds.size.y, "she is taller than Q");

            // Her mesh still has to be a sane body: not empty, and not a pile of stray triangles.
            Assert.Greater(nVertices, 500);
            Assert.That(n.Mesh.bounds.size.x, Is.InRange(0.30f, 0.90f), "a single figure is under a metre wide");

            Object.DestroyImmediate(n.Mesh);
        }

        [Test]
        public void TheTeachersCardiganHangsBelowHerHipsAndStopsAboveHerKnee()
        {
            SkinnedFigure n = Build(HumanoidProportions.TeacherN(), FigureRole.TeacherN, out _);

            // The hem is the lowest piece of coat geometry; the knee is the Shin bone. A cardigan ends
            // between them — a coat reaches further, a shirt not at all. This pins that decision.
            float lowestCoat = float.MaxValue;
            Mesh mesh = n.Mesh;
            int coatSurface = (int)CharacterSurface.Coat;

            // In the bind pose the mesh sits on its own origin, so the knees are at their rest height.
            float knee = HumanoidProportions.TeacherN().RestPosition(HumanoidBones.ShinLeft).y;
            foreach (int index in mesh.GetTriangles(coatSurface))
            {
                lowestCoat = Mathf.Min(lowestCoat, n.BindVertices[index].y);
            }

            Assert.Less(lowestCoat, 0.885f, "the cardigan must hang below the hips");
            Assert.Greater(lowestCoat, knee - 0.02f, "and it must not reach the knee");

            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void EveryDrawnFaceMarkSitsInFrontOfTheSkinItIsDrawnOn()
        {
            // The faces are drawn, not sculpted: flat quads laid over an ellipsoid skull and jaw. The jaw
            // bulges forward of the skull, so a mark placed at one fixed z ends up *inside* the chin and
            // the mouth. Nothing errors --- the quads are simply hidden behind the skin, and the face
            // reads as a blank with two eyes. This is the test that would have caught it.
            foreach (FigureRole role in new[] { FigureRole.Protagonist, FigureRole.ClassmateQ, FigureRole.TeacherN, FigureRole.TeacherB })
            {
                HumanoidProportions proportions = role == FigureRole.Protagonist
                    ? HumanoidProportions.Protagonist()
                    : role == FigureRole.ClassmateQ
                        ? HumanoidProportions.Classmate()
                        : role == FigureRole.TeacherN
                            ? HumanoidProportions.TeacherN()
                            : HumanoidProportions.TeacherB();

                SkinnedFigure figure = Build(proportions, role, out _);
                Mesh mesh = figure.Mesh;

                float headY = proportions.RestPosition(HumanoidBones.Head).y;
                float skullCentreY = headY + proportions.headLength * 0.52f;
                float band = proportions.headLength * 0.55f;

                // Skin vertices in the head, which are the surface a mark has to sit proud of.
                var skin = new List<Vector3>();
                foreach (int index in mesh.GetTriangles((int)CharacterSurface.Skin))
                {
                    Vector3 vertex = figure.BindVertices[index];
                    if (Mathf.Abs(vertex.y - skullCentreY) > band) continue;
                    skin.Add(vertex);
                }
                Assert.Greater(skin.Count, 100, $"{role}: the head has almost no skin vertices to compare against");

                // Drawn marks share the Shirt slot with the collar, which is on the chest far below.
                var marks = new List<Vector3>();
                foreach (int index in mesh.GetTriangles((int)CharacterSurface.Shirt))
                {
                    Vector3 vertex = figure.BindVertices[index];
                    if (Mathf.Abs(vertex.y - skullCentreY) > band) continue;
                    marks.Add(vertex);
                }
                Assert.Greater(marks.Count, 0, $"{role}: no drawn face marks were found on the mesh");

                // For each mark vertex, the skin directly behind it must be further back. The sampling
                // window is deliberately tight: the skin is curved, so a sample taken even 4 mm away in
                // height sits on a different part of the ellipsoid and is not the surface the mark was
                // placed against. 0.3 mm of tolerance absorbs that sampling noise; the bug being guarded
                // against buried marks by 1 to 11 mm, so the margin is not a judgement call.
                //
                // tools/mesh_visibility_check.py remains the authority on the exact clearance --- it
                // recomputes the same shell maths the factory uses. This test is the in-editor guard
                // that a later edit cannot quietly put the marks back on a plane.
                int checkedVertices = 0;
                foreach (Vector3 mark in marks)
                {
                    float surface = float.MinValue;
                    foreach (Vector3 vertex in skin)
                    {
                        if (Mathf.Abs(vertex.y - mark.y) > 0.0015f) continue;
                        if (Mathf.Abs(vertex.x - mark.x) > 0.004f) continue;
                        surface = Mathf.Max(surface, vertex.z);
                    }
                    if (surface == float.MinValue) continue;   // no skin sample this close; not a verdict

                    checkedVertices++;
                    Assert.Greater(mark.z, surface - 0.0003f,
                        $"{role}: a drawn mark at ({mark.x:0.000}, {mark.y:0.000}) is behind the skin at z={surface:0.000} and would be hidden");
                }

                Assert.Greater(checkedVertices, 5,
                    $"{role}: the sampling window found almost no skin to compare against, so this test proved nothing");

                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(_root);
                _root = null;
            }
        }

        /// <summary>Largest movement of any vertex within a radius of a bind-pose point.</summary>
        private static float MaxDisplacementNear(Vector3[] bind, Vector3[] posed, Vector3 point, float radius)
        {
            float best = 0f;
            for (int i = 0; i < bind.Length; i++)
            {
                if ((bind[i] - point).sqrMagnitude > radius * radius) continue;
                best = Mathf.Max(best, (posed[i] - bind[i]).magnitude);
            }

            return best;
        }
    }
}
