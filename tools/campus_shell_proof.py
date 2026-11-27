#!/usr/bin/env python3
"""Offline proof sheet for the campus shell. NOT a Unity render.

Reads Assets/Resources/World/campus_shell.json --- the same file a runtime builder would consume ---
and draws one sheet: a plan to check against the owner's zone map, and four views projected from the
camera descriptions in that file.

Every view comes from the same massing. The cameras are not chosen to flatter it: the eye-level view
is at 1.65 m from the ground, inside the courtyard, where a player would stand, and the classroom view
is from behind the west window at seated height. If a view is unreadable, the massing is unreadable,
not the drawing.

Nothing here is shaded. Faces are flat colour picked by which way they point, in the manner of a
nineteen-sixties competition plate: it keeps the sheet honest about the geometry, and it is the closest
a two-tone drawing gets to the reference images' cold concrete without a renderer.

Usage: python3 tools/campus_shell_proof.py [out.png]
Dependencies (not committed): matplotlib, numpy
"""

from __future__ import annotations

import json
import sys
from dataclasses import dataclass
from pathlib import Path

import matplotlib

matplotlib.use("Agg")

import matplotlib.patches as patches  # noqa: E402
import matplotlib.pyplot as plt  # noqa: E402
import numpy as np  # noqa: E402
from matplotlib.collections import PolyCollection  # noqa: E402

ROOT = Path(__file__).resolve().parents[1]
LAYOUT = ROOT / "Assets/Resources/World/campus_shell.json"


def choose_font() -> bool:
    """Use a CJK face if one is installed, and say so if not.

    matplotlib's default DejaVu has no Chinese in it: every label would come out as a row of boxes,
    which looks like a broken drawing rather than a missing font. The sheet is bilingual, so if no CJK
    face is present the labels fall back to English rather than to tofu.
    """
    from matplotlib import font_manager

    preference = ("Noto Sans CJK SC", "Noto Sans CJK JP", "Source Han Sans SC",
                  "WenQuanYi Zen Hei", "Microsoft YaHei", "SimHei", "PingFang SC")
    installed = {f.name for f in font_manager.fontManager.ttflist}
    for name in preference:
        if name in installed:
            matplotlib.rcParams["font.family"] = [name, "DejaVu Sans"]
            return True
    mathtext_safe = [n for n in matplotlib.rcParams["font.family"]]
    matplotlib.rcParams["font.family"] = mathtext_safe
    return False


HAS_CJK = choose_font()


def zh(chinese: str, english: str) -> str:
    """One label, in whichever script can actually be drawn here."""
    return chinese if HAS_CJK else english

PAPER = "#EDEAE1"
INK = "#1B1F21"
MUTED = "#5E6669"


# =============================================================================================
# Data

def load() -> dict:
    return json.loads(LAYOUT.read_text(encoding="utf-8"))


def hexc(name: str, data: dict) -> str:
    return "#" + data["materials"][name]


# =============================================================================================
# Geometry. Every solid is an axis-aligned box between two heights, which is all the massing needs.

@dataclass(frozen=True)
class Box:
    x0: float
    x1: float
    z0: float
    z1: float
    y0: float
    y1: float
    colour: str
    kind: str = "solid"

    def faces(self):
        """The six faces as (corner, normal, corner, corner, corner) quads, outward wound."""
        x0, x1, z0, z1, y0, y1 = self.x0, self.x1, self.z0, self.z1, self.y0, self.y1
        p = lambda x, y, z: np.array([x, y, z], dtype=float)
        return [
            ([p(x0, y0, z1), p(x1, y0, z1), p(x1, y1, z1), p(x0, y1, z1)], np.array([0, 0, 1.0])),
            ([p(x1, y0, z0), p(x0, y0, z0), p(x0, y1, z0), p(x1, y1, z0)], np.array([0, 0, -1.0])),
            ([p(x1, y0, z1), p(x1, y0, z0), p(x1, y1, z0), p(x1, y1, z1)], np.array([1.0, 0, 0])),
            ([p(x0, y0, z0), p(x0, y0, z1), p(x0, y1, z1), p(x0, y1, z0)], np.array([-1.0, 0, 0])),
            ([p(x0, y1, z1), p(x1, y1, z1), p(x1, y1, z0), p(x0, y1, z0)], np.array([0, 1.0, 0])),
            ([p(x0, y0, z0), p(x1, y0, z0), p(x1, y0, z1), p(x0, y0, z1)], np.array([0, -1.0, 0])),
        ]


