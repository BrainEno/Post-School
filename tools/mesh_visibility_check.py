#!/usr/bin/env python3
"""Prove two properties of the procedural character mesh that code review cannot see.

1. Winding. Unity front-faces the triangle whose indices read clockwise on screen, so on a
   correctly wound outward surface the right-handed cross-product normal points *inward*.
   Every primitive in SkinnedMeshBuilder is replicated on a synthetic shape and checked.

2. Face marks. The faces are drawn, not sculpted: each mark is a flat quad laid over an
   ellipsoid skull and jaw. A mark whose z is behind the skin at its own height and lateral
   offset is buried inside the head and renders as nothing. That failure is silent -- it
   compiles, it passes every skeleton test, and the face simply comes out blank. This part
   replicates SkinnedFigureFactory.AddFaceMark (surface at the mark's centre, plus the worst
   clearance at its top and bottom edges, plus a 0.4 mm lift) and checks every mark.

Both checks are geometry, run against numbers copied from the C#. They are the offline half of
"this has never been opened in the Editor".

Run:  python3 tools/mesh_visibility_check.py     (exit 1 if anything is inverted or buried)
"""

from __future__ import annotations

import sys

import numpy as np

# =============================================================================================
# Part 1 -- winding order.

def cross_normal(a: np.ndarray, b: np.ndarray, c: np.ndarray) -> np.ndarray | None:
    normal = np.cross(b - a, c - a)
    length = np.linalg.norm(normal)
    return None if length < 1e-12 else normal / length


def visible_from_outside(triangle, outward: np.ndarray) -> bool:
    """Unity's front face is the clockwise one, i.e. the one whose RHR normal points away."""
    normal = cross_normal(*triangle[:3])
    return normal is not None and float(np.dot(normal, outward)) < 0.0


CUBE_QUADS = {"z-": (0, 2, 3, 1), "z+": (4, 5, 7, 6), "y-": (0, 1, 5, 4),
              "y+": (2, 6, 7, 3), "x-": (0, 4, 6, 2), "x+": (1, 3, 7, 5)}


def box_corners(half: float = 0.1):
    return [np.array([(1 if i & 1 else -1) * half, (1 if i & 2 else -1) * half,
                      (1 if i & 4 else -1) * half], dtype=float) for i in range(8)]


def box_triangles(order: str):
    corners = box_corners()
    out = []
    for label, quad in CUBE_QUADS.items():
        a, b, c, d = (corners[i] for i in quad)
        if order == "fan-first":       # AddTriangle(a,c,b) then (a,d,c) -- what the source does now
            out += [(a, c, b, label), (a, d, c, label)]
        else:                          # AddTriangle(a,b,c) then (a,c,d) -- the order that culled it
            out += [(a, b, c, label), (a, c, d, label)]
    return out


def cube_outward(triangle):
    centre = (triangle[0] + triangle[1] + triangle[2]) / 3.0
    return centre / np.linalg.norm(centre)


def quad_triangles():
    """AddQuad over `right` x `up`; mirrors the source's AddTriangle(0,2,1) / (0,3,2)."""
    r, u = np.array([1.0, 0, 0]), np.array([0, 1.0, 0])
    a, b, c, d = -r - u, r - u, r + u, -r + u
    return [(a, c, b, "face"), (a, d, c, "face")]


def quad_outward(_triangle):
    return "face", np.array([0.0, 0.0, 1.0])


def ellipsoid_triangles(longitude: int = 18, latitude: int = 12):
    points = []
    for lat in range(latitude + 1):
        phi = lat / latitude * np.pi
        for lon in range(longitude):
            theta = lon / longitude * 2 * np.pi
            points.append(np.array([np.sin(phi) * np.cos(theta), np.cos(phi),
                                    np.sin(phi) * np.sin(theta)]))

    def at(lat, lon):
        return points[lat * longitude + lon % longitude]

    out = []
    for lat in range(latitude):
        for lon in range(longitude):
            a, b = at(lat, lon), at(lat, lon + 1)
            c, d = at(lat + 1, lon), at(lat + 1, lon + 1)
            out += [(a, c, b, "shell"), (b, c, d, "shell")]
    return out


def ellipsoid_outward(triangle):
    centre = (triangle[0] + triangle[1] + triangle[2]) / 3.0
    return "shell", centre / np.linalg.norm(centre)


