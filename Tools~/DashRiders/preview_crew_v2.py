from pathlib import Path
root = Path(__file__).parent
source = (root / 'preview_models.py').read_text()
source = source.replace("ROOT=os.path.dirname(os.path.abspath(__file__))", "ROOT=" + repr(str(root / 'crew-v2' / 'generated' / 'out')))
source = source.replace("['ace','nova']", "['ace','nova','bolt']")
source = source.replace('scene.cycles.samples=20', 'scene.cycles.samples=12')
source = source.replace('resolution_x=640', 'resolution_x=512').replace('resolution_y=800', 'resolution_y=640')
exec(compile(source, str(root / 'preview_models.py'), 'exec'))
