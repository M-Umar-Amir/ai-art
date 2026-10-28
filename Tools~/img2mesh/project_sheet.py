"""Make an AI-generated model match its asset-pack sheet: project the sheet's own pixels onto every
surface the sheet can see, and colour-match the rest of the texture to the sheet.

usage: PYTHONPATH=D:/ai-art/tools/pylib-mesh python project_sheet.py <in.glb> <sheet.jpg> <out.glb> [tex=2048]

1. Fit: try a range of turn angles, project the mesh orthographically, and keep the angle whose silhouette
   best overlaps the sheet's subject (IoU). The mesh's screen box is mapped onto the sheet's subject box.
2. Visibility: z-buffer the mesh from that view so only surfaces the sheet actually shows take its pixels.
3. Bake: rasterise every triangle in UV space at `tex` px; each texel gets the sheet colour, weighted by
   how squarely it faces the camera, blended over the original texture.
4. Colour-match: shift the original texture's Lab mean/std onto the sheet's (for the back and sides).
"""
import sys
import numpy as np
import trimesh
from PIL import Image
from scipy import ndimage

src, sheet_path, out = sys.argv[1], sys.argv[2], sys.argv[3]
TEX = int(sys.argv[4]) if len(sys.argv) > 4 else 2048

scene = trimesh.load(src)
mesh = list(scene.geometry.values())[0]
V = mesh.vertices.astype(np.float64)
F = mesh.faces
UV = mesh.visual.uv.astype(np.float64)
orig = np.asarray(mesh.visual.material.baseColorTexture.convert("RGB").resize((TEX, TEX), Image.BICUBIC), np.float32) / 255

# --- sheet subject mask: anything clearly off the white backdrop, minus the soft floor shadow
sheet_img = Image.open(sheet_path).convert("RGB")
S = np.asarray(sheet_img, np.float32) / 255
sat = S.max(2) - S.min(2)
mask = ((1 - S.min(2)) > 0.12) & ~((sat < 0.06) & (S.mean(2) > 0.72))
mask = ndimage.binary_opening(mask, iterations=2)
lab, n = ndimage.label(mask)
if n:
    sizes = ndimage.sum(mask, lab, range(1, n + 1))
    mask = lab == (1 + int(np.argmax(sizes)))
    mask = ndimage.binary_fill_holes(mask)
ys, xs = np.nonzero(mask)
sb = np.array([xs.min(), ys.min(), xs.max(), ys.max()], np.float64)  # subject box in sheet px


def project(yaw):
    c, s = np.cos(yaw), np.sin(yaw)
    x = V[:, 0] * c + V[:, 2] * s
    z = -V[:, 0] * s + V[:, 2] * c  # toward camera = +z
    return x, V[:, 1], z


def to_sheet(x, y):
    """Map projected model coords onto sheet pixels, fitting the model's screen box onto the subject box."""
    mx0, mx1, my0, my1 = x.min(), x.max(), y.min(), y.max()
    px = sb[0] + (x - mx0) / (mx1 - mx0) * (sb[2] - sb[0])
    py = sb[3] - (y - my0) / (my1 - my0) * (sb[3] - sb[1])
    return px, py


def raster_tris(P, H, W, values=None, depth=None):
    """Tiny scanline-free rasteriser: per triangle, test its pixel box with barycentrics.
    Returns (tri index, barycentrics) per covered pixel, keeping the nearest when depth is given."""
    tri_id = np.full((H, W), -1, np.int64)
    zbuf = np.full((H, W), -np.inf)
    bary = np.zeros((H, W, 3))
    A, B, C = P[F[:, 0]], P[F[:, 1]], P[F[:, 2]]
    for i in range(len(F)):
        a, b, c = A[i], B[i], C[i]
        x0, x1 = int(max(0, np.floor(min(a[0], b[0], c[0])))), int(min(W - 1, np.ceil(max(a[0], b[0], c[0]))))
        y0, y1 = int(max(0, np.floor(min(a[1], b[1], c[1])))), int(min(H - 1, np.ceil(max(a[1], b[1], c[1]))))
        if x1 < x0 or y1 < y0:
            continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        d = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
        if abs(d) < 1e-12:
            continue
        l0 = ((b[1] - c[1]) * (gx - c[0]) + (c[0] - b[0]) * (gy - c[1])) / d
        l1 = ((c[1] - a[1]) * (gx - c[0]) + (a[0] - c[0]) * (gy - c[1])) / d
        l2 = 1 - l0 - l1
        inside = (l0 >= -1e-4) & (l1 >= -1e-4) & (l2 >= -1e-4)
        if not inside.any():
            continue
        yy, xx = np.nonzero(inside)
        yy, xx = yy + y0, xx + x0
        if depth is not None:
            z = l0[inside] * depth[F[i, 0]] + l1[inside] * depth[F[i, 1]] + l2[inside] * depth[F[i, 2]]
            win = z > zbuf[yy, xx]
            yy, xx, z = yy[win], xx[win], z[win]
            zbuf[yy, xx] = z
            sel = inside.copy()
            sel[inside] = win
        else:
            sel = inside
        tri_id[yy, xx] = i
        bary[yy, xx] = np.stack([l0[sel], l1[sel], l2[sel]], -1)
    return tri_id, bary, zbuf


