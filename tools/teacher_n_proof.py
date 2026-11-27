#!/usr/bin/env python3
"""Offline proof sheet for N, the biology substitute. NOT a Unity render.

Everything here is ported from the C# and checked against it:

  Assets/Scripts/Character/HumanoidRig.cs        TeacherN() proportions, the 7.37-head figure
  Assets/Scripts/Character/HumanoidPose.cs       TeacherAtPodium()
  Assets/Scripts/Character/HumanoidClip.cs       TeacherAtPodiumIdle(), three beats and a pause
  Assets/Scripts/Character/SkinnedFigureFactory.cs  the cardigan, the cuffs, the bob, the face
  Assets/Scripts/Character/CharacterSkin.cs      the five colours and their grey values
  Assets/Resources/World/classroom_layout.json   the entry path and the mark behind the podium

The sheet answers the questions a reviewer actually has about a new character:

  A  three figures to one scale, so "is she taller, narrower, lighter" is visible, not asserted
  B  N's four poses in the podium loop, with the podium drawn at its real height
  C  the face at 8x, with the standard's measurement bands marked on it
  D  the palette as grey values, against the two students and the room
  E  the plan view of the walk-in, with the desk footprints she has to miss

Usage:
    python3 tools/teacher_n_proof.py [out.png]

Dependencies: matplotlib, numpy. Unicode text uses DejaVu (no CJK needed).
"""

from __future__ import annotations

import math
import sys
from dataclasses import dataclass
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[1]

# ------------------------------------------------------------------------------------------------
# Proportions, mirrored from HumanoidRig.cs. If these drift from the C#, the sheet is lying.

BONES = [
    "Hips", "Spine", "Chest", "Neck", "Head",
    "Shoulder_L", "UpperArm_L", "Forearm_L", "Hand_L",
    "Shoulder_R", "UpperArm_R", "Forearm_R", "Hand_R",
    "Thigh_L", "Shin_L", "Foot_L",
    "Thigh_R", "Shin_R", "Foot_R",
]

PARENT = {
    "Hips": None, "Spine": "Hips", "Chest": "Spine", "Neck": "Chest", "Head": "Neck",
    "Shoulder_L": "Chest", "UpperArm_L": "Shoulder_L", "Forearm_L": "UpperArm_L", "Hand_L": "Forearm_L",
    "Shoulder_R": "Chest", "UpperArm_R": "Shoulder_R", "Forearm_R": "UpperArm_R", "Hand_R": "Forearm_R",
    "Thigh_L": "Hips", "Shin_L": "Thigh_L", "Foot_L": "Shin_L",
    "Thigh_R": "Hips", "Shin_R": "Thigh_R", "Foot_R": "Shin_R",
}


@dataclass
class Proportions:
    height: float
    ankle: float
    hips: tuple
    spine: tuple
    chest: tuple
    neck: tuple
    head: tuple
    shoulder: tuple
    upper_arm: tuple
    forearm: tuple
    hand: tuple
    thigh: tuple
    shin: tuple
    foot: tuple
    head_length: float
    head_radius: float
    chest_depth: float

    def local(self, bone: str) -> np.ndarray:
        table = {
            "Hips": self.hips, "Spine": self.spine, "Chest": self.chest,
            "Neck": self.neck, "Head": self.head,
            "Shoulder_L": (-self.shoulder[0], self.shoulder[1], self.shoulder[2]),
            "Shoulder_R": self.shoulder,
            "UpperArm_L": (-self.upper_arm[0], self.upper_arm[1], self.upper_arm[2]),
            "UpperArm_R": self.upper_arm,
            "Forearm_L": self.forearm, "Forearm_R": self.forearm,
            "Hand_L": self.hand, "Hand_R": self.hand,
            "Thigh_L": (-self.thigh[0], self.thigh[1], self.thigh[2]),
            "Thigh_R": self.thigh,
            "Shin_L": self.shin, "Shin_R": self.shin,
            "Foot_L": self.foot, "Foot_R": self.foot,
        }
        return np.array(table[bone], dtype=float)

    def rest(self, bone: str) -> np.ndarray:
        position = np.zeros(3)
        current = bone
        while current is not None:
            position = position + self.local(current)
            current = PARENT[current]
        return position

    @property
    def heads_tall(self) -> float:
        return self.height / self.head_length


def protagonist() -> Proportions:
    return Proportions(
        height=1.72, ankle=0.07,
        hips=(0, 0.94, 0), spine=(0, 0.16, 0), chest=(0, 0.17, 0), neck=(0, 0.15, 0), head=(0, 0.06, 0),
        shoulder=(0.058, 0.10, 0), upper_arm=(0.145, 0, 0), forearm=(0, -0.28, 0), hand=(0, -0.25, 0),
        thigh=(0.086, -0.07, 0), shin=(0, -0.41, 0), foot=(0, -0.39, 0),
        head_length=0.245, head_radius=0.088, chest_depth=0.115,
    )


def classmate() -> Proportions:
    return Proportions(
        height=1.615, ankle=0.065,
        hips=(0, 0.86, 0), spine=(0, 0.15, 0), chest=(0, 0.16, 0), neck=(0, 0.15, 0), head=(0, 0.06, 0),
        shoulder=(0.052, 0.09, 0), upper_arm=(0.128, 0, 0), forearm=(0, -0.255, 0), hand=(0, -0.225, 0),
        thigh=(0.078, -0.06, 0), shin=(0, -0.375, 0), foot=(0, -0.36, 0),
        head_length=0.235, head_radius=0.081, chest_depth=0.105,
    )


def teacher() -> Proportions:
    """HumanoidProportions.TeacherN()."""
    return Proportions(
        height=1.65, ankle=0.062,
        hips=(0, 0.885, 0), spine=(0, 0.155, 0), chest=(0, 0.166, 0), neck=(0, 0.165, 0), head=(0, 0.055, 0),
        shoulder=(0.040, 0.085, 0), upper_arm=(0.120, 0, 0), forearm=(0, -0.265, 0), hand=(0, -0.235, 0),
        thigh=(0.082, -0.065, 0), shin=(0, -0.395, 0), foot=(0, -0.363, 0),
        head_length=0.224, head_radius=0.079, chest_depth=0.106,
    )


