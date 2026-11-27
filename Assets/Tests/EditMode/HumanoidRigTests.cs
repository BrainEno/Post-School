using System.Collections.Generic;
using NUnit.Framework;
using PostSchool.Character;
using UnityEngine;

namespace PostSchool.Tests.EditMode
{
    /// <summary>
    /// Pins the parts of the rig that are cheap to get wrong and expensive to notice: the bone count,
    /// feet on the floor, the head-unit ratio that carries the age read, and the weight table.
    /// </summary>
    public sealed class HumanoidRigTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            _root = null;
        }

        private ProceduralSkeleton CreateSkeleton(HumanoidProportions proportions)
        {
            _root = new GameObject("Rig Test Root");
            return HumanoidRig.Create(_root.transform, proportions, out _);
        }

        [Test]
        public void SkeletonHasNineteenBonesInDocumentedOrder()
        {
            ProceduralSkeleton skeleton = CreateSkeleton(HumanoidProportions.Protagonist());
            Assert.AreEqual(19, skeleton.BoneCount);
            Assert.AreEqual(HumanoidBones.Hips, skeleton.BoneNames[0]);
            Assert.AreEqual(HumanoidBones.FootRight, skeleton.BoneNames[18]);
        }

        [Test]
        public void EveryBoneIndexResolves()
        {
            ProceduralSkeleton skeleton = CreateSkeleton(HumanoidProportions.Protagonist());
            foreach (string bone in HumanoidBones.All)
            {
                Assert.GreaterOrEqual(skeleton.IndexOf(bone), 0, bone);
                Assert.IsNotNull(skeleton.Bone(skeleton.IndexOf(bone)), bone);
            }
        }

        [TestCase(FigureRole.Protagonist)]
        [TestCase(FigureRole.ClassmateQ)]
        [TestCase(FigureRole.TeacherN)]
        public void FiguresStandOnTheFloorAtTheirDeclaredHeight(FigureRole role)
        {
            HumanoidProportions proportions;
            switch (role)
            {
                case FigureRole.Protagonist: proportions = HumanoidProportions.Protagonist(); break;
                case FigureRole.ClassmateQ: proportions = HumanoidProportions.Classmate(); break;
                case FigureRole.TeacherN: proportions = HumanoidProportions.TeacherN(); break;
                default: throw new System.ArgumentOutOfRangeException(nameof(role));
            }

            ProceduralSkeleton skeleton = CreateSkeleton(proportions);
            Transform foot = skeleton.Bone(skeleton.IndexOf(HumanoidBones.FootLeft));
            Assert.AreEqual(proportions.ankleHeight, foot.position.y, 0.02f, "ankle height");

            Transform head = skeleton.Bone(skeleton.IndexOf(HumanoidBones.Head));
            Assert.AreEqual(proportions.height, head.position.y + proportions.headLength, 0.05f, "crown height");
        }

        [Test]
        public void ProtagonistIsSevenHeadsTallBetweenChildAndAdult()
        {
            HumanoidProportions p = HumanoidProportions.Protagonist();
            Assert.That(p.HeadsTall, Is.InRange(6.9f, 7.1f), "7 heads sits between a child (6) and an adult (7.5)");

            // And the head is deliberately large relative to the shoulders: the adolescent read.
            Assert.Less(p.ShoulderWidth / p.headLength, 1.05f, "shoulders should be barely one head wide");
        }

        [Test]
        public void ClassmateIsShorterAndSmallerHeaded()
        {
            HumanoidProportions q = HumanoidProportions.Classmate();
            HumanoidProportions protagonist = HumanoidProportions.Protagonist();

            Assert.Less(q.height, protagonist.height);
            Assert.Less(q.headLength, protagonist.headLength);
            Assert.Less(q.ShoulderWidth, protagonist.ShoulderWidth);
        }

        [Test]
        public void TeacherIsTheTallestFigureAndNarrowestAcrossTheShoulders()
        {
            HumanoidProportions n = HumanoidProportions.TeacherN();
            HumanoidProportions protagonist = HumanoidProportions.Protagonist();
            HumanoidProportions q = HumanoidProportions.Classmate();

            Assert.IsTrue(n.Validate(out string error), error);
            Assert.Greater(n.height, protagonist.height, "the only adult in the room is the tallest");
            Assert.Greater(n.HeadsTall, protagonist.HeadsTall, "7.4 heads against the protagonist's 7.0");
            Assert.Less(n.ShoulderWidth, q.ShoulderWidth, "narrower than the classmate she is taller than");
            Assert.Less(n.headLength, protagonist.headLength, "a smaller head is what makes her read as adult");
        }

        [Test]
        public void TeacherIsTheBrightColdValueKeyOfTheCast()
        {
            // The rule the references follow: three figures, three value keys, and the teacher is the
            // light one. Numbers, not adjectives, so a hand edit that darkens her cardigan fails here.
            float protagonistCoat = CharacterSkinPalette.Grey(CharacterSkinPalette.ProtagonistCoat);
            float qCoat = CharacterSkinPalette.Grey(CharacterSkinPalette.ClassmateCoat);
            float nCoat = CharacterSkinPalette.Grey(CharacterSkinPalette.TeacherCardigan);

            // Protagonist and Q were already 0.045 apart, which is under the palette's own 0.06
            // separation floor; that is pre-existing and is not fixed by moving N. What must hold is
            // that N sits clearly above both, in the same direction, by a step the eye can see at 4 m.
            Assert.Greater(nCoat, protagonistCoat);
            Assert.Greater(nCoat, qCoat, "N has to be clearly lighter than Q or the two greens merge");
            Assert.Greater(nCoat - qCoat, 0.12f);
            Assert.Greater(nCoat - protagonistCoat, 0.12f);

            Assert.Greater(CharacterSkinPalette.Grey(CharacterSkinPalette.TeacherSkin),
                CharacterSkinPalette.Grey(CharacterSkinPalette.ProtagonistSkin));
            Assert.Greater(CharacterSkinPalette.Grey(CharacterSkinPalette.TeacherShirt),
                CharacterSkinPalette.Grey(CharacterSkinPalette.ClassmateShirt));
        }

        [Test]
        public void EveryFigureRoleHasItsOwnPalette()
        {
            // A role added without colours used to be painted as Q by a boolean. Now it throws.
            foreach (FigureRole role in System.Enum.GetValues(typeof(FigureRole)))
            {
                CharacterSkinPalette.FigurePalette palette = CharacterSkinPalette.ForRole(role);
                Assert.IsTrue(CharacterSkinPalette.Separate(palette.Skin, palette.Hair), role + " skin/hair");
                Assert.IsTrue(CharacterSkinPalette.Separate(palette.Coat, palette.Shirt), role + " coat/shirt");
            }

            Assert.AreNotEqual(CharacterSkinPalette.ForRole(FigureRole.TeacherN).Coat,
                CharacterSkinPalette.ForRole(FigureRole.ClassmateQ).Coat);
        }

        [Test]
        public void ProportionsRejectABrokenTable()
        {
            HumanoidProportions broken = HumanoidProportions.Protagonist();
            broken.height = 3.4f;
            Assert.IsFalse(broken.Validate(out string error));
            StringAssert.Contains("outside", error);

            HumanoidProportions floating = HumanoidProportions.Protagonist();
            floating.thigh = new Vector3(0.086f, -0.2f, 0f);
            Assert.IsFalse(floating.Validate(out string error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void SkeletonRejectsAMissingBone()
        {
            _root = new GameObject("Rig Test Root");
            HumanoidRig.Create(_root.transform, HumanoidProportions.Protagonist(), out Transform skeletonRoot);
            Object.DestroyImmediate(skeletonRoot.Find(HumanoidBones.HandLeft).gameObject);

            Assert.Throws<System.ArgumentException>(() =>
                new ProceduralSkeleton(HumanoidBones.All, skeletonRoot));
        }

        [Test]
        public void BoneWeightNormalisationOrdersStrongestFirst()
        {
            BoneWeight4 weight = new BoneWeight4
            {
                bone0 = 3, weight0 = 0.2f,
                bone1 = 1, weight1 = 0.5f,
                bone2 = 2, weight2 = 0.0001f,
                bone3 = 0, weight3 = 0.3f,
            }.Normalized();

            Assert.AreEqual(1, weight.bone0, "the strongest influence must come first");
            Assert.AreEqual(0.5f, weight.weight0, 0.001f);
            Assert.AreEqual(0f, weight.weight2, "negligible influences are dropped");
            Assert.AreEqual(1f, weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3, 0.001f);
        }
    }
}
