using System;
using System.Collections.Generic;
using UnityEngine;

namespace PostSchool.World
{
    /// <summary>
    /// Builds the campus around the classroom: three storeys of teaching block in an L, a north gate,
    /// two links, and the things the owner asked to be wrong on purpose.
    ///
    /// This is massing. There are no rooms inside it, no stairs, no doors that open and nothing to walk
    /// on: the interior of every block is solid, and the only opening in the whole shell is the shaft the
    /// classroom already occupies. That is deliberate. The classroom is built by ClassroomBuilder and
    /// this file must not touch a single surface of it.
    ///
    /// Two rules keep that promise:
    ///   1. Anything that would intersect the classroom's own volume is skipped. The classroom spans
    ///      x -6.2..6.2, z -4.5..11.5 on floor 1; the shell leaves that column empty on floors 0 and 2
    ///      as well, which is what the layout's 'voids' declare.
    ///   2. Nothing is given a collider. The shell is scenery until somebody decides what of it the
    ///      protagonist is allowed to reach, and a collider added here would silently become a wall
    ///      across the courtyard.
    ///
    /// See docs/CLASSROOM_ART.md for the five things this building gets wrong on purpose, and why.
    /// </summary>
    public static class CampusShellBuilder
    {
        public const string RootName = "Campus";

        /// <summary>Named children the rest of the project is allowed to look for.</summary>
        public const string BlocksName = "Blocks";
        public const string LouvresName = "Fins";
        public const string GroundName = "Ground Grid";
        public const string LiftedName = "Lifted Slab";
        public const string ObjectsName = "Objects";

        /// <summary>
        /// Builds the shell around whatever is already there. Returns the root, so the scene owner can
        /// hide the whole campus at once.
        /// </summary>
        public static GameObject Build(CampusShellLayout layout, IClassroomSurfaceProvider surfaces, Transform parent)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (surfaces == null) throw new ArgumentNullException(nameof(surfaces));
            if (!layout.Validate(out string error))
            {
                throw new InvalidOperationException("Campus shell layout is unusable: " + error);
            }

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(parent, false);

            ShellPalette palette = layout.materials;
            Material concrete = Flat("Campus Concrete", palette.Concrete, surfaces, SurfaceRole.Building);
            Material concretePale = Flat("Campus Concrete Pale", palette.ConcretePale, surfaces, SurfaceRole.BuildingFar);
            Material concreteDeep = Flat("Campus Concrete Deep", palette.ConcreteDeep, surfaces, SurfaceRole.BuildingNear);
            Material steel = Flat("Campus Steel", palette.Steel, surfaces, SurfaceRole.Metal);
            Material glass = Flat("Campus Glass", palette.Glass, surfaces, SurfaceRole.Glass);
            Material voidMaterial = Flat("Campus Window Void", palette.Void, surfaces, SurfaceRole.Shadow);
            Material grid = Flat("Campus Grid", palette.Grid, surfaces, SurfaceRole.Wainscot);
            Material red = Flat("Campus Mat", palette.Red, surfaces, SurfaceRole.Accent);

            Transform blocks = Group(root.transform, BlocksName);
            Transform louvres = Group(root.transform, LouvresName);
            Transform ground = Group(root.transform, GroundName);
            Transform lifted = Group(root.transform, LiftedName);
            Transform objects = Group(root.transform, ObjectsName);

            BuildBlocks(blocks, layout, concrete, concretePale, concreteDeep);
            BuildWindowBands(blocks, layout, glass, voidMaterial);
            BuildGate(blocks, layout, concrete, concretePale);
            BuildLouvres(louvres, layout, steel);
            BuildGround(ground, layout, grid, red);
            BuildLifted(lifted, layout, concretePale);
            BuildObjects(objects, layout, steel);

            return root;
        }

        /// <summary>How many fins one run builds. Used by the tests to check the run was not rounded.</summary>
        public static int FinCount(ShellLouvre louvre) => louvre == null ? 0 : louvre.count;

        // ---------------------------------------------------------------- blocks