# ------------------------------------------------------------------------------------------------
# Poses, mirrored from HumanoidPose.cs / HumanoidClip.cs (degrees, Unity ZXY order).


@dataclass
class Pose:
    rot: dict
    offset: float = 0.0

    def apply(self, proportions: Proportions) -> None:
        """Nothing to do: the skin is derived from rot on demand. Kept so call sites read as steps."""
        return None


def rest_pose() -> Pose:
    return Pose({
        "Head": (0, 0, 1.6),
        "UpperArm_L": (2, 0, 6), "UpperArm_R": (2, 0, -6),
        "Forearm_L": (-8, 0, 0), "Forearm_R": (-8, 0, 0),
    })


def classmate_pose() -> Pose:
    return Pose({
        "Head": (0, 0, 0),
        "UpperArm_L": (0, 0, 1.5), "UpperArm_R": (0, 0, -1.5),
        "Forearm_L": (-2, 0, 0), "Forearm_R": (-2, 0, 0),
    })


def teacher_at_podium() -> Pose:
    return Pose({
        "Hips": (2, 0, 0.8), "Spine": (4, 0, 0.6), "Chest": (2, 0, -0.4),
        "Neck": (6, -5, 0), "Head": (4, -6, 1.4),
        "Shoulder_L": (0, 0, -2), "Shoulder_R": (0, 0, 2),
        "UpperArm_L": (-50, 0, 9), "UpperArm_R": (-52, 0, -11),
        "Forearm_L": (-58, 0, 0), "Forearm_R": (-60, 0, 0),
        "Hand_L": (-10, 0, 0), "Hand_R": (-10, 0, 0),
        "Thigh_L": (-3, 0, 1.5), "Thigh_R": (1, 0, -1.5),
        "Shin_L": (2, 0, 0), "Shin_R": (1, 0, 0),
    })


def teacher_nod() -> Pose:
    pose = teacher_at_podium()
    pose.rot.update({"Neck": (18, -4, 0), "Head": (14, -5, 1), "Chest": (4, 0, -0.4)})
    return pose


def teacher_turn() -> Pose:
    pose = teacher_at_podium()
    pose.rot.update({
        "UpperArm_R": (-62, 0, -18), "Forearm_R": (-78, 0, 0), "Hand_R": (-26, 0, 14),
        "Neck": (8, -14, 0), "Head": (6, -12, 2),
    })
    return pose


def teacher_up() -> Pose:
    pose = teacher_at_podium()
    pose.rot.update({"Spine": (2, 0, 0.5), "Neck": (0, 6, 0), "Head": (-2, 7, 0)})
    return pose


def euler_matrix(euler) -> np.ndarray:
    """Unity applies Z, then X, then Y: R = Ry * Rx * Rz."""
    x, y, z = (math.radians(v) for v in euler)
    rx = np.array([[1, 0, 0], [0, math.cos(x), -math.sin(x)], [0, math.sin(x), math.cos(x)]])
    ry = np.array([[math.cos(y), 0, math.sin(y)], [0, 1, 0], [-math.sin(y), 0, math.cos(y)]])
    rz = np.array([[math.cos(z), -math.sin(z), 0], [math.sin(z), math.cos(z), 0], [0, 0, 1]])
    return ry @ rx @ rz


# ------------------------------------------------------------------------------------------------
# Skinning. The poses are bone rotations, and the mesh is authored in the rest pose, so a point has to
# be carried from rest space into posed space by the bones it is weighted to. This is the same
# linear-blend skinning ProceduralSkeleton.cs does; without it a "posed" sheet is just the bind pose
# with a different title on it.

_POSED = {}


def _posed_positions(proportions: Proportions) -> dict:
    return _POSED


def _make_skin(proportions: Proportions, pose: Pose):
    """Returns point -> [(bone name, weight)], plus stashes the posed bone matrices for _bone_matrix."""
    rest_local = {bone: proportions.local(bone) for bone in BONES}
    rest_world, rest_rotation = {}, {}
    for bone in BONES:
        parent = PARENT[bone]
        if parent is None:
            rest_world[bone] = rest_local[bone]
            rest_rotation[bone] = np.eye(3)
        else:
            rest_world[bone] = rest_world[parent] + rest_rotation[parent] @ rest_local[bone]
            rest_rotation[bone] = rest_rotation[parent]

    posed_world, posed_rotation = {}, {}
    for bone in BONES:
        parent = PARENT[bone]
        local_rotation = euler_matrix(pose.rot.get(bone, (0, 0, 0)))
        if parent is None:
            posed_world[bone] = rest_local[bone]
            posed_rotation[bone] = local_rotation
        else:
            posed_world[bone] = posed_world[parent] + posed_rotation[parent] @ rest_local[bone]
            posed_rotation[bone] = posed_rotation[parent] @ local_rotation

    # skin matrix per bone: rest space -> posed space
    skin_matrices = {}
    for bone in BONES:
        rotation = posed_rotation[bone] @ rest_rotation[bone].T
        matrix = np.eye(4)
        matrix[:3, :3] = rotation
        matrix[:3, 3] = posed_world[bone] - rotation @ rest_world[bone]
        skin_matrices[bone] = matrix
    _SKIN.clear()
    _SKIN.update(skin_matrices)

    _POSED.clear()
    _POSED.update(posed_world)

    def weights_at(point: np.ndarray) -> list:
        """Nearest-two-bones weighting by distance to the bone segment. Coarse on purpose: it is a
        drawing, and the C# carries per-vertex weights that this cannot reproduce. What it does
        reproduce is that a bent joint bends the surface instead of tearing it."""
        distances = []
        for bone in BONES:
            parent = PARENT[bone]
            if parent is None:
                distances.append((np.linalg.norm(point - rest_world[bone]), bone))
                continue
            a, b = rest_world[parent], rest_world[bone]
            direction = b - a
            length_squared = float(direction @ direction)
            if length_squared < 1e-9:
                distances.append((np.linalg.norm(point - b), bone))
                continue
            t = float(np.clip((point - a) @ direction / length_squared, 0.0, 1.0))
            distances.append((np.linalg.norm(point - (a + direction * t)), bone))
        distances.sort()
        first, second = distances[0], distances[1]
        total = first[0] + second[0]
        if total < 1e-6:
            return [(first[1], 1.0)]
        # A hard nearest-bone falloff, softened only inside the joint blend band.
        blend = 0.06
        share = 0.5 * max(0.0, 1.0 - abs(first[0] - second[0]) / blend)
        return [(first[1], 1.0 - share), (second[1], share)]

    return weights_at


