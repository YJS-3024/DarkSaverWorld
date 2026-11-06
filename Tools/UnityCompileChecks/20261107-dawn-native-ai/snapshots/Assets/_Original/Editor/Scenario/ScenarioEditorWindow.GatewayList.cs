using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DarkSaver.Original.Campaign;

namespace DarkSaver.Original.Editor.Scenarios
{
    public sealed partial class ScenarioEditorWindow
    {
        [SerializeField] private int gatewayGrouping=1,gatewaySorting;
        [SerializeField] private string gatewaySearch="";
        [SerializeField] private List<string> closedGatewayGroups=new List<string>();
        private string gatewayDragCandidate;
        private Vector2 gatewayListDragStart;
        private const string GatewayDragKey="DarkSaver.ScenarioGatewayList";
        private sealed class GatewayListDrag { public ScenarioEditorWindow window;public string id; }
        private sealed class GatewayListGroup { public string key,title;public ScenarioGateway[] items; }

        private string GatewayGroupKey(ScenarioGateway gate) => gatewayGrouping==1?gate.fromMap.ToString():gatewayGrouping==2?gate.targetMap.ToString():gatewayGrouping==3?gate.GroupName:"";
        private string GatewayGroupTitle(ScenarioGateway gate)
        {
            if(gatewayGrouping==3)return gate.GroupName.Length==0?"그룹 미지정":gate.GroupName;
            int id=gatewayGrouping==1?gate.fromMap:gate.targetMap;
            return MapLabel(id);
        }
        private IEnumerable<ScenarioGateway> SortGateways(IEnumerable<ScenarioGateway> items)
        {
            switch(gatewaySorting)
            {
                case 1:return items.OrderBy(g=>g.name,StringComparer.OrdinalIgnoreCase);
                case 2:return items.OrderBy(g=>g.fromMap).ThenBy(g=>g.targetMap);
                case 3:return items.OrderBy(g=>g.targetMap).ThenBy(g=>g.fromMap);
                default:return items;
            }
        }
        private GatewayListGroup[] GatewayGroups()
        {
            var groups=gatewayDocument.gates.GroupBy(GatewayGroupKey);
            if(gatewayGrouping==1||gatewayGrouping==2)groups=groups.OrderBy(g=>int.Parse(g.Key));
            return groups.Select(g=>new GatewayListGroup{key=g.Key,title=GatewayGroupTitle(g.First()),items=SortGateways(g).ToArray()}).ToArray();
        }
        private void DrawGatewayGroupField(ScenarioGateway gate)
        {
            var names=gatewayDocument.gates.Select(g=>g.GroupName).Where(g=>g.Length>0).Distinct().ToArray();
            var options=new[]{"그룹 미지정"}.Concat(names).ToArray();
            int at=Array.IndexOf(names,gate.GroupName)+1,next=EditorGUILayout.Popup("사용자 그룹",at,options);
            if(next!=at){RememberGateway();gate.group=next==0?null:names[next-1];}
            string name=EditorGUILayout.DelayedTextField("그룹명 직접 입력",gate.GroupName);
            if(name.Trim()!=gate.GroupName){RememberGateway();gate.group=string.IsNullOrWhiteSpace(name)?null:name.Trim();}
        }
        private void SelectGatewayListItem(string id)
        {selectedGateway=id;selectedNativePortal=null;pickNpc=pickSpawn=dragGateway=sizingGateway=false;}
        private void MoveGatewayListItem(string id,string anchor,bool after,string group=null)
        {
            var next=gatewayDocument.Copy();var moved=next.gates.FirstOrDefault(g=>g.id==id);if(moved==null)return;
            bool changed=group!=null&&moved.GroupName!=group;
            if(group!=null)moved.group=group.Length==0?null:group;
            changed=ScenarioGatewayData.Move(next,id,anchor,after)||changed;
            if(!changed)return;
            RememberGateway();gatewayDocument=next;SelectGatewayListItem(id);GUIUtility.ExitGUI();
        }
        private void MoveGatewayGroup(string key,int direction)
        {
            var groups=gatewayDocument.gates.GroupBy(g=>g.GroupName).ToArray();int at=Array.FindIndex(groups,g=>g.Key==key),to=at+direction;
            if(at<0||to<0||to>=groups.Length)return;
            var swap=groups[at];groups[at]=groups[to];groups[to]=swap;
            RememberGateway();gatewayDocument.gates=groups.SelectMany(g=>g).ToArray();GUIUtility.ExitGUI();
        }
        private bool GatewayMatches(ScenarioGateway gate)
        {
            string search=(gatewaySearch??"").Trim();if(search.Length==0)return true;
            string text=gate.id+" "+gate.name+" "+gate.GroupName+" "+gate.fromMap+" "+gate.targetMap+" "+
                gatewayCatalog.maps.FirstOrDefault(m=>m.id==gate.fromMap)?.title+" "+gatewayCatalog.maps.FirstOrDefault(m=>m.id==gate.targetMap)?.title;
            return text.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0;
        }
        private void DrawGatewayList()
        {
            GUILayout.Label("사용자 입구 목록",EditorStyles.boldLabel);
            gatewaySearch=EditorGUILayout.TextField("검색",gatewaySearch);
            gatewayGrouping=EditorGUILayout.Popup("그룹화",gatewayGrouping,new[]{"없음","출발 맵","도착 맵","사용자 그룹"});
            gatewaySorting=EditorGUILayout.Popup("정렬",gatewaySorting,new[]{"사용자 순서","이름순","출발 맵순","도착 맵순"});
            var groups=GatewayGroups();
            using(new EditorGUI.DisabledScope(gatewaySorting==0))
                if(GUILayout.Button("현재 정렬을 저장 순서에 반영"))
                {RememberGateway();gatewayDocument.gates=groups.SelectMany(g=>g.items).ToArray();gatewaySorting=0;GUIUtility.ExitGUI();}
            GUILayout.Label("사용자 순서: ≡ 드래그 또는 ↑↓\n그룹 지정: 입구 선택 후 아래에서 입력",EditorStyles.wordWrappedMiniLabel);
            int shown=0;
            for(int groupIndex=0;groupIndex<groups.Length;groupIndex++)
            {
                var group=groups[groupIndex];var visible=group.items.Where(GatewayMatches).ToArray();if(visible.Length==0)continue;shown+=visible.Length;
                bool open=true;
                if(gatewayGrouping!=0)
                {
                    string foldKey=gatewayGrouping+":"+group.key;
                    open=!closedGatewayGroups.Contains(foldKey);GUILayout.BeginHorizontal();
                    bool nextOpen=EditorGUILayout.Foldout(open,group.title+" ("+visible.Length+"/"+group.items.Length+")",true);
                    var header=GUILayoutUtility.GetLastRect();
                    if(nextOpen!=open){if(nextOpen)closedGatewayGroups.Remove(foldKey);else closedGatewayGroups.Add(foldKey);open=nextOpen;}
                    if(gatewayGrouping==3)
                    {
                        using(new EditorGUI.DisabledScope(gatewaySorting!=0||groupIndex==0))if(GUILayout.Button("↑",GUILayout.Width(22)))MoveGatewayGroup(group.key,-1);
                        using(new EditorGUI.DisabledScope(gatewaySorting!=0||groupIndex==groups.Length-1))if(GUILayout.Button("↓",GUILayout.Width(22)))MoveGatewayGroup(group.key,1);
                    }
                    GUILayout.EndHorizontal();HandleGatewayDrop(header,group.items.Last(),true,group.key);
                }
                if(!open)continue;
                for(int i=0;i<visible.Length;i++)
                {
                    var gate=visible[i];GUILayout.BeginHorizontal();GUILayout.Label("≡",GUILayout.Width(16));var handle=GUILayoutUtility.GetLastRect();
                    string title=(gate.id==selectedGateway?"● ":"")+(gate.enabled?"":"[꺼짐] ")+gate.name+" · "+MapLabel(gate.fromMap)+" → "+MapLabel(gate.targetMap);
                    if(GUILayout.Button(new GUIContent(title,title),EditorStyles.miniButton))SelectGatewayListItem(gate.id);
                    using(new EditorGUI.DisabledScope(gatewaySorting!=0||i==0))if(GUILayout.Button("↑",GUILayout.Width(22)))MoveGatewayListItem(gate.id,visible[i-1].id,false);
                    using(new EditorGUI.DisabledScope(gatewaySorting!=0||i==visible.Length-1))if(GUILayout.Button("↓",GUILayout.Width(22)))MoveGatewayListItem(gate.id,visible[i+1].id,true);
                    GUILayout.EndHorizontal();var row=GUILayoutUtility.GetLastRect();
                    HandleGatewayDrag(handle,gate.id);HandleGatewayDrop(row,gate,Event.current.mousePosition.y>=row.center.y,group.key);
                }
            }
            GUILayout.Label("표시 "+shown+" / 전체 "+gatewayDocument.gates.Length,EditorStyles.miniLabel);
            if(Event.current.type==EventType.MouseUp)gatewayDragCandidate=null;
        }
        private void HandleGatewayDrag(Rect handle,string id)
        {
            if(gatewaySorting!=0)return;
            var e=Event.current;
            if(e.type==EventType.MouseDown&&e.button==0&&handle.Contains(e.mousePosition))
            {gatewayDragCandidate=id;gatewayListDragStart=e.mousePosition;GUI.FocusControl(null);e.Use();}
            if(e.type==EventType.MouseDrag&&gatewayDragCandidate==id&&(e.mousePosition-gatewayListDragStart).sqrMagnitude>16)
            {
                DragAndDrop.PrepareStartDrag();DragAndDrop.objectReferences=Array.Empty<UnityEngine.Object>();
                DragAndDrop.SetGenericData(GatewayDragKey,new GatewayListDrag{window=this,id=id});
                DragAndDrop.StartDrag("입구 목록 순서 변경");gatewayDragCandidate=null;e.Use();
            }
        }
        private void HandleGatewayDrop(Rect rect,ScenarioGateway anchor,bool after,string group)
        {
            var e=Event.current;if((e.type!=EventType.DragUpdated&&e.type!=EventType.DragPerform)||!rect.Contains(e.mousePosition))return;
            var drag=DragAndDrop.GetGenericData(GatewayDragKey)as GatewayListDrag;
            if(drag==null||!ReferenceEquals(drag.window,this))return;
            var source=gatewayDocument.gates.FirstOrDefault(g=>g.id==drag.id);
            bool allowed=gatewaySorting==0&&source!=null&&(gatewayGrouping==0||gatewayGrouping==3||GatewayGroupKey(source)==group);
            DragAndDrop.visualMode=allowed?DragAndDropVisualMode.Move:DragAndDropVisualMode.Rejected;
            if(allowed)
            {
                EditorGUI.DrawRect(new Rect(rect.x,after?rect.yMax-2:rect.y,rect.width,2),Color.cyan);
                if(e.type==EventType.DragPerform)
                {DragAndDrop.AcceptDrag();e.Use();MoveGatewayListItem(drag.id,anchor.id,after,gatewayGrouping==3?group:null);return;}
            }
            e.Use();Repaint();
        }
    }
}
