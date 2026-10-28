using DarkSaver.Combat;
using DarkSaver.Items;
using DarkSaver.Network;
using DarkSaver.Player;
using DarkSaver.World;
using UnityEngine;
namespace DarkSaver.UI
{
    // Original storage panel only. Unconfirmed deposit/withdraw/fee rules remain disabled.
    public sealed class NativeWarehousePanel:MonoBehaviour
    {
        private InGameUi ui;private TopDownPlayerController player;private NativeBattleWindows controls;
        public bool IsOpen{get;private set;}
        public static bool RequestOpen(ShopCounter2D npc,TopDownPlayerController player)
        {
            var session=FindAnyObjectByType<BattleTransitionController>();if(session==null)return false;
            var server=session.GetComponent<ServerManager>() ?? session.gameObject.AddComponent<ServerManager>();server.BindLocalWarehouseService();
            if(!server.TryRequest(new Request_OpenWarehouse(npc.GetEntityId().ToString()),out var response) || response is not Response_OpenWarehouse)return false;
            var panel=session.GetComponent<NativeWarehousePanel>() ?? session.gameObject.AddComponent<NativeWarehousePanel>();
            panel.player=player;panel.IsOpen=true;player.SetUiInputBlocked(true);return true;
        }
        public void Close(){if(!IsOpen)return;IsOpen=false;ui?.Hide();if(player!=null)player.SetUiInputBlocked(false);player=null;}
        private void OnDisable()=>Close();
        private void LateUpdate()
        {
            if(!IsOpen)return;
            if(player==null || !UnityEngine.SceneManagement.SceneManager.GetSceneByName("KaosiaWarehouse_Runtime").isLoaded){Close();return;}
            if(Input.GetKeyDown(KeyCode.Escape)){Close();return;}
            InGameUi.Ensure(ref ui,this,160);ui.Begin();var rect=new Rect(110,88,419,303);
            ui.NativeTexturePanel("warehouse",rect,Resources.Load<Texture2D>("UI/NativeWarehousePanel"));
            var session=GetComponent<BattleTransitionController>();var catalog=GetComponent<ItemCatalogPanel>()?.Catalog;
            for(int i=0;i<28;i++)
            {
                int id=0,count=0;bool owned=session!=null && session.TryGetOwnedItemAt(i,out id,out count);
                var item=owned && catalog!=null && catalog.TryGet(id,out var found)?found:null;
                ui.ItemSlotButton("warehouse-owned-"+i,new Rect(rect.x+20+i%7*35,rect.y+40+i/7*35,32,32),null,ItemIcons.Get(item),owned?count:0,false,item?.Name??"");
            }
            ui.NativeBitmapText("warehouse-gold",new Rect(rect.x+54,rect.y+266,60,20),session?.CurrentGold?.ToString()??"",Color.yellow,alignment:TextAnchor.MiddleRight);
            // No fictitious empty account balance, free fee or capacity is supplied.
            ui.NativeBitmapText("warehouse-pending",new Rect(rect.x+20,rect.y+191,375,60),"창고 보관 내역을 확인할 수 없습니다.",Color.black,3);
            controls??=Resources.Load<NativeBattleWindows>("UI/TownShop/NativeTownShop");
            var close=controls?.Find(10)?.controls;
            var confirm=close==null?null:System.Array.Find(close,c=>c.nativeId==19);
            if(confirm!=null)ui.NativeImageButton("warehouse-close",new Vector2(rect.x+345,rect.y+264),confirm,Close,true,"확인");
            ui.End();
        }
    }
}
