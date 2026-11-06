"""Native pattern parser/legacy projection against current effect metadata."""
import csv,hashlib,json,struct
from pathlib import Path
from check_native_magic_table_parser import NativeMagicTableParser
from native_ai_emulator import UC_X86_REG_ESP,UC_X86_REG_EIP
from unicorn.x86_const import UC_X86_REG_EBP
from native_magic_table_emulator import LinkedUserMagic
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent

def visual_slots(rows):
    """Actual Magic export prepares live slots even for a zero-effect pattern.

    Compare whole records, not just a handful of sampled fields. This does not
    run the later visual Update/draw handler or assert Unity presentation timing.
    """
    results=[]
    for row in rows:
        if int(row['번호'])not in [101,1101,6601,6602,6603,8101,8102,8103]:continue
        identifier=int(row['번호'])
        for variant in ['ready','hp0','dead80','inactive','disabled','no-source']:
            for target_variant in ['ready','hp0','inactive']:
                source=-1 if variant=='no-source'else 400
                units=[dict(id=400,x=8,y=8,hp=0 if variant=='hp0'else 100,nativeState=0xb0 if variant=='dead80'else 0x30,nativeActive=variant!='inactive',nativeEnabled=variant!='disabled'),
                       dict(id=700,x=9,y=8,hp=0 if target_variant=='hp0'else 100,nativeState=0x20,nativeActive=target_variant!='inactive')]
                case=dict(width=24,height=24,units=units,targets=[700],clock=12345)
                user=LinkedUserMagic(case)
                try:
                    for unit in units:
                        for offset in [0x174,0x1c4,0x1c8,0x1cc,0x1d0,0x1d4,0x1dc,0x1e0,0x1e8]:user.write(user.RUNTIME+unit['id']*0x2e0+offset,0x11223344)
                    expected={unit['id']:bytearray(user.uc.mem_read(user.RUNTIME+unit['id']*0x2e0,0x2e0))for unit in units}
                    def put(unit,offset,value):struct.pack_into('<I',expected[unit],offset,value&0xffffffff)
                    active=source>=0 and variant not in ['inactive','disabled']
                    if active:
                        put(400,0x100,units[0]['nativeState']|0x100)
                        for offset,value in [(0x1c4,0),(0x1e8,12345),(0x1dc,identifier+100000),(0x1e0,0)]:put(400,offset,value)
                        if int(row['범위'])>0:put(400,0x1d0,3);put(400,0x1d4,4)
                    put(700,0x100,0x120)
                    for offset,value in [(0x174,identifier),(0x1c4,0),(0x1e8,12345),(0x1c8,8 if active else -1),(0x1cc,8 if active else -1)]:put(700,offset,value)
                    if int(row['범위'])>0:put(700,0x1d0,3);put(700,0x1d4,4)
                    targets=user.DATA+0x6100;user.uc.mem_write(targets,struct.pack('<h',700))
                    user.invoke(user.exports['Magic'],[(source&0xffff)<<16|identifier,4<<16|3,0,1,targets])
                    for unit in units:assert bytes(user.uc.mem_read(user.RUNTIME+unit['id']*0x2e0,0x2e0))==expected[unit['id']],(identifier,variant,target_variant,unit['id'])
                    assert not user.sent,'Visual slot preparation must not emit a request'
                    results.append(dict(magic=identifier,pattern=int(row['패턴']),source=variant,target=target_variant))
                finally:user.dispose()
    assert len(results)==144
    return results

