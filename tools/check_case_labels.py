"""Every type name used in a `case` label has to be declared somewhere in Assets/.

A missing enum is the worst kind of break in this project: the Unity compiler would reject the
assembly, but nothing offline notices, because the only checks here read a mesh, a JSON script or a
.meta pairing. That is how `QuizScene` came to be referenced fourteen times with no definition
anywhere in the tree — the project did not compile, and every check passed.

This is deliberately narrow. A `case Something.Member:` label can only name an enum member, so a
qualifier that is not a declared enum is always a real error, with no false positives to tune. A
wider "is every type defined" check would need a symbol table and would start guessing.
"""

from __future__ import annotations

import argparse
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

# `case QuizScene.TeacherFirstClass:` -> "QuizScene". Nested labels and `case` with a when-clause
# are both covered; a label that is a bare constant (`case 3:`) has no qualifier and does not match.
CASE_QUALIFIER = re.compile(r"\bcase\s+([A-Z][A-Za-z0-9_]*)\s*\.")

DECLARED_TYPE = re.compile(
    r"\b(?:enum|class|struct|interface|record)\s+([A-Z][A-Za-z0-9_]*)"
)


def declared_types(files: dict[Path, str]) -> set[str]:
    names: set[str] = set()
    for text in files.values():
        names.update(DECLARED_TYPE.findall(text))
    return names


def sources(root: Path) -> dict[Path, str]:
    scripts = root / "Assets" / "Scripts"
    return {path: path.read_text(encoding="utf-8") for path in scripts.rglob("*.cs")}


def qualified_case_labels(files: dict[Path, str]) -> list[tuple[Path, str, int]]:
    found: list[tuple[Path, str, int]] = []
    for path, text in files.items():
        for number, line in enumerate(text.splitlines(), start=1):
            for match in CASE_QUALIFIER.finditer(line):
                found.append((path, match.group(1), number))
    return found


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--root", type=Path, default=ROOT,
                        help="repository root to inspect (default: the one this script lives in)")
    args = parser.parse_args()

    root = args.root.resolve()
    files = sources(root)
    declared = declared_types(files)
    labels = qualified_case_labels(files)

    missing: list[tuple[Path, str, int]] = [
        (path, name, line) for path, name, line in labels if name not in declared
    ]

    if missing:
        print(f"FAIL: {len(missing)} case label(s) name a type that is not declared in Assets/.")
        for path, name, line in missing:
            print(f"  {path.relative_to(root)}:{line}: case {name}.… — no enum/class named {name}")
        return 1

    print(f"PASS: all {len(labels)} qualified case label(s) name a declared type "
          f"({len(declared)} types declared under Assets/Scripts).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
