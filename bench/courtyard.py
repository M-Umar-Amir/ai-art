# Sufi courtyard at dusk — style-frame scene (Eevee). blender -b --python courtyard.py -- OUT.png [portrait|landscape] [samples] [shot]
import bpy, bmesh, math, os, sys, random
from mathutils import Vector
A = sys.argv[sys.argv.index('--') + 1:]
OUT = os.path.abspath(A[0]); ORIENT = A[1] if len(A) > 1 else 'portrait'; SAMPLES = int(A[2]) if len(A) > 2 else 64; SHOT = A[3] if len(A) > 3 else 'pool'
ENGINE = A[4] if len(A) > 4 else 'EEVEE'; RESP = int(A[5]) if len(A) > 5 else 100; NFR = int(A[6]) if len(A) > 6 else 1
HERE = os.path.dirname(os.path.abspath(__file__))
rnd = random.Random(5)
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.render.engine = 'BLENDER_EEVEE_NEXT' if ENGINE == 'EEVEE' else 'CYCLES'
if ENGINE != 'EEVEE':
    sc.cycles.device = 'CPU'; sc.cycles.samples = SAMPLES; sc.cycles.use_denoising = True
ee = sc.eevee
ee.taa_render_samples = SAMPLES
for k, v in [('use_raytracing', True), ('use_shadows', True), ('volumetric_tile_size', '4'), ('use_volumetric_shadows', True), ('volumetric_end', 60.0), ('use_gtao', True), ('fast_gi_distance', 6.0)]:
    try: setattr(ee, k, v)
    except Exception: pass
sc.render.resolution_x, sc.render.resolution_y = (1080, 1920) if ORIENT == 'portrait' else (1920, 1080)
sc.view_settings.view_transform = 'AgX'; sc.view_settings.look = 'AgX - Medium High Contrast'
sc.render.image_settings.file_format = 'PNG'