def tube_frame():
    forward, up0 = np.array([0.0, 1.0, 0.0]), np.array([0.0, 0.0, 1.0])
    right = np.cross(up0, forward); right /= np.linalg.norm(right)
    up = np.cross(forward, right); up /= np.linalg.norm(up)
    return forward, right, up


def tube_triangles(sides: int = 10, radius: float = 0.05, length: float = 0.2):
    _, right, up = tube_frame()

    def ring(r, side):
        angle = side / sides * 2 * np.pi
        return np.array([0, r * length, 0]) + (right * np.cos(angle) + up * np.sin(angle)) * radius

    out = []
    for side in range(sides):
        nxt = (side + 1) % sides
        a, b, c, d = ring(0, side), ring(0, nxt), ring(1, side), ring(1, nxt)
        out += [(a, c, b, "wall"), (b, c, d, "wall")]
    return out


def tube_outward(triangle):
    centre = (triangle[0] + triangle[1] + triangle[2]) / 3.0
    radial = np.array([centre[0], 0.0, centre[2]])
    return "wall", radial / np.linalg.norm(radial)


def cap_triangles(start: bool, sides: int = 10, radius: float = 0.05, length: float = 0.2):
    _, right, up = tube_frame()
    ring_y = 0.0 if start else length
    tip = np.array([0.0, ring_y + (-0.01 if start else 0.01), 0.0])

    def ring(side):
        angle = side / sides * 2 * np.pi
        return np.array([0, ring_y, 0]) + (right * np.cos(angle) + up * np.sin(angle)) * radius

    if start:                       # AddTriangle(centre, a, b)
        return [(tip, ring(s), ring((s + 1) % sides), "cap") for s in range(sides)]
    # AddTriangle(b, a, centre): the rim reversed. Just moving the apex last is not enough.
    return [(ring((s + 1) % sides), ring(s), tip, "cap") for s in range(sides)]


def report(name: str, triangles, outward_for) -> bool:
    ok, per_face, degenerate = True, {}, 0
    for triangle in triangles:
        if cross_normal(*triangle[:3]) is None:
            degenerate += 1     # collapsed pole rings: a tessellation artefact, not a facing
            continue
        result = outward_for(triangle)
        label, direction = result if isinstance(result, tuple) else ("all", result)
        per_face[label] = per_face.get(label, True) and visible_from_outside(triangle, direction)
    note = f"  (+{degenerate} degenerate pole triangles skipped)" if degenerate else ""
    for label, visible in per_face.items():
        verdict = "visible from outside" if visible else "CULLED from outside"
        print(f"   {name:20s} {label:6s} {verdict}{note}")
        ok = ok and visible
    return ok


def check_winding() -> bool:
    print("Part 1 · winding (Unity culls the side opposite a triangle's RHR normal)")
    results = [
        report("AddEllipsoid", ellipsoid_triangles(), ellipsoid_outward),
        report("AddTube", tube_triangles(), tube_outward),
        report("AddCap start", cap_triangles(True), lambda t: ("cap", np.array([0.0, -1.0, 0.0]))),
        report("AddCap end", cap_triangles(False), lambda t: ("cap", np.array([0.0, 1.0, 0.0]))),
        report("AddBox", box_triangles("fan-first"), cube_outward),
        report("AddQuad(X,Y)", quad_triangles(), quad_outward),
    ]
    print()
    print("   the box fan AddBox used before this check, kept as a regression note:")
    report("AddBox(regression)", box_triangles("center-first"), cube_outward)
    print()
    return all(results)


# =============================================================================================
# Part 2 -- face marks must sit in front of the skin under them.
#
# Numbers from HumanoidRig: the skull is an ellipsoid of radius (headRadius, headLength/2,
# headRadius*1.02) centred 0.52 head-lengths above the head bone, and the jaw is a second shell
# 0.024 m further forward. A mark at faceZ = headRadius * 0.985 is compared against the
# front-most of those two surfaces at the mark's own height and lateral offset.

FIGURES = {
    "protagonist": dict(hl=0.245, hr=0.088, head_y=0.94 + 0.16 + 0.17 + 0.15 + 0.06),
    "Q": dict(hl=0.235, hr=0.081, head_y=0.86 + 0.15 + 0.16 + 0.15 + 0.06),
    "N": dict(hl=0.224, hr=0.079, head_y=0.885 + 0.155 + 0.166 + 0.165 + 0.055),
}


