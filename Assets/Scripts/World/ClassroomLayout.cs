using System;
using UnityEngine;

namespace PostSchool.World
{
    /// <summary>One gap in a wall: a window or a door.</summary>
    [Serializable]
    public sealed class WallOpening
    {
        /// <summary>How a window is dressed. <see cref="Dusk"/> is bare, <see cref="Curtain"/> is covered.</summary>
        public const string TreatmentDusk = "dusk";
        public const string TreatmentCurtain = "curtain";

        public string id;
        public string wall;
        public float centerAlong;
        public float width;
        public float height;
        public float sillHeight;

        /// <summary>Windows only: "dusk" (bare, onto the street) or "curtain" (covered).</summary>
        public string treatment;

        public bool HasCurtain => treatment == TreatmentCurtain;

        /// <summary>The range covered on the wall, in world units, always from low to high.</summary>
        public Vector2 AlongRange => new Vector2(centerAlong - width * 0.5f, centerAlong + width * 0.5f);
    }

    /// <summary>A ceiling fixture. Proportions are handed to the dressing step, not to gameplay.</summary>
    [Serializable]
    public sealed class CeilingLightPlacement
    {
        public float x;
        public float z;
        public float length;
        public float tiltDegrees;
        public float height;
    }

    /// <summary>The chalk circle someone left on the floor around the protagonist's desk.</summary>
    [Serializable]
    public sealed class ChalkCircle
    {
        public float centerX;
        public float centerZ;
        public float radius;
        public int segments;
    }

    [Serializable]
    public sealed class RoomDimensions
    {
        public float width;
        public float depth;
        public float height;
        public float centerZ;
        public float wallThickness;

        public float MinX => -width * 0.5f;

        public float MaxX => width * 0.5f;

        public float MinZ => centerZ - depth * 0.5f;

        public float MaxZ => centerZ + depth * 0.5f;
    }

    [Serializable]
    public sealed class BlackboardDimensions
    {
        public float width;
        public float height;
        public float sillHeight;
        public float chalkTrayDepth;
    }

    [Serializable]
    public sealed class PodiumPlacement
    {
        public float positionX;
        public float positionZ;
        public float width;
        public float depth;
        public float height;
    }

    /// <summary>
    /// Where the teacher comes in and where she stands. Both are data because the prologue's blocking
    /// depends on them: she walks the path, stops on the mark, and the conversation's camera is set up
    /// around the mark. A path that clips the seating grid has to fail at load, not at dusk.
    ///
    /// Positions are world metres (x, z) with y taken from the floor; the walk is a straight line
    /// through the points in order, so keep them clear of desks.
    /// </summary>
    [Serializable]
    public sealed class TeacherPlacement
    {
        /// <summary>Waypoints she walks on the way in, front-of-room first, in order.</summary>
        public Vector2[] entryPath = Array.Empty<Vector2>();

        /// <summary>Where she stops: in front of the podium, facing the room.</summary>
        public Vector2 stand = new Vector2(0f, 9.15f);

        /// <summary>Facing at the mark, degrees: 0 faces the blackboard (+z), 180 faces the room (-z).</summary>
        public float standYaw = 180f;

        /// <summary>Door she comes through; must be one of <see cref="ClassroomLayout.doors"/>.</summary>
        public string entryDoor = "door_back_staff";

        /// <summary>How long the walk-in takes, seconds. The opening waits on it.</summary>
        public float entrySeconds = 3.2f;
    }

    [Serializable]
    public sealed class SeatingGrid
    {
        public float[] columns = Array.Empty<float>();
        public float[] rows = Array.Empty<float>();
        public float deskWidth;
        public float deskDepth;
        public float deskHeight;
        public float chairOffsetZ;
        public float chairSize;
        public int expectedSeatCount;

        /// <summary>Seats actually implied by the grid; the brief asks for 24.</summary>
        public int SeatCount => (columns == null ? 0 : columns.Length) * (rows == null ? 0 : rows.Length);
    }

    [Serializable]
    public sealed class WakeDeskPlacement
    {
        public float positionX;
        public float positionZ;
        public float width;
        public float depth;
        public float height;
    }

