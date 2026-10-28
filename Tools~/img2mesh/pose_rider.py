"""Bend an A-pose character mesh into a seated riding pose, no rig needed.

Usage: python pose_rider.py <in.glb> <out.glb> [height_m] [grip_x grip_y grip_z]
Thighs swing forward from the hip, shins hang from the knee, arms reach forward to the bars and the torso leans in.
Joint heights are fractions of the figure height, measured on the Food Inc characters (big head, short legs).
Output is in metres with the seat (hip pivot) at the origin, facing +Z like the input.

With a grip (metres, relative to the seat: half the bar width, height above the seat, distance ahead of it),
the arm swing, arm tuck and torso lean are solved so each hand lands on its handlebar grip.
"""
import itertools
import sys
import numpy as np
import trimesh

src, out = sys.argv[1], sys.argv[2]
height_m = float(sys.argv[3]) if len(sys.argv) > 3 else 1.7
grip = np.array([float(a) for a in sys.argv[4:7]]) if len(sys.argv) > 6 else None

HIP, KNEE, SHOULDER = 0.40, 0.18, 0.68
THIGH, SHIN = -70.0, 74.0
DEFAULT_ARMS = (-62.0, 34.0, 14.0)  # arm forward swing, arm tuck toward the body, torso lean

scene = trimesh.load(src)
geom = list(scene.geometry.values()) if isinstance(scene, trimesh.Scene) else [scene]


def rx(deg):
    a = np.radians(deg)
    return np.array([[1, 0, 0], [0, np.cos(a), -np.sin(a)], [0, np.sin(a), np.cos(a)]])


def rz(deg):
    a = np.radians(deg)
    return np.array([[np.cos(a), -np.sin(a), 0], [np.sin(a), np.cos(a), 0], [0, 0, 1]])


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def rotate(v, pivot, R, w):
    """Rotate each vertex about pivot by R, blended per vertex by weight w (slerp-free: lerp of positions)."""
    moved = (v - pivot) @ R.T + pivot
    return v + (moved - v) * w[:, None]


allv = np.vstack([g.vertices for g in geom])
y0, y1 = allv[:, 1].min(), allv[:, 1].max()
H = y1 - y0
k = height_m / H
hip = np.array([0.0, y0 + HIP * H, 0.0])
knee_y = y0 + KNEE * H
sh_y = y0 + SHOULDER * H


def arm_weight(v):
    f = (v[:, 1] - y0) / H
    ax = np.abs(v[:, 0])
    # Arms: outside the torso between hip and shoulder height.
    # Stops below the shoulder line so the hat brim, which is just as wide, stays put.
    arm = np.clip(smooth(0.20 * H / 2, 0.27 * H / 2, ax) * smooth(HIP - 0.03, HIP + 0.02, f) * (1 - smooth(SHOULDER + 0.01, SHOULDER + 0.05, f)), 0, 1)
    # Hands hang below hip height in the A-pose but sit well outside the legs: always arm.
    return np.maximum(arm, smooth(0.34 * H / 2, 0.38 * H / 2, ax) * (f > KNEE + 0.1))


def pose_upper(v, arm, fwd, tuck, lean):
    f = (v[:, 1] - y0) / H
    for side in (-1, 1):
        w = arm * (np.sign(v[:, 0]) == side)
        pivot = np.array([side * 0.11 * H, sh_y, 0.0])
        v = rotate(v, pivot, rx(fwd) @ rz(-side * tuck), w)
    # Lean the whole upper body (arms included) forward about the hip.
    return rotate(v, hip, rx(lean), smooth(HIP - 0.02, HIP + 0.04, f))


def pose_legs(v, arm):
    f = (v[:, 1] - y0) / H
    # Legs: thigh forward about the hip, then shin back down about the moved knee.
    leg = (1 - smooth(HIP - 0.04, HIP + 0.01, f)) * (1 - arm)
    v = rotate(v, hip, rx(THIGH), leg)
    knee = (np.array([0.0, knee_y, 0.0]) - hip) @ rx(THIGH).T + hip
    shin = (1 - smooth(KNEE - 0.03, KNEE + 0.03, f)) * (1 - arm)
    return rotate(v, knee, rx(SHIN), shin)


arms = DEFAULT_ARMS
if grip is not None:
    # Hands: the arm vertices furthest out from the body in the A-pose (the outermost few percent).
    hv = allv
    aw = arm_weight(hv)
    cand = np.where(aw > 0.99)[0]
    ax = np.abs(hv[cand, 0])
    hands = {s: cand[(np.sign(hv[cand, 0]) == s) & (ax > np.percentile(ax, 94))] for s in (-1, 1)}
    best = None
    for fwd, tuck, lean in itertools.product(np.arange(-95, -19, 3), np.arange(0, 66, 3), np.arange(0, 31, 3)):
        p = (pose_upper(hv, aw, fwd, tuck, lean) - hip) * k
        err = sum(np.linalg.norm(p[hands[s]].mean(0) - grip * [s, 1, 1]) for s in (-1, 1))
        if best is None or err < best[0]:
            best = (err, fwd, tuck, lean)
    err, *arms = best
    print(f"grip solve: forward {arms[0]}, tuck {arms[1]}, lean {arms[2]}, hand error {err / 2 * 100:.1f} cm per hand")

for g in geom:
    v = g.vertices.copy()
    arm = arm_weight(v)
    v = pose_upper(v, arm, *arms)
    g.vertices = pose_legs(v, arm)

for g in geom:
    g.vertices = (g.vertices - hip) * k

scene.export(out)
print(f"posed {src} -> {out}: scale {k:.3f}, seat at origin")