# ---------------- materials ----------------
def pbsdf(name, col, rough=0.6, metal=0.0, emit=None, es=0.0, tex=None, scale=1.0, bump=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; b = nt.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value = (*col, 1); b.inputs['Roughness'].default_value = rough; b.inputs['Metallic'].default_value = metal
    if emit: b.inputs['Emission Color'].default_value = (*emit, 1); b.inputs['Emission Strength'].default_value = es
    if tex:
        ti = nt.nodes.new('ShaderNodeTexImage'); ti.image = bpy.data.images.load(tex); mp = nt.nodes.new('ShaderNodeMapping'); tc = nt.nodes.new('ShaderNodeTexCoord')
        mp.inputs['Scale'].default_value = (scale, scale, scale); nt.links.new(tc.outputs['UV'], mp.inputs[0]); nt.links.new(mp.outputs[0], ti.inputs[0]); nt.links.new(ti.outputs[0], b.inputs['Base Color'])
    if bump > 0:
        nz = nt.nodes.new('ShaderNodeTexNoise'); nz.inputs['Scale'].default_value = 18; nz.inputs['Detail'].default_value = 8
        bp = nt.nodes.new('ShaderNodeBump'); bp.inputs['Strength'].default_value = bump; nt.links.new(nz.outputs[0], bp.inputs['Height']); nt.links.new(bp.outputs[0], b.inputs['Normal'])
    return m
PLASTER = pbsdf('plaster', (0.78, 0.58, 0.40), 0.85, bump=0.25)
PLASTER_DK = pbsdf('plaster_dk', (0.55, 0.36, 0.24), 0.9, bump=0.3)
MARBLE = pbsdf('marble', (0.86, 0.84, 0.8), 0.25)
WOOD = pbsdf('wood', (0.22, 0.11, 0.05), 0.55, bump=0.2)
BRASS = pbsdf('brass', (0.85, 0.6, 0.25), 0.28, metal=1.0)
GLOWGLASS = pbsdf('lampglass', (1.0, 0.6, 0.25), 0.3, emit=(1.0, 0.55, 0.2), es=18)
def pierced():
    m = bpy.data.materials.new('pierced'); m.use_nodes = True; nt = m.node_tree; b = nt.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value = (0.75, 0.5, 0.2, 1); b.inputs['Metallic'].default_value = 1.0; b.inputs['Roughness'].default_value = 0.3
    ti = nt.nodes.new('ShaderNodeTexImage'); ti.image = bpy.data.images.load(os.path.join(HERE, 'zellige.png'))
    mp = nt.nodes.new('ShaderNodeMapping'); mp.inputs['Scale'].default_value = (3, 3, 3); tc = nt.nodes.new('ShaderNodeTexCoord')
    nt.links.new(tc.outputs['UV'], mp.inputs[0]); nt.links.new(mp.outputs[0], ti.inputs[0])
    rp = nt.nodes.new('ShaderNodeValToRGB'); rp.color_ramp.elements[0].position = 0.55; rp.color_ramp.elements[1].position = 0.62; rp.color_ramp.elements[0].color = (1, 1, 1, 1); rp.color_ramp.elements[1].color = (0, 0, 0, 1)
    bw = nt.nodes.new('ShaderNodeRGBToBW'); nt.links.new(ti.outputs[0], bw.inputs[0]); nt.links.new(bw.outputs[0], rp.inputs[0])
    mu = nt.nodes.new('ShaderNodeMath'); mu.operation = 'MULTIPLY'; mu.inputs[1].default_value = 40
    nt.links.new(rp.outputs[0], mu.inputs[0]); nt.links.new(mu.outputs[0], b.inputs['Emission Strength'])
    b.inputs['Emission Color'].default_value = (1.0, 0.55, 0.2, 1); return m
PIERCED = pierced()
TILE = pbsdf('zellige', (1, 1, 1), 0.18, tex=os.path.join(HERE, 'zellige.png'), scale=1.0)
WATER = pbsdf('water', (0.01, 0.04, 0.05), 0.02)
INTERIOR = pbsdf('interior', (0.08, 0.04, 0.03), 0.9)
RIBBON = pbsdf('ribbon', (1.0, 0.7, 0.3), 0.3, emit=(1.0, 0.62, 0.25), es=14)

def add(ob): sc.collection.objects.link(ob); return ob
def box(name, loc, size, mat):
    bpy.ops.mesh.primitive_cube_add(location=loc); o = bpy.context.object; o.scale = (size[0] / 2, size[1] / 2, size[2] / 2); o.name = name
    o.data.materials.append(mat); bpy.ops.object.transform_apply(scale=True); return o
def uvwrap(o, s=1.0):
    bpy.context.view_layer.objects.active = o; o.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.uv.cube_project(cube_size=s); bpy.ops.object.mode_set(mode='OBJECT'); o.select_set(False)

# ---------------- floor: zellige ----------------
fl = box('floor', (0, 2, -0.05), (16, 22, 0.1), TILE); uvwrap(fl, 0.8)
# reflecting pool (sunk water + marble rim)
for (x, y, sx, sy) in [(0, -1.1, 3.4, 0.3), (0, 6.1, 3.4, 0.3), (-1.55, 2.5, 0.3, 7.5), (1.55, 2.5, 0.3, 7.5)]: box('rim', (x, y, 0.08), (sx, sy, 0.2), MARBLE)
wat = box('water', (0, 2.5, 0.02), (2.8, 7.0, 0.04), WATER)
wat.modifiers.new('sub', 'SUBSURF').levels = 0
# gentle ripples: displace with noise
tx = bpy.data.textures.new('rip', 'CLOUDS'); tx.noise_scale = 0.5
bpy.context.view_layer.objects.active = wat
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.subdivide(number_cuts=60); bpy.ops.object.mode_set(mode='OBJECT')
dm = wat.modifiers.new('ripple', 'DISPLACE'); dm.texture = tx; dm.strength = 0.012
# small fountain bowl
bpy.ops.mesh.primitive_cylinder_add(radius=0.45, depth=0.5, location=(0, 6.9, 0.25), vertices=32); bpy.context.object.data.materials.append(MARBLE)

# ---------------- arcade: horseshoe-pointed arches ----------------
def arch_panel(name, w=2.4, h=5.0, ow=1.7, spring=2.7, rise=1.3, depth=0.55, n=40):
    slab = box(name, (0, 0, h / 2), (w, depth, h), PLASTER)
    # cutter outline: straight jambs, then two arcs meeting at a point (slight horseshoe bulge)
    pts = [(-ow / 2, -0.01), (-ow / 2, spring)]
    pts = [(-ow / 2, -0.01), (-ow / 2, spring)]
    for k in range(1, n + 1):
        u = k / n
        z = spring + rise * math.sin(math.pi * u * 0.5) ** 0.85
        x = -ow / 2 + (ow / 2) * (1 - math.cos(math.pi / 2 * u)) - 0.06 * math.sin(math.pi * u)   # horseshoe bulge
        pts.append((x, z))
    right = [(-x, z) for (x, z) in reversed(pts[2:-1])]
    pts = pts + right + [(ow / 2, spring), (ow / 2, -0.01)]
    me = bpy.data.meshes.new('cut'); bm = bmesh.new(); vs = [bm.verts.new((x, -depth, z)) for x, z in pts]; f = bm.faces.new(vs)
    bm.to_mesh(me); bm.free(); cut = bpy.data.objects.new('cut', me); add(cut)
    # thicken the cutter through the slab
    sm = cut.modifiers.new('s', 'SOLIDIFY'); sm.thickness = depth * 3; sm.offset = 0
    cut.location = (0, depth * 0.5, 0)
    bm_ = slab.modifiers.new('b', 'BOOLEAN'); bm_.object = cut; bm_.operation = 'DIFFERENCE'; bm_.solver = 'EXACT'
    bpy.context.view_layer.objects.active = slab; bpy.ops.object.modifier_apply(modifier='b')
    bpy.data.objects.remove(cut)
    # recess trim: a thin marble band around the arch face
    return slab
def arcade(origin, direction, bays, face_rot):
    ox, oy = origin; dx, dy = direction
    for b in range(bays):
        cx, cy = ox + dx * 2.4 * b, oy + dy * 2.4 * b
        p = arch_panel('arch', 2.4); p.location = (cx, cy, 0); p.rotation_euler = (0, 0, face_rot)
        # slim marble column at each pier + capital
        for s in (-1, 1):
            px = cx + s * 1.02 * math.cos(face_rot) ; py = cy + s * 1.02 * math.sin(face_rot)
            nx, ny = -math.sin(face_rot) * -0.32, math.cos(face_rot) * -0.32
            bpy.ops.mesh.primitive_cylinder_add(radius=0.1, depth=2.5, location=(px + nx, py + ny, 1.25), vertices=24); bpy.context.object.data.materials.append(MARBLE)
            box('cap', (px + nx, py + ny, 2.58), (0.32, 0.32, 0.16), MARBLE)
        # dark interior behind each bay with a warm glow
        ix, iy = cx + math.sin(face_rot) * 1.6, cy - math.cos(face_rot) * -1.6
        L = bpy.data.objects.new('in', bpy.data.lights.new('in', 'POINT')); L.data.energy = 60; L.data.use_shadow = False; L.data.color = (1.0, 0.55, 0.25); L.location = (cx + (-math.sin(face_rot)) * -1.5, cy + math.cos(face_rot) * 1.5, 1.4); add(L)
# back wall (y=8.5) facing -y, side walls facing inward
arcade((-4.8, 8.5), (1, 0), 5, 0)
arcade((-6.5, -1.5), (0, 1), 4, math.pi / 2)
arcade((6.5, -1.5), (0, 1), 4, -math.pi / 2)
# interior boxes (dark rooms behind the arches) and upper walls with a tiled frieze band
box('room_b', (0, 10.3, 2.5), (14, 3.0, 5), INTERIOR); box('room_l', (-8.3, 3.3, 2.5), (3.0, 12, 5), INTERIOR); box('room_r', (8.3, 3.3, 2.5), (3.0, 12, 5), INTERIOR)
up_b = box('up_b', (0, 8.5, 6.4), (14.5, 0.55, 2.8), PLASTER); up_l = box('up_l', (-6.5, 3.3, 6.4), (0.55, 12, 2.8), PLASTER); up_r = box('up_r', (6.5, 3.3, 6.4), (0.55, 12, 2.8), PLASTER)
for (loc, size) in [((0, 8.2, 5.35), (14.5, 0.06, 0.5)), ((-6.2, 3.3, 5.35), (0.06, 12, 0.5)), ((6.2, 3.3, 5.35), (0.06, 12, 0.5))]:
    f = box('frieze', loc, size, TILE); uvwrap(f, 0.5)
for (loc, size) in [((0, 8.0, 7.9), (15, 1.2, 0.25)), ((-6.0, 3.3, 7.9), (1.2, 13, 0.25)), ((6.0, 3.3, 7.9), (1.2, 13, 0.25))]: box('cornice', loc, size, WOOD)
# dado of zellige on the piers' lower part is suggested by the floor; potted cypresses for scale
for (x, y) in [(-3.6, 7.2), (3.6, 7.2), (-5.2, 0.5), (5.2, 0.5)]:
    bpy.ops.mesh.primitive_cylinder_add(radius=0.38, depth=0.7, location=(x, y, 0.35), vertices=24); bpy.context.object.data.materials.append(PLASTER_DK)
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.5, location=(x, y, 2.0)); c = bpy.context.object; c.scale = (0.55, 0.55, 2.6)
    c.data.materials.append(pbsdf('cypress', (0.03, 0.09, 0.04), 0.8, bump=0.6))

