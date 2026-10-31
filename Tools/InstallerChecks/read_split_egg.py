"""Read the supplied unencrypted split EGG using strict sizes/CRC. No executable runs.
Supports store/deflate/bzip2 blocks; rejects unsupported codecs or metadata.
"""
import bz2,hashlib,json,struct,zlib
from pathlib import Path
EGG=b'EGGA';END=bytes.fromhex('2282e208');FILE=bytes.fromhex('e390850a');BLOCK=bytes.fromhex('130cb502');NAME=0x0a8591ac

def extract(volumes,destination):
 parts=[]
 for path in volumes:
  raw=Path(path).read_bytes();assert raw[:4]==EGG
  stop=raw.find(END,14);assert 14<=stop<=1024
  parts.append(raw[stop+4:])
 raw=b''.join(parts);at=0;entries=[];current=None;content=bytearray();codecs={}
 def finish():
  if current is None:return
  assert len(content)==current['size'],current
  name=Path(current['name'].replace('\\','/'));assert not name.is_absolute() and '..' not in name.parts
  target=Path(destination)/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(content)
  entries.append(dict(current,sha256=hashlib.sha256(content).hexdigest()))
 while at<len(raw):
  magic=raw[at:at+4]
  if magic==END:at+=4;continue
  if magic==FILE:
   finish();_,id,size=struct.unpack_from('<IIQ',raw,at);at+=16;current={'id':id,'size':size};content=bytearray()
   while raw[at:at+4]!=END:
    kind,flags,size=struct.unpack_from('<IBH',raw,at);at+=7;payload=raw[at:at+size];assert len(payload)==size;at+=size
    if kind==NAME:
     assert not flags&0x80,'encrypted name'
     try:current['name']=payload.decode('utf-8')
     except UnicodeDecodeError:current['name']=payload.decode('cp949')
   at+=4;assert 'name' in current
  elif magic==BLOCK:
   _,codec,hint,unpacked,packed,crc=struct.unpack_from('<IBBIII',raw,at);at+=18;assert raw[at:at+4]==END;at+=4
   payload=raw[at:at+packed];at+=packed;assert len(payload)==packed
   if codec==0:decoded=payload
   elif codec==1:decoded=zlib.decompress(payload,-15)
   elif codec==2:decoded=bz2.decompress(payload)
   else:raise ValueError(('unsupported EGG codec',codec))
   assert len(decoded)==unpacked and zlib.crc32(decoded)&0xffffffff==crc
   codecs[codec]=codecs.get(codec,0)+1;content.extend(decoded)
  else:raise ValueError(('unknown EGG record',at,magic.hex()))
 finish();return {'entries':entries,'codecs':codecs,'volumeHashes':[hashlib.sha256(Path(p).read_bytes()).hexdigest() for p in volumes]}
if __name__=='__main__':
 import argparse
 p=argparse.ArgumentParser();p.add_argument('destination',type=Path);p.add_argument('volumes',nargs='+',type=Path);a=p.parse_args();r=extract(a.volumes,a.destination);Path('/tmp/egg-extraction-report.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(len(r['entries']),'files; codecs',r['codecs'])
