using System;
using UnityEngine;

namespace PostSchool.Character
{
    /// <summary>
    /// LEGACY. Primitive-composed student placeholders, kept only so the visual-calibration scene still
    /// has something to compare against. Gameplay no longer uses this factory: the protagonist and Q are
    /// skinned figures from <see cref="SkinnedCharacterBuilder"/>, and the unlabeled student was removed
    /// from the prototype scene. Do not add new callers.
    /// </summary>

    public enum StudentModelStyle
    {
        MossCardigan,
        ClayOvershirt,
    }

    /// <summary>Shared material inputs for the project's original, procedural student models.</summary>
    public readonly struct StudentModelMaterials
    {
        public StudentModelMaterials(
            Material skin,
            Material hair,
            Material outerwear,
            Material shirt,
            Material trousers,
            Material shoes,
            Material accent,
            Material detail)
        {
            Skin = skin;
            Hair = hair;
            Outerwear = outerwear;
            Shirt = shirt;
            Trousers = trousers;
            Shoes = shoes;
            Accent = accent;
            Detail = detail;
        }

        public Material Skin { get; }
        public Material Hair { get; }
        public Material Outerwear { get; }
        public Material Shirt { get; }
        public Material Trousers { get; }
        public Material Shoes { get; }
        public Material Accent { get; }
        public Material Detail { get; }
    }

    /// <summary>Original character palette shared by the playable classroom and calibration scene.</summary>
    public static class StudentModelColorPalette
    {
        public static readonly Color Skin = new Color32(211, 168, 139, 255);
        public static readonly Color Hair = new Color32(45, 42, 49, 255);
        public static readonly Color MossCardigan = new Color32(103, 130, 125, 255);
        public static readonly Color ClayOvershirt = new Color32(168, 105, 86, 255);
        public static readonly Color Shirt = new Color32(237, 231, 214, 255);
        public static readonly Color Trousers = new Color32(54, 59, 69, 255);
        public static readonly Color Shoes = new Color32(63, 54, 47, 255);
        public static readonly Color Accent = new Color32(195, 157, 89, 255);
        public static readonly Color Detail = new Color32(29, 29, 30, 255);
    }

    /// <summary>
    /// Builds lightweight, all-3D original student figures from Unity primitives. These are
    /// deliberately stylized prototype models, not copies or imports of the Drive references.
    /// </summary>
    public static class ProceduralStudentModelFactory
    {
        private const float ReferenceHeight = 1.81f;

        public static GameObject Create(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            float yawDegrees,
            float targetHeight,
            StudentModelStyle style,
            StudentModelMaterials materials)
        {
            if (string.IsNullOrWhiteSpace(objectName)) throw new ArgumentException("Student model names must be non-empty.", nameof(objectName));
            if (targetHeight <= 0f) throw new ArgumentOutOfRangeException(nameof(targetHeight), "Student height must be positive.");
            ValidateMaterials(materials);
            if (style != StudentModelStyle.MossCardigan && style != StudentModelStyle.ClayOvershirt)
            {
                throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown student model style.");
            }

            GameObject model = new GameObject(objectName);
            model.transform.SetParent(parent, false);
            model.transform.localPosition = localPosition;
            model.transform.localRotation = Quaternion.Euler(0f, yawDegrees, 0f);

            float scale = targetHeight / ReferenceHeight;
            BuildBody(model.transform, scale, style, materials);
            BuildHead(model.transform, scale, style, materials);
            BuildClothingDetails(model.transform, scale, style, materials);
            return model;
        }

