using System;
using System.Collections.Generic;
using PostSchool.Character;
using PostSchool.Core;
using PostSchool.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PostSchool.Tools.VisualCalibration
{
    /// <summary>
    /// Builds the first visual calibration scene from primitives and the shared palette.
    /// Menu: Post School > Visual Calibration > Build Calibration Scene. Re-running overwrites the scene
    /// and the materials in <see cref="MaterialFolder"/>. Scene layout is in metres; the room is 8 x 9 m.
    /// </summary>
    public static class VisualCalibrationSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/VisualCalibration.unity";
        public const string MaterialFolder = "Assets/Art/Calibration/Materials";

        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string UnlitShader = "Universal Render Pipeline/Unlit";

        [MenuItem("Post School/Visual Calibration/Build Calibration Scene")]
        public static void Build()
        {
            Dictionary<string, Material> materials = EnsureMaterials();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildArchitecture(NewGroup("Architecture"), materials);
            BuildFurniture(NewGroup("Furniture"), materials);
            BuildStudents(NewGroup("Students"), materials);
            Transform player = BuildPlayer(materials);
            BuildCameras(player);
            BuildLighting();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Visual calibration scene written to {ScenePath}. Play with Camera_Fixed_Authored (default); to compare, disable it and enable Camera_FollowComparison.");
        }

        private static Dictionary<string, Material> EnsureMaterials()
        {
            EnsureFolder(MaterialFolder);

            Dictionary<string, Material> result = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (CalibrationMaterial spec in VisualCalibrationPalette.Materials)
            {
                string shaderName = spec.Unlit ? UnlitShader : LitShader;
                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    throw new InvalidOperationException($"Shader '{shaderName}' was not found. Check that the Universal RP package matches the pinned Unity 6000.3.29f1 baseline.");
                }

                string path = $"{MaterialFolder}/{spec.Name}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }

                material.shader = shader;
                material.SetColor("_BaseColor", spec.Color);
                if (!spec.Unlit)
                {
                    material.SetFloat("_Smoothness", spec.Smoothness);
                }

                EditorUtility.SetDirty(material);
                result[spec.Name] = material;
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        private static void BuildArchitecture(Transform parent, IReadOnlyDictionary<string, Material> m)
        {
            Solid("Floor", parent, PrimitiveType.Cube, new Vector3(0f, -0.05f, 0f), new Vector3(8f, 0.1f, 9f), m[VisualCalibrationPalette.MaterialNames.Concrete], withCollider: true);
            Solid("Wall_Back", parent, PrimitiveType.Cube, new Vector3(0f, 1.6f, -4.5f), new Vector3(8f, 3.2f, 0.1f), m[VisualCalibrationPalette.MaterialNames.PaintedWall], withCollider: true);
            Solid("Wall_Front", parent, PrimitiveType.Cube, new Vector3(0f, 1.6f, 4.5f), new Vector3(8f, 3.2f, 0.1f), m[VisualCalibrationPalette.MaterialNames.PaintedWall], withCollider: true);
            Solid("Wall_Left", parent, PrimitiveType.Cube, new Vector3(-4f, 1.6f, 0f), new Vector3(0.1f, 3.2f, 9f), m[VisualCalibrationPalette.MaterialNames.PaintedSideWall], withCollider: true);
            Solid("Wall_Right", parent, PrimitiveType.Cube, new Vector3(4f, 1.6f, 0f), new Vector3(0.1f, 3.2f, 9f), m[VisualCalibrationPalette.MaterialNames.PaintedSideWall], withCollider: true);
            Solid("Ceiling", parent, PrimitiveType.Cube, new Vector3(0f, 3.2f, 0f), new Vector3(8f, 0.1f, 9f), m[VisualCalibrationPalette.MaterialNames.PaintedSideWall], withCollider: true);

            // Blackboard and three chalk-like strokes (paper material, no collider).
            Solid("Blackboard", parent, PrimitiveType.Cube, new Vector3(0f, 1.4f, 4.44f), new Vector3(3.6f, 1.3f, 0.06f), m[VisualCalibrationPalette.MaterialNames.Board]);
            Solid("Blackboard_Stroke_A", parent, PrimitiveType.Cube, new Vector3(-0.7f, 1.7f, 4.4f), new Vector3(0.9f, 0.02f, 0.01f), m[VisualCalibrationPalette.MaterialNames.Paper]);
            Solid("Blackboard_Stroke_B", parent, PrimitiveType.Cube, new Vector3(-0.5f, 1.45f, 4.4f), new Vector3(1.4f, 0.02f, 0.01f), m[VisualCalibrationPalette.MaterialNames.Paper]);
            Solid("Blackboard_Stroke_C", parent, PrimitiveType.Cube, new Vector3(0.6f, 1.2f, 4.4f), new Vector3(0.5f, 0.02f, 0.01f), m[VisualCalibrationPalette.MaterialNames.Paper]);

            // Door on the left wall with an ink lintel. The door is the exit focal point.
            Solid("Door", parent, PrimitiveType.Cube, new Vector3(-3.94f, 1.05f, 3.0f), new Vector3(0.06f, 2.1f, 0.95f), m[VisualCalibrationPalette.MaterialNames.Oxblood]);
            Solid("Door_Lintel_Ink", parent, PrimitiveType.Cube, new Vector3(-3.93f, 2.16f, 3.0f), new Vector3(0.08f, 0.08f, 1.1f), m[VisualCalibrationPalette.MaterialNames.Ink]);

            // Projected imagery: an unlit quad on the right wall, facing into the room (normal -X).
            Solid("Projection_Image", parent, PrimitiveType.Quad, new Vector3(3.94f, 1.7f, 0.5f), new Vector3(2.2f, 1.4f, 1f), m[VisualCalibrationPalette.MaterialNames.Projection], eulerAngles: new Vector3(0f, 90f, 0f));
            Solid("Projector_Body_Ink", parent, PrimitiveType.Cube, new Vector3(0f, 2.95f, 1.0f), new Vector3(0.45f, 0.2f, 0.3f), m[VisualCalibrationPalette.MaterialNames.Ink]);

            // Paper sheet pinned to the front wall beside the board: squared paper with pencil rulings.
            // Rulings sit slightly nearer the camera than the sheet (z 4.41 < 4.42) so they stay visible.
            Solid("GridPaper_Sheet", parent, PrimitiveType.Quad, new Vector3(2.9f, 1.7f, 4.42f), new Vector3(1.4f, 1.8f, 1f), m[VisualCalibrationPalette.MaterialNames.GridPaper]);
            Solid("GridPaper_Ruling_A", parent, PrimitiveType.Cube, new Vector3(2.9f, 1.25f, 4.41f), new Vector3(1.2f, 0.012f, 0.005f), m[VisualCalibrationPalette.MaterialNames.Graphite]);
            Solid("GridPaper_Ruling_B", parent, PrimitiveType.Cube, new Vector3(2.9f, 1.7f, 4.41f), new Vector3(1.2f, 0.012f, 0.005f), m[VisualCalibrationPalette.MaterialNames.Graphite]);
            Solid("GridPaper_Ruling_C", parent, PrimitiveType.Cube, new Vector3(2.9f, 2.15f, 4.41f), new Vector3(1.2f, 0.012f, 0.005f), m[VisualCalibrationPalette.MaterialNames.Graphite]);

            // Red-pen correction tick on the board, drawn in front of the board's face (z 4.39 < 4.41).
            Solid("Board_RedPen_Tick_A", parent, PrimitiveType.Cube, new Vector3(1.25f, 1.55f, 4.39f), new Vector3(0.45f, 0.03f, 0.01f), m[VisualCalibrationPalette.MaterialNames.RedPen], eulerAngles: new Vector3(0f, 0f, 35f));
            Solid("Board_RedPen_Tick_B", parent, PrimitiveType.Cube, new Vector3(1.55f, 1.42f, 4.39f), new Vector3(0.75f, 0.03f, 0.01f), m[VisualCalibrationPalette.MaterialNames.RedPen], eulerAngles: new Vector3(0f, 0f, -50f));

            // Hatched shadow bands along the floor at the front wall and the left wall: flat tone, no texture.
            Solid("Hatch_Base_Front", parent, PrimitiveType.Cube, new Vector3(0f, 0.004f, 4.2f), new Vector3(7.8f, 0.01f, 0.45f), m[VisualCalibrationPalette.MaterialNames.Hatch]);
            Solid("Hatch_Base_Left", parent, PrimitiveType.Cube, new Vector3(-3.7f, 0.004f, 0f), new Vector3(0.45f, 0.01f, 8.0f), m[VisualCalibrationPalette.MaterialNames.Hatch]);

            // One cut-out photograph on the right wall, away from the projection. Faces into the room (normal -X).
            Solid("Collage_Photo", parent, PrimitiveType.Quad, new Vector3(3.93f, 1.5f, -2.9f), new Vector3(1.1f, 0.8f, 1f), m[VisualCalibrationPalette.MaterialNames.CollagePhoto], eulerAngles: new Vector3(0f, 90f, 0f));

            // Ink contour strips along the back wall and left wall base.
            Solid("Ink_Contour_Back", parent, PrimitiveType.Cube, new Vector3(0f, 0.01f, -4.44f), new Vector3(8f, 0.02f, 0.03f), m[VisualCalibrationPalette.MaterialNames.Ink]);
            Solid("Ink_Contour_Left", parent, PrimitiveType.Cube, new Vector3(-3.94f, 0.01f, 0f), new Vector3(0.03f, 0.02f, 9f), m[VisualCalibrationPalette.MaterialNames.Ink]);
        }

        private static void BuildFurniture(Transform parent, IReadOnlyDictionary<string, Material> m)
        {
            float[] columns = { -2.2f, 0f, 2.2f };
            float[] rows = { -1.6f, 0.4f };

            foreach (float z in rows)
            {
                foreach (float x in columns)
                {
                    GameObject desk = new GameObject($"Desk_{x:0.0}_{z:0.0}");
                    desk.transform.SetParent(parent, false);
                    desk.transform.localPosition = new Vector3(x, 0f, z);

                    Solid("Top", desk.transform, PrimitiveType.Cube, new Vector3(0f, 0.75f, 0f), new Vector3(1.1f, 0.04f, 0.6f), m[VisualCalibrationPalette.MaterialNames.DeskTop], withCollider: true);
                    foreach (float legX in new[] { -0.5f, 0.5f })
                    {
                        foreach (float legZ in new[] { -0.25f, 0.25f })
                        {
                            Solid("Leg", desk.transform, PrimitiveType.Cube, new Vector3(legX, 0.37f, legZ), new Vector3(0.05f, 0.74f, 0.05f), m[VisualCalibrationPalette.MaterialNames.Ink]);
                        }
                    }
                }
            }

            // Teacher's lectern in solid concrete, against the front wall.
            Solid("Lectern_Concrete", parent, PrimitiveType.Cube, new Vector3(0f, 0.45f, 3.2f), new Vector3(1.2f, 0.9f, 0.6f), m[VisualCalibrationPalette.MaterialNames.Concrete], withCollider: true);
        }

        private static void BuildStudents(Transform parent, IReadOnlyDictionary<string, Material> m)
        {
            // The two authored figures, at the same real-world scale as the classroom. Compare them
            // against the cut-paper pupils behind: that contrast is what this scene is for.
            CharacterSkin protagonistSkin = new CharacterSkin(
                m[VisualCalibrationPalette.MaterialNames.StudentSkin],
                m[VisualCalibrationPalette.MaterialNames.StudentHair],
                m[VisualCalibrationPalette.MaterialNames.StudentOvershirt],
                m[VisualCalibrationPalette.MaterialNames.Paper],
                m[VisualCalibrationPalette.MaterialNames.StudentAccent]);
            CharacterSkin classmateSkin = new CharacterSkin(
                m[VisualCalibrationPalette.MaterialNames.StudentSkin],
                m[VisualCalibrationPalette.MaterialNames.StudentHair],
                m[VisualCalibrationPalette.MaterialNames.StudentCardigan],
                m[VisualCalibrationPalette.MaterialNames.Paper],
                m[VisualCalibrationPalette.MaterialNames.StudentAccent]);

            SkinnedCharacterBuilder.Create(
                "Protagonist_Rest", parent, new Vector3(-0.62f, 0f, 1.0f), 172f,
                HumanoidProportions.Protagonist(), FigureRole.Protagonist, protagonistSkin);

            SkinnedCharacterBuilder.Create(
                "ClassmateQ_Rest", parent, new Vector3(0.72f, 0f, 1.0f), 188f,
                HumanoidProportions.Classmate(), FigureRole.ClassmateQ, classmateSkin);

            // One figure stands in the middle of the wake clip so the calibration shot shows a bent
            // pose, not just the bind pose: this is where a bad weight shows as a pinch.
            SkinnedFigure waking = SkinnedCharacterBuilder.Create(
                "Protagonist_WakeMidpoint", parent, new Vector3(1.85f, 0f, 1.0f), 200f,
                HumanoidProportions.Protagonist(), FigureRole.Protagonist, protagonistSkin);
            HumanoidClips.WakeFromDesk().Sample(1.6f).ApplyClean(waking.Skeleton);

            // Supporting cut-paper pupils keep the collage language in the classroom background.
            // The center seats are left open so the original 3D figures read clearly in the authored shot.
            float[] columns = { -2.2f, 0f, 2.2f };
            float[] rows = { -1.6f, 0.4f };

            foreach (float z in rows)
            {
                foreach (float x in columns)
                {
                    if (Mathf.Approximately(x, 0f)) continue;

                    float seatZ = z - 0.42f;
                    GameObject student = new GameObject($"Student_CutPaper_{x:0.0}_{z:0.0}");
                    student.transform.SetParent(parent, false);
                    student.transform.localPosition = new Vector3(x, 0f, seatZ);

                    Solid("Body_Oxblood", student.transform, PrimitiveType.Quad, new Vector3(0f, 0.95f, 0f), new Vector3(0.5f, 0.7f, 1f), m[VisualCalibrationPalette.MaterialNames.Oxblood]);
                    Solid("Shirt_Paper_Layer", student.transform, PrimitiveType.Quad, new Vector3(0f, 0.95f, -0.02f), new Vector3(0.38f, 0.52f, 1f), m[VisualCalibrationPalette.MaterialNames.Paper]);
                    Solid("Head_Oxblood", student.transform, PrimitiveType.Sphere, new Vector3(0f, 1.35f, 0f), new Vector3(0.22f, 0.22f, 0.22f), m[VisualCalibrationPalette.MaterialNames.Oxblood]);
                }
            }
        }

        private static Transform BuildPlayer(IReadOnlyDictionary<string, Material> m)
        {
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0f, -2.8f);

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.7f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.85f, 0f);

            player.AddComponent<PlayerMovement>();
            player.AddComponent<PlayerInteractor>();

            // Provisional body: capsule in the ochre accent, the only saturated colour in the room.
            Solid("Body_Ochre", player.transform, PrimitiveType.Capsule, new Vector3(0f, 0.85f, 0f), new Vector3(0.6f, 0.85f, 0.6f), m[VisualCalibrationPalette.MaterialNames.Ochre]);
            return player.transform;
        }

        private static void BuildCameras(Transform player)
        {
            GameObject fixedGo = new GameObject("Camera_Fixed_Authored");
            fixedGo.tag = "MainCamera";
            Camera fixedCamera = fixedGo.AddComponent<Camera>();
            fixedCamera.fieldOfView = 50f;
            fixedCamera.nearClipPlane = 0.1f;
            fixedGo.transform.position = new Vector3(0f, 1.65f, -4.1f);
            fixedGo.transform.LookAt(new Vector3(0f, 1.1f, 1.6f));

            // Comparison: a follow camera with the existing FollowCamera component. Disabled by default.
            GameObject followGo = new GameObject("Camera_FollowComparison");
            Camera followCamera = followGo.AddComponent<Camera>();
            followCamera.fieldOfView = 50f;
            followCamera.enabled = false;
            FollowCamera follow = followGo.AddComponent<FollowCamera>();
            SetReference(follow, "_target", player);

            // Player movement is camera-relative to the fixed shot so the default view stays coherent.
            SetReference(player.GetComponent<PlayerMovement>(), "_cameraAnchor", fixedGo.transform);
        }

        private static void BuildLighting()
        {
            GameObject sun = new GameObject("Key_Light_Directional");
            Light key = sun.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = VisualCalibrationPalette.Hex("FFE7C2");
            key.intensity = 1.0f;
            // Hard shadows: the room is lit as a paper cut-out, with edges kept crisp rather than softened.
            key.shadows = LightShadows.Hard;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Projector glow: a spot from the ceiling box onto the projected image on the right wall.
            GameObject projector = new GameObject("Projector_Spot_Light");
            projector.transform.position = new Vector3(0f, 2.9f, 1.0f);
            projector.transform.LookAt(new Vector3(3.9f, 1.7f, 0.5f));
            Light glow = projector.AddComponent<Light>();
            glow.type = LightType.Spot;
            glow.color = VisualCalibrationPalette.Projection;
            glow.intensity = 2.0f;
            glow.range = 7f;
            glow.spotAngle = 40f;
            glow.shadows = LightShadows.None;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            // Lower flat ambient so the hatched bands and wall-floor seams read as shadow.
            RenderSettings.ambientLight = new Color(0.40f, 0.38f, 0.36f);
        }

        private static Transform NewGroup(string name)
        {
            return new GameObject(name).transform;
        }

        private static GameObject Solid(
            string name,
            Transform parent,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool withCollider = false,
            Vector3? eulerAngles = null)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            if (eulerAngles.HasValue)
            {
                go.transform.localRotation = Quaternion.Euler(eulerAngles.Value);
            }

            go.GetComponent<Renderer>().sharedMaterial = material;

            if (!withCollider)
            {
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            }

            return go;
        }

        private static void SetReference(Component owner, string field, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException($"{owner.GetType().Name} has no serialized field '{field}'. Update VisualCalibrationSceneBuilder to match the component.");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
