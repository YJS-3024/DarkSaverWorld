using System;
using DarkSaver.Original.Maps;
using DarkSaver.Original.World;
using DarkSaver.Original.Campaign;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Original.Editor.Scenarios
{
    public sealed partial class ScenarioEditorWindow
    {
        private bool dragging, dragRemembered, adding;
        private int dragX, dragY, hoverX = -1, hoverY = -1;
        private Vector2 dragStart;
        private Rect canvasArea;
        private GUIStyle small, centered;
        private int visibleActors,missingActorPictures;
        private const float DefaultInspectorWidth=264,MinimumInspectorWidth=240;
        private const string InspectorWidthPreference="DarkSaver.Original.ScenarioEditor.InspectorWidth";
        [SerializeField] private float inspectorWidth=DefaultInspectorWidth;
        private bool inspectorWidthLoaded;
        private float inspectorResizeMouseX,inspectorResizeWidth,inspectorResizePreviousWidth;
        private void OnGUI()
        {
            if (small == null) small = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } };
            if (centered == null) centered = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white }, fontSize = 10 };
            DrawHeader();
            if (source == null || document == null)
            {
                DrawMaps(new Rect(0,36,220,position.height-60));
                EditorGUI.HelpBox(new Rect(234,48,Mathf.Max(100,position.width-248),150),"원본 자료를 열지 못했습니다. 다른 지도를 선택하거나 오류를 확인하세요.\n"+loadError,MessageType.Error);
                if(GUI.Button(new Rect(234,205,120,24),"오류 복사"))EditorGUIUtility.systemCopyBuffer=loadError;
                if(GUI.Button(new Rect(360,205,160,24),"목록/지도 다시 읽기"))OnEnable();
                DispatchQueuedSave();return;
            }
            mode=Mathf.Clamp(mode,0,2);
            if(float.IsNaN(zoom)||float.IsInfinity(zoom)||zoom<.125f||zoom>4||float.IsNaN(pan.x)||float.IsInfinity(pan.x)||float.IsNaN(pan.y)||float.IsInfinity(pan.y))Fit();
            const float left = 220;
            float right=ResizeInspector(left);
            var list = new Rect(0,36,left,position.height-60);
            var inspector = new Rect(position.width-right,36,right,position.height-60);
            canvasArea = new Rect(left+1,36,Mathf.Max(200,position.width-left-right-8),position.height-60);
            DrawMaps(list); DrawInspector(inspector);
            int nextMode = GUI.Toolbar(new Rect(canvasArea.x+8,canvasArea.y+5,330,24),mode,new[]{"전투 배치 편집","연출 배치 보기","맵 입구 연결"});
            if (nextMode != mode) { mode = nextMode; selected = -1; adding = false; dragging = false;pickNpc=pickSpawn=dragGateway=sizingGateway=pickBattleEntry=false; }
            bool compactActions=canvasArea.width<466;
            using (new EditorGUI.DisabledScope(mode != 0 || loadError.Length > 0))
            {
                adding = GUI.Toggle(new Rect(canvasArea.x+(compactActions?8:350),canvasArea.y+(compactActions?34:7),100,22),adding,"클릭하여 추가");if(adding)pickBattleEntry=false;
            }
            viewport = new Rect(canvasArea.x+8,canvasArea.y+36,canvasArea.width-16,canvasArea.height-44);
            if(compactActions){viewport.y+=26;viewport.height-=26;}
            if (mode == 1) { DrawScript(); viewport.y += 58; viewport.height -= 58; }
            if(fitRequested)ApplyFit();
            ProcessInput(); DrawMap();
            EditorGUI.DrawRect(new Rect(0,position.height-24,position.width,24),new Color(.12f,.14f,.17f));
            GUI.Label(new Rect(10,position.height-22,position.width-20,20),
                new[]{"전투 보기","연출 보기","맵 입구 보기"}[mode]+" | 전투 "+document.units.Length+" · 화면 범위 "+visibleActors+" · 그림 미확보 "+missingActorPictures+" | "+(hoverX >= 0 ? "타일 " + hoverX + "," + hoverY + "  ·  " : "") + Mathf.RoundToInt(zoom*100) + "% | " + message, EditorStyles.miniLabel);
            DispatchQueuedSave();
            if (loadError.Length > 0)
            {
                EditorGUI.HelpBox(new Rect(canvasArea.x+14,canvasArea.y+100,canvasArea.width-28,130),loadError,MessageType.Error);
                if(GUI.Button(new Rect(canvasArea.x+14,canvasArea.y+234,110,24),"오류 복사"))EditorGUIUtility.systemCopyBuffer=loadError;
                if(failedMapKey.Length>0&&GUI.Button(new Rect(canvasArea.x+130,canvasArea.y+234,160,24),"실패한 지도 다시 시도"))LoadMap(failedMapKey);
            }
        }
        private float ResizeInspector(float left)
        {
            if(!inspectorWidthLoaded){inspectorWidth=EditorPrefs.GetFloat(InspectorWidthPreference,inspectorWidth);inspectorWidthLoaded=true;}
            float maximum=Mathf.Max(MinimumInspectorWidth,position.width-left-360-8);
            if(float.IsNaN(inspectorWidth)||float.IsInfinity(inspectorWidth))inspectorWidth=DefaultInspectorWidth;
            float width=Mathf.Clamp(inspectorWidth,MinimumInspectorWidth,maximum);
            int control=GUIUtility.GetControlID(0x44534957,FocusType.Passive);
            var rect=new Rect(position.width-width-6,36,6,position.height-60);var e=Event.current;
            EditorGUIUtility.AddCursorRect(rect,MouseCursor.ResizeHorizontal,control);
            if(e.type==EventType.MouseDown&&e.button==0&&rect.Contains(e.mousePosition))
            {
                GUI.FocusControl(null);
                if(e.clickCount==2){GUIUtility.hotControl=0;inspectorWidth=DefaultInspectorWidth;EditorPrefs.SetFloat(InspectorWidthPreference,inspectorWidth);}
                else{GUIUtility.hotControl=control;inspectorResizeMouseX=e.mousePosition.x;inspectorResizeWidth=width;inspectorResizePreviousWidth=inspectorWidth;}
                e.Use();Repaint();
            }
            if(GUIUtility.hotControl==control)
            {
                if(e.type==EventType.MouseDrag)
                {inspectorWidth=Mathf.Clamp(inspectorResizeWidth+inspectorResizeMouseX-e.mousePosition.x,MinimumInspectorWidth,maximum);e.Use();Repaint();}
                else if(e.type==EventType.MouseUp)
                {GUIUtility.hotControl=0;EditorPrefs.SetFloat(InspectorWidthPreference,inspectorWidth);e.Use();}
                else if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape)
                {inspectorWidth=inspectorResizePreviousWidth;GUIUtility.hotControl=0;e.Use();Repaint();}
            }
            width=Mathf.Clamp(inspectorWidth,MinimumInspectorWidth,maximum);rect.x=position.width-width-6;
            EditorGUI.DrawRect(rect,new Color(.18f,.22f,.28f));
            EditorGUI.DrawRect(new Rect(rect.center.x-1,rect.center.y-16,2,32),GUIUtility.hotControl==control?Color.cyan:new Color(.45f,.55f,.65f));
            GUI.Label(rect,new GUIContent("","드래그: 오른쪽 메뉴 너비 조절 / 두 번 클릭: 기본 너비 / Esc: 조절 취소"));
            return width;
        }
        private void DrawHeader()
        {
            EditorGUI.DrawRect(new Rect(0,0,position.width,35),new Color(.14f,.17f,.21f));
            GUI.Label(new Rect(12,7,Mathf.Max(100,position.width-350),24),"시나리오 편집툴  /  " + Title(mapKey) + (dirty||dirtyGateways ? " * ["+(dirty?"배치":"")+(dirty&&dirtyGateways?" / ":"")+(dirtyGateways?"입구·진입점":"")+" 미저장]" : ""), EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(document == null))
            {
                if (GUI.Button(new Rect(position.width-325,5,65,24),"화면 맞춤")) Fit();
                if (GUI.Button(new Rect(position.width-254,5,65,24),"100%")) { zoom=1; CenterMap(); }
                if (GUI.Button(new Rect(position.width-183,5,78,24),"다시 열기")) {if(mode==2)ReloadGateways();else if(MayLeave())LoadMap(mapKey);}
                using (new EditorGUI.DisabledScope(loadError.Length > 0||mode!=2&&layoutReadOnly&&dirty&&!dirtyGateways))
                    if (GUI.Button(new Rect(position.width-99,5,86,24),mode==2?"입구 저장":"배치 저장")) {QueueEditorSave(mode==2);}
            }
        }
        private void DrawMaps(Rect rect)
        {
            GUILayout.BeginArea(rect,EditorStyles.helpBox);
            GUILayout.Label("원본 맵",EditorStyles.boldLabel);
            search=EditorGUILayout.TextField(search,EditorStyles.toolbarSearchField);
            GUILayout.BeginHorizontal();directMapKey=EditorGUILayout.TextField(directMapKey);
            if(GUILayout.Button("번호로 열기",GUILayout.Width(82)))
            {
                string requested=int.TryParse(directMapKey,out var number)?keys.FirstOrDefault(k=>int.Parse(k)==number):null;
                if(requested==null)message="목록에 있는 맵 번호를 입력하세요.";
                else{SelectMap(requested);GUIUtility.ExitGUI();}
            }
            GUILayout.EndHorizontal();
            var categories=new[]{"전체","시나리오","수련장","대련장","튜토리얼","월드/필드","마을","실내"};
            category=EditorGUILayout.Popup(category,categories.Select(FlatPopupLabel).ToArray());
            mapScroll=EditorGUILayout.BeginScrollView(mapScroll);
            foreach(var key in keys)
            {
                if (category > 0 && Category(key) != categories[category]) continue;
                if (search.Length > 0 && Title(key).IndexOf(search,StringComparison.OrdinalIgnoreCase)<0) continue;
                var old=GUI.backgroundColor;if(key==mapKey)GUI.backgroundColor=new Color(.35f,.65f,1);
                bool clicked=GUILayout.Button(new GUIContent(Title(key),Title(key)),GUILayout.Height(25),GUILayout.Width(Mathf.Max(60,rect.width-26)));GUI.backgroundColor=old;
                if(clicked){SelectMap(key);GUIUtility.ExitGUI();}
            }
            EditorGUILayout.EndScrollView();
            if(GUILayout.Button("맵 이름표 CSV 열기"))AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(BasePath+"Data/Tables/scene_maps.csv"));
            if(GUILayout.Button("맵 이름 새로고침"))RefreshMapNames(true);
            if(mapNamesError.Length>0)EditorGUILayout.HelpBox(mapNamesError,MessageType.Warning);
            GUILayout.Label("CSV display_name 저장 후 자동 반영",EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label("좌표: 원본 좌상단 (0,0)\nunit: 개체 슬롯 / job: 병종 ID",EditorStyles.wordWrappedMiniLabel);
            GUILayout.EndArea();
        }
        private void DrawInspector(Rect rect)
        {
            if(mode==2){DrawGatewayInspector(rect);return;}
            GUILayout.BeginArea(rect,EditorStyles.helpBox);
            inspectorScroll=EditorGUILayout.BeginScrollView(inspectorScroll);
            GUILayout.Label(document.width+" × "+document.height+" 타일",EditorStyles.boldLabel);
            string evidence=source.layout.source=="original-missing"?mapKey+"번 원본 전투 편성 미확보 (SET/POS·원본 목록 항목 없음)":source.layout.source=="original-empty"?"원본 SET/POS의 빈 전투 편성"
                :source.layout.source=="original-catalog-empty"?"검증된 원본 배치 목록의 빈 편성":source.layout.source=="original-catalog"?"검증된 원본 배치 목록 "+source.layout.units.Length+"개 (SET/POS 텍스트 누락)":"원본 SET/POS "+source.layout.units.Length+"개";
            EditorGUILayout.HelpBox(evidence+"\n현재 편집 배치 "+document.units.Length+"개"+(dirty?" · 저장 전":""),source.layout.source=="original-missing"?MessageType.Warning:MessageType.Info);
            if(layoutReadOnly)EditorGUILayout.HelpBox(layoutNotice,MessageType.Warning);
            else if(source.layout.units.Length>0&&document.units.Length==0)EditorGUILayout.HelpBox("원본에는 몬스터가 있으나 현재 사용자 편집 배치가 비어 있습니다. 원본 배치 불러오기로 복원할 수 있습니다.",MessageType.Warning);
            GUILayout.Label("아군 POS "+source.entries.Length+" · 상자 "+source.events.Count(m=>m.kind=="chest")+" · 조건 "+source.events.Count(m=>m.kind=="event"),EditorStyles.miniLabel);
            grid=EditorGUILayout.ToggleLeft("격자 표시",grid);labels=EditorGUILayout.ToggleLeft("ID / 이름 표시",labels);
            groups=EditorGUILayout.ToggleLeft("unit 5슬롯 묶음 테두리",groups);
            entries=EditorGUILayout.ToggleLeft("아군 POS 슬롯 / 로컬 진입점",entries);
            chests=EditorGUILayout.ToggleLeft("상자 위치",chests);quests=EditorGUILayout.ToggleLeft("이벤트 조건 위치",quests);
            DrawBattleEntry();
            if(mode==0)DrawNativeAiRules();
            GUILayout.Space(6);
            using(new EditorGUI.DisabledScope(mode!=0||loadError.Length>0||layoutReadOnly))
            {
                GUILayout.Label("몬스터 추가",EditorStyles.boldLabel);
                jobSearch=EditorGUILayout.TextField("병종 검색",jobSearch);allJobs=EditorGUILayout.ToggleLeft("전체 병종 포함",allJobs);
                var choices=jobs.Values.Where(j=>(allJobs||j.category=="monster")&&(jobSearch.Length==0||j.ToString().IndexOf(jobSearch,StringComparison.OrdinalIgnoreCase)>=0)).OrderBy(j=>j.id).ToArray();
                if(choices.Length>0)
                {
                    int at=Array.FindIndex(choices,j=>j.id==addJob);if(at<0)at=0;
                    int next=EditorGUILayout.Popup(at,choices.Select(j=>FlatPopupLabel(j.ToString())).ToArray());addJob=choices[next].id;
                    if(!picturePaths.ContainsKey(choices[next].picture))EditorGUILayout.HelpBox("이 병종의 원본 그림이 없어 ?와 ID로 표시합니다.",MessageType.Warning);
                    if(GUILayout.Button("뷰 중앙에 추가")){var center=viewport.size*.5f;AddAt(NativeMapDecoder.Grid(center.x,pan.x,zoom,document.width),NativeMapDecoder.Grid(center.y,pan.y,zoom,document.height));}
                }
                else GUILayout.Label("일치하는 병종 없음",EditorStyles.miniLabel);
                GUILayout.Space(5);GUILayout.Label("선택 개체",EditorStyles.boldLabel);
                var unit=document.units.FirstOrDefault(u=>u.unit==selected);
                if(unit!=null)
                {
                    EditorGUI.BeginChangeCheck();
                    int id=EditorGUILayout.DelayedIntField("unit 슬롯",unit.unit), job=EditorGUILayout.DelayedIntField("job 병종",unit.job);
                    int x=EditorGUILayout.DelayedIntField("X",unit.x),y=EditorGUILayout.DelayedIntField("Y",unit.y);
                    if(EditorGUI.EndChangeCheck())
                    {
                        if(id<400||document.units.Any(u=>u!=unit&&u.unit==id)||!jobs.ContainsKey(job)||x<0||x>=document.width||y<0||y>=document.height) message="중복 슬롯/병종/좌표를 확인하세요.";
                        else {Remember();unit.unit=id;unit.job=job;unit.x=x;unit.y=y;selected=id;}
                    }
                    GUILayout.Label(jobs.TryGetValue(unit.job,out var j)?j.name:"알 수 없는 병종");
                    if(jobs.TryGetValue(unit.job,out j)&&!picturePaths.ContainsKey(j.picture))GUILayout.Label("원본 그림 미확보 · ? 표시",EditorStyles.miniLabel);
                    GUILayout.Label("SET "+unit.setLine+"행 / POS "+unit.posLine+"행",EditorStyles.miniLabel);
                    if(GUILayout.Button("선택 몬스터 삭제"))DeleteSelected();
                }
                else GUILayout.Label("지도 또는 목록에서 선택",EditorStyles.miniLabel);
                GUILayout.BeginHorizontal();using(new EditorGUI.DisabledScope(undo.Count==0))if(GUILayout.Button("되돌리기"))History(true);
                using(new EditorGUI.DisabledScope(redo.Count==0))if(GUILayout.Button("다시 실행"))History(false);GUILayout.EndHorizontal();
            }
            using(new EditorGUI.DisabledScope(mode!=0))if(GUILayout.Button("원본 배치 불러오기"))RestoreOriginalLayout();
            using(new EditorGUI.DisabledScope(document.units.Length==0))
                if(GUILayout.Button("첫 몬스터 위치 보기"))
                {var first=document.units.OrderBy(u=>u.unit).First();mode=0;selected=first.unit;zoom=Mathf.Max(.5f,zoom);FocusTile(first.x,first.y);}
            int overlaps=document.units.GroupBy(u=>(u.x,u.y)).Count(g=>g.Count()>1);
            if(overlaps>0)EditorGUILayout.HelpBox("같은 타일에 겹친 위치 "+overlaps+"곳. 원본 좌표는 자동 정렬하지 않습니다.",MessageType.Warning);
            GUILayout.Space(6);GUILayout.Label("전투 개체 목록",EditorStyles.boldLabel);
            unitScroll=EditorGUILayout.BeginScrollView(unitScroll,GUILayout.Height(140));
            foreach(var unit in document.units.OrderBy(u=>u.unit))
                if(GUILayout.Button(unit.unit+" · "+(jobs.TryGetValue(unit.job,out var j)?j.name:unit.job.ToString())+" ("+unit.x+","+unit.y+")",EditorStyles.miniButton))
                {selected=unit.unit;mode=0;FocusTile(unit.x,unit.y);}
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("몬스터 저장은 Editor/ScenarioLayouts를 변경합니다. 수련장 101~107은 테스트 전투용 Resources에도 함께 내보냅니다. 진입점은 별도 UserGateways.json에 저장하며 실제 입장에 적용합니다. 원본 SET/POS·씬은 변경하지 않습니다. 시나리오 몬스터 스폰은 변경하지 않습니다.\n연출은 명령행별 위치 참고이며 대본 실행/분기 판정이 아닙니다.",MessageType.None);
            EditorGUILayout.EndScrollView();GUILayout.EndArea();
        }
        private void SetBattleEntry(int x,int y,bool enabled)
        {
            try
            {
                var entry=new ScenarioEntryPoint{map=int.Parse(mapKey),mapHash=MapInfo(mapKey).hash,x=x,y=y,enabled=enabled,useJob=entryJob!=0,job=entryJob};
                var candidate=gatewayDocument.Copy();candidate.entryPoints=(candidate.entryPoints??Array.Empty<ScenarioEntryPoint>()).Where(p=>p.map!=entry.map||p.useJob!=entry.useJob||entry.useJob&&p.job!=entry.job).Concat(new[]{entry}).ToArray();
                ScenarioGatewayData.Validate(candidate,gatewayCatalog);
                if(enabled&&!ScenarioGatewayData.EntryLocationAllowed(gatewayWorld,entry))throw new System.IO.InvalidDataException("통행 가능한 빈 칸을 선택하세요. 직원/원본 몬스터와 겹칠 수 없습니다.");
                RememberGateway();gatewayDocument=candidate;entries=true;message="지휘관 진입 위치 변경 · 진입점 저장 필요";Repaint();
            }
            catch(Exception e){message=e.Message;}
        }
        private void DrawBattleEntry()
        {
            GUILayout.Space(6);GUILayout.Label("지휘관 진입 위치",EditorStyles.boldLabel);
            if(gatewaySaveError.Length>0)EditorGUILayout.HelpBox(gatewaySaveError,MessageType.Error);
            else if(gatewaySaveNotice.Length>0)GUILayout.Label(gatewaySaveNotice,EditorStyles.wordWrappedMiniLabel);
            int id=int.Parse(mapKey);var target=gatewayWorld?.Map(id);
            bool supported=target!=null&&(target.training||gatewayWorld.campaign?.Map(id)?.kind=="scenario");
            var options=(gatewayWorld?.combatCatalog?.jobs??Array.Empty<OriginalCombatJob>()).Where(j=>j.job>=100&&j.job<200).OrderBy(j=>j.job).ToArray();
            var ids=new[]{0}.Concat(options.Select(j=>j.job)).ToArray();
            var names=new[]{"공통 진입점"}.Concat(options.Select(j=>j.name+" ["+j.job+"] 전용")).ToArray();
            EditorGUI.BeginChangeCheck();int choice=EditorGUILayout.Popup("적용 직업",Math.Max(0,Array.IndexOf(ids,entryJob)),names);
            if(EditorGUI.EndChangeCheck()){entryJob=ids[choice];pickBattleEntry=false;}
            var entry=ScenarioGatewayData.EditingEntry(gatewayDocument,id,entryJob);
            if(entryJob!=0&&!ScenarioGatewayData.HasEntryTerrain(gatewayWorld,id,entryJob))
                EditorGUILayout.HelpBox("현재 맵 데이터에 이 직업의 원본 병종이 통행할 지형이 없습니다. 좌표만 바꾸어 해결할 수 없습니다.",MessageType.Warning);
            using(new EditorGUI.DisabledScope(mode!=0||!supported||loadError.Length>0))
            {
                if(GUILayout.Button(pickBattleEntry?"지도에서 칸을 클릭하세요 (취소)":"지도에서 진입 위치 지정")){pickBattleEntry=!pickBattleEntry;adding=dragging=false;}
                if(entry!=null)
                {
                    EditorGUI.BeginChangeCheck();bool enabled=EditorGUILayout.Toggle("사용자 진입점 사용",entry.enabled);
                    int x=EditorGUILayout.DelayedIntField("진입 X",entry.x),y=EditorGUILayout.DelayedIntField("진입 Y",entry.y);
                    if(EditorGUI.EndChangeCheck())SetBattleEntry(x,y,enabled);
                    if(GUILayout.Button("진입 위치 보기")){entries=true;FocusTile(entry.x,entry.y);}
                    if(GUILayout.Button("사용자 진입점 삭제")){RememberGateway();gatewayDocument.entryPoints=gatewayDocument.entryPoints.Where(p=>p.map!=id||p.useJob!=(entryJob!=0)||entryJob!=0&&p.job!=entryJob).ToArray();pickBattleEntry=false;}
                }
                else GUILayout.Label(entryJob!=0&&ScenarioGatewayData.SelectEntry(gatewayDocument,id,entryJob)!=null?"전용 진입점 미설정 · 공통 진입점 사용":"미설정 · 기존 입구의 도착 좌표 사용",EditorStyles.wordWrappedMiniLabel);
                if(entryJob!=0)EditorGUILayout.HelpBox("이 직업의 원본 병종으로 통행 가능한 칸을 직접 지정합니다. 공통/다른 직업의 진입점은 유지됩니다.",MessageType.None);
                using(new EditorGUI.DisabledScope(!dirtyGateways))if(GUILayout.Button("진입점·입구 저장"))QueueEditorSave(true);
                GUILayout.BeginHorizontal();
                using(new EditorGUI.DisabledScope(gatewayUndo.Count==0))if(GUILayout.Button("진입점/입구 되돌리기"))GatewayHistory(true);
                using(new EditorGUI.DisabledScope(gatewayRedo.Count==0))if(GUILayout.Button("다시 실행"))GatewayHistory(false);
                GUILayout.EndHorizontal();
            }
            if(!supported)GUILayout.Label("현재 실행 가능한 시나리오·수련장에서 설정합니다.",EditorStyles.wordWrappedMiniLabel);
            GUILayout.Label("원본 POS는 보존합니다. 저장한 사용 중 진입점은 실제 입장 시 기존 도착 좌표보다 우선하며, 기존 저장 위치/서버 유닛 좌표는 변경하지 않습니다.",EditorStyles.wordWrappedMiniLabel);
        }
        private void DrawScript()
        {
            var rect=new Rect(canvasArea.x+10,viewport.y+1,canvasArea.width-20,24);
            if(source.scripts.Length==0){GUI.Label(rect,"연출 대본 없음",EditorStyles.miniLabel);return;}
            int next=EditorGUI.Popup(new Rect(rect.x,rect.y,155,20),scriptIndex,source.scripts.Select(System.IO.Path.GetFileName).ToArray());
            if(next!=scriptIndex){scriptIndex=next;ReadScript();}
            int line=EditorGUI.IntSlider(new Rect(rect.x+160,rect.y,rect.width-160,20),scriptLine,0,ScenarioLayoutData.Lines(scriptText).Length);
            if(line!=scriptLine){scriptLine=line;UpdateActors();}
            string text=scriptLine==0?"시작 전":ScenarioLayoutData.Lines(scriptText)[scriptLine-1];
            GUI.Label(new Rect(rect.x,rect.y+23,rect.width,24),scriptLine+"행  "+text,EditorStyles.miniLabel);
        }
        private void Fit()
        {
            fitRequested=true;Repaint();
        }
        private void ApplyFit()
        {
            fitRequested=false;if(document==null)return;float w=viewport.width,h=viewport.height;
            zoom=Mathf.Clamp(Mathf.Min(w/(document.width*32),h/(document.height*32)),.125f,4);pan=new Vector2((w-document.width*32*zoom)/2,(h-document.height*32*zoom)/2);Repaint();
        }
        private void CenterMap()
        {if(document!=null){pan=(viewport.size-new Vector2(document.width*32*zoom,document.height*32*zoom))*.5f;Repaint();}}
        private void FocusTile(int x,int y){pan=viewport.size*.5f-new Vector2((x+.5f)*32*zoom,(y+.5f)*32*zoom);Repaint();}
        private void ProcessInput()
        {
            var e=Event.current;if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape&&pickBattleEntry){pickBattleEntry=false;e.Use();Repaint();}
            bool inside=viewport.Contains(e.mousePosition);var local=e.mousePosition-viewport.position;
            if(inside)
            {hoverX=(int)Math.Floor((local.x-pan.x)/(32*zoom));hoverY=(int)Math.Floor((local.y-pan.y)/(32*zoom));if(hoverX<0||hoverX>=document.width||hoverY<0||hoverY>=document.height)hoverX=hoverY=-1;}
            else hoverX=hoverY=-1;
            if(e.type==EventType.ScrollWheel&&inside)
            {
                float next=(float)NativeMapDecoder.Zoom(zoom,e.delta.y);pan=local-(local-pan)*(next/zoom);zoom=next;e.Use();Repaint();
            }
            if(e.type==EventType.MouseDrag&&e.button==2&&inside){pan+=e.delta;e.Use();Repaint();}
            if(e.type==EventType.KeyDown&&GUIUtility.keyboardControl==0)
            {
                if((e.control||e.command)&&e.keyCode==KeyCode.S){QueueEditorSave(mode==2);e.Use();}
                else if(mode==0&&loadError.Length==0&&(e.control||e.command)&&e.keyCode==KeyCode.Z){History(!e.shift);e.Use();}
                else if(mode==0&&loadError.Length==0&&(e.control||e.command)&&e.keyCode==KeyCode.Y){History(false);e.Use();}
                else if(e.keyCode==KeyCode.Delete||e.keyCode==KeyCode.Backspace){DeleteSelected();e.Use();}
            }
            if(loadError.Length>0||layoutReadOnly&&mode!=2&&!pickBattleEntry)return;
            if(mode==2){ProcessGatewayInput(e,inside,local);return;}
            if(mode!=0){dragging=false;return;}
            if(e.type==EventType.MouseDown&&e.button==0&&inside&&hoverX>=0)
            {
                GUI.FocusControl(null);
                if(pickBattleEntry){SetBattleEntry(hoverX,hoverY,true);pickBattleEntry=false;e.Use();return;}
                if(adding){AddAt(hoverX,hoverY);e.Use();return;}
                var hit=document.units.Reverse().FirstOrDefault(u=>u.x==hoverX&&u.y==hoverY);selected=hit?.unit??-1;
                dragging=hit!=null;dragRemembered=false;dragStart=local;if(hit!=null){dragX=hit.x;dragY=hit.y;}e.Use();Repaint();
            }
            if(e.type==EventType.MouseDrag&&e.button==0&&dragging)
            {
                var unit=document.units.FirstOrDefault(u=>u.unit==selected);if(unit==null){dragging=false;return;}
                int x=Mathf.Clamp(dragX+Mathf.RoundToInt((local.x-dragStart.x)/(32*zoom)),0,document.width-1),y=Mathf.Clamp(dragY+Mathf.RoundToInt((local.y-dragStart.y)/(32*zoom)),0,document.height-1);
                if(x!=unit.x||y!=unit.y){if(!dragRemembered){Remember();dragRemembered=true;}unit.x=x;unit.y=y;Repaint();}e.Use();
            }
            if(e.type==EventType.MouseUp&&e.button==0){dragging=false;dragRemembered=false;}
        }
        private Color GroupColor(int id) => Color.HSVToRGB(Mathf.Repeat(id/5*.618034f,1),.55f,1);
        private Rect TileRect(int x,int y) => new Rect(pan.x+x*32*zoom,pan.y+y*32*zoom,32*zoom,32*zoom);
        private void DrawMap()
        {
            visibleActors=missingActorPictures=0;
            EditorGUI.DrawRect(viewport,new Color(.055f,.065f,.08f));GUI.BeginGroup(viewport);
            if(mapTexture!=null)GUI.DrawTexture(new Rect(pan.x,pan.y,mapTexture.width*zoom,mapTexture.height*zoom),mapTexture,ScaleMode.StretchToFill);
            if(grid&&zoom>=.3f)
            {
                var color=new Color(.75f,.85f,1,.15f);float size=32*zoom;
                for(int x=0;x<=document.width;x++){float at=pan.x+x*size;if(at>=0&&at<=viewport.width)EditorGUI.DrawRect(new Rect(at,Mathf.Max(0,pan.y),1,Mathf.Max(0,Mathf.Min(viewport.height,pan.y+document.height*size)-Mathf.Max(0,pan.y))),color);}
                for(int y=0;y<=document.height;y++){float at=pan.y+y*size;if(at>=0&&at<=viewport.height)EditorGUI.DrawRect(new Rect(Mathf.Max(0,pan.x),at,Mathf.Max(0,Mathf.Min(viewport.width,pan.x+document.width*size)-Mathf.Max(0,pan.x)),1),color);}
            }
            if(mode==0)
            {
                if(groups)
                {
                    foreach(var group in document.units.GroupBy(u=>u.unit/5))
                    {
                        var anchor=group.OrderBy(u=>u.unit).First();
                        var low=TileRect(group.Min(u=>u.x),group.Min(u=>u.y));var high=TileRect(group.Max(u=>u.x),group.Max(u=>u.y));
                        var color=GroupColor(anchor.unit);color.a=.55f;Outline(Rect.MinMaxRect(low.x-2,low.y-2,high.xMax+2,high.yMax+2),color,1);
                    }
                }
                foreach(var unit in document.units)DrawActor(unit.unit,unit.job.ToString(),unit.x,unit.y,GroupColor(unit.unit),unit.unit==selected);
                var manual=ScenarioGatewayData.EditingEntry(gatewayDocument,int.Parse(mapKey),entryJob)??ScenarioGatewayData.SelectEntry(gatewayDocument,int.Parse(mapKey),entryJob);
                if(entries&&manual!=null)DrawMarker(new ScenarioMarker{x=manual.x,y=manual.y,kind="local",label="사용자 지휘관 진입점"+(manual.useJob?" [직업 "+manual.job+"]":" [공통]")+(manual.enabled?"":" (미사용)"),file=GatewayPath},manual.enabled?new Color(.2f,1,.9f):Color.gray);
                if(entries)
                {foreach(var entry in source.entries)DrawMarker(entry,new Color(.25f,.8f,1));if(localEntry!=null)DrawMarker(localEntry,new Color(.25f,.8f,1));}
            }
            else if(mode==1)foreach(var actor in actors)DrawActor(actor.unit,actor.job,actor.x,actor.y,new Color(.8f,.6f,1),false);
            foreach(var marker in source.events)if(marker.kind=="chest"?chests:quests)DrawMarker(marker,marker.kind=="chest"?new Color(1,.78f,.18f):new Color(.2f,1,.6f));
            if(mode==2)DrawGateways();
            if(hoverX>=0)Outline(TileRect(hoverX,hoverY),new Color(1,1,1,.5f),1);
            GUI.EndGroup();
        }
        private void DrawActor(int id,string job,int x,int y,Color color,bool isSelected)
        {
            var rect=TileRect(x,y);if(!rect.Overlaps(new Rect(Vector2.zero,viewport.size)))return;
            visibleActors++;
            EditorGUI.DrawRect(rect,new Color(color.r,color.g,color.b,.2f));
            var picture=ActorPicture(job);if(picture!=null)DrawActorPicture(rect,picture);else{missingActorPictures++;GUI.Label(rect,"?",centered);}
            Outline(rect,isSelected?Color.white:color,isSelected?2:1);
            if(labels&&(zoom>=.5f||isSelected))
            {
                string name=int.TryParse(job,out int number)&&jobs.TryGetValue(number,out var info)?info.name:job;
                var label=new Rect(rect.x,rect.y-15,Mathf.Max(60,rect.width),15);EditorGUI.DrawRect(label,new Color(0,0,0,.7f));GUI.Label(label,id+" "+name,small);
            }
        }
        private void DrawMarker(ScenarioMarker marker,Color color)
        {
            var rect=TileRect(marker.x,marker.y);if(!rect.Overlaps(new Rect(Vector2.zero,viewport.size)))return;
            Outline(rect,color,2);var tag=new Rect(rect.x+2,rect.y+2,Mathf.Max(8,rect.width-4),Mathf.Min(14,rect.height-4));
            EditorGUI.DrawRect(tag,new Color(0,0,0,.8f));GUI.Label(tag,new GUIContent(marker.kind=="chest"?"상자":marker.kind=="event"?"조건":marker.kind=="local"?"진입":"아군",marker.label+"\n"+marker.file+":"+marker.line),small);
        }
        private static void Outline(Rect r,Color color,int width)
        {EditorGUI.DrawRect(new Rect(r.x,r.y,r.width,width),color);EditorGUI.DrawRect(new Rect(r.x,r.yMax-width,r.width,width),color);EditorGUI.DrawRect(new Rect(r.x,r.y,width,r.height),color);EditorGUI.DrawRect(new Rect(r.xMax-width,r.y,width,r.height),color);}
    }
}