def main():
    raw=ROOT/'Assets/_Original/SourceData/Decoded/gameres/set.arc/magicptn.atr.txt';magic=ROOT/'Assets/_Original/SourceData/Decoded/gameres/set.arc/magictbl.atr.txt';table=ROOT/'Assets/_Original/Data/Tables/Magictbl.csv';catalog=ROOT/'Assets/_Original/Data/Combat/MagicProjectiles.json'
    n=NativeMagicTableParser();patterns={};records=[]
    for i,line in enumerate(magic.read_text().splitlines()[1:]):n.uc.mem_write(0x1002dbe0+i*320,n.parse(line.encode('cp949')))
    for line in raw.read_text().splitlines()[1:]:
        words=line.split();identifier=int(words[0]);assert 0<identifier<150 and len(words)==19
        n.uc.mem_write(n.INPUT,line.encode('cp949')+b'\0');n.uc.mem_write(n.STACK,bytes(256));n.uc.mem_write(n.STACK+0x14,struct.pack('<I',n.INPUT));n.uc.reg_write(UC_X86_REG_ESP,n.STACK);n.uc.reg_write(UC_X86_REG_EBP,0x0c060000);n.stop=0x10002202
        n.uc.emu_start(0x10002109,n.stop+1,timeout=1000000,count=1000000)
        assert n.uc.reg_read(UC_X86_REG_EIP)==n.stop and n.uc.reg_read(UC_X86_REG_ESP)==n.STACK
        record=bytes(n.uc.mem_read(0x10021bb0+identifier*328,328));fields=list(struct.unpack('<17i',record[260:]));values=list(map(int,words[2:]))
        # Native layout stores both six-field strips first, then their two counts.
        expected=values[:6]+values[7:13]+[values[6],values[13],*values[14:]]
        assert struct.unpack('<i',record[:4])[0]==identifier and fields==expected,(identifier,fields,expected)
        assert record[4:260].split(b'\0')[0].decode('cp949')==words[1].replace('_',' '),identifier
        patterns[identifier]=dict(fields=fields,source=values);records.append(dict(id=identifier,sha256=hashlib.sha256(record).hexdigest()))
    projected=0;missing=[];rows=list(csv.DictReader(table.open(encoding='utf-8-sig')))
    for row in rows:
        if int(row['패턴'])not in patterns:
            # No parser row has written this slot in the supplied DLL image.
            # A lookup still copies that slot; it does not reuse the preceding
            # valid pattern. Do not turn these zeros into a synthetic effect.
            pattern=int(row['패턴']);slot=0x10021bb0+pattern*328
            assert bytes(n.uc.mem_read(slot,328))==bytes(328),(row['번호'],pattern)
            n.lookup(101);n.lookup(int(row['번호']))
            assert bytes(n.uc.mem_read(0x100053b0,68))==bytes(68),row['번호']
            missing.append(dict(magic=int(row['번호']),pattern=pattern));continue
        n.lookup(int(row['번호']));actual=list(struct.unpack('<17i',n.uc.mem_read(0x100053b0,68)))
        assert actual==patterns[int(row['패턴'])]['fields'],row['번호'];projected+=1
    compared=0
    for row in json.loads(catalog.read_text())['patterns']:
        if row['id']in{m['pattern']for m in missing}:
            assert row.get('missingSource')and row['type']==0
            # -1 is the adapter's inert audio sentinel, not a literal native
            # table field: the untouched original sound DWORDs are zero.
            assert row['sound1']==row['sound2']==-1
        if row['id']not in patterns:continue
        expected=patterns[row['id']]['source'];assert[row['type'],row['sound1'],row['sound2']]==expected[14:]
        for key,values in [('impact',expected[:7]),('travel',expected[7:14])]:
            strip=row[key];assert[strip[k]for k in ['x','y','width','height','stepX','stepY','rawCount']]==values,(row['id'],key)
        compared+=1
    slot_cases=visual_slots(rows)
    paths=[Path(__file__),raw,magic,table,catalog,OUT/'check_native_magic_table_parser.py',OUT/'native_magic_table_emulator.py',OUT/'native_mfc_format_emulator.py',OUT/'native_ai_emulator.py',OUT/'check_native_magic_packets.py',OUT/'check_native_move_hints.py',OUT/'check_native_outbound_packets.py',OUT/'check_native_feedback.py',OUT/'check_native_battle_decoder.py',ROOT/'분석/Ghidra/Saver_Late/MagicTableProjection/10001ed0.c',ROOT/'분석/Ghidra/Saver_Late/MagicTableProjection/10002610.c']
    report=dict(originalPatternRows=len(patterns),nativeSscanfCalls=len(patterns)*2,nativeConversions=len(patterns)*20,nativeLegacyPatternProjections=projected,currentCatalogPatternComparisons=compared,missingPatternTableProjections=len(missing),missingPatternIds=sorted({m['pattern']for m in missing}),missingPatternMagic=missing,mismatches=0,unityPlay=False,sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},recordSha256=records,callerRange='10002109..10002201',callerCodeSha256=hashlib.sha256(bytes(n.uc.mem_read(0x10002109,0x10002202-0x10002109))).hexdigest(),
      scope='Original1ED0 ID scan, argument construction, real MSVCRT sscanf, zero-buffer/name copy and1160 underscore normalization execute on all78 decoded source patterns. Each ID stays below native150-record capacity. Actual GetMagicTbl projects both six-value strips, two frame counts, type and sound fields for every matching source magic row. Current JSON raw frame strips/type/sounds match. Six magic rows referencing absent64/65 project zero fields from untouched image slots even after a valid pattern; current missingSource/type0 and adapter inert sound-1 are distinguished from literal native sound0. Source lines/stack/temporary buffer/parsed-magic-record handoff and C-locale TLS/locks are supplied; archive/stdio/outer file loop/DLL initialization, missing-pattern visual dispatch, drawing, audio and elapsed playback speed are not claimed. Runtime type11 count8 override/charge branches are outside this raw-field comparison.')
    report.update(nativeVisualSlotCases=len(slot_cases),nativeWholeVisualRecords=len(slot_cases)*2,visualSlotCases=slot_cases)
    report['scope']+=' Separately, actual UserUnit Magic export and actual setunit lookup prepare144 source/target contexts; both whole736-byte records match explicit slot writes. Missing64/65 and defined no-effect11 still prepare live flags/timers; inactive or disabled caster leaves its record unchanged while target preparation continues. HP0/80 are not caster gates here. This linked UserUnit instance uses the independently parser-validated CSV rows and supplied loaded flag; its pattern slots remain zero because this export only uses the projected radius for slot preparation, not effect-type dispatch. Clock/source records are supplied; later visual Update/type dispatch/clearing/drawing, Unity state integration and frame speed are not checked.'
    (OUT/'native-effect-table-parser-check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k not in ['sources','recordSha256','visualSlotCases']},ensure_ascii=False))
if __name__=='__main__':main()
