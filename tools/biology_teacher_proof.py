#!/usr/bin/env python3
"""Offline proof sheet for B, the biology teacher the protagonist imagines. NOT a Unity render.

She is the first figure in this project whose whole point is that she is wrong: she is built out of
Q's description, and the imagination made her a bird. A proof sheet for her therefore has to show the
things that were specified, so a reviewer can check them one at a time against what Q actually says:

    "个子还没我高 ... 背总是驼着的，肩膀倒是耸得老高 ... 脖子往前探，像只站在电线上的鸟"
    "鼻子是勾的 ... 眼睛小，还挨得近 ... 戴那种带帽檐的眼镜"
    "白头发 ... 盘成一个团，用一根发卡别着"

Panels:
    A  five figures to one scale: where 1.64 m actually sits, and how much shoulder she gives up
    B  her profile against the two other adults, because the hook nose and the stoop only read in profile
    C  the face at 2.6x, from the same numbers
    F  what each clause of Q's description measures on the model
    D  the silhouette as the imagination distorts it: the stretch and the afterimage
    E  the palette in grey, against N, the students and the room

Run:  python3 tools/biology_teacher_proof.py [out.png]
"""

from __future__ import annotations

import sys

import matplotlib

matplotlib.use("Agg")
import matplotlib.patches as patches
import matplotlib.transforms as transforms
import matplotlib.pyplot as plt
import numpy as np

sys.path.insert(0, __file__.rsplit("/", 1)[0])

import teacher_n_proof as base  # noqa: E402

INK = "#23211F"

# B's five colours, from CharacterSkinPalette. Repeated here so the sheet is readable on its own; the
# values are the C#'s, and the palette panel below prints the grey values they produce.
B_PALETTE_B = {"skin": "#D6CBC0", "hair": "#E4E1D8", "coat": "#3E3B3F", "shirt": "#CFC9BC", "ink": "#8C5B3C"}
B_PALETTE_Q = {"skin": "#C2B49C", "hair": "#3B3A36", "coat": "#71806B", "shirt": "#DCD8C8", "ink": "#8E5A54"}
PAPER = "#F7F2E7"
MUTED = "#8E8A84"
BAD = "#B8442F"
GOOD = "#3E6B45"


def hexc(value: str) -> str:
    return "#" + value


def draw_body(ax, proportions, role, scale, origin, *, alpha=1.0, palette=None, stretch=(1.0, 1.0)):
    """Draw the figure through the library's skinned renderer.

    `role` selects the part list (which differs in the torso radii, the hair and the extra shapes);
    `palette` overrides the colours, which is how B gets hers without a second body builder.

    `stretch` is the (x, y) part of a Transform.localScale, applied to the finished drawing about the
    feet. That is how panel D shows the scene's 0.94 × 1.04 without a second copy of the geometry: the
    figure is the same mesh, squashed and stretched, which is exactly what the C# does to it.
    """
    base.PALETTE[role] = palette
    before = len(ax.patches)
    base.draw_figure(ax, proportions, base.rest_pose(), role, scale, origin, alpha=alpha)
    if stretch != (1.0, 1.0):
        sx, sy = stretch
        # Origin is already in axes coordinates, so the transform is in those units.
        centre = np.array(origin, dtype=float)
        transform = (transforms.Affine2D()
                     .translate(-centre[0], -centre[1])
                     .scale(sx, sy)
                     .translate(centre[0], centre[1]))
        for patch in ax.patches[before:]:
            patch.set_transform(transform + ax.transData)


def _xy(point, scale, origin):
    """A world point to axes coordinates. `_to_figure` takes components, not a vector."""
    point = np.asarray(point, dtype=float)
    projected = base._to_figure(point[0], point[1], scale, origin)
    return projected[0]


def _ellipse_at(ax, centre, radii, colour, scale, origin, alpha=1.0):
    """A single flat ellipse, for the face panel where the parts are drawn as their own shapes."""
    cx, cy = _xy(centre, scale, origin)
    ax.add_patch(patches.Ellipse((cx, cy), radii[0] * 2 * scale, radii[1] * 2 * scale,
                                 facecolor=colour, edgecolor="none", alpha=alpha, zorder=3))


def _box_at(ax, centre, half, colour, scale, origin, alpha=1.0):
    cx, cy = _xy(centre, scale, origin)
    ax.add_patch(patches.Rectangle((cx - half[0] * scale, cy - half[1] * scale),
                                   2 * half[0] * scale, 2 * half[1] * scale,
                                   facecolor=colour, edgecolor="none", alpha=alpha, zorder=7))


