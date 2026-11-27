"""The shell's data, its geometry, and the agreement between the file, the C# and the proof.

There is no Unity in CI, so the C# cannot be executed. What can be checked is the thing that actually
breaks: geometry that disagrees with the file it is built from, and a value the file carries that the
builder silently ignores.

Two rules here earned their place the hard way and must not be relaxed:

  * A void takes a rectangle out of a storey; it does not delete the storey. The first version deleted
    the whole floor, which removed the middle storey of the south arm.
  * Every height is measured from the street, and the classroom's floor is the datum that floor 1 lands
    on. The first version counted storeys from the classroom upward and left the campus hovering.

Run: python3 -m unittest discover -s tests/ci -p 'test_*.py' -v
"""

from __future__ import annotations

import json
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LAYOUT = ROOT / "Assets/Resources/World/campus_shell.json"
BUILDER = ROOT / "Assets/Scripts/World/CampusShellBuilder.cs"
MASSING = ROOT / "Assets/Scripts/World/CampusShellMassing.cs"
DEFINITIONS = ROOT / "Assets/Scripts/World/CampusShellLayout.cs"

# The classroom's own wall thickness. The shell must stop one wall away from the room.
WALL = 0.26


def data() -> dict:
    return json.loads(LAYOUT.read_text(encoding="utf-8"))


def block(layout: dict, block_id: str) -> dict:
    return next(b for b in layout["blocks"] if b["id"] == block_id)


def proof():
    """The proof script's geometry readers, or a skip naming the missing dependency."""
    sys.path.insert(0, str(ROOT / "tools"))
    try:
        import campus_shell_proof as module  # noqa: E402
    except ModuleNotFoundError as missing:
        raise unittest.SkipTest(f"the proof script needs {missing.name} to be imported")
    return module


class TheFileSaysWhatTheShellIs(unittest.TestCase):

    def test_the_shell_is_marked_as_unreviewed_prototype_data(self):
        layout = data()
        self.assertEqual(layout["status"], "prototype_shell_pending_visual_review")
        # Shell only. A floors list that grows a room is a different piece of work.
        self.assertIn("no rooms", layout["note"])
        self.assertIn("nothing walkable", layout["note"])

    def test_the_classroom_keeps_its_own_footprint_and_floor(self):
        room = data()["classroomFootprint"]
        self.assertEqual((room["minX"], room["maxX"]), (-6.2, 6.2))
        self.assertEqual((room["minZ"], room["maxZ"]), (-4.5, 11.5))
        self.assertEqual(room["storey"], 1)
        self.assertEqual(room["windowWall"], "west")
        self.assertEqual(room["missingWall"], "south")

    def test_the_storey_the_room_is_on_stands_exactly_on_the_room_floor(self):
        """The datum the first version got wrong, stated as arithmetic."""
        frame = data()["frame"]
        room = data()["classroomFootprint"]
        base_of_room_storey = frame["streetY"] + room["storey"] * frame["floorHeight"]
        self.assertAlmostEqual(base_of_room_storey, frame["classroomFloorY"], places=6,
                               msg="storey 1 must start at the classroom's own floor, or the campus floats")
        # And the ground floor stands on the street, which is where DuskCityBuilder put it.
        self.assertAlmostEqual(frame["streetY"] + 0 * frame["floorHeight"], frame["streetY"], places=6)
        self.assertLess(frame["streetY"], frame["classroomFloorY"])


