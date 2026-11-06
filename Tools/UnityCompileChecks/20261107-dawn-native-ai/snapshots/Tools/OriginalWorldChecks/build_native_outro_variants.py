"""Bind six existing source DEE variants without regenerating maps or placements."""
import hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original'
def variant_paths():
    return sorted(p for p in (BASE/'SourceData/Decoded/map').glob('*/*.dee.txt')if re.fullmatch(r'\d+[ab]\.dee\.txt',p.name))
def serialized_field():
    paths=variant_paths();assert len(paths)==6
    lines=['  outroVariants:']
    for p in paths:
        guid=re.search(r'^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text(),re.M)[1];lines.append('  - {fileID: 4900000, guid: '+guid+', type: 3}')
    return '\n'.join(lines)+'\n'
def main():
    asset=BASE/'Data/Campaign/NativeCampaignArt.asset';before=asset.read_text();line=serialized_field();clean=re.sub(r'^  outroVariants:\n(?:  - .*\n)*','',before,flags=re.M)
    needle='  m_Name: NativeCampaignArt\n';assert clean.count(needle)==1;after=clean.replace(needle,needle+line)
    assert after.replace(line,'')==clean;asset.write_bytes(after.encode())
    paths=[Path(__file__),asset,*variant_paths(),*[Path(str(p)+'.meta')for p in variant_paths()]]
    report=dict(variants=[p.stem for p in variant_paths()],sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},scope='Bind existing six02/08/12 a/b DEE TextAssets only. All other art/map/portal/placement/source bytes retained; no new script text, server choices or rewards generated.')
    (Path(__file__).parent/'native-outro-variants-art.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(dict(variants=report['variants'],changedFields=['outroVariants'])))
if __name__=='__main__':main()