# --- 1. fit the turn angle by silhouette IoU at low res
small = 160
sm = np.asarray(Image.fromarray(mask[int(sb[1]):int(sb[3]) + 1, int(sb[0]):int(sb[2]) + 1].astype(np.uint8) * 255)
                .resize((small, small)), np.float32) > 127
sheet_small = np.asarray(Image.fromarray((S[int(sb[1]):int(sb[3]) + 1, int(sb[0]):int(sb[2]) + 1] * 255).astype(np.uint8))
                         .resize((small, small)), np.float32) / 255
cands = []
for deg in list(range(-40, 41, 5)) + list(range(140, 221, 5)):
    x, y, z = project(np.radians(deg))
    P = np.stack([(x - x.min()) / (x.max() - x.min()) * (small - 1), (y.max() - y) / (y.max() - y.min()) * (small - 1)], -1)
    tid, bry, _ = raster_tris(P, small, small, depth=z)
    sil = tid >= 0
    iou = (sil & sm).sum() / max(1, (sil | sm).sum())
    # colour error of the model's own texture against the sheet: front and back silhouettes are near-mirrors,
    # so the outline alone can pick the back
    uvs = (bry[sil][..., None] * UV[F[tid[sil]]]).sum(1)
    col = orig[np.clip(((1 - uvs[:, 1]) * TEX).astype(int), 0, TEX - 1), np.clip((uvs[:, 0] * TEX).astype(int), 0, TEX - 1)]
    both_px = sm[sil]
    err = np.abs(col[both_px] - sheet_small[sil][both_px]).mean() if both_px.any() else 1.0
    cands.append((iou, err, deg))
top = max(c[0] for c in cands)
iou, err, deg = min((c for c in cands if c[0] >= top - 0.04), key=lambda c: c[1])
print(f"fit: yaw {deg} deg, silhouette IoU {iou:.3f} (best {top:.3f}), colour error {err:.3f}", flush=True)
yaw = np.radians(deg)
x, y, z = project(yaw)
px, py = to_sheet(x, y)

# --- 2. visibility z-buffer in sheet pixel space
H, W = S.shape[:2]
sid, sbary, zbuf = raster_tris(np.stack([px, py], -1), H, W, depth=z)

# --- 2b. optical flow: render the model's own texture from this view, then find where each of its pixels
# sits on the sheet. The box fit is only rough (the sheet is a perspective render with the head turned),
# so without this the face ghosts.
import cv2
hit = sid >= 0
uvp = (sbary[hit][..., None] * UV[F[sid[hit]]]).sum(1)
render = np.ones_like(S)
render[hit] = orig[np.clip(((1 - uvp[:, 1]) * TEX).astype(int), 0, TEX - 1), np.clip((uvp[:, 0] * TEX).astype(int), 0, TEX - 1)]


def gray_norm(img, m):
    g = img.mean(2)
    g = (g - g[m].mean()) / (g[m].std() + 1e-6)
    return np.clip(g * 40 + 128, 0, 255).astype(np.uint8)


dis = cv2.DISOpticalFlow_create(cv2.DISOPTICAL_FLOW_PRESET_MEDIUM)
flow = dis.calc(gray_norm(render, hit), gray_norm(S, mask), None)
flow[~hit] = 0
flow = cv2.GaussianBlur(flow, (0, 0), 3)
print(f"flow: median shift {np.median(np.linalg.norm(flow[hit], axis=1)):.1f}px", flush=True)

