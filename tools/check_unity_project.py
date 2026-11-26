#!/usr/bin/env python3
"""Static checks for the Unity project, run before the Editor opens it.

These checks need no Unity install, so they run on every pull request. They catch the mistakes
that are cheap to see in the tree and expensive to see in an Editor import:

- generated folders or Visual Studio/Rider files committed to Git;
- an asset without its `.meta`, or a `.meta` whose asset is gone (GUID churn and broken references);
- duplicate or malformed GUIDs;
- JSON and `.asmdef` files that do not parse;
- CRLF line endings in Unity text assets (`.gitattributes` expects LF);
- the pinned Editor version drifting away from `ProjectSettings/ProjectVersion.txt`;
- dialogue data whose node references or effect strings would fail at runtime;
- the classroom layout drifting away from the brief (24 seats, 2 windows, 2 doors, a blackboard,
  a podium, openings that stay inside their wall and don't open onto furniture).

Usage:
    python3 tools/check_unity_project.py --check            # read the tracked files
    python3 tools/check_unity_project.py --check --github   # also emit ::error annotations
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path, PurePosixPath

REPO_ROOT = Path(__file__).resolve().parent.parent

VERSION_FILE = "ProjectSettings/ProjectVersion.txt"
WORKFLOW_FILE = ".github/workflows/unity-ci.yml"
DIALOGUE_DIRS = ("Assets/Data/Dialogue/", "Assets/Resources/Dialogue/")

FORBIDDEN_PREFIXES = (
    "Library/",
    "Temp/",
    "Obj/",
    "Build/",
    "Builds/",
    "Logs/",
    "UserSettings/",
    "MemoryCaptures/",
    "Recordings/",
    ".vs/",
    ".idea/",
    ".vscode/",
)
FORBIDDEN_SUFFIXES = (".csproj", ".sln", ".slnx", ".suo", ".user", ".userprefs")

TEXT_SUFFIXES = (".cs", ".asmdef", ".json", ".meta", ".unity", ".asset")

GUID_LINE = re.compile(r"^guid:\s*(\S+)\s*$", re.MULTILINE)
GUID_VALUE = re.compile(r"^[0-9a-f]{32}$")
VERSION_LINE = re.compile(r"^m_EditorVersion:\s*(\S+)\s*$", re.MULTILINE)
REVISION_LINE = re.compile(r"^m_EditorVersionWithRevision:\s*(\S+)\s+\(([0-9a-f]+)\)\s*$", re.MULTILINE)
UNITY_VERSION_KEY = re.compile(r"^\s*unityVersion:\s*(\S+)\s*$", re.MULTILINE)

CLASSROOM_LAYOUT_FILE = "Assets/Resources/World/classroom_layout.json"
CLASSROOM_WALLS = ("front", "back", "west", "east")

DIALOGUE_KINDS = {"line", "choice", "end"}
DIALOGUE_PRESENTATIONS = {"speech", "narration", "recollection", "imagination"}
DIALOGUE_ATTRIBUTES = {"Physique", "Psyche", "Intellect", "Perception", "Imagination"}
EFFECT_PREFIXES = ("objective:", "recollection:")


@dataclass(frozen=True)
class Problem:
    path: str
    message: str


def _is_under(path: str, prefix: str) -> bool:
    return path.startswith(prefix)


def check_forbidden_paths(files: dict[str, bytes]) -> list[Problem]:
    problems = []
    for path in files:
        if path.startswith(FORBIDDEN_PREFIXES) or path.endswith(FORBIDDEN_SUFFIXES):
            problems.append(Problem(path, "generated or local editor file is tracked; remove it from Git and keep it ignored"))
    return problems


def check_meta_pairs(files: dict[str, bytes]) -> list[Problem]:
    problems = []
    assets = {p for p in files if _is_under(p, "Assets/") and not p.endswith(".meta")}
    metas = {p for p in files if _is_under(p, "Assets/") and p.endswith(".meta")}

    for asset in sorted(assets):
        if asset + ".meta" not in metas:
            problems.append(Problem(asset, "asset has no tracked .meta file; Unity will create one and the GUID will churn"))

    # Git does not track directories, so a folder's .meta is described by the tracked files beneath it.
    directories: set[str] = set()
    for asset in assets:
        for ancestor in PurePosixPath(asset).parents:
            directories.add(str(ancestor))
    for meta in sorted(metas):
        described = meta[: -len(".meta")]
        if described in assets or described in directories:
            continue
        problems.append(Problem(meta, "orphan .meta: the asset it describes is not tracked"))

    return problems


def check_guids(files: dict[str, bytes]) -> list[Problem]:
    problems = []
    seen: dict[str, str] = {}
    for path, content in files.items():
        if not (_is_under(path, "Assets/") and path.endswith(".meta")):
            continue
        match = GUID_LINE.search(content.decode("utf-8", errors="replace"))
        if match is None:
            problems.append(Problem(path, "meta file has no guid line"))
            continue
        guid = match.group(1)
        if not GUID_VALUE.match(guid):
            problems.append(Problem(path, f"guid '{guid}' is not 32 lowercase hex digits"))
            continue
        if guid in seen:
            problems.append(Problem(path, f"guid {guid} duplicates {seen[guid]}"))
        else:
            seen[guid] = path
    return problems


def check_json_files(files: dict[str, bytes]) -> list[Problem]:
    problems = []
    for path, content in files.items():
        if not path.endswith((".json", ".asmdef")):
            continue
        if not (_is_under(path, "Assets/") or _is_under(path, "Packages/")):
            continue
        try:
            json.loads(content.decode("utf-8"))
        except (UnicodeDecodeError, json.JSONDecodeError) as error:
            problems.append(Problem(path, f"does not parse as JSON: {error}"))
    return problems


def check_line_endings(files: dict[str, bytes]) -> list[Problem]:
    problems = []
    for path, content in files.items():
        if _is_under(path, "Assets/") and path.endswith(TEXT_SUFFIXES) and b"\r" in content:
            problems.append(Problem(path, "contains CR characters; Unity text assets must use LF"))
    return problems


def check_editor_pin(files: dict[str, bytes]) -> list[Problem]:
    problems = []
    if VERSION_FILE not in files:
        return [Problem(VERSION_FILE, "ProjectVersion.txt is not tracked")]

    text = files[VERSION_FILE].decode("utf-8", errors="replace")
    version = VERSION_LINE.search(text)
    revision = REVISION_LINE.search(text)
    if version is None or revision is None:
        return [Problem(VERSION_FILE, "could not read m_EditorVersion and m_EditorVersionWithRevision")]

    pinned_version = version.group(1)
    if revision.group(1) != pinned_version:
        problems.append(Problem(VERSION_FILE, "m_EditorVersion and m_EditorVersionWithRevision disagree"))

    if WORKFLOW_FILE not in files:
        problems.append(Problem(WORKFLOW_FILE, "Unity CI workflow is not tracked"))
        return problems

    workflow = files[WORKFLOW_FILE].decode("utf-8", errors="replace")
    workflow_versions = UNITY_VERSION_KEY.findall(workflow)
    if not workflow_versions:
        problems.append(Problem(WORKFLOW_FILE, "no unityVersion is set; it must be pinned explicitly, not 'auto'"))
    for value in workflow_versions:
        if value != pinned_version:
            problems.append(
                Problem(WORKFLOW_FILE, f"unityVersion {value} does not match the project pin {pinned_version}")
            )
    return problems


def check_dialogue_data(files: dict[str, bytes]) -> list[Problem]:
    problems = []
    for path, content in files.items():
        if not (any(_is_under(path, prefix) for prefix in DIALOGUE_DIRS) and path.endswith(".json")):
            continue
        try:
            data = json.loads(content.decode("utf-8"))
        except (UnicodeDecodeError, json.JSONDecodeError):
            continue  # reported by check_json_files
        problems.extend(_check_one_dialogue(path, data))
    return problems


def _check_one_dialogue(path: str, data: object) -> list[Problem]:
    if not isinstance(data, dict) or not isinstance(data.get("nodes"), list):
        return [Problem(path, "dialogue must be an object with a 'nodes' array")]

    problems: list[Problem] = []
    nodes: dict[str, dict] = {}
    for node in data["nodes"]:
        node_id = node.get("id") if isinstance(node, dict) else None
        if not node_id:
            problems.append(Problem(path, "a node has no id"))
            continue
        if node_id in nodes:
            problems.append(Problem(path, f"node '{node_id}' is defined more than once"))
        nodes[node_id] = node

    start = data.get("startNode")
    if start not in nodes:
        problems.append(Problem(path, f"startNode '{start}' does not name a node"))

    for node_id, node in nodes.items():
        kind = node.get("kind")
        if kind not in DIALOGUE_KINDS:
            problems.append(Problem(path, f"node '{node_id}' has unknown kind '{kind}'"))
            continue
        if kind != "end" and node.get("presentation") not in DIALOGUE_PRESENTATIONS:
            problems.append(Problem(path, f"node '{node_id}' has unknown presentation '{node.get('presentation')}'"))
        problems.extend(_check_effects(path, node_id, node.get("effects")))

        if kind == "line":
            if node.get("next") not in nodes:
                problems.append(Problem(path, f"node '{node_id}' points to missing node '{node.get('next')}'"))
        elif kind == "choice":
            choices = node.get("choices")
            if not isinstance(choices, list) or not choices:
                problems.append(Problem(path, f"choice node '{node_id}' has no choices"))
                continue
            for choice in choices:
                if not isinstance(choice, dict) or not choice.get("text"):
                    problems.append(Problem(path, f"choice node '{node_id}' has a choice without text"))
                    continue
                if choice.get("next") not in nodes:
                    problems.append(Problem(path, f"choice in '{node_id}' points to missing node '{choice.get('next')}'"))
                check_attribute = choice.get("checkAttribute")
                failure_next = choice.get("failureNext")
                minimum_value = choice.get("minimumValue", 0)
                if check_attribute is not None:
                    if not isinstance(check_attribute, str) or check_attribute not in DIALOGUE_ATTRIBUTES:
                        problems.append(Problem(path, f"choice in '{node_id}' has unknown check attribute '{check_attribute}'"))
                    if not isinstance(minimum_value, int) or isinstance(minimum_value, bool) or not 1 <= minimum_value <= 5:
                        problems.append(Problem(path, f"choice in '{node_id}' check target must be an integer from 1 to 5"))
                    if failure_next not in nodes:
                        problems.append(Problem(path, f"check choice in '{node_id}' points to missing failure node '{failure_next}'"))
                elif failure_next is not None or minimum_value != 0:
                    problems.append(Problem(path, f"choice in '{node_id}' has check fields without checkAttribute"))
                problems.extend(_check_effects(path, node_id, choice.get("effects")))
    return problems


def check_classroom_layout(files: dict[str, bytes]) -> list[Problem]:
    """The classroom composition must keep the shape the runtime builder and the brief assume.

    Mirrors ClassroomLayout.Validate in Assets/Scripts/World/ClassroomLayout.cs. Keep the two in step:
    a rule that exists only in C# cannot run in CI without an Editor.
    """
    if CLASSROOM_LAYOUT_FILE not in files:
        return [Problem(CLASSROOM_LAYOUT_FILE, "the classroom layout is not tracked")]

    try:
        data = json.loads(files[CLASSROOM_LAYOUT_FILE].decode("utf-8"))
    except (UnicodeDecodeError, json.JSONDecodeError):
        return []  # reported by check_json_files

    problems: list[Problem] = []

    def fail(message: str) -> None:
        problems.append(Problem(CLASSROOM_LAYOUT_FILE, message))

    if not isinstance(data, dict):
        return [Problem(CLASSROOM_LAYOUT_FILE, "layout must be a JSON object")]

    if not data.get("status"):
        fail("layout has no 'status' field; prototype data must stay identifiable")

    room = data.get("room") or {}
    width = room.get("width", 0)
    depth = room.get("depth", 0)
    height = room.get("height", 0)
    if width < 6 or depth < 8 or height < 2.6 or room.get("wallThickness", 0) <= 0:
        fail("room is missing or too small for a 24-seat classroom (needs >= 6 x 8 x 2.6 m)")
        return problems
    min_x, max_x = -width / 2, width / 2
    center_z = room.get("centerZ", 0)
    min_z, max_z = center_z - depth / 2, center_z + depth / 2

    seating = data.get("seating") or {}
    columns = seating.get("columns") or []
    rows = seating.get("rows") or []
    seat_count = len(columns) * len(rows)
    if seat_count != seating.get("expectedSeatCount"):
        fail(f"seating grid implies {seat_count} seats but expectedSeatCount is {seating.get('expectedSeatCount')}")
    if seat_count != 24:
        fail(f"the brief asks for 24 seats; layout declares {seat_count}")

    desk_width = seating.get("deskWidth", 0)
    desk_depth = seating.get("deskDepth", 0)
    for column in columns:
        if column - desk_width / 2 < min_x + 0.35 or column + desk_width / 2 > max_x - 0.35:
            fail(f"seat column {column} leaves no aisle against the wall")
    for index, row in enumerate(rows):
        if row < min_z + 1.2 or row > max_z - 1.6:
            fail(f"seat row {row} leaves no room for the blackboard or the front aisle")
        if index and abs(row - rows[index - 1]) < desk_depth + 0.55:
            fail(f"seat rows {rows[index - 1]} and {row} leave no legroom")

    blackboard = data.get("blackboard") or {}
    if blackboard.get("width", 0) > width - 1.6:
        fail("blackboard does not fit the back wall")
    if blackboard.get("sillHeight", 0) + blackboard.get("height", 0) > height - 0.3:
        fail("blackboard is too tall for the room")

    podium = data.get("podium") or {}
    if not podium:
        fail("layout has no podium")
    wake_desk = data.get("wakeDesk") or {}
    if not wake_desk:
        fail("layout has no wake desk; the protagonist has nowhere to wake up")
    elif rows:
        first_chair_front = rows[0] + seating.get("chairOffsetZ", 0) - seating.get("chairSize", 0) / 2
        wake_desk_back = wake_desk.get("positionZ", 0) + wake_desk.get("depth", 0) / 2
        if first_chair_front - wake_desk_back < 0.7 - 1e-4:
            fail(
                f"only {first_chair_front - wake_desk_back:.2f} m between the protagonist's desk and the "
                "first chair; 0.70 m of standing room is the minimum"
            )

    windows = data.get("windows") or []
    doors = data.get("doors") or []
    if len(windows) != 2:
        fail(f"the brief asks for two windows; layout declares {len(windows)}")
    if len(doors) != 2:
        fail(f"the brief asks for two doors; layout declares {len(doors)}")
    if not data.get("ceilingLights"):
        fail("layout has no ceiling lights")

    wall_length = lambda wall: depth if wall in ("west", "east") else width
    along_origin = lambda wall: min_z if wall in ("west", "east") else min_x

    for kind, openings in (("window", windows), ("door", doors)):
        ids = set()
        for opening in openings:
            if not isinstance(opening, dict) or not opening.get("id"):
                fail(f"a {kind} has no id")
                continue
            opening_id = opening["id"]
            if opening_id in ids:
                fail(f"{kind} id '{opening_id}' is used twice")
            ids.add(opening_id)
            wall = opening.get("wall")
            if wall not in CLASSROOM_WALLS:
                fail(f"{kind} '{opening_id}' names unknown wall '{wall}'")
                continue
            length = wall_length(wall)
            opening_width = opening.get("width", 0)
            center = opening.get("centerAlong", 0)
            if opening_width <= 0.4 or opening_width >= length - 0.6:
                fail(f"{kind} '{opening_id}' is too wide for its wall")
            if center - opening_width / 2 < 0.3 or center + opening_width / 2 > length - 0.3:
                fail(f"{kind} '{opening_id}' runs into a corner")
            if opening.get("sillHeight", 0) + opening.get("height", 0) > height - 0.15:
                fail(f"{kind} '{opening_id}' is taller than the room")

            if kind != "door" or wall not in ("front", "back"):
                continue
            world_from = along_origin(wall) + center - opening_width / 2
            world_to = along_origin(wall) + center + opening_width / 2
            if wall == "back":
                board_from = -blackboard.get("width", 0) / 2
                board_to = blackboard.get("width", 0) / 2
                if world_from < board_to + 0.3 and world_to > board_from - 0.3:
                    fail(f"door '{opening_id}' opens onto the blackboard")
            else:
                desk_from = wake_desk.get("positionX", 0) - wake_desk.get("width", 0) / 2 - 0.4
                desk_to = wake_desk.get("positionX", 0) + wake_desk.get("width", 0) / 2 + 0.4
                if world_from < desk_to and world_to > desk_from:
                    fail(f"door '{opening_id}' opens onto the protagonist's desk")

    for wall in CLASSROOM_WALLS:
        on_wall = sorted(
            (o for o in windows + doors if isinstance(o, dict) and o.get("wall") == wall),
            key=lambda o: o.get("centerAlong", 0),
        )
        for previous, current in zip(on_wall, on_wall[1:]):
            gap = (current["centerAlong"] - current["width"] / 2) - (previous["centerAlong"] + previous["width"] / 2)
            if gap < 0.4:
                fail(f"{previous['id']} and {current['id']} are only {gap:.2f} m apart on the {wall} wall")

    return problems


def _check_effects(path: str, node_id: str, effects: object) -> list[Problem]:
    if effects is None:
        return []
    if not isinstance(effects, list):
        return [Problem(path, f"node '{node_id}' effects must be a list")]
    problems = []
    for effect in effects:
        if not isinstance(effect, str) or not effect.startswith(EFFECT_PREFIXES):
            problems.append(Problem(path, f"node '{node_id}' has unknown effect {effect!r}"))
            continue
        flag = effect.split(":", 1)[1]
        if not flag.strip():
            problems.append(Problem(path, f"node '{node_id}' has an effect with no flag name"))
    return problems


def run_checks(files: dict[str, bytes]) -> list[Problem]:
    problems: list[Problem] = []
    problems += check_forbidden_paths(files)
    problems += check_meta_pairs(files)
    problems += check_guids(files)
    problems += check_json_files(files)
    problems += check_line_endings(files)
    problems += check_editor_pin(files)
    problems += check_dialogue_data(files)
    problems += check_classroom_layout(files)
    return problems


def read_tracked_files(root: Path) -> dict[str, bytes]:
    listing = subprocess.run(
        ["git", "ls-files", "-z"],
        cwd=root,
        check=True,
        capture_output=True,
    ).stdout.decode("utf-8")
    files: dict[str, bytes] = {}
    for path in filter(None, listing.split("\0")):
        target = root / PurePosixPath(path)
        if target.is_file():
            files[path] = target.read_bytes()
        else:
            files[path] = b""
            # A tracked file missing from the working tree is a real problem; report it as empty content.
    return files


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--check", action="store_true", help="run every check and fail on any problem")
    parser.add_argument("--github", action="store_true", help="also print ::error annotations for GitHub Actions")
    parser.add_argument("--root", type=Path, default=REPO_ROOT, help=argparse.SUPPRESS)
    args = parser.parse_args(argv)

    if not args.check:
        parser.print_help()
        return 2

    files = read_tracked_files(args.root)
    problems = run_checks(files)

    unity_files = sum(1 for p in files if p.startswith("Assets/"))
    print(f"checked {len(files)} tracked file(s), {unity_files} under Assets/")
    for problem in problems:
        print(f"FAIL {problem.path}: {problem.message}")
        if args.github:
            print(f"::error file={problem.path}::{problem.message}")

    if problems:
        print(f"unity project check failed with {len(problems)} problem(s)")
        return 1
    print("unity project check passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
