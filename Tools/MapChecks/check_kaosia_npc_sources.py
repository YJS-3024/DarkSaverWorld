"""Audit source evidence for Kaosia NPC placement; never invent world coordinates."""
import csv,hashlib,json,struct,sys
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'닥세_리소스/분석도구'))
from decode_visuals import read_mrc,read_grs,grs_image
SOURCE=ROOT/'분석/Saver_1.2/decoded/all_arc'

def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def relative(path):return str(path.relative_to(ROOT))
def main():
    copies=[]
    for installation in ('Saver_Files','NetGame_Files','Shared_DLLs'):
        for filename in ('1000.mrc','1000t.mrc'):
            path=ROOT/'닥세_리소스/설치본_추출'/installation/'Saver/MAP'/filename
            assert path.exists(),path
            data=read_mrc(path);raw=path.read_bytes()
            expected=16+4*len(data['ids'])+7*data['width']*data['height']+16*len(data['sprites'])
            assert len(raw)==expected and (data['width'],data['height'],len(data['sprites']))==(43,43,225)
            copies.append(dict(file=relative(path),sha256=digest(path),bytes=len(raw),width=data['width'],height=data['height'],decorations=len(data['sprites']),parsedToExactEOF=True))
    assert len({row['sha256'] for row in copies})==1
    # Identified by direct inspection of the native shield/swords emblems and sign pixels.
    map_path=ROOT/'닥세_리소스/설치본_추출/Saver_Files/Saver/MAP/1000.mrc'
    grs_path=map_path.parent/'mdsr20.grs';native=read_mrc(map_path);graphics=read_grs(grs_path)
    shops=[]
    for name,object_index,group_index,x,y in [('무기점',163,9,36,29),('방어구점',141,2,30,23)]:
        sprite=native['sprites'][object_index]
        assert (sprite['resource'],sprite['index'],sprite['frame'],sprite['x'],sprite['y'])==(20,group_index,0,x,y)
        group=graphics['sprites'][group_index];image=grs_image(group['palette'],group['images'][0],True)
        assert image.size==(192,224)
        shops.append(dict(name=name,mapKey='1000',mrcObjectIndex=object_index,resource=20,spriteGroup=group_index,frame=0,
            objectOriginX=x,objectOriginY=y,coordinateMeaning='zero-based original MRC building top-left tile; not an NPC standing tile or player world coordinate',
            pixelWidth=192,pixelHeight=224,rgbaSha256=hashlib.sha256(image.tobytes()).hexdigest(),resourceFile=relative(grs_path),resourceFileSha256=digest(grs_path),
            visualReviewedOn='2026-10-24',identification='native building sign and roof emblem inspected; no separate human actor in this building image',
            npcName=None,npcAppearanceId=None,npcInstanceId=None,npcX=None,npcY=None,separateShopkeeperExistenceConfirmed=False,
            originalBuildingAlreadyInUnity=True,qualifiedForNpcPlacement=False))
    archives=sorted(p.name for p in (SOURCE/'MAP').iterdir() if p.is_dir())
    assert '1000set' not in [name.lower() for name in archives]
    placement_files=[p for p in (SOURCE/'MAP').rglob('*') if p.suffix.lower() in ('.set','.pos')]
    assert not any(p.stem.lower() in ('1000','1000t') for p in placement_files)
    abilities={int(r['직업']):r for r in csv.DictReader((ROOT/'분석/Saver_1.2/catalogs/npcs_and_special.csv').open(encoding='utf-8-sig'))}
    quests=json.loads((ROOT/'Assets/_Game/Resources/Town/Quests.json').read_text())['rows'];candidates=[]
    for quest in quests:
        source=SOURCE/quest['sources'][0];text=source.read_text(encoding='cp949');header=int(text.splitlines()[0].strip())
        assert header==quest['npcId'] and quest['npcName'] in text
        images=[p for p in (SOURCE/'gameres').glob('char*/*') if p.name.lower()==f'{header}r.bmp'];assert len(images)==1
        portrait=next(p for p in (SOURCE/'gameres/face').iterdir() if p.name.lower()==f'{header}.bmp')
        with Image.open(images[0]) as image:width,height=image.size
        assert width%3==0 and height==width//3*4
        candidates.append(dict(name=quest['npcName'],quest=quest['id'],dialogueHeaderValue=header,abilityTableName=abilities[header]['이름'],
            dialogue=relative(source),dialogueSha256=digest(source),characterSheet=relative(images[0]),characterSheetSha256=digest(images[0]),
            portrait=relative(portrait),frameWidth=width//3,frameHeight=height//4,availableDirectionalFrames=12,
            nativeTownMembershipDirectlyConfirmed=False,originalMapKey=None,originalX=None,originalY=None,qualifiedForOriginalPlacement=False))
    duty_rows=[]
    for path in sorted((SOURCE/'gameres/duty').iterdir()):
        if path.suffix.lower()!='.txt':continue
        text=path.read_text(encoding='cp949');first=text.splitlines()[0].strip()
        if first in ('805','806'):duty_rows.append(dict(file=relative(path),headerValue=int(first)))
    alternate=SOURCE/'gameres/duty/duty13-1.TXT';alternate_text=alternate.read_text(encoding='cp949')
    assert alternate_text.splitlines()[0].strip()=='806' and '김세스' in alternate_text
    intro=SOURCE/'MAP/01set/01.evt';lines=intro.read_text(encoding='cp949').splitlines()
    assert any('MESSAGE 1 805' in line for line in lines) and any('케인의 아들' in line for line in lines)
    kaosia_mentions=[];actor_occurrences=[]
    for path in sorted(SOURCE.rglob('*')):
        if not path.is_file() or path.suffix.lower() not in ('.txt','.deo','.dee','.evt','.srf','.set','.pos'):continue
        for line_number,line in enumerate(path.read_text(encoding='cp949',errors='replace').splitlines(),1):
            if '카오시아' in line:kaosia_mentions.append(dict(file=relative(path),line=line_number,text=line.strip()))
            tokens=line.split(';',1)[0].split()
            if len(tokens)>=5 and tokens[0]=='CHARPOS' and tokens[1] in ('805','806'):
                actor_occurrences.append(dict(file=relative(path),line=line_number,raw=line.strip(),scope='scenario actor occurrence; not a Kaosia town placement'))
    # A quiz tile asking about the town is not a placement coordinate in town1000.
    assert all('/MAP/1000set/' not in row['file'] for row in kaosia_mentions+actor_occurrences)
    report=dict(scope='Installed client resource audit plus explicitly recorded public-source observations; no original server/session or Unity Play execution',
        nativeMapKey='1000',mapCopies=copies,mapFormat='resource table + seven-byte tile cells + sixteen-byte static decoration records; exact EOF, no trailing NPC roster',
        decodedScenarioArchiveCount=len(archives),kaosiaScenarioArchivePresent=False,scenarioPlacementFileCount=len(placement_files),
        kaosiaSetOrPosPresent=False,shopBuildings=shops,candidates=candidates,reusedDialogueHeaders=duty_rows,
        disambiguation=['805 is also the portrait token for Kane\'s son in01.evt.', '806 is also the header of Kim-ses\' duty13 dialogue; it is not a unique world instance identity.',
                        'Kaosia mentions in56/57/59 quiz events do not locate NPCs in town1000.',
                        'Shop/church/bank signs in the native rendered map locate buildings, not their NPC standing positions.'],
        kaosiaTextMentions=kaosia_mentions,scenarioAppearanceOccurrences=actor_occurrences,
        publicSourceReview=dict(reviewedOn='2026-10-24',refreshPolicy='Public observations were reviewed manually; rerunning this tool rechecks installed files, not remote pages.',
            sources=[dict(url='https://lastlangrisser.tistory.com/63',kind='2013 repost of 2003-2004 notices; author states partial editing',
                observations=[dict(originalNoticeDate='2003-12-18',npc='Santa',locationDescription='below a Christmas tree in Kaosia',exactTile=None,scope='seasonal event; no permanent placement inferred'),
                              dict(originalNoticeDate='2004-03-11',npc='pirate',locationDescription='Kaosia center',exactTile=None,scope='limited White Day event; no appearance ID established'),
                              dict(originalNoticeDate='2004-03-11',npc='male and female NPCs',locationDescription='Kaosia/Belpueus',exactTile=None,scope='presence only; cannot identify them as Kane/Taran')]),
                     dict(url='https://lastlangrisser.tistory.com/255',kind='town images article',visualVerification=False,
                          limitation='HTML/image links found; first image returned403 and other fetched images were not exposed as inspectable image bytes by the tool. No coordinates inferred.'),
                     dict(url='https://lastlangrisser.tistory.com/132',kind='screenshot collection',visualVerification=False,limitation='Collection page found; no verifiable NPC-to-native-map coordinate match established.')],
            shopReview=dict(reviewedOn='2026-10-24',url='https://lastlangrisser.tistory.com/65',kind='2013 Kaosia weapon stock compilation',
                finding='Weapon stock descriptions found; author reports other shop material was not collected. No merchant name, portrait/appearance or standing coordinate established.',
                excluded='Later Lapis NPC names and quests are not transferred to the original game.'),
            excluded='Neo DarkSaver/Lapis and later remakes are not used as original installation NPC placement evidence.'),
        confirmedOriginalNpcPlacements=0,unityNpcPlacementsAdded=0,decision='Keep town services/quest board; do not substitute scenario coordinates or invented NPC positions.',
        neededEvidence='Original town NPC spawn list, authenticated packet capture, or identifiable original screenshot/recording with map coordinates')
    out=ROOT/'닥세_리소스/분석결과/카오시아_NPC_배치검증.json';out.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(f'PASS Kaosia source audit: {len(copies)} identical map copies, {len(archives)} scenario archives, {len(placement_files)} SET/POS files; 2 dialogue/art candidates; 0 qualified original NPC placements.')
if __name__=='__main__':main()
