"""Image -> textured GLB via free TRELLIS Hugging Face Spaces (MIT licence).

usage: python -I trellis_gen.py <image> <out.glb> [v2|v1] [faces=20000] [texture=1024]
  v2 = microsoft/TRELLIS.2 (better, ~90s GPU per job), v1 = trellis-community/TRELLIS (lighter)
Put a free Hugging Face read token in .hf_token next to this script (or set HF_TOKEN)
for a bigger ZeroGPU quota than anonymous use.
"""
import os, shutil, sys, time

sys.path.insert(0, "D:/ai-art/tools/pylib")
from gradio_client import Client, handle_file

img, out = sys.argv[1], sys.argv[2]
ver = sys.argv[3] if len(sys.argv) > 3 else "v2"
faces = int(sys.argv[4]) if len(sys.argv) > 4 else 20000
tex = int(sys.argv[5]) if len(sys.argv) > 5 else 1024
token_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), ".hf_token")
token = os.environ.get("HF_TOKEN") or (open(token_file).read().strip() if os.path.exists(token_file) else None)

def run():
  if ver == "v2":
    c = Client("microsoft/TRELLIS.2", token=token, verbose=False, httpx_kwargs=HTTPX)
    c.predict(api_name="/start_session")
    prep = c.predict(handle_file(img), api_name="/preprocess_image")
    c.predict(handle_file(prep), 0, "1024", api_name="/image_to_3d")
    glb, _ = c.predict(faces, tex, api_name="/extract_glb")
  else:
    c = Client("trellis-community/TRELLIS", token=token, verbose=False, httpx_kwargs=HTTPX)
    c.predict(api_name="/start_session")
    prep = c.predict(handle_file(img), api_name="/preprocess_image")
    # v1 simplifies by ratio, not face count; 0.95 lands near phone budgets for a single subject.
    _, _, glb = c.predict(handle_file(prep), [], 0, 7.5, 12, 3.0, 12, "stochastic", 0.95, tex,
                          api_name="/generate_and_extract_glb")
  return glb


# This connection is slow to Hugging Face, so allow long reads and retry transient network failures.
HTTPX = {"timeout": 300}
t0 = time.time()
for attempt in range(4):
    try:
        glb = run()
        break
    except Exception as e:
        if "quota" in str(e).lower() or "minimum value" in str(e) or attempt == 3:
            raise
        print(f"retry {attempt + 1}: {type(e).__name__}: {str(e)[:120]}", flush=True)
        time.sleep(10)

os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
shutil.copy(glb, out)
print(f"OK {out} {os.path.getsize(out)//1024} KB in {time.time()-t0:.0f}s")
