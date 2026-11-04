"""A forty-metre occupied high-rise whose roof is the playable parkour surface."""
import os
ROOT=os.path.dirname(os.path.abspath(__file__))
source=open(os.path.join(ROOT,'make_city.py')).read()
exec(source[:source.index('for variant,height')])
box('Forty metre occupied tower',(0,0,19.85),(8,20,39.7),shell,.05)
box('Roof cornice',(0,0,39.72),(8.2,20.1,.24),metal,.03)
slate=mat('Roof slate',(.14,.22,.27))
panel=mat('Roof panel',(.18,.27,.31))
box('Playable roof',(0,0,39.93),(8.15,20,.14),slate,.02)
for x in [-2.7,0,2.7]:
    for y in [-8,-4,0,4,8]:
        box('Inset roof panel',(x,y,40.005),(2.56,3.84,.012),panel,.012)
for side in [-1,1]:
    for h in range(2,39,3):
        for y in [-8.25,-4.95,-1.65,1.65,4.95,8.25]:
            box('Occupied side glazing',(side*4.015,y,h),(.025,2.55,2.1),glass)
            if (round(y*10)+h)%4==0:box('Warm office light',(side*4.032,y,h-.7),(.012,2.35,.08),room)
        for x in [-2.65,0,2.65]:
            box('End glazing',(x,side*10.015,h),(2.05,.025,2.1),glass)
    for y in [-9.8,-6.6,-3.3,0,3.3,6.6,9.8]:
        box('Facade structural rib',(side*4.045,y,20),(.085,.09,39.5),metal)
    for h in [8,20,32]:
        box('Architectural neon band',(side*4.065,0,h),(.025,19.8,.035),cyan)
export('skypark-tower')
