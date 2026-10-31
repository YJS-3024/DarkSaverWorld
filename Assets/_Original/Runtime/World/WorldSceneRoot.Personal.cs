using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Tutorial;
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
            public Text Balance,Info,Level,Messages;
            public bool Visible,Minimized,Expanded;
            public float NormalY;
            public int MessageOffset;
            public Vector2? DragOffset;
            public readonly List<(WaitingButton spec,RectTransform rect)> Controls=new List<(WaitingButton,RectTransform)>();
            public readonly List<(RawImage image,int slot,bool equipped)> Items=new List<(RawImage,int,bool)>();
        }
        private readonly Dictionary<int,PersonalWindow> personalWindows=new Dictionary<int,PersonalWindow>();
        private readonly List<string> worldMessages=new List<string>();
        private RawImage personalGhost;
        private bool PersonalAvailable=>running&&view!=null&&view.facility==null&&notice==null&&view.pendingPortal==null
            &&(view.presentation==null||view.presentation.completed);
        private string CommanderClass=>definition.Map(1000).map.abilities.First(a=>a.job==character.job).name;
        private void BuildPersonalWindows()
        {
            var native=art.battle;var specs=native.WaitingDefinition.windows;
            for(int i=0;i<specs.Length;i++)
            {
                int id=specs[i].id;var spec=specs[i];
                var p=new PersonalWindow{Spec=spec,Root=Group(worldWindows,"World native window"+id,new Rect(spec.x,spec.y,spec.width,spec.height))};
                personalWindows.Add(id,p);p.Full=Group(p.Root,"Full",new Rect(0,0,spec.width,spec.height));
                p.Full.gameObject.AddComponent<Image>().color=Color.clear;
                p.Background=Picture(p.Full,"Wlib scene"+spec.scene,new Rect(0,0,spec.width,spec.height),native.waitingWindows[i]);
                foreach(var b in spec.buttons)
                {
                    int control=b.id;var hit=Hit(p.Full,"Native control"+control,new Rect(b.x,b.y,b.width,b.height),()=>PersonalControl(id,control));
                    hit.interactable=control==37||control==38||control==61||control==62||control==83||control==6
                        ||id==18&&(control==20||control==21||control==31)||id==19&&(control==58||control==59);
                    var image=Picture((RectTransform)hit.transform,"Native control states",new Rect(0,0,b.width,b.height),native.waitingButtons);
                    hit.gameObject.AddComponent<NativeEntryButton>().Configure(image,native.waitingButtons,new Rect(b.sx,b.sy,b.width,b.height),
                        new Rect(b.sx+b.width,b.sy,b.width,b.height),new Rect(b.sx+2*b.width,b.sy,b.width,b.height));
                    p.Controls.Add((b,(RectTransform)hit.transform));
                }
                var header=Hit(p.Full,"Drag native ornament",new Rect(id==18?100:15,id==18?0:3,id==18?60:230,id==18?40:12),()=>{});
                header.gameObject.AddComponent<NativeTutorialDrag>().Configure(screen,(point,end)=>
                {
                    if(!p.DragOffset.HasValue)p.DragOffset=point-new Vector2(p.Root.anchoredPosition.x,-p.Root.anchoredPosition.y);
                    Vector2 at=point-p.DragOffset.Value;float height=id==19&&p.Expanded?401:spec.height;
                    Place(p.Root,new Rect(Mathf.Clamp(Mathf.Round(at.x),0,640-spec.width),Mathf.Clamp(Mathf.Round(at.y),0,480-height),spec.width,height));
                    if(end){p.DragOffset=null;if(id==19&&!p.Expanded)p.NormalY=-p.Root.anchoredPosition.y;}
                });
                var miniature=native.waitingMinimizedWindows[i];p.Mini=Group(p.Root,"Minimized",new Rect(0,0,miniature.width,miniature.height));
                var restore=Hit(p.Mini,"Restore",new Rect(0,0,miniature.width,miniature.height),()=>{p.Minimized=false;p.Root.SetAsLastSibling();PaintPersonalWindows();});
                Picture((RectTransform)restore.transform,"Original minimized icon",new Rect(0,0,miniature.width,miniature.height),miniature);
                if(id==18)
                {
                    Picture(p.Full,"Commander32",new Rect(38,86,32,32),art.Picture(character.job),new Rect(32,32,32,32));
                    PersonalText(p.Full,"Name",new Rect(116,73,108,16),character.name);
                    PersonalText(p.Full,"Class",new Rect(116,92,108,16),CommanderClass);
                    p.Balance=PersonalText(p.Full,"Gold",new Rect(84,300,58,18),"",true);
                    for(int row=0;row<3;row++)for(int col=0;col<3;col++)BlackCell(p.Full,new Rect(41+col*38,140+row*50,32,32));
                }
                if(id==21)
                {
                    var face=character.job==100?native.waitingFighterFace:character.job==101?native.waitingKnightFace:native.waitingMagicianFace;
                    Picture(p.Full,"Commander face64",new Rect(29,46,64,64),face);
                    PersonalText(p.Full,"Name",new Rect(146,39,170,16),character.name);
                    PersonalText(p.Full,"Class",new Rect(146,56,170,16),CommanderClass);
                    p.Level=PersonalText(p.Full,"Known level",new Rect(146,74,170,16),"");
                    p.Info=PersonalText(p.Full,"Known character values",new Rect(154,146,58,80),"");p.Info.alignment=TextAnchor.UpperLeft;
                }
                if(id==20)
                {
                    for(int slot=0;slot<28;slot++)PersonalItem(p,slot,false,new Rect(146+slot%7*35,42+slot/7*35,32,32));
                    for(int slot=0;slot<WaitingEquipmentLayout.SlotCount;slot++)
                    {var pos=WaitingEquipmentLayout.Origin(slot);PersonalItem(p,slot,true,new Rect(pos.X,pos.Y,32,32));}
                    p.Balance=PersonalText(p.Full,"Gold",new Rect(52,193,56,14),"",true);
                }
                if(id==19)
                {
                    p.NormalY=spec.y;
                    p.Messages=PersonalText(p.Full,"Received messages",new Rect(10,48,552,79),"");p.Messages.color=new Color(.85f,.85f,.85f);p.Messages.alignment=TextAnchor.LowerLeft;
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
            if(!PersonalAvailable||!personalWindows.TryGetValue(id,out var p))return;
            if(p.Minimized){p.Visible=true;p.Minimized=false;}else p.Visible=!p.Visible;
            if(p.Visible)p.Root.SetAsLastSibling();PaintPersonalWindows();
        }
        private void PersonalControl(int id,int control)
        {
            var p=personalWindows[id];
            if(control==37||control==61)p.Minimized=true;
            else if(control==38||control==62||id==20&&control==6)p.Visible=false;
            else if(id==18&&(control==20||control==21||control==31))
            {
                int other=control==20?20:control==21?21:19;var opened=personalWindows[other];
                opened.Visible=true;opened.Minimized=false;opened.Root.SetAsLastSibling();
            }
            else if(id==19&&control==83)
            {
                float y=-p.Root.anchoredPosition.y;
                if(!p.Expanded){p.NormalY=y;y=Mathf.Min(y,79);}else y=p.NormalY;
                p.Expanded=!p.Expanded;Place(p.Root,new Rect(p.Root.anchoredPosition.x,y,p.Spec.width,p.Expanded?401:p.Spec.height));
            }
            else if(id==19&&(control==58||control==59))p.MessageOffset=Math.Max(0,p.MessageOffset+(control==58?1:-1));
            PaintPersonalWindows();
        }
        private WorldOwnedItem PersonalOwned(int slot,bool equipped)
        {return view==null?null:(equipped?view.record.Equipment:view.record.inventory).FirstOrDefault(i=>i.slot==slot);}
        private void PersonalItem(PersonalWindow p,int slot,bool equipped,Rect rect)
        {
            var hit=Hit(p.Full,(equipped?"Equipped":"Inventory")+slot,rect,()=>
            {var item=PersonalOwned(slot,equipped);if(item!=null)RememberMessage(definition.Item(item.item)?.name??"");});
            hit.GetComponent<Image>().color=Color.black;var image=Picture((RectTransform)hit.transform,"Owned item32",new Rect(0,0,32,32),null);image.enabled=false;
            p.Items.Add((image,slot,equipped));string owned=null;
            hit.gameObject.AddComponent<NativeTutorialDrag>().Configure(screen,(point,end)=>
            {
                if(personalGhost==null)
                {
                    var item=PersonalOwned(slot,equipped);if(item==null)return;
                    owned=item.id;p.Root.SetAsLastSibling();personalGhost=Picture(overlay,"Personal item drag32",new Rect(0,0,32,32),null);PaintItem(personalGhost,item.item);
                }
                Place((RectTransform)personalGhost.transform,new Rect(point.x-16,point.y-16,32,32));if(!end)return;
                Destroy(personalGhost.gameObject);personalGhost=null;
                var local=point-new Vector2(p.Root.anchoredPosition.x,-p.Root.anchoredPosition.y);
                int target=WaitingEquipmentLayout.SlotAt(Mathf.FloorToInt(local.x),Mathf.FloorToInt(local.y));
                var product=definition.Item(PersonalOwned(slot,equipped)?.item??-1);
                if(!equipped&&target==0&&product?.equipType==10)Handle(server.Request_UseWorldItem(owned,RequestId()));
                else if(!equipped&&target>=0)Handle(server.Request_EquipWorldItem(owned,target,RequestId()));
                else if(equipped&&new Rect(146,42,242,137).Contains(local))
                {
                    int col=Mathf.FloorToInt((local.x-146)/35),row=Mathf.FloorToInt((local.y-42)/35);
                    if(new Rect(146+col*35,42+row*35,32,32).Contains(local))Handle(server.Request_EquipWorldItem(owned,-1,RequestId(),row*7+col));
                }
            },()=>PersonalAvailable&&!view.moving&&PersonalOwned(slot,equipped)!=null);
        }
        private void RememberMessage(string message)
        {if(string.IsNullOrWhiteSpace(message))return;worldMessages.Add(message);if(worldMessages.Count>100)worldMessages.RemoveAt(0);}
        private void PaintPersonalWindows()
        {
            if(view==null)return;
            foreach(var pair in personalWindows)
            {
                var p=pair.Value;bool visible=p.Visible&&!BattleMap()&&(view.presentation==null||view.presentation.completed);
                p.Root.gameObject.SetActive(visible);if(!visible)continue;
                p.Full.gameObject.SetActive(!p.Minimized);p.Mini.gameObject.SetActive(p.Minimized);
                if(p.Balance!=null)p.Balance.text=view.record.goldKnown?view.record.gold.ToString():"—";
                if(p.Level!=null)p.Level.text=view.record.level?.ToString()??"—";
                if(p.Info!=null)p.Info.text=(view.record.healthKnown?view.record.hp.ToString():"—")+"\n"+(view.record.healthKnown?view.record.mp.ToString():"—")+"\n—\n—\n—";
                foreach(var cell in p.Items)PaintItem(cell.image,PersonalOwned(cell.slot,cell.equipped)?.item??-1);
                if(pair.Key==19)
                {
                    int height=p.Expanded?401:p.Spec.height;Place(p.Full,new Rect(0,0,p.Spec.width,height));
                    p.Background.texture=p.Expanded?art.battle.waitingChatExpanded:art.battle.waitingWindows[1];
                    Place((RectTransform)p.Background.transform,new Rect(0,0,p.Spec.width,height));
                    foreach(var c in p.Controls)Place(c.rect,new Rect(c.spec.x,c.spec.id==59&&p.Expanded?379:c.spec.y,c.spec.width,c.spec.height));
                    var bounds=(RectTransform)p.Messages.transform.parent;Place(bounds,new Rect(10,48,552,height-55));
                    Place((RectTransform)p.Messages.transform,new Rect(0,0,552,height-55));
                    int count=p.Expanded?21:4;p.MessageOffset=Math.Min(p.MessageOffset,Math.Max(0,worldMessages.Count-count));
                    p.Messages.text=string.Join("\n",worldMessages.Skip(Math.Max(0,worldMessages.Count-count-p.MessageOffset)).Take(count));
                }
            }
        }
    }
}
