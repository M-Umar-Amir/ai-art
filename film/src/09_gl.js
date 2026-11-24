// ================= three.js hero scenes (deterministic: everything is a function of the shot-local time lt) =================
function glBase(T, bg = 0x070a14, fog = [12, 40]) {
  const scene = new T.Scene(); scene.background = new T.Color(bg); scene.fog = new T.Fog(bg, fog[0], fog[1]);
  const camera = new T.PerspectiveCamera(42, 16 / 9, 0.1, 200);
  scene.add(new T.AmbientLight(0x8090b0, 0.55));
  const d = new T.DirectionalLight(0xfff0dd, 2.2); d.position.set(5, 9, 6); scene.add(d);
  const r = RNG(77), pts = [];
  for (let i = 0; i < 700; i++) pts.push((r() - 0.5) * 80, (r() - 0.5) * 50, (r() - 0.5) * 80);
  const sg = new T.BufferGeometry(); sg.setAttribute('position', new T.Float32BufferAttribute(pts, 3));
  scene.add(new T.Points(sg, new T.PointsMaterial({ color: 0x7f8fbf, size: 0.06, transparent: true, opacity: 0.55 })));
  return { scene, camera };
}
const easeIn = (lt, a, b) => E.out3(seg(lt, a, b));

// ---------- H1: an arithmetic circuit for (x1 + x2)(x2 + x3) * x4 ... building itself, pulses along the wires ----------
function glCircuit(T) {
  const S = glBase(T); const nodes = [], edges = [], R = 0.34;
  const layers = [[['x₁', 'in'], ['x₂', 'in'], ['x₃', 'in'], ['x₄', 'in']], [['+', 'add'], ['×', 'mul'], ['+', 'add']], [['×', 'mul'], ['+', 'add']], [['×', 'mul']]];
  const links = [[[0, 0], [0, 1]], [[0, 1], [0, 2]], [[0, 2], [0, 3]], [[1, 0], [1, 1]], [[1, 1], [1, 2]], [[2, 0], [2, 1]]]; // inputs of each non-input node, by layer
  const col = { in: 0xe8eef8, add: 0x39c6b0, mul: 0xf2b84b };
  layers.forEach((L, li) => L.forEach(([lab, kind], i) => {
    const x = (i - (L.length - 1) / 2) * 2.4, y = -3.2 + li * 2.15, z = Math.sin(i * 1.7 + li) * 0.6;
    const m = new T.Mesh(new T.SphereGeometry(R, 32, 18), new T.MeshStandardMaterial({ color: col[kind], emissive: col[kind], emissiveIntensity: 0.55, roughness: 0.35, metalness: 0.1 }));
    m.position.set(x, y, z); S.scene.add(m); const sp = glLabel(lab, { size: 110, col: kind === 'in' ? '#ffffff' : '#101010', scale: 0.32 }); sp.material.depthTest = false; sp.position.set(x, y, z + 0.01); sp.renderOrder = 2; S.scene.add(sp);
    nodes.push({ m, sp, li, p: new T.Vector3(x, y, z) });
  }));
  const at = (li, i) => nodes.find((n, k) => n.li === li && nodes.filter(q => q.li === li).indexOf(n) === i);
  const ins = [[[0, 0], [0, 1]], [[0, 1], [0, 2]], [[0, 2], [0, 3]], [[1, 0], [1, 1]], [[1, 1], [1, 2]], [[2, 0], [2, 1]]];
  const targets = [[1, 0], [1, 1], [1, 2], [2, 0], [2, 1], [3, 0]];
  targets.forEach(([li, i], k) => {
    const to = at(li, i); for (const [a, b] of ins[k]) {
      const fr = at(a, b), curve = new T.LineCurve3(fr.p, to.p);
      const tube = new T.Mesh(new T.TubeGeometry(curve, 8, 0.045, 8, false), new T.MeshStandardMaterial({ color: 0x8fb4ff, emissive: 0x3050a0, emissiveIntensity: 0.6, transparent: true, opacity: 0.85 }));
      S.scene.add(tube); const pulse = new T.Mesh(new T.SphereGeometry(0.11, 12, 8), new T.MeshBasicMaterial({ color: 0xfff2c0 })); S.scene.add(pulse);
      edges.push({ tube, pulse, a: fr.p, b: to.p, li, ph: hash(k * 7 + b, 3) });
    }
  });
  const pl = new T.PointLight(0xffd890, 30, 20); pl.position.set(0, 1, 4); S.scene.add(pl);
  S.update = lt => {
    nodes.forEach(n => { const k = easeIn(lt, 0.3 + n.li * 0.9, 0.8 + n.li * 0.9); n.m.scale.setScalar(Math.max(0.001, k)); n.sp.material.opacity = k; });
    edges.forEach(e => { const k = seg(lt, 0.7 + e.li * 0.9, 1.1 + e.li * 0.9); e.tube.visible = k > 0; e.tube.material.opacity = 0.85 * k; const f = ((lt * 0.55 + e.ph) % 1); e.pulse.visible = k > 0.9; e.pulse.position.copy(e.a).lerp(e.b, f); });
    const ang = -0.28 + lt * 0.045; S.camera.position.set(Math.sin(ang) * 12.5, 0.9 + 0.5 * Math.sin(lt * 0.2), Math.cos(ang) * 12.5); S.camera.lookAt(0, 0.1, 0);
  };
  return S;
}

