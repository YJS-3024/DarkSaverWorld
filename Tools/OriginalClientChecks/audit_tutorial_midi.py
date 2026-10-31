"""Parse original MIDI tempo timeline; not an audible playback measurement."""
from pathlib import Path
import struct,json,argparse
parser=argparse.ArgumentParser();parser.add_argument("midi",type=Path);args=parser.parse_args()
b=args.midi.read_bytes();fmt,tracks,division=struct.unpack_from('>3H',b,8);assert not division&0x8000
pos=8+int.from_bytes(b[4:8],'big');events=[];ends=[]
def vlq(data,i):
 v=0
 while True:
  x=data[i];i+=1;v=(v<<7)|(x&127)
  if x<128:return v,i
for track in range(tracks):
 assert b[pos:pos+4]==b'MTrk';n=int.from_bytes(b[pos+4:pos+8],'big');d=b[pos+8:pos+8+n];pos+=8+n;i=tick=0;running=None
 while i<len(d):
  dt,i=vlq(d,i);tick+=dt;status=d[i]
  if status&128:i+=1
  else:assert running is not None;status=running
  if status==255:
   kind=d[i];i+=1;length,i=vlq(d,i);payload=d[i:i+length];i+=length
   if kind==81:assert length==3;events.append((tick,int.from_bytes(payload,'big')))
   if kind==47:break
  elif status in (240,247):length,i=vlq(d,i);i+=length;running=None
  else:
   assert 128<=status<240;running=status;i+=1 if status>>4 in (12,13) else 2
 ends.append(tick)
assert pos==len(b)
last=0;tempo=500000;seconds=0
for tick,newtempo in sorted(events):seconds+=(tick-last)*tempo/division/1e6;last=tick;tempo=newtempo
seconds+=(max(ends)-last)*tempo/division/1e6
r=dict(format=fmt,tracks=tracks,ticksPerQuarter=division,endTick=max(ends),tempoEvents=events,nominalSeconds=seconds,scope='MIDI timeline at embedded tempo; excludes synth latency, repeat scheduling, actual audible decay')
Path('분석/Saver_Late/tutorial_midi_timeline.json').write_text(json.dumps(r,indent=2)+'\n');print(r)
