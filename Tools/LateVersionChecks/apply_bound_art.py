"""Refresh existing, explicitly named character frames and portraits from late BMPs.
Preserves asset GUIDs. Does not guess bindings for differently named objects.
"""
import sys,json,io,re,hashlib
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from build_late_saver_catalog import arc_entries
from analyze_client import lzh_decode,lzss_decode
native=Path(sys.argv[1])/'Saver_Files/saver/gameres';sheets={};faces={}
for arc in sorted(native.glob('*.arc')):
 if not re.fullmatch(r'(char\d+|face\d*)',arc.stem,re.I):continue
 for name,method,size,packed in arc_entries(arc):
  if re.fullmatch(r'\d+r\.bmp',name,re.I):sheets[int(name[:-5])]=(arc,name,method,size,packed)
  if arc.stem.lower().startswith('face') and re.fullmatch(r'\d+\.bmp',name,re.I):faces[int(Path(name).stem)]=(arc,name,method,size,packed)
cache={};checks=[];blocked=[]
def image(entry):
 arc,name,method,size,packed=entry;key=(arc.name,name)
 if key not in cache:
  data=(lzh_decode if method==0 else lzss_decode)(packed,size);assert len(data)==size
  dest=ROOT/'분석/Saver_Late/decoded_art/gameres'/arc.name/name.lower();dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data)
  cache[key]=Image.open(io.BytesIO(data)).convert('RGBA')
 return cache[key]
def write(p,expected,kind):
 if Image.open(p).size!=expected.size:blocked.append(dict(path=str(p.relative_to(ROOT)),reason='Native frame size changed; scene binding requires review'));return
 changed=Image.open(p).convert('RGBA').tobytes()!=expected.tobytes()
 if changed:expected.save(p)
 checks.append(dict(path=str(p.relative_to(ROOT)),kind=kind,pixelSha256=hashlib.sha256(expected.tobytes()).hexdigest(),changed=changed))
def transparent(im):
 im=im.copy();im.putdata([(*p[:3],0 if p[:3]==(0,0,0) else 255) for p in im.getdata()]);return im
for p in (ROOT/'Assets/_Game').rglob('*.png'):
 rel=str(p.relative_to(ROOT));match=re.fullmatch(r'(?:Fighter|Kisra)?(\d+)_(Up|Down|Left|Right)_(\d+)',p.stem)
 if match and int(match[1]) in sheets:
  im=image(sheets[int(match[1])]);size=im.width//3;row=['Up','Down','Left','Right'].index(match[2]);frame=int(match[3]);assert frame<3
  write(p,transparent(im.crop((frame*size,row*size,(frame+1)*size,(row+1)*size))),'native directional frame')
 elif p.stem.isdigit() and ('/Portraits/' in rel or '/CharacterPortraits/' in rel) and int(p.stem) in faces:
  im=image(faces[int(p.stem)]).copy();im.putalpha(255);write(p,im,'native dialogue portrait')
 elif p.stem in ['301','302','303','304'] and ('/World/Monsters/' in rel or '/UnitStatusPictures/' in rel):
  im=image(sheets[int(p.stem)]);size=im.width//3;column=1 if '/UnitStatusPictures/' in rel else 0
  write(p,transparent(im.crop((column*size,size,(column+1)*size,size*2))),'native field/status frame')
(ROOT/'Tools/LateVersionChecks/bound_art_check.json').write_text(json.dumps(dict(checked=len(checks),updated=sum(x['changed'] for x in checks),entries=checks,blocked=blocked,unityPlay=False),ensure_ascii=False,indent=2)+'\n')
print(json.dumps(dict(checked=len(checks),updated=sum(x['changed'] for x in checks),blocked=blocked)))
