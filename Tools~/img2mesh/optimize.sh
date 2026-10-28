#!/usr/bin/env bash
# Cut an AI-generated GLB down to a phone budget: weld, simplify by ratio, cap textures at 1024px.
# usage: optimize.sh <in.glb> <out.glb> [ratio=0.15] [error=0.01]  (textures become PNG for Unity glTFast)
set -euo pipefail
in="$1"; out="$2"; ratio="${3:-0.15}"
gt=/d/ai-art/tools/gltf/node_modules/.bin/gltf-transform
tmp="$(mktemp -d)"
$gt weld "$in" "$tmp/a.glb"
$gt simplify "$tmp/a.glb" "$tmp/b.glb" --ratio "$ratio" --error "${4:-0.01}"
$gt resize "$tmp/b.glb" "$tmp/c.glb" --width 1024 --height 1024
$gt png --formats "*" "$tmp/c.glb" "$tmp/d.glb"
$gt prune "$tmp/d.glb" "$out"
rm -rf "$tmp"
$gt inspect "$out" --format md 2>/dev/null | grep -E "^\| *[0-9]+ *\|.*(TRIANGLES|mesh)" | head -5 || true
ls -la "$out"
