using System;
using UnityEngine;

namespace PostSchool.World
{
    /// <summary>One rectangular mass: a block's floor slab, a roof, a parapet, or a hanging slab.</summary>
    [Serializable]
    public sealed class ShellBox
    {
        public string id;
        public string label;
        public string labelEn;
        public float minX;
        public float maxX;
        public float minZ;
        public float maxZ;

        /// <summary>Set for lifting slabs only; the block boxes get their height from the frame.</summary>
        public float y;
        public float thickness;

        /// <summary>The lifted slab's pitch, in degrees about Z. A degree and a half reads as a mistake.</summary>
        public float tiltDegrees;

        public int[] floors = Array.Empty<int>();

        /// <summary>Set on classroomFootprint: which floor the classroom occupies.</summary>
        public int storey;

        /// <summary>flat, canopy or open. Read by ShellRoofs, so a roof type cannot be silently dropped.</summary>
        public string roof;

        /// <summary>
        /// Which face carries the window bands: east, west, south, or null for a block without any. Named
        /// rather than assumed, because "the long face" is the wrong face twice on this L.
        /// </summary>
        public string windowFace;

        /// <summary>
        /// True on a block whose ground floor is a way through rather than a floor: the north gate's porch.
        /// It is a flag rather than a void because a void is a rectangle, and the thing being subtracted here
        /// is a floor, not a piece of one.
        /// </summary>
        public bool openGround;

        public string note;

        public float Width => maxX - minX;
        public float Depth => maxZ - minZ;
        public float CentreX => (minX + maxX) * 0.5f;
        public float CentreZ => (minZ + maxZ) * 0.5f;
    }

    /// <summary>
    /// A rectangle of the site that is not built on the storeys it names.
    ///
    /// It replaces only the part of a block it overlaps, so a storey can be built around it. Two of them exist:
    /// the classroom's own rectangle, taken out of the storey the room occupies, and the slot in front of the
    /// room's missing wall, which keeps that storey and the one below it clear of the drop down to the street.
    /// </summary>
    [Serializable]
    public sealed class ShellVoid
    {
        public string id;
        public string label;
        public string labelEn;
        public float minX;
        public float maxX;
        public float minZ;
        public float maxZ;
        public int[] floors = Array.Empty<int>();
        public string note;

        public float Width => maxX - minX;
        public float Depth => maxZ - minZ;
    }

    /// <summary>How one roof type is built. Sizes live in the file; the builder only reads them.</summary>
    [Serializable]
    public sealed class ShellRoofType
    {
        public float slabThickness;
        public float parapet;
        public float overhang;
        public float fascia;
        public float kerbHeight;
        public float kerbThickness;
        public string note;
    }

    [Serializable]
    public sealed class ShellRoofs
    {
        public string note;
        public ShellRoofType flat = new ShellRoofType();
        public ShellRoofType canopy = new ShellRoofType();
        public ShellRoofType open = new ShellRoofType();

        public ShellRoofType For(string type) =>
            type == "canopy" ? canopy : type == "open" ? open : flat;
    }

    /// <summary>One run of fins standing off a long face.</summary>
    [Serializable]
    public sealed class ShellLouvre
    {
        public string id;
        public string label;
        public string labelEn;
        public string wall;
        public string block;
        public ShellSpan along = new ShellSpan();
        public int count;
        public float projection;
        public float thickness;
        public ShellBand height = new ShellBand();
        public string note;

        /// <summary>True when the wall runs along x, so the fins are thin in x and tall in y.</summary>
        public bool RunsAlongX => along != null && along.axis == "x";
    }

    [Serializable]
    public sealed class ShellSpan
    {
        public float min;
        public float max;
        public string axis;
    }

    [Serializable]
    public sealed class ShellBand
    {
        public float from;
        public float to;
    }

    /// <summary>Where the window bands sit on a face, and how deep a reveal they are set into.</summary>
    [Serializable]
    public sealed class ShellWindows
    {
        public float sill;
        public float height;
        public float margin;
        public float glassFraction;
        public float reveal;
        public float glassDepth;
        public string note;
    }