# ---------------- lanterns: pierced brass with glowing glass and a real warm light ----------------
for (x, y, z) in [(-2.4, 7.6, 3.3), (2.4, 7.6, 3.3), (0, 7.6, 3.5), (-5.8, 2.0, 3.3), (5.8, 2.0, 3.3), (-5.8, 5.0, 3.3), (5.8, 5.0, 3.3)]:
    bpy.ops.mesh.primitive_cylinder_add(radius=0.005, depth=5.0 - z, location=(x, y, z + (5.0 - z) / 2 + 0.35)); bpy.context.object.data.materials.append(BRASS)
    bpy.ops.mesh.primitive_cylinder_add(radius=0.2, depth=0.5, vertices=8, location=(x, y, z)); bpy.context.object.data.materials.append(PIERCED)
    bpy.ops.mesh.primitive_cone_add(radius1=0.24, radius2=0.03, depth=0.3, vertices=8, location=(x, y, z + 0.4)); bpy.context.object.data.materials.append(BRASS)
    bpy.ops.mesh.primitive_cone_add(radius1=0.03, radius2=0.24, depth=0.18, vertices=8, location=(x, y, z - 0.34)); bpy.context.object.data.materials.append(BRASS)
    for k in range(8):   # brass ribs
        a = k * math.pi / 4; bpy.ops.mesh.primitive_cylinder_add(radius=0.012, depth=0.52, location=(x + 0.205 * math.cos(a), y + 0.205 * math.sin(a), z)); bpy.context.object.data.materials.append(BRASS)
    L = bpy.data.objects.new('lamp', bpy.data.lights.new('lamp', 'POINT')); L.data.energy = 90; L.data.color = (1.0, 0.6, 0.28); L.data.shadow_soft_size = 0.15; L.location = (x, y, z); add(L)

