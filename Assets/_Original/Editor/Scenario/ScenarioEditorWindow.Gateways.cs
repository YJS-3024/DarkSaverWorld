using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Editor.Scenarios
{
    public sealed partial class ScenarioEditorWindow
    {
        private string gatewaySaveError="",gatewaySaveNotice="";
        private const string GatewayPath=BasePath+"Data/Campaign/UserGateways.json";
        [SerializeField] private ScenarioGatewaySettings gatewayDocument;
        [SerializeField] private bool dirtyGateways;
        [SerializeField] private string gatewayHash,selectedGateway;
        private GatewayMapCatalog gatewayCatalog;
        private WorldDefinition gatewayWorld;
        private string selectedNativePortal;
        private bool sizingGateway;
        private bool sizingArrival,dragArrival;
        private int areaStartX,areaStartY;
        private bool pickNpc,pickSpawn,dragGateway,gatewayRemembered;
        private Vector2 gatewayDragStart;
        private int gatewayStartX,gatewayStartY;
        private readonly System.Collections.Generic.List<byte[]> gatewayUndo=new System.Collections.Generic.List<byte[]>(),gatewayRedo=new System.Collections.Generic.List<byte[]>();
        private GatewayMapInfo MapInfo(string key)=>gatewayCatalog.maps.First(m=>m.key==key);
        private WorldDefinition ReadGatewayWorld()
        {
            return JsonUtility.FromJson<CampaignDefinition>(File.ReadAllText(BasePath+"Data/Campaign/CampaignCatalog.json")).Merge(
                JsonUtility.FromJson<WorldDefinition>(File.ReadAllText(BasePath+"Data/World/KaosiaWorld.json")));
        }
        private void LoadGateways(bool preserve)
        {
            gatewayCatalog=JsonUtility.FromJson<GatewayMapCatalog>(File.ReadAllText(BasePath+"Data/Campaign/GatewayMaps.json"));
            gatewayWorld=ReadGatewayWorld();
            if(preserve)return;
            var bytes=File.ReadAllBytes(GatewayPath);gatewayDocument=ScenarioGatewayData.Decode(bytes);ScenarioGatewayData.Validate(gatewayDocument,gatewayCatalog);
            gatewayHash=ScenarioGatewayData.Hash(bytes);dirtyGateways=false;
        }
        private void RememberGateway()
        {
            gatewayUndo.Add(ScenarioGatewayData.Encode(gatewayDocument));if(gatewayUndo.Count>100)gatewayUndo.RemoveAt(0);gatewayRedo.Clear();
            dirtyGateways=hasUnsavedChanges=true;message="입구 연결: 저장하지 않은 변경";Repaint();
        }
        private void GatewayHistory(bool back)
        {
            var from=back?gatewayUndo:gatewayRedo;var to=back?gatewayRedo:gatewayUndo;if(from.Count==0)return;
            to.Add(ScenarioGatewayData.Encode(gatewayDocument));gatewayDocument=ScenarioGatewayData.Decode(from[from.Count-1]);from.RemoveAt(from.Count-1);
            dirtyGateways=hasUnsavedChanges=true;dragGateway=pickNpc=pickSpawn=sizingGateway=false;Repaint();
        }
        private void CheckGatewayMapVersion(int id,string context)
        {
            try
            {
                var info=gatewayCatalog.maps.FirstOrDefault(m=>m.id==id);
                if(info==null||ScenarioGatewayData.Hash(File.ReadAllBytes(info.nativePath))!=info.hash)
                    throw new IOException("맵 "+id+"이 변경됐거나 선택되지 않았습니다.");
            }
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException)
            {throw new IOException(context+": "+e.Message,e);}
        }
        private bool SaveGateways()
        {
            try
            {
                ScenarioGatewayData.Validate(gatewayDocument,gatewayCatalog);
                foreach(var g in gatewayDocument.gates)
                    foreach(int id in new[]{g.fromMap,g.targetMap})CheckGatewayMapVersion(id,ScenarioGatewayData.GatewayContext(g));
                foreach(var point in gatewayDocument.entryPoints??Array.Empty<ScenarioEntryPoint>())
                    CheckGatewayMapVersion(point.map,ScenarioGatewayData.EntryContext(point));
                // Validate actual runtime placement too, without changing its cached native data.
                ScenarioGatewayData.Apply(gatewayDocument,gatewayCatalog,ReadGatewayWorld());
                string saved=ScenarioGatewayData.Save(GatewayPath,gatewayDocument,gatewayCatalog,string.IsNullOrEmpty(gatewayHash)?null:gatewayHash);
                if(ScenarioGatewayData.Hash(File.ReadAllBytes(GatewayPath))!=saved)throw new IOException("저장 후 파일이 변경되었거나 확인할 수 없습니다. 파일을 보존하고 다시 확인하세요.");
                gatewayHash=saved;dirtyGateways=false;hasUnsavedChanges=dirty;gatewaySaveError="";
                gatewaySaveNotice="입구·진입점 저장 완료: "+GatewayPath;
                message=gatewaySaveNotice+(dirty?" · 몬스터 배치는 아직 미저장":"");Repaint();
                try{AssetDatabase.Refresh();}
                catch(ExitGUIException){throw;}
                catch(Exception e){Debug.LogWarning("입구 파일은 저장됐지만 AssetDatabase 새로고침에 실패했습니다: "+e.Message);}
                return true;
            }
            catch(ExitGUIException){throw;}
            catch(Exception e)
            {
                gatewaySaveError="입구·진입점 저장 실패: "+e.Message;message=gatewaySaveError;
                Debug.LogException(new IOException(gatewaySaveError,e));Repaint();return false;
            }
        }
        private void ReloadGateways()
        {
            if(dirtyGateways&&!EditorUtility.DisplayDialog("입구 다시 열기","저장하지 않은 입구 편집을 버리고 저장본을 불러올까요?","불러오기","취소"))return;
            try{LoadGateways(false);gatewayUndo.Clear();gatewayRedo.Clear();hasUnsavedChanges=dirty;dragGateway=pickNpc=pickSpawn=sizingGateway=false;message="입구를 다시 불러왔습니다.";}catch(Exception e){message=e.Message;}
        }
        private void AddGateway(bool walk)
        {
            var map=MapInfo(mapKey);RememberGateway();
            var gate=new ScenarioGateway{id=Guid.NewGuid().ToString("N"),trigger=walk?"walk":"npc",width=walk?3:1,height=1,
                fromMap=map.id,fromHash=map.hash,targetMap=-1,npcJob=-1,x=-1,y=-1,spawnX=-1,spawnY=-1,spawnWidth=1,spawnHeight=1,
                returnX=-1,returnY=-1,name=walk?"새 이동 입구":"새 NPC 입구"};
            gatewayDocument.gates=gatewayDocument.gates.Concat(new[]{gate}).ToArray();selectedGateway=gate.id;selectedNativePortal=null;
            pickNpc=pickSpawn=dragGateway=sizingGateway=false;
        }
        private void EditNativePortal(WorldPortal portal)
        {
            var existing=gatewayDocument.gates.FirstOrDefault(g=>g.fromMap==int.Parse(mapKey)&&g.replacesPortal==portal.id);
            if(existing!=null){selectedGateway=existing.id;selectedNativePortal=null;return;}
            int x=portal.cells.Min(c=>c.x),y=portal.cells.Min(c=>c.y),w=portal.cells.Max(c=>c.x)-x+1,h=portal.cells.Max(c=>c.y)-y+1;
            if(portal.cells.Select(c=>c.x+":"+c.y).Distinct().Count()!=w*h){message="이 출입구는 사각형이 아닙니다. 새 이동 입구로 영역을 지정하세요.";return;}
            var from=MapInfo(mapKey);var target=gatewayCatalog.maps.First(m=>m.id==portal.targetMap);RememberGateway();
            var gate=new ScenarioGateway{id=Guid.NewGuid().ToString("N"),trigger="walk",replacesPortal=portal.id,
                fromMap=from.id,fromHash=from.hash,targetMap=target.id,targetHash=target.hash,x=x,y=y,width=w,height=h,
                spawnX=portal.spawnX,spawnY=portal.spawnY,spawnWidth=Math.Max(1,portal.spawnWidth),spawnHeight=Math.Max(1,portal.spawnHeight),returnX=portal.returnX,returnY=portal.returnY,name=portal.id,enabled=true};
            gatewayDocument.gates=gatewayDocument.gates.Concat(new[]{gate}).ToArray();selectedGateway=gate.id;selectedNativePortal=null;
        }
        private void PickGatewayMap(ScenarioGateway gate,bool arrival)
        {
            int id=arrival?gate.targetMap:gate.fromMap;SelectMap(gatewayCatalog.maps.First(m=>m.id==id).key);
            if(int.Parse(mapKey)!=id)return;
            pickSpawn=arrival;pickNpc=!arrival;dragGateway=sizingGateway=false;
        }
        private System.Collections.Generic.IEnumerable<WorldPoint> BlockedEntranceCells(ScenarioGateway gate)
        {
            var map=gatewayWorld.Map(gate.fromMap)?.map;
            if(!gate.IsWalk||map==null||gate.x<0||gate.y<0||gate.Width<=0||gate.Height<=0
                ||(long)gate.x+gate.Width>map.width||(long)gate.y+gate.Height>map.height)yield break;
            for(int y=gate.y;y<gate.y+gate.Height;y++)for(int x=gate.x;x<gate.x+gate.Width;x++)
            {
                int cost=map.Terrain(new DarkSaver.Original.Tutorial.TutorialTile(x,y),2);
                if(cost<=0||cost==99||cost==-99)yield return new WorldPoint{x=x,y=y};
            }
        }
        private void DrawGatewayInspector(Rect rect)
        {
            GUILayout.BeginArea(rect,EditorStyles.helpBox);inspectorScroll=EditorGUILayout.BeginScrollView(inspectorScroll);
            GUILayout.Label("맵 입구 연결",EditorStyles.boldLabel);
            GUILayout.Label(queuedSave!=0||saveScheduled?"입력 확정 후 저장 중…":dirtyGateways?"입구·진입점: 미저장":"입구·진입점: 변경 없음",EditorStyles.boldLabel);
            if(dirty)EditorGUILayout.HelpBox("몬스터 배치에 별도 미저장 변경이 있습니다. 입구 저장은 배치 파일을 저장하지 않습니다.",MessageType.Info);
            if(gatewaySaveError.Length>0){EditorGUILayout.HelpBox(gatewaySaveError,MessageType.Error);if(GUILayout.Button("저장 오류 복사"))EditorGUIUtility.systemCopyBuffer=gatewaySaveError;}
            else if(gatewaySaveNotice.Length>0)EditorGUILayout.HelpBox(gatewaySaveNotice,MessageType.Info);
            EditorGUILayout.HelpBox("이동 입구: 영역을 밟아 맵 이동\nNPC 입구: 클릭 → 방 목록 → 참가\n한 맵에 여러 입구를 설정합니다.\n돌아오는 방향은 별도 입구로 추가하세요.",MessageType.Info);
            bool apply=EditorGUILayout.ToggleLeft("저장한 연결을 로컬 서버에서 사용",gatewayDocument.applyToGame);
            if(apply!=gatewayDocument.applyToGame){RememberGateway();gatewayDocument.applyToGame=apply;}
            GUILayout.Label("적용은 다음 Play/프로필 진입부터",EditorStyles.miniLabel);
            var map=MapInfo(mapKey);
            bool runtimeRegistered=gatewayWorld.Map(map.id)!=null;
            if(!runtimeRegistered)EditorGUILayout.HelpBox("맵 "+map.id+"은 보기/편집 전용이며 게임 이동 대상으로 등록되어 있지 않습니다. 이 맵의 이동 연결은 만들 수 없습니다.",MessageType.Warning);
            using(new EditorGUI.DisabledScope(!runtimeRegistered))if(GUILayout.Button("이 맵에 이동 입구 추가"))AddGateway(true);
            using(new EditorGUI.DisabledScope(!runtimeRegistered||map.kind!="world"&&map.kind!="town"))
                if(GUILayout.Button("이 맵에 NPC 입구 추가"))AddGateway(false);
            GUILayout.Label("기존 출입구 (현재 지도)",EditorStyles.boldLabel);
            var nativePortals=gatewayWorld.Map(map.id)?.portals??Array.Empty<WorldPortal>();
            foreach(var portal in nativePortals)
            {
                bool overridden=gatewayDocument.gates.Any(g=>g.fromMap==map.id&&g.replacesPortal==portal.id);
                if(GUILayout.Button((selectedNativePortal==portal.id?"● ":"")+portal.id+" → "+MapLabel(portal.targetMap)+" ("+portal.cells.Length+"칸)"+(overridden?" [편집본]":""),EditorStyles.miniButton))
                {selectedNativePortal=portal.id;selectedGateway=null;pickNpc=pickSpawn=dragGateway=sizingGateway=false;}
            }
            var native=nativePortals.FirstOrDefault(p=>p.id==selectedNativePortal);
            if(native!=null)
            {
                EditorGUILayout.HelpBox("기존 도착: "+MapLabel(native.targetMap)+" / "+native.spawnX+","+native.spawnY+" ("+Math.Max(1,native.spawnWidth)+"×"+Math.Max(1,native.spawnHeight)+")\n수정본 저장 전까지 원본은 유지됩니다.",MessageType.None);
                if(GUILayout.Button("이 출입구를 편집본으로 가져오기"))EditNativePortal(native);
            }
            DrawGatewayList();
            var gate=gatewayDocument.gates.FirstOrDefault(g=>g.id==selectedGateway);
            if(gate!=null)
            {
                GUILayout.Space(7);GUILayout.Label("선택 입구",EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel("ID: "+gate.id,EditorStyles.miniLabel,GUILayout.Height(18));
                EditorGUI.BeginChangeCheck();string name=EditorGUILayout.TextField("입구 이름",gate.name);bool enabled=EditorGUILayout.Toggle("이 연결 사용",gate.enabled);
                if(EditorGUI.EndChangeCheck()){RememberGateway();gate.name=name;gate.enabled=enabled;}
                DrawGatewayGroupField(gate);
                GUILayout.Label(gate.IsWalk?"방식: 영역 진입 (자동 이동)":"방식: NPC 클릭 (방 참가)");
                GUILayout.Label("출발: "+MapLabel(gate.fromMap)+"\n도착: "+MapLabel(gate.targetMap),EditorStyles.wordWrappedLabel);
                if(!string.IsNullOrEmpty(gate.replacesPortal))GUILayout.Label("기존 출입구 수정: "+gate.replacesPortal,EditorStyles.wordWrappedMiniLabel);
                if(!gate.IsWalk){var npcs=gatewayCatalog.npcJobs;var options=new[]{"외형을 선택하세요"}.Concat(npcs.Select(id=>jobs.TryGetValue(id,out var j)?j.ToString():id.ToString())).ToArray();
                int npcAt=Array.IndexOf(npcs,gate.npcJob)+1,npc=EditorGUILayout.Popup("NPC 외형",npcAt,options.Select(FlatPopupLabel).ToArray());
                if(npc!=npcAt){RememberGateway();gate.npcJob=npc==0?-1:npcs[npc-1];}
                }
                var targets=gatewayCatalog.maps.Where(m=>m.id!=gate.fromMap&&gatewayWorld.Map(m.id)!=null&&(gate.IsWalk||m.kind=="scenario")).OrderBy(m=>m.id).ToArray();
                if(gate.targetMap>=0&&gatewayWorld.Map(gate.targetMap)==null)EditorGUILayout.HelpBox("현재 도착 맵 "+gate.targetMap+"은 실행 미등록입니다. 등록된 도착 맵으로 다시 선택하세요. 기존 값은 자동 삭제하지 않습니다.",MessageType.Error);
                if(gatewayWorld.Map(gate.fromMap)==null)EditorGUILayout.HelpBox("현재 출발 맵 "+gate.fromMap+"은 실행 미등록입니다. 이 연결은 실행할 수 없습니다.",MessageType.Error);
                int at=Array.FindIndex(targets,m=>m.id==gate.targetMap)+1,next=EditorGUILayout.Popup("도착 맵",at,new[]{"도착 맵을 선택하세요"}.Concat(targets.Select(m=>FlatPopupLabel(Title(m.key)))).ToArray());
                if(next!=at){RememberGateway();gate.targetMap=next==0?-1:targets[next-1].id;gate.targetHash=next==0?"":targets[next-1].hash;gate.spawnX=gate.spawnY=-1;pickNpc=pickSpawn=dragGateway=sizingGateway=false;}
                EditorGUI.BeginChangeCheck();int x=EditorGUILayout.DelayedIntField("출발 X (좌상단)",gate.x),y=EditorGUILayout.DelayedIntField("출발 Y (좌상단)",gate.y);
                int width=gate.IsWalk?EditorGUILayout.DelayedIntField("가로 칸 수",gate.Width):1,height=gate.IsWalk?EditorGUILayout.DelayedIntField("세로 칸 수",gate.Height):1;
                int sx=EditorGUILayout.DelayedIntField("도착 X (좌상단)",gate.spawnX),sy=EditorGUILayout.DelayedIntField("도착 Y (좌상단)",gate.spawnY);
                int sw=EditorGUILayout.DelayedIntField("도착 가로 칸 수",gate.SpawnWidth),sh=EditorGUILayout.DelayedIntField("도착 세로 칸 수",gate.SpawnHeight);
                if(EditorGUI.EndChangeCheck()){RememberGateway();gate.x=x;gate.y=y;gate.width=width;gate.height=height;gate.spawnX=sx;gate.spawnY=sy;gate.spawnWidth=Math.Max(1,sw);gate.spawnHeight=Math.Max(1,sh);}
                var blocked=BlockedEntranceCells(gate).Take(8).ToArray();
                if(blocked.Length>0)
                {
                    EditorGUILayout.HelpBox("입구 영역의 통행 불가 칸: "+string.Join(" · ",blocked.Select(c=>"("+c.x+","+c.y+")"))+(blocked.Length==8?" 외 추가 가능":"")+"\n빨간 칸을 제외하도록 입구 위치/크기를 바꾸세요.",MessageType.Error);
                    if(GUILayout.Button("첫 통행 불가 칸 보기"))
                    {
                        string key=gatewayCatalog.maps.First(m=>m.id==gate.fromMap).key;SelectMap(key);
                        if(mapKey==key)FocusTile(blocked[0].x,blocked[0].y);GUIUtility.ExitGUI();
                    }
                }
                GUILayout.BeginHorizontal();if(GUILayout.Button("도착 2 × 1")){RememberGateway();gate.spawnWidth=2;gate.spawnHeight=1;}if(GUILayout.Button("도착 1 × 2")){RememberGateway();gate.spawnWidth=1;gate.spawnHeight=2;}GUILayout.EndHorizontal();
                if(gate.SpawnWidth>1||gate.SpawnHeight>1)EditorGUILayout.HelpBox("출발 칸 순서에 비례해 도착 칸을 선택합니다. 도착한 출구는 영역을 벗어난 뒤 다시 진입할 때 작동합니다.",MessageType.None);
                if(gate.IsWalk)
                {
                    GUILayout.BeginHorizontal();if(GUILayout.Button("3칸 × 1")){RememberGateway();gate.width=3;gate.height=1;}if(GUILayout.Button("6칸 × 1")){RememberGateway();gate.width=6;gate.height=1;}GUILayout.EndHorizontal();
                    if(gatewayWorld.Map(gate.targetMap)?.training==true)
                    {
                        EditorGUI.BeginChangeCheck();int rx=EditorGUILayout.DelayedIntField("카오시아 복귀 X",gate.returnX),ry=EditorGUILayout.DelayedIntField("카오시아 복귀 Y",gate.returnY);
                        if(EditorGUI.EndChangeCheck()){RememberGateway();gate.returnX=rx;gate.returnY=ry;}
                    }
                }
                if(GUILayout.Button(gate.IsWalk?"출발 맵에서 입구 영역 그리기":"출발 맵에서 NPC 위치 찍기"))PickGatewayMap(gate,false);
                using(new EditorGUI.DisabledScope(gate.targetMap<0))
                    if(GUILayout.Button("도착 맵에서 출구 영역 그리기"))PickGatewayMap(gate,true);
                if(GUILayout.Button("출발 지도 보기")){pickNpc=pickSpawn=dragGateway=sizingGateway=false;SelectMap(gatewayCatalog.maps.First(m=>m.id==gate.fromMap).key);}
                using(new EditorGUI.DisabledScope(gate.targetMap<0))if(GUILayout.Button("도착 지도 보기")){pickNpc=pickSpawn=dragGateway=sizingGateway=false;SelectMap(gatewayCatalog.maps.First(m=>m.id==gate.targetMap).key);}
                if(GUILayout.Button("선택 입구 삭제")){RememberGateway();gatewayDocument.gates=gatewayDocument.gates.Where(g=>g.id!=selectedGateway).ToArray();selectedGateway=null;pickNpc=pickSpawn=dragGateway=sizingGateway=false;}
            }
            GUILayout.BeginHorizontal();using(new EditorGUI.DisabledScope(gatewayUndo.Count==0))if(GUILayout.Button("되돌리기"))GatewayHistory(true);
            using(new EditorGUI.DisabledScope(gatewayRedo.Count==0))if(GUILayout.Button("다시 실행"))GatewayHistory(false);GUILayout.EndHorizontal();
            if(GUILayout.Button("입구 연결 저장"))QueueEditorSave(true);
            if(pickNpc||pickSpawn||sizingGateway)EditorGUILayout.HelpBox(pickSpawn||sizingGateway&&sizingArrival?"도착 지도입니다. 클릭하면 현재 크기로 놓고, 드래그하면 도착 출구 영역을 그립니다.":gate?.IsWalk==true?"출발 지도에서 클릭하면 현재 크기로 배치합니다. 드래그하면 사각 영역을 그립니다.":"지도에서 NPC가 설 타일을 클릭하세요.",MessageType.Info);
            if(!string.IsNullOrEmpty(message))EditorGUILayout.HelpBox(message,MessageType.None);
            EditorGUILayout.EndScrollView();GUILayout.EndArea();
        }
        private void ProcessGatewayInput(Event e,bool inside,Vector2 local)
        {
            if(e.type==EventType.KeyDown&&(e.control||e.command)&&GUIUtility.keyboardControl==0&&e.keyCode==KeyCode.Z){GatewayHistory(!e.shift);e.Use();return;}
            if(e.type==EventType.KeyDown&&(e.control||e.command)&&GUIUtility.keyboardControl==0&&e.keyCode==KeyCode.Y){GatewayHistory(false);e.Use();return;}
            var gate=gatewayDocument.gates.FirstOrDefault(g=>g.id==selectedGateway);
            if(e.type==EventType.MouseDown&&e.button==0&&inside&&hoverX>=0)
            {
                if(gate!=null&&((pickNpc&&gate.fromMap==int.Parse(mapKey))||(pickSpawn&&gate.targetMap==int.Parse(mapKey))))
                {RememberGateway();sizingArrival=pickSpawn;areaStartX=hoverX;areaStartY=hoverY;if(pickNpc){gate.x=hoverX;gate.y=hoverY;sizingGateway=gate.IsWalk;}else{gate.spawnX=hoverX;gate.spawnY=hoverY;sizingGateway=true;}pickNpc=pickSpawn=false;}
                else
                {
                    dragArrival=gate!=null&&gate.targetMap==int.Parse(mapKey)&&gate.SpawnContains(hoverX,hoverY);
                    if(!dragArrival)gate=gatewayDocument.gates.LastOrDefault(g=>g.fromMap==int.Parse(mapKey)&&g.Contains(hoverX,hoverY));
                    if(gate==null){gate=gatewayDocument.gates.LastOrDefault(g=>g.targetMap==int.Parse(mapKey)&&g.SpawnContains(hoverX,hoverY));dragArrival=gate!=null;}
                    if(gate!=null){selectedGateway=gate.id;selectedNativePortal=null;dragGateway=true;gatewayRemembered=false;gatewayDragStart=local;gatewayStartX=dragArrival?gate.spawnX:gate.x;gatewayStartY=dragArrival?gate.spawnY:gate.y;}
                    else{var portal=gatewayWorld.Map(int.Parse(mapKey)).portals.LastOrDefault(p=>p.cells.Any(c=>c.x==hoverX&&c.y==hoverY));selectedNativePortal=portal?.id;selectedGateway=null;}
                }
                e.Use();Repaint();
            }
            if(e.type==EventType.MouseDrag&&e.button==0&&sizingGateway&&gate!=null&&inside&&hoverX>=0)
            {
                int left=Math.Min(areaStartX,hoverX),top=Math.Min(areaStartY,hoverY),w=Math.Abs(hoverX-areaStartX)+1,h=Math.Abs(hoverY-areaStartY)+1;
                if(sizingArrival){gate.spawnX=left;gate.spawnY=top;gate.spawnWidth=w;gate.spawnHeight=h;}
                else{gate.x=left;gate.y=top;gate.width=w;gate.height=h;}e.Use();Repaint();
            }
            if(e.type==EventType.MouseDrag&&e.button==0&&dragGateway&&gate!=null)
            {
                if(!gatewayRemembered){RememberGateway();gatewayRemembered=true;}
                int x=Mathf.Clamp(gatewayStartX+Mathf.RoundToInt((local.x-gatewayDragStart.x)/(32*zoom)),0,Math.Max(0,document.width-(dragArrival?gate.SpawnWidth:gate.Width)));
                int y=Mathf.Clamp(gatewayStartY+Mathf.RoundToInt((local.y-gatewayDragStart.y)/(32*zoom)),0,Math.Max(0,document.height-(dragArrival?gate.SpawnHeight:gate.Height)));
                if(dragArrival){gate.spawnX=x;gate.spawnY=y;}else{gate.x=x;gate.y=y;}e.Use();Repaint();
            }
            if(e.type==EventType.MouseUp){dragGateway=sizingGateway=false;gatewayRemembered=false;}
            if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){dragGateway=sizingGateway=pickNpc=pickSpawn=false;e.Use();}
        }
        private void DrawGateways()
        {
            foreach(var unit in source.layout.units)DrawActor(unit.unit,unit.job.ToString(),unit.x,unit.y,new Color(.6f,.6f,.6f),false);
            foreach(var portal in gatewayWorld.Map(int.Parse(mapKey))?.portals??Array.Empty<WorldPortal>())
            {
                if(gatewayDocument.gates.Any(g=>g.fromMap==int.Parse(mapKey)&&g.replacesPortal==portal.id))continue;
                foreach(var cell in portal.cells)Outline(TileRect(cell.x,cell.y),selectedNativePortal==portal.id?Color.yellow:new Color(.4f,.8f,1,.6f),2);
                var cell0=portal.cells.First();var r=TileRect(cell0.x,cell0.y);GUI.Label(new Rect(r.x,r.y-15,200,15),"기존 "+portal.id+" → "+portal.targetMap,small);
            }
            foreach(var gate in gatewayDocument.gates)
            {
                if(gate.fromMap==int.Parse(mapKey)&&gate.x>=0&&gate.y>=0)
                {
                    var r=TileRect(gate.x,gate.y);
                    if(gate.IsWalk){r.width*=Math.Max(1,gate.Width);r.height*=Math.Max(1,gate.Height);EditorGUI.DrawRect(r,new Color(0,.8f,1,.18f));}
                    else{var texture=ActorPicture(gate.npcJob.ToString());if(texture!=null)DrawActorPicture(r,texture);}
                    if(gate.IsWalk&&gate.id==selectedGateway)
                        foreach(var bad in BlockedEntranceCells(gate))
                        {
                            var cell=TileRect(bad.x,bad.y);if(!cell.Overlaps(new Rect(Vector2.zero,viewport.size)))continue;
                            EditorGUI.DrawRect(cell,new Color(1,.1f,.1f,.6f));Outline(cell,Color.red,1);
                        }
                    Outline(r,gate.id==selectedGateway?Color.yellow:gate.enabled?Color.cyan:Color.gray,2);GUI.Label(new Rect(r.x,r.y-15,220,15),gate.name+" → "+gate.targetMap+(gate.IsWalk?" ("+gate.Width+"×"+gate.Height+")":""),small);
                }
                if(gate.targetMap==int.Parse(mapKey)&&gate.spawnX>=0&&gate.spawnY>=0)
                {
                    var r=TileRect(gate.spawnX,gate.spawnY);r.width*=Math.Max(1,gate.SpawnWidth);r.height*=Math.Max(1,gate.SpawnHeight);
                    var color=gate.id==selectedGateway?Color.green:new Color(.3f,.65f,.4f);EditorGUI.DrawRect(r,new Color(color.r,color.g,color.b,.18f));Outline(r,color,2);
                    GUI.Label(new Rect(r.x,r.y-15,260,15),gate.name+" 도착 "+gate.SpawnWidth+"×"+gate.SpawnHeight+" (출발 "+gate.fromMap+")",small);
                }
            }
        }
    }
}
