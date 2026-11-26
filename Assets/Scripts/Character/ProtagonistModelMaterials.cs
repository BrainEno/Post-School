using System;
using UnityEngine;

namespace PostSchool.Character
{
    /// <summary>Skin, clothing and pencil-ink inputs for the protagonist model.</summary>
    public readonly struct ProtagonistMaterials
    {
        public ProtagonistMaterials(Material skin, Material coat, Material shirt, Material trousers, Material hair, Material accent, Material ink)
        {
            Skin = skin;
            Coat = coat;
            Shirt = shirt;
            Trousers = trousers;
            Hair = hair;
            Accent = accent;
            Ink = ink;
        }

        public Material Skin { get; }
        public Material Coat { get; }

        /// <summary>The school shirt showing under the adult coat.</summary>
        public Material Shirt { get; }

        public Material Trousers { get; }
        public Material Hair { get; }
        public Material Accent { get; }

        /// <summary>Drawn marks: eyes, the face seam, cuffs, the shoe.</summary>
        public Material Ink { get; }

        public bool IsComplete =>
            Skin != null && Coat != null && Shirt != null && Trousers != null && Hair != null && Accent != null && Ink != null;
    }

    /// <summary>Pivots the classroom prologue animates. The visual root's origin is at the feet.</summary>
    public sealed class ProtagonistRig
    {
        public ProtagonistRig(
            Transform visual,
            Vector3 visualRestPosition,
            Transform headPivot,
            Transform leftArmPivot,
            Transform rightArmPivot,
            Transform leftLegPivot,
            Transform rightLegPivot)
        {
            Visual = visual;
            VisualRestPosition = visualRestPosition;
            HeadPivot = headPivot;
            LeftArmPivot = leftArmPivot;
            RightArmPivot = rightArmPivot;
            LeftLegPivot = leftLegPivot;
            RightLegPivot = rightLegPivot;
        }

        /// <summary>Whole figure; the model's own origin is at the feet. Held in place while posing.</summary>
        public Transform Visual { get; }

        /// <summary>Where <see cref="Visual"/> sits relative to the character controller when standing.</summary>
        public Vector3 VisualRestPosition { get; }

        public Transform HeadPivot { get; }
        public Transform LeftArmPivot { get; }
        public Transform RightArmPivot { get; }
        public Transform LeftLegPivot { get; }
        public Transform RightLegPivot { get; }
    }

    /// <summary>
    /// The protagonist: an adult who has somehow ended up back in school, drawn so that their age
    /// refuses to resolve.
    ///
    /// The whole design is in the proportions, and they are deliberately between two conventions:
    ///   - 7 heads tall (a child measures about 6, an adult 7.5), total 1.72 m;
    ///   - narrow adolescent shoulders on an adult ribcage, with adult forearm and hand length;
    ///   - a long adult overcoat over a school shirt, cut like a uniform;
    ///   - no readable gender coding, no ethnic markers, and none of the usual age props
    ///     (no stubble, no rounded child cheeks, no beard, no makeup);
    ///   - a drawn ink seam splits the face and does not meet, so the two halves read as two
    ///     different people sharing one head;
    ///   - a red sleeve band on the right arm, like a school armband no school ever issued.
    ///
    /// Vertical landmarks in metres (y, floor = 0). Keep these true when editing; the classroom plan
    /// proof and docs/CLASSROOM_ART.md quote the same table:
    ///   shoe       0.000 - 0.070    knee     0.480 - 0.530
    ///   shin       0.190 - 0.530    hip      0.880 (leg pivot)
    ///   coat hem   0.620 - 1.060    ribcage  0.880 - 1.440
    ///   shoulder   1.320            neck     1.360 - 1.480
    ///   chin       1.460            eye line 1.600
    ///   crown      1.710            hair top 1.755
    ///   fingertip  0.732
    ///
    /// All geometry is Unity primitives arranged by code: no imported model, texture or rig.
    /// </summary>
    public static class ProtagonistModelFactory
    {
        /// <summary>Body height to the crown; the hair adds a few centimetres on top.</summary>
        public const float Height = 1.71f;

