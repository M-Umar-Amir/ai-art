"""Playable roof architecture: occupied rooms, windows, terrace garden and mechanical plant."""
import os
ROOT=os.path.dirname(os.path.abspath(__file__))
source=open(os.path.join(ROOT,'make_city.py')).read()
exec(source[:source.index('for variant,height')])
box('Apartment structure',(0,0,1.5),(4,14,3),shell,.08)
box('Roof cornice',(0,0,3.13),(4.18,14.18,.26),light,.035)
box('Walkable roof',(0,0,3.33),(4,14,.14),metal,.02)
for side in [-1,1]:
    for y in [-5.5,-3.3,-1.1,1.1,3.3,5.5]:
        box('Recessed occupied window',(side*2.01,y,1.7),(.035,1.7,1.35),glass,.01)
        box('Warm interior strip',(side*2.035,y,1.22),(.02,1.55,.1),room)
        box('Facade mullion',(side*2.045,y-.85,1.7),(.06,.07,1.65),metal,.01)
        box('Window sill',(side*2.08,y,1.02),(.22,1.9,.12),light,.02)
    box('Roof safety parapet',(side*1.96,0,3.64),(.12,14,.48),shell,.025)
    box('Neon coping',(side*1.96,0,3.9),(.15,14,.05),cyan,.01)
    for y in [-4.7,4.7]:
        box('Terrace planter',(side*1.68,y,3.57),(.4,2.1,.34),light,.03)
        for yy in [-.6,0,.6]:
            bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=.32,location=(side*1.68,y+yy,3.9))
            o=bpy.context.object;o.scale=(.6,1,.8);o.data.materials.append(green);objects.append(o)
box('Ventilation housing',(1.5,0,3.7),(.55,1.6,.6),metal,.07)
for y in [-.5,-.25,0,.25,.5]:box('Vent grille',(1.5,y,4.01),(.42,.08,.015),shell)
for y in [-5,0,5]:box('Runway inlay',(0,y,3.405),(.65,1.8,.015),cyan,.01)
export('rooftop-apartment')
