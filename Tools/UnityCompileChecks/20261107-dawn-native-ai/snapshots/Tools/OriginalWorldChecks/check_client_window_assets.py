"""Source/output/importer/reference audit for original roster, utility windows and pixel fonts."""
import hashlib,io,json,re,struct,sys
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;BASE=ROOT/'Assets/_Original'
sys.path.insert(0,str(ROOT/'Tools/OriginalRestart'))
from pixel_texture_policy import lossless
def campaign_face_bindings_preserved(raw,expected):
 # Keep the old face-builder output manifest as historical evidence. Only
 # these two independently bound additions may differ from its exact bytes.
 from build_native_outro_variants import serialized_field
 asset=BASE/'Data/Campaign/NativeAiRules.json';meta=Path(str(asset)+'.meta').read_text()
 guid=re.search(r'^guid: ([a-f0-9]{32})$',meta,re.M)[1]
 ai='  aiRules: {fileID: 4900000, guid: '+guid+', type: 3}\n';variants=serialized_field();text=raw.decode('utf-8')
 if text.count(ai)!=1 or text.count(variants)!=1:return False
 prior=text.replace(ai,'',1).replace(variants,'',1).encode('utf-8')
 return hashlib.sha256(prior).hexdigest()==expected
def main():
 checks=0
 def check(ok,label):
  nonlocal checks;checks+=1
  if not ok:raise AssertionError(label)
 def guid(path):return re.search(r'^guid: ([a-f0-9]{32})$',Path(str(path)+'.meta').read_text(),re.M)[1]
 for name in ['commander-assets.json','utility-assets.json','native-font-assets.json','world-chat-assets.json','native-face-assets.json','../OriginalTutorialChecks/waiting-window-assets.json']:
  r=json.loads((OUT/name).read_text())
  for path,digest in r['sources'].items():check(hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,path)
  for row in r['outputs']:
   p=ROOT/row['asset'];raw=p.read_bytes();matches=hashlib.sha256(raw).hexdigest()==row['sha256']
   if not matches and row['asset']=='Assets/_Original/Data/Campaign/NativeCampaignArt.asset':
    matches=campaign_face_bindings_preserved(raw,row['sha256'])
    check(not campaign_face_bindings_preserved(raw.replace(b'  faceIds:',b'  changedFaceIds:',1),row['sha256']),'old native face field mutation remains rejected')
    check(not campaign_face_bindings_preserved(raw+b'  aiRules: duplicate\n',row['sha256']),'extra serialized field remains rejected')
   check(matches,row['asset']);meta=Path(str(p)+'.meta').read_text();check(bool(guid(p)),str(p)+'.meta')
   if p.suffix=='.png':check(lossless(meta,*Image.open(p).size),'effective platform point/uncompressed/full-size UI texture')
   if p.suffix=='.json':check('TextScriptImporter:'in meta,'JSON text asset importer')
   if p.suffix=='.asset':check('NativeFormatImporter:'in meta and 'mainObjectFileID: 11400000'in meta,'ScriptableObject resource importer')
 sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
 from decode_visuals import read_lsc
 waiting_path=BASE/'Data/Tutorial/Waiting1500.json';waiting=json.loads(waiting_path.read_text())
 tutorial=(BASE/'Data/Tutorial/NativeTutorialArt.asset').read_text()
 check(guid(waiting_path)in tutorial,'waiting definition resource reference')
 for index,window in enumerate(waiting['windows']):
  for mini in [False,True]:
   texture=BASE/f'Art/Tutorial/Waiting{"Minimized"if mini else"Window"}{window["id"]}.png'
   check(Image.open(texture).size==tuple(window[k]for k in (['minWidth','minHeight']if mini else['width','height'])),'waiting full/minimized texture dimensions')
   field='waitingMinimizedWindows'if mini else'waitingWindows';references=re.search(r'^  '+field+r':\n((?:  - .*\n)*)',tutorial,re.M)[1].splitlines()
   check(guid(texture)in references[index],'ordered waiting window resource reference')
  for mini in [False,True]:
   source=read_lsc(BASE/f'SourceData/Native/wlib/scene{window["scene"]}{"m"if mini else ""}.lsc.bytes');l,t,r,b=source['region']
   prefix='minDrag'if mini else'drag'
   check([window[prefix+key]for key in ['X','Y','Width','Height']]==[l,t,r-l,b-t],'native title drag region')
   check([window[key]for key in (['minWidth','minHeight']if mini else['width','height'])]==[source['width'],source['height']],'native window dimensions')
 check([row['id']for row in waiting['items']]==[1,762,777],'waiting item name IDs only')
 world=(BASE/'Data/World/NativeWorldArt.asset').read_text();font=(BASE/'Resources/OriginalBitmapFont.asset').read_text()
 for field,p in [('commanderWindow',BASE/'Art/World/CommanderRoster3.png'),('commanderMinimized',BASE/'Art/World/CommanderRoster3Minimized.png'),('commanderLayout',BASE/'Data/World/CommanderRoster3.json'),('personalLayout',BASE/'Data/World/WorldPersonalWindows.json'),('utilityLayout',BASE/'Data/World/WorldUtilities.json'),('chatLayout',BASE/'Data/World/WorldChatInput.json'),('chatWindow',BASE/'Art/World/WorldChatInput.png'),('privateChatLayout',BASE/'Data/World/WorldPrivateChat.json'),('privateChatWindow',BASE/'Art/World/WorldPrivateChat.png')]:check(re.search(r'^  '+field+r': .*guid: '+guid(p),world,re.M)is not None,field)
 for field,p in [('normal',BASE/'Art/World/OriginalFontGulim.png'),('alternate',BASE/'Art/World/OriginalFontSol.png'),('mapping',BASE/'Data/World/NativeFontMapping.json'),('m_Script',BASE/'Runtime/UI/NativeBitmapFont.cs')]:check(re.search(r'^  '+field+r': .*guid: '+guid(p),font,re.M)is not None,field)
 layouts=json.loads((BASE/'Data/World/WorldUtilities.json').read_text())['windows'];check([v['id']for v in layouts]==[27,33,81],'original window IDs distinct from scenes')
 for row in layouts:
  p=BASE/f'Art/World/WorldUtility{row["id"]}.png';check(Image.open(p).size==(row['width'],row['height']),p.name);check(guid(p)in world,'utility texture reference')
  for b in row['buttons']:check(0<=b['sx'] and b['sx']+b['width']*3<=512 and 0<=b['sy'] and b['sy']+b['height']<=512,'three original button states within waitlib atlas')
 chat=json.loads((BASE/'Data/World/WorldChatInput.json').read_text());check(Image.open(BASE/'Art/World/WorldChatInput.png').size==(573,28),'source scene1 size');check(chat['id']==17 and chat['scene']==1 and chat['x']==33 and chat['y']==435 and [b['id']for b in chat['buttons']]==[44,45],'source input window/frame/mode IDs distinct from F5 history')
 private=json.loads((BASE/'Data/World/WorldPrivateChat.json').read_text());check(Image.open(BASE/'Art/World/WorldPrivateChat.png').size==(197,139),'source scene47 size');check(private['id']==80 and private['scene']==47 and private['x']==442 and [b['id']for b in private['buttons']]==[38,77,79],'source private close/scroll controls')
 # Independently read the native archive tables, decompress BMPs and compare pixels.
 # No full campaign rebuild: authored maps, gateways and placements stay untouched.
 sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
 from build_late_saver_catalog import arc_entries
 from analyze_client import lzh_decode,lzss_decode
 faces=json.loads((OUT/'native-face-assets.json').read_text());campaign=(BASE/'Data/Campaign/NativeCampaignArt.asset').read_text();selected={};pixels=0;fallback=None
 for index in range(4):
  archive='face'+(str(index)if index else '')+'.arc';path=BASE/'SourceData/Native/gameres'/(archive+'.bytes');declared=faces['archives'][index]
  check(declared['archive']==archive and declared['present']==path.exists(),'archive presence and lookup precedence '+archive)
  if not path.exists():continue
  count=0
  for name,method,size,packed in arc_entries(path):
   if not re.fullmatch(r'\d+\.bmp',name,re.I):continue
   count+=1;identifier=int(name.split('.')[0]);check(method in [0,1],'supported archive compression')
   if identifier in selected:continue
   raw=(lzh_decode if method==0 else lzss_decode)(packed,size);original=Image.open(io.BytesIO(raw)).convert('RGB');selected[identifier]=(archive,original)
  check(declared['entries']==count,'archive BMP count '+archive)
 for name,method,size,packed in arc_entries(BASE/'SourceData/Native/gameres/etc.arc.bytes'):
  if name.lower()=='noface.bmp':fallback=Image.open(io.BytesIO((lzh_decode if method==0 else lzss_decode)(packed,size))).convert('RGB');break
 check(fallback is not None,'native etc.arc fallback entry')
 face_ids=list(struct.unpack('<'+'i'*(len(selected)),bytes.fromhex(re.search(r'^  faceIds: (.*)$',campaign,re.M)[1])))
 references=re.search(r'^  faces:\n((?:  - .*\n)*)',campaign,re.M)[1].splitlines();check(len(references)==len(face_ids)==len(faces['faces'])==len(selected),'face ID/reference lengths')
 check(len(set(face_ids))==len(face_ids) and set(face_ids)==set(selected),'all native face IDs, without duplicate precedence overrides')
 for row,identifier,reference in zip(faces['faces'],face_ids,references):
  path=ROOT/row['asset'];archive,original=selected[identifier];converted=Image.open(path).convert('RGB');meta=Path(str(path)+'.meta').read_text()
  check(row['id']==identifier and row['archive']==archive,'ID and archive identity');check(row['sha256']==hashlib.sha256(path.read_bytes()).hexdigest(),'portrait output hash')
  check(row['guid']==guid(path) and row['guid']in reference,'portrait reference/GUID');check(original.size==converted.size==(64,64) and original.tobytes()==converted.tobytes(),'original portrait dimensions and all RGB pixels');pixels+=64*64
  check(lossless(meta,*converted.size),'portrait effective platform point/uncompressed/no scaling')
 row=faces['fallback'];path=ROOT/row['asset'];converted=Image.open(path).convert('RGB')
 check(converted.size==fallback.size==(64,64) and converted.tobytes()==fallback.tobytes(),'original noface pixels');pixels+=64*64
 check(hashlib.sha256(path.read_bytes()).hexdigest()==row['sha256'] and row['guid']==guid(path),'fallback hash/GUID');check(lossless(Path(str(path)+'.meta').read_text(),*converted.size),'fallback effective lossless importer');check(re.search(r'^  noFace: .*guid: '+row['guid'],campaign,re.M)is not None,'fallback resource reference')
 report=dict(assertions=checks,importerChecks='Effective Unity default/active-platform settings and GUID/pixels, accepting harmless Unity metadata expansion; generation metadata SHA is provenance rather than a cross-import stability rule.',portraitReferences=len(face_ids),portraitPixelChecks=pixels,unityPlay=False,scope='Source hashes, output hashes, actual importer metadata, resource/script references, literal dimensions and native button ranges. Portrait archive table/checksum/decompression and every RGB pixel/reference are compared, following face/face1/face2/face3 precedence; source GetFace analysis is static, not native execution. Not Unity import, rendered text, physical input or audio.',sources={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in [OUT/'check_client_window_assets.py',BASE/'Data/World/NativeWorldArt.asset',BASE/'Data/Campaign/NativeCampaignArt.asset',BASE/'Resources/OriginalBitmapFont.asset',BASE/'Resources/OriginalBitmapFont.asset.meta',waiting_path,BASE/'Data/Tutorial/NativeTutorialArt.asset',ROOT/'Tools/OriginalTutorialChecks/waiting-window-assets.json']})
 for path in [OUT/'build_native_outro_variants.py',BASE/'Data/Campaign/NativeAiRules.json',Path(str(BASE/'Data/Campaign/NativeAiRules.json')+'.meta')]:report['sources'][path.relative_to(ROOT).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
 report['sources']['Tools/OriginalRestart/pixel_texture_policy.py']=hashlib.sha256((ROOT/'Tools/OriginalRestart/pixel_texture_policy.py').read_bytes()).hexdigest()
 (OUT/'client-window-assets-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='sources'}))
if __name__=='__main__':main()