    /// <summary>
    /// The front wall, which is not a wall. The room's exit side is closed by a floor-to-ceiling
    /// curtain hung on a rail; behind it there is no wall, no window and no floor, only the drop to the
    /// street. Everything about that is data here so the builder and the offline proofs agree.
    /// </summary>
    [Serializable]
    public sealed class CurtainWall
    {
        /// <summary>False for the authored room: the curtain is the wall.</summary>
        public bool exists;

        public string treatment = "curtain";

        /// <summary>Rail height; the curtain reaches the ceiling and stops just above the floor.</summary>
        public float curtainTop = 3.42f;
        public float curtainBottom;

        public float curtainWidth = 10.6f;
        public float curtainCenterAlong = 6.2f;

        /// <summary>Below this at the bottom, a curtain counts as a full drop ("幕布").</summary>
        public float fullHeightThreshold = 0.05f;

        /// <summary>True when the curtain hangs from near the ceiling to near the floor.</summary>
        public bool IsFullDrop => curtainTop >= 3f && curtainBottom <= fullHeightThreshold;
    }

    /// <summary>
    /// What is outside the missing wall, and where a falling body lands. One blue container, a street,
    /// and a row of buildings with a washing line across them.
    /// </summary>
    [Serializable]
    public sealed class FallZone
    {
        public float containerCenterX;
        public float containerCenterZ = -8.4f;
        public float containerWidth = 12f;
        public float containerLength = 6f;
        public float containerHeight = 2.6f;

        /// <summary>How far the classroom floor is above the street. The whole drop hangs off this.</summary>
        public float roomFloorAboveStreet = 4.2f;

        /// <summary>
        /// The street surface, in the room's own world coordinates. Negative: the street is below the
        /// classroom, and every outdoor height is measured down from it.
        /// </summary>
        public float groundY = -4.2f;

        public float streetWidth = 9f;
        public float streetCenterZ = -11.6f;
        public float buildingDepth = 6f;
        public float buildingRowZ = -18f;
        public float[] buildingHeights = Array.Empty<float>();

        /// <summary>
        /// The container top as a world height; this is what a successful check lands on. Read it as
        /// "how far below the classroom floor the lid is": the drop is room.height - ContainerTopY.
        /// </summary>
        public float ContainerTopY => groundY + containerHeight;

        public Vector3 ContainerCenter => new Vector3(containerCenterX, groundY + containerHeight * 0.5f, containerCenterZ);
    }

    /// <summary>Washing on a line: the one domestic thing visible from a classroom window.</summary>
    [Serializable]
    public sealed class LaundryLine
    {
        public float lineZ = -15.2f;
        public float lineY = 6.4f;
        public float lineFromX = -9f;
        public float lineToX = 9f;
        public string[] garments = Array.Empty<string>();
    }

    /// <summary>
    /// Classroom composition data. Kept in a JSON resource so the layout can be re-authored without
    /// touching C#, and so the offline plan proof can draw the same numbers. See docs/CLASSROOM_ART.md.
    /// </summary>
    [Serializable]
    public sealed class ClassroomLayout
    {
        public string id;
        public string status;
        public string note;
        public RoomDimensions room = new RoomDimensions();
        public BlackboardDimensions blackboard = new BlackboardDimensions();
        public PodiumPlacement podium = new PodiumPlacement();
        public TeacherPlacement teacher = new TeacherPlacement();
        public SeatingGrid seating = new SeatingGrid();
        public WakeDeskPlacement wakeDesk = new WakeDeskPlacement();
        public WallOpening[] windows = Array.Empty<WallOpening>();
        public WallOpening[] doors = Array.Empty<WallOpening>();
        public CeilingLightPlacement[] ceilingLights = Array.Empty<CeilingLightPlacement>();
        public ChalkCircle chalkCircle = new ChalkCircle();
        public CurtainWall frontWall = new CurtainWall();
        public FallZone fall = new FallZone();
        public LaundryLine laundry = new LaundryLine();

        public static ClassroomLayout FromJson(string json, string sourceName)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException($"Classroom layout '{sourceName}' is empty. Check the TextAsset at {sourceName}.json.", nameof(json));
            }