        private static void BuildBlocks(Transform root, CampusShellLayout layout, Material concrete,
                                        Material pale, Material deep)
        {
            // The column the classroom occupies is skipped on every floor, not only on the classroom's
            // own: the storeys above and below it are the declared voids, and building them would put
            // concrete through the room's floor slab and its ceiling.
            foreach (ShellBox block in layout.blocks)
            {
                foreach (int floor in block.floors)
                {
                    if (IsInsideClassroom(block, floor, layout)) continue;
                    if (IsVoided(block, floor, layout)) continue;

                    float y0 = layout.FloorBase(floor);
                    float height = layout.frame.floorHeight - layout.frame.slabThickness;
                    Slab(root, $"{block.id} floor {floor}", block, y0, height, concrete);
                }

                // A parapet on the top floor, tall for the size of the building. The brief's own
                // note: everything here is a little too much for what it is.
                int top = TopFloor(block);
                float crown = layout.FloorBase(top) + layout.frame.floorHeight;
                Slab(root, $"{block.id} roof", block, crown, 0.28f, pale);
                Slab(root, $"{block.id} parapet", block, crown + 0.28f, 1.07f, deep);
            }

        }

        // ------------------------------------------------------------ window bands

        /// <summary>
        /// A band of dark glass inset in each long face, one per floor. The two arms use different
        /// spacings on purpose: that is the declared misalignment, and it is the only way the join at the
        /// inside corner can be seen to be wrong.
        /// </summary>
        private static void BuildWindowBands(Transform root, CampusShellLayout layout, Material glass, Material voidMaterial)
        {
            // Spacing per block, in metres. Not derived: the whole point is that the two do not agree.
            var spacing = new Dictionary<string, float>
            {
                { "teaching_block_spine", 2.6f },
                { "teaching_block_wing", 2.15f },
            };

            foreach (ShellBox block in layout.blocks)
            {
                if (!spacing.TryGetValue(block.id, out float step)) continue;

                foreach (int floor in block.floors)
                {
                    if (IsVoided(block, floor, layout)) continue;

                    float y0 = layout.FloorBase(floor) + 1.15f;
                    float height = 1.75f;

                    if (block.id == "teaching_block_spine")
                    {
                        int count = Mathf.FloorToInt((block.Width - 1.2f) / step);
                        for (int i = 0; i < count; i++)
                        {
                            float x0 = block.minX + 0.6f + i * step;
                            float width = step * 0.62f;
                            // The reveal sits in the wall, the glass a little proud of it.
                            Box("reveal", root, new Vector3(x0 + width * 0.5f, y0 + height * 0.5f, block.maxZ),
                                new Vector3(width, height, 0.04f), voidMaterial);
                            Box("glass", root, new Vector3(x0 + width * 0.5f, y0 + height * 0.5f, block.maxZ + 0.05f),
                                new Vector3(width, height, 0.10f), glass);
                        }
                    }
                    else
                    {
                        int count = Mathf.FloorToInt((block.Depth - 1.2f) / step);
                        for (int i = 0; i < count; i++)
                        {
                            float z0 = block.minZ + 0.6f + i * step;
                            float depth = step * 0.62f;
                            Box("reveal", root, new Vector3(block.minX, y0 + height * 0.5f, z0 + depth * 0.5f),
                                new Vector3(0.04f, height, depth), voidMaterial);
                            Box("glass", root, new Vector3(block.minX - 0.05f, y0 + height * 0.5f, z0 + depth * 0.5f),
                                new Vector3(0.10f, height, depth), glass);
                        }
                    }
                }
            }
        }

        // ------------------------------------------------------------- north gate

        /// <summary>
        /// The porch. Open at ground level, solid above it, driven into the north face of the spine.
        /// It is the only place the block admits anybody, and the classroom is nowhere near it: this is
        /// the gate you would arrive at, three floors of build away from the room you woke in.
        /// </summary>
        private static void BuildGate(Transform root, CampusShellLayout layout, Material concrete,
                                      Material pale)
        {
            ShellBox gate = layout.Block("north_gate");
            if (gate == null) return;

            // Two piers and a lintel at ground level: an opening wide enough to walk through, which is
            // as far as "walkable" goes in this shell.
            float pierWidth = 1.1f;
            float pierHeight = layout.FloorBase(1) - 0.6f;
            Box("gate pier west", root,
                new Vector3(gate.minX + pierWidth * 0.5f, pierHeight * 0.5f, gate.maxZ - 0.6f),
                new Vector3(pierWidth, pierHeight, 1.2f), concrete);
            Box("gate pier east", root,
                new Vector3(gate.maxX - pierWidth * 0.5f, pierHeight * 0.5f, gate.maxZ - 0.6f),
                new Vector3(pierWidth, pierHeight, 1.2f), concrete);
            Box("gate lintel", root,
                new Vector3(gate.CentreX, pierHeight + 0.3f, gate.maxZ - 0.6f),
                new Vector3(gate.Width, 0.6f, 1.2f), pale);
        }

