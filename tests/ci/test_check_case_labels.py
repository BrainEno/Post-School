"""Pins tools/check_case_labels.py, and with it the compile break it exists to catch.

`QuizScene` was referenced fourteen times across PrototypeSceneBootstrap.cs and declared nowhere.
The project could not compile; every offline check passed, because none of them had any opinion
about whether the types a script mentions exist. This test keeps the guard honest in both
directions: it must pass on the real tree, and it must fail when a case label names a type that is
not there.

Run: python3 -m unittest discover -s tests/ci -p 'test_*.py' -v
"""

from __future__ import annotations

import subprocess
import tempfile
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "check_case_labels.py"


def run() -> subprocess.CompletedProcess[str]:
    return subprocess.run([sys.executable, str(SCRIPT)], capture_output=True, text=True, check=False)


class CaseLabelTests(unittest.TestCase):

    def test_every_case_label_names_a_declared_type(self):
        result = run()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("PASS", result.stdout)
        # A checker that examines nothing would also print PASS; the count proves it looked.
        self.assertIn("qualified case label(s)", result.stdout)
        self.assertNotIn("all 0 qualified case label(s)", result.stdout)

    def test_the_enum_the_prologue_switches_on_is_declared(self):
        # The specific break, stated as a fact about the tree rather than just as a passing script.
        source = (ROOT / "Assets/Scripts/Prototype/PrototypeSceneBootstrap.cs").read_text(encoding="utf-8")
        self.assertIn("QuizScene.", source)
        self.assertTrue((ROOT / "Assets/Scripts/Prototype/QuizScene.cs").exists())
        enum_source = (ROOT / "Assets/Scripts/Prototype/QuizScene.cs").read_text(encoding="utf-8")
        for member in ("ClassroomOpening", "TeacherFirstClass", "TeacherRevisit", "ImaginedBiologyTeacher"):
            self.assertIn(member, enum_source, f"QuizScene.{member} is used by the bootstrap but not declared")

    def test_the_checker_reports_missing_types_rather_than_passing_silently(self):
        # Negative control against a scratch tree: a lone case label naming a type nobody declares.
        # Without this, a checker whose FAIL branch never fires would look exactly like a passing one.
        with tempfile.TemporaryDirectory() as tmp:
            scripts = Path(tmp) / "Assets" / "Scripts"
            scripts.mkdir(parents=True)
            (scripts / "Broken.cs").write_text(
                "class Broken { void F(object o) { switch (o) { case Missing.Thing: break; } } }",
                encoding="utf-8",
            )

            result = subprocess.run(
                [sys.executable, str(SCRIPT), "--root", tmp],
                capture_output=True, text=True, check=False,
            )

        self.assertEqual(result.returncode, 1, "a case label naming an undeclared type must fail the check")
        self.assertIn("FAIL", result.stdout)
        self.assertIn("Missing", result.stdout)


if __name__ == "__main__":
    unittest.main()
