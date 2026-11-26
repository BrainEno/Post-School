using System;
using UnityEngine;

namespace PostSchool.World
{
    /// <summary>One gap in a wall: a window or a door.</summary>
    [Serializable]
    public sealed class WallOpening
    {
        public string id;
        public string wall;
        public float centerAlong;
        public float width;
        public float height;
        public float sillHeight;

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
        public SeatingGrid seating = new SeatingGrid();
        public WakeDeskPlacement wakeDesk = new WakeDeskPlacement();
        public WallOpening[] windows = Array.Empty<WallOpening>();
        public WallOpening[] doors = Array.Empty<WallOpening>();
        public CeilingLightPlacement[] ceilingLights = Array.Empty<CeilingLightPlacement>();
        public ChalkCircle chalkCircle = new ChalkCircle();

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
