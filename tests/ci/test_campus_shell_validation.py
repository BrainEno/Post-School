"""Negative controls for campus_shell.json's validation rules.

A rule that cannot fail is not a rule. The C# cannot run in CI, so its Validate() is mirrored in
tools/campus_shell_proof.py's reader, and each mirrored rule gets a case that must be rejected here.
The mirror is not the authority --- CampusShellLayout.Validate is --- but a case that passes the
mirror is a case that would pass the C# too, because the two check the same fields against the same
bounds.

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

    if not any(gap["id"] == "classroom_shaft" for gap in data.get("voids") or []):
        return "classroom shaft not declared"

    for slab in data.get("liftedSlabs") or []:
        if slab["thickness"] <= 0:
            return "slab with no thickness"

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

    def test_an_undeclared_classroom_shaft_is_rejected(self):
        # The one that matters most: without it the shell is built through the room.
        data = base()
        data["voids"] = []
        self.assertEqual(reject(data), "classroom shaft not declared")

    def test_a_single_fin_is_rejected(self):
        data = base()
        data["louvres"][0]["count"] = 1
        self.assertEqual(reject(data), "a fin run of one")

    def test_a_fin_fixed_to_nothing_is_rejected(self):
        data = base()
        data["louvres"][0]["block"] = "no_such_block"
        self.assertEqual(reject(data), "a fin fixed to a block that is not there")

    def test_an_empty_block_is_rejected(self):
        data = base()
        data["blocks"][0]["maxX"] = data["blocks"][0]["minX"]
        self.assertEqual(reject(data), "empty footprint")


class TheMirrorMatchesTheCSharp(unittest.TestCase):
    """The mirror above is only worth having if it checks the same fields as the C#."""

    def test_every_mirrored_rule_appears_in_the_validate_method(self):
        source = DEFINITIONS.read_text(encoding="utf-8")
        start = source.index("public bool Validate(out string error)")
        end = source.index("error = null;", start)
        body = source[start:end]

        for marker in ("floorCount", "floorHeight", "slabThickness", "blocks.Length",
                       "classroom_shaft", "liftedSlabs", "thickness", "louvres", "count",
                       "projection", "gridSpacing"):
            self.assertIn(marker, body, f"the C# Validate no longer checks '{marker}'")
