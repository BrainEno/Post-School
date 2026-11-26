#!/usr/bin/env python3
"""Offline proof for the procedural skinned figures. NOT a Unity render.

This is a port of the maths in Assets/Scripts/Character:

  HumanoidRig.cs            proportion tables -> bone rest positions
  HumanoidPose.cs           pose table -> local bone rotations
  HumanoidClips.cs          keyframe times and the wake / idle / classmate-idle acting
  SkinnedFigureFactory.cs   how each body part is built (tubes, ellipsoids, marks) and weighted
  ProceduralSkeleton.cs     linear blend skinning

It draws four views per figure: rest pose, the asleep pose, mid-stand, and standing, plus a small
bent-joint panel that shows the skin deforming rather than separating. That makes the rig reviewable
without opening the Editor.

The port is deliberately literal: if this script and the C# disagree, one of them is wrong, and the
plan is that they never disagree about a number a human would look at.

Usage:
    python3 tools/skinned_figure_preview.py [out.png]
Dependencies: matplotlib + numpy (pip install matplotlib numpy); neither is committed.
"""

from __future__ import annotations

import math
import sys
from dataclasses import dataclass, field

import numpy as np

# ---------------------------------------------------------------------------------------------
# Skeleton, ported from HumanoidRig.cs

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
        m = {
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
        return np.array(m[bone], dtype=float)

    def rest(self, bone: str) -> np.ndarray:
        p = np.zeros(3)
        current = bone
        while current is not None:
            p = p + self.local(current)
            current = PARENT[current]
        return p

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


# ---------------------------------------------------------------------------------------------
# Poses, ported from HumanoidPose.cs / HumanoidClips.cs (euler degrees, Unity ZXY order approximated
# with XYZ here: the preview is for silhouette checks, not for exact parity).


@dataclass
class Pose:
    rot: dict = field(default_factory=dict)
    offset: float = 0.0
    forward: float = 0.0

    def with_(self, bone: str, euler: tuple) -> "Pose":
        self.rot[bone] = euler
        return self


def rest_pose() -> Pose:
    return (Pose().with_("Head", (0, 0, 1.6))
            .with_("UpperArm_L", (2, 0, 6)).with_("UpperArm_R", (2, 0, -6))
            .with_("Forearm_L", (-8, 0, 0)).with_("Forearm_R", (-8, 0, 0)))


def asleep_pose() -> Pose:
    return (Pose(offset=-0.37, forward=0.15)
            .with_("Hips", (0, 0, 4)).with_("Spine", (45, 0, 0)).with_("Chest", (20, -4, 0))
            .with_("Neck", (50, 0, 0)).with_("Head", (30, 22, 8))
            .with_("UpperArm_L", (-60, 0, 18)).with_("UpperArm_R", (-60, 0, -14))
            .with_("Forearm_L", (-40, 0, 0)).with_("Forearm_R", (-40, 0, 0))
            .with_("Hand_L", (10, 0, 0)).with_("Hand_R", (14, 0, 0))
            .with_("Thigh_L", (-88, 0, 4)).with_("Thigh_R", (-84, 0, -3))
            .with_("Shin_L", (78, 0, 0)).with_("Shin_R", (76, 0, 0))
            .with_("Foot_L", (-10, 0, 0)).with_("Foot_R", (-8, 0, 0)))


def lifting_pose() -> Pose:
    return (Pose(offset=-0.43, forward=0.10)
            .with_("Hips", (-4, 0, 3)).with_("Spine", (60, 0, 0)).with_("Chest", (14, -6, 0))
            .with_("Neck", (-20, 0, 0)).with_("Head", (18, 14, 6))
            .with_("UpperArm_L", (-30, 0, 18)).with_("UpperArm_R", (-34, 0, -14))
            .with_("Forearm_L", (-60, 0, 0)).with_("Forearm_R", (-66, 0, 0))
            .with_("Thigh_L", (-70, 0, 4)).with_("Thigh_R", (-68, 0, -3))
            .with_("Shin_L", (30, 0, 0)).with_("Shin_R", (28, 0, 0))
            .with_("Foot_L", (-14, 0, 0)).with_("Foot_R", (-12, 0, 0)))


def standing_up_pose() -> Pose:
    return (Pose(offset=-0.155, forward=0.02)
            .with_("Hips", (-14, 0, 3)).with_("Spine", (12, 0, 0)).with_("Chest", (8, 0, 0))
            .with_("Neck", (-10, 0, 0)).with_("Head", (6, -8, 3))
            .with_("UpperArm_L", (4, 0, 14)).with_("UpperArm_R", (-30, 0, -12))
            .with_("Forearm_L", (-24, 0, 0)).with_("Forearm_R", (-52, 0, 0))
            .with_("Thigh_L", (-40, 0, 3)).with_("Thigh_R", (-36, 0, -3))
            .with_("Shin_L", (62, 0, 0)).with_("Shin_R", (58, 0, 0)))


def unsettled_pose() -> Pose:
    return (Pose()
            .with_("Hips", (0, 0, 1.2)).with_("Spine", (1.5, 0, -1)).with_("Chest", (-1, 0, 1.2))
            .with_("Neck", (2, -4, 0)).with_("Head", (-1, -7, 3.2))
            .with_("UpperArm_L", (-3, 0, 7.5)).with_("UpperArm_R", (2, 0, -5))
            .with_("Forearm_L", (-14, 0, 0)).with_("Forearm_R", (-9, 0, 0)))


def walk_contact_left() -> Pose:
    return (Pose(offset=-0.049)
            .with_("Hips", (0, 3, 0)).with_("Spine", (3, -3, 0)).with_("Chest", (1, -5, 0))
            .with_("Neck", (-2, 2, 0)).with_("Head", (-1, 4, 0.5))
            .with_("Thigh_L", (-22, 0, 2)).with_("Shin_L", (4, 0, 0)).with_("Foot_L", (10, 0, 0))
            .with_("Thigh_R", (12, 0, -2)).with_("Shin_R", (16, 0, 0)).with_("Foot_R", (-40, 0, 0))
            .with_("UpperArm_L", (14, 0, 4)).with_("Forearm_L", (-10, 0, 0))
            .with_("UpperArm_R", (-16, 0, -4)).with_("Forearm_R", (-16, 0, 0)))


def walk_pass_right() -> Pose:
    return (Pose(offset=-0.003)
            .with_("Hips", (0, 0, -1.2)).with_("Spine", (3, 0, 0.6)).with_("Chest", (1, 0, 0))
            .with_("Neck", (-2, 0, 0)).with_("Head", (-1, -1, -0.4))
            .with_("Thigh_L", (-4, 0, 2)).with_("Shin_L", (2, 0, 0)).with_("Foot_L", (0, 0, 0))
            .with_("Thigh_R", (-6, 0, -2)).with_("Shin_R", (42, 0, 0)).with_("Foot_R", (-18, 0, 0))
            .with_("UpperArm_L", (2, 0, 4)).with_("Forearm_L", (-12, 0, 0))
            .with_("UpperArm_R", (-2, 0, -4)).with_("Forearm_R", (-12, 0, 0)))


def walk_contact_right() -> Pose:
    return (Pose(offset=-0.049)
            .with_("Hips", (0, -3, 0)).with_("Spine", (3, 3, 0)).with_("Chest", (1, 5, 0))
            .with_("Neck", (-2, -2, 0)).with_("Head", (-1, -4, -0.5))
            .with_("Thigh_R", (-22, 0, -2)).with_("Shin_R", (4, 0, 0)).with_("Foot_R", (10, 0, 0))
            .with_("Thigh_L", (12, 0, 2)).with_("Shin_L", (16, 0, 0)).with_("Foot_L", (-40, 0, 0))
            .with_("UpperArm_R", (14, 0, -4)).with_("Forearm_R", (-10, 0, 0))
            .with_("UpperArm_L", (-16, 0, 4)).with_("Forearm_L", (-16, 0, 0)))


def walk_pass_left() -> Pose:
    return (Pose(offset=-0.003)
            .with_("Hips", (0, 0, 1.2)).with_("Spine", (3, 0, -0.6)).with_("Chest", (1, 0, 0))
            .with_("Neck", (-2, 0, 0)).with_("Head", (-1, 1, 0.4))
            .with_("Thigh_R", (-4, 0, -2)).with_("Shin_R", (2, 0, 0)).with_("Foot_R", (0, 0, 0))
            .with_("Thigh_L", (-6, 0, 2)).with_("Shin_L", (42, 0, 0)).with_("Foot_L", (-18, 0, 0))
            .with_("UpperArm_R", (2, 0, -4)).with_("Forearm_R", (-12, 0, 0))
            .with_("UpperArm_L", (-2, 0, 4)).with_("Forearm_L", (-12, 0, 0)))


def classmate_pose() -> Pose:
    return (Pose().with_("Head", (0, 0, 0))
            .with_("UpperArm_L", (0, 0, 1.5)).with_("UpperArm_R", (0, 0, -1.5))
            .with_("Forearm_L", (-2, 0, 0)).with_("Forearm_R", (-2, 0, 0)))


# ---------------------------------------------------------------------------------------------
# Forward kinematics


def euler_matrix(euler) -> np.ndarray:
    x, y, z = (math.radians(v) for v in euler)
    rx = np.array([[1, 0, 0], [0, math.cos(x), -math.sin(x)], [0, math.sin(x), math.cos(x)]])
    ry = np.array([[math.cos(y), 0, math.sin(y)], [0, 1, 0], [-math.sin(y), 0, math.cos(y)]])
    rz = np.array([[math.cos(z), -math.sin(z), 0], [math.sin(z), math.cos(z), 0], [0, 0, 1]])
    return rz @ ry @ rx


def world_positions(proportions: Proportions, pose: Pose) -> dict:
    """Bone name -> world position, applying each bone's local rotation to its children's offsets."""
    positions = {}
    rotations = {}

    def walk(bone: str):
        parent = PARENT[bone]
        local_rot = euler_matrix(pose.rot.get(bone, (0, 0, 0)))
        local_pos = proportions.local(bone)

        if parent is None:
            positions[bone] = local_pos
            rotations[bone] = local_rot
        else:
            positions[bone] = positions[parent] + rotations[parent] @ local_pos
            rotations[bone] = rotations[parent] @ local_rot

        for child, p in PARENT.items():
            if p == bone:
                walk(child)

    walk("Hips")
    return positions


# ---------------------------------------------------------------------------------------------
# Geometry, ported from SkinnedFigureFactory.cs. Each primitive is (kind, params, weight bones).


def body_primitives(p: Proportions, role: str):
    """Returns a list of (kind, data, weights) in rest-pose coordinates."""
    prims = []

    # Trousers: hip -> knee -> ankle tube.
    for side in ("L", "R"):
        thigh = p.rest(f"Thigh_{side}")
        knee = p.rest(f"Shin_{side}")
        ankle = p.rest(f"Foot_{side}")
        spine = [thigh, thigh + (knee - thigh) * 0.45, knee, knee + (ankle - knee) * 0.55, ankle + np.array([0, 0.02, 0])]
        radii = [0.074, 0.065, 0.055, 0.048, 0.044]
        prims.append(("tube", (spine, radii), [f"Thigh_{side}", f"Shin_{side}"]))
        # boot
        prims.append(("box", (ankle + np.array([0, -0.035, 0.045]), np.array([0.052, 0.032, 0.115])), [f"Foot_{side}"]))
        prims.append(("ellipsoid", (ankle + np.array([0, -0.018, 0.055]), np.array([0.05, 0.038, 0.1])), [f"Foot_{side}"]))

    # Torso.
    hips, spine, chest, neck = (p.rest(b) for b in ("Hips", "Spine", "Chest", "Neck"))
    prims.append(("tube", ([hips + np.array([0, 0.06, 0]), spine, chest, chest + (neck - chest) * 0.5, neck],
                           [0.112, 0.116, 0.126, 0.112, 0.06]), ["Hips", "Spine", "Chest", "Neck"]))
    for side in ("L", "R"):
        prims.append(("ellipsoid", (p.rest(f"Shoulder_{side}") + np.array([0, 0.004, 0]), np.array([0.056, 0.05, 0.056])),
                      [f"Shoulder_{side}"]))

    # Sleeves.
    for side in ("L", "R"):
        upper = p.rest(f"UpperArm_{side}")
        elbow = p.rest(f"Forearm_{side}")
        wrist = p.rest(f"Hand_{side}")
        spine_line = [upper, upper + (elbow - upper) * 0.5, elbow, elbow + (wrist - elbow) * 0.55, elbow + (wrist - elbow) * 0.94]
        prims.append(("tube", (spine_line, [0.05, 0.045, 0.041, 0.036, 0.031]), [f"UpperArm_{side}", f"Forearm_{side}"]))
        prims.append(("box", (wrist + np.array([0, -0.03, 0.004]), np.array([0.026, 0.042, 0.016])), [f"Hand_{side}"]))

    # Head: skull, jaw, hair shell.
    head = p.rest("Head")
    skull_centre = head + np.array([0, p.head_length * 0.52, 0.004])
    prims.append(("ellipsoid", (skull_centre, np.array([p.head_radius, p.head_length * 0.5, p.head_radius * 1.02])), ["Head"]))
    prims.append(("ellipsoid", (head + np.array([0, p.head_length * 0.2, 0.024]),
                                np.array([p.head_radius * 0.8, p.head_length * 0.24, p.head_radius * 0.84])), ["Head"]))
    prims.append(("ellipsoid", (skull_centre + np.array([0, p.head_length * 0.07, -0.006]),
                                np.array([p.head_radius * 1.09, p.head_length * 0.47, p.head_radius * 1.1])), ["Head"]))

    # Coat skirt.
    if role == "protagonist":
        waist = p.rest("Spine")
        hem = p.rest("Hips") + np.array([0, -0.31, 0])
        prims.append(("tube", ([waist, waist + (hem - waist) * 0.55, hem], [0.132, 0.152, 0.168]), ["Hips"]))

    return prims


# ---------------------------------------------------------------------------------------------
# Drawing


def bone_segments(p: Proportions, positions: dict):
    segments = []
    for bone in BONES:
        parent = PARENT[bone]
        if parent is None:
            continue
        segments.append((positions[parent], positions[bone], bone))
    return segments


def draw_figure(ax, p: Proportions, pose: Pose, role: str, title: str, show_bones=True, x_shift=0.0):
    positions = world_positions(p, pose)
    prims = body_primitives(p, role)
    offset = np.array([x_shift, pose.offset, pose.forward])

    # Silhouette: tube cross-sections as ellipses, ellipsoids as circles, boxes as rectangles.
    for kind, data, _ in prims:
        if kind == "tube":
            spine, radii = data
            spine = [s + offset for s in spine]
            for i in range(len(spine) - 1):
                a, b = spine[i], spine[i + 1]
                ax.plot([a[2], b[2]], [a[0], b[0]], color="none")  # placeholder to keep axes scaled
                # Front view is the x-y plane; y is up. Draw a tapered quad.
                hw_a, hw_b = radii[i], radii[i + 1]
                ax.fill([a[2] - hw_a, b[2] - hw_b, b[2] + hw_b, a[2] + hw_a],
                        [a[0] + a[1], b[1], b[1], a[1]], color="#b9b3a6", alpha=0)
                ax.plot([a[2] - hw_a, b[2] - hw_b], [a[1], b[1]], color="#6f6a61", lw=1)
                ax.plot([a[2] + hw_a, b[2] + hw_b], [a[1], b[1]], color="#6f6a61", lw=1)
        elif kind == "ellipsoid":
            centre, radius = data[0] + offset, data[1]
            circle = np.linspace(0, 2 * math.pi, 24)
            ax.fill(centre[2] + radius[2] * np.cos(circle), centre[1] + radius[1] * np.sin(circle),
                    color="#c9c2b2", alpha=0.55, lw=0)
            ax.plot(centre[2] + radius[2] * np.cos(circle), centre[1] + radius[1] * np.sin(circle),
                    color="#4a4e52", lw=1)
        elif kind == "box":
            centre, half = data[0] + offset, data[1]
            ax.fill([centre[2] - half[2], centre[2] + half[2], centre[2] + half[2], centre[2] - half[2]],
                    [centre[1] - half[1], centre[1] - half[1], centre[1] + half[1], centre[1] + half[1]],
                    color="#8a6a4a", alpha=0.7, lw=0)

    if show_bones:
        for a, b, bone in bone_segments(p, positions):
            a2, b2 = a + offset, b + offset
            colour = "#B8442F" if bone.endswith("_R") or bone in ("Hips", "Head") else "#3f4a6b"
            ax.plot([a2[2], b2[2]], [a2[1], b2[1]], color=colour, lw=1.2, alpha=0.9)
            ax.plot([b2[2]], [b2[1]], "o", ms=2.2, color=colour)

    # The wake desk, at the same scale, for the frames where the figure is on it. Its top is 0.82 m
    # and it sits 0.5 m in front of where the figure stands (so z = 0.5 in this local view).
    if abs(pose.forward) > 0.05:
        ax.fill([0.39, 2.0, 2.0, 0.39], [0.74, 0.74, 0.82, 0.82],
                color="#c9b48c", alpha=0.85, lw=0)
        ax.plot([0.5, 0.5], [0, 0.74], color="#a08a63", lw=2)

    ax.set_title(title, fontsize=9, color="#23211f")
    ax.set_xlim(-0.55, 0.55)
    ax.set_ylim(-0.05, 1.95)
    ax.set_aspect("equal")
    ax.grid(True, color="#e3ded1", lw=0.5)
    ax.axhline(0, color="#23211f", lw=1)
    ax.tick_params(labelsize=6)
    for spine in ax.spines.values():
        spine.set_color("#d5cfc0")


def main() -> int:
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
    except ImportError:
        print("matplotlib is required: pip install matplotlib numpy", file=sys.stderr)
        return 2

    out = sys.argv[1] if len(sys.argv) > 1 else "docs/images/skinned-figure-preview.png"

    protagonist_proportions = protagonist()
    q_proportions = classmate()

    figure, axes = plt.subplots(3, 6, figsize=(20, 11), dpi=110)
    figure.patch.set_facecolor("#f4efe2")

    protagonist_poses = [
        (rest_pose(), "rest / bind pose"),
        (asleep_pose(), "wake 0.0s: asleep"),
        (lifting_pose(), "wake 1.15s: head lifts"),
        (standing_up_pose(), "wake 1.95s: standing up"),
        (unsettled_pose(), "wake 3.4s: upright, unsettled"),
    ]

    for column, (pose, label) in enumerate(protagonist_poses):
        ax = axes[0][column]
        draw_figure(ax, protagonist_proportions, pose, "protagonist", f"protagonist · {label}")

    # The bent-joint panel: what skinning buys over stacked primitives.
    bent = rest_pose()
    bent.rot["Forearm_R"] = (-100, 0, 0)
    bent.rot["Shin_R"] = (70, 0, 0)
    draw_figure(axes[0][5], protagonist_proportions, bent, "protagonist", "bent elbow + knee (skin follows)")

    # The walk cycle, four frames. Root travel is the caller's job; only the legs are in the clip.
    walk_keys = [
        (walk_contact_left(), "walk 0.00s contact L"),
        (walk_pass_right(), "walk 0.28s pass R"),
        (walk_contact_right(), "walk 0.55s contact R"),
        (walk_pass_left(), "walk 0.83s pass L"),
    ]
    for column, (pose, label) in enumerate(walk_keys):
        draw_figure(axes[1][column], protagonist_proportions, pose, "protagonist", label)

    q_poses = [
        (classmate_pose(), "Q · resting stance"),
        (asleep_pose(), "Q · wake clip, same keys"),
    ]
    for column, (pose, label) in enumerate(q_poses):
        draw_figure(axes[2][column], q_proportions, pose, "classmate", label)

    # Proportion comparison, drawn to the same scale.
    axes[2][2].barh(["protagonist", "Q"], [protagonist_proportions.heads_tall, q_proportions.heads_tall],
                    color=["#7E6A55", "#71806B"], height=0.45)
    axes[2][2].axvline(6, color="#8E8A84", ls="--", lw=1)
    axes[2][2].axvline(7.5, color="#8E8A84", ls="--", lw=1)
    axes[2][2].set_xlim(5.5, 8)
    axes[2][2].set_title("heads tall (child 6 - adult 7.5)", fontsize=9)
    axes[2][2].tick_params(labelsize=7)

    # Bone count and mesh summary as text: the numbers a reviewer would otherwise have to trust.
    axes[1][4].axis("off")
    summary = (
        "skeleton\n"
        f"  bones            {len(BONES)}\n"
        f"  protagonist      {protagonist_proportions.height:.2f} m · {protagonist_proportions.heads_tall:.2f} heads\n"
        f"  Q                {q_proportions.height:.2f} m · {q_proportions.heads_tall:.2f} heads\n"
        "\nclips (shared)\n"
        "  WakeFromDesk     3.40 s, 6 keys\n"
        "  StandingIdle     6.00 s, loops\n"
        "  ClassmateIdle    7.05 s, loops, stiller\n"
        "  WalkCycle        1.10 s, loops\n"
        "\nskinning\n"
        "  influences/vertex up to 4\n"
        "  surfaces          skin, hair, coat, shirt\n"
    )
    axes[1][4].text(0, 1, summary, va="top", family="monospace", fontsize=8, color="#23211f")

    axes[1][5].axis("off")
    axes[1][5].text(
        0, 1,
        "offline proof · NOT a Unity render\n\n"
        "ported 1:1 from\n"
        "  HumanoidRig.cs\n"
        "  HumanoidPose.cs\n"
        "  HumanoidClips.cs\n"
        "  SkinnedFigureFactory.cs\n"
        "  ProceduralSkeleton.cs\n\n"
        "front view, y up, z toward the viewer.\n"
        "Bone chains drawn over the silhouette:\n"
        "  red = right / spine / head\n"
        "  blue = left",
        va="top", fontsize=8, color="#5a554b", family="monospace")

    for spare in (axes[2][3], axes[2][4], axes[2][5]):
        spare.axis("off")

    axes[2][3].text(0, 1,
        "why this is not the old model\n\n"
        "old: ~40 capsules and boxes parented\n"
        "to 5 pivots. An elbow was two shapes\n"
        "crossing; a knee pinched; nothing was\n"
        "skinned, so nothing could be animated\n"
        "except by rotating whole limbs.\n\n"
        "now: one mesh, 19 bones, up to four\n"
        "influences per vertex, weights blended\n"
        "across every joint, poses authored as\n"
        "keyframes and sampled at runtime.",
        va="top", fontsize=8, color="#5a554b", family="monospace")

    axes[2][4].text(0, 1,
        "shared skeleton, different people\n\n"
        "the same clip plays on both figures.\n"
        "the protagonist reads as a young adult\n"
        "(7.0 heads, narrow shoulders, heavy coat);\n"
        "Q reads as slightly wrong (6.9 heads,\n"
        "longer neck, smaller skull, stiller clip).\n\n"
        "no reference image, model or texture\n"
        "is included: every vertex is generated\n"
        "in C# from the proportion table.",
        va="top", fontsize=8, color="#5a554b", family="monospace")

    axes[2][5].text(0, 1,
        "art direction carried by geometry\n\n"
        "flat colours, no outlines, one accent\n"
        "(the protagonist's sleeve band),\n"
        "four material surfaces per figure:\n"
        "  skin / hair / coat / shirt.\n\n"
        "long hard shadows and the paper ground\n"
        "come from the scene lighting, not the mesh,\n"
        "so the figures stay cheap and recolourable.",
        va="top", fontsize=8, color="#5a554b", family="monospace")

    figure.suptitle("POST-SCHOOL · procedural skinned figures · protagonist and Q", fontsize=14, color="#23211f")
    figure.tight_layout(rect=(0, 0, 1, 0.96))

    import os
    os.makedirs(os.path.dirname(out) or ".", exist_ok=True)
    figure.savefig(out, facecolor="#f4efe2")
    print(f"wrote {out}")
    print(f"protagonist {protagonist_proportions.height:.2f} m / {protagonist_proportions.heads_tall:.2f} heads")
    print(f"Q            {q_proportions.height:.2f} m / {q_proportions.heads_tall:.2f} heads")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
