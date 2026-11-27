#!/usr/bin/env python3
"""Offline art proof for the curtain scene — NOT a Unity render, NOT a screenshot.

The sandbox has no Unity, so this draws the same numbers the C# reads
(`Assets/Resources/World/classroom_layout.json` and `ClassroomArtPalette.cs`) as flat plates, the way
the scene builder itself builds the room: coloured slabs, no textures.

Panel by panel:
  A  the curtain closed — the room has three walls and a cloth wall
  B  the curtain pulled aside — no wall, no window, 5.7 m down onto a blue container
  C  plan — where the gaps are relative to the desks
  D  the bare window — dusk and somebody else's washing
  E  the covered window — the same cloth, the same full drop
  F  the rules, and what this drawing can and cannot prove

Usage:
    python3 tools/curtain_scene_proof.py [out.png]
Dependencies: matplotlib. Fonts: needs Noto Sans SC for the Chinese labels; set CURTAIN_PROOF_FONT to
a .ttf/.otf, or drop it at /tmp/fonts/NotoSansSC-Regular.ttf.
"""

from __future__ import annotations

import json
import os
import sys
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Circle, Polygon, Rectangle

ROOT = Path(__file__).resolve().parent.parent
LAYOUT = ROOT / "Assets" / "Resources" / "World" / "classroom_layout.json"

# Mirrors Assets/Scripts/World/ClassroomArtPalette.cs. A colour that drifts here has drifted there.
PAPER = "#F1EBDC"
INK = "#23211F"
WALL = "#DCD3C0"
CEILING = "#C4BDB0"
FLOOR = "#8E8A84"
WAINSCOT = "#6F6A61"
WOOD = "#B08E62"
METAL = "#4A4E52"
CURTAIN = "#B8AE9A"
FOLD = "#9C9280"
DUSK_HIGH = "#5B4A63"
DUSK_LOW = "#E2A05C"
HAZE = "#C98A63"
BUILDING = "#4A4450"
BUILDING_NEAR = "#3B3742"
BUILDING_FAR = "#6C5F66"
WINDOW_LIT = "#F0C070"
STREET = "#4E4A48"
CONTAINER = "#2F6E8C"
CONTAINER_SHADE = "#24556C"
LAUNDRY = "#E6DFCB"
LAUNDRY_WARM = "#D9A98F"
SKIN = "#C9A487"
HAIR = "#2A2730"
COAT = "#7E6A55"
SHIRT = "#EFE8D6"
RED = "#B8442F"


# --------------------------------------------------------------------------- data


def load_layout() -> dict:
    with LAYOUT.open(encoding="utf-8") as handle:
        return json.load(handle)


def drop_metres(layout: dict) -> float:
    """Mirrors CurtainFallRules.DropMetres: classroom floor (0) down to the container lid."""
    fall = layout["fall"]
    return layout["room"]["height"] - (fall["groundY"] + fall["containerHeight"])


def register_font() -> str:
    from matplotlib import font_manager

    for candidate in (
        os.environ.get("CURTAIN_PROOF_FONT", ""),
        "/tmp/fonts/NotoSansSC-Regular.ttf",
        str(Path.home() / ".fonts" / "NotoSansSC-Regular.ttf"),
        "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",
    ):
        if candidate and Path(candidate).exists():
            font_manager.fontManager.addfont(candidate)
            return font_manager.FontProperties(fname=candidate).get_name()

    raise SystemExit(
        "A CJK font is required for the labels. Set CURTAIN_PROOF_FONT=/path/to/font.ttf "
        "(npm pack @expo-google-fonts/noto-sans-sc → package/400Regular/NotoSansSC_400Regular.ttf)."
    )


# --------------------------------------------------------------------------- figures


