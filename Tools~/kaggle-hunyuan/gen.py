# Food Inc Game: every asset-pack sheet -> textured GLB with Hunyuan3D-2 (open weights) on a free Kaggle T4.
# Shapes first for every sheet, then textures, so only one big model sits on the GPU at a time.
import glob, json, os, subprocess, sys, time, traceback

def sh(cmd, cwd=None):
    print("$", cmd, flush=True)
    r = subprocess.run(cmd, shell=True, cwd=cwd, capture_output=True, text=True)
    print(r.stdout[-3000:], r.stderr[-3000:], flush=True)
    if r.returncode:
        raise SystemExit(f"failed: {cmd}")

t0 = time.time()
REPO = "/tmp/Hunyuan3D-2"  # outside /kaggle/working so it is not saved as output
OUT = "/kaggle/working/out"
os.makedirs(OUT, exist_ok=True)

sh(f"git clone --depth 1 https://github.com/Tencent/Hunyuan3D-2 {REPO}")
sh("pip install -q diffusers transformers accelerate einops omegaconf trimesh pygltflib xatlas pymeshlab "
   "opencv-python-headless rembg onnxruntime ninja pybind11 'numpy<2.3'")
# Newer diffusers refuses to run the repo's own multiview pipeline without trust_remote_code.
mv = f"{REPO}/hy3dgen/texgen/utils/multiview_utils.py"
src = open(mv).read()
assert "custom_pipeline=custom_pipeline_path," in src
open(mv, "w").write(src.replace("custom_pipeline=custom_pipeline_path,", "custom_pipeline=custom_pipeline_path, trust_remote_code=True,"))
sh("pip install -q --no-build-isolation .", cwd=f"{REPO}/hy3dgen/texgen/custom_rasterizer")
sh("python setup.py build_ext --inplace", cwd=f"{REPO}/hy3dgen/texgen/differentiable_renderer")
sys.path.insert(0, REPO)
print(f"setup done in {time.time()-t0:.0f}s", flush=True)

import torch
from PIL import Image
from hy3dgen.rembg import BackgroundRemover
from hy3dgen.shapegen import Hunyuan3DDiTFlowMatchingPipeline, FloaterRemover, DegenerateFaceRemover, FaceReducer

sh("find /kaggle/input -maxdepth 4 | head -40")
sheets = sorted(glob.glob("/kaggle/input/**/*.jpg", recursive=True) + glob.glob("/kaggle/input/**/*.png", recursive=True))
if not sheets:
    raise SystemExit("no sheets mounted under /kaggle/input")
only = os.environ.get("ONLY", "")  # comma list of sheet names for a smoke test; empty = all
if only:
    sheets = [s for s in sheets if os.path.splitext(os.path.basename(s))[0] in only.split(",")]
print("sheets:", [os.path.basename(s) for s in sheets], flush=True)

rembg = BackgroundRemover()
images = {}
for s in sheets:
    name = os.path.splitext(os.path.basename(s))[0]
    img = Image.open(s).convert("RGBA")
    images[name] = rembg(img.convert("RGB"))
    images[name].save(f"{OUT}/{name}.input.png")

report = {}
shape = Hunyuan3DDiTFlowMatchingPipeline.from_pretrained("tencent/Hunyuan3D-2")
for name, img in images.items():
    try:
        t = time.time()
        mesh = shape(image=img, num_inference_steps=50, octree_resolution=380, num_chunks=20000,
                     generator=torch.manual_seed(1234), output_type="trimesh")[0]
        mesh = FloaterRemover()(mesh)
        mesh = DegenerateFaceRemover()(mesh)
        mesh = FaceReducer()(mesh, max_facenum=40000)
        mesh.export(f"{OUT}/{name}.shape.glb")
        report[name] = {"shape_s": round(time.time() - t), "faces": int(len(mesh.faces))}
        print("SHAPE", name, report[name], flush=True)
    except Exception:
        report[name] = {"error": traceback.format_exc()[-800:]}
        print("SHAPE FAIL", name, report[name]["error"], flush=True)
del shape
torch.cuda.empty_cache()

import trimesh
from hy3dgen.texgen import Hunyuan3DPaintPipeline
paint = Hunyuan3DPaintPipeline.from_pretrained("tencent/Hunyuan3D-2")
for name, img in images.items():
    if "error" in report.get(name, {}):
        continue
    try:
        t = time.time()
        mesh = trimesh.load(f"{OUT}/{name}.shape.glb", force="mesh")
        mesh = paint(mesh, image=img)
        # glTF defaults metallic to 1 when unset, which renders the painted colours as dark metal.
        mesh.visual.material.metallicFactor = 0.0
        mesh.export(f"{OUT}/{name}.glb")
        report[name]["paint_s"] = round(time.time() - t)
        print("PAINT", name, report[name], flush=True)
    except Exception:
        report[name]["error"] = traceback.format_exc()[-800:]
        print("PAINT FAIL", name, report[name]["error"], flush=True)
    json.dump(report, open(f"{OUT}/report.json", "w"), indent=1)

json.dump(report, open(f"{OUT}/report.json", "w"), indent=1)
print(f"ALL DONE in {time.time()-t0:.0f}s", flush=True)
