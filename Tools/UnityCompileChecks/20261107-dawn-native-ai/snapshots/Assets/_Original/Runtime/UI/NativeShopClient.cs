using System;
using System.Linq;
using DarkSaver.Original.World;
namespace DarkSaver.Original.UI
{
    public sealed class NativeShopInventoryNotice
    {
        // Local association with received native inventory and the resolved shop
        // client predicate. No inventory token, category or sale price is inferred.
        public string id,character,session,token,npc;public int map;public bool approved;public int? nativeBase;
        public int[] allowedItems;public NativeOwnedItemToken[] inventory;
    }
    public sealed class NativeShopRequest
    {
        public string id,character,session,token,npc,owned;public int map,item;public bool buy;public ushort? itemToken;public byte[] packet;
        public NativeShopRequest Copy(){var result=(NativeShopRequest)MemberwiseClone();result.packet=packet?.Clone()as byte[];return result;}
    }
    internal sealed class NativeShopInventory
    {
        private readonly string character,session,token,npc;private readonly int map;private readonly int? nativeBase;private readonly int[] allowed;private readonly NativeOwnedItemToken[] inventory;
        public string Token=>token;
        public int NativeWindowId=>NativeFacilityWindows.ShopId(nativeBase);
        private NativeShopInventory(NativeShopInventoryNotice notice)
        {character=notice.character;session=notice.session;token=notice.token;npc=notice.npc;map=notice.map;nativeBase=notice.nativeBase;allowed=notice.allowedItems?.ToArray();inventory=notice.inventory.Select(row=>row.Copy()).ToArray();}
        public static bool Available(WorldView view,string token)=>view?.record!=null&&view.facility?.kind==FacilityKind.Shop&&view.facility.token==token&&!string.IsNullOrEmpty(token)
            &&!view.moving&&view.pendingPortal==null&&view.battle==null&&view.training==null&&(view.presentation==null||view.presentation.completed);
        public static NativeShopInventory Begin(WorldView view,NativeShopInventoryNotice notice)
        {
            if(notice?.approved!=true||!Available(view,notice.token)||!Guid.TryParseExact(notice.id,"N",out _)||notice.character!=view.characterId||notice.session!=view.currencySession||notice.map!=view.record.map||notice.npc!=view.facility.npc
                ||notice.nativeBase.HasValue==(notice.allowedItems!=null)||notice.nativeBase.HasValue&&!new[]{0,100,200,300}.Contains(notice.nativeBase.Value)
                ||notice.allowedItems?.Any(id=>id<0||id>short.MaxValue)==true||notice.inventory==null||notice.inventory.Any(row=>row==null||!row.token.HasValue||!view.record.inventory.Any(item=>item.id==row.owned&&item.item==row.item))
                ||notice.inventory.Select(row=>row.owned).Distinct().Count()!=notice.inventory.Length)return null;
            return new NativeShopInventory(notice);
        }
        public bool Matches(WorldView view)=>Available(view,token)&&view.characterId==character&&view.currencySession==session&&view.record.map==map&&view.facility.npc==npc;
        public bool Allows(int item)=>NativeShopPackets.Allows(item,nativeBase,allowed);
        public bool TryToken(WorldView view,string owned,int item,out ushort value)
        {
            value=0;if(!Matches(view)||!NativeShopPackets.Allows(item,nativeBase,allowed))return false;
            var row=inventory.FirstOrDefault(entry=>entry.owned==owned&&entry.item==item);if(row==null)return false;value=row.token.Value;return true;
        }
    }
    public static class NativeShopPackets
    {
        public const string FullBagMessage="가지고 있는게 너무 많아 살 수 없습니다.",CategoryMessage="여기서 팔 수 있는 물건이 아닙니다.";
        public static bool Allows(int item,int? nativeBase,int[] resolvedItems)=>nativeBase.HasValue?nativeBase.Value<=item&&item<(long)nativeBase.Value+100:resolvedItems?.Contains(item)==true;
        public static byte[] Buy(int item)=>item<0||item>short.MaxValue?null:new[]{(byte)0x4b,(byte)item,(byte)(item>>8)};
        public static byte[] Sell(int item,ushort token)=>item<0||item>short.MaxValue?null:new[]{(byte)0x4c,(byte)item,(byte)(item>>8),(byte)token,(byte)(token>>8)};
    }
}