def person(ax, x: float, feet_y: float, pose: str = "stand", facing: int = 1, z: int = 20):
    """A paper-cut figure at true scale: 1.72 m, seven heads, one saturated sleeve band."""
    h = 1.72
    head = 0.24

    if pose == "fall":
        ax.add_patch(Polygon(
            [(x - 0.16, feet_y + 0.75), (x + 0.16, feet_y + 0.75),
             (x + 0.20, feet_y + 1.42), (x - 0.20, feet_y + 1.42)],
            closed=True, color=COAT, zorder=z))
        ax.add_patch(Circle((x, feet_y + 1.58), head / 2, color=SKIN, zorder=z + 1))
        ax.add_patch(Polygon(
            [(x - 0.13, feet_y + 1.60), (x + 0.13, feet_y + 1.60),
             (x + 0.12, feet_y + 1.72), (x - 0.12, feet_y + 1.72)],
            closed=True, color=HAIR, zorder=z + 2))
        for side in (-1, 1):
            ax.plot([x + side * 0.17, x + side * 0.34], [feet_y + 1.40, feet_y + 1.74],
                    color=COAT, lw=6, solid_capstyle="round", zorder=z)
        ax.plot([x - 0.06, x - 0.16], [feet_y + 0.75, feet_y + 0.34],
                color=COAT, lw=7, solid_capstyle="round", zorder=z)
        ax.plot([x + 0.06, x + 0.20], [feet_y + 0.75, feet_y + 0.44],
                color=COAT, lw=7, solid_capstyle="round", zorder=z)
        return

    if pose == "crouch":
        # Landed brace: knees deep, one hand down, chest still pitched forward.
        ax.add_patch(Polygon(
            [(x - 0.20, feet_y + 0.02), (x - 0.24, feet_y + 0.62), (x + 0.10, feet_y + 0.78),
             (x + 0.26, feet_y + 0.36), (x + 0.14, feet_y + 0.02)],
            closed=True, color=COAT, zorder=z))
        ax.add_patch(Circle((x + 0.14, feet_y + 0.86), head / 2, color=SKIN, zorder=z + 1))
        ax.add_patch(Polygon(
            [(x + 0.02, feet_y + 0.90), (x + 0.26, feet_y + 0.90),
             (x + 0.25, feet_y + 1.02), (x + 0.03, feet_y + 1.02)],
            closed=True, color=HAIR, zorder=z + 2))
        ax.plot([x + 0.24, x + 0.30], [feet_y + 0.60, feet_y + 0.04],
                color=COAT, lw=6, solid_capstyle="round", zorder=z + 1)
        ax.add_patch(Rectangle((x + 0.26, feet_y + 0.62), 0.09, 0.06, color=RED, zorder=z + 2))
        return

    if pose == "prone":
        # Face down on the lid, one leg over the edge.
        ax.add_patch(Rectangle((x - 0.55, feet_y + 0.05), 0.72, 0.16, color=COAT, zorder=z))
        ax.add_patch(Circle((x + 0.28, feet_y + 0.14), head / 2, color=SKIN, zorder=z + 1))
        ax.plot([x - 0.55, x - 0.78], [feet_y + 0.11, feet_y - 0.10],
                color=COAT, lw=6, solid_capstyle="round", zorder=z)
        ax.plot([x + 0.10, x + 0.26], [feet_y + 0.06, feet_y - 0.02],
                color=COAT, lw=5, solid_capstyle="round", zorder=z)
        ax.add_patch(Rectangle((x - 0.02, feet_y + 0.11), 0.09, 0.06, color=RED, zorder=z + 2))
        return

    # Standing.
    ax.add_patch(Polygon(
        [(x - 0.17, feet_y + 0.80), (x + 0.17, feet_y + 0.80),
         (x + 0.15, feet_y + 0.12), (x - 0.15, feet_y + 0.12)],
        closed=True, color=COAT, zorder=z))
    ax.add_patch(Rectangle((x - 0.05, feet_y + 1.36), 0.10, 0.18, color=SHIRT, zorder=z + 1))
    ax.add_patch(Circle((x, feet_y + 1.60), head / 2, color=SKIN, zorder=z + 1))
    ax.add_patch(Polygon(
        [(x - 0.13, feet_y + 1.62), (x + 0.13, feet_y + 1.62),
         (x + 0.12, feet_y + 1.74), (x - 0.12, feet_y + 1.74)],
        closed=True, color=HAIR, zorder=z + 2))
    ax.plot([x - 0.15, x - 0.22], [feet_y + 1.30, feet_y + 0.76],
            color=COAT, lw=6, solid_capstyle="round", zorder=z)
    ax.plot([x + 0.15, x + 0.22], [feet_y + 1.30, feet_y + 0.76],
            color=COAT, lw=6, solid_capstyle="round", zorder=z)
    ax.add_patch(Rectangle((x + 0.13, feet_y + 1.18), 0.10, 0.07,
                           color=RED, zorder=z + 3, clip_on=False))


