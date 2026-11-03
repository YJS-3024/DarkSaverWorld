"""Original facility IDs/LSC title regions; no map/NPC/stock edits."""
import hashlib,json,re,sys,uuid,struct
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'));from decode_visuals import read_lsc
sys.path.insert(0,str(ROOT/'Tools/OriginalClientChecks'));from native_pe import PE
def main():
 factory=ROOT/'분석/Ghidra/Saver_Late/Bank/factory-473E30.c';text=factory.read_text();mapping={24:9,29:10,30:10,31:10,32:10,34:18,35:17,37:20,40:23,43:27,48:10,67:42};windows=[];inputs=[Path(__file__),factory];virtual=[]
 pe=PE('/workspace/.tools/ds-ghidra/installed/saver/Saver.exe');assert pe.sha256=='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689'
 tables={24:0x486358,29:0x484e10,30:0x484e88,31:0x484f00,32:0x484f78,34:0x484a90,35:0x4862ec,37:0x486430,40:0x485d24,43:0x4849b8,48:0x4851b0,67:0x485e14}
 for id,scene in mapping.items():
  block=re.search(r'case 0x'+format(id,'x')+r':(.*?)(?=\n  case |\n  default:)',text,re.S)[1];assert 's_scene'+str(scene)+'_lsc'in block
  source=ROOT/f'Assets/_Original/SourceData/Native/wlib/scene{scene}.lsc.bytes';inputs.append(source);spec=read_lsc(source);left,top,right,bottom=spec['region'];windows.append(dict(id=id,scene=scene,width=spec['width'],height=spec['height'],dragX=left,dragY=top,dragWidth=right-left,dragHeight=bottom-top))
  table=tables[id];getter=pe.thunk(pe.u32(table+4))
  if id==40:
   pe.expect(getter,'8b8198010000c3');ctor=ROOT/'분석/Ghidra/Saver_Late/FacilityWindows/00460c80.c';assert '0x198) = 0x28;'in ctor.read_text();inputs.append(ctor)
  else:pe.expect(getter,(b'\xb8'+struct.pack('<I',id)+b'\xc3').hex())
  restore=pe.thunk(pe.u32(table+0x4c));after=pe.thunk(pe.u32(table+0x50));assert restore==0x4347e0 and after in [0x4346d0,0x434ed0];windows[-1]['forceCenter']=after==0x4346d0;virtual.append(dict(id=id,vtable=hex(table),idGetter=hex(getter),idField=0x198 if id==40 else None,minimize=hex(pe.thunk(pe.u32(table+0x44))),restore=hex(restore),afterRestore=hex(after)))
  if id in [40,67]:
   source=ROOT/f'Assets/_Original/SourceData/Native/wlib/scene{scene}m.lsc.bytes';inputs.append(source);mini=read_lsc(source);left,top,right,bottom=mini['region'];windows[-1].update(minWidth=mini['width'],minHeight=mini['height'],minDragX=left,minDragY=top,minDragWidth=right-left,minDragHeight=bottom-top)
 path=ROOT/'Assets/_Original/Resources/OriginalFacilityWindows.json';path.write_text(json.dumps(dict(windows=windows),separators=(',',':'))+'\n');meta=Path(str(path)+'.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
 report=dict(windows=len(windows),virtualMethods=virtual,nativeSha256=pe.sha256,resource=path.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),resourceGuid=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(),re.M)[1],scope='Factory473E30 IDs, original LSC full/mini title rectangles and PE actual ID getter/restore/+50 post-restore centering slots. Unknown shop identity is not inferred from a local map or product; no original window assets/world routes rewritten.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in inputs});(OUT/'native-facility-windows-art.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','virtualMethods']}))
if __name__=='__main__':main()
