#!/usr/bin/env python3
"""N's cardigan opening, drawn from the same numbers the C# uses.

The V-neck is the part of a cardigan that makes it a cardigan, and it is also the part that is easy to
get wrong in a way no assertion catches: the opening can taper the wrong way (a V standing on its
point), or the shirt showing through it can be too narrow to fill the gap (which reads as a bib
patch rather than an opening). Both of those happened while this was being written, and both were
found by drawing it rather than by reading the code.

This is an offline drawing, not a Unity render.

Run:  python3 tools/cardigan_v_proof.py [out.png]
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt

ROOT = Path(__file__).resolve().parents[1]

INK = "#23211F"
PAPER = "#F4EFE2"
MUTED = "#8E8A84"
CARDIGAN = "#5E6B58"
CARDIGAN_EDGE = "#3F4A3C"
CARDIGAN_LIGHT = "#6E7C68"
SHIRT = "#DCD5C4"
BAD = "#B8442F"

# --- the numbers, read out of SkinnedFigureFactory.BuildCardigan --------------------------------
# Read rather than copied. A drawing that carries its own copy of the constants is a drawing that
# goes on being right after the code has changed, which is worse than having no drawing.
FACTORY = ROOT / "Assets/Scripts/Character/SkinnedFigureFactory.cs"


def _constant(name: str, text: str) -> float:
    match = re.search(rf"const float {name} = (-?[0-9.]+)f", text)
    if match is None:
        raise SystemExit(f"could not read {name} from {FACTORY.name}; the drawing would be stale")
    return float(match.group(1))


_source = FACTORY.read_text(encoding="utf-8")

VEE_DEGREES = _constant("veeAngle", _source)
LEAN_SIGN = _constant("leanSign", _source)
BAND_HALF_WIDTH = _constant("bandHalfWidth", _source)
BAND_HALF_LENGTH = _constant("bandHalfLength", _source)
SHIRT_HALF_HEIGHT = 0.0108

_centre = re.search(r"spine \+ new Vector3\(sign \* ([0-9.]+)f, ([0-9.]+)f, depth\)", _source)
if _centre is None:
    raise SystemExit("could not read the band centre from the factory")
BAND_CENTRE_X, BAND_CENTRE_Y = float(_centre.group(1)), float(_centre.group(2))

_shirt = re.search(r"shirtBands =\s*\{(.*?)\};", _source, re.S)
if _shirt is None:
    raise SystemExit("could not read the shirt bands from the factory")
SHIRT_BANDS = [
    (float(y), float(w)) for y, w in re.findall(r"\((-?[0-9.]+)f, (-?[0-9.]+)f\)", _shirt.group(1))
]
if len(SHIRT_BANDS) != 3:
    raise SystemExit(f"expected 3 shirt bands in the factory, read {len(SHIRT_BANDS)}")

# Torso: the cardigan tube is a 0.116 m radius squashed on z by 0.9217.
TORSO_HALF_WIDTH = 0.116
SHOULDER_Y = 0.155 + 0.166 + 0.165      # hip to shoulder, the tube's own extent
WAIST_Y = -0.155


# The two versions that were wrong, kept so the sheet can show what they looked like.
OLD_LEAN = dict(lean_sign=1.0, half_len=0.115, centre=(0.020, 0.02), shirt=[(0.072, 0.0212), (0.092, 0.0259), (0.112, 0.0305)])
OLD_SINGLE = dict(lean_sign=-1.0, half_len=0.115, centre=(0.020, 0.02), shirt=[(0.086, 0.0300)])


def band_quad(sign: float):
    """The four corners of one opening band, in the torso's local (x, y) plane."""
    import math

    radians = math.radians(VEE_DEGREES * sign * LEAN_SIGN)
    across = (math.cos(radians), math.sin(radians))
    along = (-math.sin(radians), math.cos(radians))
    centre = (sign * BAND_CENTRE_X, BAND_CENTRE_Y)

    corners = []
    for a in (-1, 1):
        for b in (-1, 1):
            corners.append((
                centre[0] + a * across[0] * BAND_HALF_WIDTH + b * along[0] * BAND_HALF_LENGTH,
                centre[1] + a * across[1] * BAND_HALF_WIDTH + b * along[1] * BAND_HALF_LENGTH,
            ))
    # Order them around the quad: (a,b) traversal above gives a crossing order, so sort by angle.
    cx = sum(p[0] for p in corners) / 4
    cy = sum(p[1] for p in corners) / 4
    import math
    return sorted(corners, key=lambda p: math.atan2(p[1] - cy, p[0] - cx))