    /// <summary>The north gate's opening: two piers and a lintel, sized off the porch itself.</summary>
    [Serializable]
    public sealed class ShellPorch
    {
        public float lintel;
        public float pierFraction;
        public float pierClearance;
        public string note;
    }

    /// <summary>The site as a grid of lines with no surface under it.</summary>
    [Serializable]
    public sealed class ShellGround
    {
        public float gridOriginX;
        public float gridOriginZ;
        public float gridExtentX;
        public float gridExtentZ;
        public float gridSpacing;
        public float gridThickness;
        /// <summary>How far the grid floats above the street, so it never z-fights the street's own plane.</summary>
        public float gridLift;
        /// <summary>The red mat's thickness: a mark laid on the ground, not a platform.</summary>
        public float matThickness;
        public ShellBox mat = new ShellBox();
        public string note;
    }

    [Serializable]
    public sealed class ShellObject
    {
        public string id;
        public string kind;
        public float x;
        public float z;
        public float yaw;
        public float height;
        public float length;
    }

    /// <summary>
    /// One thing the shell does that a building could not. Kept in the data rather than in comments,
    /// so the runtime can report it and a later reader can tell a decision from a bug.
    /// </summary>
    [Serializable]
    public sealed class ShellImpossibility
    {
        public string id;
        public string label;
        public string evidence;
        public string why;
    }

    /// <summary>
    /// The campus outside the classroom, to the owner's zone map.
    ///
    /// Shell and roof only. Nothing here is a room, a stair, a corridor you can walk or a door you can
    /// open; the massing exists so that the classroom sits inside a building instead of on a plane.
    /// The file is the single source of truth: the runtime builder, the offline proof and the tests all
    /// read it, so none of them can disagree about where a wall is.
    ///
    /// Coordinates are the classroom's own: +x east, +z north, y up with the classroom floor at 0 and
    /// the street 4.2 m below. See docs/CLASSROOM_ART.md.
    /// </summary>
    [Serializable]
    public sealed class CampusShellLayout
    {
        public string id;
        public string status;
        public string note;
        public ShellFrame frame = new ShellFrame();
        public ShellBox classroomFootprint = new ShellBox();
        public ShellPalette materials = new ShellPalette();
        public ShellBox[] blocks = Array.Empty<ShellBox>();
        public ShellVoid[] voids = Array.Empty<ShellVoid>();
        public ShellBox[] liftedSlabs = Array.Empty<ShellBox>();
        public ShellLouvre[] louvres = Array.Empty<ShellLouvre>();
        public ShellGround ground = new ShellGround();
        public ShellObject[] objects = Array.Empty<ShellObject>();
        public ShellWindows windows = new ShellWindows();
        public ShellPorch porch = new ShellPorch();
        public ShellRoofs roofs = new ShellRoofs();
        public ShellImpossibility[] impossibilities = Array.Empty<ShellImpossibility>();

        public static CampusShellLayout FromJson(string json, string sourceName)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException(
                    $"Campus shell '{sourceName}' is empty. Check the TextAsset at {sourceName}.json.", nameof(json));
            }

            CampusShellLayout layout;
            try
            {
                layout = JsonUtility.FromJson<CampusShellLayout>(json);
            }
            catch (ArgumentException jsonError)
            {
                throw new ArgumentException($"Campus shell '{sourceName}' is not valid JSON: {jsonError.Message}",
                    nameof(json), jsonError);
            }

            if (layout == null)
            {
                throw new ArgumentException($"Campus shell '{sourceName}' parsed into nothing.", nameof(json));
            }

            if (!layout.Validate(out string validationError))
            {
                throw new ArgumentException($"Campus shell '{sourceName}' is unusable: {validationError}", nameof(json));
            }

