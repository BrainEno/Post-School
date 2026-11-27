using System;
using System.Collections.Generic;
using UnityEngine;

namespace PostSchool.World
{
    /// <summary>
    /// Everything outside the missing front wall: a dusk sky, a row of buildings with lit windows, a
    /// street nine storeys down, washing strung across it, and one blue container to land on.
    ///
    /// Built as flat coloured slabs, the same way the room is built, so the two never look like they
    /// came from different projects. The only saturated thing out here is the container, because the
    /// whole scene's one accent rule says so: the protagonist's sleeve band, the curtains' drop, and the
    /// thing you land on.
    ///
    /// Nothing here is a texture: the dusk gradient is a stack of bands, the windows are geometry. That
    /// keeps the piece re-colourable and keeps the repository free of imported art.
    /// </summary>
    public static class DuskCityBuilder
    {
        public const string RootName = "Street Beyond The Wall";
        public const string ContainerName = "Blue Container (landing)";

        /// <summary>How far the camera wants to see. The buildings stop before this.</summary>
        public const float SkyDistance = 120f;

        /// <summary>
        /// Builds the street. <paramref name="room"/> is the classroom, so the ground is placed relative
        /// to the room's own floor and the drop is real. Returns the root object.
        /// </summary>
        public static GameObject Build(ClassroomLayout layout, IClassroomSurfaceProvider surfaces, Transform parent)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (surfaces == null) throw new ArgumentNullException(nameof(surfaces));

            FallZone fall = layout.fall;
            LaundryLine laundry = layout.laundry;

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(parent, false);

            BuildSky(root.transform, layout, surfaces);
            BuildGround(root.transform, layout, surfaces);
            BuildBuildings(root.transform, layout, surfaces);
            BuildWashingLine(root.transform, laundry, surfaces);
            BuildContainer(root.transform, fall, surfaces);

            return root;
        }

        // ------------------------------------------------------------------ sky

        /// <summary>
        /// The dusk: six bands from violet overhead to orange at the horizon. A gradient by geometry
        /// rather than by shader means it also survives in the offline proofs, which read the same data.
        /// </summary>
        private static void BuildSky(Transform root, ClassroomLayout layout, IClassroomSurfaceProvider surfaces)
        {
            Transform sky = new GameObject("Dusk").transform;
            sky.SetParent(root, false);

            float floorY = layout.fall.groundY;
            Color[] bands =
            {
                ClassroomArtPalette.DuskSkyHigh,
                Color.Lerp(ClassroomArtPalette.DuskSkyHigh, ClassroomArtPalette.DuskHaze, 0.35f),
                Color.Lerp(ClassroomArtPalette.DuskSkyHigh, ClassroomArtPalette.DuskSkyLow, 0.45f),
                Color.Lerp(ClassroomArtPalette.DuskHaze, ClassroomArtPalette.DuskSkyLow, 0.5f),
                ClassroomArtPalette.DuskSkyLow,
                Color.Lerp(ClassroomArtPalette.DuskSkyLow, ClassroomArtPalette.DuskHaze, 0.6f),
            };

            // The bands start at the street and stack upward, so the horizon is the bottom of the stack.
            float baseHeight = floorY;
            float bandHeight = 7.5f;
            for (int i = 0; i < bands.Length; i++)
            {
                // The gradient is carried by the three named sky surfaces; the middle bands reuse the
                // haze so the sky costs three materials, not six.
                SurfaceRole role = i <= 0 ? SurfaceRole.DuskSkyHigh : (i >= 4 ? SurfaceRole.DuskSkyLow : SurfaceRole.DuskHaze);
                Material material = surfaces.Surface(role);
                if (i == bands.Length - 1) material = surfaces.Glow(SurfaceRole.DuskSkyLow, 0.5f);

                Part(sky, "Band " + i, PrimitiveType.Cube,
                    new Vector3(0f, baseHeight + bandHeight * i, -SkyDistance + 4f),
                    new Vector3(220f, bandHeight + 0.02f, 0.4f), material);
            }

            // A low sun, above the far rooftops rather than behind them, so it is visible through both
            // the bare window and the gap where the front wall should be.
            float tallest = 0f;
            foreach (float buildingHeight in layout.fall.buildingHeights) tallest = Mathf.Max(tallest, buildingHeight);
            Part(sky, "Setting Sun", PrimitiveType.Sphere,
                new Vector3(-26f, floorY + tallest + 7.5f, -SkyDistance + 6f),
                new Vector3(7.5f, 7.5f, 7.5f), surfaces.Glow(SurfaceRole.WindowLit, 1.6f));
        }

