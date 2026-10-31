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
        private RectTransform panel, notice;
        private string panelToken;
        private string selectedRoom;
        private int rowOffset, selectedHire = -1;
        private readonly List<RawImage> ownedIcons = new List<RawImage>(), offerIcons = new List<RawImage>();
        private Text balance, details, storageFee, bankBalance, hireCount;
        private InputField amount;
        private RawImage dragGhost;
        private WorldWindowDefinition panelDefinition;
        private static string RequestId() => Guid.NewGuid().ToString("N");
        private void ClearPanel()
        {
            if(panel!=null){panel.gameObject.SetActive(false);Destroy(panel.gameObject);}
            panel=null;panelToken=null;selectedRoom=null;ownedIcons.Clear();offerIcons.Clear();rowOffset=0;selectedHire=-1;
            balance=details=storageFee=bankBalance=hireCount=null;amount=null;
            if(dragGhost!=null){Destroy(dragGhost.gameObject);dragGhost=null;}
        }
        private RectTransform NativeWindow(int scene,RectTransform parent,Action<int> control)
        {
            int index=Array.FindIndex(definition.windows,w=>w.scene==scene);var spec=scene==11?campaign.GatewayWindow:definition.windows[index];
            var root=Group(parent,"Wlib scene"+scene,new Rect((640-spec.width)/2,(480-spec.height)/2,spec.width,spec.height));
            var blocker=root.gameObject.AddComponent<Image>();blocker.color=Color.clear;
            Picture(root,"Original artwork",new Rect(0,0,spec.width,spec.height),scene==11?campaign.gatewayWindow:art.windows[index]);
            foreach(var b in spec.buttons)
            {
                int id=b.id;var hit=Hit(root,"Native control"+id,new Rect(b.x,b.y,b.width,b.height),()=>control(id));
                var image=Picture((RectTransform)hit.transform,"Original control",new Rect(0,0,b.width,b.height),art.buttons);
                hit.gameObject.AddComponent<NativeEntryButton>().Configure(image,art.buttons,new Rect(b.sx,b.sy,b.width,b.height),new Rect(b.sx+b.width,b.sy,b.width,b.height),new Rect(b.sx+2*b.width,b.sy,b.width,b.height));
            }
            return root;
        }
        private void PaintFacility()
        {
            if(view.facility==null){if(panel!=null)ClearPanel();return;}
            if(panelToken!=view.facility.token)
            {
                ClearPanel();panelToken=view.facility.token;
                int scene=view.facility.kind==FacilityKind.ScenarioGateway?11:view.facility.kind==FacilityKind.Shop?10:view.facility.kind==FacilityKind.Bank?18:view.facility.kind==FacilityKind.Warehouse?20:view.facility.kind==FacilityKind.Mercenary?9:17;
                panelDefinition=scene==11?campaign.GatewayWindow:definition.windows.First(w=>w.scene==scene);panel=NativeWindow(scene,worldWindows,FacilityControl);
                if(view.facility.kind==FacilityKind.ScenarioGateway)
                {
                    string room=view.facility.room;string title=definition.Map(view.facility.scenario).title;
                    Label(panel,"Scenario title",new Rect(105,27,380,26),title);
                    var row=Hit(panel,"Local scenario room",new Rect(20,135,442,24),()=>{selectedRoom=room;details.color=Color.yellow;});
                    details=Label((RectTransform)row.transform,"Room title",new Rect(4,3,434,20),title);details.color=Color.black;
                }
                else if(view.facility.kind==FacilityKind.Shop||view.facility.kind==FacilityKind.Warehouse)
                {
                    for(int i=0;i<28;i++)ownedIcons.Add(ItemCell(i,false));
                    for(int i=0;i<12;i++)offerIcons.Add(ItemCell(i,true));
                    balance=Label(panel,"Gold",new Rect(53,265,110,18),"",TextAnchor.MiddleLeft);
                    details=Label(panel,"Selected item",new Rect(20,190,370,58),"");
                    if(view.facility.kind==FacilityKind.Warehouse)storageFee=Label(panel,"Storage fee",new Rect(239,265,100,18),"");
                }
                else if(view.facility.kind==FacilityKind.Bank)
                {
                    balance=Label(panel,"Wallet",new Rect(53,44,60,18),"",TextAnchor.MiddleRight);
                    bankBalance=Label(panel,"Bank balance",new Rect(193,16,116,18),"",TextAnchor.MiddleRight);bankBalance.color=Color.white;
                    amount=Group(panel,"Amount input",new Rect(53,43,64,24)).gameObject.AddComponent<InputField>();
                    var bg=amount.gameObject.AddComponent<Image>();bg.color=Color.clear;var text=Label((RectTransform)amount.transform,"Amount",new Rect(0,0,64,20),"",TextAnchor.MiddleRight);text.color=Color.white;
                    amount.textComponent=text;amount.targetGraphic=bg;amount.contentType=InputField.ContentType.IntegerNumber;amount.characterLimit=20;amount.text="";
                    // The lower native black box is the entered amount, not a second balance.
                    balance.gameObject.SetActive(false);
                }
                else if(view.facility.kind==FacilityKind.Mercenary)
                {
                    for(int i=0;i<9;i++)
                    {
                        int cell=i;var hit=Hit(panel,"Mercenary cell"+i,new Rect(22+i%3*35,42+i/3*35,32,32),()=>SelectHire(cell));
                        var bg=hit.GetComponent<Image>();bg.color=Color.black;offerIcons.Add(Picture((RectTransform)hit.transform,"Mercenary",new Rect(0,0,32,32),null));
                    }
                    details=Label(panel,"Mercenary details",new Rect(16,160,132,78),"");
                    hireCount=Label(panel,"Owned count",new Rect(152,76,55,55),"",TextAnchor.MiddleCenter);
                    balance=Label(panel,"Gold",new Rect(46,243,90,18),"");
                }
                else
                {
                    details=Label(panel,"Revive",new Rect(20,12,200,22),"부활",TextAnchor.MiddleCenter);
                    balance=Label(panel,"Revival cost",new Rect(52,43,95,20),"");
                }
            }
            var f=view.facility;string gold=view.record.goldKnown?view.record.gold.ToString():"—";
            if(balance!=null)balance.text=f.kind==FacilityKind.Temple?(definition.rules.reviveRuleKnown?definition.rules.reviveFee.ToString():"—"):gold;
            if(bankBalance!=null)bankBalance.text=view.record.bankKnown?view.record.bank.ToString():"—";
            if(storageFee!=null)storageFee.text=definition.rules.storageRuleKnown?"보관료 : "+f.storageFee:"보관료 : —";
            if(f.kind==FacilityKind.Shop||f.kind==FacilityKind.Warehouse)
            {
                for(int i=0;i<ownedIcons.Count;i++)PaintItem(ownedIcons[i],f.inventory.FirstOrDefault(v=>v.slot==i)?.item??-1);
                for(int i=0;i<offerIcons.Count;i++)PaintItem(offerIcons[i],RightItem(i));
            }
            if(f.kind==FacilityKind.Mercenary)
            {
                var offers=HireOffers();
                for(int i=0;i<offerIcons.Count;i++)
                {var offer=i+rowOffset*3<offers.Length?offers[i+rowOffset*3]:null;var image=offerIcons[i];image.enabled=offer!=null;if(offer!=null){image.texture=art.Picture(offer.picture);image.uvRect=NativeEntryLayout.Uv(image.texture,new Rect(32,32,32,32));}}
                hireCount.text=view.record.hiredCount==0?"0":view.record.hiredCount.ToString();
            }
        }
        private WorldHireOffer[] HireOffers()=>definition.hires.Where(h=>h.commander==character.job).ToArray();
        private void SelectHire(int cell)
        {
            var offers=HireOffers();int i=cell+rowOffset*3;if(i>=offers.Length)return;
            selectedHire=offers[i].job;details.text=offers[i].name+"\n가격 : "+offers[i].price+"\n최대 : "+offers[i].limit;
        }
        private int RightItem(int cell)
        {
            int i=cell+rowOffset*3;
            if(view.facility.kind==FacilityKind.Shop)return i<view.facility.offers.Length?view.facility.offers[i]:-1;
            return view.facility.storage.FirstOrDefault(s=>s.slot==i)?.item??-1;
        }
        private void PaintItem(RawImage image,int id)
        {
            var item=definition.Item(id);var icon=item==null?null:ItemIcon(item.picture);image.enabled=icon!=null;
            image.texture=icon;image.uvRect=new Rect(0,0,1,1);
        }
        private RawImage ItemCell(int slot,bool right)
        {
            int columns=right?3:7;var origin=right?new Vector2(278,40):new Vector2(20,40);
            var hit=Hit(panel,(right?"Right":"Owned")+slot,new Rect(origin.x+slot%columns*35,origin.y+slot/columns*35,32,32),()=>InspectItem(right?RightItem(slot):view.facility.inventory.FirstOrDefault(i=>i.slot==slot)?.item??-1));
            hit.GetComponent<Image>().color=Color.black;
            var image=Picture((RectTransform)hit.transform,"Native item",new Rect(0,0,32,32),null);image.enabled=false;
            int dragged=-1;string owned=null;
            hit.gameObject.AddComponent<NativeTutorialDrag>().Configure(screen,(point,end)=>
            {
                if(dragGhost==null)
                {dragGhost=Picture(overlay,"Dragged item",new Rect(0,0,32,32),null);dragged=right?RightItem(slot):view.facility.inventory.FirstOrDefault(i=>i.slot==slot)?.item??-1;owned=right?view.facility.storage.FirstOrDefault(i=>i.slot==slot+rowOffset*3)?.id:view.facility.inventory.FirstOrDefault(i=>i.slot==slot)?.id;PaintItem(dragGhost,dragged);}
                Place((RectTransform)dragGhost.transform,new Rect(point.x-16,point.y-16,32,32));
                if(!end)return;
                Destroy(dragGhost.gameObject);dragGhost=null;
                var local=point-new Vector2(panel.anchoredPosition.x,-panel.anchoredPosition.y);
                bool valid=right?new Rect(20,40,242,137).Contains(local):new Rect(278,40,102,137).Contains(local);
                if(!valid||dragged<0)return;
                string token=panelToken;
                if(view.facility.kind==FacilityKind.Shop)
                {
                    int item=dragged;string instance=owned;
                    InspectItem(item);ShowTradeConfirmation(item,right,()=>Handle(right?server.Request_BuyItem(item,token,RequestId()):server.Request_SellItem(instance,token,RequestId())));
                }
                else Handle(right?server.Request_RetrieveItem(owned,token,RequestId()):server.Request_StoreItem(owned,token,RequestId()));
            },()=>view?.facility!=null&&(right?RightItem(slot)>=0:view.facility.inventory.Any(i=>i.slot==slot))&&notice==null);
            return image;
        }
        private void InspectItem(int id)
        {
            var item=definition.Item(id);if(item==null||details==null)return;
            details.text=item.name+"    가격 : "+(item.price>=0?item.price.ToString():"—")+"\n"+item.description;
        }
        private void FacilityControl(int id)
        {
            if(view?.facility==null)return;var f=view.facility;string request=RequestId();
            if(f.kind==FacilityKind.ScenarioGateway&&id==9)
            {if(selectedRoom==null){ShowFailure(WorldFailure.InvalidInput);return;}Handle(server.Request_JoinScenarioRoom(selectedRoom,f.token,request));return;}
            if(id==40){rowOffset=Math.Max(0,rowOffset-1);return;}
            if(id==39)
            {
                int count=f.kind==FacilityKind.Shop?f.offers.Length:f.kind==FacilityKind.Mercenary?HireOffers().Length:f.storage.Length==0?0:f.storage.Max(s=>s.slot)+1;
                int visible=f.kind==FacilityKind.Mercenary?9:12;if(rowOffset*3+visible<count)rowOffset++;return;
            }
            if(f.kind==FacilityKind.Bank)
            {
                if(id==6){Handle(server.Request_CloseFacility());return;}
                if(!ulong.TryParse(amount.text,out var number)||number==0){ShowFailure(WorldFailure.InvalidInput);return;}
                Handle(id==24?server.Request_BankDeposit(number,f.token,request):server.Request_BankWithdraw(number,f.token,request));return;
            }
            if(f.kind==FacilityKind.Warehouse&&id==19){Handle(server.Request_ConfirmStorage(f.token,request));return;}
            if(f.kind==FacilityKind.Temple&&id==19){Handle(server.Request_Revive(f.token,request));return;}
            if(f.kind==FacilityKind.Mercenary)
            {
                if(id==8){int job=view.record.hiredCount>0?view.record.hiredJob:selectedHire;if(job<0){ShowFailure(WorldFailure.InvalidInput);return;}Handle(server.Request_HireMercenary(job,f.token,request));return;}
                if(id==7){Handle(server.Request_DismissMercenary(f.token,request));return;}
            }
            if(id==19)Handle(server.Request_CloseFacility());
        }
        private void ShowTradeConfirmation(int item,bool buying,Action confirmed)
        {
            CloseNotice();var root=NoticeBlocker();notice=NativeWindow(17,root,id=>{CloseNotice();confirmed();});
            var product=definition.Item(item);Label(notice,"Trade",new Rect(12,9,215,28),(buying?"구매 : ":"판매 : ")+product.name,TextAnchor.MiddleCenter);
            Label(notice,"Price",new Rect(54,46,94,20),buying?product.price.ToString():product.sellKnown?product.sellPrice.ToString():"—");
        }
        private RectTransform NoticeBlocker()
        {var root=Group(overlay,"Modal input",new Rect(0,0,640,480));var image=root.gameObject.AddComponent<Image>();image.color=Color.clear;return root;}
        private void CloseNotice()
        {if(notice==null)return;var root=notice.parent;root.gameObject.SetActive(false);Destroy(root.gameObject);notice=null;}
        private void ShowFailure(WorldFailure failure)
        {
            if(failure==WorldFailure.None)return;CloseNotice();var root=NoticeBlocker();notice=NativeWindow(29,root,_=>{CloseNotice();if(!running)Begin(character);});
            string message=failure==WorldFailure.TooFar?"직원 옆이나 가판대 앞에서 이용해 주세요.":failure==WorldFailure.RulesUnavailable?"처리에 필요한 정보가 확인되지 않았습니다.":failure==WorldFailure.InsufficientFunds?"돈이 부족합니다.":failure==WorldFailure.StorageUnavailable?"저장하지 못했습니다. 다시 시도해 주세요.":failure==WorldFailure.InventoryFull?"물품을 놓을 공간이 없습니다.":failure==WorldFailure.Moving?"이동을 마친 뒤 이용해 주세요.":failure==WorldFailure.StaleRequest?"요청이 만료되었습니다. 다시 선택해 주세요.":"지금은 이 요청을 처리할 수 없습니다.";
            RememberMessage(message);
            Label(notice,"Message",new Rect(15,9,498,28),message,TextAnchor.MiddleCenter);
        }
        private void ShowQuit()
        {CloseNotice();var root=NoticeBlocker();notice=NativeWindow(27,root,id=>{CloseNotice();if(id!=19)return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        });}
        private void ShowTrainingEscape()
        {
            if(view==null||!definition.Map(view.record.map).training)return;
            CloseNotice();var root=NoticeBlocker();var spec=art.battle.Definition.windows.First(w=>w.key==9);
            notice=BattleWindow(root,spec,id=>{CloseNotice();if(id==19)Handle(server.Request_EscapeTraining());});
        }
        private RectTransform BattleWindow(RectTransform root,TutorialNativeWindow spec,Action<int> pressed)
        {
            int index=Array.FindIndex(art.battle.Definition.windows,w=>w.key==spec.key);var rect=Group(root,"Glib"+spec.scene,new Rect((640-spec.width)/2,(480-spec.height)/2,spec.width,spec.height));
            Picture(rect,"Original battle panel",new Rect(0,0,spec.width,spec.height),art.battle.windows[index]);
            foreach(var b in spec.buttons)
            {int id=b.id;var hit=Hit(rect,"Control"+id,new Rect(b.x,b.y,b.width,b.height),()=>pressed(id));var image=Picture((RectTransform)hit.transform,"Native button",new Rect(0,0,b.width,b.height),art.battle.buttons);hit.gameObject.AddComponent<NativeEntryButton>().Configure(image,art.battle.buttons,new Rect(b.sx,b.sy,b.width,b.height),new Rect(b.sx+b.width,b.sy,b.width,b.height),new Rect(b.sx+2*b.width,b.sy,b.width,b.height));}
            return rect;
        }
        private void ShowTrainingOptions()
        {
            CloseNotice();var root=NoticeBlocker();var spec=art.battle.Definition.windows.First(w=>w.key==12);
            notice=BattleWindow(root,spec,id=>{CloseNotice();if(id==59)ShowTrainingEscape();else if(id==52)ToggleTrainingStatus();else if(id!=38)ShowFailure(WorldFailure.RulesUnavailable);});
        }
    }
}