        /// <summary>
        /// Builds the model plus the pose rig the prologue needs. No colliders are added.
        /// The visual is offset so the feet land on the controller's root collider floor; the caller keeps
        /// the controller centred on the origin.
        /// </summary>
        public static ProtagonistRig Build(Transform parent, ProtagonistMaterials materials)
        {
            return Build(parent, materials, Vector3.zero);
        }

        /// <summary>Builds the model with the feet offset below the controller origin (see VisualRestPosition).</summary>
        public static ProtagonistRig Build(Transform parent, ProtagonistMaterials materials, Vector3 floorOffset)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (!materials.IsComplete)
            {
                throw new ArgumentException("All seven protagonist materials must be assigned.", nameof(materials));
            }

            Transform visual = new GameObject("Protagonist Visual").transform;
            visual.SetParent(parent, false);
            visual.localPosition = floorOffset; // Offset so the feet meet the floor collider.
            visual.localRotation = Quaternion.identity;

            Transform leftLeg = Pivot("Leg Left Pivot", visual, new Vector3(-0.105f, 0.88f, 0f));
            Transform rightLeg = Pivot("Leg Right Pivot", visual, new Vector3(0.105f, 0.88f, 0f));
            BuildLeg(leftLeg, materials);
            BuildLeg(rightLeg, materials);

            BuildCoatSkirt(visual, materials);
            BuildTorso(visual, materials);
            Transform head = BuildHead(visual, materials);
            Transform leftArm = BuildArm(visual, -1f, materials);
            Transform rightArm = BuildArm(visual, 1f, materials);

            return new ProtagonistRig(visual, floorOffset, head, leftArm, rightArm, leftLeg, rightLeg);
        }

        // ------------------------------------------------------------------ legs

        private static void BuildLeg(Transform pivot, ProtagonistMaterials materials)
        {
            // Thigh 0.48-0.86, shin 0.19-0.53, shoe 0.00-0.07.
            Part("Thigh", pivot, PrimitiveType.Capsule, new Vector3(0f, -0.21f, 0f),
                new Vector3(0.135f, 0.19f, 0.135f), materials.Trousers);
            Part("Shin", pivot, PrimitiveType.Capsule, new Vector3(0f, -0.53f, 0.004f),
                new Vector3(0.11f, 0.16f, 0.11f), materials.Trousers);
            Part("Trouser Cuff", pivot, PrimitiveType.Cylinder, new Vector3(0f, -0.665f, 0.004f),
                new Vector3(0.12f, 0.012f, 0.12f), materials.Ink);
            Part("Shoe", pivot, PrimitiveType.Sphere, new Vector3(0f, -0.845f, 0.045f),
                new Vector3(0.145f, 0.07f, 0.235f), materials.Ink);
        }

        private static void BuildCoatSkirt(Transform visual, ProtagonistMaterials materials)
        {
            // The adult overcoat: hips 0.62-1.06, wider than the ribcage, hem drawn in ink.
            Part("Coat Skirt", visual, PrimitiveType.Cylinder, new Vector3(0f, 0.84f, -0.012f),
                new Vector3(0.42f, 0.22f, 0.32f), materials.Coat);
            Part("Coat Hem", visual, PrimitiveType.Cylinder, new Vector3(0f, 0.625f, -0.012f),
                new Vector3(0.44f, 0.014f, 0.335f), materials.Ink);
            Part("Coat Vent", visual, PrimitiveType.Cube, new Vector3(0f, 0.75f, -0.185f),
                new Vector3(0.018f, 0.34f, 0.008f), materials.Ink);

            // Pockets the hands are not in.
            Part("Pocket Left", visual, PrimitiveType.Cube, new Vector3(-0.17f, 0.78f, 0.145f),
                new Vector3(0.11f, 0.07f, 0.01f), materials.Ink, new Vector3(0f, -14f, 0f));
            Part("Pocket Right", visual, PrimitiveType.Cube, new Vector3(0.17f, 0.78f, 0.145f),
                new Vector3(0.11f, 0.07f, 0.01f), materials.Ink, new Vector3(0f, 14f, 0f));
        }

