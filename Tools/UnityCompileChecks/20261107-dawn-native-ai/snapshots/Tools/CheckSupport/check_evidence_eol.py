"""Execute the actual tutorial extractor with simulated Windows text writes.
The source files/native PE stay read-only; baseline/current outputs use temp roots.
"""
import contextlib,hashlib,io,json,os,runpy,shutil,sys,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent

def main():
 source=ROOT/'Tools/OriginalTutorialChecks/extract_waiting_tutorial.py'
 canonical=ROOT/'분석/Saver_Late/waiting_tutorial_resources.json'
 expected=json.loads((ROOT/'Tools/OriginalTutorialChecks/tutorial-assets.json').read_text())['sources'][canonical.relative_to(ROOT).as_posix()]
 inventory=json.loads((ROOT/'분석/InstallerVersions/installer_inventory.json').read_text())
 exe=next(Path(r['extractedFile'])for payload in inventory['payloads']for r in payload['files']if r.get('sha256')=='dd512172256cf4795717aed156746fe6588b3434ed1e249ae59146e63d6e4689')
 before=hashlib.sha256(canonical.read_bytes()).hexdigest();outputs={};checks=0
 with tempfile.TemporaryDirectory(prefix='ds-evidence-eol-')as temp:
  for variant in ['baseline','current']:
   target=Path(temp)/variant
   for relative in ['분석/Saver_Late/files.json',*[f'Assets/_Original/SourceData/Native/{name}.bytes'for name in ['map/1500.mrc','wlib/scene2.lsc','wlib/scene3.lsc','wlib/scene38.lsc']]]:
    p=target/relative;p.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(ROOT/relative,p)
   text=source.read_text();statement="output.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\\n').encode('utf-8'))"
   assert statement in text
   if variant=='baseline':text=text.replace(statement,"output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\\n', encoding='utf-8')")
   module={'__file__':str(source),'__name__':'_extractor_eol_control'};exec(compile(text,str(source),'exec'),module)
   module['ROOT']=target;argv=sys.argv;write=Path.write_text
   def windows_write(self,data,*args,**kwargs):
    # Windows newline=None translation, only within this isolated extractor call.
    self.write_bytes(data.replace('\n','\r\n').encode(kwargs.get('encoding','utf-8')));return len(data)
   try:
    sys.argv=[str(source),str(exe)];Path.write_text=windows_write
    with contextlib.redirect_stdout(io.StringIO()):module['main']()
   finally:sys.argv=argv;Path.write_text=write
   output=(target/canonical.relative_to(ROOT)).read_bytes();outputs[variant]=hashlib.sha256(output).hexdigest()
   assert (b'\r\n'in output)==(variant=='baseline');checks+=1
   assert (outputs[variant]==expected)==(variant=='current');checks+=1
   if variant=='baseline':assert hashlib.sha256(output.replace(b'\r\n',b'\n')).hexdigest()==expected;checks+=1
 assert hashlib.sha256(canonical.read_bytes()).hexdigest()==before;checks+=1
 attrs=(ROOT/'.gitattributes').read_text();assert '/분석/Ghidra/Saver_Late/**/*.asm.txt text eol=lf'in attrs;checks+=1
 paths=[Path(__file__),source,ROOT/'.gitattributes',ROOT/'Tools/OriginalTutorialChecks/tutorial-assets.json',canonical]
 result=dict(checks=checks,mismatches=0,unityPlay=False,baselineReproduced=True,currentCanonicalLf=True,outputs=outputs,
  scope='Actual extractor runs against the exact late PE in temp roots; only Path.write_text newline translation simulated. Baseline reproduces CRLF/hash failure, current writes canonical UTF-8 bytes. The PE is read, not launched; no Windows process/Unity Play claim.',
  sources={p.relative_to(ROOT).as_posix():hashlib.sha256(p.read_bytes()).hexdigest()for p in paths})
 (OUT/'evidence-eol-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':main()
