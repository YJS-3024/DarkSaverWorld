using System;
using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Network;
using UnityEngine;
namespace DarkSaver.UI
{
    public sealed partial class NativeWorldWindows
    {
        private WorldRosterEntry[] worldRoster;
        private bool receivedRoster;
        private int rosterReceiptRevision;
        private ushort selectedRecord=100;
        private Notify_WorldInventory.Item[] nativeInventory;
        private void BindWorldCharacterServer()
        {
            var session=GetComponent<BattleTransitionController>();
            if(server==null || session==null || server.LocalWorldCharacters!=null || server.LocalServer.HasHandler(new Request_WorldCharacters()))return;
            var service=new WorldCharacterService(()=>IsAvailable && !exitModal && !HasForeignModal,session.CaptureWorldCharacterRoster,session.CaptureWorldCharacterData,session.TrySaveWorldCharacterData,p=>server.SendToClient(p));
            server.BindLocalWorldCharacters(this,service);
        }
        private void RefreshWorldCharactersFromServer()
        {BindServer();if(server!=null && server.LocalServer.HasHandler(new Request_WorldCharacters()))server.TryRequest(new Request_WorldCharacters(),out _);}
        public ushort SelectedWorldRecordId=>selectedRecord;
        public WorldRosterEntry SelectedWorldUnit
        {
            get{RefreshRoster();return Array.Find(worldRoster,e=>e.RecordId==selectedRecord)?.Clone();}
        }
        private void RefreshRoster()
        {
            if(receivedRoster)return;
            var session=GetComponent<BattleTransitionController>();
            if(session==null || !session.HasCreatedCharacter){worldRoster=Array.Empty<WorldRosterEntry>();return;}
            var profile=session.CaptureCharacterProfile();
            var rows=new List<WorldRosterEntry>{new WorldRosterEntry{RecordId=100,JobId=profile.jobId,Name=profile.name,
                JobName=NativeCharacterProfile.StartingJobName(profile.jobId),Profile=profile,Experience=session.PlayerExperience,
                MaxHp=session.HasPlayerVitals?(int?)session.PlayerMaxHp:null,MaxMp=session.HasPlayerVitals?(int?)session.PlayerMaxMp:null}};
            var owned=session.CaptureOwnedRoster()?.units;
            if(owned!=null)foreach(var unit in owned)
            {
                if(unit==null || !DarkSaver.Jobs.NativePlayerJobs.Contains(unit.jobId) || unit.instanceId<0 || unit.instanceId>ushort.MaxValue
                    || rows.Exists(e=>e.RecordId==unit.instanceId))continue;
                rows.Add(new WorldRosterEntry{RecordId=(ushort)unit.instanceId,JobId=unit.jobId,Name=unit.name,MaxHp=unit.maxHp,MaxMp=unit.maxMp,Attack=unit.attack,Defense=unit.defense,Experience=unit.experience});
                if(rows.Count==10)break;
            }
            worldRoster=rows.ToArray();
            if(!Array.Exists(worldRoster,e=>e.RecordId==selectedRecord))selectedRecord=100;
        }
        public bool BindWorldRoster(Notify_WorldRoster notification)
        {
            if(notification==null)return false;
            var rows=notification.Capture();
            worldRoster=rows;receivedRoster=true;rosterReceiptRevision++;
            if(!Array.Exists(rows,e=>e.RecordId==selectedRecord))selectedRecord=rows.Length>0?rows[0].RecordId:(ushort)100;
            RefreshOpenPersonalWindow();return true;
        }
        public bool SelectWorldRole(int slot)
        {
            RefreshRoster();if(!IsAvailable || exitModal || HasForeignModal || slot<0 || slot>=worldRoster.Length)return false;
            selectedRecord=worldRoster[slot].RecordId;RefreshOpenPersonalWindow();return true;
        }
        internal void BindPersonalWindow(TownServicesPanel panel)
        {panel.BindWorldUnit(SelectedWorldUnit,nativeInventory);}
        private void RefreshOpenPersonalWindow()
        {
            var panel=GetComponent<TownServicesPanel>();
            if(panel!=null && panel.IsOpen && (panel.Service==TownService.Inventory || panel.Service==TownService.CharacterInfo))panel.BindWorldUnit(SelectedWorldUnit,nativeInventory);
        }
        public bool SetWorldRoleFlag(int slot)
        {
            RefreshRoster();
            if(!IsAvailable || exitModal || HasForeignModal || slot<1 || slot>=worldRoster.Length)return false;
            var unit=worldRoster[slot];ushort next=(ushort)(unit.OriginalFlag==1?2:1);int receipt=rosterReceiptRevision;
            bool sent=SubmitWorldPacket(new Request_WorldRosterFlag(unit.RecordId,next));
            if(sent && receipt==rosterReceiptRevision){unit.OriginalFlag=next;receivedRoster=true;}return sent;
        }
        public bool ReorderWorldRoles(int source,int destination)
        {
            RefreshRoster();if(!IsAvailable || exitModal || HasForeignModal || source<0 || destination<0 || source>=worldRoster.Length || destination>=worldRoster.Length || source==destination)return false;
            int receipt=rosterReceiptRevision;bool sent=SubmitWorldPacket(new Request_WorldRosterOrder((ushort)destination,(ushort)source));
            if(sent && receipt==rosterReceiptRevision){var unit=worldRoster[source];worldRoster[source]=worldRoster[destination];worldRoster[destination]=unit;selectedRecord=unit.RecordId;receivedRoster=true;RefreshOpenPersonalWindow();}return sent;
        }
        private bool SubmitWorldPacket(ClientPacket request)
        {
            BindServer();
            if(server==null)return false;
            if(server.LocalServer.HasHandler(request))
            {bool accepted=server.TryRequest(request,out var response);if(!accepted)ReportCommandFailure(response?.Reason ?? "요청을 처리할 수 없습니다.");return accepted;}
            return server.SendToServer(request);
        }
        private void DrawRoleCells(Vector2 origin)
        {
            RefreshRoster();
            for(int i=0;i<worldRoster.Length;i++)
            {
                int slot=i;var entry=worldRoster[i];var cell=RoleCell(i,origin);
                ui.NativeUnitPicture("summary-body-"+i,cell,SummaryPicture(entry.JobId));
                ui.NativeRosterSelection("summary-select-"+i,cell,()=>SelectWorldRole(slot),!exitModal,entry.Name);
                ui.ItemDragSourceAt("summary-select-"+i,i+1,SummaryPicture(entry.JobId),new Rect(origin,new Vector2(263,363)),(_,point)=>
                {for(int destination=0;destination<worldRoster.Length;destination++)if(RoleCell(destination,origin).Contains(point)){ReorderWorldRoles(slot,destination);break;}});
                if(entry.RecordId==selectedRecord)
                {
                    var color=new Color32(50,50,200,255);
                    ui.SolidRect("summary-highlight-top-"+i,new Rect(cell.x-1,cell.y-1,34,1),color);
                    ui.SolidRect("summary-highlight-bottom-"+i,new Rect(cell.x-1,cell.yMax,34,1),color);
                    ui.SolidRect("summary-highlight-left-"+i,new Rect(cell.x-1,cell.y,1,32),color);
                    ui.SolidRect("summary-highlight-right-"+i,new Rect(cell.xMax,cell.y,1,32),color);
                }
            }
        }
        private static Rect RoleCell(int slot,Vector2 p)=>slot==0?new Rect(p.x+38,p.y+86,32,32):new Rect(p.x+41+(slot-1)%3*38,p.y+140+(slot-1)/3*50,32,32);
        private void ResetWorldRecords(){server?.LocalWorldCharacters?.EndEditing();worldRoster=null;receivedRoster=false;selectedRecord=100;nativeInventory=null;}
    }
}
