"""Bind existing original112 AI texts without rebuilding maps/placements/scripts."""
import hashlib,importlib.util,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];BASE=ROOT/'Assets/_Original'
def main():
    source=ROOT/'분석/Saver_Late/decoded/map';paths=sorted(source.glob('*/*.ai'));assert len(paths)==112
    rows=[]
    for p in paths:
        raw=p.read_bytes();assert all(b<128 for b in raw),p
        rows.append(dict(key=p.stem,text=raw.decode('ascii'),sha256=hashlib.sha256(raw).hexdigest()))
    assert len({int(r['key'])for r in rows})==len(rows)
    out=BASE/'Data/Campaign/NativeAiRules.json';out.write_bytes((json.dumps(dict(entries=rows),ensure_ascii=False,indent=2)+'\n').encode())
    spec=importlib.util.spec_from_file_location('original_metadata',ROOT/'Tools/OriginalRestart/reset_assets.py');meta=importlib.util.module_from_spec(spec);spec.loader.exec_module(meta)
    if not Path(str(out)+'.meta').exists():meta.meta(out,ROOT/'Assets')
    guid=re.search(r'^guid: ([a-f0-9]{32})$',Path(str(out)+'.meta').read_text(),re.M)[1]
    asset=BASE/'Data/Campaign/NativeCampaignArt.asset';before=asset.read_text();line='  aiRules: {fileID: 4900000, guid: '+guid+', type: 3}\n'
    after=re.sub(r'^  aiRules:.*\n','',before,flags=re.M);needle='  m_Name: NativeCampaignArt\n';assert after.count(needle)==1;after=after.replace(needle,needle+line)
    assert re.sub(r'^  aiRules:.*\n','',after,flags=re.M)==re.sub(r'^  aiRules:.*\n','',before,flags=re.M)
    asset.write_bytes(after.encode());report=dict(files=len(rows),output=out.relative_to(ROOT).as_posix(),sha256=hashlib.sha256(out.read_bytes()).hexdigest(),boundAsset=asset.relative_to(ROOT).as_posix(),boundGuid=guid,
      inputs={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths},scope='Original ASCII AI texts only; add one NativeCampaignArt reference, preserve every other serialized art/map/script field. No initial units/coords, stats, permissions or server values generated.')
    (Path(__file__).parent/'native-ai-rules-art.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items()if k!='inputs'}))
if __name__=='__main__':main()