def floor_base(data: dict, floor: int) -> float:
    """The underside of a floor's slab, in the classroom frame."""
    return floor * data["frame"]["floorHeight"]


def floor_top(data: dict, floor: int) -> float:
    return floor_base(data, floor) + data["frame"]["floorHeight"] - data["frame"]["slabThickness"]


def is_voided(data: dict, block: dict, floor: int) -> bool:
    """True when this floor of this block falls inside a declared void.

    The classroom is the middle floor of the building, so the storeys above and below it are not built
    at all. CampusShellBuilder skips them for the same reason, and the two must agree: a proof that
    drew a floor the runtime leaves out would be a picture of a building that does not exist.
    """
    for gap in data.get("voids", []):
        if floor not in gap["floors"]:
            continue
        overlaps = (block["minX"] < gap["maxX"] and block["maxX"] > gap["minX"]
                    and block["minZ"] < gap["maxZ"] and block["maxZ"] > gap["minZ"])
        if overlaps:
            return True
    return False


def block_boxes(data: dict) -> list[Box]:
    """Each declared floor of each block as one slab, plus the parapet that crowns it."""
    out: list[Box] = []
    concrete = hexc("concrete", data)
    pale = hexc("concretePale", data)
    deep = hexc("concreteDeep", data)
    t = data["frame"]["slabThickness"]

    for block in data["blocks"]:
        for floor in block["floors"]:
            if is_voided(data, block, floor):
                continue
            y0 = floor_base(data, floor)
            y1 = y0 + data["frame"]["floorHeight"] - t
            out.append(Box(block["minX"], block["maxX"], block["minZ"], block["maxZ"], y0, y1, concrete))

        top = max(block["floors"])
        crown = floor_base(data, top) + data["frame"]["floorHeight"]
        # The roof slab, and a parapet that is deliberately too tall for a building this size.
        out.append(Box(block["minX"], block["maxX"], block["minZ"], block["maxZ"],
                       crown, crown + 0.28, pale, kind="roof"))
        out.append(Box(block["minX"], block["maxX"], block["minZ"], block["maxZ"],
                       crown + 0.28, crown + 1.35, deep, kind="parapet"))

    for slab in data.get("liftedSlabs", []):
        out.append(Box(slab["minX"], slab["maxX"], slab["minZ"], slab["maxZ"],
                       slab["y"], slab["y"] + slab["thickness"], pale, kind="lifted"))

    return out


def window_bands(data: dict) -> list[Box]:
    """A band of dark glass inset in each long face, one per floor, offset per block.

    The two arms get different spacings on purpose: that is the declared misalignment, and drawing it
    is the only way to see whether it reads as a mistake or as a decision.
    """
    out: list[Box] = []
    glass = hexc("glass", data)
    void = hexc("void", data)
    fh = data["frame"]["floorHeight"]

    spacing = {"teaching_block_spine": 2.6, "teaching_block_wing": 2.15}

    for block in data["blocks"]:
        if block["id"] not in spacing:
            continue
        step = spacing[block["id"]]
        for floor in block["floors"]:
            if is_voided(data, block, floor):
                continue
            y0 = floor_base(data, floor) + 1.15
            y1 = y0 + 1.75
            # A deep reveal: the void behind, the glass a little in front of it.
            if block["id"] == "teaching_block_spine":
                count = int((block["maxX"] - block["minX"] - 1.2) / step)
                for i in range(count):
                    x0 = block["minX"] + 0.6 + i * step
                    out.append(Box(x0, x0 + step * 0.62, block["maxZ"] - 0.02, block["maxZ"] + 0.02, y0, y1, void))
                    out.append(Box(x0, x0 + step * 0.62, block["maxZ"], block["maxZ"] + 0.10, y0, y1, glass))
            else:
                count = int((block["maxZ"] - block["minZ"] - 1.2) / step)
                for i in range(count):
                    z0 = block["minZ"] + 0.6 + i * step
                    out.append(Box(block["minX"] - 0.02, block["minX"] + 0.02, z0, z0 + step * 0.62, y0, y1, void))
                    out.append(Box(block["minX"] - 0.10, block["minX"], z0, z0 + step * 0.62, y0, y1, glass))

    return out