def label(ax, x, y, text, color=None, size=8.0, box=None, rotate=0, ha="center", va="center"):
    kwargs = {}
    if box:
        kwargs["bbox"] = dict(fc=box, ec="none", alpha=0.86, pad=2.2)
    ax.text(x, y, text, fontsize=size, color=color or INK, ha=ha, va=va,
            rotation=rotate, zorder=40, linespacing=1.35, **kwargs)


def outdoor(ax, layout: dict, xlim=(-11.5, 11.5), ground_from=None):
    """Sky bands, the row across the street, washing on a line, and the blue lid."""
    fall = layout["fall"]
    ground = fall["groundY"] if ground_from is None else ground_from
    ch = fall["containerHeight"]

    for i, colour in enumerate([DUSK_HIGH, DUSK_HIGH, HAZE, HAZE, DUSK_LOW, DUSK_LOW]):
        ax.add_patch(Rectangle((xlim[0], ground + 3.6 * i), xlim[1] - xlim[0], 3.7,
                               color=colour, zorder=0))
    ax.add_patch(Circle((-7.6, ground + 10.2), 1.5, color=WINDOW_LIT, zorder=1))

    spacing = 7.4
    total = spacing * len(fall["buildingHeights"])
    for i, height_m in enumerate(fall["buildingHeights"]):
        bx = -total / 2 + spacing * (i + 0.5)
        ax.add_patch(Rectangle((bx - (spacing - 1.2) / 2, ground), spacing - 1.2, height_m,
                               color=BUILDING if i % 2 == 0 else BUILDING_NEAR, zorder=1))
        for c in range(3):
            for r in range(max(1, int((height_m - 2.4) / 2.6))):
                if (i * 31 + c * 7 + r * 13) % 11 in (0, 3, 5, 9):
                    continue
                wx = bx - (spacing - 1.2) / 2 + (c + 0.5) * (spacing - 1.2) / 3
                wy = ground + 2.0 + r * 2.6
                ax.add_patch(Rectangle((wx - 0.42, wy - 0.55), 0.84, 1.1, color=WINDOW_LIT, zorder=2))
    ax.add_patch(Rectangle((xlim[0], ground - 1.0), xlim[1] - xlim[0], 1.0, color=STREET, zorder=2))

    laundry = layout.get("laundry") or {}
    if laundry.get("garments"):
        ly = laundry["lineY"]
        ax.plot([laundry["lineFromX"], laundry["lineToX"]], [ly, ly], color=INK, lw=0.7, zorder=4)
        span = laundry["lineToX"] - laundry["lineFromX"]
        for i, kind in enumerate(laundry["garments"]):
            gx = laundry["lineFromX"] + span * (i + 0.5) / len(laundry["garments"])
            gw, gh = {"sheet": (1.45, 1.9), "towel": (0.72, 0.58), "trousers": (0.68, 1.05),
                      "dress": (0.88, 1.25)}.get(kind, (0.72, 0.82))
            ax.add_patch(Rectangle((gx - gw / 2, ly - gh), gw, gh,
                                   color=LAUNDRY_WARM if i % 3 == 0 else LAUNDRY, zorder=4))

    cw, cx = fall["containerWidth"], fall["containerCenterX"]
    ax.add_patch(Rectangle((cx - cw / 2, ground), cw, ch, color=CONTAINER, zorder=5))
    for i in range(12):
        rib = cx - cw / 2 + (i + 0.5) * cw / 12
        ax.plot([rib, rib], [ground + 0.1, ground + ch - 0.1], color=CONTAINER_SHADE, lw=1.4, zorder=6)
    ax.add_patch(Rectangle((cx - cw / 2, ground + ch), cw, 0.1, color=CONTAINER_SHADE, zorder=7))


