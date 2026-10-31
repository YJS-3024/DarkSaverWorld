using System;
using DarkSaver.Original.Maps;
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
        private void OnGUI()
        {
            if (small == null) small = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } };
            if (centered == null) centered = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white }, fontSize = 10 };
            DrawHeader();
            if (source == null || document == null)
            { EditorGUI.HelpBox(new Rect(12,48,position.width-24,90), "원본 자료를 열지 못했습니다. " + loadError, MessageType.Error); return; }
            const float left = 220, right = 264;
            var list = new Rect(0,36,left,position.height-60);
            var inspector = new Rect(position.width-right,36,right,position.height-60);
            canvasArea = new Rect(left+1,36,Mathf.Max(200,position.width-left-right-2),position.height-60);
            DrawMaps(list); DrawInspector(inspector);
            int nextMode = GUI.Toolbar(new Rect(canvasArea.x+8,canvasArea.y+5,330,24),mode,new[]{"전투 배치 편집","연출 배치 보기","NPC 입구 연결"});
            if (nextMode != mode) { mode = nextMode; selected = -1; adding = false; dragging = false;pickNpc=pickSpawn=dragGateway=false; }
            using (new EditorGUI.DisabledScope(mode != 0 || loadError.Length > 0))
            {
                adding = GUI.Toggle(new Rect(canvasArea.x+350,canvasArea.y+7,100,22),adding,"클릭하여 추가");
            }
            viewport = new Rect(canvasArea.x+8,canvasArea.y+36,canvasArea.width-16,canvasArea.height-44);
            if (mode == 1) { DrawScript(); viewport.y += 58; viewport.height -= 58; }
            ProcessInput(); DrawMap();
            EditorGUI.DrawRect(new Rect(0,position.height-24,position.width,24),new Color(.12f,.14f,.17f));
            GUI.Label(new Rect(10,position.height-22,position.width-20,20),
                (hoverX >= 0 ? "타일 " + hoverX + "," + hoverY + "  ·  " : "") + Mathf.RoundToInt(zoom*100) + "%  |  휠: 확대/축소  ·  가운데 버튼 드래그: 이동  ·  좌클릭/드래그: 선택/배치  ·  Delete: 삭제  |  " + message, EditorStyles.miniLabel);
            if (loadError.Length > 0) EditorGUI.HelpBox(new Rect(canvasArea.x+14,canvasArea.y+100,canvasArea.width-28,90),loadError,MessageType.Error);
        }
        private void DrawHeader()
        {
            EditorGUI.DrawRect(new Rect(0,0,position.width,35),new Color(.14f,.17f,.21f));
            GUI.Label(new Rect(12,7,245,24),"시나리오 편집툴  /  " + Title(mapKey) + (dirty||dirtyGateways ? " *" : ""), EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(document == null))
            {
                if (GUI.Button(new Rect(position.width-325,5,65,24),"화면 맞춤")) Fit();
                if (GUI.Button(new Rect(position.width-254,5,65,24),"100%")) { zoom=1; CenterMap(); }
                if (GUI.Button(new Rect(position.width-183,5,78,24),"다시 열기")) {if(mode==2)ReloadGateways();else if(MayLeave())LoadMap(mapKey);}
                using (new EditorGUI.DisabledScope(loadError.Length > 0))
                    if (GUI.Button(new Rect(position.width-99,5,86,24),mode==2?"입구 저장":"배치 저장")) {if(mode==2)SaveGateways();else Save();}
            }
        }
        private void DrawMaps(Rect rect)
        {
            GUILayout.BeginArea(rect,EditorStyles.helpBox);
            GUILayout.Label("원본 맵",EditorStyles.boldLabel);
            search=EditorGUILayout.TextField(search,EditorStyles.toolbarSearchField);
            category=EditorGUILayout.Popup(category,new[]{"전체","시나리오","수련장","대련장","튜토리얼","월드/필드"});
            mapScroll=EditorGUILayout.BeginScrollView(mapScroll);
            foreach(var key in keys)
            {
                if (category > 0 && Category(key) != new[]{"","시나리오","수련장","대련장","튜토리얼","월드/필드"}[category]) continue;
                if (search.Length > 0 && Title(key).IndexOf(search,StringComparison.OrdinalIgnoreCase)<0) continue;
                var old=GUI.backgroundColor;if(key==mapKey)GUI.backgroundColor=new Color(.35f,.65f,1);
                bool clicked=GUILayout.Button(Title(key),GUILayout.Height(25));GUI.backgroundColor=old;
                if(clicked){SelectMap(key);GUIUtility.ExitGUI();}
            }
            EditorGUILayout.EndScrollView();
            GUILayout.Label("좌표: 원본 좌상단 (0,0)\nunit: 개체 슬롯 / job: 병종 ID",EditorStyles.wordWrappedMiniLabel);
            GUILayout.EndArea();
        }
        private void DrawInspector(Rect rect)
        {
            if(mode==2){DrawGatewayInspector(rect);return;}
            GUILayout.BeginArea(rect,EditorStyles.helpBox);
            inspectorScroll=EditorGUILayout.BeginScrollView(inspectorScroll);
            GUILayout.Label(document.width+" × "+document.height+" 타일",EditorStyles.boldLabel);
            string evidence=source.layout.source=="original-missing"?"원본 전투 편성 미확보":source.layout.source=="original-empty"?"원본 SET/POS의 빈 전투 편성":"원본 SET/POS "+source.layout.units.Length+"개";
            EditorGUILayout.HelpBox(evidence+"\n현재 편집 배치 "+document.units.Length+"개"+(dirty?" · 저장 전":""),source.layout.source=="original-missing"?MessageType.Warning:MessageType.Info);
            GUILayout.Label("아군 POS "+source.entries.Length+" · 상자 "+source.events.Count(m=>m.kind=="chest")+" · 조건 "+source.events.Count(m=>m.kind=="event"),EditorStyles.miniLabel);
            grid=EditorGUILayout.ToggleLeft("격자 표시",grid);labels=EditorGUILayout.ToggleLeft("ID / 이름 표시",labels);
            groups=EditorGUILayout.ToggleLeft("unit 10단위 묶음 테두리",groups);
            entries=EditorGUILayout.ToggleLeft("아군 POS 슬롯 / 로컬 진입점",entries);
            chests=EditorGUILayout.ToggleLeft("상자 위치",chests);quests=EditorGUILayout.ToggleLeft("이벤트 조건 위치",quests);
            GUILayout.Space(6);
            using(new EditorGUI.DisabledScope(mode!=0||loadError.Length>0))
            {
                GUILayout.Label("몬스터 추가",EditorStyles.boldLabel);
                jobSearch=EditorGUILayout.TextField("병종 검색",jobSearch);allJobs=EditorGUILayout.ToggleLeft("전체 병종 포함",allJobs);
                var choices=jobs.Values.Where(j=>(allJobs||j.category=="monster")&&(jobSearch.Length==0||j.ToString().IndexOf(jobSearch,StringComparison.OrdinalIgnoreCase)>=0)).OrderBy(j=>j.id).ToArray();
                if(choices.Length>0)
                {
                    int at=Array.FindIndex(choices,j=>j.id==addJob);if(at<0)at=0;
                    int next=EditorGUILayout.Popup(at,choices.Select(j=>j.ToString()).ToArray());addJob=choices[next].id;
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
                if(GUILayout.Button("원본 배치 불러오기")){Remember();document=source.layout.Copy();selected=-1;}
            }
            int overlaps=document.units.GroupBy(u=>(u.x,u.y)).Count(g=>g.Count()>1);
            if(overlaps>0)EditorGUILayout.HelpBox("같은 타일에 겹친 위치 "+overlaps+"곳. 원본 좌표는 자동 정렬하지 않습니다.",MessageType.Warning);
            GUILayout.Space(6);GUILayout.Label("전투 개체 목록",EditorStyles.boldLabel);
            unitScroll=EditorGUILayout.BeginScrollView(unitScroll,GUILayout.Height(140));
            foreach(var unit in document.units.OrderBy(u=>u.unit))
                if(GUILayout.Button(unit.unit+" · "+(jobs.TryGetValue(unit.job,out var j)?j.name:unit.job.ToString())+" ("+unit.x+","+unit.y+")",EditorStyles.miniButton))
                {selected=unit.unit;mode=0;FocusTile(unit.x,unit.y);}
            EditorGUILayout.EndScrollView();
            EditorGUILayout.HelpBox("저장은 Editor/ScenarioLayouts의 사용자 배치만 변경합니다. 원본 SET/POS·씬·로컬 서버 스폰은 변경하지 않습니다.\n연출은 명령행별 위치 참고이며 대본 실행/분기 판정이 아닙니다.",MessageType.None);
            EditorGUILayout.EndScrollView();GUILayout.EndArea();
        }
        private void DrawScript()
        {
            var rect=new Rect(canvasArea.x+10,canvasArea.y+37,canvasArea.width-20,24);
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
            if(document==null)return;float w=Mathf.Max(200,position.width-502),h=Mathf.Max(200,position.height-104-(mode==1?58:0));
            zoom=Mathf.Clamp(Mathf.Min(w/(document.width*32),h/(document.height*32)),.125f,4);pan=new Vector2((w-document.width*32*zoom)/2,(h-document.height*32*zoom)/2);Repaint();
        }
        private void CenterMap()
        {if(document!=null){pan=(viewport.size-new Vector2(document.width*32*zoom,document.height*32*zoom))*.5f;Repaint();}}
        private void FocusTile(int x,int y){pan=viewport.size*.5f-new Vector2((x+.5f)*32*zoom,(y+.5f)*32*zoom);Repaint();}
        private void ProcessInput()
        {
            var e=Event.current;bool inside=viewport.Contains(e.mousePosition);var local=e.mousePosition-viewport.position;
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
                if((e.control||e.command)&&e.keyCode==KeyCode.S){if(mode==2)SaveGateways();else Save();e.Use();}
                else if(mode==0&&loadError.Length==0&&(e.control||e.command)&&e.keyCode==KeyCode.Z){History(!e.shift);e.Use();}
                else if(mode==0&&loadError.Length==0&&(e.control||e.command)&&e.keyCode==KeyCode.Y){History(false);e.Use();}
                else if(e.keyCode==KeyCode.Delete||e.keyCode==KeyCode.Backspace){DeleteSelected();e.Use();}
            }
            if(loadError.Length>0)return;
            if(mode==2){ProcessGatewayInput(e,inside,local);return;}
            if(mode!=0){dragging=false;return;}
            if(e.type==EventType.MouseDown&&e.button==0&&inside&&hoverX>=0)
            {
                GUI.FocusControl(null);
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
        private Color GroupColor(int id) => Color.HSVToRGB(Mathf.Repeat(id/10*.618034f,1),.55f,1);
        private Rect TileRect(int x,int y) => new Rect(pan.x+x*32*zoom,pan.y+y*32*zoom,32*zoom,32*zoom);
        private void DrawMap()
        {
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
                    foreach(var group in document.units.GroupBy(u=>u.unit/10))
                    {
                        var leader=group.FirstOrDefault(u=>u.unit%10==0);if(leader==null)continue;
                        var low=TileRect(group.Min(u=>u.x),group.Min(u=>u.y));var high=TileRect(group.Max(u=>u.x),group.Max(u=>u.y));
                        var color=GroupColor(leader.unit);color.a=.55f;Outline(Rect.MinMaxRect(low.x-2,low.y-2,high.xMax+2,high.yMax+2),color,1);
                    }
                }
                foreach(var unit in document.units)DrawActor(unit.unit,unit.job.ToString(),unit.x,unit.y,GroupColor(unit.unit),unit.unit==selected);
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
            EditorGUI.DrawRect(rect,new Color(color.r,color.g,color.b,.2f));
            var picture=ActorPicture(job);if(picture!=null)GUI.DrawTexture(rect,picture,ScaleMode.ScaleToFit,true);else GUI.Label(rect,"?",centered);
            Outline(rect,isSelected?Color.white:color,isSelected?2:1);
            if(labels)
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
