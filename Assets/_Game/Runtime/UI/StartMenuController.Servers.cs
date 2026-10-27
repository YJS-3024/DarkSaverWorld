using System;
using DarkSaver.Network;
using DarkSaver.Core;
using UnityEngine;
namespace DarkSaver.UI
{
    public sealed partial class StartMenuController
    {
        private LocalServerInfo[] listedServers=Array.Empty<LocalServerInfo>();
        private int proposedServerId,connectedServerId;
        public int DisplayedServerCount=>listedServers.Length;
        public int ConnectedServerId=>connectedServerId;
        private void ResetServerSelection(){listedServers=Array.Empty<LocalServerInfo>();proposedServerId=connectedServerId=0;}
        public bool RefreshServerList()
        {
            if(!authenticated || !IsAwaitingOriginalServerSelection || loading)return false;
            listedServers=Array.Empty<LocalServerInfo>();proposedServerId=0;
            if(!protocol.TryRequest(new ServerListRequest(),out var packet)
                || packet is not ServerListResponse response || response.AccountId!=authenticatedAccountId)
            {loadMessage=packet?.Reason ?? "서버 목록을 가져올 수 없습니다.";return false;}
            listedServers=response.CaptureServers();
            foreach(var server in listedServers)if(server!=null && server.Available){proposedServerId=server.Id;break;}
            loadMessage=listedServers.Length==0?"접속할 수 있는 서버가 없습니다.":string.Empty;
            return true;
        }
        public bool ChooseServer(int id)
        {
            if(!authenticated || !IsAwaitingOriginalServerSelection || loading
                || !Array.Exists(listedServers,s=>s!=null && s.Id==id && s.Available))return false;
            proposedServerId=id;return true;
        }
        public bool SelectServer(int id)
        {
            if(!ChooseServer(id))return false;
            if(!protocol.TryRequest(new ServerSelectRequest(id),out var packet)
                || packet is not ServerSelectResponse response || response.AccountId!=authenticatedAccountId || response.ServerId!=id)
            {loadMessage=packet?.Reason ?? "서버에 접속할 수 없습니다.";return false;}
            connectedServerId=id;
            if(!protocol.TryRequest(new CharacterListRequest(),out var characters)
                || characters is not CharacterListResponse list || list.SlotCount!=NativeCharacterSlots.Count
                || list.AccountId!=authenticatedAccountId || list.ServerId!=id)
            {connectedServerId=0;loadMessage=characters?.Reason ?? "캐릭터 목록을 가져올 수 없습니다.";return false;}
            IsAwaitingOriginalServerSelection=false;loggingIn=false;selecting=true;creating=registering=false;
            selectedSlot=0;loadMessage=string.Empty;return true;
        }
        private void DrawServerSelection()
        {
            // Original login.bmp server-selection artwork. Centering and the single
            // local row are this adaptation, not recovered original screen coordinates.
            var origin=new Vector2((Rendering.GameScreen.Width-94)*.5f,(Rendering.GameScreen.Height-116)*.5f);
            ui.EntryAtlasArtwork("servers-frame",new Rect(origin,new Vector2(94,116)),entryAtlas,new Rect(312,10,94,116));
            // Remove the three baked online server names. Retain the native frame/footer.
            ui.EntryAtlasArtwork("servers-clear-list",new Rect(origin.x+8,origin.y+24,78,65),entryAtlas,new Rect(316,32,1,1));
            for(int i=0;i<listedServers.Length;i++)
            {
                var server=listedServers[i];if(server==null)continue;int id=server.Id;
                var row=new Rect(origin.x+13,origin.y+26+i*21,68,19);
                ui.EntryAtlasArtwork("server-row-"+id,row,entryAtlas,new Rect(326,35,68,19));
                ui.SolidRect("server-name-cover-"+id,new Rect(row.x+9,row.y+2,57,15),
                    proposedServerId==id?new Color32(210,219,255,255):new Color32(241,241,255,255));
                ui.Label("server-name-"+id,new Rect(row.x+10,row.y,57,19),server.Name,12,color:new Color32(54,91,204,255));
                ui.NativeRosterSelection("server-hit-"+id,row,()=>ChooseServer(id),server.Available,server.Name);
            }
            ui.NativeRosterSelection("server-connect",new Rect(origin.x+8,origin.y+91,38,23),()=>SelectServer(proposedServerId),proposedServerId>0,"접속");
            ui.NativeRosterSelection("server-quit",new Rect(origin.x+48,origin.y+91,38,23),Quit,true,"종료");
            if(listedServers.Length==0)ui.Button("server-refresh",new Rect(origin.x-25,origin.y+126,144,28),"서버 목록 새로고침",()=>RefreshServerList());
            if(!string.IsNullOrEmpty(loadMessage))DrawEntryMessage(new Rect(140,origin.y+164,360,54));
        }
    }
}