# --- 3. bake in UV space
c, s_ = np.cos(yaw), np.sin(yaw)
view = np.array([s_, 0.0, c])  # direction toward the camera in model space
fn = mesh.face_normals
facing = fn @ view
Puv = np.stack([UV[:, 0] * TEX, (1 - UV[:, 1]) * TEX], -1)
tid, bary, _ = raster_tris(Puv, TEX, TEX)
cov = tid >= 0
ti = tid[cov]
bw = bary[cov]
tx = (bw * px[F[ti]]).sum(1)
ty = (bw * py[F[ti]]).sum(1)
tz = (bw * z[F[ti]]).sum(1)
ix, iy = np.clip(tx.astype(int), 0, W - 1), np.clip(ty.astype(int), 0, H - 1)
span = z.max() - z.min()
visible = (tz >= zbuf[iy, ix] - 0.012 * span) & mask[iy, ix]
w = np.clip((facing[ti] - 0.25) / 0.45, 0, 1) * visible
# follow the flow from the model's view onto the sheet before sampling it
tx = tx + flow[iy, ix, 0]
ty = ty + flow[iy, ix, 1]
# bilinear sample of the sheet
x0, y0 = np.clip(np.floor(tx).astype(int), 0, W - 2), np.clip(np.floor(ty).astype(int), 0, H - 2)
fx, fy = (tx - x0)[:, None], (ty - y0)[:, None]
sheet_col = (S[y0, x0] * (1 - fx) * (1 - fy) + S[y0, x0 + 1] * fx * (1 - fy)
             + S[y0 + 1, x0] * (1 - fx) * fy + S[y0 + 1, x0 + 1] * fx * fy)


# --- 4. colour-match the original texture to the sheet (Lab mean/std), using texels both describe
def rgb2lab(rgb):
    a = np.where(rgb > 0.04045, ((rgb + 0.055) / 1.055) ** 2.4, rgb / 12.92)
    M = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]])
    xyz = a @ M.T / np.array([0.9505, 1.0, 1.089])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)


def lab2rgb(lab):
    fy = (lab[..., 0] + 16) / 116
    fx, fz = fy + lab[..., 1] / 500, fy - lab[..., 2] / 200
    f = np.stack([fx, fy, fz], -1)
    xyz = np.where(f > 0.2069, f ** 3, (f - 16 / 116) / 7.787) * np.array([0.9505, 1.0, 1.089])
    Mi = np.array([[3.2406, -1.5372, -0.4986], [-0.9689, 1.8758, 0.0415], [0.0557, -0.2040, 1.0570]])
    a = np.clip(xyz @ Mi.T, 0, 1)
    return np.where(a > 0.0031308, 1.055 * a ** (1 / 2.4) - 0.055, 12.92 * a)


tex_cov = orig[cov]
both = w > 0.7
if both.sum() > 500:
    lo, ls = rgb2lab(tex_cov[both]), rgb2lab(sheet_col[both])
    mo, so = lo.mean(0), lo.std(0) + 1e-6
    ms, ss = ls.mean(0), ls.std(0)
    full = rgb2lab(orig.reshape(-1, 3))
    matched = lab2rgb((full - mo) / so * ss + ms).reshape(orig.shape)
else:
    matched = orig
final = matched.copy()
blend = final[cov]
final[cov] = blend * (1 - w[:, None]) + sheet_col * w[:, None]

# grow the baked colours a few texels past UV island edges so mip-mapping doesn't bleed dark seams
for _ in range(4):
    grown = ndimage.grey_dilation(np.where(cov[..., None], final, 0), size=(3, 3, 1))
    edge = ~cov & (ndimage.binary_dilation(cov))
    final[edge] = grown[edge]
    cov = cov | edge

mesh.visual.material.baseColorTexture = Image.fromarray((np.clip(final, 0, 1) * 255).astype(np.uint8))
mesh.visual.material.baseColorFactor = [255, 255, 255, 255]
mesh.visual.material.metallicFactor = 0.0  # unset means metallic 1 in glTF: dark, mirror-like colours
mesh.export(out)
print(f"projected {int((w > 0.5).sum())} texels from the sheet ({(w > 0.5).mean() * 100:.0f}% of covered); wrote {out}")
