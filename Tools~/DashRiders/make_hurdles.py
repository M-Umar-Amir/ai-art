"""Authored manufactured street hurdles, in metres; no external assets needed."""
import os
ROOT=os.path.dirname(os.path.abspath(__file__))
source=open(os.path.join(ROOT,'make_city.py')).read()
exec(source[:source.index('for variant,height')])
orange=mat('Safety tangerine',(1,.22,.035),.15)
for x in [-.78,.78]:
    box('Rubber stabilizer',(x,0,.055),(.38,.64,.11),shell,.04)
    box('Titanium upright',(x,0,.46),(.09,.12,.82),metal,.025)
box('Rounded impact shell',(0,0,.69),(1.9,.22,.44),orange,.06)
for x in [-.72,-.36,0,.36,.72]:
    o=box('Reflective chevron',(x,-.116,.69),(.13,.012,.35),light,.01)
    o.rotation_euler.y=-.42
for x in [-.8,.8]:
    cylinder('Warning beacon',(x,0,.93),(x,0,1.02),.075,cyan,16)
export('neon-hurdle')
for x in [-.88,.88]:
    box('Weighted footing',(x,0,.09),(.38,.72,.18),shell,.05)
    box('Gate support',(x,0,1.15),(.15,.24,2.2),metal,.04)
    box('Illuminated support',(x,-.13,1.25),(.06,.025,1.8),pink,.01)
box('Overhead cargo truss',(0,0,2.15),(1.95,.8,.34),shell,.06)
box('Slide warning strip',(0,-.415,2.12),(1.7,.035,.12),orange,.02)
for x in [-.6,-.3,0,.3,.6]:
    box('Truss vent',(x,-.425,2.28),(.13,.025,.09),metal,.01)
export('cargo-slide-gate')
