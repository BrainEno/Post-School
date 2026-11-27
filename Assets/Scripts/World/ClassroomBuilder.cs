using System;
using System.Collections.Generic;
using UnityEngine;

namespace PostSchool.World
{
    /// <summary>Named surface roles so the builder never guesses a material by index.</summary>
    public enum SurfaceRole
    {
        Floor,
        Wall,
        Ceiling,
        Wainscot,
        Blackboard,
        Chalk,
        Wood,
        Metal,
        Glass,
        Ink,
        Accent,
        Paper,
        Shadow,

        // Outside the missing front wall.
        DuskSkyHigh,
        DuskSkyLow,
        DuskHaze,
        Building,
        BuildingNear,
        BuildingFar,
        WindowLit,
        Street,
        ContainerBlue,
        ContainerBlueShadow,
        Laundry,
        LaundryWarm,
        Curtain,
        CurtainFold,
    }

    /// <summary>Receives every building step, so the scene owner decides how surfaces are made.</summary>
    public interface IClassroomSurfaceProvider
    {
        Material Surface(SurfaceRole role);

        /// <summary>
        /// Emissive "light without scenery": the outside seen through glass and door panels.
        /// The role picks the colour and the strength sets how far it burns, so DuskCityBuilder can ask
        /// for a lit window that is not the classroom's chalk glow. Defaults keep the old one-argument
        /// call meaning exactly what it did.
        /// </summary>
        Material Glow(SurfaceRole role = SurfaceRole.Chalk, float strength = 1.35f);
    }

    /// <summary>
    /// Builds the 24-seat classroom from <see cref="ClassroomLayout"/>: four walls with two window and
    /// two door openings, a blackboard wall, a podium, a seating grid, the protagonist's isolated front
    /// desk, ceiling fixtures, and a small number of deliberate impossibilities.
    ///
    /// Pure geometry composition. Nothing here owns gameplay, dialogue, or the protagonist; the caller
    /// wires interaction to the returned root's named children.
    /// See docs/CLASSROOM_ART.md for the composition rules this implements.
    /// </summary>
    public static class ClassroomBuilder
    {
        public const string RootName = "Classroom (procedural)";

        /// <summary>Named children the scene owner may need to look up. Keep in sync with the builders.</summary>
        public const string WakeDeskName = "Protagonist Desk (front, isolated)";

        /// <summary>The floor-to-ceiling curtain that stands where the front wall should be.</summary>
        public const string FrontCurtainName = "Front Curtain (the missing wall)";
        public const string ExitDoorId = "door_front_exit";

        /// <summary>Builds the whole classroom under a fresh root and returns it.</summary>
        public static GameObject Build(ClassroomLayout layout, IClassroomSurfaceProvider surfaces, Transform parent)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (surfaces == null) throw new ArgumentNullException(nameof(surfaces));
            if (!layout.Validate(out string error))
            {
                throw new InvalidOperationException("Classroom layout is unusable: " + error);
            }

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(parent, false);

            BuildShell(root.transform, layout, surfaces);
            BuildFrontCurtain(root.transform, layout, surfaces);
            BuildFloorDressing(root.transform, layout, surfaces);
            BuildBlackboardWall(root.transform, layout, surfaces);
            BuildPodium(root.transform, layout, surfaces);
            BuildSeating(root.transform, layout, surfaces);
            BuildWakeDesk(root.transform, layout, surfaces);
            BuildCeiling(root.transform, layout, surfaces);
            BuildAnnotations(root.transform, layout, surfaces);
            DuskCityBuilder.Build(layout, surfaces, root.transform);
            BuildCollision(root.transform, layout);
            return root;
        }

        /// <summary>Every seat position in the grid, front row first.</summary>
        public static IEnumerable<Vector2> SeatPositions(ClassroomLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            for (int row = 0; row < layout.seating.rows.Length; row++)
            {
                for (int column = 0; column < layout.seating.columns.Length; column++)
                {
                    yield return new Vector2(layout.seating.columns[column], layout.seating.rows[row]);
                }
            }
        }

        /// <summary>Seat index as written into the object names ("Seat 07 Desk").</summary>
        public static string SeatObjectName(int index, string suffix)
        {
            return $"Seat {index:00} {suffix}";
        }

        // ---------------------------------------------------------------- shell

        private static void BuildShell(Transform root, ClassroomLayout layout, IClassroomSurfaceProvider surfaces)
        {
            RoomDimensions room = layout.room;
            Transform shell = new GameObject("Shell").transform;
            shell.SetParent(root, false);

            BuildWall(shell, layout, "back", surfaces);
            // No front wall: the exit side is a full-drop curtain with the street behind it.
            BuildWall(shell, layout, "west", surfaces);
            BuildWall(shell, layout, "east", surfaces);
            BuildCorners(shell, layout, surfaces);
        }

