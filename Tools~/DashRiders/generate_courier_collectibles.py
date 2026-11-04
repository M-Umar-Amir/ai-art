"""Free Blender mesh generation for PDF-specific courier items; no paid APIs or assets."""
import bpy
import math
import pathlib
from mathutils import Vector

OUT = pathlib.Path(__file__).parent / 'free-curated'
OUT.mkdir(exist_ok=True)

def material(name, color, metal=0.0, emission=0.0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Metallic'].default_value = metal
    bs.inputs['Roughness'].default_value = .32
    bs.inputs['Emission Color'].default_value = (*color, 1)
    bs.inputs['Emission Strength'].default_value = emission
    return m

dark = material('Graphite courier housing', (.035, .05, .085), .35)
silver = material('Titanium silver', (.55, .66, .75), .65)
cyan = material('Neon cyan', (.015, .75, 1), .2, 1.5)
pink = material('Neon pink', (1, .025, .3), .2, 1.4)
purple = material('Neon purple', (.48, .06, .95), .2, 1.2)
gold = material('Energy gold', (1, .55, .025), .5)
white = material('Porcelain white', (.85, .92, 1), .15)

def finish(obj, mat, bevel=.025):
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new('Manufactured rounded edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 3
        obj.modifiers.new('Weighted face normals', 'WEIGHTED_NORMAL')
    return obj

def box(name, at, size, mat, bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=at)
    obj = bpy.context.object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, mat, bevel)

def cylinder(name, a, b, radius, mat):
    delta = Vector(b) - Vector(a)
    bpy.ops.mesh.primitive_cylinder_add(vertices=32, radius=radius, depth=delta.length,
                                      location=(Vector(a) + Vector(b)) / 2)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler = delta.to_track_quat('Z', 'Y').to_euler()
    return finish(obj, mat, .012)

def reset():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

def export(name):
    bpy.ops.object.select_all(action='SELECT')
    for obj in list(bpy.context.selected_objects):
        bpy.context.view_layer.objects.active = obj
        for modifier in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.context.view_layer.objects.active = bpy.context.selected_objects[0]
    bpy.ops.object.join()
    bpy.context.object.name = name
    # Unity's model wrapper applies its own facing rotation. Bake the source object's
    # transform so a cylinder chosen as the join anchor cannot turn a chip flat.
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.export_scene.gltf(filepath=str(OUT / ('free-' + name + '.glb')),
                             export_format='GLB', export_apply=True)
    print('GENERATED', name, flush=True)

reset()
# A real U-shaped magnetic yoke, coils, pole caps and wearable pack housing.
box('Magnetic field backpack', (0, .1, .35), (.56, .2, .66), dark, .07)
box('Yoke bridge', (0, -.09, .18), (.64, .22, .16), silver, .06)
for side in (-1, 1):
    box('Magnetic pole', (side * .25, -.09, .4), (.15, .22, .5), pink, .055)
    box('Pole cap', (side * .25, -.09, .67), (.16, .23, .11), white)
    for z in (.24, .31, .38):
        box('Induction winding', (side * .25, -.22, z), (.18, .035, .035), cyan, .009)
export('magnet-pack')

reset()
# Paired high-top gravity boots with separate soles, cuffs, laces and magnetic heel coils.
for side in (-1, 1):
    x = side * .22
    box('Gravity boot sole', (x, -.1, .07), (.33, .64, .14), dark, .065)
    box('Reinforced toe', (x, -.23, .2), (.31, .36, .23), silver, .08)
    box('Boot ankle', (x, .06, .35), (.27, .29, .55), purple, .055)
    box('Ankle cuff', (x, .06, .6), (.31, .32, .1), dark)
    for z in (.24, .32, .4):
        box('Boot fastening', (x, -.105, z), (.22, .04, .035), white, .01)
    cylinder('Heel graviton ring', (x, .21, .19), (x, .255, .19), .105, cyan)
    box('Magnetic sole rail', (x, -.1, .1), (.35, .52, .025), cyan, .01)
export('gravity-boots')

reset()
cylinder('Speed core housing', (0, 0, .08), (0, 0, .68), .22, dark)
cylinder('Accelerator plasma', (0, 0, .13), (0, 0, .62), .15, cyan)
for z in (.09, .2, .5, .66):
    cylinder('Titanium containment ring', (0, 0, z - .025), (0, 0, z + .025), .255, silver)
for angle in range(0, 360, 90):
    a = math.radians(angle)
    box('Core cooling fin', (.21 * math.cos(a), .21 * math.sin(a), .38), (.065, .065, .38), gold)
export('speed-core')

reset()
box('Recharge battery', (0, 0, .33), (.45, .24, .62), dark, .065)
box('Battery ceramic face', (0, -.135, .33), (.36, .04, .5), white)
for z in (.19, .3, .41):
    box('Charge level cell', (0, -.165, z), (.27, .025, .065), cyan, .01)
for x in (-.12, .12):
    box('Battery terminal', (x, 0, .69), (.1, .13, .12), gold)
export('dash-battery')

reset()
box('Shield drone body', (0, 0, .22), (.34, .45, .26), silver, .08)
box('Protective sensor eye', (0, -.24, .23), (.2, .04, .08), cyan)
for x in (-.32, .32):
    for y in (-.27, .27):
        box('Drone arm', (x / 2, y / 2, .25), (.35, .07, .055), dark)
        cylinder('Ducted shield rotor', (x, y, .15), (x, y, .27), .14, dark)
        cylinder('Rotor ion ring', (x, y, .27), (x, y, .29), .115, pink)
        box('Rotor blade', (x, y, .31), (.23, .035, .018), silver, .006)
export('shield-drone')

reset()
cylinder('Energy chip edge', (0, -.04, .3), (0, .04, .3), .29, gold)
cylinder('Chip inset', (0, -.052, .3), (0, -.045, .3), .225, dark)
box('Microprocessor core', (0, -.07, .3), (.22, .035, .22), cyan)
for side in (-1, 1):
    for v in (-.075, 0, .075):
        box('Processor pin', (side * .15, -.07, .3 + v), (.06, .02, .018), silver, .004)
export('courier-chip')

reset()
for x in (-.19, .19):
    box('Multiplier energy cell', (x, 0, .32), (.27, .25, .58), gold, .055)
    for z in (.17, .31, .45):
        box('Score light', (x, -.14, z), (.18, .025, .05), cyan)
box('Paired cell bridge', (0, 0, .28), (.2, .2, .12), dark)
export('double-core')

reset()
box('Ventilation chassis', (0, 0, .33), (1.35, 1.5, .62), silver, .055)
box('Vibration isolation plinth', (0, 0, .04), (1.3, 1.45, .08), dark)
for y in (-.38, .38):
    cylinder('Fan enclosure', (0, y, .64), (0, y, .73), .29, dark)
    cylinder('Fan spindle', (0, y, .735), (0, y, .74), .06, cyan)
    for angle in range(0, 180, 30):
        bar = box('Fan safety grille', (0, y, .735), (.55, .018, .012), silver, .004)
        bar.rotation_euler.z = math.radians(angle)
for z in (.15, .24, .33, .42, .51):
    box('Intake louvre', (0, -.76, z), (1.12, .025, .035), dark, .009)
box('Service access panel', (.68, 0, .34), (.02, .56, .4), dark)
box('Status light', (.695, -.15, .4), (.015, .1, .04), cyan, .004)
export('roof-vent')

reset()
for x in (-.6, .6):
    for y in (-.6, .6):
        box('Tank support leg', (x, y, .55), (.12, .12, 1.1), dark)
cylinder('Water tank vessel', (0, 0, 1), (0, 0, 2.9), .85, silver)
for z in (1.04, 1.9, 2.86):
    cylinder('Tank reinforcing band', (0, 0, z - .035), (0, 0, z + .035), .88, dark)
cylinder('Tank lid', (0, 0, 2.9), (0, 0, 3), .9, dark)
cylinder('Service hatch', (0, 0, 3), (0, 0, 3.12), .23, cyan)
for z in (.25, .55, .85, 1.15, 1.45, 1.75, 2.05, 2.35):
    box('Service ladder rung', (0, -.94, z), (.4, .045, .035), silver, .007)
for x in (-.22, .22):
    cylinder('Service ladder rail', (x, -.94, .15), (x, -.94, 2.6), .025, dark)
export('roof-tank')

reset()
box('Festival stage deck', (0, 0, .24), (6, 4, .48), dark, .1)
box('Stage edge light', (0, -2.01, .38), (5.8, .05, .06), pink)
for x in (-2.65, 2.65):
    for y in (-1.5, 1.5):
        box('Truss upright', (x, y, 1.9), (.13, .13, 3.3), silver)
    box('Speaker enclosure', (x, .5, 1.1), (.6, .6, 1.3), dark, .06)
    for z in (.8, 1.35):
        cylinder('Speaker driver', (x, .18, z), (x, .15, z), .21, silver)
box('Stage canopy truss', (0, 1.5, 3.5), (5.4, .16, .22), silver)
box('Performance LED backdrop', (0, 1.55, 2), (4.6, .12, 2.15), dark)
for x in (-1.8, -1.2, -.6, 0, .6, 1.2, 1.8):
    box('LED equalizer strip', (x, 1.46, 1.75), (.16, .035, 1 + .4 * math.cos(x * 3)), cyan)
    cylinder('Stage spotlight', (x, 1.3, 3.4), (x, 1.02, 3.25), .11, pink)
export('festival-stage')
