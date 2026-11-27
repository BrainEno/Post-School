using System;
using System.Collections.Generic;
using PostSchool.Character;
using UnityEngine;

namespace PostSchool.World
{
    /// <summary>
    /// Owns every runtime material for the prototype classroom and its figures, so nothing in the scene
    /// calls Shader.Find or invents a colour by index. One material per role, created on demand and
    /// destroyed by the owner. No texture, no imported asset; see docs/CLASSROOM_ART.md.
    /// </summary>
    public sealed class WorldSurfaceLibrary : IClassroomSurfaceProvider, IDisposable
    {
        private readonly Dictionary<SurfaceRole, Material> _surfaces = new Dictionary<SurfaceRole, Material>();

        // Every material ever created, including one-off character materials; Dispose must free them all.
        private readonly List<Material> _created = new List<Material>();
        private readonly Shader _shader;
        private Material _chalkGlow;

        public WorldSurfaceLibrary()
        {
            _shader = Shader.Find("Universal Render Pipeline/Lit");
            if (_shader == null) _shader = Shader.Find("Standard");
            if (_shader == null)
            {
                throw new InvalidOperationException("Neither URP/Lit nor the built-in Standard shader is available in this project.");
            }
        }

        public int MaterialCount => _created.Count;

        public Material Surface(SurfaceRole role)
        {
            if (_surfaces.TryGetValue(role, out Material existing)) return existing;

            Color color;
            float smoothness;
            switch (role)
            {
                case SurfaceRole.Floor: color = ClassroomArtPalette.Floor; smoothness = 0.12f; break;
                case SurfaceRole.Wall: color = ClassroomArtPalette.Wall; smoothness = 0.05f; break;
                case SurfaceRole.Ceiling: color = ClassroomArtPalette.Ceiling; smoothness = 0.04f; break;
                case SurfaceRole.Wainscot: color = ClassroomArtPalette.Wainscot; smoothness = 0.1f; break;
                case SurfaceRole.Blackboard: color = ClassroomArtPalette.Blackboard; smoothness = 0.28f; break;
                case SurfaceRole.Chalk: color = ClassroomArtPalette.Chalk; smoothness = 0.02f; break;
                case SurfaceRole.Wood: color = ClassroomArtPalette.Wood; smoothness = 0.18f; break;
                case SurfaceRole.Metal: color = ClassroomArtPalette.Metal; smoothness = 0.35f; break;
                case SurfaceRole.Glass: color = ClassroomArtPalette.Glass; smoothness = 0.8f; break;
                case SurfaceRole.Ink: color = ClassroomArtPalette.Ink; smoothness = 0.1f; break;
                case SurfaceRole.Accent: color = ClassroomArtPalette.AccentBlue; smoothness = 0.08f; break;
                case SurfaceRole.Paper: color = ClassroomArtPalette.Paper; smoothness = 0.03f; break;
                case SurfaceRole.Shadow: color = ClassroomArtPalette.Ink; smoothness = 0f; break;
                case SurfaceRole.DuskSkyHigh: color = ClassroomArtPalette.DuskSkyHigh; smoothness = 0f; break;
                case SurfaceRole.DuskSkyLow: color = ClassroomArtPalette.DuskSkyLow; smoothness = 0f; break;
                case SurfaceRole.DuskHaze: color = ClassroomArtPalette.DuskHaze; smoothness = 0f; break;
                case SurfaceRole.Building: color = ClassroomArtPalette.Building; smoothness = 0.02f; break;
                case SurfaceRole.BuildingNear: color = ClassroomArtPalette.BuildingNear; smoothness = 0.02f; break;
                case SurfaceRole.BuildingFar: color = ClassroomArtPalette.BuildingFar; smoothness = 0.02f; break;
                case SurfaceRole.WindowLit: color = ClassroomArtPalette.WindowLit; smoothness = 0.1f; break;
                case SurfaceRole.Street: color = ClassroomArtPalette.Street; smoothness = 0.03f; break;
                case SurfaceRole.ContainerBlue: color = ClassroomArtPalette.ContainerBlue; smoothness = 0.22f; break;
                case SurfaceRole.ContainerBlueShadow: color = ClassroomArtPalette.ContainerBlueShadow; smoothness = 0.22f; break;
                case SurfaceRole.Laundry: color = ClassroomArtPalette.Laundry; smoothness = 0.02f; break;
                case SurfaceRole.LaundryWarm: color = ClassroomArtPalette.LaundryWarm; smoothness = 0.02f; break;
                case SurfaceRole.Curtain: color = ClassroomArtPalette.Curtain; smoothness = 0.02f; break;
                case SurfaceRole.CurtainFold: color = ClassroomArtPalette.CurtainFold; smoothness = 0.02f; break;
                default: throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown surface role.");
            }

            Material material = Create("Surface " + role, color, smoothness, 0f);
            _surfaces[role] = material;
            return material;
        }

        /// <summary>Emissive version of a surface, for lit windows in the street and the sky's low band.</summary>
        public Material Glow(SurfaceRole role, float strength = 1f)
        {
            Color color;
            switch (role)
            {
                case SurfaceRole.WindowLit: color = ClassroomArtPalette.WindowLit; break;
                case SurfaceRole.DuskSkyLow: color = ClassroomArtPalette.DuskSkyLow; break;
                case SurfaceRole.DuskHaze: color = ClassroomArtPalette.DuskHaze; break;
                default: color = ClassroomArtPalette.Chalk; break;
            }

            Material material = Create("Glow " + role, color, 0f, Mathf.Max(0.05f, strength));
            return material;
        }

        /// <summary>Soft emitting material for the windows' borrowed light and the door panels.</summary>
        public Material Glow()
        {
            if (_chalkGlow != null) return _chalkGlow;

            _chalkGlow = Create("Surface Glow", ClassroomArtPalette.Chalk, 0.0f, 1.35f);
            return _chalkGlow;
        }

        /// <summary>
        /// The material set for a skinned figure. Flat, near-matte, and lit only by the sun and the
        /// ambient, which is what makes the figures read as cut paper rather than plastic.
        /// </summary>
        public CharacterSkin CreateCharacterSkin(FigureRole role)
        {
            bool protagonist = role == FigureRole.Protagonist;
            return new CharacterSkin(
                Create(role + " Body", protagonist ? CharacterSkinPalette.ProtagonistSkin : CharacterSkinPalette.ClassmateSkin, 0.06f, 0f),
                Create(role + " Hair", protagonist ? CharacterSkinPalette.ProtagonistHair : CharacterSkinPalette.ClassmateHair, 0.1f, 0f),
                Create(role + " Coat", protagonist ? CharacterSkinPalette.ProtagonistCoat : CharacterSkinPalette.ClassmateCoat, 0.05f, 0f),
                Create(role + " Shirt", protagonist ? CharacterSkinPalette.ProtagonistShirt : CharacterSkinPalette.ClassmateShirt, 0.04f, 0f),
                Create(role + " Accent", protagonist ? CharacterSkinPalette.ProtagonistAccent : CharacterSkinPalette.ClassmateAccent, 0.12f, 0f));
        }

        private Material Create(string name, Color color, float smoothness, float emission)
        {
            Material material = new Material(_shader) { name = name };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (emission > 0f && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }

            _created.Add(material);
            return material;
        }

        public void Dispose()
        {
            foreach (Material material in _created)
            {
                if (material != null) DestroyMaterial(material);
            }

            _created.Clear();
            _surfaces.Clear();
            _chalkGlow = null;
        }

        private static void DestroyMaterial(Material material)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(material);
            else UnityEngine.Object.DestroyImmediate(material);
        }
    }
}
