"""Rebuild tutorial art/data only from the clean tree's verified native sources."""
import csv, ctypes as C, ctypes.util, hashlib, importlib.util, io, json, re, sys, tempfile, subprocess
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'Assets/_Original'; NATIVE=BASE/'SourceData/Native'; OUT=BASE/'Art/Tutorial'; DATA=BASE/'Data/Tutorial'
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from decode_visuals import read_mrc,read_grs,grs_image,read_lsc,read_ui_library
spec=importlib.util.spec_from_file_location('restart_meta',ROOT/'Tools/OriginalRestart/reset_assets.py')
meta=importlib.util.module_from_spec(spec);spec.loader.exec_module(meta)
def sha(b):return hashlib.sha256(b).hexdigest()
def guid(p):return re.search(r'^guid: ([a-f0-9]{32})$',Path(str(p)+'.meta').read_text(),re.M)[1]
def key(im):
 im=im.convert('RGBA'); im.putdata([(*p[:3],0 if p[:3]==(0,0,0) else 255) for p in im.getdata()]);return im

def render_music(output):
 api=C.CDLL(ctypes.util.find_library('fluidsynth'))
 def fn(n,r,a):f=getattr(api,n);f.restype=r;f.argtypes=a;return f
 p,i,s=C.c_void_p,C.c_int,C.c_char_p
 settings=fn('new_fluid_settings',p,[])()
 fn('fluid_settings_setnum',i,[p,s,C.c_double])(settings,b'synth.sample-rate',44100)
 fn('fluid_settings_setint',i,[p,s,i])(settings,b'synth.cpu-cores',1)
 fn('fluid_settings_setstr',i,[p,s,s])(settings,b'player.timing-source',b'sample')
 with tempfile.TemporaryDirectory(prefix='ds-original-tutorial-midi-') as temp:
  wave=Path(temp)/'music.wav'; mid=Path(temp)/'nco01.mid';mid.write_bytes((NATIVE/'bgm/nco01.mid.bytes').read_bytes())
  fn('fluid_settings_setstr',i,[p,s,s])(settings,b'audio.file.name',str(wave).encode())
  fn('fluid_settings_setstr',i,[p,s,s])(settings,b'audio.file.type',b'wav')
  synth=fn('new_fluid_synth',p,[p])(settings); player=fn('new_fluid_player',p,[p])(synth);renderer=None
  try:
   assert fn('fluid_synth_sfload',i,[p,s,i])(synth,b'/usr/share/sounds/sf2/TimGM6mb.sf2',1)>=0
   assert fn('fluid_player_add',i,[p,s])(player,str(mid).encode())>=0
   renderer=fn('new_fluid_file_renderer',p,[p])(synth); assert renderer
   assert fn('fluid_player_play',i,[p])(player)>=0
   status=fn('fluid_player_get_status',i,[p]); render=fn('fluid_file_renderer_process_block',i,[p]);count=0
   while status(player)==1:
    assert render(renderer)>=0;count+=1; assert count<44100*100/64
   # Native score ends at52.5s. Do not add a new per-repeat release tail.
  finally:
   if renderer:fn('delete_fluid_file_renderer',None,[p])(renderer)
   fn('delete_fluid_player',None,[p])(player);fn('delete_fluid_synth',None,[p])(synth);fn('delete_fluid_settings',None,[p])(settings)
  subprocess.run(['ffmpeg','-v','error','-y','-i',str(wave),'-t','52.5','-c:a','libvorbis','-q:a','4',str(output)],check=True)