def louvre_boxes(data: dict) -> list[Box]:
    """The fins. Thin, tall, and thrown out from the wall they belong to.

    Their count is taken from the layout and their spacing is derived from the wall, so an odd count
    leaves a half bay at the end --- the declared impossibility, drawn rather than described.
    """
    out: list[Box] = []
    steel = hexc("steel", data)
    for louvre in data["louvres"]:
        span = louvre["along"]
        count = louvre["count"]
        start, end = span["min"], span["max"]
        step = (end - start) / (count - 1)
        y0, y1 = louvre["height"]["from"], louvre["height"]["to"]
        thickness = louvre["thickness"]
        projection = louvre["projection"]

        anchor = anchor_of(data, louvre["block"], louvre["wall"])
        for i in range(count):
            along = start + i * step
            if span["axis"] == "x":
                x0, x1 = along - thickness * 0.5, along + thickness * 0.5
                if louvre["wall"] == "south":
                    z0, z1 = anchor - projection, anchor
                else:
                    z0, z1 = anchor, anchor + projection
            else:
                z0, z1 = along - thickness * 0.5, along + thickness * 0.5
                if louvre["wall"] == "south":
                    x0, x1 = anchor - projection, anchor
                else:
                    x0, x1 = anchor, anchor + projection
            out.append(Box(x0, x1, z0, z1, y0, y1, steel, kind="louvre"))
    return out


def anchor_of(data: dict, block_id: str, wall: str) -> float:
    block = next(b for b in data["blocks"] if b["id"] == block_id)
    return block["minZ"] if wall == "south" else block["maxZ"]


def ground_grid(data: dict) -> list[tuple[np.ndarray, np.ndarray]]:
    g = data["ground"]
    y = data["frame"]["streetY"] + 0.02
    lines = []
    x = g["gridOriginX"]
    while x <= g["gridOriginX"] + g["gridExtentX"]:
        lines.append((np.array([x, y, g["gridOriginZ"]]), np.array([x, y, g["gridOriginZ"] + g["gridExtentZ"]])))
        x += g["gridSpacing"]
    z = g["gridOriginZ"]
    while z <= g["gridOriginZ"] + g["gridExtentZ"]:
        lines.append((np.array([g["gridOriginX"], y, z]), np.array([g["gridOriginX"] + g["gridExtentX"], y, z])))
        z += g["gridSpacing"]
    return lines


# =============================================================================================
# Camera

class Camera:
    def __init__(self, eye, target, fov_degrees: float, aspect: float,
                 near: float = 0.4, far: float = 400.0):
        self.near = near
        self.far = far
        self.eye = np.asarray(eye, dtype=float)
        forward = np.asarray(target, dtype=float) - self.eye
        self.forward = forward / np.linalg.norm(forward)
        up_world = np.array([0.0, 1.0, 0.0])
        right = np.cross(self.forward, up_world)
        if np.linalg.norm(right) < 1e-6:
            right = np.array([1.0, 0.0, 0.0])
        self.right = right / np.linalg.norm(right)
        self.up = np.cross(self.right, self.forward)
        self.f = 1.0 / np.tan(np.radians(fov_degrees) * 0.5)
        self.aspect = aspect

    def project(self, points: np.ndarray):
        """Returns (ndc_xy, depth). depth is the distance along the view axis, positive in front."""
        rel = points - self.eye
        depth = rel @ self.forward
        x = (rel @ self.right) / np.where(np.abs(depth) < 1e-9, 1e-9, depth)
        y = (rel @ self.up) / np.where(np.abs(depth) < 1e-9, 1e-9, depth)
        return np.column_stack([x * self.f / self.aspect, y * self.f]), depth


