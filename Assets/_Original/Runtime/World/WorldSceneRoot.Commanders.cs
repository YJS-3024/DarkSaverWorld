using System;
using System.Linq;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private sealed class CommanderCell
        {
            public int Slot;public Rect Bounds;public Button Hit;public RawImage Body;
            public Image Back;
        }
        private RawImage commanderDrag;
        private long commanderDragRevision;
        private string commanderDragIdentity;
        private int commanderDragSlot=-1;
        private OwnedCommander InspectedCommander=>view?.commanders?.rows?.ElementAtOrDefault(view.inspectedCommander);
        private int InspectedJob=>InspectedCommander?.job??CurrentCommanderJob;
        private string CommanderName=>view?.commanders==null?character.name:view.commanders.rows[0].name??"";
        private string InspectedName=>view?.commanders==null?character.name:InspectedCommander?.name??"";
        private CommanderProfile InspectedProfile=>view==null?null:view.inspectedCommander==0?CommanderProfile.Capture(view.record):InspectedCommander?.profile;
        private Texture2D CommanderIcon(int job)=>art.PalettePicture(job,art.ownPalette)??(campaign?.IsConfigured==true?campaign.Picture(job):null);
        private string ClassName(int job)=>definition.growthCatalog?.Job(job)?.name
            ??DisplayMap(1000).map.abilities.FirstOrDefault(a=>a.job==job)?.name??"";
        private Texture2D InspectedFace=>InspectedJob==100?art.battle.waitingFighterFace:InspectedJob==101?art.battle.waitingKnightFace:
            InspectedJob==103?art.battle.waitingMagicianFace:campaign?.IsConfigured==true?campaign.Face(InspectedJob):null;
        private void BuildCommanderCells(PersonalWindow p,bool alternate)
        {
            int count=alternate?3:10;
            for(int slot=0;slot<count;slot++)
            {
                int index=slot;var bounds=alternate?slot==0?new Rect(69,73,32,32):new Rect(113+(slot-1)*38,70,32,32)
                    :slot==0?new Rect(38,86,32,32):new Rect(41+(slot-1)%3*38,140+(slot-1)/3*50,32,32);
                var hit=Hit(p.Full,"Owned commander"+slot,bounds,()=>
                {
                    if(!PersonalAvailable||view.moving)return;
                    var reply=server.Request_InspectCommander(index);Handle(reply);
                    if(reply.Accepted){CloseClassWindow();PaintPersonalWindows();}
                });
                hit.gameObject.AddComponent<NativeWindowClickInput>().Configure(p.Clicks);
                hit.GetComponent<Image>().color=Color.black;
                var cell=new CommanderCell{Slot=slot,Bounds=bounds,Hit=hit,Body=Picture((RectTransform)hit.transform,"Original body32",new Rect(0,0,32,32),null),Back=hit.GetComponent<Image>()};
                p.Commanders.Add(cell);
                hit.gameObject.AddComponent<NativeTutorialDrag>().Configure(screen,(point,end)=>DragCommander(p,cell,point,end),
                    ()=>PersonalAvailable&&!view.moving&&view.commanderRequest==null&&view.commanders!=null&&index<view.commanders.rows.Length,ClearCommanderDrag);
            }
        }
        private void DragCommander(PersonalWindow panel,CommanderCell cell,Vector2 point,bool end)
        {
            if(commanderDrag==null)
            {
                var rows=view.commanders?.rows;if(rows==null||cell.Slot>=rows.Length)return;
                var inspected=server.Request_InspectCommander(cell.Slot);Handle(inspected);if(!inspected.Accepted)return;
                CloseClassWindow();panel.Root.SetAsLastSibling();commanderDragRevision=view.commanders.revision;commanderDragIdentity=rows[cell.Slot].id;commanderDragSlot=cell.Slot;
                int job=rows[cell.Slot].job;commanderDrag=Picture(overlay,"Original commander exchange drag32",new Rect(point.x-16,point.y-16,32,32),CommanderIcon(job),new Rect(32,32,32,32));
            }
            Place((RectTransform)commanderDrag.transform,new Rect(point.x-16,point.y-16,32,32));if(!end)return;
            var local=point-new Vector2(panel.Root.anchoredPosition.x,-panel.Root.anchoredPosition.y);
            var target=panel.Commanders.FirstOrDefault(c=>c.Bounds.Contains(local));var latest=server.WorldSnapshot;
            bool valid=PersonalAvailable&&latest?.commanders?.revision==commanderDragRevision&&latest.commanders.rows.ElementAtOrDefault(commanderDragSlot)?.id==commanderDragIdentity;
            int source=commanderDragSlot;ClearCommanderDrag();
            if(valid&&target!=null&&target.Slot!=source&&target.Slot<latest.commanders.rows.Length)Handle(server.Request_SwapCommanders(source,target.Slot));
        }
        private void ClearCommanderDrag()
        {if(commanderDrag!=null)Destroy(commanderDrag.gameObject);commanderDrag=null;commanderDragIdentity=null;commanderDragSlot=-1;}
        private void PaintCommanderCells(PersonalWindow panel)
        {
            foreach(var cell in panel.Commanders)
            {
                int count=view.commanders?.rows.Length??1;bool owned=cell.Slot<count;
                int job=owned?(view.commanders?.rows[cell.Slot].job??CurrentCommanderJob):-1;
                cell.Hit.interactable=owned&&PersonalAvailable&&!view.moving&&view.commanderRequest==null;
                cell.Body.texture=owned?CommanderIcon(job):null;bool dragging=commanderDrag!=null&&commanderDragSlot==cell.Slot;cell.Body.enabled=cell.Body.texture!=null&&!dragging;
                if(cell.Body.enabled)cell.Body.uvRect=NativeEntryLayout.Uv(cell.Body.texture,new Rect(32,32,32,32));
                cell.Back.color=dragging?Color.black:owned&&cell.Slot==view.inspectedCommander?new Color32(50,50,200,255)
                    :owned&&view.commanders?.rows[cell.Slot].nativeStatus==2?new Color32(192,192,192,255):Color.black;
            }
            foreach(var toggle in panel.Deployment)
            {
                var row=view.commanders?.rows.ElementAtOrDefault(toggle.slot);bool known=row?.deployment!=null&&row.nativeRecord.HasValue;
                toggle.hit.gameObject.SetActive(known);toggle.hit.interactable=known&&PersonalAvailable&&!view.moving&&view.commanderRequest==null;
                if(known){var b=toggle.spec;var checkedRect=new Rect(b.sx+b.width,b.sy,b.width,b.height);toggle.hit.GetComponent<NativeEntryButton>().Configure(toggle.image,art.battle.waitingButtons,new Rect(b.sx+(row.deployment==1?b.width:0),b.sy,b.width,b.height),checkedRect,checkedRect);}
            }
            if(commanderDrag!=null&&(!PersonalAvailable||!panel.Visible||view.commanders?.revision!=commanderDragRevision))ClearCommanderDrag();
        }
        private void CommanderControl(int window,int index)
        {
            var p=personalWindows[window];var b=p.Spec.buttons[index];
            if(b.id==48)
            {
                int slot=window==18?index-8:index-1;Handle(server.Request_ToggleCommanderDeployment(slot));return;
            }
            if(b.id==33){ToggleUtility(27);return;}
            if(b.id==34){ToggleUtility(33);return;}
            if(b.id==36){ShowQuit();return;}
            PersonalControl(window,b.id);
        }
    }
}
