"""Native setunit projection -> actual request builders vs production attribute."""
import csv,hashlib,json,subprocess
from pathlib import Path
from native_magic_table_emulator import LinkedGameMagic,LinkedUserMagic
import check_magic_projectiles as magic
ROOT=magic.ROOT;OUT=magic.OUT;WORK=Path('/tmp/ds-native-magic-table')
def main():
    WORK.mkdir(exist_ok=True);table=ROOT/'Assets/_Original/Data/Tables/Magictbl.csv';rows=list(csv.DictReader(table.open(encoding='utf-8-sig')));cases=[]
    for i,r in enumerate(rows):
        metadata={k:int(r[c])for k,c in [('id','번호'),('mp','MP'),('attribute','속성'),('distance','거리'),('range','범위'),('team','팀'),('unit','유닛'),('pattern','패턴')]}
        for mode in [1,2]:cases.append(dict(metadata=metadata,source=[0,1,999,2999][i%4],x=-1 if metadata['unit']==8 else 3,y=-1 if metadata['unit']==8 else 4,mode=mode,targets=[0,1,400],width=24,height=24,units=[]))
    catalog=ROOT/'Assets/_Original/Data/Combat/MagicMenu.json'
    exported=json.loads(catalog.read_text())['entries'];assert len(exported)==len(rows)
    for actual,row in zip(exported,rows):assert actual['id']==int(row['번호'])and actual['attribute']==int(row['속성'])and actual['pattern']==int(row['패턴']),row
    game=LinkedGameMagic();user=LinkedUserMagic(cases[0]);native=[];projection=[]
    try:
        for c in cases:
            # The getter projects the native320-byte row into a shared old record.
            legacy=game.nativeTable.call(c['metadata']['id']);m=c['metadata']
            assert legacy['legacy']==[m['id'],m['mp'],m['distance'],m['range'],m['team'],m['unit']]and legacy['attribute']==m['attribute'],c
            projection.append(legacy)
            gamePacket=game.run_cast(c);user.case=c;user.sent=[];userPacket=user.run_cast()
            assert gamePacket==userPacket,(c,gamePacket,userPacket)
            native.append(gamePacket)
        missing=[]
        for id in [806,80601,806,906,90,906,101]:missing.append(dict(requested=id,**game.nativeTable.call(id)))
        assert[r['legacy'][0]for r in missing]==[806,-1,-1,906,-1,-1,101]
    finally:game.close();user.dispose()
    inp=WORK/'cases.json';output=WORK/'managed.json';inp.write_text(json.dumps(cases,separators=(',',':')))
    old=Path('/tmp/ds-npc-gateway-checks');sdk=magic.UNITY/'DotNetSdk';exe=WORK/'NativeMagicTable.dll';harness=OUT/'NativeMagicDifferential.cs'
    args=(old/'checks.rsp').read_text().replace(str(old/'GatewayChecks.dll'),str(exe)).replace(str(OUT.resolve()/'GatewayChecks.cs'),str(harness.resolve()))
    for source in sorted((ROOT/'Assets/_Original/Runtime/Combat').glob('*.cs')):
        if str(source)not in args:args+='\n'+str(source)
    rsp=WORK/'checks.rsp';rsp.write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'GatewayChecks.runtimeconfig.json').read_bytes())
    for phase,command in [('compile',[str(sdk/'dotnet'),str(next(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))),'@'+str(rsp)]),('run',[str(sdk/'dotnet'),str(exe),str(inp),str(output)])]:
        r=subprocess.run(command,cwd=ROOT,text=True,capture_output=True);(WORK/(phase+'.log')).write_text(r.stdout+r.stderr);assert r.returncode==0,(r.stdout+r.stderr)[-2500:]
    managed=json.loads(output.read_text());assert len(managed)==len(native)
    differences=[dict(index=i,metadata=c['metadata'],mode=c['mode'],native=n,managed=m)for i,(c,n,m)in enumerate(zip(cases,native,managed))if n!=m]
    (WORK/'differences.json').write_text(json.dumps(differences,ensure_ascii=False,indent=2)+'\n');(WORK/'lookup-projections.json').write_text(json.dumps(dict(projections=projection,missing=missing),ensure_ascii=False,indent=2)+'\n')
    from check_native_battle_responses import contract_sources
    sources=contract_sources(args,__file__)
    for path in [table,catalog,OUT/'build_magic_menu.py',OUT/'magic-menu-assets.json',OUT/'native_magic_table_emulator.py',OUT/'check_native_magic_packets.py',OUT/'check_native_move_hints.py',OUT/'check_native_outbound_packets.py',OUT/'native_ai_emulator.py']:
        sources[path.relative_to(ROOT).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
    for path in (ROOT/'분석/Ghidra/Saver_Late/MagicTableProjection').iterdir():
        if path.is_file():sources[path.relative_to(ROOT).as_posix()]=hashlib.sha256(path.read_bytes()).hexdigest()
    report=dict(tableRows=len(rows),nativeCasts=len(cases),nativeBuilderExecutions=len(cases)*2,attributePatternDifferences=sum(r['속성']!=r['패턴']for r in rows),missingLookupChainCases=len(missing),mismatches=len(differences),unityPlay=False,sources=sources,logs=str(WORK),originalSetunitSha256='71b6ca3f0d7fc16405f5ea728fc0522982e2eaac169e3a1a6eb74a36e41bfaca',scope='Actual relocated setunit GetMagicTbl lookup/cache/legacy projection linked directly into both original GameCtl E4C0 and UserUnit05090.440 repository CSV records seeded at native1ED0 sscanf column destinations, loaded flag supplied; archive/file loader and pattern payload are boundaries. Legacy+170 is source attribute (속성), not visual pattern (패턴). Both request modes and full bytes compared to production. Missing80601/90 demonstrates shared stale-cache poisoning, not a guessed replacement, valid spell or runtime rule. No Unity Play/original whole Windows/transport/damage claim.')
    if not differences:(OUT/'native-magic-table-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({k:v for k,v in report.items()if k!='sources'},ensure_ascii=False));assert not differences,differences[:1]
if __name__=='__main__':main()
