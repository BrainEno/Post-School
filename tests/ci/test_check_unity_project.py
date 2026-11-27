"""Pins the verdicts of tools/check_unity_project.py with synthetic trees.

A checker that always passes would be invisible in CI, so each rule has a case that must fail
and a case that must pass. Run with: python3 -m unittest discover -s tests/ci -t . -v
"""

from __future__ import annotations

import json
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))

import check_unity_project as checker  # noqa: E402

LAYOUT = json.loads((ROOT / checker.CLASSROOM_LAYOUT_FILE).read_text())

GUID_A = "0123456789abcdef0123456789abcdef"
GUID_B = "fedcba9876543210fedcba9876543210"
GUID_C = "00112233445566778899001122334455"
GUID_D = "55443322110099887766554433221100"


def meta(guid: str) -> bytes:
    return f"fileFormatVersion: 2\nguid: {guid}\n".encode()


def pinned_project(**extra: bytes) -> dict[str, bytes]:
    files = {
        "ProjectSettings/ProjectVersion.txt": (
            b"m_EditorVersion: 6000.3.29f1\n"
            b"m_EditorVersionWithRevision: 6000.3.29f1 (96d2fe17529c)\n"
        ),
        ".github/workflows/unity-ci.yml": b"        with:\n          unityVersion: 6000.3.29f1\n",
        "Assets/Scripts/Core/Player.cs": b"class Player {}\n",
        "Assets/Scripts/Core/Player.cs.meta": meta(GUID_A),
        "Assets/Scripts/Core.meta": meta(GUID_B),
        checker.CLASSROOM_LAYOUT_FILE: json.dumps(LAYOUT).encode(),
        checker.CLASSROOM_LAYOUT_FILE + ".meta": meta(GUID_C),
        "Assets/Resources/World.meta": meta(GUID_D),
    }
    files.update({k: v for k, v in extra.items()})
    return files


def messages(problems: list[checker.Problem]) -> str:
    return "\n".join(f"{p.path}: {p.message}" for p in problems)


class MetaPairTests(unittest.TestCase):
    def test_minimal_pinned_project_passes(self) -> None:
        self.assertEqual([], checker.run_checks(pinned_project()))

    def test_asset_without_meta_fails(self) -> None:
        files = pinned_project()
        del files["Assets/Scripts/Core/Player.cs.meta"]
        problems = checker.check_meta_pairs(files)
        self.assertEqual(["Assets/Scripts/Core/Player.cs"], [p.path for p in problems])

    def test_orphan_meta_fails(self) -> None:
        files = pinned_project()
        files["Assets/Old.cs.meta"] = meta("00000000000000000000000000000001")
        problems = checker.check_meta_pairs(files)
        self.assertEqual(["Assets/Old.cs.meta"], [p.path for p in problems])

    def test_folder_meta_for_tracked_directory_is_not_an_orphan(self) -> None:
        files = pinned_project()
        self.assertEqual([], checker.check_meta_pairs(files))


class GuidTests(unittest.TestCase):
    def test_duplicate_guid_fails(self) -> None:
        files = pinned_project()
        files["Assets/Scripts/Core/Other.cs"] = b"class Other {}\n"
        files["Assets/Scripts/Core/Other.cs.meta"] = meta(GUID_A)
        problems = checker.check_guids(files)
        self.assertEqual(1, len(problems))
        self.assertIn("duplicates", problems[0].message)

    def test_malformed_guid_fails(self) -> None:
        files = pinned_project()
        files["Assets/Scripts/Core/Player.cs.meta"] = b"guid: NOT-A-GUID\n"
        self.assertEqual(1, len(checker.check_guids(files)))


class ForbiddenAndFormatTests(unittest.TestCase):
    def test_library_and_solution_files_fail(self) -> None:
        files = pinned_project(**{
            "Library/ScriptAssemblies/x.dll": b"",
            "PostSchool.csproj": b"<Project/>",
        })
        paths = {p.path for p in checker.check_forbidden_paths(files)}
        self.assertEqual({"Library/ScriptAssemblies/x.dll", "PostSchool.csproj"}, paths)

    def test_crlf_in_unity_script_fails(self) -> None:
        files = pinned_project(**{"Assets/Scripts/Core/Player.cs": b"class Player {}\r\n"})
        self.assertEqual(1, len(checker.check_line_endings(files)))

    def test_invalid_asmdef_json_fails(self) -> None:
        files = pinned_project(**{"Assets/Scripts/PostSchool.Runtime.asmdef": b"{ \"name\": "})
        problems = checker.check_json_files(files)
        self.assertEqual(["Assets/Scripts/PostSchool.Runtime.asmdef"], [p.path for p in problems])