_SKIN = {}


def _bone_matrix(bone: str) -> np.ndarray:
    return _SKIN[bone]


def world_positions(proportions: Proportions, pose: Pose) -> dict:
    positions, rotations = {}, {}

    def walk(bone: str) -> None:
        parent = PARENT[bone]
        local_rotation = euler_matrix(pose.rot.get(bone, (0, 0, 0)))
        local_position = proportions.local(bone)
        if parent is None:
            positions[bone] = local_position
            rotations[bone] = local_rotation
        else:
            positions[bone] = positions[parent] + rotations[parent] @ local_position
            rotations[bone] = rotations[parent] @ local_rotation
        for child, candidate in PARENT.items():
            if candidate == bone:
                walk(child)

    walk("Hips")
    return positions


# ------------------------------------------------------------------------------------------------
# Mesh, mirrored from SkinnedFigureFactory.BuildBody. Each entry is (kind, data); drawn in order.

PALETTE = {
    "protagonist": {"skin": "#C9A487", "hair": "#2A2730", "coat": "#7E6A55", "shirt": "#EFE8D6", "ink": "#23211F"},
    "q": {"skin": "#C2B49C", "hair": "#3B3A36", "coat": "#71806B", "shirt": "#DCD8C8", "ink": "#23211F"},
    "teacher": {"skin": "#D9CDBE", "hair": "#4A4C56", "coat": "#9BAC97", "shirt": "#FAF7EE", "ink": "#23211F"},
    "trousers": "#5A5B60",
    "boots": "#23211F",
}


def body_parts(p: Proportions, role: str) -> list:
    """(kind, data, surface) in the same order the C# writes them: legs, torso, arms, head, hair."""
    parts = []
    cardigan = role == "teacher"

    # Trousers: one tube hip -> knee -> ankle, tapering, with a ring at the knee.
    for side in ("L", "R"):
        thigh = p.rest(f"Thigh_{side}")
        knee = p.rest(f"Shin_{side}")
        ankle = p.rest(f"Foot_{side}")
        spine = [thigh, thigh + (knee - thigh) * 0.45, knee, knee + (ankle - knee) * 0.55, ankle]
        parts.append(("tube", (spine, [0.074, 0.065, 0.055, 0.048, 0.044]), "trousers"))
        parts.append(("box", (ankle + np.array([0, -0.035, 0.045]), np.array([0.052, 0.032, 0.115])), "boots"))
        parts.append(("ellipsoid", (ankle + np.array([0, -0.018, 0.055]), np.array([0.05, 0.038, 0.1])), "boots"))

    hips = p.rest("Hips")
    spine = p.rest("Spine")
    chest = p.rest("Chest")
    neck = p.rest("Neck")

    # Torso: five rings, pinched at the waist and flared at the hem for N.
    radii = [0.134, 0.104, 0.116, 0.104, 0.056] if cardigan else [0.112, 0.116, 0.126, 0.112, 0.06]
    parts.append(("tube", ([hips + np.array([0, 0.06, 0]), spine, chest, chest + (neck - chest) * 0.5, neck], radii), "coat"))

    cap = 0.046 if cardigan else 0.056
    for side in ("L", "R"):
        parts.append(("ellipsoid", (p.rest(f"Shoulder_{side}") + np.array([0, 0.004, 0]),
                                    np.array([cap, cap * 0.9, cap])), "coat"))

    # Sleeves and hands.
    for side in ("L", "R"):
        upper = p.rest(f"UpperArm_{side}")
        elbow = p.rest(f"Forearm_{side}")
        wrist = p.rest(f"Hand_{side}")
        line = [upper, upper + (elbow - upper) * 0.5, elbow, elbow + (wrist - elbow) * 0.55, elbow + (wrist - elbow) * 0.94]
        parts.append(("tube", (line, [0.05, 0.045, 0.041, 0.036, 0.031]), "coat"))
        parts.append(("box", (wrist + np.array([0, -0.03, 0.004]), np.array([0.026, 0.042, 0.016])), "skin"))

    # Head: skull and jaw. The hair is per-role.
    head = p.rest("Head")
    skull = head + np.array([0, p.head_length * 0.52, 0.004])
    parts.append(("ellipsoid", (skull, np.array([p.head_radius, p.head_length * 0.5, p.head_radius * 1.02])), "skin"))
    parts.append(("ellipsoid", (head + np.array([0, p.head_length * 0.2, 0.024]),
                                np.array([p.head_radius * 0.8, p.head_length * 0.24, p.head_radius * 0.84])), "skin"))

    if cardigan:
        parts.append(("ellipsoid", (skull + np.array([0, p.head_length * 0.07, -p.head_radius * 0.035]),
                                    np.array([p.head_radius * 1.10, p.head_length * 0.47, p.head_radius * 1.12])), "hair"))
    else:
        parts.append(("ellipsoid", (skull + np.array([0, p.head_length * 0.07, -0.006]),
                                    np.array([p.head_radius * 1.09, p.head_length * 0.47, p.head_radius * 1.1])), "hair"))

    # Cloth that is not a torso: the protagonist's coat tail, N's cardigan hem and cuffs.
    if role == "protagonist":
        waist = p.rest("Spine")
        hem = p.rest("Hips") + np.array([0, -0.31, 0])
        parts.append(("clothtube", ([waist, waist + (hem - waist) * 0.55, hem], [0.132, 0.152, 0.168]), "coat"))

    if cardigan:
        hem = hips + np.array([0, -0.135, 0])
        parts.append(("clothtube", ([hips + np.array([0, 0.045, 0]), hips, hem], [0.134, 0.140, 0.132]), "coat"))
        for side in ("L", "R"):
            elbow = p.rest(f"Forearm_{side}")
            wrist = p.rest(f"Hand_{side}")
            a = elbow + (wrist - elbow) * 0.86
            b = elbow + (wrist - elbow) * 0.97
            parts.append(("tube", ([a, b], [0.039, 0.038]), "coat"))

    return parts