def room_box(ax, layout: dict, frame: float = 1.7):
    """The room's interior, plus a thin section frame: this is a cutaway, so the cut shows as hatch.

    The frame is deliberately narrow. Earlier versions hatched five metres of building on each side and
    the hatch swallowed the sky, which is the one thing the panel exists to show.
    """
    room = layout["room"]
    w, h = room["width"], room["height"]
    half = w / 2

    for (x0, x1) in ((-half - frame, -half), (half, half + frame)):
        ax.add_patch(Rectangle((x0, -1.2), x1 - x0, h + 1.2, color=BUILDING_FAR, zorder=9))
        for i in range(int((h + 1.2) / 0.30) + 1):
            ax.plot([x0, x1], [-1.2 + 0.30 * i, -1.2 + 0.30 * i + 0.9],
                    color=BUILDING_NEAR, lw=0.6, zorder=10)
    ax.add_patch(Rectangle((-half - frame, h), w + 2 * frame, 0.55, color=BUILDING_FAR, zorder=9))
    for i in range(6):
        y0 = h + 0.09 * i
        ax.plot([-half - frame, -half + 0.5], [y0, y0 + 0.5], color=BUILDING_NEAR, lw=0.6, zorder=10)
        ax.plot([half - 0.5, half + frame], [y0, y0 + 0.5], color=BUILDING_NEAR, lw=0.6, zorder=10)

    # The interior itself is paper, not cream: cream reads as "wall" everywhere else on this plate.
    ax.add_patch(Rectangle((-half, 0), w, h, color=PAPER, zorder=8))
    ax.add_patch(Rectangle((-half, h - 0.10), w, 0.10, color=CEILING, zorder=9))
    return half, h


def axis_finish(ax, ground, title, xlabel="x (m) · 正对缺失的前墙", xlim=(-11.5, 11.5), ylim=None):
    ax.set_xlim(*xlim)
    ax.set_ylim(*(ylim if ylim else (ground - 1.5, 11.0)))
    ax.set_aspect("equal")
    ax.set_facecolor(PAPER)
    ax.tick_params(labelsize=7, colors=INK)
    ax.set_xlabel(xlabel, fontsize=8)
    ax.set_ylabel("y (m) · 教室地面 = 0", fontsize=7.5)
    ax.set_title(title, fontsize=10.5, loc="left", color=INK, pad=8)
    for spine in ax.spines.values():
        spine.set_color("#c4bbaa")


# --------------------------------------------------------------------------- panels


def panel_closed(ax, layout: dict):
    half, h = room_box(ax, layout)
    front = layout["frontWall"]
    door = next(d for d in layout["doors"] if d["wall"] == "front")
    door_x = -half + door["centerAlong"]

    # The exit gap: a black slot under the cloth, at the right-hand end.
    ax.add_patch(Rectangle((door_x - door["width"] / 2, 0.16), door["width"], door["height"],
                           color=INK, zorder=12))
    label(ax, door_x, 1.15, "出口", color=PAPER, size=7.6, rotate=90)

    folds = 26
    for i in range(folds):
        fx = -half + layout["room"]["width"] * (i + 0.5) / folds
        fw = layout["room"]["width"] / folds + 0.02
        ax.add_patch(Rectangle((fx - fw / 2, front["curtainBottom"] + 0.14), fw,
                               front["curtainTop"] - front["curtainBottom"],
                               color=CURTAIN if i % 2 == 0 else FOLD, zorder=13))
    ax.plot([-half, half], [front["curtainTop"] + 0.03] * 2, color=METAL, lw=2.6, zorder=14)
    ax.plot([-half, half], [front["curtainBottom"] + 0.16] * 2, color=FOLD, lw=1.6, zorder=14)

    # Floor and skirting in front of the cloth: the curtain has to meet the ground, not float.
    ax.add_patch(Rectangle((-half, 0), layout["room"]["width"], 0.16, color=FLOOR, zorder=15))
    ax.add_patch(Rectangle((-half, 0), layout["room"]["width"], 0.82, color=WAINSCOT, zorder=15, alpha=0.0))

    label(ax, -2.2, 2.30, "整面墙就是这块布\n3.42 m，垂到地面", box=PAPER, size=8.2)
    label(ax, 4.3, 2.62, "布后面没有墙\n也没有窗", box=PAPER, size=8.2)

    # The bare window, edge-on, and the covered one: the two things a first-time viewer compares.
    window = next(w for w in layout["windows"] if w.get("treatment") == "dusk")
    ax.add_patch(Rectangle((-half - 0.16, window["sillHeight"]), 0.16, window["height"],
                           color=WINDOW_LIT, zorder=16))
    label(ax, -half - 0.95, window["sillHeight"] + window["height"] * 0.55, "裸窗 →", size=7.2, rotate=90)

    axis_finish(ax, -1.6, "A · 幕布垂着：这就是“那面墙”",
                xlim=(-8.6, 8.6), ylim=(-0.45, 6.35))
    ax.set_yticks([0, 1, 2, 3, 4])
    ax.set_ylabel("y (m)", fontsize=7.5)


