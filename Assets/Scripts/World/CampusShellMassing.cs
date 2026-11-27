using System;
using System.Collections.Generic;
using UnityEngine;

namespace PostSchool.World
{
    /// <summary>One rectangular piece of scenery: a piece of a block's slab, a fin, a roof, a kerb.</summary>
    public struct ShellPiece
    {
        public string name;
        public ShellBox footprint;
        public float y0;
        public float y1;
        public string role;

        public static ShellPiece Of(string name, float minX, float maxX, float minZ, float maxZ,
                                    float y0, float y1, string role) =>
            new ShellPiece
            {
                name = name,
                footprint = new ShellBox { minX = minX, maxX = maxX, minZ = minZ, maxZ = maxZ },
                y0 = y0,
                y1 = y1,
                role = role,
            };
    }

    /// <summary>
    /// Turns the layout into the rectangles that get built. Nothing here touches Unity objects, so the
    /// offline proof (tools/campus_shell_proof.py) can mirror it exactly and the tests can compare the two.
    ///
    /// Two things this decides that a plain box-per-block cannot:
    ///
    ///   * A void does not delete a storey. It takes a rectangle out of it and leaves the rest, so the storey
    ///     the classroom is on is still built either side of the room. Deleting the whole storey (what the
    ///     first version did) left the middle floor of the south arm missing and made the room a hole in the
    ///     building rather than a room inside it.
    ///   * A roof type is built as declared. Before this, every roof was a slab plus a too-tall parapet and the
    ///     'roof' field in the data was decoration: the gate's canopy and the links' open decks both came out
    ///     as flat roofs.
    ///
    /// The cut runs on a grid whose lines are the block's edges, the void's edges and any fins sitting on top,
    /// so the exact coordinates of a hand edit survive into the geometry instead of being rounded to a bay.
    /// </summary>
    public static class CampusShellMassing
    {
        /// <summary>A piece of a slab is dropped when it is thinner than this in either direction.</summary>
        public const float MinimumPiece = 0.01f;

        public static List<ShellPiece> BlockPieces(CampusShellLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));

            var pieces = new List<ShellPiece>();
            foreach (ShellBox block in layout.blocks)
            {
                foreach (int floor in block.floors)
                {
                    if (block.openGround && floor == 0) continue;

                    List<Rectangle> parts = Carve(layout, block, floor);
                    if (parts.Count == 0) continue;

                    float y0 = layout.FloorBase(floor);
                    float y1 = y0 + layout.frame.floorHeight - layout.frame.slabThickness;
                    for (int i = 0; i < parts.Count; i++)
                    {
                        Rectangle part = parts[i];
                        pieces.Add(ShellPiece.Of($"{block.id} floor {floor} part {i}",
                            part.minX, part.maxX, part.minZ, part.maxZ, y0, y1, "concrete"));
                    }
                }
            }

