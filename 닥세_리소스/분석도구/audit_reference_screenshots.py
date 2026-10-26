"""Inventory and evidence checks for all user-provided reference images.
The observations below are manual visual reviews; assertions only check their
links to repository sources. Not OCR, game execution, or a Unity screen test.
"""
from pathlib import Path
from PIL import Image
import csv,json,hashlib,re
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'분석/Saver_Late/screenshots'
review={
'darksaver_overworld.jpg':('실제 전투 화면','키스라Lv1·HP450/450·MP140/150·공격126·방어55. 상태창의 중앙은 본체 그림이며 얼굴 초상이 아니다.','KISRA_HP;STATUS_BODY;MAP_UI'),
'darksaver_combat_magic.jpg':('실제 전투 화면','헤케토HP1454/1500·공격1300·방어600, 좌측 상태창/우측 미니맵, 격자와 광역 번개. 단일 화면으로 마법ID/비용/타이밍 확정 불가.','HEKETO;MAP_UI'),
'untitled.png':('실제 상태창 잘라내기','유니콘나이트Lv11,HP370/420·MP29/44. 외형/이름은129와 대응. 장비/레벨을 분리하지 않고 공격255·방어156을 기본값으로 쓰지 않음.','STATUS_BODY;UNICORN'),
'ĸó0.jpg':('실제 목록·월드 화면','가문명단/직업/계위 목록과 채팅. 표의 수치를 캐릭터 레벨로 추정하지 않음. 서버/가문 기능은 싱글판 복원 범위와 구분.','FAMILY_SCOPE'),
'ĸó1.jpg':('실제 창고·장비 화면','오른쪽 가판대 안 직원810 외형과2510 실내가 강하게 일치. 좌측 가판대는 창에 가려져 직원 유무 확인 불가.','WAREHOUSE_POSITION;COUNTER_RULE'),
'ĸó2.jpg':('실제 필드·장비 화면','케이시르 계곡 표식이 보이는 외부 지형에서 장비 장착 창을 열고 있다. 개인 장비 기능은 마을 NPC 전용이라는 근거가 아님.','FIELD_INVENTORY'),
'ĸó3.jpg':('실제 창고·보관 화면','보관 목록·스크롤·보관료0·확인 표시. 화면만으로 영구 무료/전체 용량/계정 공유/금전 입출금 규칙 확정 불가.','WAREHOUSE_POSITION;STORAGE_POLICY'),
'ĸó4.jpg':('중문 UI 보관 화면','유사 실내의 보관 창과保管费0. 중문 표기를 한국어 후기 클라이언트의 정확한 메뉴명으로 덮어쓰지 않음.','STORAGE_POLICY;VERSION_NAMES'),
'ĸó8.jpg':('실제 아이템 획득 화면','빛의검을 얻었습니다 메시지, 상자·돌벽·붉은 원형 장식은96 맵/이벤트와 대응. 붉은 문양은 MRC 장식에도 존재한다.','MAP96'),
'대련장.png':('실제 월드 화면','원형 건물·게시판·플레이어/채팅이 보인다. 외부 사진만으로101~107 훈련장 내부·부활 정책을 확정하지 않음.','ARENA_SCOPE'),
'시작 캐릭 선택.jpg':('중문 캐릭터 생성 화면','초상3개와직업3개 선택·이름·성별·확인. 원본한국어Wlib/scene24의파이터/나이트/매지션과대조. 원작3선택근거이며 후속 구현에100/101/103선택을 연결함.','STARTER_CHOICES'),
'부르고스성.png':('월드·몬스터 편집 안내 이미지','성 입구와몬스터삽입그림.01내부전체맵사진이아니며크롭/삽입그림에서실제동시배치수/좌표를역산하지않음.','WORLD_GATE_SCOPE'),
'에트나화산.png':('월드·몬스터 편집 안내 이미지','화산외부랜드마크와박쥐괴물삽입그림.03동굴내부의초기몬스터좌표증거가아님.','WORLD_GATE_SCOPE'),
'자모라.png':('월드·몬스터 편집 안내 이미지','다리랜드마크와늑대괴물삽입그림.외부출입구사진과02내부다리이벤트좌표를구분.','WORLD_GATE_SCOPE'),
'클래스 표.png':('직업 계통 안내표','10계열과상위통합직업/10단계존재.그랜드스워드/그랜드아처/그랜드메이지는후기ATR170~172의그랜드소드/그랜드보우/그랜드매직과표기차이.','VERSION_NAMES'),
'닥세_시나리오_1~4.png':('시나리오1~4안내','02표제자모라계곡/설명자모라요새.03에트나동굴,04알카디아.일러스트는SET/POS전체목록이아님.','SCENARIO_ALIASES'),
'닥세_시나리오_5~8.png':('시나리오5~8안내','05국경,06비렌시아,07신전안,08사군토항구.06의현재제목이름없는마을과차이.신전명도표기차이를별도기록.','SCENARIO_ALIASES'),
'닥세_시나리오_9~12.png':('시나리오9~12안내','10나이아드/11카론/12만타코라스처치와원문스크립트의필수대상이대응.초기전체편성/보상표는아님.','CLEAR_CORROBORATION'),
'닥세_시나리오_13~16.png':('시나리오13~16안내','14대장몬스터모두처치,15마기,16바르바투.14스크립트4대상과대응하지만현재4와일드독은단기대체편성.','ROSTER14;CLEAR_CORROBORATION'),
'닥세_시나리오_17~20.png':('시나리오17~20안내','17비공정탑승,18성안진입,19네상자중하나열기,20페피스트처치.17은위치조건완료와대응하며전멸조건으로읽지않음.','CLEAR_CORROBORATION;NOT_ALL_PLAYABLE'),
'닥세_시나리오_21~24.png':('시나리오21~24안내','아이스캐슬/플레임캐슬/부서진성/절벽성.이안내그림이현재각맵의플레이연결완료를뜻하지않음.','NOT_ALL_PLAYABLE'),
'닥세_시나리오_25~28.png':('시나리오25~28안내','암흑성/달표면/사막/교회안내.설명에나오는적과지형으로정확한전체초기편성/원작좌표를확정하지않음.','NOT_ALL_PLAYABLE'),
'닥세_시나리오_29~32.png':('시나리오29~32안내','아볼로니아/얼음성/화산/악마성안내.서사표기와원본표의이름/보상을구분하며전체플레이구현으로계산하지않음.','NOT_ALL_PLAYABLE')}
# Coverage counts source files separately from byte-identical duplicates.
paths=[p for folder in [ROOT/'닥세_리소스',ROOT/'닥세_리소스/스크린샷'] for p in folder.iterdir() if p.is_file() and p.suffix.lower() in ['.png','.jpg','.jpeg']]
groups={}
for p in paths:groups.setdefault(hashlib.sha256(p.read_bytes()).hexdigest(),[]).append(p)
assert len(paths)==25 and len(groups)==23
assert {p.name for p in paths}==set(review)
rows=[]
for sha,files in groups.items():
 p=next((p for p in files if p.parent==ROOT/'닥세_리소스'),files[0]);category,observation,findings=review[p.name]
 rows.append(dict(file=str(p.relative_to(ROOT)),aliases=[str(q.relative_to(ROOT))for q in files if q!=p],sha256=sha,size=list(Image.open(p).size),kind=category,manualVisualReview=True,observation=observation,findings=findings.split(';')))
