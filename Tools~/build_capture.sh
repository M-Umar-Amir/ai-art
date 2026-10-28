# usage: build_capture.sh <label>  -> bootstrap, Windows build, autopilot screenshots
P=G:/ai-art/dallas-dash-unity; U="/g/Unity/Editors/6000.3.27f1/Editor/Unity.exe"; L=$1
"$U" -batchmode -nographics -quit -projectPath $P -executeMethod DallasDash.EditorTools.ProjectBootstrap.Run -logFile $P/Logs/bootstrap-$L.log || { echo BOOTSTRAP_FAIL; grep -E "error CS" $P/Logs/bootstrap-$L.log | sort -u | head; exit 1; }
"$U" -batchmode -nographics -projectPath $P -executeMethod DallasDash.EditorTools.BuildTools.Windows -logFile $P/Logs/build-$L.log || { echo BUILD_FAIL; grep -E "error|Error" $P/Logs/build-$L.log | sort -u | head -20; exit 1; }
rm -rf $P/Screenshots/$L; mkdir -p $P/Screenshots/$L
timeout 120 $P/Builds/Windows/FoodIncGame.exe -screen-width 540 -screen-height 960 -screen-fullscreen 0 -tier 2 -capture $P/Screenshots/$L -logFile $P/Logs/player-$L.log
ls $P/Screenshots/$L; grep -iE "exception|error" $P/Logs/player-$L.log | sort -u | head
if [ "$TIERS" = 1 ]; then for t in 0 1; do timeout 120 $P/Builds/Windows/FoodIncGame.exe -screen-width 540 -screen-height 960 -screen-fullscreen 0 -tier $t -capture $P/Screenshots/$L/tier$t -logFile $P/Logs/player-$L-t$t.log; done; fi
cat $P/Screenshots/$L/fps.txt $P/Screenshots/$L/tier*/fps.txt 2>/dev/null
echo CAPTURE_DONE