def main():
 OUT.mkdir(parents=True,exist_ok=True); DATA.mkdir(parents=True,exist_ok=True)
 report={'sourceVersion':'Saver_Late','sources':{},'outputs':[],
  'adaptations':['CRT clock ticks mapped to milliseconds for presentation; no original wall-clock measurement.',
   'MIDI score synthesized by FluidSynth/TimGM6mb, not original Windows device timbre.',
   'Dialogue mode1 uses native origin40,50; font rasterization remains OS-dependent.',
   'No inferred AP recovery/cap, damage formula, ring grant, gold, town spawn or old single-player runtime. CHARABILITY seeds native temporary magic101/skill10101.']}
 def native(name):
  p=NATIVE/(name+'.bytes');report['sources'][p.relative_to(ROOT).as_posix()]=sha(p.read_bytes());return p
 def register(p,origin):
  meta.meta(p,ROOT/'Assets');report['outputs'].append({'asset':p.relative_to(ROOT).as_posix(),'sha256':sha(p.read_bytes()),'source':origin});return p
 def save(name,image,origin):
  p=OUT/(name+'.png');image.save(p);return register(p,origin)
 def archive_image(name):
  p=BASE/'Art/Archives/gameres'/name;report['sources'][p.relative_to(ROOT).as_posix()]=sha(p.read_bytes());return Image.open(p).convert('RGBA')
 m=read_mrc(native('map/301.mrc'));grs={i:read_grs(native(f'map/mdsr{i}.grs')) for i in m['ids']}
 ground=Image.new('RGBA',(m['width']*32,m['height']*32))
 for index,(rid,group,detail) in enumerate(zip(m['a'],m['b'],m['tiles'])):
  x=index%m['width']*32;y=index//m['width']*32
  g=grs[rid]['groups'][group];ground.alpha_composite(grs_image(g['palette'],g['images'][0]),(x,y))
  if detail[1]!=255:
   g=grs[detail[0]]['groups'][detail[1]];frame=g['tiles'][detail[2]]['frames'][0]
   ground.alpha_composite(grs_image(g['palette'],g['images'][frame],True),(x,y))
 fields={'ground':save('Ground301',ground,'301.mrc base/detail layer0 + mdsr resources')}
 decorations=[];decorpaths=[]
 for i,s in enumerate(m['sprites']):
  g=grs[s['resource']]['sprites'][s['index']];im=grs_image(g['palette'],g['images'][s['frame']],True)
  decorpaths.append(save(f'Decor{i:02}',im,'301.mrc literal decoration '+str(i)))
  decorations.append(dict(x=s['x']*32,y=s['y']*32,width=im.width,height=im.height))
 actorpaths=[];jobs=[100,101,103,125,201,301,805]
 for job in jobs:
  candidates=list((BASE/'Art/Archives/gameres').glob(f'char*.arc/{job}r.bmp.png'));assert len(candidates)==1,(job,candidates)
  source=candidates[0]; report['sources'][source.relative_to(ROOT).as_posix()]=sha(source.read_bytes())
  actorpaths.append(save(f'Actor{job}',key(Image.open(source)),source.relative_to(ROOT).as_posix()+'; RGB black key'))
 # Recover palette-index recoloring from the raw archive, not RGB color guessing.
 import build_late_saver_catalog,analyze_client
 p=native('gameres/char0.arc')
 raw=next((analyze_client.lzh_decode if method==0 else analyze_client.lzss_decode)(packed,size)
          for name,method,size,packed in build_late_saver_catalog.arc_entries(p) if name.lower()=='125r.bmp')
 for mode in [2,3]:
  im=Image.open(io.BytesIO(raw));assert im.mode=='P';im.putdata([v+mode*16 if 0xa0<=v<=0xaf else v for v in im.getdata()])
  actorpaths.append(save(f'TeacherPalette{mode}',key(im),'125r.bmp palette indicesA0..AF +'+str(mode*16)))
 board=key(archive_image('etc2.arc/board.bmp.png'));fields['board']=save('Board',board,'etc2/board.bmp RGB black key')
 dialogue=Image.new('RGBA',(562,104))
 def tile(sx,sy,x,y,w=20,h=20):dialogue.alpha_composite(board.crop((sx,sy,sx+w,sy+h)),(x,y))
 for y in range(0,84,20):
  for x in range(0,542,20):tile(20,320,x,y)
 for x in range(20,542,20):tile(20,300,x,0);tile(20,340,x,84)
 for y in range(20,84,20):tile(0,320,0,y);tile(40,320,542,y)
 for sx,sy,x,y in [(0,300,0,0),(0,340,0,84),(40,300,542,0),(40,340,542,84)]:tile(sx,sy,x,y)
 dialogue.alpha_composite(board.crop((90,300,158,368)),(18,18))
 fields['dialogue']=save('Dialogue',dialogue,'native board20px frame;562x104 with68px portrait recess')
 for name,source in [('face','face.arc/125.bmp.png'),('icons','etc.arc/micon.bmp.png'),('effects','etc.arc/fx.bmp.png'),('attackEffects','etc2.arc/fx2.bmp.png')]:
  fields[name]=save(name.title(),key(archive_image(source)),source+' RGB black key')
 ranges=[save('Range'+name,board.crop((x,y,x+31,y+31)),f'board.bmp({x},{y},31,31)') for name,x,y in [('Move',200,0),('Attack',400,200),('Magic',300,100),('Target',0,0)]]
 native('map/301hmap.bmp');fields['minimap']=BASE/'Art/Native/map/301hmap.bmp.png'
 windows=[];windowpaths=[];minimizedpaths=[]
 positions=[(1,7,10,10),(2,1,5,180),(3,3,450,302),(4,5,228,186),(5,24,5,231),(6,0,515,5),(8,6,512,136),(9,28,228,186),(12,4,0,0)]
 with tempfile.TemporaryDirectory(prefix='ds-tutorial-glib-') as temp:
  libraries={}
  for suffix in ['.lce','.lnm','.ltn']:
   p=Path(temp)/('gamelib'+suffix);p.write_bytes(native('glib/gamelib'+suffix).read_bytes());libraries[suffix]=read_ui_library(p)[:2]
  fields['buttons']=save('Buttons',key(libraries['.ltn'][1]),'glib/gamelib.ltn native button states')
  for k,s,x,y in positions:
   d=read_lsc(native(f'glib/gscene{s}.lsc'));im=Image.new('RGBA',(d['width'],d['height']))
   for decor in d['decor']:
    parts,atlas=libraries['.lce'];p=parts[decor['id']];piece=atlas.crop((p['x'],p['y'],p['x']+p['width'],p['y']+p['height']))
    for ry in range(decor['repeat_y']):
     for rx in range(decor['repeat_x']):im.alpha_composite(piece,(decor['x']+rx*p['width'],decor['y']+ry*p['height']))
   buttons=[]
   for b in d['buttons']:
    parts,atlas=libraries['.ltn' if b['kind']=='button' else '.lnm'];p=parts[b['id']]
    # Buttons rendered independently, so hidden ally rows do not leave baked checkmarks.
    if b['kind']=='animation':im.alpha_composite(atlas.crop((p['x'],p['y'],p['x']+p['width'],p['y']+p['height'])),(b['x'],b['y']))
    else:buttons.append(dict(id=b['id'],x=b['x'],y=b['y'],sx=p['x'],sy=p['y'],width=p['width'],height=p['height']))
   mini_native=NATIVE/f'glib/gscene{s}m.lsc.bytes'
   if mini_native.exists():
    md=read_lsc(native(f'glib/gscene{s}m.lsc'));mi=Image.new('RGBA',(md['width'],md['height']))
    for decor in md['decor']:
     parts,atlas=libraries['.lce'];part=parts[decor['id']];piece=atlas.crop((part['x'],part['y'],part['x']+part['width'],part['y']+part['height']))
     for ry in range(decor['repeat_y']):
      for rx in range(decor['repeat_x']):mi.alpha_composite(piece,(decor['x']+rx*part['width'],decor['y']+ry*part['height']))
    for b in md['buttons']:
     parts,atlas=libraries['.ltn' if b['kind']=='button' else '.lnm'];part=parts[b['id']]
     mi.alpha_composite(atlas.crop((part['x'],part['y'],part['x']+part['width'],part['y']+part['height'])),(b['x'],b['y']))
    minimizedpaths.append(save('MinimizedF'+str(k),key(mi),f'glib/gscene{s}m.lsc native minimized panel'))
   else:minimizedpaths.append(None)
   windows.append(dict(key=k,scene=s,x=x,y=y,width=d['width'],height=d['height'],buttons=buttons))
   windowpaths.append(save('WindowF'+str(k),key(im),f'glib/gscene{s}.lsc literal decor; buttons separate'))
 rows=list(csv.DictReader((BASE/'Data/Tables/units_all.csv').open(encoding='utf-8-sig')))
 abilities=[]
 for job in jobs:
  r=next(r for r in rows if int(r['직업'])==job)
  abilities.append(dict(job=job,**{k:int(r[v]) for k,v in dict(hp='체력수',maxHp='체력한',mp='마력수',maxMp='마력한',attack='공력수',defense='방력수').items()}))
 terrain=[]
 p=BASE/'SourceData/Decoded/gameres/set.arc/tileattr.atr.txt'
 report['sources'][p.relative_to(ROOT).as_posix()]=sha(p.read_bytes())
 for line in p.read_text().splitlines():
  if not line.strip() or line.lstrip().startswith(';'):continue
  row=line.split();assert len(row)==31;terrain.extend(map(int,row[1:]))
 assert len(terrain)==810
 attrs=[t[3]&0x7f for t in m['tiles']]
 definition=dict(map=dict(width=m['width'],height=m['height'],attributes=attrs,terrain=terrain,abilities=abilities),windows=windows,decorations=decorations,
  magicTravelX=336,magicTravelY=0,magicTravelFrames=3,magicImpactX=0,magicImpactY=0,magicImpactFrames=5,magicSound1=0,magicSound2=-1)
 fields['definition']=DATA/'Tutorial301.json';fields['definition'].write_text(json.dumps(definition,ensure_ascii=False,indent=2)+'\n');register(fields['definition'],'MRC terrain/TileAttr/abilityrecord + Glib layout + magicptn2')
 fields['help']=BASE/'SourceData/Decoded/gameres/set.arc/help.txt.txt'
 fields['script']=BASE/'SourceData/Decoded/gameres/tutorial.arc/tutorial.deo.txt'
 report['sources'][fields['script'].relative_to(ROOT).as_posix()]=sha(fields['script'].read_bytes())
 music=OUT/'Nco01.ogg';native('bgm/nco01.mid')
 if not music.exists():render_music(music)
 register(music,'bgm/nco01.mid;52.5s score synthesized using FluidSynth/TimGM6mb');fields['music']=music
 mp=Path(str(music)+'.meta');mg=guid(music);mp.write_text('fileFormatVersion: 2\nguid: '+mg+'\nAudioImporter:\n  externalObjects: {}\n  serializedVersion: 7\n  defaultSettings:\n    serializedVersion: 2\n    loadType: 0\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n    compressionFormat: 1\n    quality: 1\n    conversionMode: 0\n    preloadAudioData: 1\n  forceToMono: 0\n  normalize: 0\n  loadInBackground: 0\n  ambisonic: 0\n  3D: 0\n  userData: Native MIDI; FluidSynth timbre adaptation\n  assetBundleName: \n  assetBundleVariant: \n')
 for p in BASE.rglob('*'):
  if (p.suffix=='.cs' or p.is_dir()) and not Path(str(p)+'.meta').exists():meta.meta(p,ROOT/'Assets',folder=p.is_dir())
 asset=DATA/'NativeTutorialArt.asset';scriptguid=guid(BASE/'Runtime/Tutorial/NativeTutorialArt.cs')
 text='%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+scriptguid+', type: 3}\n  m_Name: NativeTutorialArt\n  m_EditorClassIdentifier: \n'
 def ref(p):
  if p is None:return '{fileID: 0}'
  return '{fileID: '+('4900000' if p.suffix in ['.txt','.json'] else '8300000' if p.suffix=='.ogg' else '2800000')+', guid: '+guid(p)+', type: 3}'
 for k,p in fields.items():text+='  '+k+': '+ref(p)+'\n'
 text+='  actorJobs: '+''.join(int(j).to_bytes(4,'little',signed=True).hex() for j in jobs)+'\n'
 for k,paths in [('minimizedWindows',minimizedpaths),('actors',actorpaths),('windows',windowpaths),('decorations',decorpaths),('ranges',ranges)]:
  text+='  '+k+':\n'+''.join('  - '+ref(p)+'\n' for p in paths)
 asset.write_text(text);meta.meta(asset,ROOT/'Assets');Path(str(asset)+'.meta').write_text('fileFormatVersion: 2\nguid: '+guid(asset)+'\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
 # Bind the new lesson without rebuilding or replacing existing scene objects.
 scene_path=BASE/'Scenes/Start.unity';scene=scene_path.read_text()
 tutorial_guid=guid(BASE/'Runtime/Tutorial/TutorialSceneRoot.cs')
 if 'guid: '+tutorial_guid not in scene:
  assert '  - component: {fileID: 2003}\n' in scene
  scene=scene.replace('  - component: {fileID: 2003}\n','  - component: {fileID: 2003}\n  - component: {fileID: 2004}\n',1)
  component='--- !u!114 &2004\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 2000}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: '+tutorial_guid+', type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n  art: {fileID: 11400000, guid: '+guid(asset)+', type: 2}\n'
  scene=scene.replace('--- !u!1660057539',component+'--- !u!1660057539',1);scene_path.write_text(scene)
 report['asset']=asset.relative_to(ROOT).as_posix()
 evidence=DATA/'TutorialEvidence.json';evidence.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');meta.meta(evidence,ROOT/'Assets')
 (ROOT/'Tools/OriginalTutorialChecks/tutorial-assets.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps({'outputs':len(report['outputs']),'windows':len(windows),'decorations':len(decorations),'attributes':sorted(set(attrs))}))
if __name__=='__main__':main()