def panel_open(ax, layout: dict):
    """Standing at the doorway, looking out. Only what is actually there is drawn: no interior fill,
    because any cream in this frame would read as the wall the whole scene says is absent."""
    ground = layout["fall"]["groundY"]
    room = layout["room"]
    half = room["width"] / 2
    h = room["height"]
    ch = layout["fall"]["containerHeight"]
    front = layout["frontWall"]

    outdoor(ax, layout)

    # The edge of the room: floor slab, ceiling slab, and nothing in between.
    ax.add_patch(Rectangle((-half, 0), room["width"], 0.16, color=FLOOR, zorder=15))
    ax.add_patch(Rectangle((-half, 0), room["width"], 0.10, color=WAINSCOT, zorder=16))
    ax.add_patch(Rectangle((-half, h - 0.12), room["width"], 0.12, color=CEILING, zorder=15))
    ax.plot([-half, half], [front["curtainTop"] + 0.03] * 2, color=METAL, lw=2.6, zorder=16)

    # The cloth, gathered at the right-hand edge: pulled aside, not gone.
    for i in range(7):
        fx = half - 1.5 + 1.5 * (i + 0.5) / 7
        ax.add_patch(Rectangle((fx - 0.11, 0.16), 0.22, 3.26,
                               color=CURTAIN if i % 2 == 0 else FOLD, zorder=17))

    door = next(d for d in layout["doors"] if d["wall"] == "front")
    door_x = -half + door["centerAlong"]
    lx = layout["fall"]["containerCenterX"] + 0.2

    # Beat 1 — standing at the gap.
    person(ax, door_x - 1.2, 0.16, "stand", facing=1)
    label(ax, door_x - 1.2, 2.35, "① 站在缺口前", box=PAPER, size=8.2)

    # Beat 2 — the drop. The arrow is what makes "six metres" a distance and not a caption.
    person(ax, lx - 2.4, ground + ch + 2.15, "fall", facing=1, z=19)
    ax.annotate("", xy=(lx - 1.05, ground + ch + 0.14), xytext=(lx - 1.05, ground + ch + 1.35),
                arrowprops=dict(arrowstyle="-|>", color=WINDOW_LIT, lw=1.8), zorder=22)
    label(ax, lx + 0.35, ground + ch + 2.55, f"② 落体 · 落差 {drop_metres(layout):.1f} m",
          color=PAPER, box=INK, size=8.4, ha="left")

    # Beat 3 and 3' — the two outcomes, side by side, on the same lid.
    # The two outcomes, offset along the lid, each labelled away from the other's body.
    person(ax, lx - 1.0, ground + ch, "crouch", facing=1, z=21)
    label(ax, lx - 1.1, ground + ch + 1.10, "③ 体魄过 · 站住", box=PAPER, size=8)

    person(ax, lx + 2.2, ground + ch, "prone", facing=1, z=21)
    label(ax, lx + 2.2, ground + ch + 0.52, "③′ 没过 · 摔了", box=PAPER, size=8)

    label(ax, -4.55, ground + 4.55, "黄昏 · 别人的晾衣绳", color=PAPER, size=7.8, box=BUILDING_NEAR)

    axis_finish(ax, ground, "B · 掀开之后：没有墙，没有窗，下面是一块蓝盖子",
                xlim=(-6.8, 6.8), ylim=(ground - 1.4, ground + 10.6))