def outline(ax, proportions, scale, origin, colour, *, lw=1.1, alpha=1.0, ls="-"):
    """A stick skeleton of the figure, so a profile can be read as a posture."""
    for bone, parent in base.PARENT.items():
        if parent is None:
            continue
        a = _xy(proportions.rest(bone), scale, origin)
        b = _xy(proportions.rest(parent), scale, origin)
        ax.plot([b[0], a[0]], [b[1], a[1]], color=colour, lw=lw, alpha=alpha, ls=ls, zorder=9,
                solid_capstyle="round")


def panel_a(ax):
    figures = [
        ("Q · 1.615 m", base.classmate(), "q", B_PALETTE_Q),
        ("B · 1.64 m", base.biology_teacher(), "teacher", B_PALETTE_B),
        ("N · 1.65 m", base.teacher(), "teacher", base.PALETTE["teacher"]),
        ("you · 1.72 m", base.protagonist(), "protagonist", base.PALETTE["protagonist"]),
    ]
    scale = 1.0
    for index, (label, proportions, role, palette) in enumerate(figures):
        x = 0.10 + index * 0.44
        origin = (x, 0.05)
        draw_body(ax, proportions, role, scale, origin, palette=palette)
        ax.plot([x - 0.12, x + 0.12], [0.05, 0.05], color=INK, lw=1.2, zorder=12)
        ax.text(x + 0.08, -0.03, label, ha="center", va="top", fontsize=7.2, color=INK)

    ax.set_xlim(-0.03, 1.80)
    ax.set_ylim(-0.26, 1.88)
    ax.set_aspect("equal")
    ax.axis("off")
    ax.set_title("A · the cast to one scale", fontsize=9.5, color=INK, pad=6, loc="left")


def panel_b(ax):
    """Profiles: the stoop, the forward neck and the hook are all side-view facts.

    The three marks are drawn to the left of B, in the empty column, with leader lines, because the
    space between the two figures is where they would otherwise collide with N.
    """
    scale = 1.0
    for index, (label, proportions, colour) in enumerate([
        ("B · as built", base.biology_teacher(), INK),
        ("N · upright", base.teacher(), MUTED),
    ]):
        origin = (0.42 + index * 0.60, 0.06)
        draw_body(ax, proportions, "teacher", scale, origin,
                  palette=B_PALETTE_B if index == 0 else base.PALETTE["teacher"])
        outline(ax, proportions, scale, origin, colour, lw=0.9)
        ax.text(origin[0] + 0.23, -0.03, label, ha="center", va="top", fontsize=6.8, color=colour)

    b = base.biology_teacher()
    origin = (0.42, 0.06)
    head = _xy(b.rest("Head"), scale, origin)
    hips = _xy(b.rest("Hips"), scale, origin)
    chest = _xy(b.rest("Chest"), scale, origin)
    tail = _xy(b.rest("Head") + np.array([0, -0.10, 0]), scale, origin)

    # the stoop: the chest is forward of the hips, and the head further forward again
    # Outside the shoulder line, or the coat hides the arrow.
    ax.annotate("", xy=(chest[0] - 0.19, chest[1]), xytext=(hips[0] - 0.19, hips[1]),
                arrowprops=dict(arrowstyle="->", color=GOOD, lw=1.0))
    ax.plot([chest[0] - 0.19, chest[0] - 0.14], [chest[1], chest[1]], color=GOOD, lw=0.5, ls=":")
    ax.plot([hips[0] - 0.19, hips[0] - 0.09], [hips[1], hips[1]], color=GOOD, lw=0.5, ls=":")
    ax.text(chest[0] - 0.21, (chest[1] + hips[1]) / 2, "stoop\n+16 mm", fontsize=6.6, color=GOOD,
            va="center", ha="right")
    ax.annotate("", xy=(tail[0], tail[1]), xytext=(hips[0] - 0.42, tail[1]),
                arrowprops=dict(arrowstyle="->", color=BAD, lw=0.8, ls=":"))
    ax.text(hips[0] - 0.44, tail[1], "head 14 mm\nahead", fontsize=6.6, color=BAD, va="center", ha="right")

    # the hook, called out at the face
    nose = _xy(b.rest("Head") + np.array([0, 0.145, 0]), scale, origin)
    ax.annotate("hook", xy=(nose[0] + 0.02, nose[1]), xytext=(nose[0] + 0.14, nose[1] - 0.02),
                fontsize=6.6, color=BAD,
                arrowprops=dict(arrowstyle="->", color=BAD, lw=0.8))

    ax.set_xlim(-0.22, 1.42)
    ax.set_ylim(-0.24, 1.86)
    ax.set_aspect("equal")
    ax.axis("off")
    ax.set_title("B · profile", fontsize=9.5, color=INK, pad=6, loc="left")


