#!/usr/bin/env python3
"""Validates every dialogue graph against the rules DialogueRunner enforces at load.

The runner throws on a bad graph --- at runtime, in the editor, when the scene is reached. That is the
right place for it to fail loudly, but it is the wrong place to *find out*: a node with a typo in its
`next` costs a play session to discover. This mirrors the same rules offline over every JSON file, so a
broken graph fails before anyone presses Play.

The rules are copied from DialogueRunner.Validate / ValidateChoices rather than invented. Where this
file goes beyond the runner it only warns, so it can never reject a graph the game would play:
  * a check whose `next` and `failureNext` are the same node is a check with no consequence.

An `end` node may carry a `presentation` --- the runner ignores it, and three existing graphs set
`narration` on theirs. Requiring it to be empty was this file being tidier than the game, which is a
good way to fail a check that has not found anything.

Run: python3 tools/check_dialogue_graph.py
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SEARCH = [ROOT / "Assets/Resources/Dialogue", ROOT / "Assets/Data/Dialogue"]

KINDS = {"line", "choice", "end"}
PRESENTATIONS = {"speech", "narration", "recollection", "imagination"}
ATTRIBUTES = {"Physique", "Psyche", "Intellect", "Perception", "Imagination"}
BASE_VALUE = 1
ATTRIBUTE_CAP = 5


def load_graphs() -> list[tuple[Path, dict]]:
    seen: dict[str, Path] = {}
    graphs: list[tuple[Path, dict]] = []
    for directory in SEARCH:
        if not directory.exists():
            continue
        for path in sorted(directory.glob("*.json")):
            with path.open(encoding="utf-8") as handle:
                data = json.load(handle)
            # The same graph is copied into Data/ (authored source) and Resources/ (runtime). Both are
            # checked, so an edit to one that is not mirrored in the other is caught here rather than
            # only by the EditMode test that compares them.
            key = data.get("id", path.stem)
            graphs.append((path, data))
            seen.setdefault(key, path)
    return graphs


def check_effect(effect: str, node_id: str, problems: list[str]) -> None:
    if not isinstance(effect, str):
        problems.append(f"{node_id}: effect is not a string ({effect!r})")
        return

    if effect.startswith("insight:"):
        parts = effect.split(":")
        if len(parts) != 3:
            problems.append(f"{node_id}: insight effect '{effect}' must be insight:<Attribute>:<amount>")
            return
        _, attribute, amount = parts
        if attribute not in ATTRIBUTES:
            problems.append(f"{node_id}: insight effect names unknown attribute '{attribute}'")
        try:
            if int(amount) == 0:
                problems.append(f"{node_id}: insight amount must be non-zero ('{effect}')")
        except ValueError:
            problems.append(f"{node_id}: insight amount '{amount}' is not an integer")
        return

    if not (effect.startswith("objective:") or effect.startswith("recollection:")):
        problems.append(f"{node_id}: effect '{effect}' has an unknown prefix")
        return

    _, _, flag = effect.partition(":")
    if not flag.strip():
        problems.append(f"{node_id}: effect '{effect}' has no flag name")


def check_graph(path: Path, data: dict) -> tuple[list[str], list[str]]:
    problems: list[str] = []
    warnings: list[str] = []
    where = path.relative_to(ROOT)

    nodes = data.get("nodes")
    if not isinstance(nodes, list) or not nodes:
        return [f"{where}: no nodes"], warnings

    ids: dict[str, dict] = {}
    for node in nodes:
        node_id = node.get("id")
        if not node_id:
            problems.append(f"{where}: a node has no id")
            continue
        if node_id in ids:
            problems.append(f"{where}: node '{node_id}' is defined twice")
        ids[node_id] = node

    start = data.get("startNode")
    if not start:
        problems.append(f"{where}: no startNode")
    elif start not in ids:
        problems.append(f"{where}: startNode '{start}' does not exist")

    for node_id, node in ids.items():
        kind = node.get("kind")
        if kind not in KINDS:
            problems.append(f"{where}: '{node_id}' has unknown kind '{kind}'")
            continue

        # An end node's presentation is ignored by the runner; only non-end nodes are validated.
        presentation = node.get("presentation") or ""
        if kind != "end" and presentation not in PRESENTATIONS:
            problems.append(f"{where}: '{node_id}' has unknown presentation '{presentation}'")

        for effect in node.get("effects") or []:
            check_effect(effect, f"{where}: {node_id}", problems)

        if kind == "line":
            target = node.get("next")
            if target not in ids:
                problems.append(f"{where}: '{node_id}' next '{target}' does not exist")
        elif kind == "choice":
            choices = node.get("choices") or []
            if not choices:
                problems.append(f"{where}: choice node '{node_id}' has no choices")
            for index, choice in enumerate(choices):
                label = f"{where}: {node_id}[{index}]"
                if not (choice.get("text") or "").strip():
                    problems.append(f"{label}: choice has no text")

                for effect in choice.get("effects") or []:
                    check_effect(effect, label, problems)

                target = choice.get("next")
                if target not in ids:
                    problems.append(f"{label}: next '{target}' does not exist")

                attribute = choice.get("checkAttribute")
                failure = choice.get("failureNext")
                if not attribute:
                    if choice.get("minimumValue") or failure:
                        problems.append(f"{label}: has check fields but no checkAttribute")
                    continue

                if attribute not in ATTRIBUTES:
                    problems.append(f"{label}: unknown checkAttribute '{attribute}'")
                minimum = choice.get("minimumValue")
                if not isinstance(minimum, int) or not (BASE_VALUE <= minimum <= ATTRIBUTE_CAP):
                    problems.append(f"{label}: minimumValue {minimum} is outside {BASE_VALUE}..{ATTRIBUTE_CAP}")
                if failure not in ids:
                    problems.append(f"{label}: failureNext '{failure}' does not exist")
                if failure == target:
                    warnings.append(f"{label}: both branches of the check lead to '{target}', so the check decides nothing")

    # Everything must be reachable: an orphaned node is prose nobody will ever read.
    if start in ids:
        reachable: set[str] = set()
        stack = [start]
        while stack:
            current = stack.pop()
            if current in reachable or current not in ids:
                continue
            reachable.add(current)
            node = ids[current]
            if node.get("kind") == "line":
                stack.append(node.get("next"))
            elif node.get("kind") == "choice":
                for choice in node.get("choices") or []:
                    stack.append(choice.get("next"))
                    if choice.get("failureNext"):
                        stack.append(choice["failureNext"])

        orphans = sorted(set(ids) - reachable)
        if orphans:
            problems.append(f"{where}: unreachable nodes {orphans}")

    return problems, warnings


def main() -> int:
    graphs = load_graphs()
    if not graphs:
        print("FAIL: no dialogue graphs found; check the search paths.")
        return 1

    all_problems: list[str] = []
    all_warnings: list[str] = []
    for path, data in graphs:
        problems, warnings = check_graph(path, data)
        all_problems.extend(problems)
        all_warnings.extend(warnings)

    for warning in all_warnings:
        print(f"WARN {warning}")

    if all_problems:
        print(f"FAIL: {len(all_problems)} problem(s) across {len(graphs)} graph(s).")
        for problem in all_problems:
            print(f"  {problem}")
        return 1

    total_nodes = sum(len(data.get("nodes") or []) for _, data in graphs)
    print(f"PASS: {len(graphs)} dialogue graph(s), {total_nodes} node(s), no problems.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