        /// <summary>
        /// The front wall, which is a curtain. It hangs from a rail at the ceiling down to the floor and
        /// is the only thing between the room and the street: one wide drop with vertical folds, with the
        /// exit door left as a gap so the way out stays legible.
        ///
        /// Named <see cref="FrontCurtainName"/> because it is the scene's one controllable object: the
        /// interaction layer slides and raises it.
        /// </summary>
        private static void BuildFrontCurtain(Transform root, ClassroomLayout layout, IClassroomSurfaceProvider surfaces)
        {
            CurtainWall curtain = layout.frontWall;
            RoomDimensions room = layout.room;
            Material cloth = surfaces.Surface(SurfaceRole.Curtain);
            Material fold = surfaces.Surface(SurfaceRole.CurtainFold);
            Material rail = surfaces.Surface(SurfaceRole.Metal);

            Transform group = new GameObject(FrontCurtainName).transform;
            group.SetParent(root, false);
            group.localPosition = Vector3.zero;

            float z = room.MinZ - room.wallThickness * 0.5f;
            float top = curtain.curtainTop;
            float bottom = curtain.curtainBottom;
            float height = top - bottom;
            float centerY = bottom + height * 0.5f;

            Part(group, "Rail", PrimitiveType.Cylinder,
                new Vector3(room.MinX + room.width * 0.5f, top + 0.06f, z + 0.12f),
                new Vector3(0.07f, room.width * 0.5f, 0.07f), rail, new Vector3(0f, 0f, 90f));

            int folds = 26;
            float span = Mathf.Min(curtain.curtainWidth, room.width);
            float fromX = room.MinX + curtain.curtainCenterAlong - span * 0.5f;

            for (int i = 0; i < folds; i++)
            {
                float t = (i + 0.5f) / folds;
                float x = fromX + span * t;

                // Folds alternate depth and shade: a curtain is a corrugated surface, and shading it with
                // two flat materials is both cheaper and flatter than a normal map.
                float depth = (i % 2 == 0) ? 0.16f : 0.06f;
                Material material = (i % 2 == 0) ? cloth : fold;

                Part(group, "Fold " + i, PrimitiveType.Cube,
                    new Vector3(x, centerY, z + depth * 0.5f),
                    new Vector3(span / folds * 1.06f, height, depth), material);
            }

            Part(group, "Hem", PrimitiveType.Cube, new Vector3(fromX + span * 0.5f, bottom + 0.09f, z + 0.1f),
                new Vector3(span, 0.18f, 0.2f), fold);
            Part(group, "Header", PrimitiveType.Cube, new Vector3(fromX + span * 0.5f, top - 0.1f, z + 0.1f),
                new Vector3(span, 0.2f, 0.2f), fold);

            // The exit door still exists behind the curtain; its gap is what makes the drop read as a
            // doorway rather than a painted wall.
            foreach (WallOpening door in layout.doors)
            {
                if (door.wall != "front") continue;
                float doorX = room.MinX + door.centerAlong;
                Part(group, "Door Gap", PrimitiveType.Cube,
                    new Vector3(doorX, door.height * 0.5f, z + 0.02f),
                    new Vector3(door.width, door.height, 0.02f), surfaces.Surface(SurfaceRole.Ink));
            }
        }

        /// <summary>
        /// One wall, built as slabs around every opening it carries: solid runs between openings, plus a
        /// sill piece and a head piece for each gap. A doorway is therefore a real hole, not a decal.
        /// </summary>
        private static void BuildWall(Transform shell, ClassroomLayout layout, string wall, IClassroomSurfaceProvider surfaces)
        {
            Material material = surfaces.Surface(SurfaceRole.Wall);
            RoomDimensions room = layout.room;
            float height = room.height;

            List<WallOpening> openings = new List<WallOpening>();
            foreach (WallOpening window in layout.windows) if (window.wall == wall) openings.Add(window);
            foreach (WallOpening door in layout.doors) if (door.wall == wall) openings.Add(door);
            openings.Sort((a, b) => a.centerAlong.CompareTo(b.centerAlong));

            float length = layout.WallLength(wall);
            float cursor = 0f;

            for (int i = 0; i < openings.Count; i++)
            {
                WallOpening opening = openings[i];
                float from = opening.centerAlong - opening.width * 0.5f;
                float to = opening.centerAlong + opening.width * 0.5f;

                if (from - cursor > 0.001f)
                {
                    WallSegment(shell, layout, wall, $"{wall} wall run {i}", cursor, from, material);
                }

                float sill = Mathf.Max(0f, opening.sillHeight);
                float head = opening.sillHeight + opening.height;
                if (sill > 0.001f)
                {
                    WallSegment(shell, layout, wall, $"{opening.id} sill", from, to, material, 0f, sill);
                }

                if (height - head > 0.001f)
                {
                    WallSegment(shell, layout, wall, $"{opening.id} head", from, to, material, head, height);
                }

                if (IsDoor(layout, opening)) BuildDoorDressing(shell, layout, opening, surfaces);
                else BuildWindowDressing(shell, layout, opening, surfaces);

                cursor = to;
            }

            if (length - cursor > 0.001f)
            {
                WallSegment(shell, layout, wall, $"{wall} wall run end", cursor, length, material);
            }
        }

        private static bool IsDoor(ClassroomLayout layout, WallOpening opening)
        {
            foreach (WallOpening door in layout.doors)
            {
                if (ReferenceEquals(door, opening) || door.id == opening.id) return true;
            }

            return false;
        }

        /// <summary>A slab of wall between two along-values, optionally between two heights.</summary>
        private static void WallSegment(
            Transform shell,
            ClassroomLayout layout,
            string wall,
            string name,
            float alongFrom,
            float alongTo,
            Material material,
            float heightFrom = 0f,
            float heightTo = -1f)
        {
            RoomDimensions room = layout.room;
            float bottom = heightFrom;
            float top = heightTo < 0f ? room.height : heightTo;
            float midHeight = (bottom + top) * 0.5f;
            float sizeAlong = alongTo - alongFrom;
            float midAlong = (alongFrom + alongTo) * 0.5f;
            float thickness = room.wallThickness;

            Vector3 position;
            Vector3 size;
            switch (wall)
            {
                case "back":
                    position = new Vector3(room.MinX + midAlong, midHeight, room.MaxZ + thickness * 0.5f);
                    size = new Vector3(sizeAlong, top - bottom, thickness);
                    break;
                case "front":
                    position = new Vector3(room.MinX + midAlong, midHeight, room.MinZ - thickness * 0.5f);
                    size = new Vector3(sizeAlong, top - bottom, thickness);
                    break;
                case "west":
                    position = new Vector3(room.MinX - thickness * 0.5f, midHeight, room.MinZ + midAlong);
                    size = new Vector3(thickness, top - bottom, sizeAlong);
                    break;
                case "east":
                    position = new Vector3(room.MaxX + thickness * 0.5f, midHeight, room.MinZ + midAlong);
                    size = new Vector3(thickness, top - bottom, sizeAlong);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(wall), wall, "Unknown wall.");
            }

