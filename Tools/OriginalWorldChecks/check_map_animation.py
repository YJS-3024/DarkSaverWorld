"""Execute DManage frame selection/byte tick/300 clock gate; audit real map pixels."""
import hashlib,json,re,struct,subprocess,sys
from pathlib import Path
from native_ai_emulator import NativeAi,Uc,UC_ARCH_X86,UC_MODE_32,UC_HOOK_CODE,UC_HOOK_MEM_INVALID,UC_X86_REG_ECX,pefile
from check_native_feedback import NativeFeedback
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-map-animation-checks')
DIGEST='08e34fe7a497439ba65b33f3d9b661cbd20ddf098cf536c301337870b2f776f3'
class NativeMapCode(NativeFeedback):
    def __init__(self):
        inventory=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text());row=next(f for p in inventory['payloads']for f in p['files']if f.get('sha256')==DIGEST)
        raw=Path(row['extractedFile']).read_bytes();assert hashlib.sha256(raw).hexdigest()==DIGEST
        pe=pefile.PE(data=raw);self.uc=Uc(UC_ARCH_X86,UC_MODE_32);self.uc.mem_map(0,4096);self.uc.mem_map(self.BASE,(pe.OPTIONAL_HEADER.SizeOfImage+4095)&~4095);self.uc.mem_write(self.BASE,pe.get_memory_mapped_image())
        for addr,size in [(self.STACK,0x100000),(self.STOP,4096),(self.STUB,0x100000),(self.DATA,0x100000)]:self.uc.mem_map(addr,size)
        self.uc.mem_write(self.STOP,b'\xf4');self.handles=[];self.hooks={};self.clock=0
        for n,i in enumerate(i for d in pe.DIRECTORY_ENTRY_IMPORT for i in d.imports):
            addr=self.STUB+n*16;self.write(i.address,addr);self.hooks[addr]=i.name.decode()if i.name else '#'+str(i.ordinal)
        self._hook_add(UC_HOOK_CODE,self._dispatch,begin=self.STUB,end=self.STUB+0xfffff);self._hook_add(UC_HOOK_CODE,self._dispatch,begin=self.STOP,end=self.STOP);self._hook_add(UC_HOOK_MEM_INVALID,self._invalid)
        self.map=self.DATA+0x100;self.manager=self.DATA+0x200
        for a,v in [(self.map+8,self.DATA+0x300),(self.DATA+0x304,self.DATA+0x400),(self.DATA+0x400,self.DATA+0x500),(self.DATA+0x518,self.DATA+0x600),(self.DATA+0x600,self.DATA+0x700),(self.DATA+0x70c,self.DATA+0x800),(self.DATA+0x800,self.DATA+0x900),(self.DATA+0x90c,self.DATA+0xa00),(self.map+0x30,3),(self.map+0x34,2),(self.map+0x14,self.DATA+0x1000),(self.map+0x20,self.DATA+0x2000),(self.DATA+0x2000,self.DATA+0x3000),(self.DATA+0x2004,self.DATA+0x3010),(self.manager+0x34,1),(self.manager+0x4c,self.map)]:self.write(a,v)
    def _dispatch(self,uc,addr,size,data):
        if addr==self.STOP:uc.emu_stop();return
        if self.hooks.get(addr)in ['clock','_clock']:self.ret(self.clock);return
        raise RuntimeError('Unexpected native host call '+str(self.hooks.get(addr)))
    def run(self,c):
        if c['mode']=='select':
            self.write(0x10012204,0);self.uc.mem_write(self.map+4,bytes([c['phase']]));self.uc.mem_write(self.DATA+0x1000,bytes([0,0,0,134,c['selector']])*6)
            self.uc.mem_write(self.DATA+0x3000,bytes([c['random']])*3);self.uc.mem_write(self.DATA+0x3010,bytes([c['random']])*3)
            self.uc.mem_write(self.DATA+0x906,bytes([len(c['frames'])]));self.uc.mem_write(self.DATA+0xa00,bytes(c['frames']));self.write(self.DATA+0x4000,1);self.write(self.DATA+0x4004,1)
            self.uc.reg_write(UC_X86_REG_ECX,self.map);self.invoke(0x1000ab80,[self.DATA+0x4000,self.DATA+0x4004,self.DATA+0x4008]);ptr=self.read(self.DATA+0x4008)
            assert bytes(self.uc.mem_read(self.DATA+0x1000+5*4,5))==bytes([0,0,0,134,c['selector']])
            return self.uc.mem_read(ptr+4,1)[0]
        self.write(0x100121c8,0);self.write(0x100121cc,0);self.uc.mem_write(self.map+4,bytes([c['phase']]));result=[]
        for clock,stage in zip(c['times'],c['stages']):
            self.clock=int(clock);self.uc.reg_write(UC_X86_REG_ECX,self.manager)
            if c['mode']=='clock':self.invoke(0x10008770,[0,0,0,0,0,0])
            else:self.invoke(0x100088f0,[stage,0,0,0,0,0,0])
            result.append(self.uc.mem_read(self.map+4,1)[0])
        return result
