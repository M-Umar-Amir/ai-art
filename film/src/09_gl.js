// ================= three.js hero scenes (deterministic: everything is a function of the shot-local time lt) =================
function glBase(T, bg = 0x05070f, fog = [30, 90]) {
  const scene = new T.Scene(); scene.background = new T.Color(bg); scene.fog = new T.Fog(bg, fog[0], fog[1]);
  const camera = new T.PerspectiveCamera(38, 16 / 9, 0.05, 300);
  scene.add(new T.AmbientLight(0x8090b0, 0.35));
  const r = RNG(77), pts = [];
  for (let i = 0; i < 1400; i++) { const u = r() * 2 - 1, a = r() * TAU, R = 60 + r() * 40; pts.push(Math.sqrt(1 - u * u) * Math.cos(a) * R, u * R, Math.sqrt(1 - u * u) * Math.sin(a) * R); }
  const sg = new T.BufferGeometry(); sg.setAttribute('position', new T.Float32BufferAttribute(pts, 3));
  scene.add(new T.Points(sg, new T.PointsMaterial({ color: 0xc8d4ff, size: 0.16, transparent: true, opacity: 0.8, fog: false })));
  return { scene, camera };
}
const easeIn = (lt, a, b) => E.out3(seg(lt, a, b));
function glTex(T, key) { const tx = new T.Texture(IMG[key]); tx.colorSpace = T.SRGBColorSpace; tx.anisotropy = 4; tx.needsUpdate = true; return tx; }

// ---------- H1: the Earth (our ETOPO1 relief texture) spinning, speed arrows by latitude; optional stop while the air shell keeps going ----------
// o.stop: absolute time the solid Earth stops; o.arrows: show speed arrows; o.air: show the atmosphere shell; o.spin: rad/s (sped up)
function glGlobe(T, o = {}) {
  const S = glBase(T);
  const sun = new T.DirectionalLight(0xfff2e0, 2.6); sun.position.set(-6, 3, 7); S.scene.add(sun);
  const globe = new T.Mesh(new T.SphereGeometry(1, 96, 64), new T.MeshStandardMaterial({ map: glTex(T, o.tex || 'globe00'), roughness: 0.85, metalness: 0.0 }));
  S.scene.add(globe);
  const atm = new T.Mesh(new T.SphereGeometry(1.025, 64, 48), new T.MeshBasicMaterial({ color: 0x6fa8ff, transparent: true, opacity: 0.10, side: T.BackSide, depthWrite: false })); S.scene.add(atm);
  // speed arrows on the face toward the camera, pointing east (+x on screen), length ∝ cos(latitude)
  const arrows = [];
  if (o.arrows) for (const la of [0, 20, 40, 60, 80]) for (const s of la ? [1, -1] : [1]) {
    const p = la * s * Math.PI / 180, len = 0.62 * Math.cos(p);
    const ar = new T.ArrowHelper(new T.Vector3(1, 0, 0), new T.Vector3(-len / 2, Math.sin(p) * 1.04, Math.cos(p) * 1.04), len, la === 0 ? 0xffd25a : 0xffffff, 0.09, 0.06);
    ar.line.material.linewidth = 2; S.scene.add(ar); arrows.push({ ar, la: la * s });
  }
  // the atmosphere: particles on shells just above the ground, plus fading trail copies
  const air = [], r = RNG(5), P = [];
  for (let i = 0; i < 2600; i++) { const u = r() * 2 - 1, a = r() * TAU, R = 1.035 + r() * 0.05; P.push(Math.sqrt(1 - u * u) * Math.cos(a) * R, u * R, Math.sqrt(1 - u * u) * Math.sin(a) * R); }
  const ag = new T.BufferGeometry(); ag.setAttribute('position', new T.Float32BufferAttribute(P, 3));
  if (o.air) for (let k = 0; k < 7; k++) { const pm = new T.Points(ag, new T.PointsMaterial({ color: k ? 0x9fd0ff : 0xe8f6ff, size: 0.018 * (1 - k * 0.08), transparent: true, opacity: k ? 0.5 * (1 - k / 7) : 0.95, depthWrite: false })); S.scene.add(pm); air.push(pm); }
  const w = o.spin ?? 0.5, tilt = 23.44 * Math.PI / 180;
  S.update = (lt, t) => {
    const ts = o.stop != null ? o.stop - (t - lt) : 1e9;                     // stop time in shot-local seconds
    const ag_ = w * lt; let gg = lt < ts ? w * lt : w * ts + w * 0.25 * E.out3(seg(lt, ts, ts + 0.5));
    if (o.decel) { const D = o.decel, l = Math.min(lt, D); gg = w * (l - l * l / (2 * D)); }
    globe.rotation.y = gg + (o.lon0 ?? -1.2);
    const stopped = lt > ts + 0.3 && !o.decel;
    air.forEach((pm, k) => { pm.rotation.y = (stopped ? ag_ - k * 0.035 * seg(lt, ts, ts + 1) : gg) + (o.lon0 ?? -1.2); pm.visible = k === 0 || stopped; });
    arrows.forEach(({ ar }) => { ar.visible = !stopped || o.keepArrows; });
    const d = lerp(o.d0 ?? 3.6, o.d1 ?? 3.2, E.io2(seg(lt, 0, o.dur ?? 10)));
    S.camera.position.set(Math.sin(0.15) * d, (o.camY ?? 0.35) + 0.1 * Math.sin(lt * 0.2), Math.cos(0.15) * d); S.camera.lookAt(0, o.lookY ?? 0, 0);
  };
  S.globe = globe; return S;
}