MAG = 2.6


def panel_c(ax):
    """The face, with each clause of Q's description printed beside what the model measures.

    Drawn in model metres with the head bone at the origin and framed tightly on the head, which is
    what magnifies it. Every radius and offset below is the number BuildTeacherBFace uses, so the panel
    measures the model rather than illustrating it. The claims are returned rather than drawn here:
    the panel is four inches wide and a column of them would lie across the face, so they go in panel F,
    where there is room for them.
    """
    p = base.biology_teacher()
    hl, hr = p.head_length, p.head_radius

    skull_c = np.array([0.0, 0.52 * hl])
    jaw_c = np.array([0.0, 0.20 * hl])
    skin = hexc("D6CBC0")
    hair = hexc("E4E1D8")

    # neck stub, so the head is attached to something
    ax.add_patch(patches.Rectangle((-0.043, -0.035), 0.086, 0.080, facecolor=skin, edgecolor="none", zorder=1))
    # hair shell behind, and the top of the skull over it
    ax.add_patch(patches.Ellipse(tuple(skull_c + np.array([0, 0.07 * hl])),
                                 2 * hr * 1.06, 2 * hl * 0.46, facecolor=hair, edgecolor="none", zorder=2))
    ax.add_patch(patches.Ellipse(tuple(skull_c), 2 * hr, hl, facecolor=skin, edgecolor="none", zorder=3))
    ax.add_patch(patches.Ellipse(tuple(jaw_c), 2 * hr * 0.8, 2 * hl * 0.24, facecolor=skin, edgecolor="none", zorder=3))

    # the coil of hair, low at the back, and the pin through it
    # The coil of hair and the pin through it are behind the skull, so a front view shows only the
    # hair shell's edge. They are not drawn here; panel F states where they are.

    # the hooked nose: bridge, tip, and the mass hanging below the tip
    brow = np.array([0.0, skull_c[1] - 0.02 * hl])
    bridge = brow + np.array([0, -0.055 * hl])
    tip = brow + np.array([0, -0.115 * hl])
    hook = tip + np.array([0, -0.0115])
    for centre, radii in ((bridge, (0.0075, 0.055 * hl)), (tip, (0.0085, 0.0095)), (hook, (0.0062, 0.0082))):
        ax.add_patch(patches.Ellipse(tuple(centre), 2 * radii[0], 2 * radii[1],
                                     facecolor="#C9B9AA", edgecolor="none", zorder=5))

    # spectacles: eight boxes a rim, the lens standing off the face, the shade plate above it
    rim = hr * 0.30
    for sign in (-1, 1):
        lens = np.array([sign * hr * 0.30, skull_c[1] + 0.03 * hl])
        for seg in range(8):
            angle = seg * np.pi / 4
            offset = np.array([np.cos(angle) * rim, np.sin(angle) * rim * 0.82])
            ax.add_patch(patches.Rectangle(tuple(lens + offset - np.array([0.0022, 0.0022])),
                                           0.0044, 0.0044, facecolor=INK, edgecolor="none", zorder=8))
        # eyes: flat marks behind the glass, 0.155 head-radii half-width, with a lower lid
        ax.add_patch(patches.Rectangle((lens[0] - hr * 0.155, lens[1] - 0.0022), hr * 0.31, 0.0044,
                                       facecolor=INK, edgecolor="none", zorder=7))
        ax.add_patch(patches.Rectangle((lens[0] - hr * 0.145, lens[1] - 0.0132), hr * 0.29, 0.0024,
                                       facecolor=MUTED, edgecolor="none", zorder=7))
        ax.add_patch(patches.Rectangle((lens[0] - rim * 1.12, lens[1] + rim * 0.95 - 0.0012),
                                       2 * rim * 1.12, 0.0024, facecolor=INK, edgecolor="none", zorder=8))
    # the bridge of the spectacles: eyeX - 0.28 head-radii, either side of the centre line
    bridge_half = hr * 0.30 - hr * 0.28
    ax.add_patch(patches.Rectangle((-bridge_half, skull_c[1] + 0.03 * hl - 0.0016), 2 * bridge_half, 0.0032,
                                   facecolor=INK, edgecolor="none", zorder=8))

    # brows: high, thin, and not meeting in the middle
    for sign in (-1, 1):
        ax.add_patch(patches.Rectangle((sign * hr * 0.34 - hr * 0.17, skull_c[1] + 0.125 * hl),
                                       2 * hr * 0.17, 0.0036, facecolor=INK, edgecolor="none", zorder=8))

    # the mouth, corners already down
    mouth_y = skull_c[1] - 0.265 * hl
    ax.plot([-hr * 0.155, hr * 0.155], [mouth_y, mouth_y], color=INK, lw=2.0, solid_capstyle="round", zorder=8)
    for sign in (-1, 1):
        ax.plot([sign * hr * 0.155], [mouth_y - 0.004], marker="v", ms=1.8, color=INK, zorder=8)

    # The face is magnified; the numbers in the column are not. A 0.22 m head drawn to scale inside a
    # four-inch panel is 1.2 inches tall, and at that size the eight-box spectacle rim closes up into a
    # smear, so the draws are scaled and the caption says so.
    for patch in ax.patches[:]:
        patch.set_transform(transforms.Affine2D().scale(MAG) + ax.transData)
    for line in ax.lines[:]:
        line.set_transform(transforms.Affine2D().scale(MAG) + ax.transData)

    claims = [
        ("short: 1.64 m, 7.4 heads", f"crown {p.height:.3f} m, head {hl:.3f} m"),
        ("stooped, shoulders high", f"Chest sits {(0.150 + 0.160) * 1000:.0f} mm above the hips"),
        ("neck forward, like a bird on a wire", "head is 14 mm ahead of the shoulders"),
        ("hooked nose, bird-like", f"hook hangs {(tip[1] - hook[1]) * 1000:.1f} mm below the tip, {(0.020) * 1000:.0f} mm forward"),
        ("eyes small, and close together", f"{(2 * hr * 0.30) * 1000:.0f} mm apart, eye {(2 * hr * 0.155) * 1000:.0f} mm wide"),
        ("brow-shaded spectacles", f"rim {(2 * rim) * 1000:.0f} mm, shade 12 mm off the face"),
        ("white hair, coiled and pinned", f"coil and pin at the back of the skull, behind the crown;\nnot visible from the front, and not drawn here"),
    ]


    ax.set_xlim(-0.115 * MAG - 0.02, 0.125 * MAG)
    ax.set_ylim(-0.245 * MAG, 0.245 * MAG)
    ax.set_aspect("equal")
    ax.axis("off")
    ax.set_title(f"C · the face at {MAG:g}× actual size", fontsize=9.5, color=INK, pad=6, loc="left")
    return claims


