"""Reduce a textured GLB to a target triangle count for the game, re-baking its texture.

Usage: python decimate.py <in.glb> <out.glb> <target_faces> [texture_px]
The projected models are split along many UV seams, so collapsing edges in place tears holes.
Instead: weld by position, simplify the closed shape (quadric, MeshLab), unwrap fresh UVs (MeshLab),
and bake colour from the full-detail model by nearest-surface lookup.
"""
import sys
import numpy as np
import pymeshlab
import trimesh
from PIL import Image
from scipy.spatial import cKDTree

src, out, target = sys.argv[1], sys.argv[2], int(sys.argv[3])
tex_px = int(sys.argv[4]) if len(sys.argv) > 4 else 1024

hi = trimesh.load(src, force='mesh')
faces_in = len(hi.faces)
tex = np.asarray(hi.visual.material.baseColorTexture.convert('RGB'), dtype=np.float32) / 255.0
th, tw = tex.shape[:2]

# Dense coloured point cloud of the original surface: the colour source for the bake.
pts, fidx = trimesh.sample.sample_surface_even(hi, 600000) if faces_in > 0 else (None, None)
bary = trimesh.triangles.points_to_barycentric(hi.triangles[fidx], pts)
uv_pts = (hi.visual.uv[hi.faces[fidx]] * bary[:, :, None]).sum(1)
px = np.clip((uv_pts[:, 0] * (tw - 1)).astype(int), 0, tw - 1)
py = np.clip(((1 - uv_pts[:, 1]) * (th - 1)).astype(int), 0, th - 1)
colors = tex[py, px]
tree = cKDTree(pts)

# Geometry only: weld seams, then simplify.
welded = trimesh.Trimesh(vertices=hi.vertices, faces=hi.faces, process=True)
welded.merge_vertices(merge_tex=True, merge_norm=True)
ms = pymeshlab.MeshSet()
ms.add_mesh(pymeshlab.Mesh(vertex_matrix=welded.vertices, face_matrix=welded.faces))
ms.meshing_merge_close_vertices()
ms.meshing_decimation_quadric_edge_collapse(targetfacenum=target, qualitythr=0.4, preservenormal=True, optimalplacement=True, planarquadric=True)
m = ms.current_mesh()
V, F = m.vertex_matrix(), m.face_matrix()

# Fresh UVs: one small tile per triangle. (MeshLab's Voronoi atlas needs manifold meshes, which these
# aren't, and it can hang rather than fail, so it isn't tried.)
ms.compute_texcoord_parametrization_triangle_trivial_per_wedge(textdim=tex_px, border=4)
atlas = ms.current_mesh()
V, F = atlas.vertex_matrix(), atlas.face_matrix()
W = atlas.wedge_tex_coord_matrix().reshape(-1, 3, 2)
Vn = V[F.reshape(-1)]
Fn = np.arange(len(Vn)).reshape(-1, 3)
UV = W.reshape(-1, 2)

# Bake: every covered texel looks up the nearest original surface colour.
img = np.zeros((tex_px, tex_px, 3), np.float32)
mask = np.zeros((tex_px, tex_px), bool)
uvp = UV * (tex_px - 1)
# Two passes: each triangle first fills only texels strictly inside it, then a padded pass fills
# the still-empty texels along its edges, so neighbours in the atlas never overwrite each other.
for pad in (0.0, 0.15):
    for tri in Fn:
        a, b, c = uvp[tri]
        xmin, ymin = np.floor(np.minimum(np.minimum(a, b), c)).astype(int) - 1
        xmax, ymax = np.ceil(np.maximum(np.maximum(a, b), c)).astype(int) + 1
        xs, ys = np.meshgrid(np.arange(xmin, xmax + 1), np.arange(ymin, ymax + 1))
        p = np.stack([xs.ravel() + 0.5, ys.ravel() + 0.5], 1)
        d = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
        if abs(d) < 1e-12:
            continue
        l1 = ((b[1] - c[1]) * (p[:, 0] - c[0]) + (c[0] - b[0]) * (p[:, 1] - c[1])) / d
        l2 = ((c[1] - a[1]) * (p[:, 0] - c[0]) + (a[0] - c[0]) * (p[:, 1] - c[1])) / d
        l3 = 1 - l1 - l2
        inside = (l1 >= -pad) & (l2 >= -pad) & (l3 >= -pad)
        qx = np.clip(xs.ravel(), 0, tex_px - 1)
        qy = tex_px - 1 - np.clip(ys.ravel(), 0, tex_px - 1)
        inside &= ~mask[qy, qx]
        if not inside.any():
            continue
        w = np.clip(np.stack([l1, l2, l3], 1)[inside], 0, 1)
        w /= w.sum(1, keepdims=True)
        _, nn = tree.query(w @ Vn[tri])
        img[qy[inside], qx[inside]] = colors[nn]
        mask[qy[inside], qx[inside]] = True

# Bleed colour a few texels past each island so mip-mapping doesn't pull in black.
for _ in range(6):
    grown = img.copy()
    gm = mask.copy()
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        sh = np.roll(np.roll(img, dy, 0), dx, 1)
        sm = np.roll(np.roll(mask, dy, 0), dx, 1)
        fill = sm & ~gm
        grown[fill] = sh[fill]
        gm |= fill
    img, mask = grown, gm

material = trimesh.visual.material.PBRMaterial(baseColorTexture=Image.fromarray((img * 255).astype(np.uint8)), metallicFactor=0.0, roughnessFactor=0.9)
# Smooth shading: corners take the normal of their welded vertex, not their face.
smooth = trimesh.Trimesh(vertices=V, faces=F, process=False).vertex_normals
low = trimesh.Trimesh(vertices=Vn, faces=Fn, vertex_normals=smooth[F.reshape(-1)],
                      visual=trimesh.visual.TextureVisuals(uv=UV, material=material), process=False)
low.export(out)
print(f"{src}: {faces_in} -> {len(Fn)} faces, {tex_px}px rebake")