def clip_near(poly: np.ndarray, depth: np.ndarray, near: float = 0.35):
    """Sutherland-Hodgman against the near plane. Without this a face crossing the eye inverts."""
    out_poly, out_depth = [], []
    n = len(poly)
    for i in range(n):
        cur, nxt = poly[i], poly[(i + 1) % n]
        dc, dn = depth[i], depth[(i + 1) % n]
        cur_in, nxt_in = dc >= near, dn >= near
        if cur_in:
            out_poly.append(cur)
            out_depth.append(dc)
        if cur_in != nxt_in:
            t = (near - dc) / (dn - dc)
            out_poly.append(cur + (nxt - cur) * t)
            out_depth.append(near)
    return out_poly, out_depth


def shade(colour: str, normal: np.ndarray, sun: np.ndarray) -> str:
    """Flat tonal step by facing. No gradients: four tones, so the drawing stays a drawing."""
    lambert = float(np.clip(normal @ sun, -1.0, 1.0))
    base = np.array(matplotlib.colors.to_rgb(colour))
    if normal[1] > 0.7:
        factor = 1.22
    elif normal[1] < -0.7:
        factor = 0.55
    else:
        factor = 0.80 + 0.42 * (lambert * 0.5 + 0.5)
    return matplotlib.colors.to_hex(np.clip(base * factor, 0.0, 1.0))


SUN = np.array([-0.55, 0.62, 0.56])
SUN = SUN / np.linalg.norm(SUN)


def visible_poly(camera: Camera, corners, near: float | None = None):
    """Project one face and keep it only if it survives the near and far planes.

    The far plane matters more than it sounds: the ground grid is 72 m across and some of it sits
    behind the camera, and a segment that crosses the eye projects to a line through the whole picture.
    """
    near = camera.near if near is None else near
    poly, depth = camera.project(np.array(corners, dtype=float))
    poly, depth = clip_near(poly, depth, near)
    if len(poly) < 3:
        return None
    if max(depth) > camera.far:
        return None
    return np.asarray(poly, dtype=float)


