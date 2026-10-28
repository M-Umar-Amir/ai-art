#!/usr/bin/env bash
# Generate every Food Inc Game model from the 24 Oct asset-pack sheets, one at a time.
# Skips models that already exist; stops when the free ZeroGPU quota runs out (re-run later to resume).
set -uo pipefail
echo "== $(date "+%F %T")"
here=/d/ai-art/tools/img2mesh
ref=/g/ai-art/dallas-dash-unity/Assets/_Project/Art/Reference
inp=/g/ai-art/dallas-dash-unity/Assets/_Project/Art/ModelInputs
outdir=/g/ai-art/dallas-dash-unity/Assets/_Project/Art/Models/Generated
export HF_HOME=/d/ai-art/tools/hfcache TEMP=G:/Unity/_tmp TMP=G:/Unity/_tmp
mkdir -p "$outdir" /g/Unity/_tmp
# G: is a USB drive: bail out cleanly if it isn't mounted instead of logging a FAIL per model.
[[ -d "$ref" ]] || { echo "G: not mounted"; exit 3; }

# name | source image | simplify ratio
jobs=(
  "finn|$ref/Characters/02-finn.jpeg|0.15"
  "faye|$ref/Characters/03-faye.jpeg|0.15"
  "fizz-11|$ref/Characters/04-fizz-11.jpeg|0.15"
  "bicycle|$ref/Rides/05-delivery-bicycle.jpeg|0.12"
  "police-bicycle|$ref/Rides/07-police-bicycle.jpeg|0.12"
  "scooter|$ref/Rides/06-courier-scooter.jpeg|0.12"
  "wagyu-bacon-burger|$ref/Collectibles/20-burger.jpeg|0.05"
  "presents|$inp/presents.png|0.05"
  "cone|$inp/cone.png|0.03"
  "roadworks-barrier|$inp/roadworks-barrier.png|0.05"
  "tree-planter|$inp/tree-planter.png|0.08"
  "shopper|$inp/shopper.png|0.12"
  "reunion-tower|$ref/Landmarks/08-reunion-tower.jpeg|0.2"
  "margaret-hunt-hill-bridge|$ref/Landmarks/09-margaret-hunt-hill-bridge.jpeg|0.2"
  "bank-of-america-plaza|$ref/Landmarks/10-bank-of-america-plaza.jpeg|0.2"
  "fountain-place|$ref/Landmarks/11-fountain-place.jpeg|0.2"
  "omni-dallas|$ref/Landmarks/12-omni-dallas-hotel.jpeg|0.2"
)

for j in "${jobs[@]}"; do
  IFS='|' read -r name img ratio <<<"$j"
  [[ -f "$outdir/$name.glb" ]] && { echo "skip $name"; continue; }
  log=$(python -I "$here/trellis_gen.py" "$img" "$here/raw/$name.glb" v2 100000 1024 2>&1)
  if grep -qi "quota" <<<"$log"; then echo "QUOTA $name: $(grep -oi 'try again in [0-9:]*' <<<"$log" | head -1)"; exit 2; fi
  if ! grep -q "^OK" <<<"$log"; then echo "FAIL $name: $(tail -1 <<<"$log")"; continue; fi
  bash "$here/optimize.sh" "$here/raw/$name.glb" "$outdir/$name.glb" "$ratio" 0.01 >/dev/null 2>&1 || { echo "FAIL optimize $name"; continue; }
  cp "$outdir/$name.glb" "$here/preview/model.glb"
  (cd /d/ai-art/ai-art && node dallas-dash/tools/preview-glb.mjs "$here/preview" "$here/preview/$name.png" >/dev/null 2>&1)
  echo "DONE $name $(grep -o 'in [0-9]*s' <<<"$log")"
done
echo "ALL DONE"
