"""Freeze original feedback instructions and bounce-table bytes from inventoried DLLs."""
import hashlib,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];sys.path[:0]=[str(ROOT.parent/'.tools/ds-ghidra-python'),str(ROOT/'Tools/CombatApChecks/python-deps')]
import pefile,capstone

def main():
    inventory=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text());sources=[]
    for filename,digest,functions in [('UserUnit.dll','74b2e0489b1025b03fceb5ad74163d1f07c139d394abe1ee2554b26ba837559b',[(0x1000cda0,0x6c,'SetHpDisplay'),(0x1000ce80,0x79,'SetMpDisplay'),(0x1000ce30,0x49,'SetFail'),(0x1000cf00,0x1f0,'AttackChar'),(0x1000f970,0x320,'NumericRender'),(0x1000fcd0,0x2f0,'AttackRender'),(0x100105f8,0x110,'AttackNumericUpdate')]),('GameCtl.dll','f8aca791efc019ac44610077e46521a68223afa8ee5c894dcee4083be0116da0',[(0x1000d760,0x50,'5C05Decode'),(0x1000d7b0,0x50,'5C06Decode'),(0x1000d700,0x60,'5C82Decode'),(0x1000e290,0x30,'5C0FDecode')])]:
        row=next(f for p in inventory['payloads']for f in p['files']if f.get('sha256')==digest);path=Path(row['extractedFile']);assert hashlib.sha256(path.read_bytes()).hexdigest()==digest
        pe=pefile.PE(str(path));base=pe.OPTIONAL_HEADER.ImageBase;cs=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_32);fixtures=[]
        for start,size,name in functions:
            instructions=list(cs.disasm(pe.get_data(start-base,size),start));selected=[]
            for index,i in enumerate(instructions):
                if index==0 or name=='5C0FDecode' and i.mnemonic=='call' or i.mnemonic=='movsx' or i.mnemonic in ['cmp','test','and','or','imul','shl'] and any(s in i.op_str for s in ['0x14','0x19','0x30','0x40','0xe','0xd',', 3',', 2',', 4','0x10','0x1e','0x46','0xff','0x20']):selected.append(i)
            fixtures.extend(dict(address=hex(i.address),bytes=i.bytes.hex(),instruction=i.mnemonic+' '+i.op_str,meaning=name)for i in selected)
        if filename=='UserUnit.dll':fixtures.append(dict(address='0x10016260',bytes=pe.get_data(0x16260,56).hex(),meaning='14 signed DWORD numeric/MISS bounce offsets'))
        sources.append(dict(version='late',file=filename,sha256=digest,fixtures=fixtures))
    result=dict(sourceInventory='분석/InstallerVersions/installer_inventory.json',sources=sources,semantics=['5C05/5C06 are signed unit/value WORD absolute HP/MP;5C82 is signed target/source/raw-effect WORD. Raw effect is not damage or a MISS result.','AttackChar distance<3 sets opposing16px-per-tile offsets; horizontal facing assignment wins. Both actor counters reset0, source280=target and target280=-1; source294 receives raw flag, target294 is preserved.','Connected attack counter increments to48 and clears on49. Numeric counter displays delta/MISS1..13, HP interpolation/readout1..26, clears on27. HP/MP/MISS share one slot per unit.','HP delta row0 for negative, row1 for zero/positive; MP row3. Absolute HP interpolates old->new during11..20; color boundaries below30%, above70%.','CounterMilliseconds40 is the existing configurable Unity presentation mapping; no original wall-clock/frame rate measurement, wire framing/socket or operating damage/cost rule is claimed.'])
    lifecycle=[]
    for existing,functions in [(sources[0],[(0x1000f000,0x50,'dmKillChar'),(0x1000f050,0x20,'dmKillChar2'),(0x10010120,0xf0,'CorpseUpdate')]),(sources[1],[(0x1000d800,0x30,'5C03Decode'),(0x1000d830,0x40,'5C07Decode')])]:
        row=next(f for p in inventory['payloads']for f in p['files']if f.get('sha256')==existing['sha256']);pe=pefile.PE(row['extractedFile']);cs=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_32);fixtures=[]
        for start,size,name in functions:
            ops=list(cs.disasm(pe.get_data(start-pe.OPTIONAL_HEADER.ImageBase,size),start))
            for i in ops:
                if i.address==start or i.mnemonic in ['cmp','test','or'] or i.mnemonic=='mov' and any(v in i.op_str for v in ['0x100]', '0x164]', ' + 4]']):fixtures.append(dict(address=hex(i.address),bytes=i.bytes.hex(),instruction=i.mnemonic+' '+i.op_str,meaning=name))
        lifecycle.append(dict(version='late',file=existing['file'],sha256=existing['sha256'],fixtures=fixtures))
    Path(__file__).with_name('battle-lifecycle-native-evidence.json').write_text(json.dumps(dict(sourceInventory=result['sourceInventory'],sources=lifecycle,semantics=['5C03 reads one signed unit WORD and calls dmKillChar.5C07 reads two WORDs, ignores the second, calls dmKillChar2.','dmKillChar clears active and occupancy only if80 is not set. dmKillChar2 sets80 without altering HP/ghost/XP.','40 numeric flag gates death updates/render. Normal death increments164, plays sound1 on first step, clears100/enabled after18. Job/100==7 retires immediately without death sound.','Corpse rows32 for counter<8, then counter%4. Spark is31x31 at floor(counter/3)*32. This is a client display/lifecycle rule, not ghost/revival/respawn policy.']),ensure_ascii=False,indent=2)+'\n')
    Path(__file__).with_name('battle-feedback-native-evidence.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(dict(nativeFixtures=sum(len(s['fixtures'])for s in sources))))
if __name__=='__main__':main()