def panel_plan(ax, layout: dict):
    room = layout["room"]
    w, d = room["width"], room["depth"]
    min_x, max_x = -w / 2, w / 2
    min_z = room["centerZ"] - d / 2
    max_z = room["centerZ"] + d / 2

    ax.add_patch(Rectangle((min_x, min_z), w, d, facecolor="#E9E2D0", edgecolor="none", zorder=1))

    seating = layout["seating"]
    for z in seating["rows"]:
        for x in seating["columns"]:
            ax.add_patch(Rectangle((x - seating["deskWidth"] / 2, z - seating["deskDepth"] / 2),
                                   seating["deskWidth"], seating["deskDepth"],
                                   facecolor=WOOD, edgecolor="none", zorder=2))
            ax.add_patch(Rectangle((x - seating["chairSize"] / 2, z + seating["chairOffsetZ"] - 0.15),
                                   seating["chairSize"], 0.35, facecolor=METAL, zorder=3))

    wake = layout["wakeDesk"]
    ax.add_patch(Rectangle((wake["positionX"] - wake["width"] / 2, wake["positionZ"] - wake["depth"] / 2),
                           wake["width"], wake["depth"], facecolor="#C9A472", edgecolor=INK, lw=1, zorder=4))
    ax.add_patch(Circle((wake["positionX"], wake["positionZ"]), layout["chalkCircle"]["radius"],
                        fill=False, edgecolor=PAPER, lw=1.6, ls=(0, (6, 4)), zorder=5))

    board = layout["blackboard"]
    ax.add_patch(Rectangle((-board["width"] / 2, max_z - 0.22), board["width"], 0.22,
                           facecolor="#232A26", edgecolor="none", zorder=6))

    for (x0, z0, x1, z1) in [(min_x, max_z, max_x, max_z), (min_x, min_z, min_x, max_z),
                             (max_x, min_z, max_x, max_z)]:
        ax.plot([x0, x1], [z0, z1], color=WALL, lw=6, solid_capstyle="butt", zorder=7)

    # The missing wall: a dashed line, and the cloth drawn as a light band on top of it.
    ax.plot([min_x, max_x], [min_z, min_z], color=FOLD, lw=6, ls=(0, (3, 2)), zorder=7)
    label(ax, 0, min_z + 1.1, "前墙 = 幕布（虚线 = 墙不存在）", size=7.8, box="#E9E2D0")

    for window in layout["windows"]:
        if window["wall"] != "west":
            continue
        zz = min_z + window["centerAlong"]
        bare = window.get("treatment") == "dusk"
        ax.plot([min_x, min_x], [zz - window["width"] / 2, zz + window["width"] / 2],
                color=WINDOW_LIT if bare else CURTAIN, lw=7, solid_capstyle="butt", zorder=8)
        label(ax, min_x - 0.9, zz, "黄昏 · 晾衣绳" if bare else "窗帘（垂到地）", size=7.6, rotate=90)

    for door in layout["doors"]:
        if door["wall"] == "front":
            xx = min_x + door["centerAlong"]
            ax.plot([xx - door["width"] / 2, xx + door["width"] / 2], [min_z, min_z],
                    color=INK, lw=8, solid_capstyle="butt", zorder=9)
        else:
            xx = min_x + door["centerAlong"]
            ax.plot([xx - door["width"] / 2, xx + door["width"] / 2], [max_z, max_z],
                    color=WOOD, lw=8, solid_capstyle="butt", zorder=9)

    fall = layout["fall"]
    ax.add_patch(Rectangle((fall["containerCenterX"] - fall["containerWidth"] / 2,
                            fall["containerCenterZ"] - fall["containerLength"] / 2),
                           fall["containerWidth"], fall["containerLength"],
                           facecolor=CONTAINER, edgecolor=CONTAINER_SHADE, lw=1.2, zorder=2))
    label(ax, fall["containerCenterX"], fall["containerCenterZ"], "蓝色集装箱", color=PAPER, size=8.2)

    ax.set_xlim(-13.5, 13.5)
    ax.set_ylim(-13.5, 12.5)
    ax.set_aspect("equal")
    ax.set_facecolor(PAPER)
    ax.tick_params(labelsize=7)
    ax.set_title("C · 平面：三面墙，一面布", fontsize=10.5, loc="left", pad=8)
    for spine in ax.spines.values():
        spine.set_visible(False)