class EditorPinTests(unittest.TestCase):
    def test_workflow_version_mismatch_fails(self) -> None:
        files = pinned_project(**{".github/workflows/unity-ci.yml": b"          unityVersion: 6000.0.1f1\n"})
        self.assertIn("does not match", messages(checker.check_editor_pin(files)))

    def test_auto_version_fails(self) -> None:
        files = pinned_project(**{".github/workflows/unity-ci.yml": b"          unityVersion: auto\n"})
        self.assertEqual(1, len(checker.check_editor_pin(files)))

    def test_missing_workflow_version_fails(self) -> None:
        files = pinned_project(**{".github/workflows/unity-ci.yml": b"name: x\n"})
        self.assertIn("no unityVersion", messages(checker.check_editor_pin(files)))


class DialogueDataTests(unittest.TestCase):
    PATH = "Assets/Data/Dialogue/sample.json"

    def _run(self, text: str) -> list[checker.Problem]:
        return checker.check_dialogue_data({self.PATH: text.encode("utf-8")})

    def test_valid_graph_passes(self) -> None:
        text = """{
          "startNode": "a",
          "nodes": [
            {"id": "a", "kind": "line", "presentation": "narration", "text": "x", "next": "e", "effects": ["objective:f"]},
            {"id": "e", "kind": "end", "effects": []}
          ]
        }"""
        self.assertEqual([], self._run(text))

    def test_resource_dialogue_graph_is_checked(self) -> None:
        text = """{"startNode": "a", "nodes": [
          {"id": "a", "kind": "line", "presentation": "narration", "text": "x", "next": "missing"}]}"""
        problems = checker.check_dialogue_data({"Assets/Resources/Dialogue/sample.json": text.encode()})
        self.assertIn("missing node 'missing'", messages(problems))

    def test_dangling_next_fails(self) -> None:
        text = """{"startNode": "a", "nodes": [
          {"id": "a", "kind": "line", "presentation": "narration", "text": "x", "next": "nowhere"}]}"""
        self.assertIn("missing node 'nowhere'", messages(self._run(text)))

    def test_unknown_effect_prefix_fails(self) -> None:
        text = """{"startNode": "a", "nodes": [
          {"id": "a", "kind": "end", "effects": ["memory:x"]}]}"""
        self.assertIn("unknown effect", messages(self._run(text)))

    def test_choice_without_choices_fails(self) -> None:
        text = """{"startNode": "a", "nodes": [
          {"id": "a", "kind": "choice", "presentation": "narration", "choices": []}]}"""
        self.assertIn("no choices", messages(self._run(text)))

    def test_attribute_check_choice_passes_when_both_branches_exist(self) -> None:
        text = """{"startNode": "pick", "nodes": [
          {"id": "pick", "kind": "choice", "presentation": "narration", "choices": [
            {"text": "try", "next": "success", "checkAttribute": "Imagination", "minimumValue": 3, "failureNext": "failure"}]},
          {"id": "success", "kind": "end", "effects": []},
          {"id": "failure", "kind": "end", "effects": []}]}"""
        self.assertEqual([], self._run(text))

    def test_attribute_check_rejects_unknown_attribute_and_missing_failure_branch(self) -> None:
        text = """{"startNode": "pick", "nodes": [
          {"id": "pick", "kind": "choice", "presentation": "narration", "choices": [
            {"text": "try", "next": "success", "checkAttribute": "Luck", "minimumValue": 3, "failureNext": "missing"}]},
          {"id": "success", "kind": "end", "effects": []}]}"""
        result = messages(self._run(text))
        self.assertIn("unknown check attribute", result)
        self.assertIn("missing failure node", result)

    def test_attribute_check_rejects_out_of_range_target(self) -> None:
        text = """{"startNode": "pick", "nodes": [
          {"id": "pick", "kind": "choice", "presentation": "narration", "choices": [
            {"text": "try", "next": "success", "checkAttribute": "Imagination", "minimumValue": 6, "failureNext": "failure"}]},
          {"id": "success", "kind": "end", "effects": []},
          {"id": "failure", "kind": "end", "effects": []}]}"""
        self.assertIn("check target must be an integer", messages(self._run(text)))


if __name__ == "__main__":
    unittest.main()

def classroom_layout(**overrides: object) -> dict:
    """A copy of the real classroom layout with targeted edits applied to a dotted path."""
    import copy

    data = copy.deepcopy(LAYOUT)
    for key, value in overrides.items():
        target = data
        parts = key.split("__")
        for part in parts[:-1]:
            target = target[int(part)] if isinstance(target, list) else target[part]
        last = parts[-1]
        if isinstance(target, list):
            target[int(last)] = value
        else:
            target[last] = value
    return data


def layout_files(**overrides: object) -> dict[str, bytes]:
    files = pinned_project()
    files[checker.CLASSROOM_LAYOUT_FILE] = json.dumps(classroom_layout(**overrides)).encode()
    return files