def panel_d(ax):
    """What the imagination does to her: the stretch and the afterimage."""
    scale = 1.0
    # The afterimage first, faded, at the offset and scale CreateImaginedTeacherB gives it.
    origin = (0.02, 0.05)
    draw_body(ax, base.biology_teacher(), "teacher", scale, origin, alpha=0.28, palette=B_PALETTE_B,
              stretch=(0.90, 0.97))
    # The two captions are on separate rows: the panel is 4 inches wide and the figures are 0.68 m
    # apart, so side-by-side captions of this length would run into each other.
    ax.text(origin[0] + 0.06, -0.06, "afterimage", ha="center", va="top", fontsize=6.2, color=MUTED)
    ax.text(origin[0] + 0.06, -0.14, "0.90 × 0.97, −0.48 m", ha="center", va="top", fontsize=6.2, color=MUTED)

    # Then the figure herself: the body she is built from, squashed wide and stretched tall.
    origin = (0.70, 0.05)
    draw_body(ax, base.biology_teacher(), "teacher", scale, origin, palette=B_PALETTE_B,
              stretch=(0.94, 1.04))
    ax.text(origin[0] + 0.06, -0.06, "her, as built", ha="center", va="top", fontsize=6.2, color=INK)
    ax.text(origin[0] + 0.06, -0.14, "0.94 × 1.04, 18° off", ha="center", va="top", fontsize=6.2, color=INK)

    ax.set_xlim(-0.14, 1.10)
    ax.set_ylim(-0.40, 1.76)
    ax.set_aspect("equal")
    ax.axis("off")
    ax.set_title("D · the distortion", fontsize=9.5, color=INK, pad=6, loc="left")