class OpeningsAreRectanglesNotDeletedFloors(unittest.TestCase):

    def test_the_classroom_opening_is_the_room_and_nothing_more(self):
        layout = data()
        room = layout["classroomFootprint"]
        gap = next(v for v in layout["voids"] if v["id"] == "classroom_opening")
        self.assertEqual(gap["floors"], [room["storey"]])
        self.assertEqual((gap["minX"], gap["maxX"]), (room["minX"], room["maxX"]))
        self.assertEqual((gap["minZ"], gap["maxZ"]), (room["minZ"], room["maxZ"]))

    def test_the_storey_is_built_around_the_room(self):
        """The storey the classroom is on must still exist beside the room, not vanish with it."""
        p = proof()
        layout = p.load()
        room = layout["classroomFootprint"]
        wing = next(b for b in layout["blocks"] if b["id"] == "teaching_block_wing")
        parts = p.carve(layout, wing, room["storey"])
        self.assertGreaterEqual(len(parts), 2, "the storey vanished instead of being cut")

        # A strip of floor east of the room, from the room's wall to the arm's east face: the room is
        # inside a building that continues past it. This is what the first version deleted.
        strip = [part for part in parts if abs(part[0] - room["maxX"]) < 1e-6]
        self.assertTrue(strip, "nothing is built east of the classroom, so the room is the whole storey")
        # It runs the room's whole length bar the wall the fall slot reaches into (0.26 m), which is why
        # this is a ratio rather than an equality: the strip may be shorter, but not shorter than a room.
        beside_the_room = (wing["maxX"] - room["maxX"]) * (room["maxZ"] - room["minZ"])
        self.assertGreater(sum((x1 - x0) * (z1 - z0) for x0, x1, z0, z1 in strip), beside_the_room * 0.95,
                           "the floor east of the room is a stub, not the rest of the building")

    def test_a_void_never_takes_a_piece_smaller_than_a_hand(self):
        p = proof()
        layout = p.load()
        for declared in layout["blocks"]:
            for floor in declared["floors"]:
                for x0, x1, z0, z1 in p.carve(layout, declared, floor):
                    self.assertGreater(x1 - x0, 0.01)
                    self.assertGreater(z1 - z0, 0.01)

    def test_the_cut_covers_exactly_the_floor_no_void_asks_for(self):
        """Sampled, not summed: a point is built if and only if it is outside every void.

        Sampling is what makes this independent of the way the cut merges its pieces. It is also the only
        version that survives two voids overlapping, which they do here: the fall slot reaches into the
        classroom's opening by one wall thickness on the storey the room is on.
        """
        p = proof()
        layout = p.load()
        step = 0.25
        for declared in layout["blocks"]:
            for floor in declared["floors"]:
                parts = p.carve(layout, declared, floor)
                gaps = p.voids_on(layout, declared, floor)
                x = declared["minX"] + step * 0.5
                while x < declared["maxX"]:
                    z = declared["minZ"] + step * 0.5
                    while z < declared["maxZ"]:
                        in_void = any(g["minX"] < x < g["maxX"] and g["minZ"] < z < g["maxZ"] for g in gaps)
                        hits = [part for part in parts
                                if part[0] <= x <= part[1] and part[2] <= z <= part[3]]
                        where = f"{declared['id']} floor {floor} at ({x:.2f}, {z:.2f})"
                        if in_void:
                            self.assertFalse(hits, f"{where} is inside a void but still built")
                        else:
                            self.assertEqual(len(hits), 1, f"{where} is not built exactly once")
                        z += step
                    x += step

    def test_nothing_is_built_inside_the_room(self):
        p = proof()
        layout = p.load()
        room = layout["classroomFootprint"]
        inner = (room["minX"] + WALL, room["maxX"] - WALL, room["minZ"] + WALL, room["maxZ"] - WALL)
        for box in p.block_boxes(layout) + p.window_bands(layout):
            if box.kind == "lifted":
                continue
            overlaps_x = box.x0 < inner[1] and box.x1 > inner[0]
            overlaps_z = box.z0 < inner[3] and box.z1 > inner[2]
            overlaps_y = box.y0 < 3.5 and box.y1 > 0.0
            self.assertFalse(overlaps_x and overlaps_z and overlaps_y,
                             f"a {box.kind} box at x {box.x0}..{box.x1}, z {box.z0}..{box.z1} "
                             f"intrudes on the classroom")

    def test_the_fall_slot_keeps_the_drop_open_to_the_street(self):
        layout = data()
        slot = next(v for v in layout["voids"] if v["id"] == "fall_slot")
        room = layout["classroomFootprint"]
        # The room's missing wall is the south one; the slot has to start there and run south.
        self.assertAlmostEqual(slot["maxZ"], room["minZ"] + WALL, places=6)
        self.assertLess(slot["minZ"], room["minZ"])
        # And it has to include the ground floor, or the container has concrete around it.
        self.assertIn(0, slot["floors"])