        // ----------------------------------------------------------------- torso

        private static void BuildTorso(Transform visual, ProtagonistMaterials materials)
        {
            // Ribcage 0.88-1.44: adult volume, narrow shoulders at 1.32.
            Part("Ribcage", visual, PrimitiveType.Capsule, new Vector3(0f, 1.16f, 0f),
                new Vector3(0.34f, 0.28f, 0.235f), materials.Coat);
            Part("Coat Chest", visual, PrimitiveType.Cube, new Vector3(0f, 1.2f, 0.108f),
                new Vector3(0.3f, 0.34f, 0.05f), materials.Coat, new Vector3(-2f, 0f, 0f));

            // School shirt: a placket and two collar points under the adult lapels.
            Part("Shirt Placket", visual, PrimitiveType.Cube, new Vector3(0f, 1.2f, 0.138f),
                new Vector3(0.072f, 0.3f, 0.012f), materials.Shirt);
            Part("Collar Left", visual, PrimitiveType.Cube, new Vector3(-0.055f, 1.345f, 0.115f),
                new Vector3(0.085f, 0.055f, 0.045f), materials.Shirt, new Vector3(0f, 0f, 18f));
            Part("Collar Right", visual, PrimitiveType.Cube, new Vector3(0.055f, 1.345f, 0.115f),
                new Vector3(0.085f, 0.055f, 0.045f), materials.Shirt, new Vector3(0f, 0f, -18f));
            Part("Lapel Left", visual, PrimitiveType.Cube, new Vector3(-0.085f, 1.255f, 0.125f),
                new Vector3(0.075f, 0.2f, 0.018f), materials.Coat, new Vector3(0f, 0f, -7f));
            Part("Lapel Right", visual, PrimitiveType.Cube, new Vector3(0.085f, 1.255f, 0.125f),
                new Vector3(0.075f, 0.2f, 0.018f), materials.Coat, new Vector3(0f, 0f, 7f));

            // One button done, one thread pulled loose.
            Part("Button Done", visual, PrimitiveType.Sphere, new Vector3(0f, 1.24f, 0.15f),
                new Vector3(0.022f, 0.022f, 0.012f), materials.Accent);
            Part("Undone Thread", visual, PrimitiveType.Cube, new Vector3(0.016f, 1.15f, 0.15f),
                new Vector3(0.008f, 0.03f, 0.008f), materials.Accent, new Vector3(0f, 0f, 14f));

            // Shoulders: adult bone, adolescent mass.
            Part("Shoulder Left", visual, PrimitiveType.Sphere, new Vector3(-0.205f, 1.315f, 0f),
                new Vector3(0.115f, 0.1f, 0.125f), materials.Coat);
            Part("Shoulder Right", visual, PrimitiveType.Sphere, new Vector3(0.205f, 1.315f, 0f),
                new Vector3(0.115f, 0.1f, 0.125f), materials.Coat);

            // Two drawn seams across the chest, the second one missing the first.
            Part("Chest Seam", visual, PrimitiveType.Cube, new Vector3(0f, 1.1f, 0.145f),
                new Vector3(0.33f, 0.006f, 0.004f), materials.Ink, new Vector3(0f, 0f, 3.5f));
            Part("Chest Seam Second Pass", visual, PrimitiveType.Cube, new Vector3(0.012f, 1.092f, 0.146f),
                new Vector3(0.29f, 0.004f, 0.004f), materials.Ink, new Vector3(0f, 0f, 5f));
        }

        // ------------------------------------------------------------------ head