jobs={int(r['직업']):r for r in csv.DictReader((ROOT/'분석/Saver_Late/unity_catalogs/jobs.csv').open(encoding='utf-8-sig'))};monsters={int(r['직업']):r for r in csv.DictReader((ROOT/'분석/Saver_Late/unity_catalogs/monsters.csv').open(encoding='utf-8-sig'))}
assert [jobs[i]['이름']for i in [100,101,103]]==['파이터','나이트','매지션']
assert monsters[701]['체력수']=='300' and monsters[701]['공력수']=='126' and monsters[701]['방력수']=='55'
scenario01=json.loads((ROOT/'Assets/_Game/Resources/Scenarios/01/Content.json').read_text());assert next(u for u in scenario01['units']if u['id']==700)['hp']==300
script=(ROOT/'분석/Saver_Late/decoded/map/96set.arc/96.evt').read_text();assert 'GETITEM 1515' in script and '빛의검을 얻었습니다.' in script
s14=json.loads((ROOT/'Assets/_Game/Resources/Scenarios/14/Content.json').read_text());s17=json.loads((ROOT/'Assets/_Game/Resources/Scenarios/17/Content.json').read_text())
e14=next(e for e in s14['events']if e['id']==99);targets={int(c['args'][0])for c in e14['conditions']if c['name']=='CHARDEAD'};assert targets=={700,710,720,730};assert {u['job']for u in s14['units']if u['id']in targets}=={301}
assert {c['name']for c in next(e for e in s17['events']if e['id']==99)['conditions']}=={'CHARPOS'}
map_data=json.loads((ROOT/'Assets/_Game/Resources/Interiors/2510/Map.json').read_text());assert map_data['attributes'][4*20+15]==9
report=dict(sourceFiles=len(paths),uniqueImages=len(groups),duplicateFiles=len(paths)-len(groups),reviewedUniqueImages=len(rows),manualReview=True,unityPlay=False,images=rows,evidenceChecks={'nativeStarterLabels':[jobs[i]['이름']for i in [100,101,103]],'kisraTableHp':300,'kisraScreenshotMaxHp':450,'kisraBakedSourceHp':300,'kisraCombatMaximumHp':450,'heketoTableHp':int(monsters[484]['체력수']),'unicornTableHpMp':[int(jobs[129][k])for k in ['체력수','마력수']],'scenario14RequiredTargets':sorted(targets),'scenario14CurrentTargetJobs':[301],'scenario17ClearCondition':'CHARPOS','screenshot8Script':'96set.arc/96.evt GETITEM1515','warehouseVisibleSpriteTopLeftCellAttribute':9},scope='All source reference screenshots/guides in resource root and screenshot folder. Excludes generated analysis renders, extracted texture/sprite sheets, and Unity QA captures. Native file evidence, authored implementation differences, and unconfirmed screenshot versions remain separate.')
# Export inspectable evidence using the existing native renderer/decoders.
import sys
sys.path.insert(0,str(Path(__file__).parent));sys.path.insert(0,str(ROOT/'Tools/MapChecks'))
from analyze_storage_screenshots import render_ui
import world_asset_baker as native
OUT.mkdir(exist_ok=True)
render_ui(24).save(OUT/'Wlib_scene24_starter.png')
Image.open(ROOT/'닥세_리소스/darksaver_overworld.jpg').crop((514,192,640,408)).resize((378,648)).save(OUT/'kisra_status_crop.png')
m=native.load_map('96');w,h=m['width'],m['height'];canvas=Image.new('RGBA',(w*32,h*32),(0,0,0,255))
for i,(rid,group,tile) in enumerate(zip(m['a'],m['b'],m['tiles'])):
 canvas.alpha_composite(native.frame(rid,group,0),(i%w*32,i//w*32))
 if tile[1]!=255:
  g=native.resource(tile[0])['groups'][tile[1]];canvas.alpha_composite(native.frame(tile[0],tile[1],g['tiles'][tile[2]]['frames'][0],transparent=True),(i%w*32,i//w*32))
for obj in m['sprites']:canvas.alpha_composite(native.frame(obj['resource'],obj['index'],obj['frame'],True,True),(obj['x']*32,obj['y']*32))
assert any(o['resource']==25 and o['index']==12 and (o['x'],o['y'])==(12,10) for o in m['sprites'])
canvas.thumbnail((640,640));canvas.save(OUT/'Map96_static.png')
report['evidenceChecks']['map96CircleDecoration']=dict(resource=25,index=12,frame=0,sourceTopLeft=[12,10])
report['evidenceChecks']['upperStaffVisualRegionIsNotLogicalSpawnCell']=True
report['evidenceChecks']['tier10Labels']=[dict(job=i,guide=guide,lateTable=jobs[i]['이름'])for i,guide in [(170,'그랜드스워드'),(171,'그랜드아처'),(172,'그랜드메이지')]]
OUT.mkdir(exist_ok=True);(OUT/'all_reference_review.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps(dict(sourceFiles=len(paths),uniqueImages=len(groups),reviewed=len(rows),duplicates=len(paths)-len(groups),sourceEvidenceAssertions='passed',unityPlay=False)))
