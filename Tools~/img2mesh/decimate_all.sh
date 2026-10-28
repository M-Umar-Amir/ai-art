# Game-ready LODs: projected/v4 + officer -> game/, triangle budget by on-screen size.
export PYTHONPATH=D:/ai-art/tools/pylib-mesh
OFF="C:/Users/aa/AppData/Local/Temp/claude/D--ai-art-ai-art/e10e1d11-6d9b-4bf5-aa75-8118aa5d7248/scratchpad/kaggle/out-officer/out"
run() { timeout 900 python decimate.py "$1" "game/$2.glb" $3 $4 2>&1 | tail -1; }
for n in wagyu-bacon-burger presents cone; do run projected/v4/$n.glb $n 3000 1024; done
for n in roadworks-barrier tree-planter shopper; do run projected/v4/$n.glb $n 5000 1024; done
for n in bicycle police-bicycle scooter fizz-11 finn faye; do run projected/v4/$n.glb $n 9000 2048; done
for n in finn-ride faye-ride; do run projected/v4/$n.glb $n 12000 2048; done
for n in officer-riding officer-standing; do run "$OFF/$n.glb" $n 10000 2048; done
for n in reunion-tower margaret-hunt-hill-bridge bank-of-america-plaza fountain-place omni-dallas; do run projected/v4/$n.glb $n 12000 2048; done
echo ALL_DONE