        private static Transform BuildHead(Transform visual, ProtagonistMaterials materials)
        {
            // Neck 1.36-1.48, then a head with an adult jaw under a young cranium.
            Part("Neck", visual, PrimitiveType.Cylinder, new Vector3(0f, 1.42f, 0f),
                new Vector3(0.082f, 0.06f, 0.082f), materials.Skin);

            Transform head = Pivot("Head Pivot", visual, new Vector3(0f, 1.48f, 0f));
            // The head is attached, but not quite aligned: the neck enters off-centre.
            head.localRotation = Quaternion.Euler(0f, 0f, 1.6f);

            // Cranium 1.50-1.71, jaw 1.46-1.60, chin at 1.46.
            Part("Cranium", head, PrimitiveType.Sphere, new Vector3(0f, 0.125f, 0.005f),
                new Vector3(0.176f, 0.21f, 0.185f), materials.Skin);
            Part("Jaw", head, PrimitiveType.Sphere, new Vector3(0f, 0.05f, 0.028f),
                new Vector3(0.15f, 0.14f, 0.16f), materials.Skin);
            Part("Ear Left", head, PrimitiveType.Sphere, new Vector3(-0.088f, 0.115f, 0.008f),
                new Vector3(0.028f, 0.048f, 0.03f), materials.Skin);
            Part("Ear Right", head, PrimitiveType.Sphere, new Vector3(0.093f, 0.12f, 0.012f),
                new Vector3(0.028f, 0.048f, 0.03f), materials.Skin);
            Part("Nose", head, PrimitiveType.Cube, new Vector3(-0.004f, 0.098f, 0.092f),
                new Vector3(0.026f, 0.055f, 0.03f), materials.Skin, new Vector3(6f, 0f, 0f));

            // The face disagrees with itself: left eye a drawn slit, right eye a hollow socket.
            Part("Eye Left Slit", head, PrimitiveType.Cube, new Vector3(-0.043f, 0.12f, 0.089f),
                new Vector3(0.048f, 0.008f, 0.01f), materials.Ink, new Vector3(0f, 0f, -6f));
            Part("Eye Right Socket", head, PrimitiveType.Sphere, new Vector3(0.044f, 0.124f, 0.084f),
                new Vector3(0.042f, 0.032f, 0.022f), materials.Ink);
            Part("Eye Right Pupil", head, PrimitiveType.Sphere, new Vector3(0.047f, 0.124f, 0.093f),
                new Vector3(0.015f, 0.016f, 0.01f), materials.Skin);

            Part("Mouth Line", head, PrimitiveType.Cube, new Vector3(0.004f, 0.052f, 0.086f),
                new Vector3(0.06f, 0.006f, 0.01f), materials.Ink, new Vector3(0f, 0f, -7f));
            Part("Brow Line", head, PrimitiveType.Cube, new Vector3(-0.048f, 0.152f, 0.086f),
                new Vector3(0.066f, 0.006f, 0.008f), materials.Ink, new Vector3(0f, 0f, -5f));

            // The seam: a crack from the crown past the cheek, with a mismatched second line beside it.
            Part("Face Seam", head, PrimitiveType.Cube, new Vector3(0.006f, 0.115f, 0.08f),
                new Vector3(0.005f, 0.195f, 0.004f), materials.Ink, new Vector3(0f, 0f, -4f));
            Part("Face Seam Offset", head, PrimitiveType.Cube, new Vector3(0.023f, 0.062f, 0.082f),
                new Vector3(0.004f, 0.085f, 0.004f), materials.Ink, new Vector3(0f, 0f, 9f));

            BuildHair(head, materials);
            return head;
        }

