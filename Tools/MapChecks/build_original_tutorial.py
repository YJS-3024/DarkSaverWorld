"""Copy the recovered tutorial and crop native BMP frames; no authored lesson/rewards."""
from pathlib import Path
import hashlib, json, re, uuid
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'분석/Saver_1.2/decoded/all_arc/gameres'
OUT=ROOT/'Assets/_Game/Resources/Tutorial'
def guid(p):return uuid.uuid5(uuid.NAMESPACE_URL,str(p.relative_to(ROOT))).hex
def meta(p,template=None):
    m=Path(str(p)+'.meta')
    if m.exists():return
    if template:
        text=re.sub(r'guid: [a-f0-9]{32}','guid: '+guid(p),template,count=1)
        text=re.sub(r'spriteID: [a-f0-9]{32}','spriteID: '+guid(p),text)
    else:text='fileFormatVersion: 2\nguid: '+guid(p)+'\nTextScriptImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
    m.write_text(text)
def normalize_frame_importer(p):
    # Old scenario templates retained Kisra64px multi-sprite entries in32px tutorial imports.
    # Keep GUIDs; import every native frame as its full32px cell, not tight/trimmed bounds.
    import yaml
    m=Path(str(p)+'.meta');fields=yaml.safe_load(m.read_text());imp=fields['TextureImporter']
    imp['spriteMode']=1;imp['spriteMeshType']=0;imp['spriteAlignment']=0
    imp['spritePivot']={'x':.5,'y':.5};imp['spritePixelsToUnits']=32
    imp['internalIDToNameTable']=[];imp['mipmaps']['enableMipMap']=0;imp['textureSettings']['filterMode']=0
    sheet=imp['spriteSheet'];sheet['sprites']=[];sheet['spriteID']=guid(p);sheet['nameFileIdTable']={}
    for key in ['outline','physicsShape','bones','vertices','edges','weights']:sheet[key]=[]
    for platform in imp.get('platformSettings',[]):platform['textureCompression']=0
    m.write_text(yaml.safe_dump(fields,sort_keys=False))
