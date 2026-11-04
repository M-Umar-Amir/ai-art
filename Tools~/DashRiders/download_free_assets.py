"""Download free Kenney packs through their public download flow; preserve licenses."""
import hashlib
import json
import pathlib
import re
import urllib.request
import zipfile

ROOT = pathlib.Path(__file__).parent / 'free-assets'
ROOT.mkdir(exist_ok=True)
for pack in ('food-kit', 'city-kit-commercial', 'city-kit-industrial', 'racing-kit'):
    page_url = 'https://kenney.nl/assets/' + pack
    request = urllib.request.Request(page_url, headers={'User-Agent': 'Mozilla/5.0'})
    page = urllib.request.urlopen(request, timeout=60).read().decode()
    identifier = re.search(r"data-download-id=['\"]([^'\"]+)", page).group(1)
    request = urllib.request.Request('https://kenney.nl/download-link/' + identifier,
                                     headers={'Referer': page_url, 'User-Agent': 'Mozilla/5.0'})
    response = json.load(urllib.request.urlopen(request, timeout=60))
    link = response.get('url') or response.get('download') or response.get('link')
    if not link:
        raise RuntimeError('No public download URL for ' + pack)
    archive = ROOT / (pack + '.zip')
    urllib.request.urlretrieve(link, archive)
    destination = ROOT / pack
    destination.mkdir(exist_ok=True)
    with zipfile.ZipFile(archive) as z:
        for item in z.infolist():
            target = (destination / item.filename).resolve()
            if not target.is_relative_to(destination.resolve()):
                raise ValueError('Unsafe archive path')
        z.extractall(destination)
    (destination / 'SOURCE.json').write_text(json.dumps({
        'creator': 'Kenney', 'page': page_url, 'license': 'CC0-1.0',
        'sha256': hashlib.sha256(archive.read_bytes()).hexdigest(),
    }, indent=2))
    print(pack, 'downloaded', archive.stat().st_size, flush=True)