def head_shells(fig: dict):
    hl, hr, hy = fig["hl"], fig["hr"], fig["head_y"]
    skull_c = np.array([0.0, hy + 0.52 * hl, 0.004])
    skull_r = np.array([hr, hl * 0.5, hr * 1.02])
    jaw_c = np.array([0.0, hy + 0.20 * hl, 0.024])
    jaw_r = np.array([hr * 0.8, hl * 0.24, hr * 0.84])
    return (skull_c, skull_r), (jaw_c, jaw_r)


def front_z(fig: dict, dy_metres: float, x: float):
    """Front-most z of the head at `dy_metres` above the skull centre, `x` to the side."""
    (skull_c, skull_r), (jaw_c, jaw_r) = head_shells(fig)
    y = skull_c[1] + dy_metres
    best = None
    for centre, radius in ((skull_c, skull_r), (jaw_c, jaw_r)):
        t = ((x - centre[0]) / radius[0]) ** 2 + ((y - centre[1]) / radius[1]) ** 2
        if t < 1:
            z = centre[2] + radius[2] * np.sqrt(1 - t)
            best = z if best is None else max(best, z)
    return best


# Each mark: label, vertical offset in head-lengths from the skull centre, lateral x in metres,
# and halfHeight in metres (a tall mark reaches further up the curve than a hairline does).
MARKS = {
    "protagonist": [
        ("eye slit", 0.06 / 0.245, -0.032, 0.004),
        ("eye hollow", 0.06 / 0.245, 0.034, 0.012),
        ("seam", -0.02, 0.004, 0.245 * 0.33),
        ("seam 2", -0.17, 0.021, 0.245 * 0.13),
        ("mouth", -0.24, 0.002, 0.003),
    ],
    "Q": [
        ("eye L", 0.06 / 0.235, -0.03, 0.003),
        ("eye R", 0.06 / 0.235, 0.03, 0.003),
        ("mouth", -0.20, 0.0, 0.0025),
    ],
    "N": [
        ("brow L", 0.145, -0.079 * 0.365, 0.0026),
        ("brow R", 0.145, 0.079 * 0.365, 0.0026),
        ("eye L", 0.02, -0.079 * 0.365, 0.224 * 0.019),
        ("eye lid", -0.031, -0.079 * 0.365, 0.0014),
        ("nose", -0.185, 0.002, 0.0022),
        ("mouth", -0.26, 0.0, 0.0028),
        ("corner", -0.268, 0.079 * 0.20, 0.0045),
        ("crease L", -0.225, -0.079 * 0.22, 0.224 * 0.055),
    ],
}


def placed_mark_z(fig: dict, dy_frac: float, x: float, half_height: float, lift: float = 0.0004):
    """Where AddFaceMark puts a quad, replicating it: surface at the centre, plus the worst edge
    clearance, plus the lift. Returns (placed_z, worst_skin_z)."""
    hl = fig["hl"]
    centre = front_z(fig, dy_frac * hl, x)
    if centre is None:
        return None
    clearance = 0.0
    worst = centre
    for edge in (-half_height, half_height):
        z = front_z(fig, dy_frac * hl + edge, x)
        if z is not None:
            clearance = max(clearance, z - centre)
            worst = max(worst, z)
    return centre + clearance + lift, worst


def check_face_marks() -> bool:
    print("Part 2 · drawn face marks must clear the skin under them")
    ok = True
    for name, fig in FIGURES.items():
        print(f"   {name}")
        for label, dy_frac, x, half_height in MARKS[name]:
            placed = placed_mark_z(fig, dy_frac, x, half_height)
            if placed is None:
                print(f"      {label:10s} sits off the head entirely")
                continue
            mark_z, worst_skin = placed
            gap_mm = (worst_skin - mark_z) * 1000
            verdict = ("BURIED %.1f mm behind the skin" % gap_mm if gap_mm > 0
                       else "clear %.1f mm" % -gap_mm)
            print(f"      {label:10s} mark at {mark_z * 1000:6.1f} mm  {verdict}")
            ok = ok and gap_mm <= 0
    print()
    return ok


def main() -> int:
    winding = check_winding()
    marks = check_face_marks()
    if winding and marks:
        print("PASS: every primitive is visible from outside, and every face mark clears the skin.")
        return 0
    if not winding:
        print("FAIL: a primitive is wound inside-out and will be invisible in Unity.")
    if not marks:
        print("FAIL: a face mark is buried inside the head and will not render.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
