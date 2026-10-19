/**
 * Dallas Dash — the renderer (WebGL via three.js).
 *
 * Real models and textures: a CC0 modular city, scanned street furniture and
 * rigged characters with faces (see assets/CREDITS.md). The simulation in
 * sim.js stays the single source of truth; this file only draws it, plus a
 * visual-only motion layer (hops, leans, squash and stretch, camera bank).
 *
 * Same API as the old Canvas 2D renderer: createRenderer(canvas) returns
 * { draw(run, opts), resize(), ready, progress }.
 *
 * Porting note (Expo/EAS): everything here is plain three.js; on device it
 * runs on expo-gl through @react-three/fiber/native or expo-three. Only
 * art.js (canvas textures) and assets.js (loading) need device shims.
 */

import * as THREE from "three";
import { RoomEnvironment } from "three/addons/environments/RoomEnvironment.js";
import { routeById, districtAt as routeDistrictAt, DISTRICT_LEN } from "./routes.js";
import { loadAssets, makeArt } from "./assets.js";
import { CrewFactory } from "./crew.js";
import { City, makeSky } from "./city.js";
import { Course } from "./course.js";

export { DISTRICT_LEN };
export function districtAt(distance, routeId) {
  return routeDistrictAt(distance, routeId);
}

export const CAM = { h: 3.1, back: 6.4, lookY: 0.8, lookAhead: 9, cy: 0.6, focal: 1.38 };

