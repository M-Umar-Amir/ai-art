import os
ROOT=os.path.dirname(os.path.abspath(__file__))
source=open(os.path.join(ROOT,'make_city.py')).read()
exec(source[:source.index('for variant,height')])
for name,accent in [('electric-board',cyan),('glide-board',pink)]:
    box('Rounded carbon deck',(0,0,.09),(.7,1.5,.18),shell,.14)
    box('Inset grip',(0,0,.19),(.52,1.12,.025),glass,.08)
    for side in [-1,1]:
        box('Illuminated edge',(side*.335,0,.08),(.035,1.12,.065),accent,.015)
        for y in [-.5,.5]:
            cylinder('Magnetic wheel',(side*.34,y,.015),(side*.43,y,.015),.16,metal,16)
            cylinder('Wheel light',(side*.425,y,.015),(side*.445,y,.015),.1,accent,16)
    for y in [-.42,.42]:
        cylinder('Hover turbine',(0,y,-.04),(0,y,-.11),.18,shell,16)
        cylinder('Ion exhaust',(0,y,-.105),(0,y,-.12),.12,accent,16)
    for y in [-.22,0,.22]:box('Grip crossbar',(0,y,.21),(.4,.025,.01),light)
    export(name)
box('Autonomous pod chassis',(0,0,.7),(1.6,3.5,.7),metal,.26)
box('Passenger shell',(0,.1,1.25),(1.45,2.65,.9),light,.3)
box('Panoramic front glass',(0,-1.05,1.37),(1.26,.18,.54),glass,.11)
box('Rear glass',(0,1.38,1.37),(1.26,.12,.45),glass,.1)
for side in [-1,1]:
    box('Side glazing',(side*.735,0,1.45),(.06,1.8,.42),glass,.025)
    box('Door seam',(side*.772,.1,.95),(.02,2.05,.025),shell)
    box('Cyan sill',(side*.79,0,.64),(.035,2.8,.055),cyan,.015)
    for y in [-1.08,1.08]:
        cylinder('Wheel tyre',(side*.69,y,.36),(side*.88,y,.36),.36,shell,24)
        cylinder('Alloy wheel',(side*.88,y,.36),(side*.91,y,.36),.23,metal,16)
        cylinder('Wheel LEDs',(side*.911,y,.36),(side*.923,y,.36),.13,cyan,16)
for y in [-1.76,1.76]:box('Light bar',(0,y,.86),(1.2,.045,.075),cyan if y<0 else pink,.015)
box('Roof delivery cargo',(0,.2,1.91),(.95,1.2,.45),shell,.09)
box('Delivery cargo lid',(0,.2,2.16),(1,1.25,.08),pink,.035)
cylinder('Autonomy sensor',(0,-.55,1.86),(0,-.55,2.05),.1,metal,16)
export('electric-pod')
print('FUTURE EQUIPMENT COMPLETE',flush=True)