            return layout;
        }

        /// <summary>
        /// Checks what a hand edit is most likely to break: a floor count that disagrees with the frame,
        /// a slab with nothing holding it up that has quietly gained supports, and the classroom shaft
        /// being filled in — which would put solid concrete through the room's floor and ceiling.
        /// </summary>
        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                error = "Campus shell has no 'status' field; prototype massing must stay identifiable.";
                return false;
            }

            if (frame == null || frame.floorCount < 1 || frame.floorHeight < 2.6f || frame.slabThickness <= 0f)
            {
                error = "Frame is missing or unusable: needs floorCount >= 1, floorHeight >= 2.6 and slabThickness > 0.";
                return false;
            }

            if (frame.slabThickness >= frame.floorHeight)
            {
                error = "slabThickness is not smaller than floorHeight, so the slabs would have no voids above them.";
                return false;
            }

            if (blocks == null || blocks.Length == 0)
            {
                error = "The shell has no blocks to build.";
                return false;
            }

            foreach (ShellBox block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block.id))
                {
                    error = "Every block needs an id.";
                    return false;
                }

                if (block.maxX <= block.minX || block.maxZ <= block.minZ)
                {
                    error = $"Block '{block.id}' has no footprint.";
                    return false;
                }

                if (block.floors == null || block.floors.Length == 0)
                {
                    error = $"Block '{block.id}' has no floors.";
                    return false;
                }

                foreach (int floor in block.floors)
                {
                    if (floor < 0 || floor >= frame.floorCount)
                    {
                        error = $"Block '{block.id}' claims floor {floor}, outside the frame's {frame.floorCount} floors.";
                        return false;
                    }
                }
            }

            // The classroom's own storey has to be cut, and cut in the right place, or the shell is either
            // built straight through the room or the room ends up outside the building.
            ShellVoid opening = FindVoid("classroom_opening");
            if (opening == null)
            {
                error = "The classroom opening is not declared as a void; the shell would be built through the room.";
                return false;
            }

            if (Array.IndexOf(opening.floors, classroomFootprint.storey) < 0)
            {
                error = "The classroom opening does not include the storey the classroom is on.";
                return false;
            }

            if (!Covers(opening, classroomFootprint))
            {
                error = "The classroom opening is smaller than the room, so the shell would cut through its walls.";
                return false;
            }

            if (ContainsAnyBlock(opening, classroomFootprint.storey, out string insideOpening))
            {
                error = $"Block '{insideOpening}' is entirely inside the classroom opening, so that storey would vanish.";
                return false;
            }

            // The room's missing wall is the south one, and the drop below it has to reach the street, or the
            // fall onto the container ends on a slab of campus instead of on the container.
            if (FindVoid("fall_slot") == null)
            {
                error = "The slot in front of the classroom is not declared; the ground floor would fill in the fall.";
                return false;
            }

            foreach (ShellBox slab in liftedSlabs ?? Array.Empty<ShellBox>())
            {
                if (slab.thickness <= 0f)
                {
                    error = $"Lifted slab '{slab.id}' has no thickness.";
                    return false;
                }
            }

            if (roofs == null)
            {
                error = "The shell declares no roof types.";
                return false;
            }

            foreach (ShellBox block in blocks)
            {
                if (block.roof != "flat" && block.roof != "canopy" && block.roof != "open")
                {
                    error = $"Block '{block.id}' asks for roof '{block.roof}', which is not a declared type.";
                    return false;
                }

                ShellRoofType type = roofs.For(block.roof);
                if (block.roof == "open" ? type.kerbHeight <= 0f || type.kerbThickness <= 0f
                                          : type.slabThickness <= 0f)
                {
                    error = $"Roof type '{block.roof}' has no thickness, so '{block.id}' would be built roofless.";
                    return false;
                }
            }

            foreach (ShellLouvre louvre in louvres ?? Array.Empty<ShellLouvre>())
            {
                if (louvre.count < 2)
                {
                    error = $"Louvre run '{louvre.id}' needs at least two fins.";
                    return false;
                }

                if (louvre.along == null || louvre.along.max <= louvre.along.min)
                {
                    error = $"Louvre run '{louvre.id}' spans nothing.";
                    return false;
                }

                if (louvre.projection <= 0f || louvre.thickness <= 0f)
                {
                    error = $"Louvre run '{louvre.id}' needs a positive projection and thickness.";
                    return false;
                }

                if (Array.FindIndex(blocks, b => b.id == louvre.block) < 0)
                {
                    error = $"Louvre run '{louvre.id}' is fixed to '{louvre.block}', which is not a block.";
                    return false;
                }
            }

            if (ground == null || ground.gridSpacing <= 0f)
            {
                error = "The ground needs a positive grid spacing.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// The underside of a floor's structural slab, in the classroom frame: floor 0 stands on the street,
        /// floor 1 is the classroom's, and the room's own floor is 0, so the two datums agree by construction.
        /// </summary>
        public float FloorBase(int floor) => frame.streetY + floor * frame.floorHeight;

        /// <summary>The top of a floor's void, where the next slab starts.</summary>
        public float FloorTop(int floor) => FloorBase(floor) + frame.floorHeight - frame.slabThickness;

        public ShellBox Block(string id) => Array.Find(blocks, b => b.id == id);

        public ShellVoid FindVoid(string id) => Array.Find(voids ?? Array.Empty<ShellVoid>(), v => v.id == id);

        /// <summary>True when this floor of this block is taken out by a declared void.</summary>
        public bool IsVoided(ShellBox block, int floor, out ShellVoid gap)
        {
            foreach (ShellVoid candidate in voids ?? Array.Empty<ShellVoid>())
            {
                if (Array.IndexOf(candidate.floors, floor) >= 0 && Overlaps(candidate, block))
                {
                    gap = candidate;
                    return true;
                }
            }

            gap = null;
            return false;
        }

        private static bool Overlaps(ShellVoid gap, ShellBox box) =>
            box.minX < gap.maxX && box.maxX > gap.minX && box.minZ < gap.maxZ && box.maxZ > gap.minZ;

        /// <summary>True when the void swallows the whole box on that storey, which would delete it.</summary>
        private bool ContainsAnyBlock(ShellVoid gap, int floor, out string blockId)
        {
            foreach (ShellBox block in blocks)
            {
                if (Array.IndexOf(block.floors, floor) < 0) continue;
                if (!Overlaps(gap, block)) continue;
                bool swallows = gap.minX <= block.minX && gap.maxX >= block.maxX
                                && gap.minZ <= block.minZ && gap.maxZ >= block.maxZ;
                if (swallows)
                {
                    blockId = block.id;
                    return true;
                }
            }

            blockId = null;
            return false;
        }

        private bool Covers(ShellVoid gap, ShellBox room) =>
            gap.minX <= room.minX && gap.maxX >= room.maxX
            && gap.minZ <= room.minZ && gap.maxZ >= room.maxZ;
    }

    [Serializable]
    public sealed class ShellFrame
    {
        public float classroomFloorY;
        public float streetY;
        public float floorHeight;
        public int floorCount;
        public float slabThickness;
        public string note;
    }

    /// <summary>
    /// The shell's colours. Deliberately separate from ClassroomArtPalette: the room is warm paper and
    /// the building it sits in is cold concrete, and the two must not bleed into each other.
    /// </summary>
    [Serializable]
    public sealed class ShellPalette
    {
        public string note;
        public string concrete;
        public string concreteDeep;
        public string concreteShadow;
        public string concretePale;
        public string steel;
        public string steelDark;
        public string glass;
        public string @void;
        public string grid;
        public string red;
        public string ochre;
        public string paper;

        public Color Concrete => ClassroomArtPalette.Hex(concrete);
        public Color ConcreteDeep => ClassroomArtPalette.Hex(concreteDeep);
        public Color ConcreteShadow => ClassroomArtPalette.Hex(concreteShadow);
        public Color ConcretePale => ClassroomArtPalette.Hex(concretePale);
        public Color Steel => ClassroomArtPalette.Hex(steel);
        public Color SteelDark => ClassroomArtPalette.Hex(steelDark);
        public Color Glass => ClassroomArtPalette.Hex(glass);
        public Color Void => ClassroomArtPalette.Hex(@void);
        public Color Grid => ClassroomArtPalette.Hex(grid);
        public Color Red => ClassroomArtPalette.Hex(red);
        public Color Ochre => ClassroomArtPalette.Hex(ochre);
        public Color Paper => ClassroomArtPalette.Hex(paper);
    }
}