# ---------------- the voice: a golden ribbon of light over the pool ----------------
cu = bpy.data.curves.new('voice', 'CURVE'); cu.dimensions = '3D'; cu.bevel_depth = 0.03; cu.bevel_resolution = 4
sp = cu.splines.new('NURBS'); N = 260; sp.points.add(N - 1)
for i in range(N):
    u = i / (N - 1); x = -3.6 + 7.2 * u; ph = u * 22
    z = 2.0 + 0.42 * math.sin(ph) + 0.16 * math.sin(2 * ph + 0.7) + 0.07 * math.sin(3 * ph + 1.3)
    y = 3.0 + 0.9 * math.sin(u * 3.1)
    sp.points[i].co = (x, y, z, 1)
sp.use_endpoint_u = True; sp.order_u = 4
rib = add(bpy.data.objects.new('voice', cu)); cu.materials.append(RIBBON)
# motes of light drifting
for k in range(60):
    bpy.ops.mesh.primitive_ico_sphere_add(radius=0.012, subdivisions=1, location=(rnd.uniform(-4, 4), rnd.uniform(-1, 8), rnd.uniform(0.5, 5)))
    bpy.context.object.data.materials.append(GLOWGLASS)

# ---------------- dusk sky, sun, haze ----------------
w = bpy.data.worlds.new('W'); sc.world = w; w.use_nodes = True; nt = w.node_tree
tc = nt.nodes.new('ShaderNodeTexCoord'); sx = nt.nodes.new('ShaderNodeSeparateXYZ'); rp = nt.nodes.new('ShaderNodeValToRGB'); cr = rp.color_ramp
cr.elements[0].position = 0.0; cr.elements[0].color = (1.0, 0.45, 0.18, 1)
cr.elements[1].position = 1.0; cr.elements[1].color = (0.04, 0.06, 0.22, 1)
e = cr.elements.new(0.3); e.color = (0.8, 0.3, 0.38, 1)
e = cr.elements.new(0.6); e.color = (0.25, 0.16, 0.5, 1)
nt.links.new(tc.outputs['Generated'], sx.inputs[0]); nt.links.new(sx.outputs['Z'], rp.inputs[0]); nt.links.new(rp.outputs[0], nt.nodes['Background'].inputs[0])
nt.nodes['Background'].inputs[1].default_value = 0.45
# stars on a far dome + a crescent moon
STAR = pbsdf('star', (1, 1, 1), 0.5, emit=(1.0, 0.95, 0.85), es=30)
for k in range(260):
    a = rnd.uniform(0, 2 * math.pi); el = math.radians(rnd.uniform(14, 85)); R = 90
    bpy.ops.mesh.primitive_ico_sphere_add(radius=rnd.uniform(0.05, 0.16), subdivisions=1, location=(R * math.cos(el) * math.cos(a), R * math.cos(el) * math.sin(a), R * math.sin(el)))
    bpy.context.object.data.materials.append(STAR)
