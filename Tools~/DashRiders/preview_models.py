import bpy, os, math
from mathutils import Vector
ROOT=os.path.dirname(os.path.abspath(__file__))
for name in ['ace','nova']:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    path=os.path.join(ROOT,name+'.glb')
    if not os.path.exists(path):path=os.path.join(ROOT,'generated','out',name+'.glb')
    bpy.ops.import_scene.gltf(filepath=path)
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    coords=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
    lo=Vector(tuple(min(v[i] for v in coords) for i in range(3)));hi=Vector(tuple(max(v[i] for v in coords) for i in range(3)))
    center=(lo+hi)*.5;height=hi.z-lo.z
    for o in meshes:
        for m in o.data.materials:
            if m and m.use_nodes:
                bs=m.node_tree.nodes.get('Principled BSDF')
                if bs:bs.inputs['Metallic'].default_value=0;bs.inputs['Roughness'].default_value=.85
    world=bpy.context.scene.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs['Color'].default_value=(.35,.4,.5,1);world.node_tree.nodes['Background'].inputs['Strength'].default_value=.7
    for at,power,size in [((-2,-3,4),300,4),((3,0,2),150,3),((0,3,3),250,3)]:
        bpy.ops.object.light_add(type='AREA',location=center+Vector(at)*height/2);o=bpy.context.object;o.data.energy=power;o.data.size=size*height/2;o.rotation_euler=(center-o.location).to_track_quat('-Z','Y').to_euler()
    bpy.ops.object.camera_add(location=center+Vector((.25,-2.6,.12))*height);cam=bpy.context.object;cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=height*1.22
    scene=bpy.context.scene;scene.camera=cam;scene.render.engine='CYCLES';scene.cycles.samples=20;scene.render.threads_mode='FIXED';scene.render.threads=2
    scene.render.resolution_x=640;scene.render.resolution_y=800;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.film_transparent=True
    scene.render.filepath=os.path.join(ROOT,name+'-preview.png');bpy.ops.render.render(write_still=True)
    print('PREVIEW',name,flush=True)