            Box(name, shell, position, size, material);
        }

        /// <summary>Four corner posts close the seams where two walls meet.</summary>
        private static void BuildCorners(Transform shell, ClassroomLayout layout, IClassroomSurfaceProvider surfaces)
        {
            RoomDimensions room = layout.room;
            float t = room.wallThickness;
            Material material = surfaces.Surface(SurfaceRole.Wall);
            float[] xs = { room.MinX - t * 0.5f, room.MaxX + t * 0.5f };
            float[] zs = { room.MinZ - t * 0.5f, room.MaxZ + t * 0.5f };
            for (int i = 0; i < xs.Length; i++)
            {
                for (int j = 0; j < zs.Length; j++)
                {
                    Box($"Corner {i}{j}", shell, new Vector3(xs[i], room.height * 0.5f, zs[j]),
                        new Vector3(t * 1.02f, room.height, t * 1.02f), material);
                }
            }
        }

        /// <summary>Where a wall sits in world space and which axis it runs along.</summary>
        private static void WallPlane(ClassroomLayout layout, string wall, out Vector3 basePoint, out Vector3 alongAxis, out Vector3 inwardNormal)
        {
            RoomDimensions room = layout.room;
            float t = room.wallThickness;
            switch (wall)
            {
                case "back":
                    basePoint = new Vector3(room.MinX, 0f, room.MaxZ + t * 0.5f);
                    alongAxis = Vector3.right;
                    inwardNormal = Vector3.back;
                    break;
                case "front":
                    basePoint = new Vector3(room.MinX, 0f, room.MinZ - t * 0.5f);
                    alongAxis = Vector3.right;
                    inwardNormal = Vector3.forward;
                    break;
                case "west":
                    basePoint = new Vector3(room.MinX - t * 0.5f, 0f, room.MinZ);
                    alongAxis = Vector3.forward;
                    inwardNormal = Vector3.right;
                    break;
                case "east":
                    basePoint = new Vector3(room.MaxX + t * 0.5f, 0f, room.MinZ);
                    alongAxis = Vector3.forward;
                    inwardNormal = Vector3.left;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(wall), wall, "Unknown wall.");
            }
        }

        private static void BuildDoorDressing(Transform shell, ClassroomLayout layout, WallOpening door, IClassroomSurfaceProvider surfaces)
        {
            WallPlane(layout, door.wall, out Vector3 basePoint, out Vector3 alongAxis, out Vector3 inward);
            Vector3 center = basePoint + alongAxis * door.centerAlong;

            Transform group = new GameObject(door.id + " Frame").transform;
            group.SetParent(shell, false);
            group.position = center;
            group.rotation = Quaternion.LookRotation(inward, Vector3.up);

            Material wood = surfaces.Surface(SurfaceRole.Wood);
            Material ink = surfaces.Surface(SurfaceRole.Ink);
            Material glass = surfaces.Surface(SurfaceRole.Glass);

            Part(group, "Jamb Left", PrimitiveType.Cube, new Vector3(-door.width * 0.5f, door.height * 0.5f, 0f),
                new Vector3(0.09f, door.height, 0.2f), wood);
            Part(group, "Jamb Right", PrimitiveType.Cube, new Vector3(door.width * 0.5f, door.height * 0.5f, 0f),
                new Vector3(0.09f, door.height, 0.2f), wood);
            Part(group, "Head", PrimitiveType.Cube, new Vector3(0f, door.height - 0.04f, 0f),
                new Vector3(door.width + 0.18f, 0.1f, 0.2f), wood);

            // A school door's tall vision panel, with the corridor beyond it lit flat white.
            Part(group, "Vision Panel", PrimitiveType.Cube, new Vector3(0f, door.height - 0.62f, 0.02f),
                new Vector3(door.width * 0.42f, 0.62f, 0.03f), glass);
            Part(group, "Vision Panel Light", PrimitiveType.Cube, new Vector3(0f, door.height - 0.62f, 0.05f),
                new Vector3(door.width * 0.34f, 0.54f, 0.01f), surfaces.Glow());
            Part(group, "Handle", PrimitiveType.Cylinder, new Vector3(door.width * 0.33f, 1.02f, 0.12f),
                new Vector3(0.03f, 0.09f, 0.03f), ink, new Vector3(90f, 0f, 0f));
        }

        private static void BuildWindowDressing(Transform shell, ClassroomLayout layout, WallOpening window, IClassroomSurfaceProvider surfaces)
        {
            WallPlane(layout, window.wall, out Vector3 basePoint, out Vector3 alongAxis, out Vector3 inward);
            Vector3 center = basePoint + alongAxis * window.centerAlong;
            float head = window.sillHeight + window.height;

            Transform group = new GameObject(window.id + " Frame").transform;
            group.SetParent(shell, false);
            group.position = center;
            group.rotation = Quaternion.LookRotation(inward, Vector3.up);

            Material frame = surfaces.Surface(SurfaceRole.Wood);
            Material glass = surfaces.Surface(SurfaceRole.Glass);
            float w = window.width;
            float h = window.height;
            float midY = (window.sillHeight + head) * 0.5f;

            // The pane sits slightly outside the wall line so it never z-fights the sill slab.
            Part(group, "Borrowed Light", PrimitiveType.Cube, new Vector3(0f, midY, -0.02f), new Vector3(w - 0.1f, h - 0.1f, 0.01f), surfaces.Glow());
            Part(group, "Glass", PrimitiveType.Cube, new Vector3(0f, midY, 0.01f), new Vector3(w - 0.08f, h - 0.08f, 0.02f), glass);
            Part(group, "Jamb Near", PrimitiveType.Cube, new Vector3(-w * 0.5f + 0.04f, midY, 0f), new Vector3(0.08f, h, 0.16f), frame);
            Part(group, "Jamb Far", PrimitiveType.Cube, new Vector3(w * 0.5f - 0.04f, midY, 0f), new Vector3(0.08f, h, 0.16f), frame);
            Part(group, "Head Rail", PrimitiveType.Cube, new Vector3(0f, head - 0.04f, 0f), new Vector3(w, 0.08f, 0.16f), frame);
            Part(group, "Sill Rail", PrimitiveType.Cube, new Vector3(0f, window.sillHeight + 0.03f, 0.07f), new Vector3(w + 0.16f, 0.06f, 0.26f), frame);
            Part(group, "Transom", PrimitiveType.Cube, new Vector3(0f, head - h * 0.32f, 0f), new Vector3(w - 0.1f, 0.05f, 0.1f), frame);
            Part(group, "Mullion", PrimitiveType.Cube, new Vector3(0f, midY, 0f), new Vector3(0.05f, h - 0.1f, 0.1f), frame);

            if (window.HasCurtain)
            {
                BuildFullDropCurtain(group, layout, window, surfaces);
            }
        }

        /// <summary>
        /// A covered window: a curtain hung from the ceiling to the floor, wider than the opening. Same
        /// language as the front wall's drop, so the room reads as one decision repeated twice, and the
        /// covered window becomes the clue for the wall that is missing.
        /// </summary>
        private static void BuildFullDropCurtain(Transform window, ClassroomLayout layout, WallOpening opening, IClassroomSurfaceProvider surfaces)
        {
            Material cloth = surfaces.Surface(SurfaceRole.Curtain);
            Material fold = surfaces.Surface(SurfaceRole.CurtainFold);
            Material rail = surfaces.Surface(SurfaceRole.Metal);

            float top = layout.frontWall.curtainTop;
            float bottom = layout.frontWall.curtainBottom;
            float height = top - bottom;
            float span = opening.width + 0.9f;

            Part(window, "Curtain Rail", PrimitiveType.Cylinder,
                new Vector3(0f, top + 0.04f, 0.2f), new Vector3(0.055f, span * 0.5f, 0.055f), rail,
                new Vector3(0f, 0f, 90f));

            int folds = 9;
            for (int i = 0; i < folds; i++)
            {
                float t = (i + 0.5f) / folds;
                float x = -span * 0.5f + span * t;
                float depth = (i % 2 == 0) ? 0.13f : 0.05f;
                Part(window, "Curtain Fold " + i, PrimitiveType.Cube,
                    new Vector3(x, bottom + height * 0.5f, 0.2f + depth * 0.5f),
                    new Vector3(span / folds * 1.08f, height, depth),
                    (i % 2 == 0) ? cloth : fold);
            }

            Part(window, "Curtain Hem", PrimitiveType.Cube,
                new Vector3(0f, bottom + 0.06f, 0.24f), new Vector3(span, 0.12f, 0.18f), fold);
        }

        private static void BuildFloorDressing(Transform root, ClassroomLayout layout, IClassroomSurfaceProvider surfaces)
        {
            RoomDimensions room = layout.room;
            Box("Floor", root, new Vector3(0f, -0.075f, room.centerZ), new Vector3(room.width, 0.15f, room.depth),
                surfaces.Surface(SurfaceRole.Floor));

            // Painted aisle band, the way institutional floors are marked down their length.
            Box("Aisle Band", root, new Vector3(0f, 0.004f, room.centerZ),
                new Vector3(1.6f, 0.008f, room.depth - 1.6f), surfaces.Surface(SurfaceRole.Accent));

            // Wainscot on the long walls: a darker band to 0.9 m with an ink dado rail.
            Material wainscot = surfaces.Surface(SurfaceRole.Wainscot);
            Material ink = surfaces.Surface(SurfaceRole.Ink);
            Box("West Wainscot", root, new Vector3(room.MinX + 0.03f, 0.45f, room.centerZ),
                new Vector3(0.1f, 0.9f, room.depth), wainscot);
            Box("East Wainscot", root, new Vector3(room.MaxX - 0.03f, 0.45f, room.centerZ),
                new Vector3(0.1f, 0.9f, room.depth), wainscot);
            Box("West Dado Rail", root, new Vector3(room.MinX + 0.07f, 0.92f, room.centerZ),
                new Vector3(0.05f, 0.05f, room.depth), ink);
            Box("East Dado Rail", root, new Vector3(room.MaxX - 0.07f, 0.92f, room.centerZ),
                new Vector3(0.05f, 0.05f, room.depth), ink);
        }

        // ------------------------------------------------------------ blackboard

        private static void BuildBlackboardWall(Transform root, ClassroomLayout layout, IClassroomSurfaceProvider surfaces)
        {
            Transform wall = new GameObject("Back Wall Dressing").transform;
            wall.SetParent(root, false);

            BlackboardDimensions board = layout.blackboard;
            float z = layout.room.MaxZ - 0.09f;
            float centerY = board.sillHeight + board.height * 0.5f;

            Box("Blackboard", wall, new Vector3(0f, centerY, z), new Vector3(board.width, board.height, 0.07f),
                surfaces.Surface(SurfaceRole.Blackboard));

            // Chalk frame drawn as thin marks, not a picture frame.
            Material chalk = surfaces.Surface(SurfaceRole.Chalk);
            float halfW = board.width * 0.5f;
            float halfH = board.height * 0.5f;
            float face = z - 0.045f;
            Box("Chalk Line Bottom", wall, new Vector3(0f, centerY - halfH + 0.02f, face), new Vector3(board.width, 0.018f, 0.005f), chalk);
            Box("Chalk Line Top", wall, new Vector3(0f, centerY + halfH - 0.02f, face), new Vector3(board.width, 0.018f, 0.005f), chalk);
            Box("Chalk Line Left", wall, new Vector3(-halfW + 0.02f, centerY, face), new Vector3(0.018f, board.height, 0.005f), chalk);
            Box("Chalk Line Right", wall, new Vector3(halfW - 0.02f, centerY, face), new Vector3(0.018f, board.height, 0.005f), chalk);

            // A lesson interrupted: a circle, an arrow pointing nowhere, and four tally marks.
            Ring("Chalk Circle On Board", wall, new Vector3(-1.45f, centerY, face), 0.4f, 0.03f, chalk, 26, Quaternion.Euler(90f, 0f, 0f));
            Box("Chalk Arrow Shaft", wall, new Vector3(0.5f, centerY + 0.1f, face), new Vector3(1.45f, 0.03f, 0.005f), chalk);
            Box("Chalk Arrow Head A", wall, new Vector3(1.17f, centerY + 0.05f, face), new Vector3(0.2f, 0.026f, 0.005f), chalk, Quaternion.Euler(0f, 0f, 28f));
            Box("Chalk Arrow Head B", wall, new Vector3(1.17f, centerY + 0.15f, face), new Vector3(0.2f, 0.026f, 0.005f), chalk, Quaternion.Euler(0f, 0f, -28f));
            for (int i = 0; i < 4; i++)
            {
                Box("Chalk Tally " + i, wall, new Vector3(-0.3f + i * 0.11f, centerY - 0.34f, face),
                    new Vector3(0.026f, 0.26f, 0.005f), chalk, Quaternion.Euler(0f, 0f, i % 2 == 0 ? -4f : 3f));
            }

            // Chalk tray with two sticks, one broken.
            Box("Chalk Tray", wall, new Vector3(0f, board.sillHeight - 0.06f, z - board.chalkTrayDepth * 0.5f),
                new Vector3(board.width, 0.05f, board.chalkTrayDepth), surfaces.Surface(SurfaceRole.Wood));
            Box("Chalk Stick A", wall, new Vector3(-1.1f, board.sillHeight - 0.025f, z - 0.08f),
                new Vector3(0.16f, 0.022f, 0.022f), chalk, Quaternion.Euler(0f, 22f, 0f));
            Box("Chalk Stick B", wall, new Vector3(0.42f, board.sillHeight - 0.025f, z - 0.07f),
                new Vector3(0.09f, 0.02f, 0.02f), chalk, Quaternion.Euler(0f, -14f, 0f));

            // Institutional specifics rather than generic dressing: a notice and a clock.
            Box("Notice Sheet", wall, new Vector3(-3.3f, 2.1f, z - 0.02f), new Vector3(0.42f, 0.56f, 0.01f),
                surfaces.Surface(SurfaceRole.Paper), Quaternion.Euler(0f, 0f, 2.5f));
            Box("Notice Pin", wall, new Vector3(-3.3f, 2.32f, z - 0.03f), new Vector3(0.03f, 0.03f, 0.02f),
                surfaces.Surface(SurfaceRole.Ink));
            Part(wall, "Clock Rim", PrimitiveType.Cylinder, new Vector3(3.3f, 2.4f, z - 0.03f),
                new Vector3(0.46f, 0.03f, 0.46f), surfaces.Surface(SurfaceRole.Ink), new Vector3(90f, 0f, 0f));
            Part(wall, "Clock Face", PrimitiveType.Cylinder, new Vector3(3.3f, 2.4f, z - 0.05f),
                new Vector3(0.4f, 0.02f, 0.4f), surfaces.Surface(SurfaceRole.Paper), new Vector3(90f, 0f, 0f));
            Box("Clock Hand", wall, new Vector3(3.3f, 2.47f, z - 0.06f), new Vector3(0.018f, 0.15f, 0.005f),
                surfaces.Surface(SurfaceRole.Ink), Quaternion.Euler(0f, 0f, -28f));
        }

        // ---------------------------------------------------------------- podium

        private static void BuildPodium(Transform root, ClassroomLayout layout, IClassroomSurfaceProvider surfaces)
        {
            PodiumPlacement podium = layout.podium;
            Transform group = new GameObject("Podium").transform;
            group.SetParent(root, false);

            Box("Body", group, new Vector3(podium.positionX, podium.height * 0.5f, podium.positionZ),
                new Vector3(podium.width, podium.height, podium.depth), surfaces.Surface(SurfaceRole.Wood));
            Box("Top", group, new Vector3(podium.positionX, podium.height + 0.035f, podium.positionZ),
                new Vector3(podium.width + 0.14f, 0.07f, podium.depth + 0.1f), surfaces.Surface(SurfaceRole.Ink));

            BoxCollider podiumCollider = group.gameObject.AddComponent<BoxCollider>();
            podiumCollider.center = new Vector3(podium.positionX, podium.height * 0.5f, podium.positionZ);
            podiumCollider.size = new Vector3(podium.width, podium.height, podium.depth);

            // Flat props on the top read clearly from the gameplay camera.
            Box("Book", group, new Vector3(podium.positionX - 0.24f, podium.height + 0.1f, podium.positionZ - 0.02f),
                new Vector3(0.32f, 0.05f, 0.24f), surfaces.Surface(SurfaceRole.Paper), Quaternion.Euler(0f, 12f, 0f));
            Box("Register", group, new Vector3(podium.positionX + 0.28f, podium.height + 0.09f, podium.positionZ + 0.04f),
                new Vector3(0.26f, 0.04f, 0.34f), surfaces.Surface(SurfaceRole.Paper), Quaternion.Euler(0f, -8f, 0f));

            // The teacher's chair, turned away from the room.
            BuildChair(group, "Podium Chair", new Vector3(podium.positionX, 0f, podium.positionZ - 0.9f),
                Quaternion.Euler(0f, 168f, 0f), layout, surfaces, true);
        }

        // --------------------------------------------------------------- seating

        private static void BuildSeating(Transform root, ClassroomLayout layout, IClassroomSurfaceProvider surfaces)
        {
            Transform grid = new GameObject("Seating Grid").transform;
            grid.SetParent(root, false);

            int seat = 0;
            foreach (Vector2 position in SeatPositions(layout))
            {
                BuildDesk(grid, SeatObjectName(seat, "Desk"), new Vector3(position.x, 0f, position.y), layout, surfaces);
                BuildChair(grid, SeatObjectName(seat, "Chair"),
                    new Vector3(position.x, 0f, position.y + layout.seating.chairOffsetZ),
                    Quaternion.identity, layout, surfaces, true);
                seat++;
            }

            ApplyImpossibilities(grid, layout);
        }

        /// <summary>
        /// Two impossibilities placed where the player will walk into them: one desk hangs off the floor,
        /// one chair is sunk into it. Both are inside the 24, never extra seats.
        /// </summary>
        private static void ApplyImpossibilities(Transform grid, ClassroomLayout layout)
        {
            const int levitatingSeat = 6;
            const int sinkingSeat = 17;

            if (levitatingSeat < layout.seating.expectedSeatCount)
            {
                Transform desk = grid.Find(SeatObjectName(levitatingSeat, "Desk"));
                if (desk != null) desk.localPosition += new Vector3(0f, 0.24f, 0f);
            }

            if (sinkingSeat < layout.seating.expectedSeatCount)
            {
                Transform chair = grid.Find(SeatObjectName(sinkingSeat, "Chair"));
                if (chair != null) chair.localPosition -= new Vector3(0f, 0.2f, 0f);
            }
        }

        private static void BuildDesk(Transform parent, string name, Vector3 position, ClassroomLayout layout, IClassroomSurfaceProvider surfaces)
        {
            Material top = surfaces.Surface(SurfaceRole.Wood);
            Material leg = surfaces.Surface(SurfaceRole.Metal);
            float w = layout.seating.deskWidth;
            float d = layout.seating.deskDepth;
            float h = layout.seating.deskHeight;

            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = position;

            Part(group, "Top", PrimitiveType.Cube, new Vector3(0f, h, 0f), new Vector3(w, 0.05f, d), top);

            // Real furniture: the player cannot walk through a desk. One box per desk, not per part.
            BoxCollider collider = group.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, h * 0.5f, 0f);
            collider.size = new Vector3(w, h, d);
            Part(group, "Apron", PrimitiveType.Cube, new Vector3(0f, h - 0.09f, -d * 0.34f), new Vector3(w * 0.9f, 0.12f, 0.03f), leg);
            Part(group, "Book", PrimitiveType.Cube, new Vector3(0.06f, h + 0.044f, -0.02f), new Vector3(0.28f, 0.035f, 0.2f),
                surfaces.Surface(SurfaceRole.Paper), new Vector3(0f, 8f, 0f));
            for (int i = 0; i < 4; i++)
            {
                float sx = i % 2 == 0 ? -1f : 1f;
                float sz = i < 2 ? -1f : 1f;
                Part(group, "Leg " + i, PrimitiveType.Cylinder,
                    new Vector3(sx * (w * 0.5f - 0.07f), (h - 0.05f) * 0.5f, sz * (d * 0.5f - 0.07f)),
                    new Vector3(0.032f, (h - 0.05f) * 0.5f, 0.032f), leg);
            }
        }

        private static void BuildChair(
            Transform parent,
            string name,
            Vector3 position,
            Quaternion rotation,
            ClassroomLayout layout,
            IClassroomSurfaceProvider surfaces,
            bool withCollider)
        {
            Material wood = surfaces.Surface(SurfaceRole.Wood);
            Material metal = surfaces.Surface(SurfaceRole.Metal);
            float s = layout.seating.chairSize;

            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = position;
            group.localRotation = rotation;

            Part(group, "Seat", PrimitiveType.Cube, new Vector3(0f, 0.44f, 0f), new Vector3(s, 0.05f, s * 0.95f), wood);
            Part(group, "Back", PrimitiveType.Cube, new Vector3(0f, 0.72f, s * 0.42f), new Vector3(s, 0.42f, 0.045f), wood);
            for (int i = 0; i < 4; i++)
            {
                float sx = i % 2 == 0 ? -1f : 1f;
                float sz = i < 2 ? -1f : 1f;
                Part(group, "Leg " + i, PrimitiveType.Cylinder, new Vector3(sx * (s * 0.5f - 0.05f), 0.21f, sz * (s * 0.5f - 0.05f)),
                    new Vector3(0.026f, 0.21f, 0.026f), metal);
            }

            if (withCollider)
            {
                // Chairs are obstacles as well: seat plus back plus legs, one box.
                BoxCollider collider = group.gameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 0.45f, 0.1f);
                collider.size = new Vector3(s, 0.9f, s);
            }
        }

        // ------------------------------------------------------------- wake desk

        private static void BuildWakeDesk(Transform root, ClassroomLayout layout, IClassroomSurfaceProvider surfaces)
        {
            WakeDeskPlacement desk = layout.wakeDesk;
            Material wood = surfaces.Surface(SurfaceRole.Wood);
            Material metal = surfaces.Surface(SurfaceRole.Metal);

            Transform group = new GameObject(WakeDeskName).transform;
            group.SetParent(root, false);
            group.localPosition = new Vector3(desk.positionX, 0f, desk.positionZ);

            Part(group, "Top", PrimitiveType.Cube, new Vector3(0f, desk.height, 0f), new Vector3(desk.width, 0.08f, desk.depth), wood);

            // The protagonist wakes up on this desk, so it has to be solid.
            BoxCollider deskCollider = group.gameObject.AddComponent<BoxCollider>();
            deskCollider.center = new Vector3(0f, desk.height * 0.5f, 0f);
            deskCollider.size = new Vector3(desk.width, desk.height, desk.depth);
            Part(group, "Top Lip", PrimitiveType.Cube, new Vector3(0f, desk.height - 0.03f, -desk.depth * 0.5f + 0.02f),
                new Vector3(desk.width + 0.05f, 0.1f, 0.04f), metal);
            for (int i = 0; i < 4; i++)
            {
                float sx = i % 2 == 0 ? -1f : 1f;
                float sz = i < 2 ? -1f : 1f;
                Part(group, "Leg " + i, PrimitiveType.Cube,
                    new Vector3(sx * (desk.width * 0.5f - 0.1f), (desk.height - 0.08f) * 0.5f, sz * (desk.depth * 0.5f - 0.12f)),
                    new Vector3(0.08f, desk.height - 0.08f, 0.08f), metal);
            }

            // One thing on the desk: a school bag, dropped rather than placed.
            Transform bag = new GameObject("Dropped Bag").transform;
            bag.SetParent(group, false);
            bag.localPosition = new Vector3(-0.62f, desk.height + 0.16f, 0.18f);
            Part(bag, "Body", PrimitiveType.Cube, Vector3.zero, new Vector3(0.34f, 0.3f, 0.2f),
                surfaces.Surface(SurfaceRole.Ink));
            Part(bag, "Flap", PrimitiveType.Cube, new Vector3(0f, 0.12f, 0.1f), new Vector3(0.36f, 0.12f, 0.02f),
                surfaces.Surface(SurfaceRole.Wainscot));
            Part(bag, "Strap", PrimitiveType.Cube, new Vector3(0.14f, 0f, -0.02f), new Vector3(0.04f, 0.3f, 0.22f),
                surfaces.Surface(SurfaceRole.Wainscot));

            // The protagonist's chair is pushed aside and turned: they got up from it in a hurry,
            // and the standing position after the wake animation has to stay clear of it.
            BuildChair(group, "Protagonist Chair", new Vector3(0.62f, 0f, -1.5f),
                Quaternion.Euler(0f, 34f, 0f), layout, surfaces, true);
        }

        // -------------------------------------------------------------- ceiling

        private static void BuildCeiling(Transform root, ClassroomLayout layout, IClassroomSurfaceProvider surfaces)
        {
            Transform ceiling = new GameObject("Ceiling").transform;
            ceiling.SetParent(root, false);

            Box("Slab", ceiling, new Vector3(0f, layout.room.height + 0.08f, layout.room.centerZ),
                new Vector3(layout.room.width, 0.16f, layout.room.depth), surfaces.Surface(SurfaceRole.Ceiling));

            for (int i = 0; i < layout.ceilingLights.Length; i++)
            {
                CeilingLightPlacement light = layout.ceilingLights[i];
                Transform group = new GameObject("Ceiling Light " + i).transform;
                group.SetParent(ceiling, false);
                group.localPosition = new Vector3(light.x, light.height, light.z);
                group.localRotation = Quaternion.Euler(0f, 0f, light.tiltDegrees);

                Part(group, "Tube", PrimitiveType.Cube, Vector3.zero, new Vector3(0.2f, 0.06f, light.length),
                    surfaces.Surface(SurfaceRole.Chalk));
                Part(group, "Glow", PrimitiveType.Cube, new Vector3(0f, -0.045f, 0f),
                    new Vector3(0.28f, 0.02f, light.length + 0.24f), surfaces.Glow());
                Part(group, "Cord", PrimitiveType.Cylinder, new Vector3(0f, (layout.room.height - light.height) * 0.5f + 0.03f, 0f),
                    new Vector3(0.012f, (layout.room.height - light.height) * 0.5f, 0.012f), surfaces.Surface(SurfaceRole.Ink));
            }

            // Two cords that end before they reach anything: a drawn gesture rather than a fixture.
            for (int i = 0; i < 2; i++)
            {
                Part(ceiling, "Hanging Cord " + i, PrimitiveType.Cylinder,
                    new Vector3(-1.7f + i * 3.5f, layout.room.height - 0.55f, 4.9f + i * 1.3f),
                    new Vector3(0.012f, 0.55f, 0.012f), surfaces.Surface(SurfaceRole.Ink));
            }
        }

        // ------------------------------------------------------------ annotations

        /// <summary>Chalk circle, floor ticks and wall marks: the "ink and absence" layer.</summary>
        private static void BuildAnnotations(Transform root, ClassroomLayout layout, IClassroomSurfaceProvider surfaces)
        {
            Transform marks = new GameObject("Chalk Marks").transform;
            marks.SetParent(root, false);

            Material chalk = surfaces.Surface(SurfaceRole.Chalk);
            ChalkCircle circle = layout.chalkCircle;
            Ring("Chalk Circle On Floor", marks, new Vector3(circle.centerX, 0.008f, circle.centerZ),
                circle.radius, 0.05f, chalk, circle.segments, Quaternion.Euler(90f, 0f, 0f));

            // Four corner ticks, drawn like a plan sheet laid on the floor.
            for (int i = 0; i < 4; i++)
            {
                float sx = i % 2 == 0 ? -1f : 1f;
                float sz = i < 2 ? -1f : 1f;
                Box("Floor Tick " + i, marks,
                    new Vector3(sx * (layout.room.width * 0.5f - 1.2f), 0.009f, layout.room.centerZ + sz * (layout.room.depth * 0.5f - 1.4f)),
                    new Vector3(0.9f, 0.005f, 0.05f), chalk, Quaternion.Euler(0f, 37f * i, 0f));
            }

            // Writing on the east wall made of thin marks, not a texture: three unreadable lines.
            for (int i = 0; i < 7; i++)
            {
                Box("Wall Mark " + i, marks,
                    new Vector3(layout.room.MaxX - 0.06f, 1.4f + (i % 3) * 0.085f, 5.3f + i * 0.2f),
                    new Vector3(0.008f, 0.01f, 0.32f + (i % 2) * 0.16f), chalk);
            }
        }

        // -------------------------------------------------------------- collision

        /// <summary>
        /// Collision is one box per wall plus floor and ceiling, with the two doorways left open so both
        /// doors are walkable. Props carry no colliders, which keeps interaction rays cheap.
        /// </summary>
        private static void BuildCollision(Transform root, ClassroomLayout layout)
        {
            RoomDimensions room = layout.room;
            Transform collision = new GameObject("Collision").transform;
            collision.SetParent(root, false);

            Box("Floor Collider", collision, new Vector3(0f, -0.4f, room.centerZ), new Vector3(room.width + 2f, 0.8f, room.depth + 2f), null);
            Box("Ceiling Collider", collision, new Vector3(0f, room.height + 0.4f, room.centerZ), new Vector3(room.width + 2f, 0.8f, room.depth + 2f), null);

            // No collider on the front: the room is open onto the street by design. The side walls stop
            // at the front edge, so the exit behaves like an opening and not like a walled corner.
            AddWallColliders(collision, layout, "back");
            AddWallColliders(collision, layout, "west");
            AddWallColliders(collision, layout, "east");

            // Outside: the street, and the container's lid. Without these the one branch where the
            // protagonist survives ends with them falling a second time, through the street.
            FallZone fall = layout.fall;
            Box("Street Collider", collision,
                new Vector3(0f, fall.groundY - 0.5f, fall.streetCenterZ),
                new Vector3(60f, 1f, fall.streetWidth + 24f), null);

            Box("Container Collider", collision,
                new Vector3(fall.containerCenterX, fall.groundY + fall.containerHeight * 0.5f, fall.containerCenterZ),
                new Vector3(fall.containerWidth, fall.containerHeight, fall.containerLength), null);
        }

        private static void AddWallColliders(Transform collision, ClassroomLayout layout, string wall)
        {
            RoomDimensions room = layout.room;
            float thickness = 0.6f;
            float outer = 0.5f;

            List<WallOpening> doors = new List<WallOpening>();
            foreach (WallOpening door in layout.doors) if (door.wall == wall) doors.Add(door);
            doors.Sort((a, b) => a.centerAlong.CompareTo(b.centerAlong));

            float length = layout.WallLength(wall);
            float cursor = -outer;
            int index = 0;
            foreach (WallOpening door in doors)
            {
                float from = door.centerAlong - door.width * 0.5f;
                float to = door.centerAlong + door.width * 0.5f;
                ColliderSlab(collision, layout, wall, $"{wall} collider {index++}", cursor, from, thickness, outer);

                // The lintel above the door keeps the player from stepping over the opening frame.
                if (room.height - door.height > 0.1f)
                {
                    ColliderSlab(collision, layout, wall, $"{wall} collider lintel {index}", from, to, thickness, outer,
                        door.height, room.height);
                }

                cursor = to;
            }

            ColliderSlab(collision, layout, wall, $"{wall} collider end", cursor, length + outer, thickness, outer);
        }

        private static void ColliderSlab(
            Transform collision,
            ClassroomLayout layout,
            string wall,
            string name,
            float alongFrom,
            float alongTo,
            float thickness,
            float outerOffset,
            float heightFrom = 0f,
            float heightTo = -1f)
        {
            if (alongTo - alongFrom <= 0.001f) return;

            RoomDimensions room = layout.room;
            float top = heightTo < 0f ? room.height : heightTo;
            float midHeight = (heightFrom + top) * 0.5f;
            float midAlong = (alongFrom + alongTo) * 0.5f;
            Vector3 position;
            Vector3 size;
            switch (wall)
            {
                case "back":
                    position = new Vector3(room.MinX + midAlong, midHeight, room.MaxZ + outerOffset);
                    size = new Vector3(alongTo - alongFrom, top - heightFrom, thickness);
                    break;
                case "front":
                    position = new Vector3(room.MinX + midAlong, midHeight, room.MinZ - outerOffset);
                    size = new Vector3(alongTo - alongFrom, top - heightFrom, thickness);
                    break;
                case "west":
                    position = new Vector3(room.MinX - outerOffset, midHeight, room.MinZ + midAlong);
                    size = new Vector3(thickness, top - heightFrom, alongTo - alongFrom);
                    break;
                case "east":
                    position = new Vector3(room.MaxX + outerOffset, midHeight, room.MinZ + midAlong);
                    size = new Vector3(thickness, top - heightFrom, alongTo - alongFrom);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(wall), wall, "Unknown wall.");
            }

            Box(name, collision, position, size, null);
        }

        // ------------------------------------------------------------- primitives

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, Quaternion? rotation = null)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            if (rotation.HasValue) box.transform.localRotation = rotation.Value;

            Renderer renderer = box.GetComponent<Renderer>();
            if (material != null) renderer.sharedMaterial = material;
            else renderer.enabled = false; // Collision-only boxes stay invisible but keep their collider.

            return box;
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

        /// <summary>Flat ring of thin quads, used for the floor circle and the mark on the board.</summary>
        private static GameObject Ring(string name, Transform parent, Vector3 center, float radius, float thickness, Material material, int segments, Quaternion rotation)
        {
            Transform ring = new GameObject(name).transform;
            ring.SetParent(parent, false);
            ring.localPosition = center;
            ring.localRotation = rotation;

            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float next = ((i + 1) / (float)segments) * Mathf.PI * 2f;
                Vector3 a = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector3 b = new Vector3(Mathf.Cos(next) * radius, 0f, Mathf.Sin(next) * radius);
                Vector3 mid = (a + b) * 0.5f;
                float length = Vector3.Distance(a, b) * 1.08f;
                float yaw = -Mathf.Atan2(b.z - a.z, b.x - a.x) * Mathf.Rad2Deg;
                Part(ring, "Segment " + i, PrimitiveType.Cube, mid, new Vector3(length, 0.006f, thickness), material, new Vector3(0f, yaw, 0f));
            }

            return ring.gameObject;
        }
    }
}