// ---------- H2: a 4x4 grid cycling through all 24 "pick one per row and column" paths; then a field of thousands of grids ----------
const PERM4 = (() => { const out = []; const rec = (p, rest) => { if (!rest.length) { out.push(p); return; } rest.forEach((v, i) => rec([...p, v], rest.filter((_, j) => j !== i))); }; rec([], [0, 1, 2, 3]); return out; })();
function glPerms(T, o = {}) {
  const S = glBase(T, 0x080812, [14, 70]); const cubes = [], nums = [[3, 1, 4, 1], [5, 9, 2, 6], [5, 3, 5, 8], [9, 7, 9, 3]];
  for (let r = 0; r < 4; r++) for (let c = 0; c < 4; c++) {
    const cv = document.createElement('canvas'); cv.width = cv.height = 128; const g = cv.getContext('2d');
    g.fillStyle = '#1b2036'; g.fillRect(0, 0, 128, 128); g.fillStyle = '#e8eef8'; g.font = '700 84px Georgia'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(String(nums[r][c]), 64, 70);
    const tex = new T.CanvasTexture(cv); tex.colorSpace = T.SRGBColorSpace;
    const side = new T.MeshStandardMaterial({ color: 0x252b48, roughness: 0.6 });
    const top = new T.MeshStandardMaterial({ map: tex, emissive: 0xffc040, emissiveIntensity: 0, roughness: 0.5 });
    const m = new T.Mesh(new T.BoxGeometry(0.92, 0.35, 0.92), [side, side, top, side, side, side]); m.position.set((c - 1.5) * 1.05, 0, (r - 1.5) * 1.05); S.scene.add(m); cubes.push({ m, top, r, c });
  }
  // the field: an instanced carpet of tiny boards, each with one gold path (rows x cols = 90 x 90)
  const N = 90, geo = new T.BoxGeometry(0.9, 0.06, 0.9), mat = new T.MeshStandardMaterial({ color: 0xffffff, emissive: 0x3a2a10, emissiveIntensity: 1.0, roughness: 0.6 });
  const inst = new T.InstancedMesh(geo, mat, N * N), dm = new T.Object3D(), colr = new T.Color();
  for (let i = 0; i < N; i++) for (let j = 0; j < N; j++) { const k = i * N + j; dm.position.set((i - N / 2) * 1.2, -0.25, (j - N / 2) * 1.2); dm.updateMatrix(); inst.setMatrixAt(k, dm.matrix); colr.setHSL(0.1 + 0.05 * hash(k, 2), 0.8, 0.35 + 0.4 * hash(k, 5)); inst.setColorAt(k, colr); }
  inst.visible = false; S.scene.add(inst);
  const pl = new T.PointLight(0xffd080, 25, 12); pl.position.set(0, 3, 0); S.scene.add(pl);
  S.update = (lt, t) => {
    const p = PERM4[Math.floor(lt * 3.2) % 24], pull = E.io3(o.p0 != null ? seg(t, o.p0, o.p0 + 4.5) : seg(lt, 7.5, 13));
    cubes.forEach(q => { const on = p[q.r] === q.c; q.top.emissiveIntensity = on ? 0.9 : 0; q.m.position.y = on ? 0.12 : 0; });
    inst.visible = pull > 0.02;
    const h = lerp(6.2, 20, pull), d = lerp(5.2, 22, pull); S.camera.position.set(Math.sin(0.5 + lt * 0.05) * d, h, Math.cos(0.5 + lt * 0.05) * d); S.camera.lookAt(0, 0, 0);
  };
  return S;
}