MOON = pbsdf('moon', (1, 1, 1), 0.5, emit=(1.0, 0.92, 0.75), es=12)
bpy.ops.mesh.primitive_uv_sphere_add(radius=2.2, location=(-14, 70, 30)); bpy.context.object.data.materials.append(MOON)
bpy.ops.mesh.primitive_uv_sphere_add(radius=2.15, location=(-13.0, 68.5, 30.6)); bpy.context.object.data.materials.append(pbsdf('moonshadow', (0.01, 0.01, 0.03), 1.0))
vol = nt.nodes.new('ShaderNodeVolumeScatter'); vol.inputs['Density'].default_value = 0.012; vol.inputs['Anisotropy'].default_value = 0.55
# Eevee Next treats a world volume as infinite (it swallows the sky), so the haze lives in a box around the courtyard instead
hz = box('haze', (0, 3, 4), (13, 13, 8), bpy.data.materials.new('hazemat'))
hm = hz.data.materials[0]; hm.use_nodes = True; hn = hm.node_tree; hn.nodes.remove(hn.nodes['Principled BSDF'])
vs = hn.nodes.new('ShaderNodeVolumeScatter'); vs.inputs['Density'].default_value = 0.035; vs.inputs['Anisotropy'].default_value = 0.5
hn.links.new(vs.outputs[0], hn.nodes['Material Output'].inputs['Volume'])
sun = add(bpy.data.objects.new('sun', bpy.data.lights.new('sun', 'SUN'))); sun.data.energy = 2.2; sun.data.color = (1.0, 0.55, 0.3); sun.data.angle = math.radians(1.5)
sun.rotation_euler = (math.radians(84), 0, math.radians(-118))

# ---------------- camera ----------------
cam = add(bpy.data.objects.new('cam', bpy.data.cameras.new('cam'))); sc.camera = cam
def aim(loc, tgt, lens):
    cam.location = loc; cam.rotation_euler = (Vector(tgt) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler(); cam.data.lens = lens
if SHOT == 'pool': aim((0.3, -4.2, 1.05), (0, 6, 2.4), 22 if ORIENT == 'portrait' else 24)
elif SHOT == 'lantern': aim((3.6, 4.4, 2.8), (5.8, 5.0, 3.3), 45); cam.data.dof.use_dof = True; cam.data.dof.focus_distance = 2.3; cam.data.dof.aperture_fstop = 1.8
elif SHOT == 'sky': aim((1.5, -3.0, 0.6), (-2.5, 8, 5.5), 20)
elif SHOT == 'high': aim((-4.5, -3.5, 6.5), (0.5, 4.5, 0.5), 22)
cam.data.clip_end = 200
sc.render.resolution_percentage = RESP
import time
for k in range(NFR):
    cam.location.x += 0.03
    sc.render.filepath = OUT.replace('.png', '_%d.png' % k); t0 = time.time(); bpy.ops.render.render(write_still=True)
    print('BENCHTIME', ENGINE, ORIENT, SAMPLES, RESP, k, round(time.time() - t0, 2), flush=True)
print('STYLE_DONE', OUT)
