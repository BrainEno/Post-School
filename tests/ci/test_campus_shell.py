"""The campus shell's declared impossibilities have to stay declared, and stay impossible.

campus_shell.json is the single source of truth for the massing, and nothing in CI would otherwise
notice it drifting: a slab quietly given a support, a louvre count made even, a corridor extended
until it reaches a building. Those are the five things the sheet exists to show, so each one gets an
assertion that fails if it is tidied away.

The last test is the one that matters most: every solid in the file has to be reachable from the
proof script's own readers. A block declared but never drawn would be a plan nobody can check.

Run: python3 -m unittest discover -s tests/ci -p 'test_*.py' -v
"""

from __future__ import annotations

import json
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LAYOUT = ROOT / "Assets/Resources/World/campus_shell.json"


def data() -> dict:
    return json.loads(LAYOUT.read_text(encoding="utf-8"))


def block(layout: dict, block_id: str) -> dict:
    return next(b for b in layout["blocks"] if b["id"] == block_id)


class CampusShellTests(unittest.TestCase):

    def test_the_shell_is_marked_as_unreviewed_prototype_data(self):
        layout = data()
        self.assertIn(layout["status"], ("prototype_shell_pending_visual_review",))
        self.assertIn("note", layout)
        # Shell only. A floors list that grows a room is a different piece of work.
        self.assertIn("no rooms", layout["note"])

    def test_the_classroom_keeps_its_own_footprint_and_floor(self):
        layout = data()
        room = layout["classroomFootprint"]
        self.assertEqual(room["minX"], -6.2)
        self.assertEqual(room["maxX"], 6.2)
        self.assertEqual(room["minZ"], -4.5)
        self.assertEqual(room["maxZ"], 11.5)
        self.assertEqual(room["storey"], 1)
        # The classroom floor is y = 0 by definition; every other height is measured from it.
        self.assertEqual(layout["frame"]["classroomFloorY"], 0.0)

    def test_the_room_sits_inside_a_block_that_has_three_floors(self):
        layout = data()
        room = layout["classroomFootprint"]
        wing = block(layout, "teaching_block_wing")
        self.assertGreaterEqual(room["minZ"], wing["minZ"])
        self.assertLessEqual(room["maxZ"], wing["maxZ"])
        self.assertEqual(wing["floors"], [0, 1, 2])

    def test_the_lifted_slab_has_nothing_holding_it_up(self):
        layout = data()
        slabs = layout["liftedSlabs"]
        self.assertEqual(len(slabs), 1)
        slab = slabs[0]
        # Declared in the impossibilities list, and with no support keys of any kind: a change that
        # quietly adds "supports" or "columns" is exactly the edit this test exists to stop.
        self.assertIn("lifted_slab", {i["id"] for i in layout["impossibilities"]})
        for key in ("supports", "columns", "bearing", "attachedTo"):
            self.assertNotIn(key, slab)
        # Higher than every roof on the site, so it is not simply the top floor of something.
        tops = [max(b["floors"]) * layout["frame"]["floorHeight"] + layout["frame"]["floorHeight"]
                for b in layout["blocks"]]
        self.assertGreater(slab["y"], min(tops))

    def test_the_louvre_counts_are_odd_where_the_joke_needs_them_to_be(self):
        layout = data()
        counts = {l["id"]: l["count"] for l in layout["louvres"]}
        self.assertEqual(counts["louvre_fin_south"], 34)
        self.assertEqual(counts["louvre_fin_north"], 26)
        # An odd count leaves a half bay at the end; an even one closes the rhythm and the joke dies.
        self.assertEqual(counts["louvre_fin_north"] % 2, 0,
                         "the north fins are counted unevenly on purpose: (count - 1) bays over a fixed span")
        for louvre in layout["louvres"]:
            self.assertGreater(louvre["projection"], 1.0)
            self.assertLess(louvre["thickness"], 0.25)

    def test_the_east_link_reaches_no_building(self):
        layout = data()
        link = block(layout, "corridor_south")

        # "link" names the block a corridor leaves from. The east one leaves the wing and must not
        # name anything else, because there is nothing else for it to arrive at.
        self.assertEqual(link.get("link"), "teaching_block_wing")
        self.assertEqual(sorted(link["floors"]), [1])

        # And nothing may be built where it ends.
        for other in layout["blocks"]:
            if other["id"] == link["id"]:
                continue
            overlaps_x = not (other["maxX"] <= link["maxX"] or other["minX"] >= link["maxX"])
            overlaps_z = not (other["maxZ"] <= link["minZ"] or other["minZ"] >= link["minZ"])
            self.assertFalse(overlaps_x and overlaps_z,
                             f"{other['id']} now abuts the east link, which is supposed to stop in mid-air")

    def test_the_ground_is_a_grid_and_not_a_surface(self):
        layout = data()
        ground = layout["ground"]
        self.assertGreater(ground["gridSpacing"], 0.0)
        self.assertGreater(ground["gridExtentX"], 40.0)
        # No material name beyond the line colour: a ground *surface* would need one, and there is none.
        self.assertNotIn("surface", ground)
        self.assertNotIn("material", ground)

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
                # A field of view past 75 degrees is a fisheye; past 90 it is a mistake.
                self.assertLess(view["fovDegrees"], 80.0, f"{view['id']} is wider than the sheet can read")
                self.assertGreater(view["fovDegrees"], 20.0)
                self.assertGreater(view["far"], view["near"])
            else:
                self.assertEqual(view["elevation"], 90)

    def test_every_block_the_file_declares_is_drawable(self):
        """A block that the builder cannot draw is a plan that cannot be checked."""
        sys.path.insert(0, str(ROOT / "tools"))
        try:
            import campus_shell_proof as proof  # noqa: E402
        except ModuleNotFoundError as missing:
            # The proof needs matplotlib; CI installs numpy only. Skip with the reason named rather
            # than pretending the check ran.
            self.skipTest(f"the proof script needs {missing.name} to be imported")

        layout = proof.load()
        boxes = proof.block_boxes(layout)
        for declared in layout["blocks"]:
            for floor in declared["floors"]:
                if proof.is_voided(layout, declared, floor):
                    # Declared as a void: the classroom's column, above and below the room. Nothing
                    # should be built there, and the next test checks that nothing is.
                    continue
                y0 = proof.floor_base(layout, floor)
                matches = [b for b in boxes
                           if abs(b.x0 - declared["minX"]) < 1e-6 and abs(b.x1 - declared["maxX"]) < 1e-6
                           and abs(b.z0 - declared["minZ"]) < 1e-6 and abs(b.z1 - declared["maxZ"]) < 1e-6
                           and abs(b.y0 - y0) < 1e-6]
                self.assertEqual(len(matches), 1,
                                 f"{declared['id']} floor {floor} is declared but not built as a solid")

    def test_nothing_is_built_in_the_classroom_column_but_the_room_itself(self):
        """The one place the shell must stay out of, on every floor it does not own."""
        sys.path.insert(0, str(ROOT / "tools"))
        try:
            import campus_shell_proof as proof  # noqa: E402
        except ModuleNotFoundError as missing:
            self.skipTest(f"the proof script needs {missing.name} to be imported")

        layout = proof.load()
        room = layout["classroomFootprint"]
        for floor in (0, 2):
            wing = next(b for b in layout["blocks"] if b["id"] == "teaching_block_wing")
            self.assertTrue(proof.is_voided(layout, wing, floor),
                            f"floor {floor} of the wing is built, so the shell intersects the classroom")

        # And the storey the room is on is the one the data says it is.
        self.assertEqual(room["storey"], 1)
        self.assertEqual(layout["frame"]["floorCount"], 3)


