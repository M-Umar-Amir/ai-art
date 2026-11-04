"""Render actual generated meshes for review, without invoking an image generator."""
import bpy
import pathlib
from mathutils import Vector

ROOT = pathlib.Path(__file__).parent
OUT = ROOT.parent / 'review/free-assets'
OUT.mkdir(parents=True, exist_ok=True)
for name in ('magnet-pack', 'gravity-boots', 'speed-core', 'dash-battery', 'shield-drone', 'courier-chip', 'double-core', 'roof-vent', 'roof-tank', 'festival-stage'):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=str(ROOT / 'free-curated' / ('free-' + name + '.glb')))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ Vector(v) for o in meshes for v in o.bound_box]
    low = Vector([min(v[i] for v in points) for i in range(3)])
    high = Vector([max(v[i] for v in points) for i in range(3)])
    center = (low + high) / 2
    extent = max(high - low)
    bpy.ops.object.camera_add(location=center + Vector((1.6, -3, 1.7)) * extent)
    cam = bpy.context.object
    cam.rotation_euler = (center - cam.location).to_track_quat('-Z', 'Y').to_euler()
    cam.data.type = 'ORTHO'
    cam.data.ortho_scale = extent * 1.4
    scene = bpy.context.scene
    scene.camera = cam
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_shadows = True
    scene.display.shading.show_cavity = True
    scene.display.shading.background_type = 'WORLD'
    scene.world.color = (.025, .035, .06)
    scene.render.resolution_x = 480
    scene.render.resolution_y = 480
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(OUT / (name + '.png'))
    bpy.ops.render.render(write_still=True)