        // ---------------------------------------------------------------- louvres

        /// <summary>
        /// The fins. Counted, not fitted: the run divides the wall by (count - 1) and the thickness is
        /// appended at each end, so an odd count leaves a half bay rather than a bay. That is the
        /// impossibility, and it is arithmetic here rather than a note.
        /// </summary>
        private static void BuildLouvres(Transform root, CampusShellLayout layout, Material steel)
        {
            foreach (ShellLouvre run in layout.louvres)
            {
                ShellBox host = layout.Block(run.block);
                if (host == null) continue;

                float step = (run.along.max - run.along.min) / (run.count - 1);
                bool south = run.wall == "south";
                float anchor = south ? host.minZ : host.maxZ;
                float centreY = (run.height.from + run.height.to) * 0.5f;
                float height = run.height.to - run.height.from;

                for (int i = 0; i < run.count; i++)
                {
                    float along = run.along.min + i * step;

                    if (run.RunsAlongX)
                    {
                        float z = south ? anchor - run.projection * 0.5f : anchor + run.projection * 0.5f;
                        Box("fin", root, new Vector3(along, centreY, z),
                            new Vector3(run.thickness, height, run.projection), steel);
                    }
                    else
                    {
                        float x = south ? anchor - run.projection * 0.5f : anchor + run.projection * 0.5f;
                        Box("fin", root, new Vector3(x, centreY, along),
                            new Vector3(run.projection, height, run.thickness), steel);
                    }
                }

                // An odd count means (count - 1) is even, so the fins do not land on the wall's ends:
                // the run is centred on the span and overhangs by half a bay at each end. An even count
                // would close the rhythm exactly and the impossibility would quietly disappear, so the
                // layout keeps the counts odd and tests/ci/test_campus_shell.py holds it there.
            }
        }

        // ------------------------------------------------------------------ ground

        /// <summary>
        /// The site is a grid of thin bars and no surface. Every line is geometry, because a plane with a
        /// grid texture would be a surface wearing a drawing, and this is a drawing with nothing under it.
        /// </summary>
        private static void BuildGround(Transform root, CampusShellLayout layout, Material grid, Material red)
        {
            float y = layout.frame.streetY + 0.02f;
            float t = layout.ground.gridThickness;

            int across = Mathf.FloorToInt(layout.ground.gridExtentX / layout.ground.gridSpacing);
            for (int i = 0; i <= across; i++)
            {
                float x = layout.ground.gridOriginX + i * layout.ground.gridSpacing;
                Box("grid line", root,
                    new Vector3(x, y, layout.ground.gridOriginZ + layout.ground.gridExtentZ * 0.5f),
                    new Vector3(t, t, layout.ground.gridExtentZ), grid);
            }

            int down = Mathf.FloorToInt(layout.ground.gridExtentZ / layout.ground.gridSpacing);
            for (int i = 0; i <= down; i++)
            {
                float z = layout.ground.gridOriginZ + i * layout.ground.gridSpacing;
                Box("grid line", root,
                    new Vector3(layout.ground.gridOriginX + layout.ground.gridExtentX * 0.5f, y, z),
                    new Vector3(layout.ground.gridExtentX, t, t), grid);
            }

            // The one saturated thing on the site, laid where the grid runs out.
            ShellBox mat = layout.ground.mat;
            if (mat != null && mat.maxX > mat.minX)
            {
                Box("red mat", root,
                    new Vector3(mat.CentreX, layout.frame.streetY + 0.03f, mat.CentreZ),
                    new Vector3(mat.Width, 0.02f, mat.Depth), red);
            }
        }

        // ------------------------------------------------------------ lifted slab