def panel_window(ax, layout: dict, curtained: bool):
    window = next(w for w in layout["windows"]
                  if (w.get("treatment") == "curtain") == curtained)
    w, h, sill = window["width"], window["height"], window["sillHeight"]
    top = sill + h
    half = w / 2

    # Inside the opening: sky, a roofline, washing. Composed for the window, not drawn to survey scale.
    ax.add_patch(Rectangle((-half, sill), w, h, color=HAZE, zorder=1))
    for i, colour in enumerate([DUSK_HIGH, HAZE, DUSK_LOW]):
        ax.add_patch(Rectangle((-half, sill + h * (i + 1) / 4), w, h / 4, color=colour, zorder=1))
    ax.add_patch(Circle((-half + 0.62, top - 0.42), 0.27, color=WINDOW_LIT, zorder=2))

    ax.add_patch(Rectangle((-half, sill), w, h * 0.26, color=BUILDING, zorder=3))
    for c in range(4):
        for r in range(2):
            if (c * 7 + r * 13) % 11 in (0, 3, 5):
                continue
            ax.add_patch(Rectangle((-half + (c + 0.6) * w / 4.4, sill + 0.12 + r * 0.34),
                                   0.22, 0.26, color=WINDOW_LIT, zorder=4))

    line_y = sill + h * 0.52
    ax.plot([-half - 0.04, half + 0.04], [line_y, line_y], color=INK, lw=0.8, zorder=6)
    for i, (gw, gh) in enumerate([(0.58, 0.80), (0.32, 0.38), (0.30, 0.24), (0.28, 0.44), (0.30, 0.34)]):
        gx = -half + 0.14 + i * (w - 0.26) / 5
        ax.add_patch(Rectangle((gx, line_y - gh), gw, gh,
                               color=LAUNDRY_WARM if i % 2 == 0 else LAUNDRY, zorder=5))
        ax.plot([gx + gw / 2] * 2, [line_y, line_y + 0.03], color=INK, lw=0.5, zorder=5)

    # Frame.
    ax.add_patch(Rectangle((-half - 0.09, sill - 0.11), w + 0.18, 0.11, color=WOOD, zorder=7))
    ax.add_patch(Rectangle((-half - 0.09, top), w + 0.18, 0.09, color=WOOD, zorder=7))
    ax.plot([0, 0], [sill, top], color=WOOD, lw=2.0, zorder=7)

    # Wall around the opening.
    ax.add_patch(Rectangle((-4.6, 0), 4.6 - half, 4.0, color=WALL, zorder=8))
    ax.add_patch(Rectangle((half, 0), 4.6 - half, 4.0, color=WALL, zorder=8))
    ax.add_patch(Rectangle((-4.6, 0), 9.2, sill, color=WALL, zorder=8))
    ax.add_patch(Rectangle((-4.6, top), 9.2, 0.32, color=WALL, zorder=8))
    ax.add_patch(Rectangle((-4.6, 0), 9.2, 0.62, color=WAINSCOT, zorder=8))

    if curtained:
        for i in range(13):
            fx = -half - 0.16 + (w + 0.32) * (i + 0.5) / 13
            ax.add_patch(Rectangle((fx - 0.10, 0.60), 0.20, 2.82,
                                   color=CURTAIN if i % 2 == 0 else FOLD, zorder=12))
        ax.add_patch(Rectangle((-half - 0.2, 0.55), w + 0.4, 0.10, color=FOLD, zorder=13))
        ax.plot([-half - 0.35, half + 0.35], [3.44, 3.44], color=METAL, lw=2.4, zorder=10)
        label(ax, 0, 2.34, "从天花板垂到地面", box=PAPER, size=7.8)
        ax.set_title("E · 西墙南侧 · window_west_south\n同样的布 · 缺失墙的伏笔", fontsize=9.6, loc="left", pad=8)
    else:
        label(ax, 0, sill + 0.16, "别人的晾衣绳", color=PAPER, box=INK, size=7.6)
        ax.set_title("D · 西墙北侧 · window_west_north\n黄昏 · 没有窗帘", fontsize=9.6, loc="left", pad=8)

    ax.set_xlim(-4.0, 4.0)
    ax.set_ylim(-0.2, 4.1)
    ax.set_aspect("equal")
    ax.set_facecolor(PAPER)
    ax.set_xticks([])
    ax.set_yticks([])
    for spine in ax.spines.values():
        spine.set_color("#c4bbaa")


