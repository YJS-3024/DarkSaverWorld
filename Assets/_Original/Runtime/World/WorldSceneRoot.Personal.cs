using System;
using System.Collections.Generic;
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
        private sealed class PersonalWindow
        {
            public WaitingWindowDefinition Spec;
            public RectTransform Root,Full,Mini;
            public RawImage Background;
            public Text Balance,Level,Experience,Class,Name,Age,Gender,Spouse,ItemCaption;
            public int CaptionCommander=-1;public long CaptionRoster;
            public readonly NativeWindowClickClock Clicks=new NativeWindowClickClock();
            public RawImage Face,Hired;
            public Text HiredCount;
            public readonly List<Text> Numbers=new List<Text>();
            public readonly List<CommanderCell> Commanders=new List<CommanderCell>();
            public readonly List<(int slot,Button hit,RawImage image,WaitingButton spec)> Deployment=new List<(int,Button,RawImage,WaitingButton)>();
            public bool Visible,Minimized,Expanded;
            public readonly List<(WaitingButton spec,RectTransform rect)> Controls=new List<(WaitingButton,RectTransform)>();
            public readonly List<(RawImage image,int slot,bool equipped)> Items=new List<(RawImage,int,bool)>();
            public readonly List<Text> ChatRows=new List<Text>();
        }
        private readonly Dictionary<int,PersonalWindow> personalWindows=new Dictionary<int,PersonalWindow>();
        private readonly NativeWorldChatLog worldMessages=new NativeWorldChatLog();
        private RawImage personalGhost;
        private bool PersonalAvailable=>running&&view!=null&&view.facility==null&&notice==null&&view.pendingPortal==null
            &&(view.presentation==null||view.presentation.completed);
        private int CurrentCommanderJob=>view?.record.growth?.job??view?.commanders?.rows[0].job??character.job;
        private string CommanderClass=>definition.growthCatalog?.Job(CurrentCommanderJob)?.name
            ??DisplayMap(1000).map.abilities.FirstOrDefault(a=>a.job==CurrentCommanderJob)?.name??CurrentCommanderJob.ToString();
        private Texture2D CommanderPicture=>art.PalettePicture(CurrentCommanderJob,art.ownPalette)??(campaign?.IsConfigured==true?campaign.Picture(CurrentCommanderJob):null);
        private void BuildPersonalWindows()
        {
            var native=art.battle;var specs=art.PersonalLayout?.windows?.ToList()??native.WaitingDefinition.windows.ToList();
            if(art.CommanderLayout!=null&&art.commanderWindow!=null&&art.commanderMinimized!=null&&!specs.Any(w=>w.id==67))
            {var c=art.CommanderLayout;var layout=NativeFacilitySpec(67,42,c.width,c.height)??new WaitingWindowDefinition{id=67,scene=42,width=c.width,height=c.height};layout.buttons=c.buttons;specs.Add(layout);}
            for(int i=0;i<specs.Count;i++)
            {
                int id=specs[i].id;var spec=specs[i];
                var saved=OriginalWindowPreferences.LoadState(spec);var at=saved.Position;bool expanded=saved.Expanded;var p=new PersonalWindow{Spec=spec,Minimized=saved.Minimized,Expanded=expanded,Root=Group(worldWindows,"World native window"+id,new Rect(at.X,at.Y,spec.width,expanded?401:spec.height))};
                personalWindows.Add(id,p);p.Full=Group(p.Root,"Full",new Rect(0,0,spec.width,spec.height));
                p.Full.gameObject.AddComponent<Image>().color=Color.clear;
                p.Full.gameObject.AddComponent<NativeWindowClickInput>().Configure(p.Clicks);
                p.Background=Picture(p.Full,"Wlib scene"+spec.scene,new Rect(0,0,spec.width,spec.height),id==67?art.commanderWindow:native.waitingWindows[i]);
                for(int controlIndex=0;controlIndex<spec.buttons.Length;controlIndex++)
                {
                    var b=spec.buttons[controlIndex];int index=controlIndex;
                    int control=b.id;var hit=Hit(p.Full,"Native control"+index,new Rect(b.x,b.y,b.width,b.height),()=>{if(id==18||id==67)CommanderControl(id,index);else if(id!=19||control!=58&&control!=59)PersonalControl(id,control);});
                    if(id==19&&(control==58||control==59))hit.gameObject.AddComponent<NativeRepeatButton>().Configure(()=>PersonalControl(id,control));
                    hit.interactable=control==37||control==38||control==61||control==62||control==83||control==6
                        ||(id==18||id==67)&&(control==20||control==21||control==31||control==33||control==34||control==36||control==48)||id==19&&(control==58||control==59)||id==21&&control==82;
                    var image=Picture((RectTransform)hit.transform,"Native control states",new Rect(0,0,b.width,b.height),native.waitingButtons);
                    hit.gameObject.AddComponent<NativeEntryButton>().Configure(image,native.waitingButtons,new Rect(b.sx,b.sy,b.width,b.height),
                        new Rect(b.sx+b.width,b.sy,b.width,b.height),new Rect(b.sx+2*b.width,b.sy,b.width,b.height));
                    p.Controls.Add((b,(RectTransform)hit.transform));
                    if((id==18||id==67)&&control==48)p.Deployment.Add((id==18?index-8:index-1,hit,image,b));
                }
                var header=Hit(p.Full,"Drag native ornament",new Rect(spec.dragX,spec.dragY,spec.dragWidth,spec.dragHeight),()=>{});header.transform.SetSiblingIndex(1);
                header.gameObject.AddComponent<NativeWindowClickInput>().Configure(p.Clicks);
                header.gameObject.AddComponent<NativeWindowDragInput>().Configure(screen,p.Root,new Rect(spec.dragX,spec.dragY,spec.dragWidth,spec.dragHeight),()=>NativeWindowDragAllowed);
                var miniature=id==67?art.commanderMinimized:native.waitingMinimizedWindows[i];p.Mini=Group(p.Root,"Minimized",new Rect(0,0,miniature.width,miniature.height));
                var restore=Hit(p.Mini,"Restore",new Rect(0,0,miniature.width,miniature.height),()=>{});
                restore.gameObject.AddComponent<NativeWindowClickInput>().Configure(p.Clicks,()=>{p.Minimized=false;p.Root.SetAsLastSibling();PaintPersonalWindows();});
                Picture((RectTransform)restore.transform,"Original minimized icon",new Rect(0,0,miniature.width,miniature.height),miniature);
                restore.gameObject.AddComponent<NativeWindowDragInput>().Configure(screen,p.Root,new Rect(spec.minDragX,spec.minDragY,spec.minDragWidth,spec.minDragHeight),()=>NativeWindowDragAllowed);
                if(id==18)
                {
                    p.Name=PersonalText(p.Full,"Name",new Rect(115,74,111,18),CommanderName);p.Name.alignment=TextAnchor.UpperLeft;
                    p.Class=PersonalText(p.Full,"Class",new Rect(115,93,111,18),CommanderClass);p.Class.alignment=TextAnchor.UpperLeft;
                    p.Balance=PersonalText(p.Full,"Gold",new Rect(83,301,58,14),"",true);
                    BuildCommanderCells(p,false);
                }
                if(id==67)
                {
                    p.Name=PersonalText(p.Full,"Name",new Rect(62,123,99,15),CommanderName);
                    p.Class=PersonalText(p.Full,"Class",new Rect(62,142,99,15),CommanderClass);
                    p.Balance=PersonalText(p.Full,"Gold",new Rect(114,237,54,14),"",true);
                    BuildCommanderCells(p,true);
                }
                if(id==21)
                {
                    p.Face=Picture(p.Full,"Commander face64",new Rect(29,46,64,64),InspectedFace);
                    p.Name=PersonalText(p.Full,"Name",new Rect(146,41,174,16),InspectedName);
                    p.Class=PersonalText(p.Full,"Class",new Rect(146,58,174,16),CommanderClass);
                    p.Level=PersonalText(p.Full,"Known level",new Rect(146,73,174,16),"");
                    p.Experience=PersonalText(p.Full,"Received experience",new Rect(146,90,174,16),"");
                    p.Age=PersonalText(p.Full,"Received age",new Rect(146,107,60,16),"—");p.Gender=PersonalText(p.Full,"Received sex",new Rect(248,107,72,16),"—");
                    p.Spouse=PersonalText(p.Full,"Received spouse",new Rect(14,145,80,16),"—");p.Spouse.alignment=TextAnchor.MiddleCenter;
                    p.Balance=PersonalText(p.Full,"Shared gold",new Rect(146,124,172,16),"");
                    p.Hired=Picture(p.Full,"Hired soldier32",new Rect(29,178,32,32),null,new Rect(32,32,32,32));
                    p.HiredCount=PersonalText(p.Full,"Hired count",new Rect(73,186,30,16),"");
                    foreach(var label in new[]{p.Name,p.Class,p.Level,p.Experience,p.Age,p.Gender,p.Balance,p.HiredCount})label.alignment=TextAnchor.UpperLeft;
                    p.Spouse.alignment=TextAnchor.UpperCenter;
                    for(int number=0;number<NativeSelfInformation.Count;number++)
                    {var label=PersonalText(p.Full,"Native numeric field"+number,new Rect(NativeSelfInformation.X(number),NativeSelfInformation.Y(number),NativeSelfInformation.Width(number),14),"");label.alignment=TextAnchor.MiddleCenter;p.Numbers.Add(label);}
                    BuildCommanderName(p);
                }
                if(id==20)
                {
                    p.ItemCaption=Label(p.Full,"Native held-item caption",new Rect(230,192,640,14),"",TextAnchor.UpperLeft);p.ItemCaption.color=Color.black;p.ItemCaption.horizontalOverflow=HorizontalWrapMode.Overflow;
                    for(int slot=0;slot<28;slot++)PersonalItem(p,slot,false,new Rect(146+slot%7*35,42+slot/7*35,32,32));
                    for(int slot=0;slot<WaitingEquipmentLayout.SlotCount;slot++)
                    {var pos=WaitingEquipmentLayout.Origin(slot);PersonalItem(p,slot,true,new Rect(pos.X,pos.Y,32,32));}
                    p.Balance=Label(p.Full,"Native right-anchored gold",new Rect(0,193,640,14),"",TextAnchor.UpperLeft);p.Balance.color=Color.white;p.Balance.horizontalOverflow=HorizontalWrapMode.Overflow;
                }
                if(id==19)
                {
                    var bounds=Group(p.Full,"Original chat viewport",new Rect(6,52,545,72));bounds.gameObject.AddComponent<RectMask2D>();
                    for(int row=0;row<23;row++)
                    {
                        var label=Label(bounds,"Received line"+row,new Rect(0,row*14,545,14),"",TextAnchor.UpperLeft);
                        ((NativeBitmapText)label).NativeParagraphWrap=true;var shadow=label.gameObject.AddComponent<Shadow>();shadow.effectColor=Color.black;shadow.effectDistance=new Vector2(1,-1);p.ChatRows.Add(label);
                    }
                }
                p.Root.gameObject.SetActive(false);
            }
        }
        private Text PersonalText(RectTransform parent,string name,Rect rect,string value,bool white=false)
        {
            var clip=Group(parent,name+" bounds",rect);clip.gameObject.AddComponent<RectMask2D>();
            var text=Label(clip,name,new Rect(0,0,rect.width,rect.height),value,white?TextAnchor.MiddleRight:TextAnchor.MiddleLeft);
            text.horizontalOverflow=HorizontalWrapMode.Overflow;if(white)text.color=Color.white;return text;
        }
        private void BlackCell(RectTransform parent,Rect rect)
        {var image=Group(parent,"Black native cell",rect).gameObject.AddComponent<Image>();image.color=Color.black;image.raycastTarget=false;}
        private void TogglePersonal(int id)
        {
            if(!PersonalAvailable)return;
            if(id==18)
            {
                if(server.CommanderAuthority!=null&&view.commanderRequest==null)Handle(server.Request_CommanderState());
                id=view.commanders?.Extended==true?67:18;
                if(personalWindows.TryGetValue(id==67?18:67,out var other))other.Visible=false;
            }
            if(!personalWindows.TryGetValue(id,out var p))return;
            if(p.Minimized){p.Visible=true;p.Minimized=false;}else p.Visible=!p.Visible;
            if(p.Visible)p.Root.SetAsLastSibling();PaintPersonalWindows();
        }
        private void PersonalControl(int id,int control)
        {
            var p=personalWindows[id];
            if(id==21&&control==82){OpenClassWindow();return;}
            if(control==37||control==61)p.Minimized=true;
            else if(control==38||control==62||id==20&&control==6)p.Visible=false;
            else if((id==18||id==67)&&(control==20||control==21||control==31))
            {
                int other=control==20?20:control==21?21:19;var opened=personalWindows[other];
                opened.Visible=true;opened.Minimized=false;opened.Root.SetAsLastSibling();
            }
            else if(id==19&&control==83)
            {
                p.Expanded=!p.Expanded;float x=p.Root.anchoredPosition.x,y=-p.Root.anchoredPosition.y;
                if(p.Expanded){x=(640-p.Spec.width)/2;y=(480-401)/2;}
                Place(p.Root,new Rect(x,y,p.Spec.width,p.Expanded?401:p.Spec.height));
            }
            else if(id==19&&(control==58||control==59))worldMessages.Scroll(control==58?-1:1);
            PaintPersonalWindows();
        }
        private WorldOwnedItem PersonalOwned(int slot,bool equipped)
        {return NativeItemDragContext.PersonalItem(view,slot,equipped);}
        private void PersonalItem(PersonalWindow p,int slot,bool equipped,Rect rect)
        {
            var hit=Hit(p.Full,(equipped?"Equipped":"Inventory")+slot,rect,()=>{});
            hit.gameObject.AddComponent<NativeWindowClickInput>().Configure(p.Clicks);
            hit.GetComponent<Image>().color=Color.black;var image=Picture((RectTransform)hit.transform,"Owned item32",new Rect(0,0,32,32),null);image.enabled=false;
            hit.gameObject.AddComponent<NativeItemPress>().Configure(pressed=>
            {
                if(p.ItemCaption==null)return;
                if(!pressed){p.ItemCaption.text="";return;}
                if(!PersonalAvailable)return;var item=PersonalOwned(slot,equipped);if(item==null)return;
                p.Root.SetAsLastSibling();p.CaptionCommander=view.inspectedCommander;p.CaptionRoster=view.commanders?.revision??0;
                p.ItemCaption.text=NativeItemCaption.Format(definition.Item(item.item)?.name,item.remainingUses);AnchorPersonalLine(p.ItemCaption,230,192,6);
            });
            p.Items.Add((image,slot,equipped));string owned=null,identity=null;int inspecting=-1;long revision=0;NativeItemDragContext dragContext=null;
            hit.gameObject.AddComponent<NativeTutorialDrag>().Configure(screen,(point,end)=>
            {
                if(personalGhost==null)
                {
                    if(end)return;dragContext=NativeItemDragContext.Personal(view,slot,equipped);if(dragContext==null||!dragContext.Matches(server.WorldSnapshot))return;
                    var item=PersonalOwned(slot,equipped);if(item==null)return;
                    owned=item.id;inspecting=view.inspectedCommander;identity=InspectedCommander?.id;revision=view.commanders?.revision??0;p.Root.SetAsLastSibling();personalGhost=Picture(overlay,"Personal item drag32",new Rect(0,0,32,32),null);PaintItem(personalGhost,item.item);
                }
                Place((RectTransform)personalGhost.transform,new Rect(point.x-16,point.y-16,32,32));if(!end)return;
                Destroy(personalGhost.gameObject);personalGhost=null;
                var local=point-new Vector2(p.Root.anchoredPosition.x,-p.Root.anchoredPosition.y);
                int target=WaitingEquipmentLayout.SlotAt(Mathf.FloorToInt(local.x),Mathf.FloorToInt(local.y));
                var latest=server.WorldSnapshot;
                if(latest==null||latest.inspectedCommander!=inspecting||(latest.commanders?.revision??0)!=revision||latest.commanders?.rows.ElementAtOrDefault(inspecting)?.id!=identity)return;
                WorldReply Equip(int destination,int inventory=-1)=>identity==null?server.Request_EquipWorldItem(owned,destination,RequestId(),inventory):server.Request_EquipCommanderItem(inspecting,identity,revision,owned,destination,RequestId(),inventory);
                var product=definition.Item(PersonalOwned(slot,equipped)?.item??-1);
                if(!equipped&&target==0&&product?.equipType==10){if(inspecting==0)Handle(server.Request_UseWorldItem(owned,RequestId()));else ShowFailure(WorldFailure.RulesUnavailable);}
                else if(!equipped&&target>=0)Handle(Equip(target));
                else if(equipped&&new Rect(146,42,242,137).Contains(local))
                {
                    int col=Mathf.FloorToInt((local.x-146)/35),row=Mathf.FloorToInt((local.y-42)/35);
                    if(new Rect(146+col*35,42+row*35,32,32).Contains(local))Handle(Equip(-1,row*7+col));
                }
            },()=>PersonalAvailable&&!view.moving&&view.commanderRequest==null&&view.itemUseRequest==null&&PersonalOwned(slot,equipped)!=null,
                ()=>{if(personalGhost!=null)Destroy(personalGhost.gameObject);personalGhost=null;dragContext=null;},
                ()=>personalGhost!=null&&p.Visible&&!p.Minimized&&PersonalAvailable&&!BattleMap()&&dragContext?.Matches(server.WorldSnapshot)==true);
        }
        private NativeCodePage949 worldTextCodec;
        private NativeBitmapFont worldBitmapFont;
        private void AnchorPersonalLine(Text label,int x,int y,int alignment)
        {
            if(worldBitmapFont==null)worldBitmapFont=Resources.Load<NativeBitmapFont>("OriginalBitmapFont");
            if(worldBitmapFont?.Ready!=true)return;int width=NativeTextMetrics.Width(label.text,worldBitmapFont.Mapping);
            Place(label.rectTransform,new Rect(NativeTextMetrics.LineOriginX(x,width,alignment),y,640,14));
        }
        private void SavePersonalPositions()
        {foreach(var p in personalWindows.Values)if(p.Root!=null)OriginalWindowPreferences.Save(p.Spec,Mathf.RoundToInt(p.Root.anchoredPosition.x),Mathf.RoundToInt(-p.Root.anchoredPosition.y),p.Expanded,p.Minimized);}
        private void RememberMessage(string message)
        {if(!string.IsNullOrWhiteSpace(message))worldMessages.Append(message,5,worldTextCodec);}
        private void ReceiveWorldMessage(WorldMessageNotice message)
        {
            if(!running||message==null||!OriginalClientPreferences.Current.AcceptsMessage(message.kind)||!personalWindows.TryGetValue(19,out var p))return;
            if(!worldMessages.Append(message.text,message.kind,worldTextCodec))return;
            if(message.kind==4){p.Visible=true;p.Minimized=false;p.Root.SetAsLastSibling();}PaintPersonalWindows();
        }
        private void PaintPersonalWindows()
        {
            if(view==null)return;
            PaintCommanderName();
            if(commanderDrag!=null&&(!PersonalAvailable||BattleMap()||!personalWindows.Any(p=>(p.Key==18||p.Key==67)&&p.Value.Visible&&!p.Value.Minimized)))ClearCommanderDrag();
            foreach(var pair in personalWindows)
            {
                var p=pair.Value;bool visible=p.Visible&&!BattleMap()&&(view.presentation==null||view.presentation.completed);
                p.Root.gameObject.SetActive(visible);if(!visible)continue;
                p.Full.gameObject.SetActive(!p.Minimized);p.Mini.gameObject.SetActive(p.Minimized);
                p.Root.sizeDelta=p.Minimized?p.Mini.sizeDelta:new Vector2(p.Spec.width,p.Expanded?401:p.Spec.height);
                if(p.ItemCaption!=null&&(p.CaptionCommander!=view.inspectedCommander||p.CaptionRoster!=(view.commanders?.revision??0)))
                {p.ItemCaption.text="";}
                if(p.Balance!=null)p.Balance.text=view.record.goldKnown?view.record.gold.ToString():"—";
                if(pair.Key==20)AnchorPersonalLine(p.Balance,104,193,2);
                var profile=InspectedProfile;
                if(p.Name!=null)p.Name.text=pair.Key==18||pair.Key==67?CommanderName:InspectedName;
                if(p.Face!=null){p.Face.texture=InspectedFace;p.Face.enabled=p.Face.texture!=null;}
                if(p.Level!=null)p.Level.text=NativeSelfInformation.Level(profile);
                if(p.Experience!=null)p.Experience.text=NativeSelfInformation.Experience(profile);
                if(p.Age!=null)p.Age.text=view.record.identity?.age?.ToString(System.Globalization.CultureInfo.InvariantCulture)??"—";
                if(p.Gender!=null)p.Gender.text=NativeSelfInformation.Gender(view.record.identity?.gender);
                if(p.Spouse!=null)p.Spouse.text=view.record.identity?.spouse??"—";
                if(p.Class!=null)p.Class.text=pair.Key==18||pair.Key==67?CommanderClass:ClassName(InspectedJob);
                PaintCommanderCells(p);
                if(p.Numbers.Count>0){var values=NativeSelfInformation.Values(profile);for(int n=0;n<p.Numbers.Count;n++)p.Numbers[n].text=values[n];}
                if(p.Hired!=null)
                {p.Hired.texture=profile?.hiredJob>=0?CommanderIcon(profile.hiredJob):null;p.Hired.enabled=p.Hired.texture!=null;if(p.Hired.enabled)p.Hired.uvRect=NativeEntryLayout.Uv(p.Hired.texture,new Rect(32,32,32,32));p.HiredCount.text=profile==null?"":"X "+profile.hiredCount;}
                foreach(var cell in p.Items)PaintItem(cell.image,PersonalOwned(cell.slot,cell.equipped)?.item??-1);
                if(pair.Key==19)
                {
                    int height=p.Expanded?401:p.Spec.height;Place(p.Full,new Rect(0,0,p.Spec.width,height));
                    p.Background.texture=p.Expanded?art.battle.waitingChatExpanded:art.battle.waitingWindows[1];
                    Place((RectTransform)p.Background.transform,new Rect(0,0,p.Spec.width,height));
                    foreach(var c in p.Controls)Place(c.rect,new Rect(c.spec.x,c.spec.id==59&&p.Expanded?379:c.spec.y,c.spec.width,c.spec.height));
                    worldMessages.SetExpanded(p.Expanded);var bounds=(RectTransform)p.ChatRows[0].transform.parent;
                    Place(bounds,p.Expanded?new Rect(10,52,545,329):new Rect(6,52,545,72));
                    var rows=worldMessages.Visible();
                    for(int row=0;row<p.ChatRows.Count;row++)
                    {
                        var label=p.ChatRows[row];label.gameObject.SetActive(row<rows.Length);if(row>=rows.Length)continue;
                        label.text=rows[row].Text;int rgb=NativeWorldChatLog.Rgb(rows[row].Kind);label.color=new Color32((byte)(rgb>>16),(byte)(rgb>>8),(byte)rgb,255);
                    }
                }
            }
        }
    }
}