        private static void BuildBody(Transform root, float scale, StudentModelStyle style, StudentModelMaterials materials)
        {
            Part("Torso", root, PrimitiveType.Capsule, new Vector3(0f, 1.04f, 0f), new Vector3(0.34f, 0.31f, 0.22f), materials.Outerwear, scale);
            Part("Shirt_Front_Panel", root, PrimitiveType.Cube, new Vector3(0f, 1.08f, 0.177f), new Vector3(0.17f, 0.37f, 0.025f), materials.Shirt, scale);
            Part("Collar", root, PrimitiveType.Cube, new Vector3(0f, 1.32f, 0.17f), new Vector3(0.21f, 0.08f, 0.04f), materials.Shirt, scale);
            Part("Shoulder_Left", root, PrimitiveType.Sphere, new Vector3(-0.28f, 1.25f, 0f), new Vector3(0.16f, 0.16f, 0.17f), materials.Outerwear, scale);
            Part("Shoulder_Right", root, PrimitiveType.Sphere, new Vector3(0.28f, 1.25f, 0f), new Vector3(0.16f, 0.16f, 0.17f), materials.Outerwear, scale);

            Part("Arm_Left_Upper", root, PrimitiveType.Capsule, new Vector3(-0.34f, 1.05f, 0f), new Vector3(0.12f, 0.23f, 0.12f), materials.Outerwear, scale, new Vector3(0f, 0f, -7f));
            Part("Arm_Right_Upper", root, PrimitiveType.Capsule, new Vector3(0.34f, 1.05f, 0f), new Vector3(0.12f, 0.23f, 0.12f), materials.Outerwear, scale, new Vector3(0f, 0f, 7f));
            Part("Arm_Left_Forearm", root, PrimitiveType.Capsule, new Vector3(-0.37f, 0.79f, 0.015f), new Vector3(0.085f, 0.19f, 0.09f), materials.Outerwear, scale, new Vector3(0f, 0f, -3f));
            Part("Arm_Right_Forearm", root, PrimitiveType.Capsule, new Vector3(0.37f, 0.79f, 0.015f), new Vector3(0.085f, 0.19f, 0.09f), materials.Outerwear, scale, new Vector3(0f, 0f, 3f));
            Part("Hand_Left", root, PrimitiveType.Sphere, new Vector3(-0.38f, 0.62f, 0.045f), new Vector3(0.07f, 0.09f, 0.055f), materials.Skin, scale);
            Part("Hand_Right", root, PrimitiveType.Sphere, new Vector3(0.38f, 0.62f, 0.045f), new Vector3(0.07f, 0.09f, 0.055f), materials.Skin, scale);

            Part("Leg_Left", root, PrimitiveType.Capsule, new Vector3(-0.12f, 0.39f, 0f), new Vector3(0.13f, 0.29f, 0.13f), materials.Trousers, scale);
            Part("Leg_Right", root, PrimitiveType.Capsule, new Vector3(0.12f, 0.39f, 0f), new Vector3(0.13f, 0.29f, 0.13f), materials.Trousers, scale);
            Part("Shoe_Left", root, PrimitiveType.Sphere, new Vector3(-0.12f, 0.08f, 0.055f), new Vector3(0.14f, 0.085f, 0.23f), materials.Shoes, scale);
            Part("Shoe_Right", root, PrimitiveType.Sphere, new Vector3(0.12f, 0.08f, 0.055f), new Vector3(0.14f, 0.085f, 0.23f), materials.Shoes, scale);
        }

        private static void BuildHead(Transform root, float scale, StudentModelStyle style, StudentModelMaterials materials)
        {
            Part("Neck", root, PrimitiveType.Cylinder, new Vector3(0f, 1.37f, 0f), new Vector3(0.075f, 0.105f, 0.075f), materials.Skin, scale);
            Part("Face", root, PrimitiveType.Sphere, new Vector3(0f, 1.53f, 0.02f), new Vector3(0.19f, 0.225f, 0.17f), materials.Skin, scale);
            Part("Ear_Left", root, PrimitiveType.Sphere, new Vector3(-0.185f, 1.51f, 0.01f), new Vector3(0.045f, 0.065f, 0.04f), materials.Skin, scale);
            Part("Ear_Right", root, PrimitiveType.Sphere, new Vector3(0.185f, 1.51f, 0.01f), new Vector3(0.045f, 0.065f, 0.04f), materials.Skin, scale);
            Part("Eye_Left", root, PrimitiveType.Sphere, new Vector3(-0.064f, 1.55f, 0.174f), new Vector3(0.022f, 0.027f, 0.014f), materials.Detail, scale);
            Part("Eye_Right", root, PrimitiveType.Sphere, new Vector3(0.064f, 1.55f, 0.174f), new Vector3(0.022f, 0.027f, 0.014f), materials.Detail, scale);
            Part("Nose", root, PrimitiveType.Sphere, new Vector3(0f, 1.49f, 0.188f), new Vector3(0.034f, 0.052f, 0.035f), materials.Skin, scale);
            Part("Mouth", root, PrimitiveType.Cube, new Vector3(0f, 1.415f, 0.181f), new Vector3(0.055f, 0.012f, 0.012f), materials.Detail, scale);

            Part("Hair_Back", root, PrimitiveType.Sphere, new Vector3(0f, 1.55f, -0.025f), new Vector3(0.205f, 0.235f, 0.18f), materials.Hair, scale);
            Part("Hair_Crown", root, PrimitiveType.Sphere, new Vector3(0f, 1.67f, 0.005f), new Vector3(0.205f, 0.12f, 0.18f), materials.Hair, scale);
            if (style == StudentModelStyle.MossCardigan)
            {
                Part("Hair_SideSweep_Left", root, PrimitiveType.Sphere, new Vector3(-0.09f, 1.615f, 0.14f), new Vector3(0.12f, 0.075f, 0.055f), materials.Hair, scale, new Vector3(0f, 0f, -18f));
                Part("Hair_SideSweep_Right", root, PrimitiveType.Sphere, new Vector3(0.045f, 1.62f, 0.15f), new Vector3(0.13f, 0.06f, 0.05f), materials.Hair, scale, new Vector3(0f, 0f, 10f));
                Part("Hair_Clip", root, PrimitiveType.Cube, new Vector3(-0.15f, 1.66f, 0.12f), new Vector3(0.07f, 0.025f, 0.025f), materials.Accent, scale, new Vector3(0f, 0f, -18f));
            }
            else
            {
                Part("Hair_Coil_Left", root, PrimitiveType.Sphere, new Vector3(-0.12f, 1.68f, 0.015f), new Vector3(0.09f, 0.095f, 0.09f), materials.Hair, scale);
                Part("Hair_Coil_Center", root, PrimitiveType.Sphere, new Vector3(0f, 1.72f, 0.025f), new Vector3(0.1f, 0.085f, 0.095f), materials.Hair, scale);
                Part("Hair_Coil_Right", root, PrimitiveType.Sphere, new Vector3(0.12f, 1.68f, 0.015f), new Vector3(0.09f, 0.095f, 0.09f), materials.Hair, scale);
                Part("Hair_Front_Lock", root, PrimitiveType.Sphere, new Vector3(0.09f, 1.61f, 0.15f), new Vector3(0.11f, 0.07f, 0.055f), materials.Hair, scale, new Vector3(0f, 0f, 14f));
            }
        }