        private static void BuildGround(Transform root, ClassroomLayout layout, IClassroomSurfaceProvider surfaces)
        {
            FallZone fall = layout.fall;
            Part(root, "Street", PrimitiveType.Cube,
                new Vector3(0f, fall.groundY - 0.5f, fall.streetCenterZ),
                new Vector3(60f, 1f, fall.streetWidth + 24f), surfaces.Surface(SurfaceRole.Street));

            // The kerb on the far side of the street, so the ground has an edge to read.
            Part(root, "Far Kerb", PrimitiveType.Cube,
                new Vector3(0f, fall.groundY + 0.06f, fall.streetCenterZ - fall.streetWidth * 0.5f - 1.4f),
                new Vector3(60f, 0.12f, 2.8f), surfaces.Surface(SurfaceRole.BuildingFar));
        }

        // ------------------------------------------------------------- buildings

        private static void BuildBuildings(Transform root, ClassroomLayout layout, IClassroomSurfaceProvider surfaces)
        {
            FallZone fall = layout.fall;
            Transform city = new GameObject("Buildings").transform;
            city.SetParent(root, false);

            float[] heights = fall.buildingHeights;
            float spacing = 7.2f;
            float total = spacing * heights.Length;

            for (int i = 0; i < heights.Length; i++)
            {
                float x = -total * 0.5f + spacing * (i + 0.5f);
                float height = heights[i];
                float depth = fall.buildingDepth;

                // Nearer the street the darker the block, so the row reads as receding dusk.
                SurfaceRole role = i % 2 == 0 ? SurfaceRole.Building : SurfaceRole.BuildingNear;
                // Measured from the street up: the block reaches the street, not the classroom floor.
                Part(city, "Block " + i, PrimitiveType.Cube,
                    new Vector3(x, fall.groundY + height * 0.5f, fall.buildingRowZ),
                    new Vector3(spacing - 0.9f, height, depth), surfaces.Surface(role));

                // Lit windows: a sparse grid, deterministic (no Random) so the shot is reproducible.
                int columns = 4;
                int rows = Mathf.Max(2, Mathf.FloorToInt(height / 2.6f) - 1);
                for (int c = 0; c < columns; c++)
                {
                    for (int r = 0; r < rows; r++)
                    {
                        int seed = (i * 31 + c * 7 + r * 13) % 11;
                        if (seed is 0 or 3 or 5 or 9) continue;

                        float wx = x - (spacing - 0.9f) * 0.5f + (c + 0.5f) * (spacing - 0.9f) / columns;
                        float wy = fall.groundY + 1.8f + r * 2.6f;
                        Part(city, $"Window {i}-{c}-{r}", PrimitiveType.Cube,
                            new Vector3(wx, wy, fall.buildingRowZ + depth * 0.5f + 0.02f),
                            new Vector3(0.9f, 1.25f, 0.06f), surfaces.Glow(SurfaceRole.WindowLit, 0.9f));
                    }
                }
            }

            // A far, pale block to close the view without pretending to be a detailed skyline.
            Part(city, "Far Block", PrimitiveType.Cube,
                new Vector3(-14f, fall.groundY + 16f, fall.buildingRowZ - 16f),
                new Vector3(46f, 32f, 6f), surfaces.Surface(SurfaceRole.BuildingFar));
        }

        // -------------------------------------------------------------- washing