// ---------- H3: the ocean draining to the poles (13 precomputed frames of our ETOPO1 no-spin calculation), crossfaded ----------
// o.m0, o.m1: absolute start/end time of the morph; camera tilts to show the north, then the south
function glMorph(T, o = {}) {
  const S = glBase(T);
  const sun = new T.DirectionalLight(0xfff2e0, 2.4); sun.position.set(-5, 4, 7); S.scene.add(sun);
  S.scene.add(new T.HemisphereLight(0xbfd8ff, 0x202030, 0.5));
  const tex = []; for (let i = 0; i < 13; i++) tex.push(glTex(T, 'globe' + String(i).padStart(2, '0')));
  const a = new T.Mesh(new T.SphereGeometry(1, 96, 64), new T.MeshStandardMaterial({ map: tex[0], roughness: 0.85 }));
  const b = new T.Mesh(new T.SphereGeometry(1.002, 96, 64), new T.MeshStandardMaterial({ map: tex[1], roughness: 0.85, transparent: true, opacity: 0 }));
  S.scene.add(a); S.scene.add(b);
  S.update = (lt, t) => {
    const m0 = (o.m0 ?? 1) - (t - lt), m1 = (o.m1 ?? 8) - (t - lt), u = E.io2(seg(lt, m0, m1)) * 12, i = Math.min(11, Math.floor(u)), f = u - i;
    a.material.map = tex[i]; b.material.map = tex[i + 1]; b.material.opacity = f; a.material.needsUpdate = b.material.needsUpdate = true;
    const rot = (o.lon0 ?? -0.4) + lt * 0.06; a.rotation.y = b.rotation.y = rot;
    const el = o.el ? o.el(lt, t) : 0.25 * Math.sin(lt * 0.25), d = o.d ?? 3.3;
    S.camera.position.set(Math.cos(el) * Math.sin(0.1) * d, Math.sin(el) * d, Math.cos(el) * Math.cos(0.1) * d); S.camera.lookAt(0, 0, 0);
  };
  return S;
}

