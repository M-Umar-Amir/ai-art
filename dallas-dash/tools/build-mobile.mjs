/**
 * Pack the game into ONE self-contained HTML file for the phone app
 * (dallas-dash-mobile). Everything is inline — the bundled JS (three.js
 * included), the models, the illustrations, the logo and the fonts — so the
 * WebView needs no network and no file access beyond the page itself.
 *
 *   node tools/build-mobile.mjs [out.html]
 *   default out: ../dallas-dash-mobile/assets/game/index.html
 */

import { build } from "esbuild";
import { readFileSync, writeFileSync, mkdirSync, readdirSync, existsSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const GAME = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const OUT = resolve(process.argv[2] || join(GAME, "..", "dallas-dash-mobile", "assets", "game", "index.html"));
const FONT_CACHE = join(GAME, "tools", ".fonts");

const b64 = (buf) => Buffer.from(buf).toString("base64");
const dataUri = (path, mime) => `data:${mime};base64,${b64(readFileSync(path))}`;

/* 1. the JS: client.js and everything it imports, with the import map resolved */
const three = join(GAME, "vendor", "three");
const res = await build({
  entryPoints: [join(GAME, "client.js")],
  bundle: true,
  format: "esm",
  minify: true,
  target: ["chrome100", "safari15"],
  write: false,
  legalComments: "none",
  plugins: [
    {
      name: "importmap",
      setup(b) {
        b.onResolve({ filter: /^three$/ }, () => ({ path: join(three, "three.module.min.js") }));
        b.onResolve({ filter: /^three\/addons\// }, (a) => ({ path: join(three, "addons", a.path.slice("three/addons/".length)) }));
      },
    },
  ],
});
const js = res.outputFiles[0].text.replace(/<\/script/gi, "<\\/script");

/* 2. the assets, keyed the way assetUrl() / art() look them up */
const assets = {};
for (const f of ["city", "props", "crew", "anims"]) assets[`models/${f}.glb`] = dataUri(join(GAME, "assets", "models", `${f}.glb`), "model/gltf-binary");
assets["kfc-logo.svg"] = dataUri(join(GAME, "assets", "kfc-logo.svg"), "image/svg+xml");
for (const f of readdirSync(join(GAME, "assets", "art"))) assets[`art/${f}`] = dataUri(join(GAME, "assets", "art", f), "image/webp");

/* 3. the fonts: Barlow Condensed, fetched once and cached next to this tool */
async function fontCss() {
  const cssFile = join(FONT_CACHE, "fonts.css");
  if (!existsSync(cssFile)) {
    mkdirSync(FONT_CACHE, { recursive: true });
    const url = "https://fonts.googleapis.com/css2?family=Barlow+Condensed:ital,wght@0,600;0,800;1,800;1,900&display=swap";
    // a modern UA gets woff2
    let css = await (await fetch(url, { headers: { "user-agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36" } })).text();
    // keep only the latin subsets, inline each file
    const blocks = css.split("/* ").filter((x) => x.startsWith("latin */"));
    let out = "";
    for (const blk of blocks) {
      const m = blk.match(/url\((https:[^)]+\.woff2)\)/);
      const font = Buffer.from(await (await fetch(m[1])).arrayBuffer());
      out += blk.slice("latin */".length).replace(m[0], `url(data:font/woff2;base64,${b64(font)})`);
    }
    writeFileSync(cssFile, out);
  }
  return readFileSync(cssFile, "utf8");
}
const fonts = await fontCss();

/* 4. the page: the game's own index.html with its external references replaced */
let html = readFileSync(join(GAME, "index.html"), "utf8");
html = html.replace(/<link rel="preconnect"[^>]*>\s*/g, "");
html = html.replace(/<link\s+href="https:\/\/fonts\.googleapis\.com[^>]*>\s*/s, "");
html = html.replace(/<script type="importmap">[\s\S]*?<\/script>\s*/, "");
html = html.replace(/<script src="\/dallas-dash\/runtime\.js"><\/script>\s*/, "");
html = html.replace(/<script type="module" src="\/dallas-dash\/client\.js"><\/script>/, "");
html = html.replaceAll("/dallas-dash/assets/kfc-logo.svg", assets["kfc-logo.svg"]);
html = html.replace(/<meta name="viewport"[^>]*>/, '<meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no, viewport-fit=cover" />');
const boot = `<style>${fonts}</style>
<script>window.DASH_LOCAL = true; window.DASH_APP = true; window.DD_ASSETS = ${JSON.stringify(assets)};</script>
<script type="module">${js}</script>`;
// a function, not a string: the minified JS contains $& and $' sequences
html = html.replace("</body>", () => `${boot}\n</body>`);
for (const bad of ["/dallas-dash/", "fonts.googleapis"]) if (html.split(js).join("").includes(bad)) console.warn(`warning: "${bad}" still referenced`);

mkdirSync(dirname(OUT), { recursive: true });
writeFileSync(OUT, html);
console.log(`wrote ${OUT} (${(html.length / 1e6).toFixed(1)} MB, js ${(js.length / 1e3).toFixed(0)} kB)`);
