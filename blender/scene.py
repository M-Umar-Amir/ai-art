# Sound & Soul · Short 01 · "How Auto-Tune works" — the whole 9:16 short as one procedural Blender scene.
# blender -b --python scene.py -- START END OUTDIR [res%] [samples]
# Renders frames START..END-1 (24 fps) as PNG into OUTDIR, skipping frames that already exist (resumable).
# Timing comes from vo/short.words.json (edge-tts word boundaries), so every beat lands on its spoken word.
import bpy, bmesh, math, json, os, sys, random
from mathutils import Vector

A = sys.argv[sys.argv.index('--') + 1:]
LIST = [int(v) for v in A[0][5:].split(',')] if A[0].startswith('list:') else None
F0, F1, OUT = (0, 0, A[2]) if LIST else (int(A[0]), int(A[1]), A[2])
OUT = os.path.abspath(OUT)
RES = int(A[3]) if len(A) > 3 else 100
SAMPLES = int(A[4]) if len(A) > 4 else 32
FPS = 24
HERE = os.path.dirname(os.path.abspath(__file__))
WORDS = json.load(open(os.path.join(HERE, 'vo', 'short.words.json')))
W = lambda i: WORDS[i][1]
DUR = 28.5
os.makedirs(OUT, exist_ok=True)

# ---------------- helpers ----------------
def clamp(x, a=0.0, b=1.0): return max(a, min(b, x))
def seg(t, a, b): return clamp((t - a) / (b - a)) if b > a else float(t >= a)
def ss(x): x = clamp(x); return x * x * (3 - 2 * x)
def lerp(a, b, u): return a + (b - a) * u
def fade(t, a, b, fi=0.6, fo=0.6): return min(ss(seg(t, a, a + fi)), 1 - ss(seg(t, b, b + fo)))

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.render.engine = 'CYCLES'
sc.cycles.device = 'CPU'
sc.cycles.samples = SAMPLES
sc.cycles.use_denoising = True
sc.cycles.max_bounces = 4
sc.render.resolution_x, sc.render.resolution_y, sc.render.resolution_percentage = 1080, 1920, RES
sc.render.fps = FPS
sc.render.image_settings.file_format = 'PNG'
sc.view_settings.view_transform = 'AgX'
sc.view_settings.look = 'AgX - Punchy'
sc.render.film_transparent = False

# world: deep indigo with a warm horizon glow
w = bpy.data.worlds.new('W'); sc.world = w; w.use_nodes = True; wn = w.node_tree
bg = wn.nodes['Background']; bg.inputs[1].default_value = 1.0
grad = wn.nodes.new('ShaderNodeTexGradient'); grad.gradient_type = 'LINEAR'
tc = wn.nodes.new('ShaderNodeTexCoord'); mp = wn.nodes.new('ShaderNodeMapping'); mp.inputs['Rotation'].default_value = (0, math.radians(90), 0)
ramp = wn.nodes.new('ShaderNodeValToRGB'); r = ramp.color_ramp
r.elements[0].position = 0.42; r.elements[0].color = (0.012, 0.008, 0.03, 1)
r.elements[1].position = 0.62; r.elements[1].color = (0.0015, 0.002, 0.012, 1)
e = r.elements.new(0.5); e.color = (0.05, 0.022, 0.03, 1)
wn.links.new(tc.outputs['Generated'], mp.inputs[0]); wn.links.new(mp.outputs[0], grad.inputs[0]); wn.links.new(grad.outputs[1], ramp.inputs[0]); wn.links.new(ramp.outputs[0], bg.inputs[0])

MATS = {}
def emis(name, col, strength=4.0, glassy=False):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; nt.nodes.clear()
    o = nt.nodes.new('ShaderNodeOutputMaterial')
    em = nt.nodes.new('ShaderNodeEmission'); em.inputs[0].default_value = (*col, 1); em.inputs[1].default_value = strength
    if glassy:
        gl = nt.nodes.new('ShaderNodeBsdfGlass'); gl.inputs['Color'].default_value = (0.75, 0.9, 1.0, 1); gl.inputs['Roughness'].default_value = 0.05
        ad = nt.nodes.new('ShaderNodeAddShader'); nt.links.new(gl.outputs[0], ad.inputs[0]); nt.links.new(em.outputs[0], ad.inputs[1]); nt.links.new(ad.outputs[0], o.inputs[0])
    else:
        nt.links.new(em.outputs[0], o.inputs[0])
    MATS[name] = em; return m
def setE(name, s, col=None):
    MATS[name].inputs[1].default_value = max(0.0, s)
    if col: MATS[name].inputs[0].default_value = (*col, 1)

