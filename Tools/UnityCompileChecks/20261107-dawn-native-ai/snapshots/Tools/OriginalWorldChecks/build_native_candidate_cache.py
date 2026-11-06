"""Package the original789-byte Candid.Dat as a Unity TextAsset; no filter activation."""
import hashlib,json,re,uuid
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
 source=ROOT/'Assets/_Original/SourceData/Native/candid.dat.bytes';assert sha(source)=='3ce93b8abc59895dc8e3e6890f68c4be40e2415ecabf249ed1f91858413c8d7d'
 path=ROOT/'Assets/_Original/Resources/OriginalCandidateCache.bytes';path.write_bytes(source.read_bytes());meta=Path(str(path)+'.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(uuid.NAMESPACE_URL,'DarkSaverOriginalRestart/'+path.relative_to(ROOT/'Assets').as_posix()).hex+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
 r=dict(resource=path.relative_to(ROOT).as_posix(),sha256=sha(path),resourceGuid=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(),re.M)[1],bytes=path.stat().st_size,scope='Exact original compressed Candid.Dat. Configure only; apply on explicit approved logical11/12 or11/13 metadata. No automatic chat filter/name policy/entry-handshake transition.',sources={p.relative_to(ROOT).as_posix():sha(p)for p in [Path(__file__),source]});(OUT/'native-candidate-cache-art.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in r.items()if k!='sources'}))
if __name__=='__main__':main()
