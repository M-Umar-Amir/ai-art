"""Free Blender architecture kit: layered facades, roof cafes, gardens and suspended walkways."""
import bpy, math, os, random
from mathutils import Vector
OUT=os.path.dirname(os.path.abspath(__file__))
random.seed(17)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def mat(name,color,metal=0,glow=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*color,1);b.inputs['Metallic'].default_value=metal;b.inputs['Roughness'].default_value=.3 if metal else .7
    b.inputs['Emission Color'].default_value=(*color,1);b.inputs['Emission Strength'].default_value=glow;return m
shell=mat('Architectural graphite',(.19,.24,.32),.25)
metal=mat('Titanium facade',(.38,.46,.55),.45)
light=mat('Pale stone',(.68,.72,.72))
glass=mat('Blue tinted glazing',(.08,.23,.32),.5)
room=mat('Warm interior',(.75,.41,.16),0,.22)
cyan=mat('Neon cyan',(.05,.73,.9),0,2)
pink=mat('Neon pink',(.95,.04,.42),0,2)
green=mat('Garden leaves',(.15,.42,.17))
wood=mat('Cafe teak',(.45,.25,.12))
cream=mat('Terrace awnings',(.84,.63,.38))
objects=[]
def box(name,at,size,m,bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1,location=at);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Manufactured edge','BEVEL');mod.width=bevel;mod.segments=2;bpy.ops.object.modifier_apply(modifier=mod.name)
    o.data.materials.append(m);objects.append(o);return o
def cylinder(name,a,b,r,m,vertices=8):
    a,b=Vector(a),Vector(b);bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=(b-a).length,location=(a+b)*.5)
    o=bpy.context.object;o.name=name;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();o.data.materials.append(m);objects.append(o);return o
def tree(x,y,z,scale=1):
    box('Roof planter',(x,y,z+.25),(1.5*scale,1.5*scale,.5),light,.08)
    cylinder('Tree trunk',(x,y,z+.5),(x,y,z+2.4*scale),.12*scale,wood)
    for dx,dy,dz,s in [(-.4,0,2.4,.85),(.4,.2,2.8,1),(0,-.3,3.2,.8)]:
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=s*scale,location=(x+dx*scale,y+dy*scale,z+dz*scale));o=bpy.context.object;o.name='Sculpted foliage';o.scale=(1,.8,.7);o.data.materials.append(green);objects.append(o)
def railing(x,y,z,length,alongx=True):
    dx,dy=(length/2,0) if alongx else (0,length/2)
    cylinder('Handrail',(x-dx,y-dy,z+1.1),(x+dx,y+dy,z+1.1),.045,metal)
    for p in range(int(length/1.6)+1):
        f=p/max(1,int(length/1.6))-.5;px=x+length*f if alongx else x;py=y if alongx else y+length*f
        cylinder('Balustrade',(px,py,z),(px,py,z+1.1),.035,metal)
    box('Glass balustrade',(x,y,z+.55),(length,.035,.8) if alongx else (.035,length,.8),glass)
def cafe(z):
    box('Cantilever cafe deck',(0,-8.4,z),(16,5,.4),light,.12)
    box('Deck underside',(0,-8.1,z-.35),(13,4,.3),shell,.06)
    railing(0,-10.8,z+.2,15.5)
    for x in [-5,0,5]:
        cylinder('Table pedestal',(x,-8.5,z+.2),(x,-8.5,z+1.02),.08,metal)
        bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=.8,depth=.1,location=(x,-8.5,z+1.04));o=bpy.context.object;o.name='Cafe table';o.data.materials.append(wood);objects.append(o)
        for dx in [-1.2,1.2]:
            box('Chair seat',(x+dx,-8.5,z+.65),(.6,.65,.12),wood,.04)
            box('Chair back',(x+dx,-8.8,z+1),(.6,.12,.65),wood,.04)
            for a in [-.22,.22]:cylinder('Chair leg',(x+dx+a,-8.5,z+.2),(x+dx+a,-8.5,z+.62),.035,metal)
        cylinder('Umbrella mast',(x,-8.5,z+.2),(x,-8.5,z+3.1),.045,metal)
        bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=1.85,radius2=.12,depth=.65,location=(x,-8.5,z+3));o=bpy.context.object;o.name='Cafe parasol';o.data.materials.append(cream);objects.append(o)
    for x in [-7.2,7.2]:tree(x,-8.3,z+.2,.7)