# ------------------------------------------------------------------------------------------------
# Drawing helpers.


try:  # imported here, not in main(), because the drawing helpers below need it before main runs.
    from matplotlib.patches import Polygon, Rectangle
except ImportError:  # pragma: no cover - report it from main(), where there is a user to tell.
    Polygon = Rectangle = None

INK = "#23211F"
PAPER = "#F4EFE2"
MUTED = "#8E8A84"


def _project3d(points: np.ndarray, view_yaw: float) -> np.ndarray:
    """Orthographic projection from world (x, y, z) to sheet (u, v), with the camera turned by yaw."""
    radians = math.radians(view_yaw)
    cos, sin = math.cos(radians), math.sin(radians)
    return np.column_stack([points[:, 0] * cos + points[:, 2] * sin, points[:, 1]])


def _xform(points, skin) -> np.ndarray:
    """Apply the skin to a list of rest-pose points. Points are 3-vectors; the result is (n, 2) of
    (x, y), because the sheets that matter here are orthographic front views."""
    array = np.atleast_2d(np.asarray(points, dtype=float))
    out = np.empty((len(array), 3))
    for index, point in enumerate(array):
        weight = skin(point)
        homogeneous = np.append(point, 1.0)
        total = np.zeros(3)
        for bone, share in weight:
            total += (_bone_matrix(bone) @ homogeneous)[:3] * share
        out[index] = total
    return out


def _to_figure(u, v, scale, origin):
    """(u, v) in metres -> (x, y) on the axes, as a flat (n, 2) array for matplotlib patches."""
    return np.column_stack([origin[0] + np.asarray(u, dtype=float).ravel() * scale,
                            origin[1] + np.asarray(v, dtype=float).ravel() * scale])


def _fill(ax, uv, colour, scale, origin, alpha, zorder=3):
    xy = _to_figure(uv[:, 0], uv[:, 1], scale, origin)
    ax.fill(xy[:, 0], xy[:, 1], color=colour, alpha=alpha, lw=0, zorder=zorder)


def _ellipse(ax, centre, radii, colour, scale, origin, alpha=1.0, edge=None, lw=0.8, zorder=3):
    """A flat ellipse in metres, for the face sheet (which needs no skinning: the head is the bone)."""
    theta = np.linspace(0, 2 * math.pi, 60)
    xy = _to_figure(centre[0] + radii[0] * np.cos(theta), centre[1] + radii[1] * np.sin(theta), scale, origin)
    ax.fill(xy[:, 0], xy[:, 1], color=colour, alpha=alpha, lw=0, zorder=zorder)
    if edge:
        ax.plot(xy[:, 0], xy[:, 1], color=edge, lw=lw, zorder=zorder + 1)


def _ellipse_polygon(p, radii, z):
    theta = np.linspace(0, 2 * math.pi, 48)
    return [(p[0] + radii[0] * math.cos(t), p[1] + radii[1] * math.sin(t), z) for t in theta]


def _tube(ax, spine, radii, colour, skin, scale, origin, alpha=1.0, rings=True, view_yaw=0.0, zorder=2):
    """One silhouette per tapered tube, skinned.

    The rest-pose spine and its radii go through the same weights the C# gives the mesh, so a bent
    elbow bends the sleeve instead of leaving it hanging in bind pose. Each ring is pushed out along
    the screen direction perpendicular to the local spine, in world space, then skinned.
    """
    spine = [np.asarray(point, dtype=float) for point in spine]
    ring_points = []
    for index, point in enumerate(spine):
        previous = spine[max(0, index - 1)]
        following = spine[min(len(spine) - 1, index + 1)]
        # The offset direction is the view-space perpendicular to the segment, so a camera turned off
        # the front still gets round limbs instead of flattened ones.
        flat = _project3d(np.array([following - previous]), view_yaw)[0]
        perpendicular = np.array([flat[1], -flat[0], 0.0])
        if np.linalg.norm(perpendicular) < 1e-6:
            perpendicular = np.array([1.0, 0.0, 0.0])
        perpendicular /= np.linalg.norm(perpendicular)
        radius = radii[index] if index < len(radii) else radii[-1]
        ring_points.append((point - perpendicular * radius, point + perpendicular * radius))

    left = np.array([pair[0] for pair in ring_points])
    right = np.array([pair[1] for pair in ring_points])
    polygon = _project3d(_xform(np.vstack([left, right[::-1]]), skin), view_yaw)
    ax.add_patch(Polygon(_to_figure(polygon[:, 0], polygon[:, 1], scale, origin), closed=True,
                         facecolor=colour, alpha=alpha, lw=0, zorder=zorder))

    if rings:
        for x, y in ((ring_points[0][0], ring_points[0][1]), (ring_points[-1][0], ring_points[-1][1])):
            centre = (np.asarray(x) + np.asarray(y)) / 2.0
            radius = float(np.linalg.norm(np.asarray(y) - np.asarray(x)) / 2.0)
            cap = np.array(_ellipse_polygon(centre, (radius, radius), 0.0))
            uv = _project3d(_xform(cap, skin), view_yaw)
            _fill(ax, uv, colour, scale, origin, alpha, zorder)


