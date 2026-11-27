"""Pins the verdict of tools/mesh_visibility_check.py.

Two things that a C# compile cannot catch and code review reliably misses:
  * a primitive wound inside-out, which Unity silently culls;
  * a drawn face mark placed behind the skin it is drawn on, which renders as nothing.

Both are geometry, so both are checkable offline against numbers copied from the C#.

Run: python3 -m unittest discover -s tests/ci -p 'test_*.py' -v
"""

from __future__ import annotations

import re
import subprocess
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "mesh_visibility_check.py"


def run() -> subprocess.CompletedProcess[str]:
    return subprocess.run([sys.executable, str(SCRIPT)], capture_output=True, text=True, check=False)


def require_numpy(test: unittest.TestCase, result: subprocess.CompletedProcess[str]) -> None:
    """Skip, loudly, when numpy is absent.

    The checker computes geometry with numpy. Without it the script cannot run, and the honest
    outcome is a skip that names the missing dependency: reporting it as a repository failure would
    be wrong, and letting it pass would be worse. CI installs numpy in the workflow so this fires
    only on a machine that has not.
    """
    if result.returncode != 0 and "No module named 'numpy'" in result.stderr:
        test.skipTest("numpy is not installed; mesh_visibility_check.py needs it to compute geometry")


class MeshVisibilityTests(unittest.TestCase):

    def test_every_primitive_is_visible_and_every_face_mark_clears_the_skin(self):
        result = run()
        require_numpy(self, result)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("PASS", result.stdout)

    def test_the_checker_can_still_fail(self):
        """A check that cannot fail proves nothing. Both known-bad cases must still be reported."""
        result = run()
        require_numpy(self, result)
        output = result.stdout
        # The regression box fan is deliberately wound inside-out: the cull branch has to fire on it,
        # or the check has never been able to fail and passing it means nothing.
        self.assertIn("CULLED from outside", output, "the inverted box fan should be reported as culled")

        # And every real mark must be reported as clearing the skin. "BURIED" is the failing verdict;
        # asserting on a string the script never prints would be a test that cannot fail either way.
        self.assertNotIn("BURIED", output, "a drawn face mark ended up behind the skin")
        self.assertIn("clear", output)


if __name__ == "__main__":
    unittest.main()