def curve_obj(name, n, mat, depth=0.035):
    cu = bpy.data.curves.new(name, 'CURVE'); cu.dimensions = '3D'; cu.bevel_depth = depth; cu.bevel_resolution = 3; cu.use_fill_caps = True
    sp = cu.splines.new('POLY'); sp.points.add(n - 1)
    ob = bpy.data.objects.new(name, cu); sc.collection.objects.link(ob); cu.materials.append(mat); return ob
def set_pts(ob, pts):
    sp = ob.data.splines[0]
    for i, p in enumerate(pts): sp.points[i].co = (p[0], p[1], p[2], 1)

GOLD, BLUE, ROSE, ICE = (1.0, 0.6, 0.22), (0.3, 0.55, 1.0), (1.0, 0.42, 0.45), (0.65, 0.88, 1.0)

# ---------------- floor: an eight-point-star (khatam) inlay, faint gold ----------------
bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, -5.2)); floor = bpy.context.object
fm = bpy.data.materials.new('floor'); fm.use_nodes = True; pb = fm.node_tree.nodes['Principled BSDF']
pb.inputs['Base Color'].default_value = (0.01, 0.008, 0.015, 1); pb.inputs['Roughness'].default_value = 0.18; pb.inputs['Metallic'].default_value = 0.4
floor.data.materials.append(fm)
starmat = emis('star', GOLD, 0.35)
for i in range(-5, 6):
    for j in range(0, 9):
        cx, cy = i * 2.4, j * 2.4
        for rot in (0, math.pi / 4):
            ob = curve_obj('st', 5, starmat, 0.018)
            pts = [(cx + 0.95 * math.cos(rot + k * math.pi / 2), cy + 0.95 * math.sin(rot + k * math.pi / 2), -5.17) for k in range(5)]
            set_pts(ob, pts)

# ---------------- the voice wave ----------------
NW = 420
wave = curve_obj('wave', NW, emis('wave', GOLD, 5.0), 0.04)
wave_glass = emis('waveglass', ICE, 1.5, glassy=True)
def voice(x, t, sq=0.0):
    ph = x * 3.1 - t * 2.2
    y = 0.62 * math.sin(ph) + 0.24 * math.sin(2 * ph + 0.7) + 0.1 * math.sin(3 * ph + 1.3)
    if sq > 0: y = lerp(y, 0.62 * math.tanh(6 * math.sin(ph)), sq)   # "turns to glass": hard, squared edges
    return y

# ---------------- geology cross-section: rock layers + seismic echoes ----------------
LAY = []
for k, z in enumerate((-1.2, -2.3, -3.5)):
    ob = curve_obj('lay%d' % k, 120, emis('lay%d' % k, (0.9, 0.55, 0.3), 0.0), 0.022)
    set_pts(ob, [(-4 + 8 * i / 119, 0.4, z + 0.12 * math.sin(i * 0.11 + k)) for i in range(120)]); LAY.append(ob)
ECHO = [curve_obj('echo%d' % k, 64, emis('echo%d' % k, (1.0, 0.8, 0.5), 0.0), 0.02) for k in range(4)]

# ---------------- autocorrelation: two copies of one cycle ----------------
CYC = [curve_obj('cyc%d' % k, 90, emis('cyc%d' % k, GOLD if k == 0 else BLUE, 0.0), 0.04) for k in range(2)]

# ---------------- the note ladder (12 rings) and the pitch orb ----------------
RINGS = []
for k in range(12):
    bpy.ops.mesh.primitive_torus_add(major_radius=0.32, minor_radius=0.022, location=(0, 0.2, -3.0 + k * 0.55), rotation=(math.pi / 2, 0, 0))
    o = bpy.context.object; o.data.materials.append(emis('ring%d' % k, BLUE, 0.0)); RINGS.append(o)
RZ = lambda k: -3.0 + k * 0.55
bpy.ops.mesh.primitive_uv_sphere_add(radius=0.13, location=(0, 0, 0), segments=32, ring_count=16); orb = bpy.context.object
orb.data.materials.append(emis('orb', (1.0, 0.85, 0.6), 0.0))
SHARDS = []
rnd = random.Random(7)
for k in range(26):
    bpy.ops.mesh.primitive_ico_sphere_add(radius=0.05, subdivisions=1); s = bpy.context.object
    s.data.materials.append(emis('shard%d' % k, ICE, 0.0)); s['dir'] = (rnd.uniform(-1, 1), rnd.uniform(-0.3, 0.3), rnd.uniform(-1, 1)); SHARDS.append(s)

