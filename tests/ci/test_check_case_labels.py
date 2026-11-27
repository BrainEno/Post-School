"""Pins tools/check_case_labels.py, and with it the compile break it exists to catch.

`QuizScene` was referenced fourteen times across PrototypeSceneBootstrap.cs and declared nowhere.
The project could not compile; every offline check passed, because none of them had any opinion
about whether the types a script mentions exist. This test keeps the guard honest in both
directions: it must pass on the real tree, and it must fail when a case label names a type that is
not there.

Run: python3 -m pytest tests/ci -q
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "tools" / "check_case_labels.py"


def run() -> subprocess.CompletedProcess[str]:
    return subprocess.run([sys.executable, str(SCRIPT)], capture_output=True, text=True, check=False)


def test_every_case_label_names_a_declared_type():
    result = run()
    assert result.returncode == 0, result.stdout + result.stderr
    assert "PASS" in result.stdout
    # A checker that examines nothing would also print PASS; the count proves it looked.
    assert "qualified case label(s)" in result.stdout
    assert "all 0 qualified case label(s)" not in result.stdout


def test_the_enum_the_prologue_switches_on_is_declared():
    # The specific break, stated as a fact about the tree rather than just as a passing script.
    source = (ROOT / "Assets/Scripts/Prototype/PrototypeSceneBootstrap.cs").read_text(encoding="utf-8")
    assert "QuizScene." in source
    assert (ROOT / "Assets/Scripts/Prototype/QuizScene.cs").exists()
    enum_source = (ROOT / "Assets/Scripts/Prototype/QuizScene.cs").read_text(encoding="utf-8")
    for member in ("ClassroomOpening", "TeacherFirstClass", "TeacherRevisit"):
        assert member in enum_source, f"QuizScene.{member} is used by the bootstrap but not declared"


def test_the_checker_reports_missing_types_rather_than_passing_silently(tmp_path):
    # Negative control against a scratch tree: a lone case label naming a type nobody declares.
    # Without this, a checker whose FAIL branch never fires would look exactly like a passing one.
    scripts = tmp_path / "Assets" / "Scripts"
    scripts.mkdir(parents=True)
    (scripts / "Broken.cs").write_text(
        "class Broken { void F(object o) { switch (o) { case Missing.Thing: break; } } }",
        encoding="utf-8",
    )

    result = subprocess.run(
        [sys.executable, str(SCRIPT), "--root", str(tmp_path)],
        capture_output=True, text=True, check=False,
    )

    assert result.returncode == 1, "a case label naming an undeclared type must fail the check"
    assert "FAIL" in result.stdout
    assert "Missing" in result.stdout