def draw_scene(ax, data, camera: Camera, *, line_width: float = 0.35):
    boxes = [b for b in block_boxes(data) + louvre_boxes(data) + window_bands(data)]

    # The plan is a rectangle of the world; without a far plane the street grid behind the camera
    # would be drawn across the picture as a straight line. Keep only what is in front, and near.
    far = camera.far
    boxes = [b for b in boxes if (np.hypot((b.x0 + b.x1) * 0.5 - camera.eye[0],
                                           (b.z0 + b.z1) * 0.5 - camera.eye[2])) < far]

    polys, depths, colours, edges = [], [], [], []

    for box in boxes:
        for corners, normal in box.faces():
            poly = visible_poly(camera, corners)
            if poly is None:
                continue
            centre = np.mean(np.array(corners, dtype=float), axis=0)
            depth = float(np.linalg.norm(centre - camera.eye))
            polys.append(poly)
            depths.append(depth)
            colours.append(shade(box.colour, normal, SUN))
            # Only the silhouette and the roof planes get a line: a wireframe reads as a model, and a
            # model is not what this sheet is for.
            edges.append(INK if abs(normal[1]) > 0.7 else "none")

    order = np.argsort(-np.array(depths))
    collection = PolyCollection([polys[i] for i in order],
                                facecolors=[colours[i] for i in order],
                                edgecolors=[edges[i] for i in order],
                                linewidths=line_width, antialiased=True, zorder=2)
    ax.add_collection(collection)

    # Ground: grid lines only, no surface under them.
    #
    # Each segment is clipped in camera space, before any division by depth. Clipping in screen space
    # after projecting is the wrong order: a segment with one end behind the eye still produces a
    # finite endpoint, and the projected line then runs across the whole sheet. That artefact is what
    # made the first draft of this sheet unreadable.
    grid = ground_grid(data)
    from matplotlib.collections import LineCollection
    segments = []
    for a, b in grid:
        a = np.asarray(a, dtype=float)
        b = np.asarray(b, dtype=float)
        da = float((a - camera.eye) @ camera.forward)
        db = float((b - camera.eye) @ camera.forward)
        near = camera.near
        if da < near and db < near:
            continue
        if da < near:
            a = a + (b - a) * ((near - da) / (db - da))
            da = near
        elif db < near:
            b = b + (a - b) * ((near - db) / (da - db))
            db = near
        if max(da, db) > camera.far:
            continue
        pa, _ = camera.project(a[None, :])
        pb, _ = camera.project(b[None, :])
        segments.append([pa[0], pb[0]])

    ax.add_collection(LineCollection(segments, colors=hexc("grid", data), linewidths=0.26,
                                     alpha=0.8, zorder=1))

    # The red mat: the one saturated thing, laid where the grid runs out.
    mat = data["ground"]["mat"]
    c = np.array([[mat["minX"], data["frame"]["streetY"] + 0.03, mat["minZ"]],
                  [mat["maxX"], data["frame"]["streetY"] + 0.03, mat["minZ"]],
                  [mat["maxX"], data["frame"]["streetY"] + 0.03, mat["maxZ"]],
                  [mat["minX"], data["frame"]["streetY"] + 0.03, mat["maxZ"]]])
    poly = visible_poly(camera, c)
    if poly is not None:
        ax.add_patch(patches.Polygon(poly, closed=True, facecolor=hexc("red", data),
                                     edgecolor="none", alpha=0.92, zorder=1.5))

    # The objects on the mat: flat marks, small enough to be furniture and not props to inspect.
    for obj in data["objects"]:
        draw_object(ax, camera, obj, data)

    # The crop follows the solids, not the grid. A ground plane runs to the horizon in every view;
    # letting it set the frame shrinks the building to a speck and puts the vanishing point in the
    # middle of the sheet.
    ax.set_aspect("equal")
    if polys:
        allx = np.concatenate([p[:, 0] for p in polys])
        ally = np.concatenate([p[:, 1] for p in polys])
        mid_x, mid_y = (allx.min() + allx.max()) * 0.5, (ally.min() + ally.max()) * 0.5
        half = max(allx.max() - allx.min(), ally.max() - ally.min()) * 0.5 * 1.10
        ax.set_xlim(mid_x - half, mid_x + half)
        ax.set_ylim(mid_y - half, mid_y + half)


def draw_object(ax, camera: Camera, obj: dict, data: dict):
    x, z, yaw = obj["x"], obj["z"], np.radians(obj["yaw"])
    base = data["frame"]["streetY"]
    h = obj["height"]
    w = obj.get("length", 0.5)
    drop = np.array([np.cos(yaw) * w * 0.5, 0.0, np.sin(yaw) * w * 0.5])
    centre = np.array([x, base, z])
    a = centre - drop
    b = centre + drop
    top_a = a + np.array([0, h, 0])
    top_b = b + np.array([0, h, 0])
    quad = np.array([a, b, top_b, top_a])
    poly = visible_poly(camera, quad)
    if poly is not None:
        ax.add_patch(patches.Polygon(poly, closed=True, facecolor=hexc("steelDark", data),
                                     edgecolor="none", zorder=1.8))


# =============================================================================================
# Plan

