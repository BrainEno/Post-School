using System;
using System.Collections.Generic;
using UnityEngine;

namespace PostSchool.World
{
    /// <summary>
    /// Builds the campus around the classroom: three storeys of teaching block in an L, a north gate, two
    /// links, and the things the owner asked to be wrong on purpose.
    ///
    /// This is massing. There are no rooms inside it, no stairs, no doors that open and nothing to walk on:
    /// the interior of every block is solid, and the only openings in the whole shell are the ones the
    /// layout declares as voids. That is deliberate. The classroom is built by ClassroomBuilder and this
    /// file must not touch a single surface of it.
    ///
    /// Three rules keep that promise:
    ///   1. The storey the classroom is on is still built, around the room. The room's own rectangle is a
    ///      declared void, not the whole floor, so the shell keeps its concrete east and south of it.
    ///   2. Nothing is given a collider. The shell is scenery until somebody decides what of it the
    ///      protagonist is allowed to reach, and a collider added here would silently become a wall across
    ///      the courtyard.
    ///   3. No dimension is written here. Sizes, storey heights, fin counts, roof thicknesses and the void
    ///      rectangles all come from campus_shell.json, through CampusShellMassing.
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
        public const string RoofsName = "Roofs";

        /// <summary>
        /// The shell's own materials. They are copies, so they outlive nothing: whoever builds the shell owns
        /// them and must dispose them, exactly as WorldSurfaceLibrary disposes the room's.
        /// </summary>
        public sealed class ShellMaterials : IDisposable
        {
            private readonly List<Material> _created = new List<Material>();
            private Dictionary<string, Material> _byRole;

            internal void Set(Dictionary<string, Material> byRole, List<Material> created)
            {
                _byRole = byRole;
                _created.AddRange(created);
            }

            public Material this[string role] => _byRole[role];

            public int Count => _created.Count;

            public void Dispose()
            {
                foreach (Material material in _created)
                {
                    if (material == null) continue;
                    if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                    else UnityEngine.Object.DestroyImmediate(material);
                }

                _created.Clear();
                _byRole = null;
            }
        }

        /// <summary>
        /// Builds the shell and hands back the materials it made, so the caller can dispose them. The
        /// three-argument overload leaves them to the scene, which is what the prototype did before materials
        /// were owned at all: prefer this one.
        /// </summary>
        public static GameObject Build(CampusShellLayout layout, IClassroomSurfaceProvider surfaces, Transform parent,
                                       out ShellMaterials materials)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (surfaces == null) throw new ArgumentNullException(nameof(surfaces));
            if (!layout.Validate(out string error))
            {
                throw new InvalidOperationException("Campus shell layout is unusable: " + error);
            }

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(parent, false);

            var created = new List<Material>();
            Dictionary<string, Material> palette = BuildPalette(layout.materials, surfaces, created);
            materials = new ShellMaterials();
            materials.Set(palette, created);

            Transform blocks = Group(root.transform, BlocksName);
            Transform roofs = Group(root.transform, RoofsName);
            Transform louvres = Group(root.transform, LouvresName);
            Transform ground = Group(root.transform, GroundName);
            Transform lifted = Group(root.transform, LiftedName);
            Transform objects = Group(root.transform, ObjectsName);

            BuildPieces(blocks, CampusShellMassing.BlockPieces(layout), palette);
            BuildPieces(blocks, CampusShellMassing.WindowPieces(layout), palette);
            BuildGate(blocks, layout, palette);

            foreach (ShellBox block in layout.blocks)
            {
                BuildPieces(roofs, CampusShellMassing.RoofPieces(layout, block), palette);
            }

            BuildLouvres(louvres, layout, palette);
            BuildGround(ground, layout, palette);
            BuildLifted(lifted, layout, palette);
            BuildObjects(objects, layout, palette);