        /// <summary>
        /// The slab that is not held up. Nothing is added to hold it: no column, no cable, no bracket.
        /// If a later edit wants one, it has to delete this comment and the layout's declaration, which
        /// is the point.
        /// </summary>
        private static void BuildLifted(Transform root, CampusShellLayout layout, Material pale)
        {
            foreach (ShellBox slab in layout.liftedSlabs)
            {
                GameObject lifted = Box("slab", root,
                    new Vector3(slab.CentreX, slab.y + slab.thickness * 0.5f, slab.CentreZ),
                    new Vector3(slab.Width, slab.thickness, slab.Depth), pale);

                // The pitch is small enough to read as "this should be level" rather than as a
                // deliberate slope. A bigger angle would look like a design decision; a degree and a
                // half looks like a mistake, which is the note being played.
                lifted.transform.localRotation = Quaternion.Euler(0f, 0f, slab.tiltDegrees);
            }
        }

        // ----------------------------------------------------------------- objects

        /// <summary>
        /// The few things on the mat. Flat marks, not props: they are here to give the courtyard a scale
        /// and to say somebody was standing here, and nothing in them is interactive.
        /// </summary>
        private static void BuildObjects(Transform root, CampusShellLayout layout, Material dark)
        {
            float baseY = layout.frame.streetY;
            foreach (ShellObject item in layout.objects)
            {
                float length = item.length > 0f ? item.length : 0.5f;
                GameObject body = Box(item.kind, root,
                    new Vector3(item.x, baseY + item.height * 0.5f, item.z),
                    new Vector3(length, item.height, 0.12f), dark);
                body.transform.localRotation = Quaternion.Euler(0f, item.yaw, 0f);
            }
        }

        // -------------------------------------------------------------- primitives

        private static bool IsInsideClassroom(ShellBox block, int floor, CampusShellLayout layout)
        {
            // Only the classroom's own storey matters here: the storeys above and below it are handled
            // by the declared voids, which cover the whole column rather than one floor of it.
            if (floor != ClassroomStorey(layout)) return false;

            ShellBox room = layout.classroomFootprint;
            bool overlaps = block.minX < room.maxX && block.maxX > room.minX
                            && block.minZ < room.maxZ && block.maxZ > room.minZ;
            return overlaps;
        }

        private static int ClassroomStorey(CampusShellLayout layout)
        {
            // Read from the data, not inferred: if the classroom ever moves to another floor, the shell
            // has to move its hole with it.
            return layout.classroomFootprint.storey;
        }

        private static bool IsVoided(ShellBox block, int floor, CampusShellLayout layout)
        {
            foreach (ShellVoid gap in layout.voids)
            {
                if (Array.IndexOf(gap.floors, floor) < 0) continue;
                bool overlaps = block.minX < gap.maxX && block.maxX > gap.minX
                                && block.minZ < gap.maxZ && block.maxZ > gap.minZ;
                if (overlaps) return true;
            }

            return false;
        }

        private static int TopFloor(ShellBox block)
        {
            int top = block.floors[0];
            foreach (int floor in block.floors)
            {
                if (floor > top) top = floor;
            }

            return top;
        }

        private static Transform Group(Transform parent, string name)
        {
            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static void Slab(Transform parent, string name, ShellBox footprint, float y0, float height, Material material)
        {
            Box(name, parent,
                new Vector3(footprint.CentreX, y0 + height * 0.5f, footprint.CentreZ),
                new Vector3(footprint.Width, height, footprint.Depth), material);
        }

        /// <summary>
        /// A box of scenery. No collider, ever: this shell is something to look at from the window and
        /// across the courtyard, and a collider here would become a wall nobody decided to build.
        /// </summary>
        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;

            Collider collider = box.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(collider);
                else UnityEngine.Object.DestroyImmediate(collider);
            }

            Renderer renderer = box.GetComponent<Renderer>();
            if (material != null) renderer.sharedMaterial = material;
            return box;
        }

        /// <summary>
        /// A flat material for the shell, created through the surface library when it has one for the
        /// role and made here when it does not. The shell's colours live in its own palette, so it cannot
        /// borrow the room's warmth by accident.
        /// </summary>
        private static Material Flat(string name, Color color, IClassroomSurfaceProvider surfaces, SurfaceRole fallback)
        {
            Material material = surfaces.Surface(fallback);
            if (material != null && material.HasProperty("_BaseColor"))
            {
                // A dedicated material, not a mutation of the shared surface: the classroom uses the
                // same roles, and recolouring them here would change the room.
                Material copy = new Material(material) { name = name };
                if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", color);
                if (copy.HasProperty("_Color")) copy.SetColor("_Color", color);
                return copy;
            }

            return material;
        }
    }
}