def draw_plan(ax, data):
    ax.set_facecolor(PAPER)
    g = data["ground"]

    # The grid, in plan: the giveaway that the ground is drawn and not built.
    x = g["gridOriginX"]
    while x <= g["gridOriginX"] + g["gridExtentX"]:
        ax.plot([x, x], [g["gridOriginZ"], g["gridOriginZ"] + g["gridExtentZ"]],
                color=hexc("grid", data), lw=0.3, alpha=0.55, zorder=1)
        x += g["gridSpacing"]
    z = g["gridOriginZ"]
    while z <= g["gridOriginZ"] + g["gridExtentZ"]:
        ax.plot([g["gridOriginX"], g["gridOriginX"] + g["gridExtentX"]], [z, z],
                color=hexc("grid", data), lw=0.3, alpha=0.55, zorder=1)
        z += g["gridSpacing"]

    mat = data["ground"]["mat"]
    ax.add_patch(patches.Rectangle((mat["minX"], mat["minZ"]), mat["maxX"] - mat["minX"],
                                   mat["maxZ"] - mat["minZ"], facecolor=hexc("red", data),
                                   edgecolor="none", alpha=0.85, zorder=2))

    for index, block in enumerate(data["blocks"], start=1):
        ax.add_patch(patches.Rectangle((block["minX"], block["minZ"]),
                                       block["maxX"] - block["minX"], block["maxZ"] - block["minZ"],
                                       facecolor=hexc("concrete", data),
                                       edgecolor=INK, lw=0.6, zorder=3))
        if len(block["floors"]) == 1:
            # One storey: washed out, so the sheet shows which masses are full height.
            ax.add_patch(patches.Rectangle((block["minX"], block["minZ"]),
                                           block["maxX"] - block["minX"], block["maxZ"] - block["minZ"],
                                           facecolor=PAPER, edgecolor=INK, lw=0.5, alpha=0.42, zorder=3.1))
        # A key number, haloed, at the block's middle: names in the plan collide on 3 m wide slabs.
        cx, cz = (block["minX"] + block["maxX"]) * 0.5, (block["minZ"] + block["maxZ"]) * 0.5
        ax.text(cx, cz, str(index), ha="center", va="center", fontsize=7.0, color=INK,
                zorder=6, fontweight="bold",
                bbox=dict(boxstyle="circle,pad=0.22", facecolor=PAPER, edgecolor=INK, lw=0.6))

    # The voided column, drawn as what it is: nothing built on the floors above and below the room.
    for gap in data.get("voids", []):
        ax.add_patch(patches.Rectangle((gap["minX"], gap["minZ"]),
                                       gap["maxX"] - gap["minX"], gap["maxZ"] - gap["minZ"],
                                       facecolor=PAPER, edgecolor=MUTED, lw=0.5,
                                       ls=(0, (2, 2)), alpha=0.9, zorder=3.4))
        ax.text((gap["minX"] + gap["maxX"]) * 0.5, gap["maxZ"] - 2.6,
                zh("上下两层留空", "no floors above or below"), ha="center", va="top",
                fontsize=5.4, color=MUTED, zorder=6)

    # The classroom, so the sheet can be read against the room it belongs to.
    room = data["classroomFootprint"]
    ax.add_patch(patches.Rectangle((room["minX"], room["minZ"]),
                                   room["maxX"] - room["minX"], room["maxZ"] - room["minZ"],
                                   facecolor="none", edgecolor=hexc("red", data), lw=0.9,
                                   ls=(0, (4, 2)), zorder=4))
    ax.annotate(zh("教室（主角在此醒来）", "classroom (the room you wake in)"),
                xy=((room["minX"] + room["maxX"]) * 0.5, room["maxZ"]),
                xytext=((room["minX"] + room["maxX"]) * 0.5 - 5.0, room["maxZ"] + 4.4),
                fontsize=6.0, color=hexc("red", data), ha="center",
                arrowprops=dict(arrowstyle="->", color=hexc("red", data), lw=0.7), zorder=6)

    for slab in data.get("liftedSlabs", []):
        ax.add_patch(patches.Rectangle((slab["minX"], slab["minZ"]),
                                       slab["maxX"] - slab["minX"], slab["maxZ"] - slab["minZ"],
                                       facecolor=hexc("concretePale", data), edgecolor=INK,
                                       lw=0.8, ls=(0, (3, 1.6)), alpha=0.55, zorder=4.2))
        ax.text((slab["minX"] + slab["maxX"]) * 0.5, slab["minZ"] + 1.2,
                zh("悬空楼板（9.4 m）", "lifted slab, 9.4 m"), ha="center", va="bottom",
                fontsize=6.2, color=INK, zorder=5)

    # The fins, seen from above: 34 lines where a wall would be.
    for louvre in data["louvres"]:
        span = louvre["along"]
        anchor = anchor_of(data, louvre["block"], louvre["wall"])
        direction = -1 if louvre["wall"] == "south" else 1
        step = (span["max"] - span["min"]) / (louvre["count"] - 1)
        for i in range(louvre["count"]):
            along = span["min"] + i * step
            if span["axis"] == "z":
                ax.plot([anchor, anchor + direction * louvre["projection"]], [along, along],
                        color=hexc("steelDark", data), lw=0.45, zorder=4.4)
            else:
                ax.plot([along, along], [anchor, anchor + direction * louvre["projection"]],
                        color=hexc("steelDark", data), lw=0.45, zorder=4.4)

    # Crop to the buildings and the mat. The grid runs further, but a plan framed on empty grid
    # wastes a third of the sheet on nothing.
    ax.set_xlim(-34, 34)
    ax.set_ylim(-27, 26)
    ax.set_aspect("equal")
    ax.axis("off")



