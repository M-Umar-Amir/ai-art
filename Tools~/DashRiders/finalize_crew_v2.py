"""Preserve the new native UV atlases and reduce only geometry for the playable asset budget."""
import bpy, os, shutil
from mathutils import Vector
ROOT=os.path.dirname(os.path.abspath(__file__))
OUT=os.path.join(ROOT,'crew-v2','optimized');os.makedirs(OUT,exist_ok=True)
for name in ['ace','nova','bolt']:
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.gltf(filepath=os.path.join(ROOT,'crew-v2','generated','out',name+'.glb'))
    for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
        bpy.context.view_layer.objects.active=obj
        if name=='bolt':
            # The reconstruction painted small mechanical side covers as extra green footwear.
            # Reassign those native mesh faces to graphite; retain the two real outer boots and wheel.
            graphite=bpy.data.materials.new('Mechanical side stabilizers');graphite.diffuse_color=(.055,.075,.105,1)
            graphite.use_nodes=True;bs=graphite.node_tree.nodes.get('Principled BSDF')
            bs.inputs['Base Color'].default_value=graphite.diffuse_color;bs.inputs['Metallic'].default_value=.25
            obj.data.materials.append(graphite);index=len(obj.data.materials)-1;changed=0
            for face in obj.data.polygons:
                p=obj.matrix_world@face.center
                x,y,z=p.x,p.z,-p.y
                if -.57<y<-.28 and -.12<z<.085 and .075<abs(x)<.29:
                    face.material_index=index;changed+=1
            print('Bolt stabilizer faces',changed,flush=True)
        modifier=obj.modifiers.new('Mobile geometry budget','DECIMATE');modifier.ratio=.55 if name!='bolt' else .65
        modifier.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=modifier.name)
        for face in obj.data.polygons:face.use_smooth=True
        for material in obj.data.materials:
            if material and material.use_nodes:
                bs=material.node_tree.nodes.get('Principled BSDF')
                if bs:bs.inputs['Metallic'].default_value=.05 if name=='bolt' else 0;bs.inputs['Roughness'].default_value=.65
        print(name,len(obj.data.polygons),'faces',flush=True)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,name+'.glb'),export_format='GLB',use_selection=True)
print('CREW V2 OPTIMIZED',flush=True)