            return pieces;
        }

        /// <summary>The pieces of one block's storey, with every void already taken out of it.</summary>
        public static List<Rectangle> Carve(CampusShellLayout layout, ShellBox block, int floor)
        {
            var voids = new List<ShellVoid>();
            foreach (ShellVoid gap in layout.voids ?? Array.Empty<ShellVoid>())
            {
                if (Array.IndexOf(gap.floors, floor) < 0) continue;
                if (gap.minX >= block.maxX || gap.maxX <= block.minX) continue;
                if (gap.minZ >= block.maxZ || gap.maxZ <= block.minZ) continue;
                voids.Add(gap);
            }

            var xs = new List<float>(6);
            var zs = new List<float>(6);
            xs.Add(block.minX);
            xs.Add(block.maxX);
            zs.Add(block.minZ);
            zs.Add(block.maxZ);
            foreach (ShellVoid gap in voids)
            {
                xs.Add(Mathf.Clamp(gap.minX, block.minX, block.maxX));
                xs.Add(Mathf.Clamp(gap.maxX, block.minX, block.maxX));
                zs.Add(Mathf.Clamp(gap.minZ, block.minZ, block.maxZ));
                zs.Add(Mathf.Clamp(gap.maxZ, block.minZ, block.maxZ));
            }

            xs.Sort();
            zs.Sort();

            var cells = new List<Rectangle>();
            for (int xi = 0; xi < xs.Count - 1; xi++)
            {
                for (int zi = 0; zi < zs.Count - 1; zi++)
                {
                    float x0 = xs[xi], x1 = xs[xi + 1];
                    float z0 = zs[zi], z1 = zs[zi + 1];
                    if (x1 - x0 < MinimumPiece || z1 - z0 < MinimumPiece) continue;

                    float cx = (x0 + x1) * 0.5f;
                    float cz = (z0 + z1) * 0.5f;

                    bool taken = false;
                    foreach (ShellVoid gap in voids)
                    {
                        if (cx > gap.minX && cx < gap.maxX && cz > gap.minZ && cz < gap.maxZ)
                        {
                            taken = true;
                            break;
                        }
                    }

                    if (!taken) cells.Add(new Rectangle(x0, x1, z0, z1));
                }
            }

            return Merge(cells);
        }

        /// <summary>
        /// Joins cells that share a full edge, row by row and then column by column. A storey cut by one void
        /// is four rectangles instead of a handful of tiles, which keeps the draw count and the proof readable.
        /// </summary>
        public static List<Rectangle> Merge(List<Rectangle> cells)
        {
            List<Rectangle> rows = MergeAxis(cells, alongX: true);
            return MergeAxis(rows, alongX: false);
        }

        private static List<Rectangle> MergeAxis(List<Rectangle> cells, bool alongX)
        {
            var result = new List<Rectangle>();
            var used = new bool[cells.Count];

            for (int i = 0; i < cells.Count; i++)
            {
                if (used[i]) continue;

                Rectangle run = cells[i];
                used[i] = true;

                bool extended = true;
                while (extended)
                {
                    extended = false;
                    for (int j = 0; j < cells.Count; j++)
                    {
                        if (used[j]) continue;
                        if (!run.Touches(cells[j], alongX)) continue;
                        run = run.Union(cells[j], alongX);
                        used[j] = true;
                        extended = true;
                    }
                }

                result.Add(run);
            }

            return result;
        }

        /// <summary>The slab and whatever crowns it, for one block, in the type the data asks for.</summary>
        public static List<ShellPiece> RoofPieces(CampusShellLayout layout, ShellBox block)
        {
            var pieces = new List<ShellPiece>();
            if (layout == null || block == null) return pieces;
            if (block.floors == null || block.floors.Length == 0) return pieces;

            int top = block.floors[0];
            foreach (int floor in block.floors) top = Mathf.Max(top, floor);

            float crown = layout.FloorBase(top) + layout.frame.floorHeight;
            ShellRoofType type = layout.roofs.For(block.roof);

            if (block.roof == "open")
            {
                // A kerb, not a roof: the link stays open to the sky and still reads as a floor with an edge.
                float k = type.kerbThickness;
                float h = type.kerbHeight;
                pieces.Add(ShellPiece.Of($"{block.id} kerb south", block.minX, block.maxX,
                    block.minZ, block.minZ + k, crown, crown + h, "concreteDeep"));
                pieces.Add(ShellPiece.Of($"{block.id} kerb north", block.minX, block.maxX,
                    block.maxZ - k, block.maxZ, crown, crown + h, "concreteDeep"));
                pieces.Add(ShellPiece.Of($"{block.id} kerb west", block.minX, block.minX + k,
                    block.minZ + k, block.maxZ - k, crown, crown + h, "concreteDeep"));
                pieces.Add(ShellPiece.Of($"{block.id} kerb east", block.maxX - k, block.maxX,
                    block.minZ + k, block.maxZ - k, crown, crown + h, "concreteDeep"));
                return pieces;
            }

            float slab = type.slabThickness;
            float overhang = type.overhang;

            if (block.roof == "canopy")
            {
                // Bigger than the thing under it, on every side, and with a fascia deep enough to read as a
                // deliberate gesture rather than as a building that was measured badly.
                pieces.Add(ShellPiece.Of($"{block.id} canopy",
                    block.minX - overhang, block.maxX + overhang,
                    block.minZ - overhang, block.maxZ + overhang,
                    crown, crown + slab, "concretePale"));
                pieces.Add(ShellPiece.Of($"{block.id} canopy fascia",
                    block.minX - overhang, block.maxX + overhang,
                    block.minZ - overhang, block.maxZ + overhang,
                    crown - type.fascia, crown, "concreteDeep"));
                return pieces;
            }

            pieces.Add(ShellPiece.Of($"{block.id} roof", block.minX, block.maxX, block.minZ, block.maxZ,
                crown, crown + slab, "concretePale"));
            pieces.Add(ShellPiece.Of($"{block.id} parapet", block.minX, block.maxX, block.minZ, block.maxZ,
                crown + slab, crown + slab + type.parapet, "concreteDeep"));
            return pieces;
        }

        /// <summary>The two long faces' window bands, as pieces, one band per storey that is built.</summary>
        public static List<ShellPiece> WindowPieces(CampusShellLayout layout)
        {
            var pieces = new List<ShellPiece>();
            if (layout == null) return pieces;

            foreach (ShellBox block in layout.blocks)
            {
                // The step is per block and deliberately unequal between the two arms: the join at the
                // inside corner is one of the things that is wrong on purpose.
                float step = WindowStep(block.windowFace);
                if (step <= 0f) continue;

                foreach (int floor in block.floors)
                {
                    if (block.openGround && floor == 0) continue;

                    float y0 = layout.FloorBase(floor) + layout.windows.sill;
                    float y1 = y0 + layout.windows.height;

                    foreach (Rectangle part in Carve(layout, block, floor))
                    {
                        // A band belongs to the block's own face, so a piece that was cut back from it gets
                        // none: otherwise the storey the classroom is on would grow windows inside the room.
                        bool onFace = block.windowFace == "east" ? Mathf.Abs(part.maxX - block.maxX) < 0.0001f
                                    : block.windowFace == "west" ? Mathf.Abs(part.minX - block.minX) < 0.0001f
                                    : Mathf.Abs(part.minZ - block.minZ) < 0.0001f;
                        if (!onFace) continue;

                        bool alongZ = block.windowFace == "east" || block.windowFace == "west";
                        if (alongZ)
                        {
                            float span = part.maxZ - part.minZ - 2f * layout.windows.margin;
                            int count = Mathf.FloorToInt(span / step);
                            float outer = block.windowFace == "east" ? block.maxX : block.minX;
                            float outward = block.windowFace == "east" ? 1f : -1f;
                            for (int i = 0; i < count; i++)
                            {
                                float z0 = part.minZ + layout.windows.margin + i * step;
                                float z1 = z0 + step * layout.windows.glassFraction;
                                float r = layout.windows.reveal;
                                float depth = layout.windows.glassDepth;
                                pieces.Add(ShellPiece.Of($"{block.id} reveal",
                                    outer - r * 0.5f, outer + r * 0.5f, z0, z1, y0, y1, "void"));
                                pieces.Add(ShellPiece.Of($"{block.id} glass",
                                    Mathf.Min(outer, outer + outward * depth), Mathf.Max(outer, outer + outward * depth),
                                    z0, z1, y0, y1, "glass"));
                            }
                        }
                        else
                        {
                            float span = part.maxX - part.minX - 2f * layout.windows.margin;
                            int count = Mathf.FloorToInt(span / step);
                            for (int i = 0; i < count; i++)
                            {
                                float x0 = part.minX + layout.windows.margin + i * step;
                                float x1 = x0 + step * layout.windows.glassFraction;
                                float r = layout.windows.reveal;
                                float depth = layout.windows.glassDepth;
                                pieces.Add(ShellPiece.Of($"{block.id} reveal",
                                    x0, x1, block.minZ - r * 0.5f, block.minZ + r * 0.5f, y0, y1, "void"));
                                pieces.Add(ShellPiece.Of($"{block.id} glass",
                                    x0, x1, block.minZ - depth, block.minZ, y0, y1, "glass"));
                            }
                        }
                    }
                }
            }

            return pieces;
        }

        /// <summary>
        /// Band spacing per face. The two arms are on different spacings on purpose: it is the declared
        /// misalignment, and it is the only way the join at the inside corner can be seen to be wrong.
        /// </summary>
        public static float WindowStep(string face) =>
            face == "south" ? 2.6f
            : face == "west" || face == "east" ? 2.15f
            : 0f;

        /// <summary>A rectangle in plan. Exact float comparisons are never used on it.</summary>
        public struct Rectangle
        {
            public float minX;
            public float maxX;
            public float minZ;
            public float maxZ;

            public Rectangle(float minX, float maxX, float minZ, float maxZ)
            {
                this.minX = minX;
                this.maxX = maxX;
                this.minZ = minZ;
                this.maxZ = maxZ;
            }

            public float Width => maxX - minX;
            public float Depth => maxZ - minZ;

            public bool Touches(Rectangle other, bool alongX)
            {
                const float tolerance = 0.0001f;
                if (alongX)
                {
                    return Mathf.Abs(minZ - other.minZ) < tolerance
                           && Mathf.Abs(maxZ - other.maxZ) < tolerance
                           && Mathf.Abs(maxX - other.minX) < tolerance;
                }

                return Mathf.Abs(minX - other.minX) < tolerance
                       && Mathf.Abs(maxX - other.maxX) < tolerance
                       && Mathf.Abs(maxZ - other.minZ) < tolerance;
            }

            public Rectangle Union(Rectangle other, bool alongX) =>
                alongX
                    ? new Rectangle(minX, Mathf.Max(maxX, other.maxX), Mathf.Min(minZ, other.minZ), Mathf.Max(maxZ, other.maxZ))
                    : new Rectangle(Mathf.Min(minX, other.minX), Mathf.Max(maxX, other.maxX), minZ, Mathf.Max(maxZ, other.maxZ));
        }
    }
}