def cases():
    result=[]
    for mode in [0,1,2,3,255]:
        for phase in [0,1,2,127,254,255]:
            for random in [0,1,127,128,255]:
                for count in [1,2,3,7,255]:result.append(dict(mode='select',selector=mode,random=random,phase=phase,frames=[(i*5+3)%256 for i in range(count)]))
    for mode in ['clock','four-step']:
        for phase in [0,254,255]:
            times=[0,299,300,301,301,600,601,602,903,5000]+[5301+i*301 for i in range(260)]
            result.append(dict(mode=mode,phase=phase,times=times,stages=[0]*len(times)))
    result.append(dict(mode='four-step',phase=0,times=[301,302,603,604,605,906],stages=[1,2,3,0,1,0]))
    result.append(dict(mode='clock',phase=0,times=[299.9,300,300.1,300.999,301.001,601.9,602.001],stages=[0]*7))
    return result
def maps():
    sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'));from decode_visuals import read_mrc,read_grs
    from build_world_assets import grs_image
    world=json.loads((ROOT/'Assets/_Original/Data/World/KaosiaWorld.json').read_text());campaign=json.loads((ROOT/'Assets/_Original/Data/Campaign/CampaignCatalog.json').read_text());cache={};result=[];sources={}
    for row in world['maps']+[m['world']|{'key':m['key']}for m in campaign['maps']]:
        key=row.get('key',str(row['id']));folder=ROOT/'Assets/_Original/SourceData'/('KaosiaLegacy'if row['sourceVersion']=='Saver_1.2'else'Native/map');path=folder/(key+'.mrc.bytes');m=read_mrc(path);samples=[];dynamic=static=0;state=123;banks=set();initialSamples=0
        sources[path.relative_to(ROOT).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
        def graphic(rid):
            p=folder/f'mdsr{rid}.grs.bytes'
            if p not in cache:cache[p]=read_grs(p);sources[p.relative_to(ROOT).as_posix()]=hashlib.sha256(p.read_bytes()).hexdigest()
            return cache[p]
        for i,t in enumerate(m['tiles']):
            state=(state*214013+2531011)&0xffffffff;random=(state>>16)&255
            if t[1]==255:continue
            group=graphic(t[0])['groups'][t[1]];sequence=group['tiles'][t[2]]['frames'];assert sequence
            for f in sequence:assert group['images'][f]['width']==group['images'][f]['height']==32
            if len(set(sequence))>1:
                if t[4]>=2:dynamic+=1
                else:static+=1
            bank=(m['a'][i],m['b'][i],t[0],t[1],t[2])
            if t[4]>=2 and len(set(sequence))>1 and bank not in banks:
                banks.add(bank);ground=graphic(m['a'][i])['groups'][m['b'][i]]
                for phase,selected in enumerate(sequence):
                    image=grs_image(ground['palette'],ground['images'][0]).convert('RGBA');image.alpha_composite(grs_image(group['palette'],group['images'][selected],True))
                    samples.append(dict(cell=i,phase=phase,animation=True,hash=hashlib.sha256(image.tobytes()).hexdigest()))
            if initialSamples<4 or i%997==0:
                ground=graphic(m['a'][i])['groups'][m['b'][i]];image=grs_image(ground['palette'],ground['images'][0]).convert('RGBA')
                selected=sequence[(random if t[4]<2 else 0)%len(sequence)];image.alpha_composite(grs_image(group['palette'],group['images'][selected],True))
                samples.append(dict(cell=i,hash=hashlib.sha256(image.tobytes()).hexdigest()))
                initialSamples+=1
        result.append(dict(key=key,folder=str(folder),animated=dynamic,staticVariants=static,samples=samples))
    return result,sources
def main():
    WORK.mkdir(exist_ok=True);vectors=cases();mapRows,mapSources=maps();input=WORK/'input.json';output=WORK/'managed.json';input.write_text(json.dumps(dict(cases=vectors,maps=mapRows)))
    bindings=json.loads((OUT/'map-animation-assets.json').read_text());guids={}
    for p,digest in bindings['sources'].items():
        assert hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==digest,p
        guid=re.search(r'^guid: ([a-f0-9]+)$',Path(str(ROOT/p)+'.meta').read_text(),re.M).group(1);guids[guid]=p
    for p,digest in bindings['outputs'].items():assert hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==digest,p
    asset=(ROOT/'Assets/_Original/Data/World/NativeWorldArt.asset').read_text();references=0
    for field,count in [('nativeMaps',bindings['maps']),('nativeGraphics',bindings['nativeGraphics']),('legacyGraphics',bindings['legacyGraphics'])]:
        block=re.search(r'^  '+field+r':\n((?:  - [^\n]*\n)*)',asset,re.M).group(1);items=re.findall(r'guid: ([a-f0-9]+)',block)
        assert len(items)==count and all(g in guids for g in items),field
        references+=len(items)
    # System.Text.Json serializes byte[] as base64; fixtures intentionally use byte values as arrays.
    import base64
    data=json.loads(input.read_text())
    for c in data['cases']:
        if 'frames'in c:c['frames']=base64.b64encode(bytes(c['frames'])).decode()
    input.write_text(json.dumps(data))
    sdk=magic.UNITY/'DotNetSdk';dotnet=sdk/'dotnet';csc=next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'));exe=WORK/'MapAnimationChecks.dll'
    refs=['-r:'+str(p)for p in sorted(next(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0')).glob('*.dll'))]
    sources=[ROOT/'Assets/_Original/Runtime/Maps/NativeMapDecoder.cs',ROOT/'Assets/_Original/Runtime/Maps/NativeMapAnimation.cs',OUT/'MapAnimationChecks.cs'];rsp=WORK/'checks.rsp';rsp.write_text('\n'.join(['-nologo','-target:exe','-langversion:latest','-nostdlib+','-out:'+str(exe)]+refs+list(map(str,sources))))
    exe.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':next((sdk/'shared/Microsoft.NETCore.App').iterdir()).name}}}))
    for name,cmd in [('compile',[str(dotnet),str(csc),'@'+str(rsp)]),('run',[str(dotnet),str(exe),str(input),str(output)])]:
        r=subprocess.run(cmd,cwd=ROOT,text=True,capture_output=True);(WORK/(name+'.log')).write_text(r.stdout+r.stderr)
        if r.returncode:raise RuntimeError((r.stdout+r.stderr)[-2400:])
    managed=json.loads(output.read_text());native=NativeMapCode();mismatches=[]
    try:
        for i,(c,current)in enumerate(zip(vectors,managed['results'])):
            actual=native.run(c)
            if actual!=current:mismatches.append(dict(index=i,input=c,native=actual,managed=current))
    finally:native.close()
    (WORK/'mismatches.json').write_text(json.dumps(mismatches,indent=2))
    sources+=[Path(__file__),ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.MapAnimation.cs',ROOT/'Assets/_Original/Runtime/World/WorldSceneRoot.cs',ROOT/'Assets/_Original/Runtime/World/NativeWorldArt.cs',ROOT/'Assets/_Original/Runtime/Campaign/WorldSceneRoot.Campaign.cs']
    result=dict(cases=len(vectors),clockSamples=sum(len(c.get('times',[]))for c in vectors),mismatches=len(mismatches),assertions=managed['assertions'],pixelSamples=sum(len(m['samples'])for m in mapRows),animatedPixelSamples=sum(s.get('animation',False)for m in mapRows for s in m['samples']),assetReferences=references,maps=managed['maps'],animatedTiles=managed['animated'],randomStaticTiles=managed['staticVariants'],nativeDllSha256=DIGEST,nativeCodeExecuted=True,unityPlayExecuted=False,sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in sources},mapSources=mapSources,scope='Actual DManage1000AB80/AD40 frame selector/byte wrap and TileDraw/FourStep stage0 strict>300 single update. All current native maps decoded in animated mode; independent initial/animated frame pixel samples. Supplied clock/random seed; actual Unity presentation and historical loading-time random seed not measured.',logs=str(WORK))
    (OUT/'map-animation-check.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k not in ['sources','mapSources']}))
    if mismatches:print(json.dumps(mismatches[0]));raise SystemExit(1)
if __name__=='__main__':main()