        private static void BuildHair(Transform head, ProtagonistMaterials materials)
        {
            // Cut short at the sides, left long on top and thrown the wrong way: neither a school cut
            // nor a professional one. Angular slabs, not a smooth cap, so the outline reads as drawn.
            Part("Hair Mass", head, PrimitiveType.Sphere, new Vector3(0f, 0.2f, -0.006f),
                new Vector3(0.19f, 0.125f, 0.19f), materials.Hair);
            Part("Hair Back", head, PrimitiveType.Sphere, new Vector3(0f, 0.16f, -0.05f),
                new Vector3(0.172f, 0.17f, 0.115f), materials.Hair);
            Part("Hair Sweep A", head, PrimitiveType.Cube, new Vector3(0.018f, 0.243f, 0.062f),
                new Vector3(0.175f, 0.028f, 0.06f), materials.Hair, new Vector3(-8f, 12f, -12f));
            Part("Hair Sweep B", head, PrimitiveType.Cube, new Vector3(-0.052f, 0.236f, 0.048f),
                new Vector3(0.125f, 0.026f, 0.05f), materials.Hair, new Vector3(-4f, -18f, 16f));
            Part("Hair Spike", head, PrimitiveType.Cube, new Vector3(0.072f, 0.255f, -0.008f),
                new Vector3(0.028f, 0.095f, 0.028f), materials.Hair, new Vector3(14f, 0f, -24f));
            Part("Hair Fringe Left", head, PrimitiveType.Cube, new Vector3(-0.13f, 0.185f, 0.04f),
                new Vector3(0.03f, 0.05f, 0.09f), materials.Hair, new Vector3(0f, 0f, 12f));
            Part("Sideburn Left", head, PrimitiveType.Cube, new Vector3(-0.083f, 0.1f, 0.026f),
                new Vector3(0.012f, 0.048f, 0.03f), materials.Hair);
        }

        // ------------------------------------------------------------------ arms

        private static Transform BuildArm(Transform visual, float side, ProtagonistMaterials materials)
        {
            Transform pivot = Pivot(side < 0f ? "Arm Left Pivot" : "Arm Right Pivot", visual,
                new Vector3(side * 0.213f, 1.315f, 0f));

            Part("Upper", pivot, PrimitiveType.Capsule, new Vector3(side * 0.012f, -0.135f, 0f),
                new Vector3(0.095f, 0.14f, 0.098f), materials.Coat);
            Part("Elbow", pivot, PrimitiveType.Sphere, new Vector3(side * 0.016f, -0.265f, 0f),
                new Vector3(0.084f, 0.072f, 0.084f), materials.Coat);

            // Adult forearm: longer and more defined than the rest of the silhouette.
            Part("Forearm", pivot, PrimitiveType.Capsule, new Vector3(side * 0.022f, -0.375f, 0.004f),
                new Vector3(0.08f, 0.15f, 0.08f), materials.Coat);
            Part("Cuff", pivot, PrimitiveType.Cylinder, new Vector3(side * 0.024f, -0.5f, 0.004f),
                new Vector3(0.084f, 0.014f, 0.084f), materials.Ink);
            Part("Hand", pivot, PrimitiveType.Sphere, new Vector3(side * 0.026f, -0.545f, 0.014f),
                new Vector3(0.06f, 0.082f, 0.048f), materials.Skin);
            Part("Knuckle Line", pivot, PrimitiveType.Cube, new Vector3(side * 0.026f, -0.545f, 0.042f),
                new Vector3(0.048f, 0.004f, 0.008f), materials.Ink);

            // The sleeve band: the only saturated mark on the figure, worn like an armband.
            if (side > 0f)
            {
                Part("Sleeve Band", pivot, PrimitiveType.Cylinder, new Vector3(side * 0.012f, -0.185f, 0f),
                    new Vector3(0.1f, 0.021f, 0.1f), materials.Accent);
                Part("Sleeve Band Thread", pivot, PrimitiveType.Cube, new Vector3(side * 0.06f, -0.165f, 0.018f),
                    new Vector3(0.006f, 0.055f, 0.006f), materials.Accent, new Vector3(12f, 0f, 0f));
            }

            return pivot;
        }

        private static Transform Pivot(string name, Transform parent, Vector3 localPosition)
        {
            Transform pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPosition;
            return pivot;
        }

        private static GameObject Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 size, Material material, Vector3? rotation = null)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            if (rotation.HasValue) part.transform.localRotation = Quaternion.Euler(rotation.Value);
            part.GetComponent<Renderer>().sharedMaterial = material;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(collider);
                else UnityEngine.Object.DestroyImmediate(collider);
            }

            return part;
        }
    }
}
