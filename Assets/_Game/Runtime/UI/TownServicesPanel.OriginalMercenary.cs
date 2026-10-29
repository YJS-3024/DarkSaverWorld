using System;
using System.Collections.Generic;
using DarkSaver.Network;
using DarkSaver.World;
using DarkSaver.Player;
using UnityEngine;
namespace DarkSaver.UI
{
    public sealed partial class TownServicesPanel
    {
        private NativeBattleWindows nativeMercenaryCatalog;
        private MercenaryOffer[] nativeMercenaryOffers=Array.Empty<MercenaryOffer>();
        private int nativeMercenaryPage,nativeMercenarySelection=-1;
        private string nativeMercenaryNpc;
        private Notify_Mercenaries nativeMercenaryState;
        public void BindMercenaryState(Notify_Mercenaries state){if(state?.OwnerId==100)nativeMercenaryState=state;}
        public bool RequestOpenMercenary(ShopCounter2D npc,TopDownPlayerController controller)
        {
            if(npc==null || controller==null || IsOpen)return false;
            var server=GetComponent<ServerManager>();if(server==null)return false;server.BindLocalMercenaryService();
            if(!SendMercenaryRequest(server,new Request_OpenMercenary(npc.GetEntityId().ToString(),100),out var reply) || !(reply is Response_OpenMercenary response))return false;
            nativeMercenaryOffers=response.CaptureOffers();nativeMercenaryPage=0;nativeMercenarySelection=-1;hireMessage=null;
            if(!Open(TownService.Mercenary,controller))return false;nativeMercenaryNpc=response.NpcId;return true;
        }
        private bool SendMercenaryRequest(ServerManager server,ClientPacket request,out LocalCommandResponse response)
        {
            void Receive(ServerPacket packet){if(packet is Notify_Mercenaries state)BindMercenaryState(state);}
            server.ResponseReceived+=Receive;
            try{return server.TryRequest(request,out response);}
            finally{server.ResponseReceived-=Receive;}
        }
        public MercenaryOffer[] CaptureOriginalMercenaryOffers()
        {
            var result=new List<MercenaryOffer>();if(mercenaryJobs==null || hireSkills==null)return result.ToArray();
            // GetSoldierList10002B33 merges duplicate jobs by maximum, not sum.
            foreach(int skillId in CaptureCurrentHireSkills())if(hireSkills.TryGet(skillId,out var skill))foreach(int id in skill.CopyJobIds())
            {
                if(!mercenaryJobs.TryGet(id,out var job))continue;
                var existing=result.Find(o=>o.JobId==id);if(existing!=null){existing.Maximum=Math.Max(existing.Maximum,skill.Maximum);continue;}
                result.Add(new MercenaryOffer{JobId=id,Maximum=skill.Maximum,Price=job.GetNumericValue(33),Hp=(int)job.GetNumericValue(2),Mp=(int)job.GetNumericValue(4),Attack=(int)job.GetNumericValue(6),Defense=(int)job.GetNumericValue(8),Name=(job.Name??"").Replace('_',' '),Type=skill.Name});
            }
            return result.ToArray();
        }
        public bool HasMercenaryCounter(string id)=>nativeMercenaryNpc!=null && nativeMercenaryNpc==id;
        public bool RequestMercenaryTrade(bool hire)
        {
            if(!IsOpen || Service!=TownService.Mercenary || !CanOpen || nativeMercenaryNpc==null)return false;
            int job=nativeMercenaryState?.JobId ?? -1;
            if(job== -2){hireMessage="보유 용병 병종을 확인하세요.";return false;}
            MercenaryOffer offer=job>=0?Array.Find(nativeMercenaryOffers,o=>o.JobId==job):nativeMercenarySelection>=0 && nativeMercenarySelection<nativeMercenaryOffers.Length?nativeMercenaryOffers[nativeMercenarySelection]:null;
            ClientPacket request=hire && offer!=null?new Request_HireMercenary(100,(ushort)offer.JobId,(ushort)offer.Maximum):!hire && job>=0?new Request_DismissMercenary(100,(ushort)job):null;
            if(request==null)return false;var server=GetComponent<ServerManager>();if(server==null)return false;
            bool accepted=SendMercenaryRequest(server,request,out var response);hireMessage=response?.Reason;return accepted;
        }
        private void DrawOriginalMercenaries()
        {
            nativeMercenaryCatalog??=Resources.Load<NativeBattleWindows>("UI/Mercenary/NativeMercenary");var w=nativeMercenaryCatalog?.Find(9);
            if(w==null)return;var p=new Vector2((Rendering.GameScreen.Width-w.size.x)/2f,(Rendering.GameScreen.Height-w.size.y)/2f);
            ui.SolidRect("mercenary-list-black",new Rect(p.x+21,p.y+41,105,105),Color.black);
            ui.SolidRect("mercenary-gold-black",new Rect(p.x+50,p.y+241,70,20),Color.black);
            ui.SolidRect("mercenary-owned-black",new Rect(p.x+155,p.y+39,32,32),Color.black);
            ui.NativeTexturePanel("mercenary-window",new Rect(p,w.size),w.background);
            int first=nativeMercenaryPage*3;
            for(int i=0;i<9 && first+i<nativeMercenaryOffers.Length;i++)
            {
                int index=first+i;var row=nativeMercenaryOffers[index];var rect=new Rect(p.x+22+i%3*35,p.y+42+i/3*35,32,32);
                ui.NativeUnitPicture("mercenary-picture-"+i,rect,CharacterAdjustmentClassIcon(row.JobId));
                ui.NativeRosterSelection("mercenary-cell-"+i,rect,()=>{nativeMercenarySelection=index;hireMessage=null;},true,row.Name);
            }
            var selected=nativeMercenarySelection>=0 && nativeMercenarySelection<nativeMercenaryOffers.Length?nativeMercenaryOffers[nativeMercenarySelection]:null;
            string details=selected==null?"":selected.Name+"\n체력: "+selected.Hp+"\n공격력: "+selected.Attack+"\n방어력: "+selected.Defense+"\n가격: "+selected.Price+"\n고용가능수: "+selected.Maximum;
            ui.NativeBitmapText("mercenary-info",new Rect(p.x+24,p.y+156,126,84),hireMessage??details,Color.black,maximumLines:6,alignment:TextAnchor.UpperLeft);
            int count=nativeMercenaryState?.Count??0,job=nativeMercenaryState?.JobId??-1;
            if(count>0 && job>=0){ui.NativeUnitPicture("mercenary-current",new Rect(p.x+155,p.y+39,32,32),CharacterAdjustmentClassIcon(job));ui.NativeBitmapText("mercenary-count",new Rect(p.x+189,p.y+48,28,18),"X"+count,Color.black);}
            ui.NativeStatusNumber("mercenary-gold",p+new Vector2(50,241),(nativeMercenaryState?.Gold??0).ToString());
            for(int i=0;i<w.controls.Length;i++)
            {
                int index=i;bool enabled=i==0?selected!=null || count>0:i==1?count>0:i==3?nativeMercenaryPage>0:i==4?(nativeMercenaryPage+1)*3+9-3<nativeMercenaryOffers.Length:true;
                ui.NativeImageButton("mercenary-control-"+i,p+w.controls[i].rect.position,w.controls[i],()=>{if(index<2)RequestMercenaryTrade(index==0);else if(index==2)Close();else nativeMercenaryPage+=index==3?-1:1;},enabled,i==0?"고용":i==1?"해고":i==2?"확인":i==3?"위로":"아래로");
            }
        }
    }
}