def export(name):
    # Merge by material; architectural repetition should cost a few batches, not hundreds.
    groups={}
    for o in objects:
        if o.type=='MESH':groups.setdefault(o.data.materials[0].name,[]).append(o)
    for group in groups.values():
        bpy.ops.object.select_all(action='DESELECT')
        for o in group:o.select_set(True)
        bpy.context.view_layer.objects.active=group[0];bpy.ops.object.join()
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,name+'.glb'),export_format='GLB',use_selection=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,name+'.blend'))
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);objects.clear()
for variant,height in [('neon-block-a',30),('neon-block-b',42)]:
    box('Stepped tower',(0,0,height/2),(12,15,height),shell,.18)
    box('Tower crown',(.8,1,height+2.5),(9,11,5),metal,.2)
    box('Roof coping',(0,0,height+.12),(12.5,15.5,.24),light,.06)
    for level in range(int((height-4)/3.3)):
        z=4.5+level*3.3
        box('Horizontal facade ledge',(0,-7.65,z-1.25),(12.7,.5,.18),metal,.035)
        for col in range(6):
            x=-5+col*2
            box('Deep framed window',(x,-7.62,z),(1.74,.2,2.05),metal)
            box('Recessed glazing',(x,-7.76,z),(1.5,.08,1.81),room if (col+level)%7==0 else glass)
            box('Glazing mullion',(x,-7.82,z),(.035,.025,1.81),shell)
        for side in [-1,1]:
            for row in range(5):box('Side ribbon glazing',(side*6.04,-5.8+row*2.8,z),(.06,2.4,1.85),glass)
        if level%3==0:box('Architectural light ribbon',(0,-7.87,z-1.1),(12.3,.035,.055),cyan if level%2 else pink)
    for x in [-5.8,5.8]:box('Facade spine',(x,-7.9,height/2),(.22,.35,height),metal,.035)
    # Ground floor food-market frontage with real frames, doors and canopies.
    for x in [-4,0,4]:
        box('Shopfront frame',(x,-7.68,1.55),(3.5,.3,3.1),metal,.04)
        box('Shopfront glass',(x,-7.87,1.55),(3.15,.06,2.75),glass)
        for dx in [-1.15,0,1.15]:box('Shop mullion',(x+dx,-7.92,1.55),(.06,.06,2.8),light)
        box('Food counter',(x,-8.1,.95),(3.1,.55,.15),wood,.04)
        box('Shop awning',(x,-8.2,3.15),(3.7,1.5,.18),pink if x==0 else cyan,.04)
        box('Shop fascia',(x,-7.92,3.65),(3.4,.18,.65),shell,.03)
    cafe(7.8 if variant.endswith('a') else 14.4)
    for x,y in [(-4,-5),(4,-4),(-4,5),(4,5)]:tree(x,y,height+.25,.9)
    # Roof utilities and communication mast complete the skyline silhouette.
    for x in [-2,2]:box('Air handling unit',(x,2,height+.75),(2,2,1.1),metal,.1)
    cylinder('Antenna',(2,3,height+4),(2,3,height+10),.055,metal)
    export(variant)
# A proper suspended bridge with cables, balustrades and garden ends.
box('Bridge deck',(0,0,0),(18,3,.35),metal,.08)
box('Inset walkway',(0,0,.2),(18,2.6,.06),light)
for y in [-1.45,1.45]:
    railing(0,y,.2,18)
    for x in [-8,8]:cylinder('Suspension mast',(x,y,0),(x,y,5),.09,metal)
    points=[(-8,y,5),(-4,y,2.6),(0,y,1.8),(4,y,2.6),(8,y,5)]
    for a,b in zip(points,points[1:]):cylinder('Suspension cable',a,b,.035,metal)
    for x in [-6,-3,0,3,6]:cylinder('Suspension tie',(x,y,.2),(x,y,1.8+3.2*(abs(x)/8)**2),.018,metal)
    box('Bridge edge light',(0,y,.14),(18,.045,.06),cyan)
export('skybridge')
print('LAYERED CITY KIT COMPLETE',flush=True)