        /// <summary>
        /// Washing on a line across the street. Each garment is a flat slab with a slight twist, because
        /// "clothes on a line" reads from the silhouette, not from cloth simulation.
        /// </summary>
        private static void BuildWashingLine(Transform root, LaundryLine laundry, IClassroomSurfaceProvider surfaces)
        {
            if (laundry == null || laundry.garments == null || laundry.garments.Length == 0) return;

            Transform line = new GameObject("Washing Line").transform;
            line.SetParent(root, false);

            float from = laundry.lineFromX;
            float to = laundry.lineToX;
            float length = to - from;

            Part(line, "Line", PrimitiveType.Cylinder,
                new Vector3((from + to) * 0.5f, laundry.lineY, laundry.lineZ),
                new Vector3(0.03f, length * 0.5f, 0.03f), surfaces.Surface(SurfaceRole.Ink),
                new Vector3(0f, 0f, 90f));

            Material cloth = surfaces.Surface(SurfaceRole.Laundry);
            Material warm = surfaces.Surface(SurfaceRole.LaundryWarm);

            for (int i = 0; i < laundry.garments.Length; i++)
            {
                string kind = laundry.garments[i] ?? "shirt";
                float t = (i + 0.5f) / laundry.garments.Length;
                float x = from + length * t;

                (float w, float h) size = kind switch
                {
                    "sheet" => (1.55f, 2.05f),
                    "towel" => (0.8f, 0.62f),
                    "trousers" => (0.72f, 1.15f),
                    "dress" => (0.95f, 1.35f),
                    _ => (0.78f, 0.9f),
                };

                Material material = i % 3 == 0 ? warm : cloth;
                float twist = ((i % 2 == 0) ? 1f : -1f) * (2.5f + i * 0.6f);

                Part(line, "Garment " + i + " (" + kind + ")", PrimitiveType.Cube,
                    new Vector3(x, laundry.lineY - size.h * 0.5f - 0.05f, laundry.lineZ),
                    new Vector3(size.w, size.h, 0.035f), material,
                    new Vector3(0f, 0f, twist));

                // The peg: one small block per garment, so nothing appears to float.
                Part(line, "Peg " + i, PrimitiveType.Cube,
                    new Vector3(x, laundry.lineY - 0.03f, laundry.lineZ),
                    new Vector3(0.05f, 0.1f, 0.05f), surfaces.Surface(SurfaceRole.Wood));
            }
        }

        // ------------------------------------------------------------ container

        /// <summary>
        /// The blue container. Its top is the landing target for a successful fall check, so the maths
        /// lives in <see cref="FallZone"/> and this only draws it.
        /// </summary>
        private static void BuildContainer(Transform root, FallZone fall, IClassroomSurfaceProvider surfaces)
        {
            Transform container = new GameObject(ContainerName).transform;
            container.SetParent(root, false);
            container.position = fall.ContainerCenter;

            float w = fall.containerWidth;
            float l = fall.containerLength;
            float h = fall.containerHeight;

            Material blue = surfaces.Surface(SurfaceRole.ContainerBlue);
            Material shadow = surfaces.Surface(SurfaceRole.ContainerBlueShadow);

            Part(container, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(w, h, l), blue);
            Part(container, "Lid", PrimitiveType.Cube, new Vector3(0f, h * 0.5f + 0.03f, 0f),
                new Vector3(w + 0.12f, 0.1f, l + 0.12f), shadow);

            // Corrugation: vertical ribs down the long sides. Twelve ribs is enough to read at 20 m.
            int ribs = 12;
            for (int i = 0; i < ribs; i++)
            {
                float x = -w * 0.5f + (i + 0.5f) * w / ribs;
                Part(container, "Rib " + i, PrimitiveType.Cube,
                    new Vector3(x, 0f, l * 0.5f + 0.03f),
                    new Vector3(0.12f, h * 0.86f, 0.06f), shadow);
            }

            // The door end, with its locking bars. This is the side the street sees.
            Part(container, "Door End", PrimitiveType.Cube,
                new Vector3(w * 0.5f + 0.04f, 0f, 0f), new Vector3(0.08f, h * 0.92f, l * 0.92f), shadow);
            for (int i = 0; i < 4; i++)
            {
                float z = -l * 0.36f + i * l * 0.24f;
                Part(container, "Locking Bar " + i, PrimitiveType.Cylinder,
                    new Vector3(w * 0.5f + 0.09f, 0f, z), new Vector3(0.035f, h * 0.44f, 0.035f), blue);
            }

            // A shadow slab under it: the container is the only thing between you and the street.
            Part(container, "Ground Shadow", PrimitiveType.Cube,
                new Vector3(0f, -fall.containerHeight * 0.5f + 0.01f, 0f),
                new Vector3(w + 1.4f, 0.02f, l + 1.2f), surfaces.Surface(SurfaceRole.Ink));
        }

        // ------------------------------------------------------------ primitives

        private static GameObject Part(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 position,
            Vector3 size,
            Material material,
            Vector3? euler = null)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            if (euler.HasValue) part.transform.localRotation = Quaternion.Euler(euler.Value);
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
