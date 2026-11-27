"""Guards against the failure this file was written for: a test file that CI never runs.

The workflow runs `python3 -m unittest discover`. A test written in pytest style --- a bare
`def test_x()`, a `@pytest.fixture`, `assert` at module level --- is simply not collected by
unittest. The file imports cleanly, discover walks past it, and the job goes green having run
nothing from it. That is not hypothetical: tests/ci/test_cardigan_opening.py,
test_check_case_labels.py, test_mesh_visibility.py and test_imagination_scene.py were all in that
state, and one of them contained an assertion against a string the script never prints.

So this asserts the property directly rather than trusting a convention:

  * every test_*.py in tests/ci is collected by unittest at all;
  * the number of tests unittest collects equals the number of test functions in the files;
  * no test file needs pytest to be present.

Run: python3 -m unittest discover -s tests/ci -p 'test_*.py' -v
"""

from __future__ import annotations

import ast
import subprocess
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CI_DIR = ROOT / "tests/ci"


def ci_test_files() -> list[Path]:
    return sorted(p for p in CI_DIR.glob("test_*.py"))


def collectable_test_count() -> int:
    """Count test functions that unittest would collect: methods on a TestCase."""
    total = 0
    for path in ci_test_files():
        tree = ast.parse(path.read_text(encoding="utf-8"))
        for node in ast.walk(tree):
            if isinstance(node, ast.ClassDef) and any(
                (isinstance(base, ast.Attribute) and base.attr == "TestCase")
                or (isinstance(base, ast.Name) and base.id == "TestCase")
                for base in node.bases
            ):
                total += sum(
                    1 for item in node.body
                    if isinstance(item, ast.FunctionDef) and item.name.startswith("test_")
                )
    return total


def module_level_test_functions() -> list[str]:
    """A `def test_x()` at module level is invisible to unittest. Name them, if any."""
    found = []
    for path in ci_test_files():
        tree = ast.parse(path.read_text(encoding="utf-8"))
        for node in tree.body:
            if isinstance(node, ast.FunctionDef) and node.name.startswith("test_"):
                found.append(f"{path.name}::{node.name}")
    return found


class EveryTestFileIsActuallyRun(unittest.TestCase):

    def count_mine(self) -> int:
        """This class's own test methods, which `others` deliberately excludes."""
        return sum(1 for name in dir(self) if name.startswith("test_"))

    def test_no_test_function_sits_at_module_level(self):
        # unittest only collects methods on a TestCase subclass.
        module_level = module_level_test_functions()
        self.assertEqual(
            module_level, [],
            "these test functions are invisible to `python3 -m unittest discover`, so CI would "
            f"pass without running them: {module_level}",
        )

    def test_no_test_file_imports_pytest(self):
        # CI runs on a stock image with no pytest, so an import fails the job at collection.
        # Matched through the syntax tree rather than on the raw text: this file's own docstring
        # names pytest, and a substring search would accuse it.
        offenders = []
        for path in ci_test_files():
            tree = ast.parse(path.read_text(encoding="utf-8"))
            for node in ast.walk(tree):
                names = []
                if isinstance(node, ast.Import):
                    names = [alias.name for alias in node.names]
                elif isinstance(node, ast.ImportFrom) and node.module:
                    names = [node.module]
                if any(name == "pytest" or name.startswith("pytest.") for name in names):
                    offenders.append(path.name)
                    break
        self.assertEqual(offenders, [], f"CI has no pytest, but these files import it: {offenders}")

    def test_unittest_collects_every_test_function_in_the_directory(self):
        # Run the other files, not this one: discovering the whole directory from inside a test in
        # that directory recurses until it times out.
        others = [path for path in ci_test_files() if path.name != Path(__file__).name]
        self.assertTrue(others, "there should be other test files to check")

        ran = 0
        for path in others:
            result = subprocess.run(
                [sys.executable, "-m", "unittest", "discover", "-s", str(CI_DIR), "-p", path.name, "-v"],
                capture_output=True, text=True, check=False, cwd=str(ROOT),
            )
            self.assertEqual(result.returncode, 0, f"{path.name}:\n{result.stdout}{result.stderr}")
            ran += int(result.stderr.rsplit("Ran ", 1)[1].split(" ")[0])

        written = collectable_test_count() - self.count_mine()
        self.assertEqual(
            ran, written,
            f"unittest ran {ran} tests but the directory declares {written} test functions, so "
            "something is not being collected",
        )


if __name__ == "__main__":
    unittest.main()