class ClassroomLayoutTests(unittest.TestCase):
    def test_shipped_layout_passes(self) -> None:
        self.assertEqual([], checker.check_classroom_layout(pinned_project()))

    def test_missing_layout_fails(self) -> None:
        files = pinned_project()
        del files[checker.CLASSROOM_LAYOUT_FILE]
        problems = checker.check_classroom_layout(files)
        self.assertEqual([checker.CLASSROOM_LAYOUT_FILE], [p.path for p in problems])

    def test_seat_count_must_be_24(self) -> None:
        files = layout_files()
        layout = json.loads(files[checker.CLASSROOM_LAYOUT_FILE])
        layout["seating"]["columns"] = layout["seating"]["columns"][:5]
        files[checker.CLASSROOM_LAYOUT_FILE] = json.dumps(layout).encode()
        problems = checker.check_classroom_layout(files)
        self.assertIn("24", messages(problems))
        self.assertIn("implies 20 seats", messages(problems))

    def test_two_windows_and_two_doors_are_required(self) -> None:
        files = layout_files()
        layout = json.loads(files[checker.CLASSROOM_LAYOUT_FILE])
        layout["doors"] = layout["doors"][:1]
        files[checker.CLASSROOM_LAYOUT_FILE] = json.dumps(layout).encode()
        self.assertIn("two doors", messages(checker.check_classroom_layout(files)))

    def test_opening_running_into_a_corner_fails(self) -> None:
        problems = checker.check_classroom_layout(layout_files(windows__1__centerAlong=0.4))
        self.assertIn("runs into a corner", messages(problems))

    def test_door_onto_the_blackboard_fails(self) -> None:
        problems = checker.check_classroom_layout(layout_files(doors__0__centerAlong=6.2))
        self.assertIn("opens onto the blackboard", messages(problems))

    def test_door_onto_the_protagonist_desk_fails(self) -> None:
        problems = checker.check_classroom_layout(layout_files(doors__1__centerAlong=6.2))
        self.assertIn("opens onto the protagonist's desk", messages(problems))

    def test_crowded_wake_desk_fails(self) -> None:
        problems = checker.check_classroom_layout(layout_files(wakeDesk__positionZ=2.0))
        self.assertIn("standing room is the minimum", messages(problems))

    def test_layout_without_status_fails(self) -> None:
        problems = checker.check_classroom_layout(layout_files(status=""))
        self.assertIn("no 'status' field", messages(problems))



class MissingFrontWallTests(unittest.TestCase):
    """The exit side is a curtain and a drop. Each rule here is one way to lose that."""

    def test_shipped_layout_has_a_missing_front_wall(self) -> None:
        self.assertEqual([], checker.check_classroom_layout(pinned_project()))

    def test_a_front_wall_is_rejected(self) -> None:
        problems = checker.check_classroom_layout(layout_files(frontWall__exists=True))
        self.assertIn("must not exist", messages(problems))

    def test_a_half_length_curtain_is_rejected(self) -> None:
        # A curtain that stops at the sill is a blind, and the exit side would read as a window.
        problems = checker.check_classroom_layout(layout_files(frontWall__curtainBottom=1.0))
        self.assertIn("floor to ceiling", messages(problems))

    def test_a_narrow_curtain_is_rejected(self) -> None:
        problems = checker.check_classroom_layout(layout_files(frontWall__curtainWidth=3.0))
        self.assertIn("too narrow", messages(problems))

    def test_moving_the_exit_door_off_the_front_wall_is_rejected(self) -> None:
        problems = checker.check_classroom_layout(layout_files(doors__1__wall="east"))
        self.assertIn("exit door", messages(problems))

    def test_a_container_under_the_wrong_part_of_the_doorway_is_rejected(self) -> None:
        problems = checker.check_classroom_layout(layout_files(fall__containerCenterX=9.0))
        self.assertIn("does not open above the container", messages(problems))

    def test_a_container_inside_the_room_is_rejected(self) -> None:
        problems = checker.check_classroom_layout(layout_files(fall__containerCenterZ=-5.0))
        self.assertIn("beyond the front wall", messages(problems))

    def test_a_street_with_one_building_is_rejected(self) -> None:
        problems = checker.check_classroom_layout(layout_files(fall__buildingHeights=[9.0]))
        self.assertIn("three buildings", messages(problems))

    def test_a_washing_line_below_the_container_is_rejected(self) -> None:
        # The line is a world height (the street is at -4.2), so this puts the washing level with the lid.
        problems = checker.check_classroom_layout(layout_files(laundry__lineY=-3.5))
        self.assertIn("below the container top", messages(problems))

    def test_two_curtained_windows_are_rejected(self) -> None:
        problems = checker.check_classroom_layout(layout_files(windows__0__treatment="curtain"))
        self.assertIn("one bare 'dusk' window", messages(problems))

    def test_a_window_with_an_unknown_treatment_is_rejected(self) -> None:
        problems = checker.check_classroom_layout(layout_files(windows__1__treatment="blinds"))
        self.assertIn("one bare 'dusk' window", messages(problems))
