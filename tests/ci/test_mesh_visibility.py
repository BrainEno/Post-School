"""Pins the verdict of tools/mesh_visibility_check.py.

Two things that a C# compile cannot catch and code review reliably misses:
  * a primitive wound inside-out, which Unity silently culls;
  * a drawn face mark placed behind the skin it is drawn on, which renders as nothing.

Both are geometry, so both are checkable offline against numbers copied from the C#.

Run: python3 -m pytest tests/ci -q
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "mesh_visibility_check.py"


def run() -> subprocess.CompletedProcess[str]:
    return subprocess.run([sys.executable, str(SCRIPT)], capture_output=True, text=True, check=False)


def test_every_primitive_is_visible_and_every_face_mark_clears_the_skin() -> None:
    result = run()
    assert result.returncode == 0, result.stdout + result.stderr
    assert "PASS" in result.stdout


def test_the_checker_can_still_fail() -> None:
    """A check that cannot fail proves nothing. Both known-bad cases must still be reported."""
    output = run().stdout
    assert "CULLED from outside" in output, "the inverted box fan should be reported as culled"