# =============================================================================================

def sheet_plan(data, out: Path) -> None:
    """The plan on its own, big enough to read against the owner's zone map."""
    figure = plt.figure(figsize=(11.0, 9.6), dpi=135)
    figure.patch.set_facecolor(PAPER)
    ax = figure.add_axes([0.04, 0.235, 0.92, 0.635])
    draw_plan(ax, data)

    figure.text(0.04, 0.958, zh("总平面：校园外壳", "Site plan: the campus shell"),
                fontsize=15, color=INK, fontweight="bold")
    figure.text(0.04, 0.914,
                zh("同一份 campus_shell.json 的俯视图，用来和作者给的区划草图对照。只做外壳与屋顶：没有室内、没有楼梯、没有可走的路。",
                   "The same campus_shell.json, seen from above, to compare against the zone map. Shell and roof "
                   "only: no rooms, no stairs, nothing walkable."),
                fontsize=7.6, color=MUTED, va="top")

    notes = (
        "1  楼板悬空 —— 一块 32 × 22 m 的楼板停在 9.4 m，和楼下同一层高、同一厚度、同一柱网，下面什么都没有。\n"
        "2  翼片比墙长 —— 南立面 34 片、北立面 26 片，都伸出墙体两端；北面是奇数，最后只剩半跨。\n"
        "3  连廊通向无物 —— 东侧那段连廊从南翼出去，停在离地 4.2 m 的中庭上空，另一头没有楼。\n"
        "4  没有地面的网格 —— 整片场地只是一层线，没有面；线跑过建筑脚下，再跑出去。\n"
        "5  窗户对不上层 —— 两翼的窗带用了不同的间距，转角处永远对不齐。"
    )
    notes_en = (
        "1  A floor in the air - 32 x 22 m of slab at 9.4 m, on the block's own floor height, thickness and bay spacing,\n"
        "     with nothing underneath it.\n"
        "2  Fins that outlast the wall - 34 south, 26 north, every one past the ends; the north count is odd, so the last\n"
        "     gap is half a bay.\n"
        "3  A corridor to nothing - the east link leaves the wing and stops 4.2 m above the courtyard.\n"
        "4  A grid with no ground - lines only, no surface, running under the buildings and past them.\n"
        "5  Windows that never line up - the two arms use different spacings, so the corner never matches."
    )
    key = "1 教学楼·北翼   2 教学楼·南翼   3 北门·门廊   4 器材室   5 连廊·北   6 连廊·东"
    key_en = "1 north arm   2 south arm   3 north gate porch   4 equipment shed   5 north link   6 east link"
    figure.text(0.04, 0.186, zh(key, key_en), fontsize=7.6, color=INK, va="top")
    figure.text(0.04, 0.128, zh("这张图里故意做错的五件事\n\n", "The five things this plan gets wrong on purpose\n\n")
                + zh(notes, notes_en), fontsize=7.0, color=MUTED, va="top", linespacing=1.75)

    out.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(out, facecolor=PAPER)
    plt.close(figure)
    print(f"wrote {out}")


