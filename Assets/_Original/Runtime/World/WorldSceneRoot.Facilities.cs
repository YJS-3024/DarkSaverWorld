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
        private RectTransform panel, notice;
        private string panelToken;
        private string selectedRoom;
        private int rowOffset, selectedHire = -1;
        private readonly List<RawImage> ownedIcons = new List<RawImage>(), offerIcons = new List<RawImage>();
        private readonly List<RawImage> ownedSelection=new List<RawImage>(),offerSelection=new List<RawImage>();
        private readonly NativeShopSelection shopSelection=new NativeShopSelection();private Texture2D facilitySelectionTexture;
        private Text balance, details, storageFee, bankBalance, hireCount;
        private NativeShopDetailsCatalog shopDetails;
        private RawImage dragGhost,hiredIcon;
        private WorldWindowDefinition panelDefinition;
        private string revivalQuoteAttempt;
        private string currencyBankAttempt;
        private string warehouseStateAttempt;
        private NativeWarehouseDisplay shownWarehouse;
        private sealed class NativeFacilityFrame
        {public WaitingWindowDefinition Spec;public RectTransform Header;}
        private NativeFacilityWindows facilityWindowCatalog;
        private bool NativeWindowDragAllowed=>running&&notice==null&&!clientMessageState.Visible;
        private readonly Dictionary<RectTransform,NativeFacilityFrame> facilityFrames=new Dictionary<RectTransform,NativeFacilityFrame>();
        private WaitingWindowDefinition NativeFacilitySpec(int id,int scene,int width,int height)
        {if(facilityWindowCatalog==null){var data=Resources.Load<TextAsset>("OriginalFacilityWindows");if(data!=null)facilityWindowCatalog=JsonUtility.FromJson<NativeFacilityWindows>(data.text);}return facilityWindowCatalog?.Find(id,scene,width,height);}
        private void BindNativeFacilityFrame(RectTransform frame,int id,int scene,int width,int height)
        {
            if(id==0)return;
            if(facilityFrames.TryGetValue(frame,out var old)&&old.Spec.id==id)return;
            var spec=NativeFacilitySpec(id,scene,width,height);if(spec==null)return;
            SaveNativeFacilityFrame(frame);var at=OriginalWindowPreferences.Load(spec);Place(frame,new Rect(at.X,at.Y,width,height));
            var state=new NativeFacilityFrame{Spec=spec};facilityFrames.Add(frame,state);
            if(spec.dragWidth<=0||spec.dragHeight<=0)return;
            var region=new Rect(spec.dragX,spec.dragY,spec.dragWidth,spec.dragHeight);var header=Hit(frame,"Original facility title",region,()=>{});header.transform.SetSiblingIndex(1);state.Header=(RectTransform)header.transform;
            header.gameObject.AddComponent<NativeWindowDragInput>().Configure(screen,frame,region,()=>NativeWindowDragAllowed);
        }
        private void SaveNativeFacilityFrame(RectTransform frame)
        {
            if(frame==null||!facilityFrames.TryGetValue(frame,out var state))return;
            OriginalWindowPreferences.Save(state.Spec,Mathf.RoundToInt(frame.anchoredPosition.x),Mathf.RoundToInt(-frame.anchoredPosition.y));facilityFrames.Remove(frame);
            if(state.Header!=null){state.Header.gameObject.SetActive(false);Destroy(state.Header.gameObject);}
        }
        private void SaveNativeFacilityFrames()
        {foreach(var pair in facilityFrames)if(pair.Key!=null)OriginalWindowPreferences.Save(pair.Value.Spec,Mathf.RoundToInt(pair.Key.anchoredPosition.x),Mathf.RoundToInt(-pair.Key.anchoredPosition.y));}
        private void MaintainWarehouseState()
        {
            var f=view.facility;
            if(f?.kind!=FacilityKind.Warehouse){warehouseStateAttempt=null;return;}
            if(server.WarehouseClient(f.token)!=null)return;
            if(server.WarehouseAuthority==null||f.warehouseTerms!=null||view.warehouseRequest!=null||warehouseStateAttempt==f.token)return;
            warehouseStateAttempt=f.token;Handle(server.Request_WarehouseState(f.token,RequestId()));
        }
        private void MaintainCurrencyState()
        {
            var f=view.facility;
            if(f?.kind!=FacilityKind.Bank){currencyBankAttempt=null;return;}
            if(view.bankTransferRequest!=null||currencyBankAttempt==f.token)return;
            var result=server.Request_BankBalance(f.token);if(result.Accepted)currencyBankAttempt=f.token;
            else if(result.Failure!=WorldFailure.Busy){currencyBankAttempt=f.token;Handle(result);}
        }
        private void MaintainRevivalQuote()
        {
            var f=view.facility;
            if(f?.kind!=FacilityKind.Temple){revivalQuoteAttempt=null;return;}
            if(server.RevivalQuoteAuthority==null||!view.record.ghost||f.revivalQuote!=null||view.revivalQuoteRequest!=null||view.revivalRequest!=null||revivalQuoteAttempt==f.token)return;
            revivalQuoteAttempt=f.token;Handle(server.Request_RevivalQuote(f.token,RequestId()));
        }
        private static string RequestId() => Guid.NewGuid().ToString("N");
        private void ClearPanel()
        {
            if(panel!=null&&panelDefinition?.scene==18&&panelToken!=null){if(bankAmountEdit!=null)CloseNotice();server?.Request_CancelBankClient(panelToken);}
            if(panel!=null&&panelDefinition?.scene==20&&panelToken!=null)server?.Request_CancelWarehouseClient(panelToken);
            if(panel!=null&&panelDefinition?.scene==10&&panelToken!=null)server?.Request_CancelShopClient(panelToken);
            if(panel!=null&&panelDefinition?.scene==9&&panelToken!=null)server?.Request_CancelMercenaryClient(panelToken);
            if(panel!=null){SaveNativeFacilityFrame(panel);panel.gameObject.SetActive(false);Destroy(panel.gameObject);}
            panel=null;panelDefinition=null;panelToken=null;shownWarehouse=null;selectedRoom=null;ownedIcons.Clear();offerIcons.Clear();rowOffset=0;selectedHire=-1;
            ownedSelection.Clear();offerSelection.Clear();shopSelection.Clear();
            balance=details=storageFee=bankBalance=hireCount=null;hiredIcon=null;
            if(dragGhost!=null){Destroy(dragGhost.gameObject);dragGhost=null;}
        }
        private RectTransform NativeWindow(int scene,RectTransform parent,Action<int> control,int nativeId=0)
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
            BindNativeFacilityFrame(root,nativeId!=0?nativeId:NativeFacilityWindows.DefaultId(scene),scene,spec.width,spec.height);return root;
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
                    string room=view.facility.room;string title=DisplayMap(view.facility.scenario).title;
                    Label(panel,"Scenario title",new Rect(105,27,380,26),title);
                    var row=Hit(panel,"Local scenario room",new Rect(20,135,442,24),()=>{selectedRoom=room;details.color=Color.yellow;});
                    details=Label((RectTransform)row.transform,"Room title",new Rect(4,3,434,20),title);details.color=Color.black;
                }
                else if(view.facility.kind==FacilityKind.Shop||view.facility.kind==FacilityKind.Warehouse)
                {
                    for(int i=0;i<28;i++)ownedIcons.Add(ItemCell(i,false));
                    for(int i=0;i<12;i++)offerIcons.Add(ItemCell(i,true));
                    balance=Label(panel,"Gold",new Rect(NativeShopStorageLayout.WalletX,NativeShopStorageLayout.WalletY,640,14),"");balance.color=Color.white;
                    details=Label(panel,"Selected item",new Rect(NativeShopDetails.X,NativeShopDetails.Y,NativeShopDetails.Width,NativeShopDetails.Height),"");
                    details.horizontalOverflow=HorizontalWrapMode.Overflow;
                    if(view.facility.kind==FacilityKind.Warehouse)storageFee=Label(panel,"Storage fee",new Rect(NativeShopStorageLayout.FeeX,NativeShopStorageLayout.FeeY,190,14),"");
                }
                else if(view.facility.kind==FacilityKind.Bank)
                {
                    balance=Label(panel,"Wallet",new Rect(NativeBankLayout.WalletX,NativeBankLayout.WalletY,640,14),"");balance.color=Color.white;
                    bankBalance=Label(panel,"Bank balance",new Rect(NativeBankLayout.SavingsX,NativeBankLayout.SavingsY,640,14),"");bankBalance.color=Color.white;
                }
                else if(view.facility.kind==FacilityKind.Mercenary)
                {
                    for(int i=0;i<9;i++)
                    {
                        int cell=i;var hit=Hit(panel,"Mercenary cell"+i,new Rect(22+i%3*35,42+i/3*35,32,32),()=>SelectHire(cell));
                        hit.gameObject.AddComponent<NativeItemPress>().Configure(down=>{if(down)SelectHire(cell);});
                        var bg=hit.GetComponent<Image>();bg.color=Color.black;offerIcons.Add(Picture((RectTransform)hit.transform,"Mercenary",new Rect(0,0,32,32),null));
                    }
                    hiredIcon=Picture(panel,"Owned mercenary",new Rect(NativeMercenaryLayout.OwnedX,NativeMercenaryLayout.OwnedY,32,32),null);
                    details=Label(panel,"Mercenary details",new Rect(NativeMercenaryLayout.TextX,NativeMercenaryLayout.TextY,NativeMercenaryLayout.TextWidth,NativeMercenaryLayout.TextHeight),"");((NativeBitmapText)details).NativeParagraphWrap=true;
                    hireCount=Label(panel,"Owned count",new Rect(NativeMercenaryLayout.CountX,NativeMercenaryLayout.CountY,NativeMercenaryLayout.CountWidth,NativeMercenaryLayout.CountHeight),"");((NativeBitmapText)hireCount).NativeParagraphWrap=true;
                    balance=Label(panel,"Gold",new Rect(0,NativeMercenaryLayout.GoldY,640,14),"");balance.color=Color.white;
                }
                else
                {
                    details=Label(panel,"Revive",new Rect(20,12,200,22),"부활",TextAnchor.MiddleCenter);
                    balance=Label(panel,"Revival cost",new Rect(52,43,95,20),"");
                }
            }
            var f=view.facility;string gold=view.record.goldKnown?NativeCurrencyText.Wallet(view.record.gold):"—";
            if(f.kind==FacilityKind.Shop&&server.ShopClientWindow(f.token)is int nativeId)BindNativeFacilityFrame(panel,nativeId,10,panelDefinition.width,panelDefinition.height);
            if(balance!=null)balance.text=f.kind==FacilityKind.Temple?(f.revivalQuote?.fee?.ToString()??(server.RevivalQuoteAuthority==null&&server.RevivalAuthority==null&&definition.rules.reviveRuleKnown?definition.rules.reviveFee.ToString():"—")):gold;
            if(bankBalance!=null)
            {bankBalance.text=server.BankBalanceReady(f.token)&&view.record.bankKnown?NativeCurrencyText.Savings(view.record.bank):"";AnchorPersonalLine(balance,NativeBankLayout.WalletX,NativeBankLayout.WalletY,2);AnchorPersonalLine(bankBalance,NativeBankLayout.SavingsX,NativeBankLayout.SavingsY,2);}
            PaintBankAmount();
            shownWarehouse=f.kind==FacilityKind.Warehouse?server.WarehouseClient(f.token)?.Display():null;
            if(storageFee!=null)storageFee.text=shownWarehouse!=null?(shownWarehouse.Loaded?NativeShopStorageLayout.Fee(shownWarehouse.Fee):"보관료 : —"):f.storageFeeKnown?"보관료 : "+f.storageFee:"보관료 : —";
            if(f.kind==FacilityKind.Shop||f.kind==FacilityKind.Warehouse)
            {
                if(shopSelection.Index>=0&&!shopSelection.Right&&f.inventory.All(item=>item.slot!=shopSelection.Index)){shopSelection.Clear();details.text="";}
                AnchorPersonalLine(balance,NativeShopStorageLayout.WalletX,NativeShopStorageLayout.WalletY,2);
                for(int i=0;i<ownedIcons.Count;i++)PaintItem(ownedIcons[i],f.inventory.FirstOrDefault(v=>v.slot==i)?.item??-1);
                for(int i=0;i<offerIcons.Count;i++)PaintItem(offerIcons[i],RightItem(i));
                for(int i=0;i<ownedSelection.Count;i++)ownedSelection[i].enabled=ownedIcons[i].enabled&&shopSelection.Cell(false,i,rowOffset*3);
                for(int i=0;i<offerSelection.Count;i++)offerSelection[i].enabled=offerIcons[i].enabled&&shopSelection.Cell(true,i,rowOffset*3);
            }
            if(f.kind==FacilityKind.Mercenary)
            {
                server.Request_OpenMercenaryClient(f.token);var client=server.MercenaryClient(f.token);
                var offers=HireOffers();
                for(int i=0;i<offerIcons.Count;i++)
                {var offer=i+rowOffset*3<offers.Length?offers[i+rowOffset*3]:null;var image=offerIcons[i];image.texture=offer==null?null:MercenaryPicture(offer.picture);image.enabled=image.texture!=null;if(image.enabled)image.uvRect=NativeEntryLayout.Uv(image.texture,new Rect(32,32,32,32));}
                int job=client?.PreviewJob??view.record.hiredJob,count=client?.PreviewCount??view.record.hiredCount;
                var unit=definition.mercenaryCatalog?.units?.FirstOrDefault(row=>row.job==job);hiredIcon.texture=unit==null?null:MercenaryPicture(unit.picture);hiredIcon.enabled=hiredIcon.texture!=null;
                if(hiredIcon.enabled)hiredIcon.uvRect=NativeEntryLayout.Uv(hiredIcon.texture,new Rect(32,32,32,32));
                hireCount.text=NativeMercenaryLayout.Count(count);balance.text=view.record.goldKnown?NativeMercenaryLayout.Gold(view.record.gold):"—";AnchorPersonalLine(balance,NativeMercenaryLayout.GoldX,NativeMercenaryLayout.GoldY,2);
                int inspected=job>=0?job:selectedHire;details.text=NativeMercenaryLayout.Details(definition.mercenaryCatalog?.units?.FirstOrDefault(row=>row.job==inspected),offers.FirstOrDefault(row=>row.job==inspected)?.limit??0);
            }
        }
        private Texture2D MercenaryPicture(int picture)=>art.Picture(picture)??(campaign?.IsConfigured==true?campaign.Picture(picture):null);
        private WorldHireOffer[] HireOffers()=>definition.mercenaryCatalog?.Offers(view.record)??definition.hires.Where(h=>h.commander==CurrentCommanderJob).ToArray();
        private void SelectHire(int cell)
        {
            if(view?.facility?.kind!=FacilityKind.Mercenary||notice!=null||(server.MercenaryClient(view.facility.token)?.PreviewJob??view.record.hiredJob)>=0)return;
            var offers=HireOffers();int i=cell+rowOffset*3;if(i>=offers.Length)return;
            selectedHire=offers[i].job;
        }
        private int RightItem(int cell)
        {
            int i=cell+rowOffset*3;
            if(view.facility.kind==FacilityKind.Shop)return i<view.facility.offers.Length?view.facility.offers[i]:-1;
            if(shownWarehouse?.Token==view.facility.token)return shownWarehouse.ItemAt(i);
            return view.facility.storage.FirstOrDefault(s=>s.slot==i)?.item??-1;
        }
        private void PaintItem(RawImage image,int id)
        {
            var item=definition.Item(id);var icon=item==null?null:ItemIcon(item.picture);image.enabled=icon!=null;
            image.texture=icon;image.uvRect=new Rect(0,0,1,1);
        }
        private RawImage NativeSelectionBorder(RectTransform parent)
        {
            if(facilitySelectionTexture==null){facilitySelectionTexture=new Texture2D(33,33,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};facilitySelectionTexture.LoadRawTextureData(NativeShopSelection.BorderPixels());facilitySelectionTexture.Apply(false,true);}
            var outline=Picture(parent,"Original selection pen",new Rect(0,0,33,33),facilitySelectionTexture);outline.enabled=false;return outline;
        }
        private RawImage ItemCell(int slot,bool right)
        {
            string cellToken=panelToken;
            int columns=right?3:7;var origin=right?new Vector2(278,40):new Vector2(20,40);
            var hit=Hit(panel,(right?"Right":"Owned")+slot,new Rect(origin.x+slot%columns*35,origin.y+slot/columns*35,32,32),()=>{});
            hit.GetComponent<Image>().color=Color.black;
            var outline=NativeSelectionBorder((RectTransform)hit.transform);(right?offerSelection:ownedSelection).Add(outline);
            var image=Picture((RectTransform)hit.transform,"Native item",new Rect(0,0,32,32),null);image.enabled=false;
            hit.gameObject.AddComponent<NativeItemPress>().Configure(down=>{if(down&&panelToken==cellToken&&view?.facility?.token==cellToken)SelectFacilityItem(right,slot);});
            int dragged=-1,dragOffset=0;string owned=null;NativeItemDragContext dragContext=null;NativeWarehouseGesture warehouseDrag=null;
            hit.gameObject.AddComponent<NativeTutorialDrag>().Configure(screen,(point,end)=>
            {
                if(dragGhost==null)
                {
                    if(end)return;dragContext=null;warehouseDrag=null;dragged=-1;owned=null;
                    var native=server.WarehouseClient(panelToken);
                    if(native!=null)
                    {
                        if(shownWarehouse?.Matches(native)!=true)return;
                        owned=right?null:view.facility.inventory.FirstOrDefault(item=>item.slot==slot)?.id;
                        warehouseDrag=server.Request_BeginWarehouseDrag(panelToken,!right,owned,right?slot+rowOffset*3:-1);if(warehouseDrag==null)return;
                        if(right?shownWarehouse.ItemAt(slot+rowOffset*3)!=warehouseDrag.Item:view.facility.inventory.FirstOrDefault(item=>item.id==owned)?.remainingUses!=warehouseDrag.Uses)return;
                        dragged=warehouseDrag.Item;dragOffset=rowOffset;
                    }
                    else
                    {
                        dragContext=NativeItemDragContext.Facility(view,panelToken,slot,right,rowOffset);if(dragContext==null||!dragContext.Matches(server.WorldSnapshot,rowOffset))return;
                        dragged=dragContext.Item;owned=dragContext.Owned;
                    }
                    dragGhost=Picture(overlay,"Dragged item",new Rect(0,0,32,32),null);PaintItem(dragGhost,dragged);
                }
                Place((RectTransform)dragGhost.transform,new Rect(point.x-16,point.y-16,32,32));
                if(!end)return;
                Destroy(dragGhost.gameObject);dragGhost=null;
                var local=point-new Vector2(panel.anchoredPosition.x,-panel.anchoredPosition.y);
                bool valid=right?new Rect(20,40,242,137).Contains(local):new Rect(278,40,102,137).Contains(local);
                if(!valid||dragged<0)return;
                if(warehouseDrag!=null){var reply=server.Request_WarehouseItem(warehouseDrag,RequestId());if(reply.Failure==WorldFailure.InventoryFull)HandleNativeRejection(reply,NativeWarehousePackets.FullBagMessage);else Handle(reply);return;}
                string token=panelToken;
                if(view.facility.kind==FacilityKind.Shop)
                {
                    InspectItem(dragged);var reply=server.Request_ShopItem(dragContext,right,token,RequestId(),rowOffset);
                    if(reply.Failure==WorldFailure.InventoryFull)HandleNativeRejection(reply,NativeShopPackets.FullBagMessage);
                    else if(reply.Failure==WorldFailure.ShopCategoryMismatch)HandleNativeRejection(reply,NativeShopPackets.CategoryMessage);else Handle(reply);
                }
                else Handle(right?server.Request_RetrieveItem(owned,token,RequestId()):server.Request_StoreItem(owned,token,RequestId()));
            },()=>view?.facility!=null&&(right?RightItem(slot)>=0:view.facility.inventory.Any(i=>i.slot==slot))&&notice==null&&view.warehouseRequest==null&&view.shopQuoteRequest==null&&view.shopTradeRequest==null,
                ()=>{if(dragGhost!=null)Destroy(dragGhost.gameObject);dragGhost=null;dragContext=null;warehouseDrag=null;dragged=-1;owned=null;},
                ()=>dragGhost!=null&&panel!=null&&notice==null&&(warehouseDrag!=null?(!right||dragOffset==rowOffset)&&server.WarehouseDragCurrent(warehouseDrag):dragContext?.Matches(server.WorldSnapshot,rowOffset)==true));
            return image;
        }
        private void InspectItem(int id)
        {
            var item=definition.Item(id);if(item==null||details==null)return;
            if(shopDetails==null){var resource=Resources.Load<TextAsset>("OriginalShopDetails");if(resource!=null)shopDetails=JsonUtility.FromJson<NativeShopDetailsCatalog>(resource.text);}
            if(worldBitmapFont==null)worldBitmapFont=Resources.Load<NativeBitmapFont>("OriginalBitmapFont");
            int? kind=definition.growthCatalog?.Job(CurrentCommanderJob)?.kind;
            details.text=NativeShopDetails.Lines(NativeShopDetails.Paragraphs(item,NativeShopDetails.Usable(item,view.record.level,kind),shopDetails?.Properties(id)),worldBitmapFont?.Ready==true?worldBitmapFont.Mapping:null);
        }
        private void SelectFacilityItem(bool right,int slot)
        {
            if(view?.facility==null||view.facility.token!=panelToken||panel==null||notice!=null||clientMessageState.Visible||view.facility.kind!=FacilityKind.Shop&&view.facility.kind!=FacilityKind.Warehouse)return;
            int item=right?RightItem(slot):view.facility.inventory.FirstOrDefault(row=>row.slot==slot)?.item??-1;
            if(item<0){shopSelection.Clear();details.text="";return;}shopSelection.Select(right,right?rowOffset*3+slot:slot);InspectItem(item);
        }
        private void FacilityControl(int id)
        {
            if(view?.facility==null)return;var f=view.facility;string request=RequestId();
            if(f.kind==FacilityKind.ScenarioGateway&&id==9)
            {if(selectedRoom==null){ShowFailure(WorldFailure.InvalidInput);return;}Handle(server.Request_JoinScenarioRoom(selectedRoom,f.token,request));return;}
            if(id==40){rowOffset=Math.Max(0,rowOffset-1);return;}
            if(id==39)
            {
                int count=f.kind==FacilityKind.Shop?f.offers.Length:f.kind==FacilityKind.Mercenary?HireOffers().Length:shownWarehouse?.Count??(f.storage.Length==0?0:f.storage.Max(s=>s.slot)+1);
                int visible=f.kind==FacilityKind.Mercenary?9:12;if(rowOffset*3+visible<count)rowOffset++;return;
            }
            if(f.kind==FacilityKind.Bank)
            {
                if(id==6){Handle(server.Request_CloseFacility());return;}
                if(id==24||id==25)OpenBankAmount(id==24);return;
            }
            if(f.kind==FacilityKind.Warehouse&&id==19)
            {
                if(server.WarehouseClient(f.token)!=null){Handle(server.Request_CloseFacility());return;}
                if(server.WarehouseAuthority!=null&&(f.warehouseTerms==null||!f.storageFeeKnown))Handle(server.Request_WarehouseState(f.token,request));
                else Handle(server.Request_ConfirmStorage(f.token,request));
                return;
            }
            if(f.kind==FacilityKind.Temple&&id==19)
            {
                if(server.RevivalQuoteAuthority!=null&&f.revivalQuote==null)Handle(server.Request_RevivalQuote(f.token,request));
                else Handle(server.Request_Revive(f.token,request));
                return;
            }
            if(f.kind==FacilityKind.Mercenary)
            {
                if(id==8){Handle(server.Request_MercenaryItem(true,selectedHire,f.token,request));return;}
                if(id==7){Handle(server.Request_MercenaryItem(false,-1,f.token,request));return;}
            }
            if(id==19)Handle(server.Request_CloseFacility());
        }
        private RectTransform NoticeBlocker()
        {var root=Group(overlay,"Modal input",new Rect(0,0,640,480));var image=root.gameObject.AddComponent<Image>();image.color=Color.clear;return root;}
        private void CloseNotice()
        {CancelBankAmount();if(notice==null)return;SaveNativeFacilityFrame(notice);var root=notice.parent;root.gameObject.SetActive(false);Destroy(root.gameObject);notice=null;}
        private void ShowFailure(WorldFailure failure)
        {
            if(failure==WorldFailure.None)return;
            string message=failure==WorldFailure.TooFar?"직원 옆이나 가판대 앞에서 이용해 주세요.":failure==WorldFailure.RulesUnavailable?"처리에 필요한 정보가 확인되지 않았습니다.":failure==WorldFailure.InsufficientFunds?"돈이 부족합니다.":failure==WorldFailure.InsufficientResource?"MP 또는 행동력이 부족합니다.":failure==WorldFailure.StorageUnavailable?"저장하지 못했습니다. 다시 시도해 주세요.":failure==WorldFailure.InventoryFull?"물품을 놓을 공간이 없습니다.":failure==WorldFailure.Moving?"이동을 마친 뒤 이용해 주세요.":failure==WorldFailure.StaleRequest?"요청이 만료되었습니다. 다시 선택해 주세요.":"지금은 이 요청을 처리할 수 없습니다.";
            ShowClientMessage(message);
        }
        private void ShowQuit()
        {CloseNotice();var root=NoticeBlocker();notice=NativeWindow(27,root,id=>{CloseNotice();if(id!=19)return;DarkSaver.Original.UI.OriginalClientPreferences.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        },43);}
        private void ShowBattleEscape()
        {if(view!=null&&BattleMap()&&notice==null)NativeBattleOptions.Activate(0,ActiveBattleCommanders,BattleDialogRequest);}
    }
}
