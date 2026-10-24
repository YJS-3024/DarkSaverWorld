"""Offline transport/dispatch analysis. No sockets or original PE entrypoints are opened."""
from pathlib import Path
import struct,json,csv,re,hashlib,subprocess
from analyze_special_skills import PE,ROOT,OUT,write

HEADER=b'\xaa\x55\x01';TRAILER=b'\x55\xaa'
def escape(data):return b''.join(b'GP' if x==9 else b'GG' if x==0x47 else bytes([x]) for x in data)
def encode_frame(payload):
 if len(payload)>2048:raise ValueError('original sender limit 2048')
 n=struct.pack('<H',len(payload));return HEADER+escape(n+payload+bytes([(1+sum(n)+sum(payload))&255]))+TRAILER
class StreamDecoder:
 """Strict reference decoder; does not claim identical malformed-input recovery to original."""
 def __init__(self):self.buffer=bytearray()
 def feed(self,data):
  self.buffer.extend(data);out=[]
  while True:
   pos=self.buffer.find(HEADER)
   if pos<0:
    self.buffer[:]=self.buffer[-2:];break
   if pos:del self.buffer[:pos]
   decoded=bytearray();i=3;need=None;invalid=False
   while i<len(self.buffer):
    x=self.buffer[i];i+=1
    if x==0x47:
     if i==len(self.buffer):i-=1;break
     y=self.buffer[i];i+=1
     if y==0x50:x=9
     elif y==0x47:x=0x47
     else:invalid=True;break
    decoded.append(x)
    if len(decoded)==2:
     n=int.from_bytes(decoded,'little')
     if n>2048:invalid=True;break
     need=n+3
    if need is not None and len(decoded)==need:break
   if invalid:del self.buffer[0];continue
   if need is None or len(decoded)<need or len(self.buffer)<i+2:break
   if self.buffer[i:i+2]!=TRAILER or decoded[-1]!=(1+sum(decoded[:-1]))&255:
    del self.buffer[0];continue
   out.append(bytes(decoded[2:-1]));del self.buffer[:i+2]
  return out