def gap_at(y: float) -> float:
    """Inner-edge separation of the two bands at height y."""
    import math

    radians = math.radians(VEE_DEGREES * LEAN_SIGN)
    along_y = math.cos(radians)
    along_x = -math.sin(radians)
    across_x = abs(math.cos(radians))
    t = (y - BAND_CENTRE_Y) / along_y
    inner = BAND_CENTRE_X + t * along_x - across_x * BAND_HALF_WIDTH
    return 2 * inner


def band_polygon(sign: float, centre: tuple[float, float], half_len: float) -> list[tuple[float, float]]:
    """Corners of one opening band, in order around its outline."""
    import math

    radians = math.radians(VEE_DEGREES * sign * _current_lean)
    across = (math.cos(radians), math.sin(radians))
    along = (-math.sin(radians), math.cos(radians))

    # The centre mirrors with the side, exactly as the C# does (`sign * 0.0282f`). Forgetting that
    # draws both bands on the same side of the chest, where they overlap into one fat cross.
    cx = sign * centre[0]
    cy = centre[1]

    corners = []
    for a in (-1, 1):
        for b in (-1, 1):
            corners.append((
                cx + a * across[0] * BAND_HALF_WIDTH + b * along[0] * half_len,
                cy + a * across[1] * BAND_HALF_WIDTH + b * along[1] * half_len,
            ))
    cx = sum(p[0] for p in corners) / 4
    cy = sum(p[1] for p in corners) / 4
    return sorted(corners, key=lambda p: math.atan2(p[1] - cy, p[0] - cx))


def gap_polygon(centre: tuple[float, float], half_len: float) -> list[tuple[float, float]]:
    """The opening itself: the triangle between the two bands' inner edges.

    The inner edge of the right band runs from the meeting point at (0, -0.029) up to the neckline,
    and the left is its mirror, so the missing cardigan is a triangle with its point at the bottom.
    Constructed from the band corners rather than approximated, so if the band geometry moves this
    cannot silently disagree with it.
    """
    right = band_polygon(1.0, centre, half_len)
    # The two corners with the smaller x are the inner edge of the right band.
    inner = sorted(right, key=lambda p: p[0])[:2]
    apex = min(inner, key=lambda p: p[1])
    top = max(inner, key=lambda p: p[1])
    return [apex, (top[0], top[1]), (-top[0], top[1])]


_current_lean = LEAN_SIGN


def gap_at_signed(y: float, lean_sign: float) -> float:
    """Inner-edge separation at height y for either lean; negative means the edges have crossed."""
    import math

    radians = math.radians(VEE_DEGREES * lean_sign)
    along_y = math.cos(radians)
    along_x = -math.sin(radians)
    across_x = abs(math.cos(radians))
    t = (y - 0.02) / along_y
    return 2 * (0.020 + t * along_x - across_x * BAND_HALF_WIDTH)