            return root;
        }

        /// <summary>
        /// The same build, without handing back the materials. Only for callers that rebuild the shell once and
        /// then keep it; the copies are Unity objects and Unity will not collect them on its own.
        /// </summary>
        public static GameObject Build(CampusShellLayout layout, IClassroomSurfaceProvider surfaces, Transform parent) =>
            Build(layout, surfaces, parent, out _);

        /// <summary>How many fins one run builds. Used by the tests to check the run was not rounded.</summary>
        public static int FinCount(ShellLouvre louvre) => louvre == null ? 0 : louvre.count;

        // ---------------------------------------------------------------- blocks

        private static void BuildPieces(Transform parent, List<ShellPiece> pieces, Dictionary<string, Material> palette)
        {
            foreach (ShellPiece piece in pieces)
            {
                Material material = palette.TryGetValue(piece.role, out Material found) ? found : null;
                Box(piece.name, parent,
                    new Vector3(piece.footprint.CentreX, (piece.y0 + piece.y1) * 0.5f, piece.footprint.CentreZ),
                    new Vector3(piece.footprint.Width, piece.y1 - piece.y0, piece.footprint.Depth),
                    material);
            }
        }

        // ------------------------------------------------------------- north gate

        /// <summary>
        /// The porch. Open at ground level, solid above it, driven into the courtyard face of the spine. It is
        /// the only place the block admits anybody, and the classroom is nowhere near it: this is the gate you
        /// would arrive at, three floors of build away from the room you woke in.
        /// </summary>
        private static void BuildGate(Transform root, CampusShellLayout layout, Dictionary<string, Material> palette)
        {
            ShellBox gate = layout.Block("north_gate");
            if (gate == null || !gate.openGround) return;

            // Two piers and a lintel at ground level: an opening wide enough to walk through, which is as far
            // as "walkable" goes in this shell. The ground floor of the gate itself is never built.
            float pierWidth = Mathf.Min(gate.Width * layout.porch.pierFraction, gate.Depth - layout.porch.pierClearance);
            // From the street up, not from zero: the site's datum is the street, so piers measured from
            // zero float a whole storey above the ground they are supposed to stand on.
            float street = layout.frame.streetY;
            float pierTop = layout.FloorBase(1);
            float pierHeight = pierTop - street;

            Box("gate pier west", root,
                new Vector3(gate.minX + pierWidth * 0.5f, street + pierHeight * 0.5f, gate.CentreZ),
                new Vector3(pierWidth, pierHeight, gate.Depth), palette["concrete"]);
            Box("gate pier east", root,
                new Vector3(gate.maxX - pierWidth * 0.5f, street + pierHeight * 0.5f, gate.CentreZ),
                new Vector3(pierWidth, pierHeight, gate.Depth), palette["concrete"]);
            Box("gate lintel", root,
                new Vector3(gate.CentreX, pierTop + layout.porch.lintel * 0.5f, gate.CentreZ),
                new Vector3(gate.Width, layout.porch.lintel, gate.Depth), palette["concretePale"]);
        }

        // ---------------------------------------------------------------- louvres

        /// <summary>
        /// The fins. Counted, not fitted: the run divides the span by (count - 1), so the blades sit exactly
        /// where the layout says and the run overhangs the wall at both ends. That overhang is the declared
        /// impossibility, and it is arithmetic here rather than a note.
        /// </summary>
        private static void BuildLouvres(Transform root, CampusShellLayout layout, Dictionary<string, Material> palette)
        {
            Material steel = palette["steel"];
            foreach (ShellLouvre run in layout.louvres ?? Array.Empty<ShellLouvre>())
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
            }
        }

        // ------------------------------------------------------------------ ground

        /// <summary>
        /// The site is a grid of thin bars and no surface. Every line is geometry, because a plane with a grid
        /// texture would be a surface wearing a drawing, and this is a drawing with nothing under it.
        /// </summary>
        private static void BuildGround(Transform root, CampusShellLayout layout, Dictionary<string, Material> palette)
        {
            float y = layout.frame.streetY + layout.ground.gridLift;
            float t = layout.ground.gridThickness;

            int across = Mathf.FloorToInt(layout.ground.gridExtentX / layout.ground.gridSpacing);
            for (int i = 0; i <= across; i++)
            {
                float x = layout.ground.gridOriginX + i * layout.ground.gridSpacing;
                Box("grid line", root,
                    new Vector3(x, y, layout.ground.gridOriginZ + layout.ground.gridExtentZ * 0.5f),
                    new Vector3(t, t, layout.ground.gridExtentZ), palette["grid"]);
            }

            int down = Mathf.FloorToInt(layout.ground.gridExtentZ / layout.ground.gridSpacing);
            for (int i = 0; i <= down; i++)
            {
                float z = layout.ground.gridOriginZ + i * layout.ground.gridSpacing;
                Box("grid line", root,
                    new Vector3(layout.ground.gridOriginX + layout.ground.gridExtentX * 0.5f, y, z),
                    new Vector3(layout.ground.gridExtentX, t, t), palette["grid"]);
            }

            // The one saturated thing on the site, laid in the courtyard between the two arms.
            ShellBox mat = layout.ground.mat;
            if (mat != null && mat.maxX > mat.minX)
            {
                Box("red mat", root,
                    new Vector3(mat.CentreX, layout.frame.streetY + 0.03f, mat.CentreZ),
                    new Vector3(mat.Width, layout.ground.matThickness, mat.Depth), palette["red"]);
            }
        }

        // ------------------------------------------------------------ lifted slab

        /// <summary>
        /// The slab that is not held up. Nothing is added to hold it: no column, no cable, no bracket. If a
        /// later edit wants one, it has to delete this comment and the layout's declaration, which is the point.
        /// </summary>
        private static void BuildLifted(Transform root, CampusShellLayout layout, Dictionary<string, Material> palette)
        {
            foreach (ShellBox slab in layout.liftedSlabs ?? Array.Empty<ShellBox>())
            {
                GameObject lifted = Box("slab", root,
                    new Vector3(slab.CentreX, slab.y + slab.thickness * 0.5f, slab.CentreZ),
                    new Vector3(slab.Width, slab.thickness, slab.Depth), palette["concretePale"]);

                // The pitch is small enough to read as "this should be level" rather than as a deliberate
                // slope. A bigger angle would look like a design decision; a degree and a half looks like a
                // mistake, which is the note being played.
                lifted.transform.localRotation = Quaternion.Euler(0f, 0f, slab.tiltDegrees);
            }
        }

        // ----------------------------------------------------------------- objects

        /// <summary>
        /// The few things on the mat. Flat marks, not props: they give the courtyard a scale and say somebody
        /// was standing here, and nothing in them is interactive.
        /// </summary>
        private static void BuildObjects(Transform root, CampusShellLayout layout, Dictionary<string, Material> palette)
        {
            float baseY = layout.frame.streetY;
            foreach (ShellObject item in layout.objects ?? Array.Empty<ShellObject>())
            {
                float length = item.length > 0f ? item.length : 0.5f;
                GameObject body = Box(item.kind, root,
                    new Vector3(item.x, baseY + item.height * 0.5f, item.z),
                    new Vector3(length, item.height, 0.12f), palette["steelDark"]);
                body.transform.localRotation = Quaternion.Euler(0f, item.yaw, 0f);
            }
        }

        // -------------------------------------------------------------- primitives

        private static Transform Group(Transform parent, string name)
        {
            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        /// <summary>
        /// A box of scenery. No collider, ever: this shell is something to look at from the window and across
        /// the courtyard, and a collider here would become a wall nobody decided to build.
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
        /// One flat material per palette role, created through the surface library when it has one for the role
        /// and made here when it does not. The shell's colours live in its own palette, so it cannot borrow the
        /// room's warmth by accident.
        /// </summary>
        private static Dictionary<string, Material> BuildPalette(ShellPalette palette, IClassroomSurfaceProvider surfaces,
                                                                 List<Material> created)
        {
            return new Dictionary<string, Material>
            {
                { "concrete", Flat("Campus Concrete", palette.Concrete, surfaces, SurfaceRole.Building, created) },
                { "concretePale", Flat("Campus Concrete Pale", palette.ConcretePale, surfaces, SurfaceRole.BuildingFar, created) },
                { "concreteDeep", Flat("Campus Concrete Deep", palette.ConcreteDeep, surfaces, SurfaceRole.BuildingNear, created) },
                { "steel", Flat("Campus Steel", palette.Steel, surfaces, SurfaceRole.Metal, created) },
                { "steelDark", Flat("Campus Steel Dark", palette.SteelDark, surfaces, SurfaceRole.Ink, created) },
                { "glass", Flat("Campus Glass", palette.Glass, surfaces, SurfaceRole.Glass, created) },
                { "void", Flat("Campus Window Void", palette.Void, surfaces, SurfaceRole.Shadow, created) },
                { "grid", Flat("Campus Grid", palette.Grid, surfaces, SurfaceRole.Wainscot, created) },
                { "red", Flat("Campus Mat", palette.Red, surfaces, SurfaceRole.Accent, created) },
            };
        }

        private static Material Flat(string name, Color color, IClassroomSurfaceProvider surfaces, SurfaceRole fallback,
                                     List<Material> created)
        {
            Material material = surfaces.Surface(fallback);
            if (material == null) return null;

            // A dedicated material, not a mutation of the shared surface: the classroom uses the same roles,
            // and recolouring them here would change the room. Works for URP/Lit (_BaseColor) and for the
            // built-in Standard shader (_Color); if neither is present the copy is still labelled, so the
            // failure is a grey shell rather than a recoloured classroom.
            var copy = new Material(material) { name = name };
            if (copy.HasProperty("_BaseColor")) copy.SetColor("_BaseColor", color);
            if (copy.HasProperty("_Color")) copy.SetColor("_Color", color);
            created.Add(copy);
            return copy;
        }
    }
}