def sheet_views(data, out: Path) -> None:
    """Four large views: the shell as you would actually meet it."""
    import matplotlib.gridspec as gridspec

    figure_w, figure_h = 13.6, 11.6
    figure = plt.figure(figsize=(figure_w, figure_h), dpi=135)
    figure.patch.set_facecolor(PAPER)
    grid = gridspec.GridSpec(2, 2, figure=figure, left=0.012, right=0.988, top=0.872, bottom=0.150,
                             wspace=0.055, hspace=0.34)

    views = {v["id"]: v for v in data["viewpoints"]}
    cells = {
        "courtyard":  grid[0, 0],
        "fins_along": grid[0, 1],
        "under_slab": grid[1, 0],
        "oblique":    grid[1, 1],
    }

    for view_id, cell in cells.items():
        ax = figure.add_subplot(cell)
        ax.set_facecolor(PAPER)
        view = views[view_id]
        box = ax.get_position()
        camera = Camera(view["eye"], view["target"], view["fovDegrees"],
                        aspect=(box.width * figure_w) / (box.height * figure_h),
                        near=view["near"], far=view["far"])
        draw_scene(ax, data, camera)
        ax.axis("off")
        ax.set_title(zh(view["label"], view["labelEn"]), fontsize=9.4, color=INK, loc="left", pad=6)
        ax.text(0.0, -0.045, zh(view["note"], view["noteEn"]), transform=ax.transAxes,
                fontsize=6.6, color=MUTED, va="top", wrap=True)

    figure.text(0.012, 0.962,
                zh("校园外壳：从四个位置看", "The campus shell, from four positions"),
                fontsize=15, color=INK, fontweight="bold")
    figure.text(0.012, 0.938,
                zh("四个视点的位置写在 campus_shell.json 里，投影是算出来的，不是画出来的。"
                   "离线绘制，不是 Unity 渲染。总平面另见 campus-shell-plan.png。",
                   "The four camera positions live in campus_shell.json and the projections are computed, not "
                   "drawn by hand. Offline drawing, not a Unity render. The plan is in campus-shell-plan.png.")
                if HAS_CJK else
                "The four camera positions live in campus_shell.json and the projections are computed, not drawn "
                "by hand. Offline drawing, not a Unity render. The plan is in campus-shell-plan.png.",
                fontsize=7.8, color=MUTED, va="top")

    figure.text(0.012, 0.112,
                zh("为什么是这五件：第二张参考图把整栋楼立在海浪里，第四张把一块楼板挂在空中，第一张本身就是一整面翼片墙。"
                   "它们共同的动作是——把一个本来有道理的结构，放在一个它撑不住的位置上。",
                   "Why these five: the second reference stands a building in the surf, the fourth hangs a slab in "
                   "the sky, the first is already a wall of fins. The shared move is to take a structure that is "
                   "perfectly reasonable and put it where it cannot hold."),
                fontsize=7.2, color=INK, va="top")

    out.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(out, facecolor=PAPER)
    plt.close(figure)
    print(f"wrote {out}")


def main() -> int:
    out_dir = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "docs/images"
    data = load()
    sheet_plan(data, out_dir / "campus-shell-plan.png")
    sheet_views(data, out_dir / "campus-shell-views.png")
    return 0


if __name__ == "__main__":
    sys.exit(main())