class RoofsAreBuiltAsDeclared(unittest.TestCase):

    def test_every_roof_type_in_use_is_declared(self):
        layout = data()
        declared = {k for k in layout["roofs"] if k != "note"}
        for block in layout["blocks"]:
            self.assertIn(block["roof"], declared,
                          f"{block['id']} asks for '{block['roof']}', which the file does not define")

    def test_the_open_links_have_no_roof_and_a_kerb(self):
        p = proof()
        layout = p.load()
        for name in ("corridor_north", "corridor_south"):
            declared = next(b for b in layout["blocks"] if b["id"] == name)
            roof = [b for b in p.roof_boxes(layout, declared, "#000000", "#000000")]
            self.assertTrue(roof, f"{name} has no edge at all")
            self.assertTrue(all(b.kind == "kerb" for b in roof), f"{name} gained a slab")
            top = p.floor_base(layout, max(declared["floors"])) + layout["frame"]["floorHeight"]
            for piece in roof:
                self.assertAlmostEqual(piece.y0, top, places=6)

    def test_the_gate_roof_overhangs_the_gate(self):
        p = proof()
        layout = p.load()
        gate = next(b for b in layout["blocks"] if b["id"] == "north_gate")
        roof = p.roof_boxes(layout, gate, "#000000", "#000000")
        widest = max(roof, key=lambda b: (b.x1 - b.x0) * (b.z1 - b.z0))
        self.assertLess(widest.x0, gate["minX"])
        self.assertGreater(widest.x1, gate["maxX"])
        self.assertLess(widest.z0, gate["minZ"])

    def test_a_flat_roof_is_a_slab_and_a_parapet(self):
        p = proof()
        layout = p.load()
        wing = next(b for b in layout["blocks"] if b["id"] == "teaching_block_wing")
        kinds = [b.kind for b in p.roof_boxes(layout, wing, "#000000", "#000000")]
        self.assertEqual(sorted(kinds), ["parapet", "roof"])


class TheDeliberateMistakesSurvive(unittest.TestCase):

    def test_the_lifted_slab_has_nothing_holding_it_up(self):
        layout = data()
        slabs = layout["liftedSlabs"]
        self.assertEqual(len(slabs), 1)
        slab = slabs[0]
        self.assertIn("lifted_slab", {i["id"] for i in layout["impossibilities"]})
        for key in ("supports", "columns", "bearing", "attachedTo"):
            self.assertNotIn(key, slab)
        # Higher than every roof on the site, so it is not simply the top floor of something.
        tops = [max(b["floors"]) * layout["frame"]["floorHeight"] + layout["frame"]["streetY"]
                + layout["frame"]["floorHeight"] for b in layout["blocks"]]
        self.assertGreater(slab["y"], min(tops))

    def test_the_fin_counts_are_the_owner_s(self):
        layout = data()
        counts = {l["id"]: l["count"] for l in layout["louvres"]}
        # 34 and 26 are the owner's numbers and this task did not touch them.
        self.assertEqual(sorted(counts.values()), [26, 34])
        for louvre in layout["louvres"]:
            self.assertGreater(louvre["projection"], 1.0)
            self.assertLess(louvre["thickness"], 0.25)

    def test_the_fins_run_past_the_wall_they_are_fixed_to(self):
        layout = data()
        for louvre in layout["louvres"]:
            host = block(layout, louvre["block"])
            self.assertLess(louvre["along"]["min"], host["minX"],
                            f"{louvre['id']} now starts on the wall, so nothing overhangs")
            self.assertGreater(louvre["along"]["max"], host["maxX"],
                               f"{louvre['id']} now stops on the wall, so nothing overhangs")

    def test_the_east_link_reaches_no_building(self):
        layout = data()
        link = block(layout, "corridor_south")
        self.assertEqual(sorted(link["floors"]), [1])
        for other in layout["blocks"]:
            if other["id"] == link["id"] or other["id"] == "teaching_block_wing":
                continue  # the wall it leaves from
            overlaps_x = other["minX"] < link["maxX"] and other["maxX"] > link["minX"]
            overlaps_z = other["minZ"] < link["maxZ"] and other["maxZ"] > link["minZ"]
            self.assertFalse(overlaps_x and overlaps_z,
                             f"{other['id']} now abuts the east link, which is supposed to stop in mid-air")

    def test_the_north_link_arrives(self):
        layout = data()
        link = block(layout, "corridor_north")
        spine = block(layout, "teaching_block_spine")
        self.assertAlmostEqual(link["maxX"], spine["minX"], places=6,
                               msg="the north link stops short of the arm it is supposed to join")
        self.assertAlmostEqual(link["minX"], block(layout, "equipment_shed")["maxX"], places=6)

    def test_the_ground_is_a_grid_and_not_a_surface(self):
        ground = data()["ground"]
        self.assertGreater(ground["gridSpacing"], 0.0)
        self.assertGreater(ground["gridExtentX"], 40.0)
        self.assertNotIn("surface", ground)
        self.assertNotIn("material", ground)

    def test_the_red_mat_sits_in_the_courtyard_the_room_looks_at(self):
        layout = data()
        mat = layout["ground"]["mat"]
        room = layout["classroomFootprint"]
        self.assertLess(mat["maxX"], room["minX"], "the mat has to be west of the room's window wall")
        self.assertGreater(mat["maxZ"], room["minZ"])