        private static void BuildClothingDetails(Transform root, float scale, StudentModelStyle style, StudentModelMaterials materials)
        {
            for (int i = 0; i < 3; i++)
            {
                Part("Shirt_Button_" + i, root, PrimitiveType.Sphere, new Vector3(0f, 1.21f - i * 0.09f, 0.203f), new Vector3(0.018f, 0.018f, 0.012f), materials.Accent, scale);
            }

            Part("Cuff_Left", root, PrimitiveType.Cylinder, new Vector3(-0.37f, 0.68f, 0.015f), new Vector3(0.09f, 0.025f, 0.09f), materials.Shirt, scale);
            Part("Cuff_Right", root, PrimitiveType.Cylinder, new Vector3(0.37f, 0.68f, 0.015f), new Vector3(0.09f, 0.025f, 0.09f), materials.Shirt, scale);

            if (style == StudentModelStyle.MossCardigan)
            {
                Part("Bag_Satchel", root, PrimitiveType.Cube, new Vector3(-0.31f, 0.88f, -0.04f), new Vector3(0.22f, 0.27f, 0.16f), materials.Accent, scale);
                Part("Bag_Flap", root, PrimitiveType.Cube, new Vector3(-0.31f, 1.00f, 0.055f), new Vector3(0.2f, 0.08f, 0.025f), materials.Shirt, scale);
                Part("Bag_Strap", root, PrimitiveType.Cube, new Vector3(0.06f, 1.05f, 0.196f), new Vector3(0.055f, 0.48f, 0.025f), materials.Accent, scale, new Vector3(0f, 0f, -24f));
                Part("Cardigan_Pocket", root, PrimitiveType.Cube, new Vector3(0.2f, 1.08f, 0.181f), new Vector3(0.1f, 0.11f, 0.022f), materials.Accent, scale);
            }
            else
            {
                Part("Notebook_Cover", root, PrimitiveType.Cube, new Vector3(0.28f, 0.91f, 0.18f), new Vector3(0.2f, 0.29f, 0.045f), materials.Shirt, scale, new Vector3(0f, -8f, -4f));
                Part("Notebook_Spine", root, PrimitiveType.Cube, new Vector3(0.19f, 0.91f, 0.207f), new Vector3(0.018f, 0.28f, 0.012f), materials.Accent, scale);
                Part("Overshirt_Patch", root, PrimitiveType.Cube, new Vector3(-0.2f, 1.08f, 0.18f), new Vector3(0.1f, 0.12f, 0.025f), materials.Accent, scale);
            }
        }

        private static GameObject Part(
            string name,
            Transform parent,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 size,
            Material material,
            float scale,
            Vector3? eulerAngles = null)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position * scale;
            part.transform.localScale = size * scale;
            if (eulerAngles.HasValue) part.transform.localRotation = Quaternion.Euler(eulerAngles.Value);
            part.GetComponent<Renderer>().sharedMaterial = material;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(collider);
                else UnityEngine.Object.DestroyImmediate(collider);
            }

            return part;
        }

        private static void ValidateMaterials(StudentModelMaterials materials)
        {
            if (materials.Skin == null || materials.Hair == null || materials.Outerwear == null || materials.Shirt == null ||
                materials.Trousers == null || materials.Shoes == null || materials.Accent == null || materials.Detail == null)
            {
                throw new ArgumentException("All eight student model materials must be assigned.", nameof(materials));
            }
        }
    }
}