def main():
 saver=PE(ROOT/'Saver.exe');game=PE(ROOT/'GameCtl.dll');rows=[];asm=[]
 ranges=[('Saver.exe',0x439320,0x439418,'Sender frame: AA5501, escaped LE length/payload/checksum, 55AA'),('Saver.exe',0x439f30,0x439f96,'Escape 09 -> 4750; 47 -> 4747'),('Saver.exe',0x439a60,0x439e9a,'Receive ring parser: header, unescape, length and checksum'),('Saver.exe',0x439560,0x439630,'Socket queued Send and WSAEWOULDBLOCK'),('Saver.exe',0x439630,0x4397c9,'Socket receive ring, multiple parsed-frame dispatch'),('Saver.exe',0x4397d0,0x4398a2,'ReadFile handle transport and mode2 text branch'),('Saver.exe',0x43df20,0x43e0a0,'argc8/10 launcher input fields including endpoint string and port'),('Saver.exe',0x43b640,0x43b6cd,'Socket Create/Connect wrapper'),('Saver.exe',0x43cff0,0x43d0a5,'Initial opcode19 request serialization'),('Saver.exe',0x43cf30,0x43cfeb,'Server response2D sets ready flag and sends2D plus 3 shorts'),('Saver.exe',0x43ba55,0x43c0e0,'Application dispatch tables'),('Saver.exe',0x42a4a0,0x42a5e3,'Forward application message to GameCtl'),('GameCtl.dll',0x10002ca0,0x10002cb4,'OnPacketReceive forwards positive length'),('GameCtl.dll',0x1000ea20,0x1000ed96,'Game message and subtype dispatch tables')]
 for fn,a,b,n in ranges:
  p=ROOT/fn;asm.append(n+'\n'+subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={b}',str(p)],text=True));rows.append(dict(file=fn,start_va=hex(a),end_va_exclusive=hex(b),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),observation=n))
 write('패킷_코드근거.csv',rows);(OUT/'패킷_코드발췌.asm.txt').write_text('\n'.join(asm))
 dispatch=[]
 for op in range(256):
  if not 0x5a<=op<=0x7b:continue
  case=game.u32(0x1000ec24+game.data(0x1000ec3c+op-0x5a,1)[0]*4)
  if case==0x1000ec20:continue
  if case==0x1000ea43:
   for sub in range(1,179):
    target=game.u32(0x1000ec60+game.data(0x1000ece4+sub-1,1)[0]*4)
    if target!=0x1000ec20:dispatch.append(dict(opcode=hex(op),subtype=hex(sub),case_va=hex(target),layer='GameCtl',scope='handler address decoded; packet schema separate'))
  else:dispatch.append(dict(opcode=hex(op),subtype='',case_va=hex(case),layer='GameCtl',scope='handler address decoded; packet schema separate'))
 write('패킷_GameCtl_분기.csv',dispatch)
 app=[]
 for stage,lo,hi,lut,tab,default in [(1,0x32,0x91,0x43c004,0x43bfe0,0x43bd3c),(2,0x22,0x7c,0x43c084,0x43c064,0x43bf9e)]:
  for op in range(lo,hi+1):
   target=saver.u32(tab+saver.data(lut+op-lo,1)[0]*4)
   if target!=default:app.append(dict(stage=stage,opcode=hex(op),case_va=hex(target),meaning='dispatch target only; stages may both run'))
 write('패킷_Saver_분기.csv',app)
 schemas=[]
 for sub,handler,fields in [(1,0x1000d560,'unit_index,x,y'),(0x0e,0x1000d5c0,'unit_index,x,y'),(5,0x1000d680,'unit_index,hp_display'),(6,0x1000d6d0,'unit_index,mp_display'),(0x82,0x1000d620,'short0,short1,additional_hit_value')]:
  for i,name in enumerate(fields.split(',')):schemas.append(dict(opcode='0x5c',subtype=hex(sub),payload_offset=2+2*i,type='int16_le',field=name,handler_va=hex(handler),scope='client receive; additional hit value product name unresolved'))
 for i in range(4):schemas.append(dict(opcode='0x5c',subtype='0x89',payload_offset=2+2*i,type='int16_le',field='magic_short'+str(i),handler_va='0x1000d930',scope='field semantic names require further confirmation'))
 schemas.extend([dict(opcode='0x5c',subtype='0x89',payload_offset=10,type='uint8',field='target_count',handler_va='0x1000d930',scope='followed by target_count int16_le unit indices at offset11')])
 write('패킷_수신필드.csv',schemas)
 # Add internal handler destinations and imported call names without inventing wire names.
 imports={int(a,16):n for a,n in re.findall(r'^\s*([0-9a-f]{8})\s+<none>\s+\w+\s+(.+)$',subprocess.check_output(['objdump','-p',str(ROOT/'GameCtl.dll')],text=True),re.M)}
 expanded=[]
 for r in dispatch:
  a=int(r['case_va'],16);listing=subprocess.check_output(['objdump','-d','--no-show-raw-insn','-Mintel',f'--start-address={a}',f'--stop-address={a+16}',str(ROOT/'GameCtl.dll')],text=True)
  calls=re.findall(r'(?:call|jmp)\s+0x([0-9a-f]+)',listing);iat=re.findall(r'(?:call|jmp)\s+DWORD PTR ds:0x([0-9a-f]+)',listing)
  # Case13C branches directly to body, otherwise call to named handler is the stable target.
  handler=int(calls[0],16) if calls and not iat else None
  name=imports.get(int(iat[0],16)-0x10000000,'') if iat else ''
  expanded.append(dict(**r,handler_va=hex(handler) if handler else '',direct_import=name))
 write('패킷_GameCtl_처리함수.csv',expanded)
 # Launcher parser starts at argv1 with index -1 (skipped), then consumes the listed fields.
 args=[]
 for argc,fields in [(8,['ignored','identity_string_1','identity_string_2','read_handle','write_handle','u16_login_field','option']), (10,['ignored','identity_string_1','identity_string_2','read_handle','write_handle','u16_login_field','endpoint_host','endpoint_port','option'])]:
  for i,name in enumerate(fields,1):args.append(dict(argc=argc,argv_index=i,meaning=name,confidence='storage/consumer verified; identity labels not assumed account/password'))
 write('패킷_실행인수_구조.csv',args)
 summary=dict(frame_header=HEADER.hex(),frame_trailer=TRAILER.hex(),length='little endian uint16, sender uses <=2048',checksum='(1+len_low+len_high+sum(payload)) &255',escape={'09':'4750','47':'4747'},gamectl_dispatch_entries=len(dispatch),initial_request_opcode='0x19',initial_ready_response_opcode='0x2d',ready_ack_opcode='0x2d',endpoint='argc10 argv7 host argv8 port; no fixed host/port established',scope='offline static analysis plus separate native-instruction framing tests; login/map session not implemented')
 (OUT/'패킷_분석요약.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2));print(json.dumps(summary,ensure_ascii=False))
if __name__=='__main__':main()
