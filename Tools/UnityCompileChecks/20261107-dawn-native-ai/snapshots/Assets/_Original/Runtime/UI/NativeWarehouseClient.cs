using System;
using System.Linq;
using DarkSaver.Original.World;
namespace DarkSaver.Original.UI
{
    public static class NativeShopStorageLayout
    {
        public const int WalletX=114,WalletY=266,FeeX=150,FeeY=266;
        public static string Fee(uint amount)=>"보관료 : "+amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
    public sealed class NativeWarehouseRow
    {
        public int item;public ushort token;
        public NativeWarehouseRow Copy()=>(NativeWarehouseRow)MemberwiseClone();
    }
    public sealed class NativeOwnedItemToken
    {
        public string owned;public int item;public ushort? token;
        public NativeOwnedItemToken Copy()=>(NativeOwnedItemToken)MemberwiseClone();
    }
    public sealed class NativeWarehouseOpenNotice
    {
        // Explicit transport ownership and GUID association. Neither the key nor
        // item token is derived from the local NPC, bag slot or owned-item GUID.
        public string id,character,session,token,npc;public int map;public ushort? key;public bool approved;
        public NativeOwnedItemToken[] inventory;
    }
    public sealed class NativeWarehouseContentsNotice
    {public string id,open,character,session,token;public bool approved;public byte[] packet;}
    public sealed class NativeWarehouseRequest
    {
        public string id,open,character,session,token,owned;public bool deposit;public int item;public ushort key,itemToken;public byte[] packet;
        public NativeWarehouseRequest Copy(){var result=(NativeWarehouseRequest)MemberwiseClone();result.packet=packet?.Clone()as byte[];return result;}
    }
    public sealed class NativeWarehouseGesture
    {
        public string Open{get;}public string Character{get;}public string Session{get;}public string Token{get;}public string Owned{get;}
        public long Revision{get;}public int Item{get;}public ushort ItemToken{get;}public int Row{get;}public bool Deposit{get;}public int? Uses{get;}
        internal NativeWarehouseGesture(NativeWarehouseWindow window,bool deposit,int row,WorldOwnedItem owned,NativeWarehouseRow item)
        {Open=window.Open;Character=window.Character;Session=window.Session;Token=window.Token;Revision=window.Revision;Deposit=deposit;Row=row;Owned=owned?.id;Uses=owned?.remainingUses;Item=item.item;ItemToken=item.token;}
    }
    // One presented frame. A refreshed list must not retarget a pointer that
    // still sees an older item icon, and drawing12 cells must not clone N rows12 times.
    public sealed class NativeWarehouseDisplay
    {
        public string Open{get;}public string Token{get;}public long Revision{get;}public bool Loaded{get;}public uint Fee{get;}public int Count=>items.Length;
        private readonly int[] items;
        internal NativeWarehouseDisplay(NativeWarehouseWindow window)
        {Open=window.Open;Token=window.Token;Revision=window.Revision;Loaded=window.Loaded;Fee=window.Fee;items=window.Rows.Select(row=>row.item).ToArray();}
        public int ItemAt(int index)=>index>=0&&index<items.Length?items[index]:-1;
        public bool Matches(NativeWarehouseWindow window)=>window!=null&&window.Open==Open&&window.Token==Token&&window.Revision==Revision;
    }
    public sealed class NativeWarehouseWindow
    {
        public string Open{get;}public string Character{get;}public string Session{get;}public string Token{get;}public string Npc{get;}public int Map{get;}public ushort Key{get;}
        public bool Loaded{get;private set;}public uint Fee{get;private set;}public long Revision{get;private set;}
        private readonly NativeOwnedItemToken[] inventory;private NativeWarehouseRow[] rows=Array.Empty<NativeWarehouseRow>();
        public NativeWarehouseRow[] Rows=>rows.Select(row=>row.Copy()).ToArray();public int Count=>rows.Length;
        public int ItemAt(int index)=>index>=0&&index<rows.Length?rows[index].item:-1;
        public NativeWarehouseDisplay Display()=>new NativeWarehouseDisplay(this);
        private NativeWarehouseWindow(NativeWarehouseOpenNotice notice)
        {Open=notice.id;Character=notice.character;Session=notice.session;Token=notice.token;Npc=notice.npc;Map=notice.map;Key=notice.key.Value;inventory=notice.inventory.Select(row=>row.Copy()).ToArray();}
        public static bool Available(WorldView view)=>view?.record!=null&&view.facility?.kind==FacilityKind.Warehouse&&!view.moving&&view.pendingPortal==null
            &&view.training==null&&view.battle==null&&(view.presentation==null||view.presentation.completed);
        public static NativeWarehouseWindow Begin(WorldView view,NativeWarehouseOpenNotice notice)
        {
            if(!Available(view)||notice?.approved!=true||!notice.key.HasValue||!Guid.TryParseExact(notice.id,"N",out _)||notice.character!=view.characterId||notice.session!=view.currencySession
                ||string.IsNullOrEmpty(notice.character)||string.IsNullOrEmpty(notice.session)||string.IsNullOrEmpty(notice.token)||notice.token!=view.facility.token||notice.npc!=view.facility.npc||notice.map!=view.record.map
                ||view.warehouseRequest!=null||view.facility.storageDraftRevision!=0
                ||notice.inventory==null||notice.inventory.Any(row=>row==null||!row.token.HasValue||!view.record.inventory.Any(owned=>owned.id==row.owned&&owned.item==row.item))
                ||notice.inventory.Select(row=>row.owned).Distinct().Count()!=notice.inventory.Length)return null;
            return new NativeWarehouseWindow(notice);
        }
        public bool Matches(WorldView view)=>Available(view)&&view.characterId==Character&&view.currencySession==Session&&view.facility.token==Token&&view.facility.npc==Npc&&view.record.map==Map;
        internal bool Receive(byte[] packet,Func<int,bool> knownItem)
        {
            if(Revision==long.MaxValue||!NativeWarehousePackets.TryContents(packet,knownItem,out var next,out uint fee))return false;
            rows=next;Fee=fee;Loaded=true;Revision++;return true;
        }
        public NativeWarehouseGesture BeginDrag(WorldView view,bool deposit,string owned,int row)
        {
            if(!Matches(view))return null;
            if(!deposit)return row<0||row>=rows.Length?null:new NativeWarehouseGesture(this,false,row,null,rows[row]);
            var instance=view.record.inventory.FirstOrDefault(item=>item.id==owned);var mapping=inventory.FirstOrDefault(item=>item.owned==owned&&item.item==instance?.item);
            return instance==null||mapping==null?null:new NativeWarehouseGesture(this,true,-1,instance,new NativeWarehouseRow{item=mapping.item,token=mapping.token.Value});
        }
        public bool Matches(NativeWarehouseGesture gesture,WorldView view)
        {
            if(gesture==null||gesture.Open!=Open||gesture.Revision!=Revision||gesture.Character!=Character||gesture.Session!=Session||gesture.Token!=Token)return false;
            var current=BeginDrag(view,gesture.Deposit,gesture.Owned,gesture.Row);
            return current!=null&&current.Item==gesture.Item&&current.ItemToken==gesture.ItemToken&&current.Uses==gesture.Uses;
        }
    }
    public static class NativeWarehousePackets
    {
        public const string FullBagMessage="가지고 있는게 너무 많아 찾을 수 없습니다.";
        public static byte[] Query(ushort key)=>new[]{(byte)0x97,(byte)key,(byte)(key>>8)};
        public static byte[] Move(bool deposit,ushort key,int item,ushort token)=>item<0||item>short.MaxValue?null:new[]{deposit?(byte)0x99:(byte)0x9a,(byte)key,(byte)(key>>8),(byte)item,(byte)(item>>8),(byte)token,(byte)(token>>8)};
        public static bool TryContents(byte[] packet,Func<int,bool> knownItem,out NativeWarehouseRow[] rows,out uint fee)
        {
            rows=null;fee=0;if(packet==null||packet.Length<7||packet[0]!=0x98||knownItem==null)return false;
            int count=(short)(packet[1]|packet[2]<<8);if(count<0||packet.Length!=7+count*4)return false;
            var accepted=new System.Collections.Generic.List<NativeWarehouseRow>();
            for(int i=0;i<count;i++)
            {
                int at=3+i*4,item=(short)(packet[at]|packet[at+1]<<8);ushort token=(ushort)(packet[at+2]|packet[at+3]<<8);
                if(knownItem(item))accepted.Add(new NativeWarehouseRow{item=item,token=token});
            }
            int end=3+count*4;for(int i=0;i<4;i++)fee|=(uint)packet[end+i]<<(i*8);rows=accepted.ToArray();return true;
        }
    }
}