def panel_card(ax, layout: dict):
    ax.axis("off")
    fall = layout["fall"]
    rows = [
        ("这一页是什么", "离线美术校样，不是 Unity 截图。\n沙箱里没有 Unity，所以画面\n是用同一份布局数据重画的。"),
        ("数据源", "Assets/Resources/World/\n  classroom_layout.json\n  → 尺寸、落差、窗、晾衣绳"),
        ("房间", f"{layout['room']['width']} × {layout['room']['depth']} × {layout['room']['height']} m"),
        ("前墙", "不存在（一整幅幕布代替）"),
        ("幕布", f"{layout['frontWall']['curtainWidth']} m 宽 · 垂到地面"),
        ("窗", "1 扇裸窗（黄昏 + 晾衣绳）\n1 扇被同一块布遮住"),
        ("落差", f"教室地面 → 集装箱盖\n{drop_metres(layout):.1f} m（盖高 {fall['containerHeight']} m）"),
        ("判定", "观察　　感知 ≥ 2\n不看就走　体魄 ≥ 4\n看过再走　体魄 ≥ 3"),
        ("观察的代价", "腿软概率 8% + (感知-1)×10%\n腿软 → 仍落在盖子上，但是摔的"),
        ("为什么是体魄", "五个属性里没有敏捷；\n唯一能接住下落的是身体。"),
    ]

    y = 1.0
    for title, body in rows:
        lines = body.count("\n") + 1
        ax.text(0.0, y, title, fontsize=8.6, color="#8E5A54", va="top", ha="left")
        ax.text(0.30, y, body, fontsize=8.4, color=INK, va="top", ha="left", linespacing=1.45)
        y -= 0.030 + 0.037 * lines

    ax.text(0.0, y - 0.02,
            "能证明：幕布确实到底、门口确实没墙、\n集装箱确实在门下方、外面的唯一饱和色是它。\n"
            "不能证明：Unity 光照、字体、任何一行 C# 能编译。",
            fontsize=7.8, color="#6b6459", va="top", ha="left", linespacing=1.5)


def main() -> int:
    family = register_font()
    plt.rcParams["font.family"] = family
    plt.rcParams["axes.unicode_minus"] = False

    layout = load_layout()

    fig = plt.figure(figsize=(18, 12.4), dpi=120, facecolor=PAPER)
    grid = fig.add_gridspec(2, 3, width_ratios=(1.02, 1.02, 1.0), height_ratios=(1.06, 1.0),
                            wspace=0.17, hspace=0.22)

    panel_closed(fig.add_subplot(grid[0, 0]), layout)
    panel_open(fig.add_subplot(grid[0, 1]), layout)
    panel_plan(fig.add_subplot(grid[0, 2]), layout)
    panel_window(fig.add_subplot(grid[1, 0]), layout, curtained=False)
    panel_window(fig.add_subplot(grid[1, 1]), layout, curtained=True)
    panel_card(fig.add_subplot(grid[1, 2]), layout)

    fig.suptitle("POST-SCHOOL · 幕布 / 缺失的前墙 / 黄昏的街道 · 离线立面校样（非 Unity 渲染）",
                 fontsize=15, color=INK, y=0.975)
    fig.text(0.5, 0.012,
             "tools/curtain_scene_proof.py ← Assets/Resources/World/classroom_layout.json "
             "· 同一份数据驱动 C# 场景与这张图",
             ha="center", fontsize=7.5, color="#6b6459")

    out = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "docs" / "images" / "curtain-scene-proof.png"
    out.parent.mkdir(parents=True, exist_ok=True)
    fig.savefig(out, facecolor=PAPER)
    print(f"wrote {out}")
    print(f"drop {drop_metres(layout):.2f} m")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