class TheViewpointsAreCameras(unittest.TestCase):

    def test_every_viewpoint_is_a_camera_the_proof_can_place(self):
        layout = data()
        views = layout["viewpoints"]
        self.assertEqual(len(views), 6)
        ids = {v["id"] for v in views}
        self.assertIn("plan", ids)
        self.assertEqual(len(ids), len(views), "viewpoint ids must be unique")

        for view in views:
            if view["kind"] == "perspective":
                self.assertEqual(len(view["eye"]), 3)
                self.assertEqual(len(view["target"]), 3)
                self.assertNotEqual(view["eye"], view["target"])
                self.assertLess(view["fovDegrees"], 80.0, f"{view['id']} is wider than the sheet can read")
                self.assertGreater(view["fovDegrees"], 20.0)
                self.assertGreater(view["far"], view["near"])
            else:
                self.assertEqual(view["elevation"], 90)


class TheBuilderMirrorsTheData(unittest.TestCase):
    """The C# and the layout must not drift apart. Both files are read, never run."""

    def test_neither_csharp_file_restates_a_dimension(self):
        source = BUILDER.read_text(encoding="utf-8") + MASSING.read_text(encoding="utf-8")
        for forbidden in ("12.4f", "16.0f", "9.4f", "4.15f", "2.35f", "1.6f", "0.95f", "-4.2f",
                          "0.6f", "0.18f", "0.02f", "0.10f"):
            self.assertNotIn(forbidden, source,
                             f"{forbidden} is written into the C#; it belongs in campus_shell.json")

    def test_the_builder_never_gives_the_shell_a_collider(self):
        for path in (BUILDER, MASSING):
            source = path.read_text(encoding="utf-8")
            self.assertNotIn("AddComponent<BoxCollider>", source)
            self.assertNotIn("AddComponent<MeshCollider>", source)
            self.assertNotIn("AddComponent<Rigidbody>", source)
        self.assertIn("Destroy(collider)", BUILDER.read_text(encoding="utf-8"))

    def test_the_cut_is_written_once_and_shapes_every_storey(self):
        source = MASSING.read_text(encoding="utf-8")
        self.assertIn("public static List<Rectangle> Carve(", source)
        self.assertIn("voids", source)
        # The builder must not carry a second copy of the cut.
        builder = BUILDER.read_text(encoding="utf-8")
        self.assertNotIn("IsInsideClassroom", builder, "the room is cut by a void, not by a special case")
        self.assertNotIn("IsVoided", builder)

    def test_the_roof_type_is_read_rather_than_assumed(self):
        source = BUILDER.read_text(encoding="utf-8") + MASSING.read_text(encoding="utf-8")
        self.assertIn("layout.roofs.For(block.roof)", source)
        # A hard-coded flat roof is exactly what this replaced.
        self.assertNotIn("0.28f", source)

    def test_the_gate_stands_on_the_street(self):
        source = BUILDER.read_text(encoding="utf-8")
        start = source.index("private static void BuildGate")
        end = source.index("// ---------------------------------------------------------------- louvres")
        body = source[start:end]
        self.assertIn("layout.frame.streetY", body,
                      "the gate's piers must be measured up from the street, not from zero")

    def test_the_materials_are_owned_by_whoever_builds_the_shell(self):
        source = BUILDER.read_text(encoding="utf-8")
        self.assertIn("ShellMaterials", source)
        self.assertIn("public void Dispose()", source)
        bootstrap = (ROOT / "Assets/Scripts/Prototype/PrototypeSceneBootstrap.cs").read_text(encoding="utf-8")
        self.assertIn("_campusMaterials.Dispose()", bootstrap)

    def test_the_data_classes_declare_every_key_the_file_uses(self):
        """JsonUtility ignores unknown keys silently, so a missing field is a silently ignored value."""
        definitions = DEFINITIONS.read_text(encoding="utf-8")
        layout = json.loads(LAYOUT.read_text(encoding="utf-8"))

        for key in layout["frame"]:
            if key == "note":
                continue
            expected = "public int floorCount;" if key == "floorCount" else f"public float {key};"
            self.assertTrue(expected in definitions, f"ShellFrame has no field for '{key}'")

        for key in layout["ground"]:
            if key == "note":
                continue
            self.assertTrue(key in definitions, f"ShellGround has no field for '{key}'")

        for key in layout["liftedSlabs"][0]:
            if key in ("note", "label", "id", "labelEn"):
                continue
            self.assertTrue(f"public float {key};" in definitions, f"ShellBox has no field for '{key}'")

        for key in layout["voids"][0]:
            if key in ("note", "label", "labelEn", "id", "floors"):
                continue
            self.assertTrue(f"public float {key};" in definitions, f"ShellVoid has no field for '{key}'")

        for key in layout["louvres"][0]:
            if key in ("note", "id", "label", "labelEn", "wall", "block", "along", "height"):
                continue
            self.assertIn(f"public float {key};" if key in ("projection", "thickness")
                          else f"public int {key};", definitions, f"ShellLouvre has no field for '{key}'")

        for key in layout["objects"][0]:
            self.assertIn(key, definitions, f"ShellObject has no field for '{key}'")

        for key in layout["windows"]:
            if key == "note":
                continue
            self.assertIn(f"public float {key};", definitions, f"ShellWindows has no field for '{key}'")

        for key in layout["porch"]:
            if key == "note":
                continue
            self.assertIn(f"public float {key};", definitions, f"ShellPorch has no field for '{key}'")

        # The additions this round: the per-block face and the roof table.
        self.assertIn("public string windowFace;", definitions)
        self.assertIn("public bool openGround;", definitions)
        self.assertIn("public ShellRoofs roofs", definitions)
        for key in layout["roofs"]["canopy"]:
            if key == "note":
                continue
            self.assertIn(f"public float {key};", definitions, f"ShellRoofType has no field for '{key}'")

    def test_the_materials_the_builder_uses_are_all_in_the_palette(self):
        definitions = DEFINITIONS.read_text(encoding="utf-8")
        layout = json.loads(LAYOUT.read_text(encoding="utf-8"))
        for name in ("concrete", "concreteDeep", "concreteShadow", "concretePale", "steel", "steelDark",
                     "glass", "void", "grid", "red", "ochre", "paper"):
            self.assertIn(name, layout["materials"], f"the palette has no '{name}'")
        self.assertIn("@void", definitions)


if __name__ == "__main__":
    unittest.main()