# ---------------- the melisma ribbon (between the notes) ----------------
NR = 300
rib = curve_obj('rib', NR, emis('rib', ROSE, 0.0), 0.045)
set_pts(rib, [(0.85 * math.sin(i / NR * 9.0), 0.1, RZ(0) - 0.3 + (RZ(11) - RZ(0) + 0.6) * i / (NR - 1) + 0.18 * math.sin(i / NR * 23)) for i in range(NR)])

# ---------------- dust ----------------
DUST = []
for k in range(90):
    bpy.ops.mesh.primitive_ico_sphere_add(radius=0.018, subdivisions=1, location=(rnd.uniform(-4, 4), rnd.uniform(-1, 6), rnd.uniform(-5, 6)))
    d = bpy.context.object; d.data.materials.append(starmat if k % 3 else emis('dust%d' % k, ROSE, 1.2)); d['v'] = rnd.uniform(0.05, 0.16); d['z0'] = d.location.z; DUST.append(d)

# ---------------- camera ----------------
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera = cam
cam.data.lens = 35
def aim(loc, tgt):
    cam.location = loc; d = Vector(tgt) - Vector(loc); cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()

# compositor bloom
sc.use_nodes = True; nt = sc.node_tree; rl = nt.nodes['Render Layers']; comp = nt.nodes['Composite']
gl = nt.nodes.new('CompositorNodeGlare'); gl.glare_type = 'FOG_GLOW'; gl.threshold = 0.6; gl.size = 8
nt.links.new(rl.outputs['Image'], gl.inputs['Image']); nt.links.new(gl.outputs['Image'], comp.inputs['Image'])

