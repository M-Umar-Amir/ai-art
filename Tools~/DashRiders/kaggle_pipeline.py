"""Scoped free-GPU workflow. Private dataset/kernel; never prints account credentials."""
import argparse, json, os, pathlib, shutil, sys, time
sys.path.insert(0, 'D:/ai-art/tools/pylib-kaggle')
import requests
old_send=requests.Session.send
def send(self, request, **kwargs):
    kwargs['timeout']=(20,180)
    return old_send(self, request, **kwargs)
requests.Session.send=send
from kaggle.api.kaggle_api_extended import KaggleApi
ROOT=pathlib.Path(__file__).resolve().parent
USER='muhammadumaramir'
DATA=USER+'/dash-riders-concept-sheets'
KERNEL=USER+'/dash-riders-hunyuan3d'
def retry(fn):
    for attempt in range(3):
        try:return fn()
        except Exception as e:
            print('Attempt',attempt+1,type(e).__name__,str(e)[:180],flush=True)
            response=getattr(e,'response',None)
            if response is not None: print('Service error:', response.text[:800],flush=True)
            if attempt==2:raise
            time.sleep(3)
def main():
    global ROOT, DATA, KERNEL
    parser=argparse.ArgumentParser();parser.add_argument('action',choices=['prepare','status','logs','submit','push','download'])
    parser.add_argument('--v2', action='store_true', help='Use clean neutral-pose references and higher-detail geometry in a separate private job')
    args=parser.parse_args()
    if args.v2:
        ROOT=ROOT/'crew-v2'
        ROOT.mkdir(exist_ok=True)
        DATA=USER+'/dash-riders-clean-crew-v2'
        KERNEL=USER+'/dash-riders-crew-model-workshop-v2'
    if args.action=='prepare':
        sheets=ROOT/'sheets';sheets.mkdir(exist_ok=True)
        for name in (['ace','nova','bolt'] if args.v2 else ['ace','nova']):shutil.copyfile(ROOT/(name+'.png'),sheets/(name+'.png'))
        (sheets/'dataset-metadata.json').write_text(json.dumps({'id':DATA,'title':'Dash Riders Concept Sheets','licenses':[{'name':'other'}]},indent=2))
        kernel=ROOT/'kernel';kernel.mkdir(exist_ok=True)
        src=pathlib.Path('G:/ai-art/dallas-dash-unity/Tools~/kaggle-hunyuan/gen.py').read_text()
        if args.v2:
            src=src.replace('octree_resolution=380', 'octree_resolution=512').replace('max_facenum=40000', 'max_facenum=80000')
        # The previous working recipe is retained, with a final explicit failure if every asset failed.
        src += '\nif not any("paint_s" in v for v in report.values()):\n    raise RuntimeError("No textured models generated; inspect report.json")\n'
        (kernel/'gen.py').write_text(src)
        (kernel/'kernel-metadata.json').write_text(json.dumps({'id':KERNEL,'title':'Dash Riders Crew Model Workshop V2' if args.v2 else 'Dash Riders Hunyuan3D','code_file':'gen.py','language':'python','kernel_type':'script','is_private':True,'enable_gpu':True,'enable_internet':True,'accelerator':'nvidiaTeslaT4','dataset_sources':[DATA],'competition_sources':[],'kernel_sources':[]},indent=2))
        print('Prepared private Kaggle workflow:',KERNEL,flush=True);return
    api=KaggleApi();retry(api.authenticate)
    if args.action=='status':
        result=retry(lambda:api.kernels_status(KERNEL));print(result,flush=True)
    elif args.action=='logs':
        result=retry(lambda:api.kernels_logs(KERNEL))
        (ROOT/'kernel-log.txt').write_text(str(result),encoding='utf-8')
        print(str(result)[-7000:],flush=True)
    elif args.action=='submit':
        response=retry(lambda:api.dataset_create_version(str(ROOT/'sheets'),'Clean Ace Nova and PDF mono-wheel Bolt references',quiet=False,convert_to_csv=False) if args.v2 else api.dataset_create_new(str(ROOT/'sheets'),public=False,quiet=False,convert_to_csv=False))
        print('Private dataset upload returned',type(response).__name__,flush=True)
        errors=getattr(response,'errors',None)
        if errors:raise RuntimeError(str(errors))
        response=retry(lambda:api.kernels_push(str(ROOT/'kernel'),timeout='10800',acc='NvidiaTeslaT4'))
        print('Free T4 kernel submitted:',KERNEL,flush=True)
        print(response,flush=True)
    elif args.action=='push':
        response=retry(lambda:api.kernels_push(str(ROOT/'kernel'),timeout='10800',acc='NvidiaTeslaT4'))
        print('Free T4 kernel submitted:',KERNEL,flush=True)
        print(response,flush=True)
    elif args.action=='download':
        out=ROOT/'generated';out.mkdir(exist_ok=True)
        retry(lambda:api.kernels_output(KERNEL,path=str(out),force=True,quiet=False))
        print('Downloaded outputs to',out,flush=True)
if __name__=='__main__':main()
