"""Flatten the pencil hatching that sheet projection bakes into model textures, for a clean toy look.

Usage: python toonify_glb.py <in.glb> <out.glb> [--preview <dir>]

Each embedded image goes through edge-preserving mean-shift smoothing (flat colour regions, crisp
boundaries), a light bilateral pass to kill leftover strokes, then a gentle saturation lift. The mesh and
everything else in the GLB is copied byte for byte; only image bufferViews are replaced.
"""
import json
import struct
import sys
from pathlib import Path

import cv2
import numpy as np


def read_glb(path):
    data = Path(path).read_bytes()
    magic, version, length = struct.unpack_from('<4sII', data, 0)
    assert magic == b'glTF', 'not a GLB'
    off, gltf, binary = 12, None, b''
    while off < length:
        clen, ctype = struct.unpack_from('<I4s', data, off)
        chunk = data[off + 8: off + 8 + clen]
        if ctype == b'JSON':
            gltf = json.loads(chunk.decode('utf-8'))
        elif ctype == b'BIN\x00':
            binary = chunk
        off += 8 + clen
    return gltf, binary


def write_glb(path, gltf, binary):
    js = json.dumps(gltf, separators=(',', ':')).encode('utf-8')
    js += b' ' * ((4 - len(js) % 4) % 4)
    binary += b'\x00' * ((4 - len(binary) % 4) % 4)
    total = 12 + 8 + len(js) + 8 + len(binary)
    out = struct.pack('<4sII', b'glTF', 2, total)
    out += struct.pack('<I4s', len(js), b'JSON') + js
    out += struct.pack('<I4s', len(binary), b'BIN\x00') + binary
    Path(path).write_bytes(out)


def toonify(img):
    has_alpha = img.ndim == 3 and img.shape[2] == 4
    bgr = img[:, :, :3] if has_alpha else img
    flat = cv2.pyrMeanShiftFiltering(bgr, sp=7, sr=26, maxLevel=1)
    flat = cv2.bilateralFilter(flat, 7, 40, 7)
    hsv = cv2.cvtColor(flat, cv2.COLOR_BGR2HSV).astype(np.float32)
    hsv[:, :, 1] = np.clip(hsv[:, :, 1] * 1.12, 0, 255)
    flat = cv2.cvtColor(hsv.astype(np.uint8), cv2.COLOR_HSV2BGR)
    if has_alpha:
        flat = np.dstack([flat, img[:, :, 3]])
    return flat


def main():
    src, dst = sys.argv[1], sys.argv[2]
    preview = sys.argv[sys.argv.index('--preview') + 1] if '--preview' in sys.argv else None
    gltf, binary = read_glb(src)
    views = gltf['bufferViews']
    replace = {}
    for i, image in enumerate(gltf.get('images', [])):
        if 'bufferView' not in image:
            continue
        bv = views[image['bufferView']]
        start = bv.get('byteOffset', 0)
        raw = np.frombuffer(binary[start:start + bv['byteLength']], np.uint8)
        img = cv2.imdecode(raw, cv2.IMREAD_UNCHANGED)
        if img is None:
            continue
        out = toonify(img)
        ok, enc = cv2.imencode('.png', out)
        assert ok
        replace[image['bufferView']] = enc.tobytes()
        image['mimeType'] = 'image/png'
        if preview:
            Path(preview).mkdir(parents=True, exist_ok=True)
            name = Path(src).stem
            side = np.hstack([img[:, :, :3] if img.shape[2] == 4 else img, out[:, :, :3]])
            cv2.imwrite(str(Path(preview) / f'{name}-{i}.jpg'), cv2.resize(side, None, fx=0.5, fy=0.5))

    # Rebuild the binary chunk: copy every bufferView in order, swapping in the new images, 4-byte aligned.
    order = sorted(range(len(views)), key=lambda k: views[k].get('byteOffset', 0))
    new_bin = bytearray()
    for k in order:
        bv = views[k]
        start = bv.get('byteOffset', 0)
        blob = replace.get(k, binary[start:start + bv['byteLength']])
        new_bin += b'\x00' * ((4 - len(new_bin) % 4) % 4)
        bv['byteOffset'] = len(new_bin)
        bv['byteLength'] = len(blob)
        new_bin += blob
    gltf['buffers'][0]['byteLength'] = len(new_bin)
    write_glb(dst, gltf, bytes(new_bin))
    print(f'{Path(src).name}: {len(replace)} image(s) toonified')


if __name__ == '__main__':
    main()