def draw_figure(ax, proportions: Proportions, pose: Pose, role: str, scale: float, origin, alpha=1.0,
                view_yaw: float = 0.0):
    """Draw the figure the way the mesh would actually look under the pose: every primitive is skinned.

    <paramref name="view_yaw"/> turns the camera around the figure's vertical axis. The sheet keeps it
    at 0, because the outlines are rebuilt in view space: at 35 degrees the sleeve and hem polygons
    tear open along the seams where the two sides of a tube have different depths. A front view is
    blind to the z axis, so B states the measured displacement in words instead of pretending to show
    it. The parameter stays for a future renderer that shades rings properly.
    """
    pose.apply(proportions)
    positions = _posed_positions(proportions)
    skin = _make_skin(proportions, pose)
    colours = PALETTE[role]

    # Draw order by surface, not by build order. The C# writes the skull and then the hair shell over
    # it, which is right in three dimensions (the face pokes out of the shell's front) and wrong in a
    # flat projection (the shell hides the face). So cloth goes down first, then hair, then skin.
    # The C# writes the skull and then the hair shell over it, which is what a 3D body wants (the face
    # pokes out of the shell's front) and what a flat front view wants too: hair over skin, skin over
    # cloth, cloth over trousers.
    zorder = {"trousers": 2, "boots": 2, "coat": 3, "skin": 4, "hair": 5, "shirt": 6}
    for kind, data, surface in body_parts(proportions, role):
        colour = colours.get(surface) or PALETTE.get(surface) or "#8E8A84"
        layer = zorder.get(surface, 3)
        if kind in ("tube", "clothtube"):
            spine, radii = data
            _tube(ax, spine, radii, colour, skin, scale, origin, alpha, rings=(kind == "tube"),
                  view_yaw=view_yaw, zorder=layer)
        elif kind == "ellipsoid":
            centre, radii = data
            uv = _project3d(_xform(_ellipse_polygon(centre, (radii[0], radii[1]), 0.0), skin), view_yaw)
            _fill(ax, uv, colour, scale, origin, alpha, layer)
        elif kind == "box":
            centre, half = data
            corners = [(centre[0] + sx * half[0], centre[1] + sy * half[1], centre[2])
                       for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
            uv = _project3d(_xform(corners, skin), view_yaw)
            _fill(ax, uv, colour, scale, origin, alpha, layer)

    return positions


def draw_face(ax, p: Proportions, origin, scale: float = 1.0) -> None:
    """N's face at 1:1 metres, with the marks SkinnedFigureFactory.BuildTeacherFace writes.

    Drawn in the order the C# builds it — hair shell, then skull and jaw on top of it, then the marks
    as flat quads — so the sheet shows what the mesh would actually show, and the caller's panel size
    decides how large it reads.
    """
    head = np.array([origin[0], origin[1], 0.0])
    skull = head + np.array([0, p.head_length * 0.52 * scale, 0.0])
    hl, hr = p.head_length * scale, p.head_radius * scale
    colours = PALETTE["teacher"]

    # 1. the bob: a shell pulled back and cut above the jaw
    _ellipse(ax, skull + np.array([0, hl * 0.075, 0]), (hr * 1.10, hl * 0.44), colours["hair"],
             1.0, (0.0, 0.0), zorder=3)

    # 2. skull and jaw, on top of the shell, so the face is skin and only the outline is hair
    _ellipse(ax, skull, (hr, hl * 0.5), colours["skin"], 1.0, (0.0, 0.0), zorder=4)
    _ellipse(ax, head + np.array([0, hl * 0.2, 0]), (hr * 0.8, hl * 0.24), colours["skin"], 1.0, (0.0, 0.0), zorder=4)

    def mark(x, y, half_width, half_height, zorder=6):
        ax.add_patch(plt_rect(skull[0] + x - half_width, skull[1] + y - half_height,
                              half_width * 2, half_height * 2, INK, zorder))

    brow_y = hl * 0.145
    eye_x = hr * 0.365
    eye_half = hr * 0.20
    eye_y = hl * 0.02

    mark(-eye_x, brow_y, eye_half * 0.95, 0.0026)
    mark(eye_x - 0.0015, brow_y + 0.001, eye_half * 0.9, 0.0026)
    for side in (-1, 1):
        x = side * eye_x
        _ellipse(ax, np.array([skull[0] + x, skull[1] + eye_y]), (eye_half, hl * 0.019), INK, 1.0, (0.0, 0.0), zorder=6)
        mark(x, eye_y - hl * 0.031, eye_half * 0.92, 0.0014)
    mark(0.002, -hl * 0.185, hr * 0.115, 0.0022)
    mark(0.0, -hl * 0.26, hr * 0.20, 0.0028)
    mark(hr * 0.20, -hl * 0.268, 0.0022, 0.0045)
    for side in (-1, 1):
        mark(side * hr * 0.245, -hl * 0.215, 0.0009, hl * 0.032)

    # The face bands from the model standard, chin 0 to crown 1, so the face is checkable.
    chin = skull[1] - hl * 0.5
    for fraction, label, colour in ((0.47, "eye line 0.47", "#8E5A54"), (0.53, "", "#8E5A54"),
                                    (0.30, "nose base 0.30", "#59637A"), (0.36, "", "#59637A")):
        y = chin + fraction * hl
        ax.plot([skull[0] - 1.02 * hr, skull[0] + 1.02 * hr], [y, y],
                color=colour, lw=0.7, ls=(0, (2.4, 1.6)), zorder=8)
        if label:
            ax.text(skull[0] + 1.10 * hr, y, label, fontsize=6.2, color=colour, va="center", clip_on=True)


def plt_rect(x, y, width, height, colour, zorder=6):
    return Rectangle((x, y), width, height, facecolor=colour, lw=0, zorder=zorder)


def fit_metres(ax, figure, x_min, x_max, y_min, y_max) -> tuple:
    """Set limits to the requested data box, then widen it to the panel's true aspect.

    The figure panels must be 1:1 or a height comparison is meaningless, and a panel with 1:1 data in
    a panel that is not 1:1 leaves dead space and overlapping labels. So the data box is grown, never
    squashed: the scale stays uniform and the panel is filled.
    """
    bbox = ax.get_position()
    panel_aspect = (bbox.width * figure.get_figwidth()) / (bbox.height * figure.get_figheight())
    width = x_max - x_min
    height = y_max - y_min
    data_aspect = width / height
    if panel_aspect > data_aspect:
        extra = height * panel_aspect - width
        x_min -= extra / 2
        x_max += extra / 2
    else:
        extra = width / panel_aspect - height
        y_min -= extra / 2
        y_max += extra / 2
    ax.set_xlim(x_min, x_max)
    ax.set_ylim(y_min, y_max)
    ax.set_aspect("equal")
    return (x_min, x_max, y_min, y_max)


def grey(hex_colour: str) -> float:
    value = hex_colour.lstrip("#")
    r, g, b = (int(value[i:i + 2], 16) / 255 for i in (0, 2, 4))
    return 0.299 * r + 0.587 * g + 0.114 * b


# ------------------------------------------------------------------------------------------------
# The sheet.


def main() -> int:
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
    except ImportError:
        print("matplotlib and numpy are required: pip install matplotlib numpy", file=sys.stderr)
        return 2

    out = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "docs/images/teacher-n-proof.png"

    figure = plt.figure(figsize=(16.6, 13.4), dpi=120)
    figure.patch.set_facecolor(PAPER)

    figure.text(0.020, 0.955, "N  ·  the biology substitute  ·  offline proof sheet",
                fontsize=19.0, color=INK, fontweight="bold")
    figure.text(0.020, 0.930,
                "Every number is ported by hand from Assets/Scripts/Character and "
                "Assets/Resources/World/classroom_layout.json.",
                fontsize=8.6, color="#5A564F")
    figure.text(0.020, 0.914,
                "Not a Unity render: nothing has been compiled or played. This is a drawing of the numbers, not a screenshot.",
                fontsize=8.6, color="#8E5A54")

    json = __import__("json")
    layout = json.loads((ROOT / "Assets/Resources/World/classroom_layout.json").read_text())
    podium = layout["podium"]

    def panel(left, bottom, width, height, title, subtitle=None):
        ax = figure.add_axes([left, bottom, width, height])
        ax.set_facecolor("#FBF7EC")
        ax.set_xticks([])
        ax.set_yticks([])
        for spine in ax.spines.values():
            spine.set_color("#DDD6C6")
            spine.set_linewidth(0.8)
        # Title and subtitle both sit above the (1:1) data area, in axes fractions, so a fitted panel
        # never pushes its own caption into the panel above it.
        ax.set_title(title, fontsize=9.6, loc="left", color=INK, pad=24)
        if subtitle:
            ax.text(0.0, 1.010, subtitle, transform=ax.transAxes, fontsize=6.8, color="#5A564F", va="bottom")
        return ax

    def draw_podium(ax, x_centre: float, zorder: int = 6) -> None:
        """The podium at its real height: 1.02 m body plus the 0.07 m top slab her hands rest on."""
        ax.add_patch(Rectangle((x_centre - 0.088, 0.0), 0.176, 1.02, facecolor="#B08E62",
                               edgecolor="#8A6B45", lw=0.9, zorder=zorder))
        ax.add_patch(Rectangle((x_centre - 0.095, 1.02), 0.190, 0.07, facecolor="#23211F",
                               edgecolor=INK, lw=0.7, zorder=zorder + 1))

    # ================================ A: the three figures ==================================
    ax_a = panel(0.018, 0.535, 0.470, 0.330,
                 "A · three figures, one scale",
                 "1:1 in metres, one floor line. The podium's near edge is 0.44 m in front of N's feet; "
                 "her wrist reaches 0.442 m, so the heel of the hand lands on the edge itself.")
    n_x = 0.40
    for x, proportions, role, pose, label in (
        (-0.44, protagonist(), "protagonist", rest_pose(), "protagonist"),
        (-0.02, classmate(), "q", classmate_pose(), "Q"),
        (n_x, teacher(), "teacher", teacher_at_podium(), "N"),
    ):
        draw_figure(ax_a, proportions, pose, role, 1.0, (x, 0.0))
        ax_a.plot([x - 0.13, x + 0.13], [0.0, 0.0], color=INK, lw=1.3, zorder=9)
        # Under the floor line, aligned to that figure, so three labels never meet.
        ax_a.text(x, -0.055, label, fontsize=6.5, color="#3A362F", ha="center", va="top")
        ax_a.text(x, proportions.height + 0.04, f"{proportions.height:.2f} m", fontsize=7.2,
                  color="#3A362F", ha="center", va="bottom")

    # The podium itself is drawn in B, where it is the subject. Here her hands are simply on it,
    # marked where the pose actually puts them: the heel of the right hand, 0.072 m below the hand
    # bone (HumanoidClipTests measures the same offset), which must land on the podium's cap.
    _make_skin(teacher(), teacher_at_podium())
    heel = _POSED["Hand_R"] - np.array([0.0, 0.072, 0.0])
    ax_a.plot([n_x + heel[0]], [heel[1]], "o", ms=3.0, color="#B08E62", zorder=9)
    ax_a.annotate(f"her hands: palm heel\nat {heel[1]:.2f} m", (n_x + heel[0], heel[1]), fontsize=6.0,
                  color="#5A564F", xytext=(30, -6), textcoords="offset points", ha="left")
    for x, heads in ((n_x, "7.37 heads"), (-0.02, "6.87 heads"), (-0.44, "7.02 heads")):
        ax_a.text(x, -0.185, heads, fontsize=6.0, color=MUTED, ha="center", va="top")
    fit_metres(ax_a, figure, -0.58, 0.52, -0.34, 1.98)

    # ================================ B: the loop ===========================================
    ax_b = panel(0.505, 0.535, 0.478, 0.330,
                 "B · TeacherAtPodiumIdle: a nod, one hand, then a second of nothing",
                 "A 3.6 s loop. The pause is longer than either gesture: the gestures do not add up to "
                 "control.")
    for index, (pose, label) in enumerate((
        (teacher_at_podium(), "0.0 s"), (teacher_nod(), "0.7 s"),
        (teacher_turn(), "2.0 s"), (teacher_up(), "3.4 s"),
    )):
        origin = (0.22 + index * 0.46, 0.0)
        draw_figure(ax_b, teacher(), pose, "teacher", 1.0, origin)
        draw_podium(ax_b, origin[0] + 0.20, zorder=4)
        ax_b.plot([origin[0] - 0.14, origin[0] + 0.34], [0.0, 0.0], color="#C9C3B4", lw=1.0, zorder=1)
        ax_b.text(origin[0] + 0.12, -0.050, label, fontsize=6.6, color="#3A362F", ha="center", va="top")
    # A front view cannot show a nod or a hand reaching forward, so each beat is captioned with what
    # actually moved, measured through the same forward kinematics this sheet draws with. The text is
    # built from the numbers rather than typed beside them, so it cannot drift out of agreement.
    _make_skin(teacher(), teacher_at_podium())
    base = {bone: _POSED[bone].copy() for bone in ("Head", "Hand_R")}
    captions = [(0.22, "base", "hands on the podium")]
    for pose, label in ((teacher_nod(), "the nod"), (teacher_turn(), "the register"),
                        (teacher_up(), "up at the room")):
        _make_skin(teacher(), pose)
        head_mm = (_POSED["Head"][1] - base["Head"][1]) * 1000.0
        hand_mm = (_POSED["Hand_R"] - base["Hand_R"]) * 1000.0
        # One line per caption, and only the movement that is the beat: a nod also carries the hand
        # down 15 mm with the spine, but that is the body settling, not a gesture she makes.
        if abs(hand_mm[1]) >= 20.0 or abs(hand_mm[0]) >= 20.0:
            measured = f"hand {abs(hand_mm[1]):.0f} mm {'up' if hand_mm[1] > 0 else 'down'}, " \
                       f"{abs(hand_mm[0]):.0f} mm {'in' if hand_mm[0] < 0 else 'out'}"
        else:
            measured = f"head {abs(head_mm):.1f} mm {'down' if head_mm < 0 else 'up'}"
        captions.append((0.22 + len(captions) * 0.46, label, measured))

    for index, (x, label, measured) in enumerate(captions):
        row = index % 2
        ax_b.text(x, -0.180 - row * 0.105, label, fontsize=6.2, color="#3A362F", ha="center")
        ax_b.text(x, -0.235 - row * 0.105, measured, fontsize=5.5, color=MUTED, ha="center")
    fit_metres(ax_b, figure, 0.04, 1.72, -0.40, 1.98)

    # ================================ C: the face ===========================================
    ax_c = panel(0.018, 0.300, 0.235, 0.205, "C · her face at 3:1",
                 "Dashed: the standard's eye line (0.47-0.53)\nand nose base (0.30-0.36), chin 0 to crown 1.")
    draw_face(ax_c, teacher(), (0.0, 0.0), scale=3.0)
    fit_metres(ax_c, figure, -0.27, 0.42, -0.09, 0.735)

    # ================================ D: the palette ========================================
    ax_d = panel(0.272, 0.300, 0.711, 0.205, "D · three value keys",
                 "Dark protagonist, mid Q, bright and cold N. Her cardigan is 0.170 above Q's coat and 0.215 "
                 "above the protagonist's.")
    for row_index, (name, surfaces) in enumerate((
        ("protagonist", [("skin", "#C9A487"), ("hair", "#2A2730"), ("coat", "#7E6A55"), ("shirt", "#EFE8D6")]),
        ("Q", [("skin", "#C2B49C"), ("hair", "#3B3A36"), ("coat", "#71806B"), ("shirt", "#DCD8C8")]),
        ("N", [("skin", "#D9CDBE"), ("hair", "#4A4C56"), ("cardigan", "#9BAC97"), ("shirt", "#FAF7EE")]),
    )):
        # Rows are 0.62 apart and each swatch is 0.40 tall: the numbers live inside the swatch, so no
        # label can land on a neighbouring row (which is how "0.968" disappeared on an earlier draft).
        y = 1.62 - row_index * 0.62
        ax_d.text(-0.02, y + 0.20, name, fontsize=8.2, color=INK, ha="right", va="center")
        for surface_index, (surface, colour) in enumerate(surfaces):
            x = surface_index * 0.25
            ax_d.add_patch(Rectangle((x, y), 0.235, 0.40, facecolor=colour, edgecolor="#C9C3B4", lw=0.7))
            value = grey(colour)
            ink = "#FBF7EC" if value < 0.45 else INK
            ax_d.text(x + 0.010, y + 0.27, surface, fontsize=6.6, color=ink)
            ax_d.text(x + 0.010, y + 0.075, f"{value:.3f}", fontsize=7.0, color=ink)

    ramp_y = -0.72
    for step in range(26):
        value = step / 25
        band = int(round(value * 255))
        ax_d.add_patch(Rectangle((step * 0.0385, ramp_y), 0.0385, 0.34,
                                 facecolor=(band / 255, band / 255, band / 255), lw=0))
    # P and Q are four hundredths apart, so they share one tick; N gets its own.
    ax_d.plot([0.430, 0.430], [ramp_y, ramp_y + 0.34], color="#8E5A54", lw=1.2)
    ax_d.plot([0.645, 0.645], [ramp_y, ramp_y + 0.34], color="#8E5A54", lw=1.2)
    ax_d.text(0.430, ramp_y - 0.06, "P / Q coats 0.430 / 0.475", fontsize=6.2, color="#8E5A54",
              ha="center", va="top")
    ax_d.text(0.645, ramp_y - 0.06, "N cardigan 0.645", fontsize=6.2, color="#8E5A54", ha="left", va="top")
    ax_d.text(1.02, ramp_y + 0.17, "grey value\n0 → 1", fontsize=6.4, color=MUTED, va="center")
    ax_d.set_xlim(-0.18, 1.16)
    ax_d.set_ylim(-1.10, 2.10)

    # ================================ E: the walk-in ========================================
    ax_e = panel(0.018, 0.020, 0.300, 0.235,
                 "E · the walk-in",
                 "4.51 m in 3.2 s (1.41 m/s). Tightest clearance to any desk: 0.44 m. The path never enters "
                 "the seating grid.")
    room = layout["room"]
    width, depth, center_z = room["width"], room["depth"], room["centerZ"]
    min_z = center_z - depth / 2
    max_z = center_z + depth / 2
    ax_e.add_patch(Rectangle((-width / 2, min_z), width, depth, facecolor="#EDE6D5",
                             edgecolor="#9A9384", lw=1.0))
    seating = layout["seating"]
    for column in seating["columns"]:
        for row in seating["rows"]:
            ax_e.add_patch(Rectangle((column - seating["deskWidth"] / 2, row - seating["deskDepth"] / 2),
                                     seating["deskWidth"], seating["deskDepth"], facecolor="#C4B7A0",
                                     edgecolor="#9A9384", lw=0.5, zorder=2))
    wake = layout["wakeDesk"]
    ax_e.add_patch(Rectangle((-wake["width"] / 2, wake["positionZ"] - wake["depth"] / 2),
                             wake["width"], wake["depth"], facecolor="#D8C3A0", edgecolor="#9A9384",
                             lw=0.8, zorder=2, hatch="//"))
    ax_e.text(0, wake["positionZ"], "wake desk", fontsize=5.8, color="#5A564F", ha="center", va="center")
    ax_e.add_patch(Rectangle((podium["positionX"] - podium["width"] / 2, podium["positionZ"] - podium["depth"] / 2),
                             podium["width"], podium["depth"], facecolor="#B08E62", edgecolor="#8A6B45",
                             lw=0.9, zorder=3))
    ax_e.annotate("podium", (0, podium["positionZ"] - 0.4), fontsize=5.8, color="#3A362F",
                  ha="center", xytext=(0, -10), textcoords="offset points")
    path = [(p["x"], p["z"]) for p in layout["teacher"]["entryPath"]]
    ax_e.plot([p[0] for p in path], [p[1] for p in path], color="#8E5A54", lw=1.8, ls=(0, (4, 2)), zorder=6)
    for point in path:
        ax_e.plot([point[0]], [point[1]], "o", ms=3.0, color="#8E5A54", zorder=7)
    stand = layout["teacher"]["stand"]
    ax_e.plot([stand["x"]], [stand["y"]], "o", ms=6.6, color="#9BAC97", markeredgecolor=INK, zorder=8)
    ax_e.annotate("her mark", (stand["x"], stand["y"]), textcoords="offset points",
                  xytext=(17, -15), fontsize=6.0, color=INK)
    ax_e.annotate("staff door", (path[0][0], path[0][1]), textcoords="offset points", xytext=(-6, -9),
                  fontsize=6.0, color="#8E5A54", ha="center", va="top")
    # Inside the room's top-right corner: the top edge itself is where the path enters, and a label
    # above the edge lands on the panel's subtitle.
    ax_e.annotate("blackboard", (width / 2, max_z), textcoords="offset points", xytext=(-2, -4),
                  fontsize=5.8, color=MUTED, ha="right", va="top")
    fit_metres(ax_e, figure, -width / 2 - 0.4, width / 2 + 0.4, min_z - 0.4, max_z + 0.4)

    # ================================ F: the numbers ========================================
    ax_f = panel(0.335, 0.020, 0.270, 0.235, "F · what was added")
    entries = [
        ("proportions · TeacherN()", [
            f"height        {teacher().height:.3f} m  {teacher().heads_tall:.2f} heads",
            f"shoulders     {teacher().shoulder[0] * 2 + teacher().upper_arm[0] * 2:.3f} m",
            f"              P {protagonist().shoulder[0] * 2 + protagonist().upper_arm[0] * 2:.3f}  "
            f"Q {classmate().shoulder[0] * 2 + classmate().upper_arm[0] * 2:.3f}",
            f"head length   {teacher().head_length:.3f} m  (P {protagonist().head_length:.3f})",
            f"chest depth   {teacher().chest_depth:.3f} m",
        ]),
        ("mesh · the TeacherN branch", [
            "cardigan hem  +0.045 .. -0.135 from the hips",
            "cuffs         0.86 .. 0.97 of the forearm",
            "hair          cropped bob, jaw cut, ears clear",
            "face          brows, lids, nose base, mouth",
        ]),
        ("rig", [
            "the same 19 bones, the same clips, one mesh",
        ]),
    ]
    y = 1.02
    for heading, lines in entries:
        ax_f.text(0.0, y, heading, fontsize=7.0, color="#8E5A54", va="top")
        y -= 0.095
        for line in lines:
            ax_f.text(0.015, y, line, fontsize=6.0, color="#3A362F", va="top", family="DejaVu Sans Mono")
            y -= 0.052
        y -= 0.042
    ax_f.set_xlim(0, 1)
    ax_f.set_ylim(0, 1.10)

    # ================================ G: the opening ========================================
    ax_g = panel(0.620, 0.020, 0.362, 0.235, "G · the opening after N · unscaled seconds")
    for index, (time, line) in enumerate((
        ("0.00", "protagonist face-down on the desk; Q idles on the mark"),
        ("0.00", "N starts walking in from the staff door, on her own clock"),
        ("1.15", "Q crosses the room while the protagonist is still getting up"),
        ("3.20", "N reaches the mark, turns to the room, hands on the podium"),
        ("3.40", "the protagonist stands; teacher_arrived is set; her trigger turns on"),
        ("3.55", "the prologue starts and Q asks about the dream"),
        ("end", "her scene is the next thing pressable in the room"),
    )):
        y = 0.93 - index * 0.138
        ax_g.text(0.0, y, time, fontsize=6.2, color="#8E5A54", va="top", family="DejaVu Sans Mono")
        ax_g.text(0.09, y, line, fontsize=6.3, color="#3A362F", va="top")
    ax_g.plot([0.072, 0.072], [0.02, 0.99], color="#D5CFC0", lw=0.8)
    ax_g.set_xlim(0, 1)
    ax_g.set_ylim(-0.02, 1.02)

    out.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(out, facecolor=PAPER)
    print(f"wrote {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
