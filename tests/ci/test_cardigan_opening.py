"""The cardigan opening has to taper the right way, and nothing else in CI looks at it.

`tools/cardigan_v_proof.py` draws the opening from constants it reads out of
`SkinnedFigureFactory.BuildCardigan`. This file checks the two properties that make the drawing look
like a cardigan rather than like a coat, a bib or a tailcoat:

  * the gap between the two front edges widens upwards and closes at the waist --- a V, not a Λ;
  * the bands meet and stop, rather than crossing each other below the meeting point.

Both were wrong in earlier versions of this code and neither produced an error anywhere. They are
pure arithmetic on the constants, so they can be checked without Unity.

Run: python3 -m unittest discover -s tests/ci -p 'test_*.py' -v
"""

from __future__ import annotations

import math
import re
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FACTORY = ROOT / "Assets/Scripts/Character/SkinnedFigureFactory.cs"

sys.path.insert(0, str(ROOT / "tools"))


def factory_text() -> str:
    return FACTORY.read_text(encoding="utf-8")


def constant(name: str) -> float:
    match = re.search(rf"const float {name} = (-?[0-9.]+)f", factory_text())
    assert match is not None, f"{name} is no longer a const float in the factory; this test is stale"
    return float(match.group(1))


def geometry():
    vee = constant("veeAngle")
    lean = constant("leanSign")
    half_width = constant("bandHalfWidth")
    half_length = constant("bandHalfLength")

    match = re.search(r"spine \+ new Vector3\(sign \* ([0-9.]+)f, ([0-9.]+)f, depth\)", factory_text())
    assert match is not None, "the band centre is no longer written as `sign * <x>f, <y>f`"
    centre_x, centre_y = float(match.group(1)), float(match.group(2))

    return vee, lean, half_width, half_length, centre_x, centre_y


def inner_edge_gap(y: float) -> float:
    """Separation of the two bands' inner edges at height `y`, in metres."""
    vee, lean, half_width, half_length, centre_x, centre_y = geometry()
    radians = math.radians(vee * lean)
    across_x = abs(math.cos(radians))
    along = (-math.sin(radians), math.cos(radians))

    t = (y - centre_y) / along[1]
    # The inner edge of the right-hand band, mirrored across x = 0.
    right_inner = centre_x + t * along[0] - across_x * half_width
    return 2 * right_inner


class CardiganOpeningTests(unittest.TestCase):

    def test_the_opening_widens_upwards(self):
        # A cardigan's opening is narrow at the waist and widest at the collar. If this reverses, the
        # front reads as a tailcoat, which is exactly what the first version of this geometry did.
        low = inner_edge_gap(0.02)
        mid = inner_edge_gap(0.08)
        high = inner_edge_gap(0.12)

        self.assertLess(low, mid, f"opening does not widen upwards: {low * 1000:.1f} mm at the chest, "
                                  f"{mid * 1000:.1f} at 80 mm")
        self.assertLess(mid, high, f"opening does not widen upwards: {mid * 1000:.1f} mm at 80 mm, "
                                   f"{high * 1000:.1f} at the collar")
        self.assertGreater(high, 0.04,
                           f"only {high * 1000:.0f} mm of shirt at the collar is too little to read as an opening")

    def test_the_bands_meet_at_the_waist_and_do_not_cross(self):
        # The inner edges converge to a single point. Below it the sign flips, which would mean the
        # two bands pass through each other --- the previous version's overshoot.
        apex = None
        previous = inner_edge_gap(0.12)
        y = 0.12
        while y > -0.12:
            y -= 0.001
            gap = inner_edge_gap(y)
            if previous > 0 >= gap and apex is None:
                apex = y
            previous = gap

        self.assertIsNotNone(apex, "the edges never meet: the opening runs past the hem")
        self.assertTrue(-0.06 < apex < 0.0, f"the edges meet at y = {apex:.3f}, which is not at the waist")

        # Everything below the apex has crossed, so the bands must simply not reach there.
        vee, lean, half_width, half_length, centre_x, centre_y = geometry()
        radians = math.radians(vee * lean)
        along = (-math.sin(radians), math.cos(radians))
        lowest = centre_y - half_width * abs(math.sin(radians)) - half_length * along[1]
        self.assertGreater(lowest, apex - 0.005,
                           f"the band geometry reaches y = {lowest:.3f}, past the meeting point at "
                           f"{apex:.3f}: the two front edges intersect")

    def test_the_shirt_fills_the_opening_without_poking_through_the_cardigan(self):
        # Each band of shirt is sized to the gap at its own bottom edge, so it can never be wider than
        # the opening it sits in. Too narrow and the cardigan shows through the middle of the V.
        text = factory_text()
        block = re.search(r"shirtBands =\s*\{(.*?)\};", text, re.S)
        self.assertIsNotNone(block, "the shirt bands have moved or been renamed")

        bands = [(float(y), float(w)) for y, w in re.findall(r"\((-?[0-9.]+)f, (-?[0-9.]+)f\)", block.group(1))]
        self.assertEqual(len(bands), 3, f"expected three stacked shirt bands, found {len(bands)}")

        for y, half_width in bands:
            # The band is drawn with `AddQuad(..., halfWidth, 0.0108f)`, so it spans y +/- 0.0108.
            narrowest = min(inner_edge_gap(y - 0.0108), inner_edge_gap(y + 0.0108))
            width = 2 * half_width
            self.assertLess(width, narrowest,
                            f"the shirt band at y = {y:.3f} is {width * 1000:.0f} mm wide but the opening "
                            f"there is only {narrowest * 1000:.0f} mm at its narrowest: it would poke "
                            f"through the cardigan")
            self.assertGreater(width, narrowest * 0.6,
                               f"the shirt band at y = {y:.3f} covers only {100 * width / narrowest:.0f}% "
                               f"of the opening: the cardigan would show through the middle of the V")


if __name__ == "__main__":
    unittest.main()
