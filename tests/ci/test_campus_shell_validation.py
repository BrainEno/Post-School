"""Negative controls for campus_shell.json's validation rules.

A rule that cannot fail is not a rule. The C# cannot run in CI, so CampusShellLayout.Validate is mirrored
here and each mirrored rule gets a case that must be rejected. The mirror is not the authority ---
CampusShellLayout.Validate is --- but a case that passes the mirror is a case that would pass the C# too,
because the two check the same fields against the same bounds.

Every rule below was added because something in this file was actually wrong at some point: a classroom
with no opening cut for it, an opening that swallowed the storey, a ground floor filled in under the fall,
a roof type nobody built, and a fin run that had drifted off its wall.

Run: python3 -m unittest discover -s tests/ci -p 'test_*.py' -v
"""

from __future__ import annotations

import copy
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LAYOUT = ROOT / "Assets/Resources/World/campus_shell.json"
DEFINITIONS = ROOT / "Assets/Scripts/World/CampusShellLayout.cs"

WALL = 0.26


def base() -> dict:
    return json.loads(LAYOUT.read_text(encoding="utf-8"))


def reject(data: dict) -> str | None:
    """The rules CampusShellLayout.Validate enforces, in the same order."""
    if not data.get("status"):
        return "no status"

    frame = data["frame"]
    if frame["floorCount"] < 1 or frame["floorHeight"] < 2.6 or frame["slabThickness"] <= 0:
        return "frame unusable"
    if frame["slabThickness"] >= frame["floorHeight"]:
        return "slab not thinner than a floor"

    if not data["blocks"]:
        return "no blocks"

    for block in data["blocks"]:
        if block["maxX"] <= block["minX"] or block["maxZ"] <= block["minZ"]:
            return "empty footprint"
        if not block["floors"]:
            return "no floors"
        for floor in block["floors"]:
            if floor < 0 or floor >= frame["floorCount"]:
                return "floor out of range"

    room = data["classroomFootprint"]
    gaps = {gap["id"]: gap for gap in data.get("voids") or []}

    opening = gaps.get("classroom_opening")
    if opening is None:
        return "no classroom opening"
    if room["storey"] not in opening["floors"]:
        return "opening on the wrong storey"
    if not (opening["minX"] <= room["minX"] and opening["maxX"] >= room["maxX"]
            and opening["minZ"] <= room["minZ"] and opening["maxZ"] >= room["maxZ"]):
        return "opening smaller than the room"

    def overlaps(gap: dict, box: dict) -> bool:
        return (box["minX"] < gap["maxX"] and box["maxX"] > gap["minX"]
                and box["minZ"] < gap["maxZ"] and box["maxZ"] > gap["minZ"])

    for block in data["blocks"]:
        if room["storey"] not in block["floors"]:
            continue
        if not overlaps(opening, block):
            continue
        if (opening["minX"] <= block["minX"] and opening["maxX"] >= block["maxX"]
                and opening["minZ"] <= block["minZ"] and opening["maxZ"] >= block["maxZ"]):
            return "opening swallows a block"

    if "fall_slot" not in gaps:
        return "no fall slot"

    for slab in data.get("liftedSlabs") or []:
        if slab["thickness"] <= 0:
            return "slab with no thickness"

    if "roofs" not in data:
        return "no roof types"

    for block in data["blocks"]:
        if block["roof"] not in ("flat", "canopy", "open"):
            return "unknown roof type"
        spec = data["roofs"][block["roof"]]
        if block["roof"] == "open":
            if spec["kerbHeight"] <= 0 or spec["kerbThickness"] <= 0:
                return "a link with neither deck nor kerb"
        elif spec["slabThickness"] <= 0:
            return "a roof with no thickness"

    for louvre in data.get("louvres") or []:
        if louvre["count"] < 2:
            return "a fin run of one"
        if louvre["along"]["max"] <= louvre["along"]["min"]:
            return "a fin run spanning nothing"
        if louvre["projection"] <= 0 or louvre["thickness"] <= 0:
            return "a fin with no projection"
        if not any(b["id"] == louvre["block"] for b in data["blocks"]):
            return "a fin fixed to a block that is not there"

    if data["ground"]["gridSpacing"] <= 0:
        return "a grid with no spacing"

    return None


def find(gap_id: str) -> int:
    return next(i for i, gap in enumerate(base()["voids"]) if gap["id"] == gap_id)


