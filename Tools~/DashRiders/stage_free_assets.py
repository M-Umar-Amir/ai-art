"""Stage only selected CC0 models, with per-file provenance and reproducible hashes."""
import hashlib
import json
import pathlib
import shutil

ROOT = pathlib.Path(__file__).parent
LIB = ROOT / 'free-assets/kenney-library'
OUT = ROOT / 'free-curated'
OUT.mkdir(exist_ok=True)
SELECTION = {
    'food-burger': ('food-kit', 'burger-cheese-double'),
    'food-pizza': ('food-kit', 'pizza'),
    'food-drink': ('food-kit', 'soda'),
    'food-taco': ('food-kit', 'taco'),
    'food-parcel': ('food-kit', 'bag'),
    'city-a': ('city-kit-commercial', 'building-c'),
    'city-b': ('city-kit-commercial', 'building-f'),
    'city-c': ('city-kit-commercial', 'building-i'),
    'city-tower-a': ('city-kit-commercial', 'building-skyscraper-b'),
    'city-tower-b': ('city-kit-commercial', 'building-skyscraper-d'),
    'city-awning': ('city-kit-commercial', 'detail-awning-wide'),
    'city-parasol': ('city-kit-commercial', 'detail-parasol-a'),
    'service-console': ('space-kit', 'desk-computer'),
    'service-charger': ('space-kit', 'machine-wirelesscable'),
    'delivery-platform': ('space-kit', 'platform-large'),
    'service-gate': ('space-kit', 'gate-simple'),
    'power-core': ('space-kit', 'machine-generator'),
    'power-battery': ('space-kit', 'machine-barrel'),
    'power-drone': ('space-kit', 'craft-miner'),
    'power-magnet': ('space-kit', 'machine-wireless'),
    'energy-chip': ('platformer-kit', 'coin-gold'),
    'roof-chimney': ('space-kit', 'chimney-detailed'),
}
manifest = []
for name, (pack, model) in SELECTION.items():
    src = LIB / 'packs' / pack / (model + '.glb')
    dst = OUT / ('free-' + name + '.glb')
    shutil.copyfile(src, dst)
    manifest.append({'file': dst.name, 'creator': 'Kenney', 'license': 'CC0-1.0',
                     'source': 'https://kenney.nl/assets/' + pack,
                     'download': 'https://github.com/Hidencod/tge-assets/tree/d92c6e923bbef8649d4e5be3ab375a40f88672b0/packs/' + pack,
                     'original': model + '.glb',
                     'sha256': hashlib.sha256(dst.read_bytes()).hexdigest()})
known = {item['file'] for item in manifest}
for generated in sorted(OUT.glob('free-*.glb')):
    if generated.name not in known:
        manifest.append({'file': generated.name, 'creator': 'Local project generation',
                         'source': 'generate_courier_collectibles.py',
                         'method': 'Free Blender procedural mesh generation; no AI inference',
                         'license': 'Project-generated; no third-party mesh inputs',
                         'sha256': hashlib.sha256(generated.read_bytes()).hexdigest()})
(OUT / 'SOURCES.json').write_text(json.dumps(manifest, indent=2))
shutil.copyfile(LIB / 'LICENSE', OUT / 'LICENSE.txt')
print('Recorded', len(SELECTION), 'CC0 models and', len(manifest) - len(SELECTION), 'project-generated models')