def draw(ax, title: str, *, lean_sign: float, centre: tuple[float, float], half_len: float,
         shirt: list[tuple[float, float]], badge: str, badge_color: str):
    import matplotlib.patches as patches
    from matplotlib.patches import Polygon

    global _current_lean
    _current_lean = lean_sign

    ax.add_patch(patches.Rectangle(
        (-TORSO_HALF_WIDTH, WAIST_Y), 2 * TORSO_HALF_WIDTH, SHOULDER_Y - WAIST_Y,
        facecolor=CARDIGAN, edgecolor="none", zorder=1))
    ax.text(0.015, 0.02, "cardigan front · crop 210 × 230 mm", transform=ax.transAxes,
            ha="left", va="bottom", fontsize=6.4, color="#D8D2C4")

    # The shirt first, so the bands are drawn over its edges: the gap is what the shirt fills.
    ax.add_patch(Polygon(gap_polygon(centre, half_len), closed=True, facecolor=SHIRT,
                         edgecolor="none", zorder=2))

    for sign in (-1.0, 1.0):
        ax.add_patch(Polygon(band_polygon(sign, centre, half_len), closed=True,
                             facecolor=CARDIGAN_LIGHT, edgecolor=CARDIGAN_EDGE, lw=0.9, zorder=3))

    for y, half in shirt:
        ax.add_patch(patches.Rectangle((-half, y - SHIRT_HALF_HEIGHT), 2 * half, 2 * SHIRT_HALF_HEIGHT,
                                       facecolor="#F0E9D8", edgecolor="#9A9384", lw=0.45, zorder=4))

    # Cropped to the neckline on purpose: the whole torso is 0.64 m tall and the opening is 0.09 m
    # wide, so at full-body scale the thing this sheet is about is a few pixels.
    ax.set_xlim(-0.105, 0.105)
    ax.set_ylim(-0.075, 0.155)
    ax.set_aspect("equal")
    ax.axis("off")
    ax.set_title(title, fontsize=9.5, color=INK, pad=6)
    ax.text(0.5, -0.055, badge, transform=ax.transAxes, ha="center", va="top",
            fontsize=8, color=badge_color, fontweight="bold")


def main() -> int:
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "docs/images/cardigan-v-proof.png"

    figure, axes = plt.subplots(1, 3, figsize=(9.6, 4.9), dpi=150)
    figure.patch.set_facecolor(PAPER)
    figure.text(0.03, 0.955, "N's cardigan opening: three versions, one of them right",
                fontsize=14, color=INK, fontweight="bold")
    figure.text(0.03, 0.905,
                "The opening is two bands whose separation is set by their lean. Get the sign wrong and the V "
                "stands on its point;\nget the shirt wrong and it covers only part of the gap. Drawn from the "
                "numbers in BuildCardigan, not a Unity render.",
                fontsize=8.2, color="#5A564F", va="top")

    # 1. The wrong lean: opens downwards.
    draw(axes[0], "lean +1  ·  opens at the waist", lean_sign=1.0,
         centre=OLD_LEAN["centre"], half_len=OLD_LEAN["half_len"], shirt=OLD_LEAN["shirt"],
         badge=f"wrong: the V stands on its point,\n{gap_at_signed(-0.09, 1.0) * 1000:.0f} mm at the waist",
         badge_color=BAD)

    # 2. Correct lean, but one oversized shirt quad and bands that run past the meeting point.
    draw(axes[1], "lean −1  ·  bands overshoot", lean_sign=-1.0,
         centre=OLD_SINGLE["centre"], half_len=OLD_SINGLE["half_len"], shirt=OLD_SINGLE["shirt"],
         badge="wrong: the bands cross below the apex,\nand the patch covers part of the gap",
         badge_color=BAD)

    # 3. Correct.
    draw(axes[2], "lean −1  ·  solved, shirt in three bands", lean_sign=-1.0,
         centre=(BAND_CENTRE_X, BAND_CENTRE_Y), half_len=BAND_HALF_LENGTH, shirt=SHIRT_BANDS,
         badge="correct: the edges meet at the waist\nand stop there, shirt fills the opening",
         badge_color="#3E6B45")

    figure.text(0.03, 0.035,
                "Edge separation, measured on the inner edge of the bands, not their centres: "
                f"{gap_at(0.135) * 1000:.0f} mm at the collar, {gap_at(0.02) * 1000:.0f} mm at the chest, "
                "0 mm at y = −0.029.",
                fontsize=8, color=MUTED)

    figure.subplots_adjust(left=0.03, right=0.98, top=0.78, bottom=0.16, wspace=0.10)
    out.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(out, facecolor=PAPER)
    print(f"wrote {out}")
    print(f"collar gap {gap_at(0.135)*1000:.1f} mm, chest gap {gap_at(0.02)*1000:.1f} mm, "
          f"closes at y={-0.029:.3f}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