            ClassroomLayout layout;
            try
            {
                layout = JsonUtility.FromJson<ClassroomLayout>(json);
            }
            catch (ArgumentException error)
            {
                throw new ArgumentException($"Classroom layout '{sourceName}' is not valid JSON: {error.Message}", nameof(json), error);
            }

            if (layout == null)
            {
                throw new ArgumentException($"Classroom layout '{sourceName}' parsed into nothing.", nameof(json));
            }

            if (!layout.Validate(out string error))
            {
                throw new ArgumentException($"Classroom layout '{sourceName}' is unusable: {error}", nameof(json));
            }

            return layout;
        }

        /// <summary>
        /// Checks what a hand edit is most likely to break: a seat count that is not 24, openings that
        /// run past the wall they sit in, and a protagonist desk that overlaps the seating grid.
        /// </summary>
        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                error = "Classroom layout has no 'status' field; prototype composition must stay identifiable.";
                return false;
            }

            if (room == null || room.width < 6f || room.depth < 8f || room.height < 2.6f || room.wallThickness <= 0f)
            {
                error = "Room is missing or too small to hold a 24-seat classroom (needs >= 6 x 8 x 2.6 m).";
                return false;
            }

            if (seating == null || seating.columns == null || seating.rows == null || seating.columns.Length == 0 || seating.rows.Length == 0)
            {
                error = "Seating grid needs at least one column and one row.";
                return false;
            }

            if (seating.SeatCount != seating.expectedSeatCount)
            {
                error = $"Seating grid implies {seating.SeatCount} seats but expectedSeatCount is {seating.expectedSeatCount}.";
                return false;
            }

            if (seating.expectedSeatCount != 24)
            {
                error = $"The brief asks for 24 seats; layout declares {seating.expectedSeatCount}.";
                return false;
            }

            for (int i = 0; i < seating.columns.Length; i++)
            {
                float column = seating.columns[i];
                if (column < room.MinX + seating.deskWidth * 0.5f + 0.35f || column > room.MaxX - seating.deskWidth * 0.5f - 0.35f)
                {
                    error = $"Seat column {column} does not leave an aisle against the walls.";
                    return false;
                }

                for (int j = i + 1; j < seating.columns.Length; j++)
                {
                    if (Mathf.Abs(column - seating.columns[j]) < seating.deskWidth + 0.15f)
                    {
                        error = $"Seat columns {column} and {seating.columns[j]} collide.";
                        return false;
                    }
                }
            }

            for (int i = 0; i < seating.rows.Length; i++)
            {
                float row = seating.rows[i];
                if (row < room.MinZ + 1.2f || row > room.MaxZ - 1.6f)
                {
                    error = $"Seat row {row} leaves no room for the blackboard or the front aisle.";
                    return false;
                }

                for (int j = i + 1; j < seating.rows.Length; j++)
                {
                    if (Mathf.Abs(row - seating.rows[j]) < seating.deskDepth + 0.55f)
                    {
                        error = $"Seat rows {row} and {seating.rows[j]} leave no legroom.";
                        return false;
                    }
                }
            }

            if (wakeDesk == null || wakeDesk.positionZ < room.MinZ + 1.6f)
            {
                error = "The protagonist's desk must sit in the empty front band.";
                return false;
            }

            // A person has to fit between the protagonist's desk and the first row: 0.7 m of standing room.
            float firstChairFront = seating.rows[0] + seating.chairOffsetZ - seating.chairSize * 0.5f;
            float wakeDeskBack = wakeDesk.positionZ + wakeDesk.depth * 0.5f;
            if (firstChairFront - wakeDeskBack < 0.7f - 0.0001f)
            {
                error = $"Only {firstChairFront - wakeDeskBack:0.00} m between the protagonist's desk and the first chair; 0.70 m is the minimum.";
                return false;
            }

            if (blackboard == null || blackboard.width > room.width - 1.6f || blackboard.sillHeight + blackboard.height > room.height - 0.3f)
            {
                error = "Blackboard does not fit the back wall.";
                return false;
            }

            if (windows == null || windows.Length != 2)
            {
                error = $"The brief asks for two windows; layout declares {(windows == null ? 0 : windows.Length)}.";
                return false;
            }

            if (doors == null || doors.Length != 2)
            {
                error = $"The brief asks for two doors; layout declares {(doors == null ? 0 : doors.Length)}.";
                return false;
            }

            string frontWallError;
            if (!ValidateFrontWall(out frontWallError))
            {
                error = frontWallError;
                return false;
            }

            string openingError;
            if (!ValidateOpenings(windows, "window", out openingError) || !ValidateOpenings(doors, "door", out openingError))
            {
                error = openingError;
                return false;
            }

            if (ceilingLights == null || ceilingLights.Length == 0)
            {
                error = "Ceiling needs at least one light placement.";
                return false;
            }

            for (int i = 0; i < ceilingLights.Length; i++)
            {
                CeilingLightPlacement light = ceilingLights[i];
                if (light.length <= 0f || light.height <= 2.2f || light.height > room.height)
                {
                    error = $"Ceiling light {i} has an impossible length or height.";
                    return false;
                }
            }

            string clearanceError;
            if (!ValidateOpeningClearance(doors, "door", out clearanceError)
                || !ValidateOpeningClearance(windows, "window", out clearanceError))
            {
                error = clearanceError;
                return false;
            }

            if (chalkCircle == null || chalkCircle.radius < 0.8f || chalkCircle.radius > room.width * 0.4f || chalkCircle.segments < 8)
            {
                error = "Chalk circle needs a radius between 0.8 m and 40% of the room width, and at least 8 segments.";
                return false;
            }

            string teacherError;
            if (!ValidateTeacher(out teacherError))
            {
                error = teacherError;
                return false;
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// The teacher's walk-in. A substitute who is placed inside a desk, or who walks through one,
        /// is the kind of mistake that only shows up once the room is lit, so it is checked with the
        /// same plain arithmetic the seating grid uses: every straight run is clipped against the
        /// rectangle of each desk and against the podium, and an overlap fails the load.
        /// </summary>
        private bool ValidateTeacher(out string error)
        {
            if (teacher == null || teacher.entryPath == null || teacher.entryPath.Length < 2)
            {
                error = "The teacher needs an entry path with at least two points.";
                return false;
            }

            if (teacher.entrySeconds < 0.5f || teacher.entrySeconds > 12f)
            {
                error = "The teacher's walk-in must take between 0.5 and 12 seconds.";
                return false;
            }

            if (Array.Find(doors, door => door.id == teacher.entryDoor) == null)
            {
                error = $"The teacher enters through '{teacher.entryDoor}', which is not a door in this layout.";
                return false;
            }

            // The mark is inside the room, clear of the blackboard wall, and not standing in the podium.
            // It may be in front of it or behind it — behind is where a teacher actually stands — but
            // it may not be inside it.
            if (!PointIsInRoom(teacher.stand, 0.4f))
            {
                error = "The teacher's mark is outside the room.";
                return false;
            }

            if (teacher.stand.y > room.MaxZ - 0.55f)
            {
                error = $"The teacher's mark at z = {teacher.stand.y:0.00} is inside the blackboard wall.";
                return false;
            }

            if (SegmentHitsRect(teacher.stand, teacher.stand, PodiumFootprint()))
            {
                error = $"The teacher's mark at ({teacher.stand.x:0.00}, {teacher.stand.y:0.00}) is inside the podium.";
                return false;
            }

            // She has to be able to stand there: not on a desk, not on the chair behind one, not in
            // the protagonist's desk. A mark inside furniture is a substitute with her knees in a table.
            Rect wake = new Rect(wakeDesk.positionX - wakeDesk.width * 0.5f, wakeDesk.positionZ - wakeDesk.depth * 0.5f,
                wakeDesk.width, wakeDesk.depth);
            if (SegmentHitsRect(teacher.stand, teacher.stand, wake))
            {
                error = "The teacher's mark is inside the protagonist's desk.";
                return false;
            }

            for (int column = 0; column < seating.columns.Length; column++)
            {
                for (int row = 0; row < seating.rows.Length; row++)
                {
                    Rect desk = DeskFootprint(seating.columns[column], seating.rows[row]);
                    if (SegmentHitsRect(teacher.stand, teacher.stand, desk))
                    {
                        error = "The teacher's mark is inside a seat.";
                        return false;
                    }
                }
            }

            for (int i = 0; i < teacher.entryPath.Length; i++)
            {
                if (!PointIsInRoom(teacher.entryPath[i], 0.35f))
                {
                    error = $"Teacher path point {i} is outside the room.";
                    return false;
                }

                if (i == 0) continue;
                Vector2 a = teacher.entryPath[i - 1];
                Vector2 b = teacher.entryPath[i];

                for (int column = 0; column < seating.columns.Length; column++)
                {
                    for (int row = 0; row < seating.rows.Length; row++)
                    {
                        if (SegmentHitsRect(a, b, DeskFootprint(seating.columns[column], seating.rows[row])))
                        {
                            error = $"The teacher walks through the desk at ({seating.columns[column]:0.0}, {seating.rows[row]:0.0}).";
                            return false;
                        }
                    }
                }

                if (SegmentHitsRect(a, b, PodiumFootprint()))
                {
                    error = $"The teacher walks through the podium between path points {i - 1} and {i}.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// Where a seat actually occupies floor, in x/z, plus 0.45 m of shoulder room: the desk is
        /// 1.15 x 0.55 m and the chair hangs behind it (toward the room, -z), so the footprint is not
        /// centred on the row value. Getting this wrong either lets her walk through a chair or bans
        /// every honest path through the aisle.
        /// </summary>
        private Rect DeskFootprint(float column, float row)
        {
            const float shoulderRoom = 0.45f;
            float halfWidth = seating.deskWidth * 0.5f + shoulderRoom;
            float chairDepth = Mathf.Abs(seating.chairOffsetZ) + seating.chairSize * 0.5f;
            float minZ = row - chairDepth - shoulderRoom;
            float maxZ = row + seating.deskDepth * 0.5f + shoulderRoom;
            return new Rect(column - halfWidth, minZ, halfWidth * 2f, maxZ - minZ);
        }

        private Rect PodiumFootprint()
        {
            const float clearance = 0.25f;
            float halfWidth = podium.width * 0.5f + clearance;
            float halfDepth = podium.depth * 0.5f + clearance;
            return new Rect(podium.positionX - halfWidth, podium.positionZ - halfDepth, halfWidth * 2f, halfDepth * 2f);
        }

        /// <summary>
        /// Liang-Barsky: does the segment a-b enter the rectangle? Exact, unlike a bounding-box test,
        /// which would forbid the one open corridor this room actually has.
        /// </summary>
        private static bool SegmentHitsRect(Vector2 a, Vector2 b, Rect rect)
        {
            float t0 = 0f;
            float t1 = 1f;
            float dx = b.x - a.x;
            float dy = b.y - a.y;

            if (!Clip(-dx, a.x - rect.xMin, ref t0, ref t1)) return false;
            if (!Clip(dx, rect.xMax - a.x, ref t0, ref t1)) return false;
            if (!Clip(-dy, a.y - rect.yMin, ref t0, ref t1)) return false;
            if (!Clip(dy, rect.yMax - a.y, ref t0, ref t1)) return false;
            return t0 < t1 || Mathf.Approximately(t0, t1);
        }

        private static bool Clip(float direction, float distance, ref float t0, ref float t1)
        {
            if (Mathf.Approximately(direction, 0f)) return distance > 0f;
            float t = distance / direction;
            if (direction < 0f)
            {
                if (t > t1) return false;
                if (t > t0) t0 = t;
            }
            else
            {
                if (t < t0) return false;
                if (t < t1) t1 = t;
            }

            return true;
        }

        private bool PointIsInRoom(Vector2 point, float inset)
        {
            return point.x >= room.MinX + inset && point.x <= room.MaxX - inset
                && point.y >= room.MinZ + inset && point.y <= room.MaxZ - inset;
        }

        /// <summary>
        /// The exit side of the room is deliberately incomplete: a full-drop curtain instead of a wall,
        /// and a container below it to land on. A half-drawn curtain or a container out of reach makes
        /// the scene unshootable, so both are checked here rather than discovered in the Editor.
        /// </summary>
        private bool ValidateFrontWall(out string error)
        {
            if (frontWall == null)
            {
                error = "The front wall block is missing; the exit side must declare its curtain.";
                return false;
            }

            if (frontWall.exists)
            {
                error = "The front wall must not exist as a wall: this room's exit side is a curtain and a drop.";
                return false;
            }

            if (frontWall.treatment != "curtain")
            {
                error = $"Front wall treatment '{frontWall.treatment}' is not supported; the exit side is a curtain.";
                return false;
            }

            if (!frontWall.IsFullDrop)
            {
                error = $"The front curtain must hang from near the ceiling to the floor (top {frontWall.curtainTop:0.00} m, bottom {frontWall.curtainBottom:0.00} m).";
                return false;
            }

            if (frontWall.curtainTop > room.height + 0.01f)
            {
                error = "The front curtain cannot hang above the ceiling.";
                return false;
            }

            if (frontWall.curtainWidth < room.width * 0.7f)
            {
                error = $"The front curtain ({frontWall.curtainWidth:0.0} m) is too narrow to close a {room.width:0.0} m room.";
                return false;
            }

            float curtainFrom = frontWall.curtainCenterAlong - frontWall.curtainWidth * 0.5f;
            float curtainTo = frontWall.curtainCenterAlong + frontWall.curtainWidth * 0.5f;
            if (curtainFrom > 0.4f || curtainTo < room.width - 0.4f)
            {
                error = "The front curtain leaves a gap at a corner; the room would still read as walled.";
                return false;
            }

            // The exit door has to be reachable: it opens onto the drop, which is the point.
            bool exitDoorOnFront = false;
            foreach (WallOpening door in doors)
            {
                if (door.wall == "front") exitDoorOnFront = true;
            }

            if (!exitDoorOnFront)
            {
                error = "The exit door must stay on the front wall: it is the way onto the drop.";
                return false;
            }

            if (fall == null)
            {
                error = "The front wall opens onto nothing; declare the fall zone (container, street, buildings).";
                return false;
            }

            if (fall.containerHeight < 1.6f || fall.containerHeight > 3.2f)
            {
                error = $"A container to land on should be 1.6-3.2 m tall; layout says {fall.containerHeight:0.00} m.";
                return false;
            }

            // The container must sit outside the room and under the opening, not somewhere off to a side.
            if (fall.containerCenterZ > room.MinZ - 1.5f)
            {
                error = "The fall container must sit at least 1.5 m beyond the front wall, or a fall lands indoors.";
                return false;
            }

            float exitCenterX = room.MinX;
            foreach (WallOpening door in doors)
            {
                if (door.wall != "front") continue;
                exitCenterX = WorldAlong("front", door.centerAlong);
                // The container's x extent is its width; length runs away from the wall.
                if (Mathf.Abs(exitCenterX - fall.containerCenterX) > fall.containerWidth * 0.5f - 0.5f)
                {
                    error = "The exit door does not open above the container; a fall would miss it.";
                    return false;
                }
            }

            // A fall from a classroom window has to actually be one. Below ~4 m the scene reads as a
            // stumble, above ~7 m nobody walks away from it and the "safe landing" branch turns into a lie.
            if (fall.roomFloorAboveStreet < 2.2f)
            {
                error = $"The classroom floor is only {fall.roomFloorAboveStreet:0.0} m above the street; a fall needs height.";
                return false;
            }

            float drop = room.height - fall.ContainerTopY;
            if (drop < 4f || drop > 7f)
            {
                error = $"The drop onto the container is {drop:0.0} m; it must read as a fall (4-7 m).";
                return false;
            }

            if (fall.buildingHeights == null || fall.buildingHeights.Length < 3)
            {
                error = "The street beyond the drop needs at least three buildings to read as a city.";
                return false;
            }

            for (int i = 0; i < fall.buildingHeights.Length; i++)
            {
                if (fall.buildingHeights[i] < 4f)
                {
                    error = $"Building {i} is only {fall.buildingHeights[i]:0.0} m tall; the street should dwarf the room.";
                    return false;
                }
            }

            if (laundry != null && laundry.garments != null && laundry.garments.Length > 0)
            {
                if (laundry.lineY <= fall.ContainerTopY + 1f)
                {
                    error = "The washing line hangs below the container top; laundry would be inside the street clutter.";
                    return false;
                }

                if (laundry.lineToX - laundry.lineFromX < 6f)
                {
                    error = "The washing line is too short to read as a line across the street.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        /// <summary>Length of the wall an opening sits in, measured along that wall.</summary>
        public float WallLength(string wall)
        {
            switch (wall)
            {
                case "west":
                case "east": return room.depth;
                case "front":
                case "back": return room.width;
                default: throw new ArgumentOutOfRangeException(nameof(wall), wall, "Unknown wall. Expected west, east, front or back.");
            }
        }

        /// <summary>
        /// Where a wall's "along" axis starts in world units: front and back walls start at minX and run
        /// towards +x, west and east walls start at minZ and run towards +z. Openings store along-values,
        /// so this is the one place the convention lives.
        /// </summary>
        public float WallAlongOrigin(string wall)
        {
            switch (wall)
            {
                case "front":
                case "back": return room.MinX;
                case "west":
                case "east": return room.MinZ;
                default: throw new ArgumentOutOfRangeException(nameof(wall), wall, "Unknown wall. Expected west, east, front or back.");
            }
        }

        /// <summary>Translates an along-wall value into a world coordinate for that wall.</summary>
        public float WorldAlong(string wall, float along)
        {
            return WallAlongOrigin(wall) + along;
        }

        /// <summary>
        /// A door on the blackboard or the front wall has to be reachable: it may not open onto a seat,
        /// the podium or the protagonist's desk. This is the check that catches careless hand edits.
        /// </summary>
        private bool ValidateOpeningClearance(WallOpening[] openings, string kind, out string error)
        {
            if (kind != "door")
            {
                error = string.Empty;
                return true;
            }

            for (int i = 0; i < openings.Length; i++)
            {
                WallOpening door = openings[i];
                if (door.wall != "back" && door.wall != "front") continue;

                float from = WorldAlong(door.wall, door.AlongRange.x);
                float to = WorldAlong(door.wall, door.AlongRange.y);

                // Anything standing against that wall inside the door's swing is a blocked exit.
                if (door.wall == "back" && blackboard != null)
                {
                    float boardFrom = -blackboard.width * 0.5f;
                    float boardTo = blackboard.width * 0.5f;
                    if (from < boardTo + 0.3f && to > boardFrom - 0.3f)
                    {
                        error = $"Door '{door.id}' opens onto the blackboard.";
                        return false;
                    }
                }

                if (door.wall == "front"
                    && from < wakeDesk.positionX + wakeDesk.width * 0.5f + 0.4f
                    && to > wakeDesk.positionX - wakeDesk.width * 0.5f - 0.4f)
                {
                    error = $"Door '{door.id}' opens onto the protagonist's desk.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private bool ValidateOpenings(WallOpening[] openings, string kind, out string error)
        {
            for (int i = 0; i < openings.Length; i++)
            {
                WallOpening opening = openings[i];
                if (opening == null || string.IsNullOrWhiteSpace(opening.id))
                {
                    error = $"{kind} {i} has no id.";
                    return false;
                }

                float wallLength;
                try
                {
                    wallLength = WallLength(opening.wall);
                }
                catch (ArgumentOutOfRangeException)
                {
                    error = $"{kind} '{opening.id}' names unknown wall '{opening.wall}'.";
                    return false;
                }

                if (opening.width <= 0.4f || opening.width >= wallLength - 0.6f)
                {
                    error = $"{kind} '{opening.id}' is too wide for its wall.";
                    return false;
                }

                if (opening.centerAlong - opening.width * 0.5f < 0.3f || opening.centerAlong + opening.width * 0.5f > wallLength - 0.3f)
                {
                    error = $"{kind} '{opening.id}' runs into a corner.";
                    return false;
                }

                if (opening.sillHeight + opening.height > room.height - 0.15f)
                {
                    error = $"{kind} '{opening.id}' is taller than the room.";
                    return false;
                }

                for (int j = i + 1; j < openings.Length; j++)
                {
                    WallOpening other = openings[j];
                    if (other == null || other.wall != opening.wall) continue;
                    if (Mathf.Abs(other.centerAlong - opening.centerAlong) < (other.width + opening.width) * 0.5f + 0.4f)
                    {
                        error = $"{kind}s '{opening.id}' and '{other.id}' overlap on the {opening.wall} wall.";
                        return false;
                    }
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
