#!/usr/bin/env python3
"""Draw the three faces twice: with the marks where they used to be, and where they are now.

The bug this documents is invisible in a compile and nearly invisible in a render, because a
mark that is behind the skin simply is not there --- the face looks blank rather than broken.
So the sheet puts the old placement and the new placement side by side on the same skull
outline, with the buried marks drawn as hollow outlines at their real depth. A reader can see
which lines survive.

Everything is the same geometry the C# uses (HumanoidRig proportions, SkinnedFigureFactory's
skull and jaw ellipsoids). This is an offline drawing, not a Unity render.

Run:  python3 tools/face_marks_proof.py [out.png]
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Ellipse, Rectangle

ROOT = Path(__file__).resolve().parents[1]

INK = "#23211F"
PAPER = "#F4EFE2"
MUTED = "#8E8A84"
OLD = "#B8442F"          # the marks that were buried
NEW = "#23211F"          # the marks as placed now
SKIN = "#D9CDBE"
SKIN_OLD_LINE = "#C9A487"

FIGURES = {
    "protagonist": dict(hl=0.245, hr=0.088, head_y=0.94 + 0.16 + 0.17 + 0.15 + 0.06,
                        skin="#C9A487", title="protagonist · 7.02 heads"),
    "Q": dict(hl=0.235, hr=0.081, head_y=0.86 + 0.15 + 0.16 + 0.15 + 0.06,
              skin="#C2B49C", title="Q · 6.87 heads"),
    "N": dict(hl=0.224, hr=0.079, head_y=0.885 + 0.155 + 0.166 + 0.165 + 0.055,
              skin="#D9CDBE", title="N · 7.37 heads"),
}

# label, dy (in head-lengths from the skull centre), x (metres), half-width, half-height
MARKS = {
    "protagonist": [
        ("eye slit", 0.06 / 0.245, -0.032, 0.022, 0.004),
        ("seam", -0.02, 0.004, 0.0035, 0.245 * 0.33),
        ("seam 2", -0.17, 0.021, 0.0028, 0.245 * 0.13),
        ("mouth", -0.24, 0.002, 0.019, 0.003),
    ],
    "Q": [
        ("eye L", 0.06 / 0.235, -0.03, 0.018, 0.003),
        ("eye R", 0.06 / 0.235, 0.03, 0.018, 0.003),
        ("mouth", -0.20, 0.0, 0.017, 0.0025),
    ],
    "N": [
        ("brow L", 0.145, -0.079 * 0.365, 0.079 * 0.20 * 0.95, 0.0026),
        ("brow R", 0.145, 0.079 * 0.365, 0.079 * 0.20 * 0.90, 0.0026),
        ("eye L", 0.02, -0.079 * 0.365, 0.079 * 0.20, 0.224 * 0.019),
        ("eye lid", -0.031, -0.079 * 0.365, 0.079 * 0.20 * 0.92, 0.0014),
        ("nose", -0.185, 0.002, 0.079 * 0.115, 0.0022),
        ("mouth", -0.26, 0.0, 0.079 * 0.20, 0.0028),
        ("corner", -0.268, 0.079 * 0.20, 0.0022, 0.0045),
        ("crease L", -0.225, -0.079 * 0.22, 0.0012, 0.224 * 0.055),
    ],
}


def shells(fig):
    hl, hr, hy = fig["hl"], fig["hr"], fig["head_y"]
    skull = (np.array([0.0, hy + 0.52 * hl, 0.004]), np.array([hr, hl * 0.5, hr * 1.02]))
    jaw = (np.array([0.0, hy + 0.20 * hl, 0.024]), np.array([hr * 0.8, hl * 0.24, hr * 0.84]))
    return skull, jaw


def front_z(fig, dy, x):
    (sc, sr), (jc, jr) = shells(fig)
    y = sc[1] + dy
    best = None
    for c, r in ((sc, sr), (jc, jr)):
        t = ((x - c[0]) / r[0]) ** 2 + ((y - c[1]) / r[1]) ** 2
        if t < 1:
            z = c[2] + r[2] * np.sqrt(1 - t)
            best = z if best is None else max(best, z)
    return best


def placed(fig, dy_frac, x, half_height, lift=0.0004):
    hl = fig["hl"]
    centre = front_z(fig, dy_frac * hl, x)
    if centre is None:
        return None
    clearance = 0.0
    for edge in (-half_height, half_height):
        z = front_z(fig, dy_frac * hl + edge, x)
        if z is not None:
            clearance = max(clearance, z - centre)
    return centre + clearance + lift


def draw_face(ax, name, fig, marks, side):
    """`side` 'old' draws at the legacy planar z; 'new' draws where AddFaceMark puts them."""
    hl, hr = fig["hl"], fig["hr"]
    (sc, sr), (jc, jr) = shells(fig)
    y0 = sc[1]

    # The head outline in the (x, y) plane, which is all an orthographic front view shows.
    # Millimetres throughout: this face is 200 mm tall, and the marks are a few mm thick.
    ax.add_patch(Ellipse((sc[0] * 1000, sc[1] * 1000), 2 * sr[0] * 1000, 2 * sr[1] * 1000,
                         facecolor=fig["skin"], edgecolor="#9A9384", lw=0.5, zorder=1))
    ax.add_patch(Ellipse((jc[0] * 1000, jc[1] * 1000), 2 * jr[0] * 1000, 2 * jr[1] * 1000,
                         facecolor=fig["skin"], edgecolor="#9A9384", lw=0.4, zorder=2))

    face_z = hr * 0.985
    for label, dy_frac, x, hw, hh in marks:
        y = y0 + dy_frac * hl
        surface = front_z(fig, dy_frac * hl, x)
        if side == "old":
            z = face_z if label != "mouth" else face_z * 0.99
            buried = surface is not None and z < surface - 1e-6
        else:
            z = placed(fig, dy_frac, x, hh)
            buried = False
        if z is None:
            continue

        if buried:
            # Hollow: this line is inside the head. Drawn so the loss is visible, not silent.
            ax.add_patch(Rectangle((x * 1000 - hw * 1000, y * 1000 - hh * 1000),
                                   2 * hw * 1000, 2 * hh * 1000, facecolor="none",
                                   edgecolor=OLD, lw=0.7, ls=(0, (1.4, 1.0)), zorder=3))
        else:
            ax.add_patch(Rectangle((x * 1000 - hw * 1000, y * 1000 - hh * 1000),
                                   2 * hw * 1000, 2 * hh * 1000, facecolor=NEW,
                                   edgecolor="none", zorder=4))
            if side == "new":
                ax.plot([x * 1000], [y * 1000], "o", ms=0.8, color="#8E5A54", zorder=5)

    # Everything above is drawn in millimetres, so the limits must be too.
    ax.set_xlim(-hr * 1000 * 1.3, hr * 1000 * 1.3)
    ax.set_ylim(y0 * 1000 - hl * 1000 * 0.72, y0 * 1000 + hl * 1000 * 0.60)
    ax.set_aspect("equal")
    ax.axis("off")
    return sum(1 for _ in marks)


def main() -> int:
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "docs/images/face-marks-proof.png"

    figure, axes = plt.subplots(2, 3, figsize=(11.6, 8.4), dpi=140)
    figure.patch.set_facecolor(PAPER)

    figure.text(0.03, 0.965, "The three faces, drawn on the skin instead of on a plane",
                fontsize=17, color=INK, fontweight="bold")
    figure.text(0.03, 0.915,
                "Left: marks at the old fixed z, where the skin in front of them hides them. "
                "Right: marks placed on the skull or jaw surface.\n"
                "Red dashed outlines are marks buried inside the head — they render as nothing in Unity. "
                "Not a Unity render: the same ellipsoids the C# builds, drawn offline.",
                fontsize=8.6, color="#5A564F", va="top")

    for column, name in enumerate(FIGURES):
        fig = FIGURES[name]
        for row, side in enumerate(("old", "new")):
            ax = axes[row][column]
            draw_face(ax, name, fig, MARKS[name], side)
            if row == 0:
                ax.set_title(fig["title"], fontsize=10.5, color=INK, pad=26)
                ax.text(0.5, 1.02, "old · marks at a fixed z", transform=ax.transAxes,
                        ha="center", va="bottom", fontsize=7.6, color=OLD)
            else:
                ax.text(0.5, 1.02, "placed on the skin", transform=ax.transAxes,
                        ha="center", va="bottom", fontsize=7.6, color="#4A6B4E")

    # The count line, computed rather than typed.
    buried_total = 0
    for name, fig in FIGURES.items():
        for _label, dy_frac, x, _hw, hh in MARKS[name]:
            surface = front_z(fig, dy_frac * fig["hl"], x)
            if surface is not None and fig["hr"] * 0.985 < surface - 1e-6:
                buried_total += 1
    figure.text(0.03, 0.028,
                f"{buried_total} of {sum(len(v) for v in MARKS.values())} marks were behind the skin "
                f"at the old z. Every mark now clears it by 0.4 mm at its nearest edge.",
                fontsize=9.0, color=MUTED)

    figure.subplots_adjust(left=0.03, right=0.98, top=0.82, bottom=0.075, wspace=0.06, hspace=0.20)
    out.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(out, facecolor=PAPER)
    print(f"wrote {out}")
    print(f"{buried_total} of {sum(len(v) for v in MARKS.values())} marks were buried before the fix")
    return 0


if __name__ == "__main__":
    sys.exit(main())
