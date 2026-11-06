"""Bridge source-current UserUnit target results into the actual GameCtl68 receiver.
Both DLLs use their original preferred base, so the import result crosses bounded instances.
"""
import hashlib,json
from pathlib import Path
from check_native_magic_redirect import Redirect,NativeMagicClientOutgoing
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
def main():
    target_path=OUT/'native-magic-target-catalog-check.json';target=json.loads(target_path.read_text());assert target['mismatches']==0 and target['tableRows']==440
    for path,digest in target['sources'].items():assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,'Rerun real target producer: '+path
    redirect=Redirect();builder=NativeMagicClientOutgoing();checks=0;sent=0;effects=0;empty=0;transformed=0
    for row in target['cases']:
        m=row['metadata'];point=row['point'];result=row['native']
        for mode in [0x400,0x800]:
            c=dict(magic=m['id'],source=0,x=point[0],y=point[1],metadata=m,ok=result['valid'],targetResult=result,gameMode=mode)
            actual=redirect.receive(c);packet=None;expected_effect=[]
            if result['valid']:
                effect={key:result[key]for key in ['magic','source','x','y','targets']};expected_effect=[effect]
                if mode==0x400:packet=builder.run_cast(dict(metadata=m,source=effect['source'],x=effect['x'],y=effect['y'],mode=2,targets=effect['targets']))
                empty+=not effect['targets'];transformed+=(effect['x'],effect['y'])!=tuple(point)
            assert actual==dict(query=[[m['id'],0,*point]],requests=[packet]if packet else[],effects=expected_effect),(m,point,mode,actual)
            checks+=1;sent+=packet is not None;effects+=bool(expected_effect)
    redirect.close();builder.close()
    paths=[Path(__file__),OUT/'check_native_magic_redirect.py',OUT/'check_native_magic_packets.py',OUT/'check_native_battle_decoder.py',OUT/'check_native_move_hints.py',OUT/'native_ai_emulator.py',target_path]
    report=dict(nativeRedirectCases=checks,targetOracleCases=target['nativeTargetCases'],tableRows=440,requests=sent,magicCalls=effects,emptyTargetMagicCalls=empty,transformedCoordinateCalls=transformed,mismatches=0,unityPlay=False,runtime68Connected=False,
        sourceGameCtlSha256='f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0',sourceUserUnitSha256=target['originalUserUnitSha256'],
        scope='Source-current actual UserUnit target traces are copied unchanged at the GameCtl CheckMagicTarget import boundary; actual68 decoder and E4C0 execute for connected/disconnected game modes. Compares complete emitted bytes with the separately invoked original builder and captures Magic spell/source/coordinate/target-list arguments, including Unit8 -1/-1 coordinates and valid zero-target results. Pointer addresses and unused/padding words are not equated. Bounded cross-instance import bridge, not one Windows process, production68 routing, target permissions, resource changes or final effect rendering.',
        sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths})
    (OUT/'native-magic-redirect-catalog-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'},ensure_ascii=False))
if __name__=='__main__':main()
