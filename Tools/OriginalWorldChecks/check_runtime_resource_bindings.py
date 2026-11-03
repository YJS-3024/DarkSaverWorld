"""Static Roslyn Resources.Load bindings and referenced ScriptableObject GUID graph.
No Unity import, deserialization, EventSystem, frame rendering or Play occurs.
"""
import hashlib,json,os,re,shutil,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];OUT=Path(__file__).parent;WORK=Path('/tmp/ds-runtime-resource-bindings')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def inventory(root=ROOT):
 keys={};guids={};rows=[]
 for path in sorted((root/'Assets').rglob('*')):
  if not path.is_file():continue
  relative=path.relative_to(root).as_posix()
  if path.suffix=='.meta':
   match=re.search(r'^guid: ([a-f0-9]{32})$',path.read_text(),re.M)
   if match:
    guid=match[1];guids.setdefault(guid,[]).append(Path(str(path)[:-5]));rows.append((relative,guid))
  elif 'Resources'in path.parts:
   index=len(path.parts)-1-list(reversed(path.parts)).index('Resources');key='/'.join(path.parts[index+1:]);key=str(Path(key).with_suffix('')).replace('\\','/').lower();keys.setdefault(key,[]).append(path);rows.append(('resource:'+relative,key))
 return keys,guids,hashlib.sha256(json.dumps(rows,separators=(',',':')).encode()).hexdigest()
def main():
 WORK.mkdir(exist_ok=True);sdk=ROOT.parent/'.tools/unity/6000.5.7f1/Editor/Data/DotNetSdk';roslyn=next(sdk.glob('sdk/*/Roslyn/bincore'));old=Path('/tmp/ds-original-entry-checks');exe=WORK/'ResourceSyntaxAudit.dll';env=dict(os.environ,DOTNET_CLI_HOME='/tmp/ds-dotnet')
 args='\n'.join(line for line in (old/'contracts.rsp').read_text().splitlines()if not line.strip('"').endswith('.cs')and not line.strip('"').startswith('-out:'))+'\n-out:'+str(exe)+'\n"'+str(OUT.resolve()/'ResourceSyntaxAudit.cs')+'"\n'
 for name in ['Microsoft.CodeAnalysis.dll','Microsoft.CodeAnalysis.CSharp.dll']:
  source=roslyn/name;args+='\n-r:"'+str(source)+'"\n';shutil.copyfile(source,WORK/name)
 (WORK/'checks.rsp').write_text(args);exe.with_suffix('.runtimeconfig.json').write_bytes((old/'EntryChecks.runtimeconfig.json').read_bytes())
 for phase,cmd in [('compile',[str(sdk/'dotnet'),str(roslyn/'csc.dll'),'@'+str(WORK/'checks.rsp')]),('run',[str(sdk/'dotnet'),str(exe),str(ROOT)])]:
  p=subprocess.run(cmd,cwd=ROOT,env=env,capture_output=True,text=True);(WORK/(phase+'.log')).write_text(p.stdout+p.stderr)
  if p.returncode:raise RuntimeError((p.stdout+p.stderr)[-2000:])
 syntax=json.loads(p.stdout);keys,guids,inventorySha=inventory();checks=0;paths=set([Path(__file__),OUT/'ResourceSyntaxAudit.cs']);visited=set();bindings=[]
 def check(value,label):
  nonlocal checks;checks+=1
  if not value:raise AssertionError(label)
 def observe(path):
  check(path.is_file(),'referenced asset exists '+str(path));paths.add(path);meta=Path(str(path)+'.meta');check(meta.is_file(),'referenced metadata exists '+str(meta));paths.add(meta)
  match=re.search(r'^guid: ([a-f0-9]{32})$',meta.read_text(),re.M);check(match and len(guids.get(match[1],[]))==1,'referenced GUID unique '+str(meta))
 def asset_type(path):
  if path.suffix.lower()in ['.json','.bytes','.txt','.csv','.xml']:return 'TextAsset'
  if path.suffix.lower()in ['.png','.bmp','.jpg','.jpeg','.tga','.psd']:return 'Texture2D'
  if path.suffix=='.asset':
   match=re.search(r'm_Script:.*guid: ([a-f0-9]{32})',path.read_text());check(match and len(guids.get(match[1],[]))==1,'ScriptableObject MonoScript reference resolves '+str(path));script=guids[match[1]][0];observe(script);declared=[c['name']for c in syntax['classes']if c['source']==script.relative_to(ROOT).as_posix()];check(script.stem in declared,'MonoScript filename/class binding '+str(script));return script.stem
  return None
 def compatible(actual,wanted):
  wanted=wanted.split('.')[-1]
  if actual==wanted or wanted=='Object':return True
  candidates=[row for row in syntax['classes']if row['name']==actual]
  return any(compatible(base.split('.')[-1],wanted)for row in candidates for base in row['bases']if base.split('.')[-1]!=actual)
 def resolve(key,wanted,catalog):
  values=catalog.get(key.lower(),[])
  return values[0]if len(values)==1 and compatible(asset_type(values[0]),wanted)else None
 def visit(path):
  if path in visited:return
  visited.add(path);observe(path)
  if path.suffix=='.json':json.loads(path.read_text())
  if path.suffix=='.asset':
   for guid in re.findall(r'guid: ([a-f0-9]{32})',path.read_text()):
    if guid=='0'*32:continue
    check(len(guids.get(guid,[]))==1,'serialized GUID reference resolves uniquely '+guid);visit(guids[guid][0])
 for call in syntax['calls']:
  path=ROOT/call['source'];paths.add(path);check(call['method']=='Load'and call['key']is not None,'dynamic/all/async load requires an explicit resource binding: '+str(call));target=resolve(call['key'],call['type'],keys);check(target is not None,'literal resource type/name resolves '+str(call));visit(target);bindings.append(call|dict(asset=target.relative_to(ROOT).as_posix()))
 # Mutation controls operate on the in-memory index; no game assets are changed.
 for call in syntax['calls']:
  missing=dict(keys);missing.pop(call['key'].lower());check(resolve(call['key'],call['type'],missing)is None,'missing resource mutation detected')
  duplicate=dict(keys);target=keys[call['key'].lower()][0];duplicate[call['key'].lower()]=[target,target];check(resolve(call['key'],call['type'],duplicate)is None,'ambiguous resource mutation detected')
 for path in (ROOT/'Assets/_Original/Runtime').rglob('*.cs'):paths.add(path)
 result=dict(assertions=checks,mismatches=0,literalCalls=len(bindings),resourceKeys=len({row['key']for row in bindings}),resourceAssets=len(visited),resourceInventoryKeys=len(keys),assetMetaInventory=sum(len(v)for v in guids.values()),inventorySha256=inventorySha,bindings=bindings,unityEditorImport=False,unityPlay=False,scope='Roslyn syntax extraction of all current runtime Resources.Load calls: literal name/type lookup, duplicate/missing mutation controls, requested ScriptableObject MonoScript/class compatibility, recursive serialized GUID graph, JSON syntax and referenced GUID uniqueness. Asset/GUID inventory fingerprint excludes irrelevant importer formatting. No Unity import/deserialization, missing components, actual rendering/input/audio or Player Resources.Load execution.',logs=str(WORK),sources={p.relative_to(ROOT).as_posix():sha(p)for p in sorted(paths)});(OUT/'runtime-resource-bindings-check.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items()if k not in ['sources','bindings']}))
if __name__=='__main__':main()
