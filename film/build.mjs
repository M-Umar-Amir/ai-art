// Join src/*.js (in file-name order) into ONE self-contained HTML file (v2: real images, doc screenshots, clips, three.js).
// usage: node build.mjs [out.html]      (default: build/<slug>.html, slug from beats.json title)
// Embeds: B (src/beats.json), IMGSRC {key: dataURL} (../assets/photos/*.jpg + ../assets/docs/*.jpg), DOCS (../assets/docs/docs.json),
// CLIPSRC {key: [dataURL...]} (../assets/clips/<key>/f*.jpg, 12 fps), STATEMENT (../assets/docs/Statement.lean),
// and three.js (an import map with data: URLs; window.THREE is set by a module script, then the page becomes ready).
import { readFileSync, readdirSync, existsSync, mkdirSync, writeFileSync, statSync } from 'fs';
import { execFileSync } from 'child_process';

const beatsText = readFileSync('src/beats.json', 'utf8'), beats = JSON.parse(beatsText);
const slug = (beats.title || 'animation').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
const out = process.argv[2] || `build/${slug}.html`;
mkdirSync('build', { recursive: true });
const A = '../assets/';
const du = (f, type = 'image/jpeg') => `data:${type};base64,` + readFileSync(f).toString('base64');
const imgs = {};
for (const dir of ['photos', 'docs']) if (existsSync(A + dir)) for (const f of readdirSync(A + dir)) if (/\.jpe?g$/i.test(f)) imgs[(dir === 'docs' ? 'doc_' : '') + f.replace(/\.jpe?g$/i, '')] = du(A + dir + '/' + f);
const clips = {};
if (existsSync(A + 'clips')) for (const k of readdirSync(A + 'clips')) { if (!statSync(A + 'clips/' + k).isDirectory()) continue; clips[k] = readdirSync(A + 'clips/' + k).filter(f => /\.jpg$/.test(f)).sort().map(f => du(A + 'clips/' + k + '/' + f)); }
const docs = existsSync(A + 'docs/docs.json') ? readFileSync(A + 'docs/docs.json', 'utf8') : '{}';
const statement = existsSync(A + 'docs/Statement.lean') ? readFileSync(A + 'docs/Statement.lean', 'utf8') : '';

const files = readdirSync('src').filter(f => f.endsWith('.js')).sort();
const js = (`'use strict';\nconst B = ${beatsText.trim()};\nconst IMGSRC = ${JSON.stringify(imgs)};\nconst CLIPSRC = ${JSON.stringify(clips)};\nconst DOCS = ${docs};\nconst STATEMENT = ${JSON.stringify(statement)};\n` +
  files.map(f => `// ===== ${f}\n` + readFileSync(`src/${f}`, 'utf8').replace(/^'use strict';\s*/, '')).join('\n')).replace(/<\/script/gi, '<\\/script');

// three.js r17x split build -> import map of data: URLs (relative import rewritten to a bare specifier)
const V = 'D:/ai-art/ai-art/dallas-dash/vendor/three/';
const T = existsSync('vendor/three.core.min.js') ? 'vendor/' : V;
const core = readFileSync(T + 'three.core.min.js', 'utf8');
const mod = readFileSync(T + 'three.module.min.js', 'utf8').replace(/from\s*"\.\/three\.core\.min\.js"/g, 'from"three-core"').replace(/from\s*'\.\/three\.core\.min\.js'/g, "from'three-core'");
const tdu = s => 'data:text/javascript;base64,' + Buffer.from(s).toString('base64');
const three = `<script type="importmap">${JSON.stringify({ imports: { 'three-core': tdu(core), 'three': tdu(mod) } })}</script>
<script type="module">import * as THREE from 'three'; window.THREE = THREE; window.__threeReady && window.__threeReady();</script>`;

let audio = '';
if (existsSync('build/audio.wav')) {
  const m4a = 'build/audio.m4a';
  if (!existsSync(m4a) || statSync(m4a).mtimeMs < statSync('build/audio.wav').mtimeMs) execFileSync(process.env.FFMPEG || 'D:/ai-art/tools/ffbin/ffmpeg.exe', ['-y', '-loglevel', 'error', '-i', 'build/audio.wav', '-af', 'loudnorm=I=-14:TP=-1.5:LRA=11', '-ar', '48000', '-c:a', 'aac', '-b:a', '160k', m4a]);
  audio = `<audio id="snd" preload="auto" src="data:audio/mp4;base64,${readFileSync(m4a).toString('base64')}"></audio>`;
}

const html = `<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>${beats.title || 'Animation'}</title>
<style>html,body{margin:0;height:100%;background:#07060a;overflow:hidden}
canvas{display:block;width:100vw;height:100vh;object-fit:contain;cursor:pointer}
#ui{position:fixed;left:12px;bottom:10px;font:12px/1.4 system-ui,sans-serif;color:#bbb;text-shadow:0 1px 2px #000;user-select:none}</style></head>
<body><canvas id="c" width="1920" height="1080"></canvas><div id="ui"></div>${audio}
<script>${js}</script>${three}</body></html>`;
writeFileSync(out, html);
console.log(`built ${out}  ${(html.length / 1048576).toFixed(1)} MB  (${Object.keys(imgs).length} images, ${Object.keys(clips).length} clips, ${files.length} src files)${audio ? '  (with sound)' : ''}`);