// ---------- H2: Foucault's pendulum: the swing plane turns clockwise over a ring of sand and pins (swing and drift sped up) ----------
function glFoucault(T, o = {}) {
  const S = glBase(T, 0x0d0b09, [20, 70]);
  S.scene.add(new T.HemisphereLight(0xfff0d8, 0x2a2018, 0.9));
  const key = new T.SpotLight(0xffe6c0, 260, 60, 0.6, 0.5); key.position.set(4, 18, 6); key.target.position.set(0, 0, 0); S.scene.add(key); S.scene.add(key.target);
  const floor = new T.Mesh(new T.CircleGeometry(9, 96), new T.MeshStandardMaterial({ color: 0x8a7f72, roughness: 0.6, metalness: 0.05 })); floor.rotation.x = -Math.PI / 2; S.scene.add(floor);
  const sand = new T.Mesh(new T.CircleGeometry(4.6, 96), new T.MeshStandardMaterial({ color: 0xcdb48a, roughness: 1 })); sand.rotation.x = -Math.PI / 2; sand.position.y = 0.01; S.scene.add(sand);
  const rail = new T.Mesh(new T.TorusGeometry(4.75, 0.08, 12, 128), new T.MeshStandardMaterial({ color: 0xb08a40, metalness: 0.8, roughness: 0.3 })); rail.rotation.x = -Math.PI / 2; rail.position.y = 0.35; S.scene.add(rail);
  // traces cut in the sand: one thin dark line per past swing direction
  const traces = [], tm = new T.MeshBasicMaterial({ color: 0x6e5a3c });
  for (let k = 0; k < 90; k++) { const m = new T.Mesh(new T.BoxGeometry(7.2, 0.004, 0.025), tm); m.position.y = 0.02; m.visible = false; S.scene.add(m); traces.push(m); }
  // pins around the ring, knocked flat when the swing plane reaches them
  const pins = [], pm = new T.MeshStandardMaterial({ color: 0xf2efe6, roughness: 0.5 });
  for (let k = 0; k < 36; k++) { const a = k / 36 * TAU, g = new T.Group(); const m = new T.Mesh(new T.CylinderGeometry(0.05, 0.07, 0.5, 10), pm); m.position.y = 0.25; g.add(m); g.position.set(Math.cos(a) * 3.85, 0, Math.sin(a) * 3.85); S.scene.add(g); pins.push({ g, a }); }
  const L = 14, A = 3.6, bob = new T.Mesh(new T.SphereGeometry(0.42, 48, 32), new T.MeshStandardMaterial({ color: 0xd8b25a, metalness: 0.95, roughness: 0.22 }));
  S.scene.add(bob);
  const wire = new T.Mesh(new T.CylinderGeometry(0.012, 0.012, 1, 6), new T.MeshStandardMaterial({ color: 0x333333, metalness: 0.6 })); S.scene.add(wire);
  const pivot = new T.Vector3(0, L + 0.6, 0);
  const Tsw = o.period ?? 2.6, drift = o.drift ?? 0.11;                // seconds per swing; radians per second of plane drift (sped up)
  const th0 = 0.35;
  S.update = (lt, t) => {
    const th = th0 - drift * lt, ph = Math.sin(TAU * lt / Tsw), x = A * ph, sw = Math.asin(x / L);
    bob.position.set(Math.cos(th) * x, 0.75 + L * (1 - Math.cos(sw)), Math.sin(th) * x);
    const mid = pivot.clone().add(bob.position).multiplyScalar(0.5), len = pivot.distanceTo(bob.position);
    wire.position.copy(mid); wire.scale.set(1, len, 1); wire.lookAt(bob.position); wire.rotateX(Math.PI / 2);
    const swept = drift * lt, nTr = Math.min(traces.length, Math.floor(swept / 0.03) + 1);
    traces.forEach((m, k) => { m.visible = k < nTr; m.rotation.y = -(th0 - k * 0.03); });
    pins.forEach(p => { let d = ((th0 - p.a) % Math.PI + Math.PI) % Math.PI; const hit = d <= swept; p.g.rotation.set(0, -p.a, hit ? Math.PI / 2 * 0.98 : 0); });
    const ca = 0.6 + lt * 0.04, cr = o.camR ?? 10.5; S.camera.position.set(Math.sin(ca) * cr, o.camY ?? 8.5, Math.cos(ca) * cr); S.camera.lookAt(0, o.lookY ?? 1.2, 0);
  };
  return S;
}
