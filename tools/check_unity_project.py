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
- dialogue data whose node references or effect strings would fail at runtime.

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
DIALOGUE_DIR = "Assets/Data/Dialogue"

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

DIALOGUE_KINDS = {"line", "choice", "end"}
DIALOGUE_PRESENTATIONS = {"speech", "narration", "recollection", "imagination"}
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
        if not (_is_under(path, DIALOGUE_DIR + "/") and path.endswith(".json")):
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
                problems.extend(_check_effects(path, node_id, choice.get("effects")))
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