if __name__ == "__main__":
    unittest.main()


class ShellBuilderMirrorsTheData(unittest.TestCase):
    """The C# builder and the layout must not drift apart.

    There is no Unity in CI, so the C# cannot be executed. What can be checked is the thing that
    actually breaks: a constant written into the builder that disagrees with the file it is supposed to
    read, or a value the file carries that the builder silently ignores.
    """

    BUILDER = ROOT / "Assets/Scripts/World/CampusShellBuilder.cs"
    DEFINITIONS = ROOT / "Assets/Scripts/World/CampusShellLayout.cs"

    def builder_source(self) -> str:
        return self.BUILDER.read_text(encoding="utf-8")

    def test_the_builder_reads_the_file_and_does_not_restate_it(self):
        source = self.builder_source()
        # The block extents, floor heights and louvre counts all have to come from the layout. A number
        # that appears in both the C# and the JSON is a number that will drift.
        data_text = LAYOUT.read_text(encoding="utf-8")
        for forbidden in ("12.4f", "16.0f", "9.4f", "0.94f", "1.04f"):
            self.assertNotIn(forbidden, source,
                             f"{forbidden} is written into the builder; it belongs in campus_shell.json")
        layout = json.loads(data_text)
        self.assertEqual(layout["frame"]["floorCount"], 3)

    def test_the_builder_never_gives_the_shell_a_collider(self):
        source = self.builder_source()
        # The shell is scenery. A collider here would become a wall across the courtyard that nobody
        # decided to build, and the classroom's own collision is built by ClassroomBuilder.
        self.assertIn("Destroy(collider)", source)
        self.assertNotIn("AddComponent<BoxCollider>", source)
        self.assertNotIn("AddComponent<MeshCollider>", source)
        self.assertNotIn("gameObject.AddComponent<Collider>", source)

    def test_the_builder_skips_the_classroom_column(self):
        source = self.builder_source()
        self.assertIn("IsVoided", source)
        self.assertIn("voids", source)
        # And the storey comes from the data rather than from an assumption about "the middle floor".
        self.assertIn("classroomFootprint.storey", source)

    def test_the_lifted_slab_is_built_with_no_support(self):
        source = self.builder_source()
        start = source.index("private static void BuildLifted")
        end = source.index("// ----------------------------------------------------------------- objects")
        body = source[start:end]
        for support_word in ("Column", "Cable", "Bracket", "Strut", "Pillar"):
            self.assertNotIn(f'"{support_word}', body,
                             f"the lifted slab gained a {support_word.lower()}, which is the one thing it must not have")

    def test_the_data_classes_declare_every_key_the_file_uses(self):
        """JsonUtility ignores unknown keys silently, so a missing field is a silently ignored value."""
        definitions = self.DEFINITIONS.read_text(encoding="utf-8")
        layout = json.loads(LAYOUT.read_text(encoding="utf-8"))

        for key in layout["frame"]:
            if key == "note":
                continue
            self.assertIn(f"public float {key};" if not key.startswith("floorCount") else "public int floorCount;",
                          definitions, f"ShellFrame has no field for '{key}'")

        for key in layout["classroomFootprint"]:
            if key in ("note", "windowWall", "missingWall"):
                continue
            self.assertIn(key, definitions, f"classroomFootprint.{key} has no field in CampusShellLayout")

        for key in layout["ground"]:
            if key == "note":
                continue
            self.assertIn(key, definitions, f"ShellGround has no field for '{key}'")

        for key in layout["louvres"][0]:
            if key == "note":
                continue
            self.assertIn(key, definitions, f"ShellLouvre has no field for '{key}'")

        for key in layout["objects"][0]:
            self.assertIn(key, definitions, f"ShellObject has no field for '{key}'")

    def test_the_materials_the_builder_uses_are_all_in_the_palette(self):
        definitions = self.DEFINITIONS.read_text(encoding="utf-8")
        layout = json.loads(LAYOUT.read_text(encoding="utf-8"))
        for name in ("concrete", "concreteDeep", "concreteShadow", "concretePale", "steel", "steelDark",
                     "glass", "void", "grid", "red", "ochre", "paper"):
            self.assertIn(name, layout["materials"], f"the palette has no '{name}'")
        # 'void' is a C# keyword, so the field is written with an escape; check it is there.
        self.assertIn("@void", definitions)