// ---------- H3: Hong–Ou–Mandel. A 50:50 beam splitter; photons leave together, electrons leave apart ----------
function glHOM(T, o = {}) {
  const S = glBase(T, 0x05070f, [10, 40]);
  const cube = new T.Mesh(new T.BoxGeometry(1.6, 1.6, 1.6), new T.MeshStandardMaterial({ color: 0x9fc8ff, transparent: true, opacity: 0.16, roughness: 0.1, metalness: 0.2, depthWrite: false })); S.scene.add(cube);
  const edgesG = new T.EdgesGeometry(new T.BoxGeometry(1.6, 1.6, 1.6)); S.scene.add(new T.LineSegments(edgesG, new T.LineBasicMaterial({ color: 0xbfe0ff, transparent: true, opacity: 0.6 })));
  const mirror = new T.Mesh(new T.PlaneGeometry(2.2, 1.6), new T.MeshStandardMaterial({ color: 0xd8e6ff, metalness: 0.9, roughness: 0.15, transparent: true, opacity: 0.55, side: T.DoubleSide })); mirror.rotation.y = Math.PI / 4; S.scene.add(mirror);
  // beam paths (in the x-z plane): inputs from -x and -z, outputs to +x and +z
  const mk = (a, b, c) => { const m = new T.Mesh(new T.TubeGeometry(new T.LineCurve3(a, b), 2, 0.025, 6, false), new T.MeshBasicMaterial({ color: c, transparent: true, opacity: 0.35 })); S.scene.add(m); return m; };
  const O = new T.Vector3(0, 0, 0), L = 7;
  mk(new T.Vector3(-L, 0, 0), O, 0x8fb4ff); mk(new T.Vector3(0, 0, -L), O, 0x8fb4ff); mk(O, new T.Vector3(L, 0, 0), 0x8fb4ff); mk(O, new T.Vector3(0, 0, L), 0x8fb4ff);
  const ball = col => { const grp = new T.Group(); const m = new T.Mesh(new T.SphereGeometry(0.26, 24, 16), new T.MeshStandardMaterial({ color: col, emissive: col, emissiveIntensity: 1.4 })); grp.add(m); const trail = []; for (let i = 1; i <= 10; i++) { const s = new T.Mesh(new T.SphereGeometry(0.22 * (1 - i / 12), 10, 8), new T.MeshBasicMaterial({ color: col, transparent: true, opacity: 0.5 * (1 - i / 11) })); S.scene.add(s); trail.push(s); } S.scene.add(grp); return { grp, trail, mat: m.material }; };
  const A = ball(0xffd25a), Bq = ball(0xffd25a);
  const lamp = new T.PointLight(0xfff0c0, 18, 10); S.scene.add(lamp);
  // one passage: 0..1 in, 1..2 out; dirA/dirB in {'x','z'}
  const pos = (u, from, out) => { const v = new T.Vector3(); if (u < 1) { const k = 1 - u; return from === 'x' ? v.set(-L * k, 0, 0) : v.set(0, 0, -L * k); } const k = u - 1; return out === 'x' ? v.set(L * k, 0, 0) : v.set(0, 0, L * k); };
  S.update = (lt, t) => {
    const f0 = o.f0 != null ? o.f0 - (t - lt) : 9, fermion = lt >= f0, cyc = fermion ? lt - f0 : lt, n = Math.floor(cyc / 4.5), u = Math.min(2, (cyc % 4.5) / 1.6);
    const outs = fermion ? ['x', 'z'] : (n % 2 ? ['z', 'z'] : ['x', 'x']), off = fermion ? 0 : 0.18;
    const col = fermion ? 0x5aa8ff : 0xffd25a; [A, Bq].forEach(b => { b.mat.color.setHex(col); b.mat.emissive.setHex(col); b.trail.forEach(s => s.material.color.setHex(col)); });
    const pa = pos(u, 'x', outs[0]), pb = pos(u, 'z', outs[1]); if (u > 1 && outs[0] === outs[1]) { const side = outs[0] === 'x' ? new T.Vector3(0, 0, off) : new T.Vector3(off, 0, 0); pa.add(side); pb.sub(side); }
    A.grp.position.copy(pa); Bq.grp.position.copy(pb);
    [[A, 'x', outs[0]], [Bq, 'z', outs[1]]].forEach(([b, fr, ot]) => b.trail.forEach((s, i) => { const uu = Math.max(0, u - i * 0.035); s.position.copy(pos(uu, fr, ot)); }));
    lamp.position.set(0, 1.2, 0); lamp.intensity = 10 + 20 * Math.exp(-Math.pow((u - 1) * 4, 2));
    S.camera.position.set(-4.6 + Math.sin(lt * 0.08) * 0.4, 5.0, 6.2); S.camera.lookAt(0.9, -0.3, 0.9);
  };
  return S;
}