# ---------------- the timeline ----------------
T_EARTH, T_MACH, T_MEAS, T_NOTE, T_PULL, T_FAST, T_GLASS, T_BUT, T_BETWEEN = W(14), W(30), W(35), W(39), W(45), W(50), W(60), W(61), W(66)
def update(t):
    # camera: slow dolly, dips toward the layers, rises to the ladder, pulls back at the end
    keys = [(0, (0, -9.0, 0.6), (0, 0, 0.6)), (T_EARTH, (0.3, -10.5, -0.6), (0, 0, -1.2)), (T_MACH, (0, -10.5, 0.2), (0, 0, 0.2)),
            (T_NOTE, (0.2, -11.0, 0.0), (0, 0, 0.0)), (T_FAST, (-0.3, -9.5, 0.3), (0, 0, 0.3)), (T_BUT, (0, -11.5, 0.0), (0, 0, 0.0)), (DUR + 1, (0, -14.0, 0.8), (0, 0, 0.0))]
    i = max(k for k in range(len(keys) - 1) if keys[k][0] <= t)
    a, b = keys[i], keys[i + 1]; u = ss(seg(t, a[0], a[0] + min(3.0, b[0] - a[0])))
    prev = keys[i - 1] if i > 0 else a
    aim([lerp(a[1][j], b[1][j], u) for j in range(3)], [lerp(a[2][j], b[2][j], u) for j in range(3)])
    # --- the wave: born from a point (Listen), lives through A and C, rises during the geology beat, turns to glass, then fades for the ribbon
    born = ss(seg(t, 0.0, W(7) + 0.6))
    wz = lerp(0.6, 2.9, ss(seg(t, T_EARTH, T_EARTH + 1.5))) if t < T_MACH else lerp(2.9, 3.9, ss(seg(t, T_MACH, T_MACH + 1.2)))
    sq = ss(seg(t, T_FAST + 0.2, T_GLASS))
    amp = born * (1 - 0.6 * ss(seg(t, T_NOTE, T_NOTE + 1)))
    half = 3.4 * born
    set_pts(wave, [(x, 0, wz + amp * voice(x, t, sq)) for x in (-half + 2 * half * i / (NW - 1) for i in range(NW))])
    wave.data.bevel_depth = 0.04 * max(born, 0.05)
    glassy = t >= T_FAST + 0.6 and t < T_BUT + 0.3
    wave.data.materials[0] = wave_glass if glassy else bpy.data.materials['wave']
    setE('wave', 5.0 * (1 - ss(seg(t, T_BUT, T_BUT + 1.5))) * (0.85 + 0.15 * math.sin(t * 3)) + 30 * math.exp(-max(0, t - W(8)) * 3) * (t >= W(8)) * 0.3)
    setE('waveglass', 1.5 + 8 * math.exp(-max(0, t - T_GLASS) * 4) * (t >= T_GLASS))
    # --- geology: layers glow, echoes go down and bounce back up
    lay_a = fade(t, T_EARTH + 0.4, T_MACH + 0.8)
    for k in range(3): setE('lay%d' % k, 1.4 * lay_a)
    for k, ob in enumerate(ECHO):
        t0 = W(24) - 0.6 + k * 0.45; u = seg(t, t0, t0 + 2.4)
        depth = (-1.2, -2.3, -3.5, -2.3)[k]
        zz = lerp(0.2, depth, ss(u * 2)) if u < 0.5 else lerp(depth, 0.2, ss(u * 2 - 1))
        rad = 0.2 + 2.2 * u
        set_pts(ob, [(rad * math.cos(math.pi * (0.15 + 0.7 * i / 63)), 0.3, zz - 0.25 * rad * math.sin(math.pi * (0.15 + 0.7 * i / 63)) * (1 if u < 0.5 else -1)) for i in range(64)])
        setE('echo%d' % k, 3.0 * lay_a * math.sin(math.pi * u))
    # --- autocorrelation: one cycle and its neighbour slide together and lock
    ca = fade(t, T_MEAS - 0.2, T_NOTE + 0.4)
    slide = 1 - ss(seg(t, T_MEAS + 0.2, W(38) + 0.2))
    period = 2 * math.pi / 3.1
    for k, ob in enumerate(CYC):
        dx = (k * 1.4 * slide)
        set_pts(ob, [(-period / 2 + period * i / 89 + dx - 0.7 * slide, -0.3 * k, 1.2 + 0.9 * voice(-period / 2 + period * i / 89, 0.0)) for i in range(90)])
        lock = math.exp(-max(0, t - (W(38) + 0.2)) * 3) * (t >= W(38) + 0.2)
        setE('cyc%d' % k, ca * (4.0 + 25 * lock))
    # --- note ladder + orb
    la = ss(seg(t, T_NOTE - 0.2, T_NOTE + 0.8)) * (1 - 0.75 * ss(seg(t, T_BUT, T_BUT + 1.2)))
    for k, rg in enumerate(RINGS):
        setE('ring%d' % k, la * (2.2 + 0.4 * math.sin(t * 2 + k)))
    # orb: sits between ring 6 and 7, gently pulled to 7; then (too fast) snaps around a wobbling melody
    if t < T_FAST:
        z = lerp(RZ(6) + 0.33, RZ(7), ss(seg(t, T_PULL, W(49) + 0.4)))
    else:
        mel = 7 + 1.6 * math.sin((t - T_FAST) * 2.3) + 0.8 * math.sin((t - T_FAST) * 5.1)
        z = RZ(round(clamp(mel, 0, 11)))
    oa = ss(seg(t, T_NOTE + 0.3, T_NOTE + 0.9)) * (1 - ss(seg(t, T_BUT, T_BUT + 1.0)))
    orb.location = (0, 0.1, z); orb.scale = (oa,) * 3 if oa > 0.01 else (0.001,) * 3
    setE('orb', 12 * oa)
    # --- shards at "glass"
    for s in SHARDS:
        u = seg(t, T_GLASS, T_GLASS + 1.6); d = s['dir']
        s.location = (d[0] * 3.5 * ss(u), d[1] * 2 * u, wz + d[2] * 2.2 * ss(u))
        s.scale = ((0.001,) * 3) if u <= 0 or u >= 1 else ((1.2 - u,) * 3)
        setE(s.data.materials[0].name, 10 * (1 - u) * (u > 0))
    # --- the ribbon between the notes
    ra = ss(seg(t, T_BUT + 0.3, W(68) + 0.8)) * (1 - ss(seg(t, DUR - 1.4, DUR - 0.2)))
    rib.data.bevel_factor_end = ss(seg(t, T_BUT + 0.3, W(68) + 1.4))
    setE('rib', 7 * ra)
    # --- dust drifts upward, everything breathes; fade to the loop point
    for d in DUST: d.location.z = d['z0'] + (t * d['v']) % 11 - (0 if d['z0'] < 0 else 0)
    setE('star', 0.35 * (0.6 + 0.4 * math.sin(t * 0.8)) * (1 - ss(seg(t, DUR - 1.2, DUR))) + 0.3 * ss(seg(t, T_BUT, T_BUT + 2)))

def hide_dark():
    for ob in sc.objects:
        if ob.type in ('CURVE', 'MESH') and ob.data.materials and ob.data.materials[0] and ob.data.materials[0].name in MATS and ob.data.materials[0].name != 'star':
            ob.hide_render = MATS[ob.data.materials[0].name].inputs[1].default_value < 0.02
for f in (LIST or range(F0, F1)):
    path = os.path.join(OUT, 'f%05d.png' % f)
    if os.path.exists(path): continue
    update(f / FPS); hide_dark()
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print('FRAME', f, flush=True)
print('CHUNK_DONE', F0, F1)