def main():
    OUT.mkdir(parents=True,exist_ok=True);(OUT/'Characters').mkdir(exist_ok=True)
    raw=(SOURCE/'tutorial/TUTORIAL.DEO').read_bytes();text=raw.decode('cp949')
    p=OUT/'TUTORIAL.txt';p.write_text(text);meta(p)
    template=(ROOT/'Assets/_Game/Resources/Scenarios/01/Characters/301_Up_0.png.meta').read_text()
    count=0
    for job in [100,101,103,125,201,301,805]:
        sources=[p for p in SOURCE.glob('char*/*') if p.name.lower()==f'{job}r.bmp']
        if len(sources)!=1:raise ValueError((job,sources))
        indexed=Image.open(sources[0]);size=indexed.width//3
        for mode in ([0,2,3] if job==125 else [0]):
            image=indexed.copy()
            if mode:
                # Same recovered index remap as ClassArtworkConverter.RemapColorIndex.
                image.putdata([i+mode*16 if 0xa0<=i<=0xaf else i for i in indexed.getdata()])
            image=image.convert('RGBA')
            for row,direction in enumerate(['Up','Down','Left','Right']):
                for frame in range(3):
                    crop=image.crop((frame*size,row*size,(frame+1)*size,(row+1)*size))
                    crop.putdata([(r,g,b,0 if r==g==b==0 else 255) for r,g,b,a in crop.getdata()])
                    p=OUT/'Characters'/f'{job}_{mode}_{direction}_{frame}.png';crop.save(p);meta(p,template);normalize_frame_importer(p);count+=1
    # GameCtl 0x10008d50 uses the ToneSurface = etc2/board.bmp, with 20px frame tiles.
    atlas=Image.open(SOURCE/'etc2/board.bmp').convert('RGBA')
    atlas.putdata([(r,g,b,0 if r==g==b==0 else a) for r,g,b,a in atlas.getdata()])
    panel=Image.new('RGBA',(562,104))
    def tile(sx,sy,x,y,w=20,h=20):
        crop=atlas.crop((sx,sy,sx+w,sy+h));panel.alpha_composite(crop,(x,y))
    for y in range(0,84,20):
        for x in range(0,542,20):tile(20,320,x,y)
    for x in range(20,542,20):tile(20,300,x,0);tile(20,340,x,84)
    for y in range(20,84,20):tile(0,320,0,y);tile(40,320,542,y)
    for sx,sy,x,y in [(0,300,0,0),(0,340,0,84),(40,300,542,0),(40,340,542,84)]:tile(sx,sy,x,y)
    panel.alpha_composite(atlas.crop((90,300,158,368)),(18,18))
    p=OUT/'Dialogue.png';panel.save(p);meta(p,(ROOT/'Assets/_Game/Resources/UI/Entry/LoginPanel.png.meta').read_text())
    # GameCtl 0x10008ff0: native checker tiles, colorkey black; 31x31 per 32px cell.
    ranges=OUT/'Ranges';ranges.mkdir(exist_ok=True)
    for name,kind in [('Move',4),('Attack',12),('Magic',8),('Target',3)]:
        x,y=(0,0) if kind==3 else (200+(kind-4)%3*100,(kind-4)//3*100)
        patch=Image.new('RGBA',(32,32));patch.alpha_composite(atlas.crop((x,y,x+31,y+31)),(0,0))
        p=ranges/(name+'.png');patch.save(p);meta(p,(ROOT/'Assets/_Game/Resources/UI/Entry/LoginPanel.png.meta').read_text())
    # GetAttrTable2 returns the original attack/defense/movement triple, not an AP cost.
    raw_rows=[line.split() for line in (SOURCE/'set/TileAttr.atr').read_text('cp949').splitlines()
              if line.strip() and not line.lstrip().startswith(';')]
    assert len(raw_rows)==27 and all(len(row)==31 for row in raw_rows)
    terrain_rows=[]
    for index,row in enumerate(raw_rows):
        assert int(row[0],36)==index
        terrain_rows.append('            {'+','.join(str(int(v)) for v in row[1:])+'},')
    terrain_code="""// Generated from original TileAttr.atr by build_original_tutorial.py; identical early/late numeric rows.
namespace DarkSaver.Events
{
    public static class OriginalTutorialTerrain
    {
        private static readonly int[,] values={
"""+'\n'.join(terrain_rows)+"""
        };
        public static bool TryGet(int attribute,int troop,out int attack,out int defense,out int movement)
        {
            attack=defense=movement=0;
            if(attribute<0 || attribute>=27 || troop<0 || troop>=10)return false;
            attack=values[attribute,troop*3];defense=values[attribute,troop*3+1];movement=values[attribute,troop*3+2];return true;
        }
        public static string Display(int value)=>System.Math.Abs((long)value)==99?"**":value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
"""
    (ROOT/'Assets/_Game/Runtime/Events/OriginalTutorialTerrain.cs').write_text(terrain_code)
    # Editor-only normalized map fixture; tests do not depend on an untracked installation directory.
    import struct
    map_path=ROOT/'분석/Saver_1.2/extracted/Saver_Files/Saver/MAP/301.mrc'
    map_bytes=map_path.read_bytes();resource_count=struct.unpack_from('<i',map_bytes)[0]
    width,height,_=struct.unpack_from('<iii',map_bytes,4+resource_count*4)
    start=16+resource_count*4+2*width*height
    attrs=[map_bytes[start+i*5+3]&0x7f for i in range(width*height)]
    attrs=[a if a<=0x1b else 0 for a in attrs]
    fixture=ROOT/'Assets/_Game/Tests/EditMode/OriginalTutorial301Terrain.json'
    fixture.write_text(json.dumps(dict(width=width,height=height,attributes=attrs,sourceSha256=hashlib.sha256(map_bytes).hexdigest()),indent=2)+'\n');meta(fixture)
    # Raw abilityrecord fields copied by UserUnit::dmSetAbility; no stat roll/formula.
    import csv
    rows={int(r['직업']):r for r in csv.DictReader((ROOT/'분석/Saver_Late/unity_catalogs/units_all.csv').open(encoding='utf-8-sig'))}
    cases=[]
    for job in [100,101,103,125,201,301,805]:
        r=rows[job];cases.append(f"                case {job}: actor.hp={r['체력수']};actor.maxHp={r['체력한']};actor.mp={r['마력수']};actor.maxMp={r['마력한']};break;")
    code="""// Generated by Tools/MapChecks/build_original_tutorial.py from native abilityrecord columns.
using System;
namespace DarkSaver.Events
{
    public static class OriginalTutorialAbilities
    {
        public static void Apply(DarkSaver.Core.TutorialActor actor)
        {
            switch(actor.job)
            {
"""+'\n'.join(cases)+"""
                default:throw new InvalidOperationException("Tutorial ability definition unavailable: "+actor.job);
            }
            actor.magic=101;actor.skill=10101;
        }
    }
}
"""
    (ROOT/'Assets/_Game/Runtime/Events/OriginalTutorialAbilities.cs').write_text(code)
    p=OUT/'Source.json';p.write_text(json.dumps(dict(source=str((SOURCE/'tutorial/TUTORIAL.DEO').relative_to(ROOT)),sha256=hashlib.sha256(raw).hexdigest(),commands=420,messages=100,frames=count),ensure_ascii=False,indent=2)+'\n');meta(p)
    print('Original tutorial: 420 commands, 100 messages,',count,'native frames')
if __name__=='__main__':main()
