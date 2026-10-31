"""Audit every supplied ARC header, including binary/embedded entries omitted by text decoding.
File literals are evidence of explicit names, not proof against dynamically constructed paths.
"""
import collections
import hashlib
import importlib.util
import json
import re
import struct
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'분석/InstallerVersions'
sys.path.insert(0,str(ROOT/'Tools/OriginalClientChecks'))
from native_pe import PE


def module(name,path):
    spec=importlib.util.spec_from_file_location(name,path);result=importlib.util.module_from_spec(spec);spec.loader.exec_module(result);return result


def literal_paths(raw):
    result=[]
    for match in re.finditer(rb'[\x20-\x7e]{3,}\x00',raw):
        text=match.group()[:-1].decode('ascii')
        if re.search(r'\.(?:pos|set|all|ai|arc|mrc|grs)(?:$|[^A-Za-z])',text,re.I):
            result.append(dict(offset=match.start(),text=text))
    return result


def main():
    sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
    catalog=module('catalog',ROOT/'닥세_리소스/분석도구/build_late_saver_catalog.py')
    decoder=module('decoder',ROOT/'닥세_리소스/분석도구/analyze_client.py')
    visual=module('deployment_visual',ROOT/'닥세_리소스/분석도구/decode_visuals.py')
    inventory=json.loads((OUT/'installer_inventory.json').read_text())
    cache={};versions=[];candidates={};core={}
    work=Path('/tmp/ds-deployment-source-audit');work.mkdir(exist_ok=True)
    for version in inventory['payloads']:
        archives=[];suffixes=collections.Counter();map_entries=0
        for file in version['files']:
            path=Path(file['extractedFile'])
            if file['file'] in ['saver.exe','gamectl.dll','userunit.dll','dmanage.dll']:
                key=file['sha256']
                if key not in core:
                    pe=PE(path)
                    records=literal_paths(pe.data)
                    for record in records:record['va']=hex(pe.va(record['offset']))
                    core[key]=dict(file=file['file'],sha256=key,containers=[],fileLiterals=records,
                                   explicitPosSetAll=[r for r in records if re.search(r'\.(?:pos|set|all)$',r['text'],re.I)])
                core[key]['containers'].extend(version['containers'])
            if not file['file'].endswith('.arc'):continue
            digest=file['sha256']
            if digest not in cache:
                headers=[]
                for name,method,size,packed in catalog.arc_entries(path):
                    suffix=Path(name).suffix.lower()
                    row=dict(entry=name,method=method,bytes=size)
                    # Do not treat a successful text-only pass as an inventory of all resource entries.
                    if suffix in ['.all','.exe']:
                        raw=(decoder.lzh_decode if method==0 else decoder.lzss_decode)(packed,size)
                        assert len(raw)==size
                        h=hashlib.sha256(raw).hexdigest();row['sha256']=h
                        if h not in candidates:
                            candidate=dict(entry=name,bytes=size,sha256=h,occurrences=[])
                            if suffix=='.all':
                                target=OUT/'decoded'/h[:16]/name.lower();target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw)
                                words=[(offset,struct.unpack_from('<I',raw,offset)[0]) for offset in range(0,len(raw)-3,4)]
                                candidate.update(decoded=str(target.relative_to(ROOT)),nonzeroAlignedWords=sum(v!=0 for _,v in words),
                                    alignedWordHistogram=dict(collections.Counter(str(v) for _,v in words)),
                                    observation='Binary scenario-settings candidate. Repeated400/800 and footer strings are not a recovered unique-unit coordinate roster.')
                            else:
                                target=work/(h[:16]+'.exe');target.write_bytes(raw);pe=PE(target)
                                candidate.update(peSha256=pe.sha256,fileLiterals=literal_paths(raw),executed=False,
                                    observation='Embedded charview tool; inspected as PE bytes, not executed.')
                            candidates[h]=candidate
                        candidates[h]['occurrences'].append(dict(containers=version['containers'],archive=file['file'],entry=name))
                    headers.append(row)
                cache[digest]=headers
            headers=cache[digest]
            # Equal archive bytes still have distinct version/path provenance.
            for row in headers:
                suffixes[Path(row['entry']).suffix.lower()]+=1
                if file['file'].startswith('map/'):map_entries+=1
                if row.get('sha256') in candidates:
                    occurrence=dict(containers=version['containers'],archive=file['file'],entry=row['entry'])
                    if occurrence not in candidates[row['sha256']]['occurrences']:candidates[row['sha256']]['occurrences'].append(occurrence)
            archives.append(dict(file=file['file'],sha256=digest,entries=headers))
        versions.append(dict(containers=version['containers'],payloadId=version['payloadId'],archiveCount=len(archives),
                             entrySuffixCounts=dict(suffixes),mapArchiveEntries=map_entries,archives=archives))
    late=next(v for v in versions if 'saver.exe' in v['containers'])
    late_payload=next(v for v in inventory['payloads'] if 'saver.exe' in v['containers'])
    graphics=dict(mrcFiles=0,grsFiles=0,errors=[])
    for file in late_payload['files']:
        suffix=Path(file['file']).suffix.lower()
        if suffix not in ['.mrc','.grs']:continue
        try:
            (visual.read_mrc if suffix=='.mrc' else visual.read_grs)(Path(file['extractedFile']))
            graphics['mrcFiles' if suffix=='.mrc' else 'grsFiles']+=1
        except Exception as error:graphics['errors'].append(dict(file=file['file'],error=str(error)))
    report=dict(scope='All ARC directory entries across supplied installer payloads; selected binary candidates and explicit core filename literals. No native execution, no proof that a filename cannot be constructed dynamically.',
                uniqueArchives=len(cache),versions=versions,binaryCandidates=list(candidates.values()),coreBinaries=list(core.values()),
                lateSummary=dict(archiveCount=late['archiveCount'],entrySuffixCounts=late['entrySuffixCounts'],mapArchiveEntries=late['mapArchiveEntries']),
                lateGraphicsParse=graphics,
                findings=['The late ARC inventory includes19 SET/19 POS plus one29.all, not just decoded text files.',
                          '01..17 source archives have no initial SET/POS entries. AI centers and scripted CHARPOS remain actual client data of different scope.',
                          'No candidate is promoted to a populated 01..17 monster roster without IDs/types/coordinates and a proven consumer.'])
    (OUT/'deployment_source_audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(dict(uniqueArchives=len(cache),late=report['lateSummary'],binaryCandidates=[dict(entry=v['entry'],bytes=v['bytes'],sha256=v['sha256']) for v in candidates.values()],
                          coreFiles=len(core),explicitPosSetAll=sum(len(v['explicitPosSetAll']) for v in core.values())),ensure_ascii=False))


if __name__=='__main__':main()
