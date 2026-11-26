using System;
using NUnit.Framework;
using PostSchool.Character;
using UnityEngine;

namespace PostSchool.Tests.EditMode
{
    [TestFixture]
    public sealed class ProceduralStudentModelTests
    {
        private GameObject _parent;
        private Material _material;

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("Procedural student model test root");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Assert.IsNotNull(shader, "The project needs its configured URP Lit shader for character materials.");
            _material = new Material(shader);
        }

        [TearDown]
        public void TearDown()
        {
            if (_parent != null) UnityEngine.Object.DestroyImmediate(_parent);
            if (_material != null) UnityEngine.Object.DestroyImmediate(_material);
        }

        [Test]
        public void OriginalStudentStyles_BuildDistinctThreeDimensionalFiguresWithoutPrimitiveColliders()
        {
            StudentModelMaterials materials = new StudentModelMaterials(
                _material, _material, _material, _material, _material, _material, _material, _material);

            GameObject cardigan = ProceduralStudentModelFactory.Create(
                "Student_Original_A", _parent.transform, new Vector3(-1f, 0f, 2f), 155f, 1.60f,
                StudentModelStyle.MossCardigan, materials);
            GameObject overshirt = ProceduralStudentModelFactory.Create(
                "Student_Original_B", _parent.transform, new Vector3(1f, 0f, 2f), 205f, 1.68f,
                StudentModelStyle.ClayOvershirt, materials);

            Assert.Greater(cardigan.GetComponentsInChildren<MeshRenderer>().Length, 20);
            Assert.Greater(overshirt.GetComponentsInChildren<MeshRenderer>().Length, 20);
            Assert.AreEqual(0, cardigan.GetComponentsInChildren<Collider>().Length);
            Assert.AreEqual(0, overshirt.GetComponentsInChildren<Collider>().Length);
            Assert.IsNotNull(cardigan.transform.Find("Hair_Clip"));
            Assert.IsNotNull(cardigan.transform.Find("Bag_Satchel"));
            Assert.IsNotNull(overshirt.transform.Find("Hair_Coil_Center"));
            Assert.IsNotNull(overshirt.transform.Find("Notebook_Cover"));
            Assert.AreEqual(1.67f * 1.60f / 1.81f, cardigan.transform.Find("Hair_Crown").localPosition.y, 0.001f);
            Assert.AreEqual(1.67f * 1.68f / 1.81f, overshirt.transform.Find("Hair_Crown").localPosition.y, 0.001f);
        }

        [Test]
        public void Create_RejectsMissingMaterialsAndUnknownStyle()
        {
            Assert.Throws<ArgumentException>(() => ProceduralStudentModelFactory.Create(
                "Missing materials", _parent.transform, Vector3.zero, 0f, 1.6f,
                StudentModelStyle.MossCardigan, default));

            StudentModelMaterials materials = new StudentModelMaterials(
                _material, _material, _material, _material, _material, _material, _material, _material);
            Assert.Throws<ArgumentOutOfRangeException>(() => ProceduralStudentModelFactory.Create(
                "Unknown style", _parent.transform, Vector3.zero, 0f, 1.6f,
                (StudentModelStyle)999, materials));
        }
    }
}