export function createRenderer(canvas) {
  const dpr = window.devicePixelRatio || 1;
  // MSAA is close to free on phone (tile-based) GPUs and keeps the toon outlines clean
  const gl = new THREE.WebGLRenderer({ canvas, antialias: true, powerPreference: "high-performance", alpha: false });
  gl.outputColorSpace = THREE.SRGBColorSpace;
  gl.toneMapping = THREE.ACESFilmicToneMapping;
  gl.toneMappingExposure = 1.05;
  const phone = Boolean(window.DASH_APP) || (window.matchMedia && window.matchMedia("(pointer: coarse)").matches);
  gl.setPixelRatio(Math.min(dpr, phone ? 1.5 : 1.6));
  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(50, 1, 0.3, 420);
  const hemi = new THREE.HemisphereLight(0xdcecff, 0x8f8070, 1.25);
  const sun = new THREE.DirectionalLight(0xfff0d8, 2.3);
  sun.position.set(-30, 50, 20);
  scene.add(hemi, sun, sun.target);
  scene.environment = new THREE.PMREMGenerator(gl).fromScene(new RoomEnvironment(), 0.04).texture;
  scene.environmentIntensity = 0.45;
  scene.fog = new THREE.Fog(0xe8d0b0, 110, 300);
  const sky = makeSky(scene);

  let W = 1;
  let H = 1;
  let focalBase = 900;
  function resize() {
    W = Math.max(320, Math.floor(canvas.clientWidth || window.innerWidth));
    H = Math.max(320, Math.floor(canvas.clientHeight || window.innerHeight));
    gl.setSize(W, H, false);
    camera.aspect = W / H;
    focalBase = Math.min(W, H) * (W > H * 1.2 ? CAM.focal * 0.8 : CAM.focal);
    // keep the old frame: the horizon sits at CAM.cy of the screen height
    camera.setViewOffset(W, H, 0, (0.5 - CAM.cy) * H, W, H);
    camera.updateProjectionMatrix();
  }
  resize();

  const state = { ready: false, progress: 0, error: null };
  let A = null;
  let art = null;
  let city = null;
  let course = null;
  let crew = null;
  const runners = new Map();
  let officer = null;
  let board = null;
  let jet = null;
  let dust = null;
  let route = null;

  loadAssets((p) => (state.progress = p))
    .then((assets) => {
      A = assets;
      art = makeArt(A);
      city = new City(scene, A, art, gl);
      course = new Course(scene, A, art);
      crew = new CrewFactory(A.crew, A.clips, art);
      officer = crew.make("officer");
      officer.root.visible = false;
      scene.add(officer.root);
      board = makeBoard();
      scene.add(board);
      jet = makeJet();
      scene.add(jet);
      dust = makeDust();
      scene.add(dust.points);
      // build and compile everything now, behind the loading screen, so the
      // first container or landmark never stalls a frame mid-run
      course.prewarm();
      runnerFor("dante");
      city.setRoute(routeById(null));
      return warm();
    })
    .then(() => {
      state.ready = true;
      state.progress = 1;
    })
    .catch((e) => {
      state.error = e;
      console.error(e);
    });

  /**
   * Compile every shader and upload every texture now, so nothing new stalls
   * a frame mid-run. three compiles only what's visible, so hidden things are
   * shown just while the compile gathers them (synchronously).
   */
  function warm() {
    const hidden = [];
    scene.traverse((o) => {
      if (!o.visible) {
        hidden.push(o);
        o.visible = true;
      }
    });
    const maps = new Set();
    scene.traverse((o) => {
      for (const m of [].concat(o.material || [])) for (const k of ["map", "gradientMap", "normalMap", "bumpMap"]) if (m[k]) maps.add(m[k]);
    });
    let p;
    try {
      p = gl.compileAsync ? gl.compileAsync(scene, camera) : Promise.resolve(gl.compile(scene, camera));
    } finally {
      for (const o of hidden) o.visible = false;
    }
    for (const t of maps) gl.initTexture(t);
    return p.catch(() => {});
  }

  function runnerFor(id) {
    let r = runners.get(id);
    if (!r) {
      r = crew.make(id);
      scene.add(r.root);
      runners.set(id, r);
    }
    for (const [k, v] of runners) v.root.visible = k === id;
    return r;
  }

  function makeBoard() {
    const g = new THREE.Group();
    const deck = new THREE.Mesh(new THREE.BoxGeometry(0.62, 0.06, 1.55), new THREE.MeshStandardMaterial({ color: 0xd8102e, roughness: 0.35, metalness: 0.3 }));
    const stripe = new THREE.Mesh(new THREE.BoxGeometry(0.2, 0.065, 1.56), new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.4 }));
    const glow = new THREE.Mesh(new THREE.PlaneGeometry(0.9, 1.9), new THREE.MeshBasicMaterial({ color: 0xff6a7f, transparent: true, opacity: 0.35, depthWrite: false }));
    glow.rotation.x = -Math.PI / 2;
    glow.position.y = -0.2;
    g.add(deck, stripe, glow);
    g.visible = false;
    return g;
  }

  function makeJet() {
    const g = new THREE.Group();
    const tank = new THREE.MeshStandardMaterial({ color: 0xb8bec8, metalness: 0.8, roughness: 0.3 });
    for (const x of [-0.17, 0.17]) {
      const t = new THREE.Mesh(new THREE.CylinderGeometry(0.11, 0.11, 0.55, 12), tank);
      t.position.set(x, 1.15, 0.28);
      const flame = new THREE.Mesh(new THREE.ConeGeometry(0.09, 0.5, 10).rotateX(Math.PI), new THREE.MeshBasicMaterial({ color: 0xffa23a, transparent: true, opacity: 0.9 }));
      flame.position.set(x, 0.62, 0.28);
      flame.name = "flame";
      g.add(t, flame);
    }
    g.visible = false;
    return g;
  }

  function makeDust() {
    const N = 80;
    const geo = new THREE.BufferGeometry();
    const pos = new Float32Array(N * 3);
    geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    const c = document.createElement("canvas");
    c.width = c.height = 64;
    const ctx = c.getContext("2d");
    const gr = ctx.createRadialGradient(32, 32, 2, 32, 32, 30);
    gr.addColorStop(0, "rgba(255,255,255,0.9)");
    gr.addColorStop(1, "rgba(255,255,255,0)");
    ctx.fillStyle = gr;
    ctx.fillRect(0, 0, 64, 64);
    const mat = new THREE.PointsMaterial({ size: 0.35, map: new THREE.CanvasTexture(c), transparent: true, depthWrite: false, color: 0xe8dcc8, opacity: 0.7 });
    const points = new THREE.Points(geo, mat);
    points.frustumCulled = false;
    return { points, pos, parts: [], N };
  }

  /* ── visual-only motion layer (as in the 2D renderer) ─────────────── */
  const fx = { run: null, tick: -1, visX: 0, velX: 0, visY: 0, lane: 1, hop: 1, dir: 0, jumps: 0, rolls: 0, coins: 0, stumbles: 0, air: false, base: 0, land: 0, spin: 0, spinOn: false, camX: 0, bank: 0, kick: 0, ev: {} };

  function puff(x, y, z, n, spread, up) {
    if (!dust) return;
    for (let i = 0; i < n; i += 1) {
      const a = Math.random() * Math.PI * 2;
      dust.parts.push({ x: x + Math.cos(a) * 0.2, y: y + 0.05, z, vx: Math.cos(a) * spread, vy: up * (0.5 + Math.random()), vz: 1.5 + Math.abs(Math.sin(a)) * spread * 0.5, life: 0, max: 0.35 + Math.random() * 0.3 });
    }
  }

  function updateFx(run, dt) {
    if (fx.run !== run) Object.assign(fx, { run, tick: run.tick, visX: run.x, velX: 0, visY: run.y, lane: run.lane, hop: 1, dir: 0, jumps: run.jumps, rolls: run.rolls, coins: run.coins, stumbles: run.stumbles, air: false, base: run.y, land: 0, spin: 0, spinOn: false, camX: run.x * 0.34, bank: 0, kick: 0 });
    const ev = { stumble: false, landed: false, air: false, rising: false };
    const grounded = run.y <= run.floor + 0.001 && run.vy <= 0;
    const z = -run.distance;
    if (run.lane !== fx.lane) {
      fx.dir = Math.sign(run.lane - fx.lane);
      fx.lane = run.lane;
      fx.hop = 0;
      if (grounded && run.jet <= 0) puff(run.x, run.floor, z + 0.2, 4, 1.4, 1.0);
    }
    if (run.jumps !== fx.jumps) {
      fx.jumps = run.jumps;
      fx.air = true;
      fx.base = run.floor;
      fx.kick = 1;
      fx.spinOn = run.sneakers > 0;
      fx.spin = 0;
      puff(run.x, run.floor, z, 6, 1.2, 1.4);
    }
    if (run.rolls !== fx.rolls) fx.rolls = run.rolls;
    if (run.stumbles !== fx.stumbles) {
      fx.stumbles = run.stumbles;
      ev.stumble = true;
    }
    if (fx.air && grounded && run.tick !== fx.tick) {
      fx.air = false;
      fx.land = 1;
      ev.landed = true;
      puff(run.x, run.floor, z, 8, 2.2, 1.2);
    }
    fx.tick = run.tick;
    ev.air = fx.air && run.jet <= 0;
    ev.rising = run.vy > 0;
    const k = 420;
    const damp = 2 * Math.sqrt(k) * 0.52;
    const steps = Math.max(1, Math.round(dt / (1 / 120)));
    const h = dt / steps;
    for (let i = 0; i < steps; i += 1) {
      fx.velX += (k * (run.x - fx.visX) - damp * fx.velX) * h;
      fx.visX += fx.velX * h;
    }
    const target = fx.air ? run.y + Math.max(0, run.y - fx.base) * 0.45 : run.y;
    fx.visY += (target - fx.visY) * Math.min(1, dt * (fx.air ? 40 : 26));
    if (run.jet > 0) fx.visY = run.y;
    fx.hop = Math.min(1, fx.hop + dt / 0.26);
    fx.land = Math.max(0, fx.land - dt / 0.2);
    fx.kick = Math.max(0, fx.kick - dt / 0.35);
    if (fx.spinOn) fx.spin = Math.min(Math.PI * 2, fx.spin + dt * 13);
    if (!fx.air) fx.spinOn = false;
    fx.camX += (fx.visX * 0.42 - fx.camX) * Math.min(1, dt * 7);
    const bankTarget = Math.max(-0.06, Math.min(0.06, -fx.velX * 0.005));
    fx.bank += (bankTarget - fx.bank) * Math.min(1, dt * 10);
    if (run.slide > 0 && grounded && Math.random() < 0.5) puff(run.x, run.floor, z + 0.3, 1, 1.2, 0.5);
    fx.ev = ev;
  }

  function drawDust(dt) {
    const d = dust;
    for (let i = d.parts.length - 1; i >= 0; i -= 1) {
      const p = d.parts[i];
      p.life += dt;
      if (p.life >= p.max) {
        d.parts.splice(i, 1);
        continue;
      }
      p.x += p.vx * dt;
      p.y += p.vy * dt;
      p.z += p.vz * dt;
      p.vy -= 4 * dt;
    }
    if (d.parts.length > d.N) d.parts.splice(0, d.parts.length - d.N);
    for (let i = 0; i < d.N; i += 1) {
      const p = d.parts[i];
      d.pos[i * 3] = p ? p.x : 0;
      d.pos[i * 3 + 1] = p ? p.y : -50;
      d.pos[i * 3 + 2] = p ? p.z : 0;
    }
    d.points.geometry.attributes.position.needsUpdate = true;
  }

  // adaptive resolution: phones vary wildly, so hold the frame rate by
  // trading pixels. Each change reallocates the framebuffer (a visible
  // hitch), so it only judges steady running — never the first seconds of a
  // run, when shaders and new streets arrive — changes rarely, and never goes
  // below 1x: below that the picture turns to mush.
  const maxRatio = Math.min(dpr, phone ? 1.5 : 1.6);
  const minRatio = Math.min(1, maxRatio);
  let ratio = maxRatio;
  let frameEma = 16.7;
  let lastAdjust = 0;
  let steadySince = 0;
  function adapt(dtMs, now, steady) {
    if (!steady) {
      steadySince = 0;
      return;
    }
    if (!steadySince) {
      steadySince = now;
      frameEma = 16.7;
    }
    frameEma += (Math.min(60, dtMs) - frameEma) * 0.03;
    if (now - steadySince < 3500 || now - lastAdjust < 5000) return;
    let next = ratio;
    if (frameEma > 24 && ratio > minRatio) next = Math.max(minRatio, ratio - 0.15);
    else if (frameEma < 13.5 && ratio < maxRatio) next = Math.min(maxRatio, ratio + 0.15);
    if (next !== ratio) {
      ratio = next;
      gl.setPixelRatio(ratio);
      resize();
      lastAdjust = now;
      frameEma = 16.7;
    }
  }

  let lastNow = 0;
  let show = 1; // 1 = home showcase (facing the camera), 0 = running camera
  function draw(run, opts = {}) {
    const t0 = performance.now();
    const now = opts.now || t0;
    const dt = lastNow ? Math.max(0.001, Math.min(0.05, (now - lastNow) / 1000)) : 1 / 60;
    if (lastNow && !opts.fixed) adapt(now - lastNow, now, Boolean(run) && !opts.idle && show < 0.02 && !run.downed);
    lastNow = now;
    const R = routeById(opts.route);
    if (route !== R) {
      const switched = route !== null;
      route = R;
      if (switched && state.ready) {
        city.setRoute(R);
        warm();
      }
      sky.set(R);
      const hz = R.sky.haze;
      scene.fog.color.setRGB(hz[0] / 255, hz[1] / 255, hz[2] / 255, THREE.SRGBColorSpace);
      sun.color.set(R.id === "toronto" ? 0xfff6ea : 0xffe9cc);
    }
    if (!state.ready || !run) {
      camera.position.set(0, CAM.h, CAM.back);
      camera.lookAt(0, CAM.lookY, -CAM.lookAhead);
      sky.follow(0, 0);
      gl.render(scene, camera);
      return { polys: 0, ms: performance.now() - t0, ready: state.ready, progress: state.progress };
    }
    updateFx(run, dt);
    const d = run.distance;
    const pace = Math.max(0, Math.min(1, (run.speed - 20) / 26));
    // a wider lens as the pace climbs, and a punch on take-off
    const focal = focalBase * (1 - 0.12 * pace - 0.05 * Math.sin(Math.PI * fx.kick) - (run.jet > 0 ? 0.06 : 0));
    camera.fov = (2 * Math.atan(H / 2 / focal) * 180) / Math.PI;
    camera.updateProjectionMatrix();
    const dip = run.slide > 0 ? 0.9 : 0;
    const landDip = Math.sin(Math.PI * fx.land) * 0.28;
    const camY = CAM.h + fx.visY * 0.62 - dip - landDip;
    // home: the camera swings round in front of the runner so you see their
    // face; starting a run glides it back behind them
    show += ((opts.idle ? 1 : 0) - show) * Math.min(1, dt * 3.2);
    const e = show * show * (3 - 2 * show);
    const runPos = new THREE.Vector3(fx.camX, camY, -d + CAM.back - pace * 0.6);
    const runLook = new THREE.Vector3(fx.visX * 0.16, CAM.lookY + fx.visY * 0.72 - dip * 0.4, -d - CAM.lookAhead);
    const wide = W > H * 1.2;
    const showPos = new THREE.Vector3(wide ? 1.4 : 0.5, 1.75, -d - (wide ? 4.6 : 5.6));
    const showLook = new THREE.Vector3(wide ? 1.2 : 0, wide ? 1.2 : 1.45, -d);
    camera.position.lerpVectors(runPos, showPos, e);
    camera.up.set(0, 1, 0);
    camera.lookAt(new THREE.Vector3().lerpVectors(runLook, showLook, e));
    camera.rotateZ(fx.bank * (1 - e));
    sky.follow(camera.position.x, camera.position.z);
    sun.target.position.set(0, 0, -d - 20);
    sun.position.set(-30, 50, -d + 20);

    city.update(d, run.objects, R, fx.camX, show > 0.02 ? 70 : 14);
    course.update(run, now);

    // the runner
    const id = opts.outfit || "dante";
    const r = runnerFor(id);
    const hopS = fx.hop < 1 ? Math.sin(Math.PI * fx.hop) : 0;
    const onBoard = run.board > 0;
    const flying = run.jet > 0;
    const down = Boolean(run.downed);
    let sy = 1;
    if (!down && fx.air && run.vy > 0) sy = 1.06;
    else if (fx.land > 0) sy = 1 - 0.12 * Math.sin(Math.PI * fx.land);
    const blink = run.invuln > 0 && !down && Math.floor(now / 70) % 2 === 0 && run.invuln < 140;
    r.root.visible = !blink;
    r.root.position.set(fx.visX, fx.visY + (onBoard ? 0.2 : 0) + (run.slide > 0 || flying || down ? 0 : hopS * 0.35), -d);
    r.root.rotation.set(0, Math.PI + fx.dir * 0.35 * hopS + fx.spin + (opts.idle ? Math.sin(now * 0.0006) * 0.18 : 0), -fx.dir * 0.18 * hopS + (flying ? Math.sin(now * 0.004) * 0.06 : 0));
    r.root.scale.set(1 / Math.sqrt(sy), sy, 1 / Math.sqrt(sy));
    r.drive(run, fx.ev, Boolean(opts.idle), dt);
    const sh = r.shadow;
    sh.visible = !flying;
    sh.position.y = run.floor - fx.visY + 0.03;
    const lift = Math.max(0, fx.visY - run.floor);
    sh.scale.setScalar(Math.max(0.5, 1 - lift * 0.12));
    board.visible = onBoard;
    if (onBoard) board.position.set(fx.visX, fx.visY + 0.1, -d - 0.05);
    jet.visible = flying;
    if (flying) {
      jet.position.set(fx.visX, fx.visY, -d);
      for (const f of jet.children) if (f.name === "flame") f.scale.y = 0.7 + 0.4 * Math.abs(Math.sin(now * 0.05));
    }
    // the parking officer, close behind after a stumble
    const caught = run.downed === "caught";
    const chasing = !opts.idle && (run.chase >= 90 || caught);
    officer.root.visible = chasing;
    if (chasing) {
      // just behind the runner and half a lane over: close enough to feel, clear of the lens
      const zz = caught ? 1.2 : 1.5 + (1 - run.chase / 300) * 1.3;
      const side = run.x > 1 ? -1 : 1;
      officer.root.position.set(fx.visX + side * (caught ? 0.7 : 0.95), 0, -d + zz);
      officer.root.rotation.set(0, Math.PI, 0);
      officer.drive({ ...run, downed: caught ? "" : "", jet: 0, slide: 0 }, { air: false }, false, dt);
    }
    drawDust(dt);
    gl.render(scene, camera);
    const info = gl.info.render;
    return { polys: info.triangles, calls: info.calls, ms: performance.now() - t0, ready: true, progress: 1, ratio };
  }

  return {
    draw,
    resize,
    debug: () => scene,
    gl,
    get ready() {
      return state.ready;
    },
    get progress() {
      return state.progress;
    },
    get error() {
      return state.error;
    },
  };
}
