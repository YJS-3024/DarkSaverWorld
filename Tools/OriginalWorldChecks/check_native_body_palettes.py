"""Native getsnum selection and actual Compress.ReadArcBmp2 pixel-index loop."""
import hashlib,itertools,json,struct,subprocess
from pathlib import Path
from check_native_move_hints import NativeMove
from native_ai_emulator import Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE,pefile
from unicorn.x86_const import UC_X86_REG_EBP,UC_X86_REG_ESP,UC_X86_REG_EAX,UC_X86_REG_EIP
from build_body_palettes import remap
from PIL import Image
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-body-palette-checks')
class NativePalette(NativeMove):
    def __init__(self,c):
        super().__init__(dict(c,width=12,height=12,units=[dict(id=1,x=5,y=5,team=c['team'])]));self.palette_case=c
        for address,name in [(0x100024a0,'palette-force'),(self.exports['ally3'],'palette-ally')]:
            known=address in self.hooks;self.hooks[address]=name
            if not known:self._hook_add(UC_HOOK_CODE,self._dispatch,begin=address,end=address)
    def _dispatch(self,uc,address,size,data):
        name=self.hooks.get(address)
        if name in ['palette-force','palette-ally']:self.ret(int(self.palette_case['sameForce'if name=='palette-force'else'allied']));return
        super()._dispatch(uc,address,size,data)
    def run_style(self):
        c=self.palette_case;self.write(0x1010b3d8,int(c['connected']));self.write(self.RUNTIME+0x2e0+0x2dc,c['role'])
        for address,key in [(0x10016110,'own'),(0x10016114,'friend'),(0x10016118,'other')]:self.write(address,c[key])
        self.invoke(self.exports['getsnum'],[1]);style=self.uc.reg_read(UC_X86_REG_EAX);self.close()
        # Confirmed surface-selection clamp following getsnum in B5D0/loadcharbmp.
        return 0 if c['appearance']>299 or style>3 else style
def native_indices():
    path=ROOT.parent/'.tools/ds-ghidra/installed/saver/Compress.dll';raw=path.read_bytes();digest=hashlib.sha256(raw).hexdigest()
    inventory=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text());assert any(f.get('sha256')==digest for p in inventory['payloads']for f in p['files'])
    pe=pefile.PE(data=raw);uc=Uc(UC_ARCH_X86,UC_MODE_32);base=pe.OPTIONAL_HEADER.ImageBase;uc.mem_map(base,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);uc.mem_write(base,pe.get_memory_mapped_image());uc.mem_map(0x100000,0x20000)
    ebp=0x101000;buf=0x110000;width=0x100100;height=0x100104
    def write(p,v):uc.mem_write(p,struct.pack('<I',v&0xffffffff))
    for p,v in [(0x42f768,buf),(ebp-0x16,54),(ebp+0x10,width),(ebp+0x14,height),(width,256),(height,1)]:write(p,v)
    for style in [-1,0,1,2,3,4]:
        uc.mem_write(buf,b'\0'*54+bytes(range(256)));write(ebp+0x18,style);uc.reg_write(UC_X86_REG_EBP,ebp);uc.reg_write(UC_X86_REG_ESP,0x105000);uc.emu_start(0x42cfcb,0x42d029,count=20000)
        actual=bytes(uc.mem_read(buf+54,256));expected=bytes(v+16*style if 0<style<4 and 160<=v<=175 else v for v in range(256));assert actual==expected,style
    return dict(sourceSha256=digest,styles=6,pixelCases=1536,start='0x42CFCB',end='0x42D029')
def main():
    WORK.mkdir(exist_ok=True);cases=[]
    for connected,key,team,same,allied,role,prefs in itertools.product([False,True],[100,299,300,701],[-1,0,7,8],[False,True],[False,True],[-3,-2,0],[(0,3,2),(1,2,3)]):
        cases.append(dict(connected=connected,appearance=key,team=team,sameForce=same,allied=allied,role=role,own=prefs[0],friend=prefs[1],other=prefs[2]))
    input=WORK/'cases.json';output=WORK/'managed.json';input.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'BodyPaletteDifferential.dll'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(OUT.resolve()/'BodyPaletteDifferential.cs'))
    for p in (ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs'):
        if str(p)not in args:args+='\n'+str(p)
    rsp=WORK/'checks.rsp';rsp.write_text(args)
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        if name=='run':exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2000:])
    managed=json.loads(output.read_text());assert len(managed)==len(cases);mismatches=[]
    for c,current in zip(cases,managed):
        native=NativePalette(c).run_style()
        if native!=current:mismatches.append(dict(input=c,native=native,managed=current))
    pixels=native_indices();assets=json.loads((OUT/'body-palette-assets.json').read_text());verified=0
    for row in assets['outputs']:
        source=ROOT/row['source'];output=ROOT/row['asset'];assert hashlib.sha256(source.read_bytes()).hexdigest()==row['sourceSha256'];assert hashlib.sha256(output.read_bytes()).hexdigest()==row['sha256']
        image=Image.open(output).convert('RGBA');assert hashlib.sha256(image.tobytes()).hexdigest()==row['pixelSha256'];verified+=1
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources=['Assets/_Original/Runtime/Combat/NativeBodyPalette.cs','Tools/OriginalWorldChecks/BodyPaletteDifferential.cs','Tools/OriginalWorldChecks/check_native_body_palettes.py','Tools/OriginalWorldChecks/build_body_palettes.py','Tools/OriginalWorldChecks/native_ai_emulator.py']
    result=dict(cases=len(cases),mismatches=len(mismatches),nativeCodeExecuted=True,unityPlayExecuted=False,nativePixelLoop=pixels,verifiedPaletteAssets=verified,sources={p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest()for p in sources},scope='Actual UserUnit getsnum for supplied team/same-force/ally predicates and original prefs0/3/2, class>299 surface clamp; actual Compress ReadArcBmp2 index-remap loop. Original73 indexed BMPs ->219 variants. Team/ally predicates are inputs, not inferred networking logic.',logs=str(WORK))
    (OUT/'native-body-palette-check.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k!='sources'}))
    if mismatches:print(json.dumps(mismatches[0]));raise SystemExit(1)
if __name__=='__main__':main()
