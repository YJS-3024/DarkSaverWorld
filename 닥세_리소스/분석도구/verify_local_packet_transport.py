"""Loopback-only TCP protocol lab. Synthetic peer, not a working original game server."""
import socket,threading,json,struct
from analyze_network_packets import StreamDecoder,encode_frame
from verify_packet_framing import native_encode
from verify_native_receive import Receiver
from analyze_special_skills import OUT

def main():
 request=b'\x19\x04Demo\x05Local'+struct.pack('<H',1)
 ready=b'\x2d'+struct.pack('<3H',1,2,3)
 updates=[b'\x5c\x01'+struct.pack('<3h',7,20,30),b'\x5c\x05'+struct.pack('<2h',7,100)]
 listener=socket.socket(socket.AF_INET,socket.SOCK_STREAM);listener.bind(('127.0.0.1',0));listener.listen(1);listener.settimeout(5)
 port=listener.getsockname()[1];events=[];errors=[]
 def peer():
  try:
   conn,_=listener.accept()
   with conn:
    conn.settimeout(5);d=StreamDecoder();packets=[]
    while not packets:packets+=d.feed(conn.recv(7))
    assert packets==[request];events.append('synthetic peer received19')
    frame=encode_frame(ready)
    for i in range(0,len(frame),2):conn.sendall(frame[i:i+2])
    packets=[]
    while not packets:packets+=d.feed(conn.recv(7))
    assert packets==[ready];events.append('synthetic peer received2D')
    conn.sendall(b''.join(encode_frame(p) for p in updates))
  except Exception as e:errors.append(repr(e))
 t=threading.Thread(target=peer,daemon=True);t.start()
 try:
  with socket.create_connection(('127.0.0.1',port),timeout=5) as conn:
   conn.settimeout(5);conn.sendall(native_encode(request));r=Receiver();received=[]
   while not received:
    data=conn.recv(3);assert data;received+=r.feed(data)
   assert received==[ready];conn.sendall(native_encode(ready));received=[]
   while len(received)<2:
    data=conn.recv(3);assert data;received+=r.feed(data)
   assert received==updates;events.append('original parser decoded2D,5C01,5C05')
 finally:listener.close();t.join(6)
 assert not t.is_alive() and not errors,errors
 result=dict(status='passed',binding='127.0.0.1 ephemeral port',tcp_messages=5,events=events,scope='synthetic Python peer + original x86 encoder/receiver kernels over real loopback TCP; no Windows client, original app handshake/state machine, login/map/gameplay session')
 (OUT/'패킷_로컬TCP검증.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
