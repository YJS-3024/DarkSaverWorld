"""Verify late executable entry-layout instruction evidence without executing it.
Uses the PE section table; coordinates are 640x480 client pixels, not BMP atlas origins.
"""
from pathlib import Path
import sys,struct,json,hashlib
ROOT=Path(__file__).resolve().parents[2];p=Path(sys.argv[1])/'Saver_Files/saver/Saver.exe';raw=p.read_bytes();pe_offset=struct.unpack_from('<I',raw,0x3c)[0]
assert raw[pe_offset:pe_offset+4]==b'PE\0\0'
count=struct.unpack_from('<H',raw,pe_offset+6)[0];optional_size=struct.unpack_from('<H',raw,pe_offset+20)[0]
optional=pe_offset+24;base=struct.unpack_from('<I',raw,optional+28)[0];sections=optional+optional_size
def at(va,size):
 for i in range(count):
  virtual_size,rva,file_size,offset=struct.unpack_from('<IIII',raw,sections+i*40+8)
  if rva<=va-base<rva+file_size:return raw[offset+va-base-rva:offset+va-base-rva+size]
 raise ValueError(hex(va))
fixtures=[(0x406fd0,'68a8ea4800','login.bmp load'),(0x40a517,'bfe2000000','panel destination left226'),(0x40a51c,'bd17010000','panel destination top279'),(0x40a538,'c744244c60000000','source bottom96'),(0x40b6ee,'b8e2000000','second panel draw left226'),(0x40b701,'b917010000','second panel draw top279'),(0x40c9c7,'81fa2a010000','ID/password left test298'),(0x40c9cf,'81fa98010000','ID/password right test408'),(0x40c9dd,'81fe28010000','ID top test296'),(0x40c9e5,'81fe3c010000','ID bottom test316'),(0x40ca14,'81fa40010000','password top test320'),(0x40ca1c,'81fa52010000','password bottom test338')]
fixtures += [
 (0x406f4e,'68c4ea4800','Logo6 background load'),
 (0x4079e2,'899e04010000','initial stage zero'),
 (0x407d34,'937c4000','stage-zero renderer jump-table target'),
 (0x407c9a,'e807acffff','stage-zero renders login directly via thunk4028a6'),
 (0x40b783,'b9ef000000','connect fast destination x239'),
 (0x40b788,'ba53010000','connect fast destination y339'),
 (0x40bad9,'b926010000','join fast destination x294'),
 (0x40b91a,'b94e010000','options fast destination x334'),
 (0x40bc55,'b976010000','exit fast destination x374'),
 (0x40bdee,'b8e2000000','options drawer destination x226'),
 (0x40bdf3,'b968010000','options drawer destination y360'),
 (0x40c36e,'68ffffff00','entry GDI text color white'),
 (0x40635e,'6880ea4800','entry GDI Gulim font name'),
 (0x406371,'6858020000','entry GDI font weight600'),
 (0x406378,'6a05','entry GDI average font width5'),
 (0x40637a,'6a0c','entry GDI font height12')]
fixtures += [(0x40c3b1,'6826010000','ID text top294'),(0x40c3b6,'6860010000','ID text center352'),(0x40c3c7,'683e010000','password text top318'),(0x40c3cc,'6860010000','password text center352')]
for va,code,meaning in fixtures:assert at(va,len(bytes.fromhex(code)))==bytes.fromhex(code),(hex(va),meaning)
report=dict(executable='Saver.exe',sha256=hashlib.sha256(p.read_bytes()).hexdigest(),version='Saver_Late',logicalSize=[640,480],loginAtlas='login.bmp',atlasLoad={'instruction':'0x406fd0','stringVA':'0x48eaa8','surfaceMember':'0x2138'},panel={'sourceRectLTRB':[9,9,206,96],'destinationOrigin':[226,279],'drawSites':['0x40a517','0x40a51c','0x40b6ee','0x40b701'],'fastPathSize':[197,87],'directDrawDestinationRectLTRB':[226,279,432,375],'note':'BltFast keeps native crop size; Blt branch supplies a206x96 destination rect. Common origin is confirmed.'},inputHitTests={'boundsAreExclusive':True,'idLTRB':[298,296,408,316],'passwordLTRB':[298,320,408,338],'function':'0x40c9b0'},scope='Static x86 instruction and import-signature analysis. Not an executed original screen or proof that every rendering branch has identical scaling.',verifiedInstructions=[dict(address=hex(va),bytes=code,meaning=meaning) for va,code,meaning in fixtures])
report['inputText']={'idCenterTop':[352,294],'passwordCenterTop':[352,318],'alignment':'horizontal center; independent of exclusive hitboxes','draw':'0x40C3B1..0x40C3D8','centeringHelpers':['0x41A89D..0x41A8AC','0x41A61A..0x41A62C']}
report['startup']={'initialStage':0,'renderer':'0x407c93 -> 0x40b630','extraStartButtons':False,'background':'Logo6.bmp'}
report['buttons']=[
 dict(key='login',destination=[239,339],hoverSource=[10,379,65,398],pressedSource=[77,379,127,395],action='connect'),
 dict(key='open-register',destination=[294,339],hoverSource=[209,55,247,74],pressedSource=[209,79,247,95],action='original web registration; adapted to existing local account form'),
 dict(key='login-options',destination=[334,339],hoverSource=[209,10,247,29],pressedSource=[209,34,244,50],action='toggle drawer'),
 dict(key='quit',destination=[374,339],hoverSource=[252,54,290,73],pressedSource=[252,79,287,95],action='exit')]
report['optionsDrawer']={'destination':[226,360],'sourceRectLTRB':[9,100,206,145],'note':'Billing is retired; original mode-selection backend is not identified and is not replaced by invented functionality.'}
report['textColor']='white (0x40c36e)';report['font']={'face':'굴림','height':12,'width':5,'weight':600,'charset':129,'note':'Unity OS-font fallback and Bold approximate native GDI width/weight; pixel equivalence not established.'};report['geometryPolicy']='BltFast native-size drawing; DirectDraw stretch branch differs. Exact OS font rasterization and pointer hotspots require original execution.'
out=ROOT/'분석/Saver_Late/entry/layout.json';out.parent.mkdir(exist_ok=True);out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print('PASS',len(fixtures),'native instruction fixtures; login origin226,279')