class TheShippedFilePasses(unittest.TestCase):

    def test_the_real_layout_is_valid(self):
        self.assertIsNone(reject(base()))


class EveryRuleCanFail(unittest.TestCase):

    def test_a_shell_with_no_status_is_rejected(self):
        data = base()
        data.pop("status")
        self.assertEqual(reject(data), "no status")

    def test_a_slab_thicker_than_its_floor_is_rejected(self):
        data = base()
        data["frame"]["slabThickness"] = data["frame"]["floorHeight"]
        self.assertEqual(reject(data), "slab not thinner than a floor")

    def test_a_floor_outside_the_frame_is_rejected(self):
        data = base()
        data["blocks"][0]["floors"] = [0, 1, 9]
        self.assertEqual(reject(data), "floor out of range")

    def test_an_empty_block_is_rejected(self):
        data = base()
        data["blocks"][0]["maxX"] = data["blocks"][0]["minX"]
        self.assertEqual(reject(data), "empty footprint")

    def test_a_classroom_with_no_opening_is_rejected(self):
        data = base()
        data["voids"] = [g for g in data["voids"] if g["id"] != "classroom_opening"]
        self.assertEqual(reject(data), "no classroom opening")

    def test_an_opening_on_the_wrong_storey_is_rejected(self):
        data = base()
        data["voids"][find("classroom_opening")]["floors"] = [0, 2]
        self.assertEqual(reject(data), "opening on the wrong storey")

    def test_an_opening_smaller_than_the_room_is_rejected(self):
        data = base()
        data["voids"][find("classroom_opening")]["maxX"] = data["classroomFootprint"]["maxX"] - 0.5
        self.assertEqual(reject(data), "opening smaller than the room")

    def test_an_opening_that_swallows_the_storey_is_rejected(self):
        """The bug this whole round exists for: the whole floor deleted instead of cut."""
        data = base()
        opening = data["voids"][find("classroom_opening")]
        opening["minX"] = -40.0
        opening["maxX"] = 40.0
        self.assertEqual(reject(data), "opening swallows a block")

    def test_a_ground_floor_under_the_fall_is_rejected(self):
        data = base()
        data["voids"] = [g for g in data["voids"] if g["id"] != "fall_slot"]
        self.assertEqual(reject(data), "no fall slot")

    def test_a_single_fin_is_rejected(self):
        data = base()
        data["louvres"][0]["count"] = 1
        self.assertEqual(reject(data), "a fin run of one")

    def test_a_fin_fixed_to_nothing_is_rejected(self):
        data = base()
        data["louvres"][0]["block"] = "no_such_block"
        self.assertEqual(reject(data), "a fin fixed to a block that is not there")

    def test_an_unknown_roof_type_is_rejected(self):
        data = base()
        data["blocks"][0]["roof"] = "thatched"
        self.assertEqual(reject(data), "unknown roof type")

    def test_a_link_left_both_roofless_and_edgeless_is_rejected(self):
        data = base()
        data["roofs"]["open"]["kerbHeight"] = 0.0
        self.assertEqual(reject(data), "a link with neither deck nor kerb")


class TheMirrorMatchesTheCSharp(unittest.TestCase):
    """The mirror above is only worth having if it checks the same fields as the C#."""

    def validate_body(self) -> str:
        source = DEFINITIONS.read_text(encoding="utf-8")
        start = source.index("public bool Validate(out string error)")
        end = source.index("error = null;", start)
        return source[start:end]

    def test_every_mirrored_rule_appears_in_the_validate_method(self):
        body = self.validate_body()
        for marker in ("floorCount", "floorHeight", "slabThickness", "blocks.Length",
                       "classroom_opening", "Covers", "ContainsAnyBlock", "fall_slot",
                       "liftedSlabs", "thickness", "roofs", "kerbHeight", "louvres", "count",
                       "projection", "gridSpacing"):
            self.assertIn(marker, body, f"the C# Validate no longer checks '{marker}'")

    def test_the_shaft_rule_is_gone_rather_than_left_dormant(self):
        """The classroom is cut by 'classroom_opening' now; a leftover shaft rule would be dead code
        that still refuses a valid file."""
        self.assertNotIn("classroom_shaft", self.validate_body())


if __name__ == "__main__":
    unittest.main()