def panel_e(ax):
    """Grey values, because the separation rule is about grey, not hue."""
    swatches = [
        ("B coat", "3E3B3F"),
        ("B shirt", "CFC9BC"),
        ("B skin", "D6CBC0"),
        ("B hair", "E4E1D8"),
        ("B accent", "8C5B3C"),
        ("N cardigan", "9BAC97"),
        ("Q coat", "71806B"),
        ("room wood", "6B5540"),
    ]
    for index, (label, rgb) in enumerate(swatches):
        y = 0.52 - index * 0.062
        ax.add_patch(patches.Rectangle((0.04, y), 0.09, 0.045, facecolor=hexc(rgb), edgecolor="none"))
        grey = base_grey(hexc(rgb))
        ax.text(0.16, y + 0.022, f"{label}", fontsize=7, color=INK, va="center")
        ax.text(0.44, y + 0.022, f"{grey:.3f}", fontsize=7, color=MUTED, va="center", ha="right")
        ax.add_patch(patches.Rectangle((0.46, y), grey * 0.42, 0.045,
                                       facecolor=hexc(rgb), edgecolor="none"))

    ax.set_xlim(0, 0.64)
    ax.set_ylim(0, 0.58)
    ax.axis("off")
    ax.set_title("E · palette, grey value", fontsize=9.5, color=INK, pad=6, loc="left")


def base_grey(css: str) -> float:
    """Rec. 601, the same formula CharacterSkinPalette.Grey uses."""
    value = css.lstrip("#")
    r, g, b = (int(value[i:i + 2], 16) / 255 for i in (0, 2, 4))
    return 0.299 * r + 0.587 * g + 0.114 * b


def panel_f(ax, claims):
    """Q's clauses and the measured value of each, as a table."""
    ax.axis("off")
    ax.set_title("F · what Q's description measures", fontsize=9.5, color=INK, pad=6, loc="left")
    for index, (claim, measured) in enumerate(claims):
        y = 0.90 - index * 0.135
        ax.text(0.0, y, claim, fontsize=8.0, color=INK, va="top")
        ax.text(0.0, y - 0.055, measured, fontsize=7.2, color="#5A564F", va="top")
    ax.set_xlim(0, 1)
    ax.set_ylim(0, 1)


def main() -> int:
    from pathlib import Path

    out = Path(sys.argv[1]) if len(sys.argv) > 1 else base.ROOT / "docs/images/biology-teacher-proof.png"

    figure, axes = plt.subplots(2, 3, figsize=(13.2, 9.9), dpi=140)
    figure.patch.set_facecolor(PAPER)

    panel_a(axes[0][0])
    panel_b(axes[0][1])
    claims = panel_c(axes[0][2])
    panel_d(axes[1][0])
    panel_e(axes[1][1])
    panel_f(axes[1][2], claims)

    figure.text(0.03, 0.972, "B, the biology teacher the protagonist imagines",
                fontsize=17, color=INK, fontweight="bold")
    figure.text(0.03, 0.936,
                "Built from Q's description, one clause at a time: short, stooped, shoulders up, neck forward, "
                "hooked nose, small close-set eyes,\nbrow-shaded spectacles, white coiled hair. Offline drawing "
                "from the same proportion table the C# builds — not a Unity render.",
                fontsize=8.2, color="#5A564F", va="top")

    # The footnote goes in the margin below every panel, on three lines it breaks itself.
    figure.text(0.02, 0.088,
                "What this sheet cannot show.  The colours are the palette's hex values, flat: no lighting, no material and no skinning,\n"
                "so the mesh is drawn as its parts and the joins are visible here and will not be in the game.  The stretch in panel D is the\n"
                "transform scale from CreateImaginedTeacherB, applied to the drawing; whether 4% reads as uncanny or as a mistake is a\n"
                "question for the Editor.  Nothing about her animation is drawn, because she has none: she is given no animator on purpose.",
                fontsize=7.0, color="#5A564F", va="top", linespacing=1.6)

    figure.subplots_adjust(left=0.02, right=0.985, top=0.855, bottom=0.115, wspace=0.10, hspace=0.16)
    out.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(out, facecolor=PAPER)
    print(f"wrote {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
