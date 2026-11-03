using System;
using System.Globalization;
using System.Linq;
using DarkSaver.Original.World;
namespace DarkSaver.Original.UI
{
    public static class NativeMercenaryLayout
    {
        public const int OwnedX=155,OwnedY=39,CountX=189,CountY=48,CountWidth=22,CountHeight=16,TextX=19,TextY=148,TextWidth=129,TextHeight=86,GoldX=116,GoldY=243;
        private static string Number(int value)=>value.ToString(CultureInfo.InvariantCulture);
        public static string Count(int value)=>"X"+Number(value);
        //The source label is %lu (32bit), independently of wallet accounting.
        public static string Gold(ulong value)=>NativeCurrencyText.Wallet(value);
        public static string Details(NativeHireUnit unit,int maximum)=>unit==null?"":"이름: "+(unit.name??"")+"\n체력: "+Number(unit.hp)+"\n공격력: "+Number(unit.attack)+"\n방어력: "+Number(unit.defense)+"\n가격: "+Number(unchecked((int)unit.price))+"\n고용가능수: "+Number(maximum);
    }
    public sealed class NativeMercenaryRequest
    {
        public string id,character,session,token,npc;public int map,job,limit;public ushort commander;public bool hire;public byte[] packet;
        public NativeMercenaryRequest Copy(){var r=(NativeMercenaryRequest)MemberwiseClone();r.packet=packet?.Clone()as byte[];return r;}
    }
    public sealed class NativeMercenaryWindow
    {
        private readonly WorldRecord baseline;private readonly string character,session,npc,commanderIdentity;private readonly ushort commander;
        private readonly WorldHireOffer[] offers;
        public string Token{get;}public int PreviewJob{get;private set;}public int PreviewCount{get;private set;}
        private NativeMercenaryWindow(WorldView view,WorldHireOffer[] values,string token)
        {baseline=view.record.Copy();character=view.characterId;session=view.currencySession;npc=view.facility.npc;Token=token;commanderIdentity=view.commanders.rows[0].id;commander=(ushort)view.commanders.rows[0].nativeRecord.Value;PreviewJob=baseline.hiredJob;PreviewCount=baseline.hiredCount;offers=values.Select(Copy).ToArray();}
        private static WorldHireOffer Copy(WorldHireOffer row)=>new WorldHireOffer{commander=row.commander,job=row.job,limit=row.limit,price=row.price,picture=row.picture,name=row.name};
        public static bool Available(WorldView view,string token)=>view?.record!=null&&view.facility?.kind==FacilityKind.Mercenary&&view.facility.token==token&&!string.IsNullOrEmpty(token)&&!string.IsNullOrEmpty(view.facility.npc)&&!view.moving&&view.pendingPortal==null&&view.battle==null&&view.training==null&&(view.presentation==null||view.presentation.completed);
        public static NativeMercenaryWindow Begin(WorldView view,WorldHireOffer[] offers,string token)
        {
            var first=view?.commanders?.rows?.FirstOrDefault();
            if(!Available(view,token)||string.IsNullOrEmpty(view.characterId)||string.IsNullOrEmpty(view.currencySession)||string.IsNullOrEmpty(first?.id)||first.nativeRecord.HasValue!=true||first.nativeRecord<0||first.nativeRecord>=ushort.MaxValue||view.record.hiredJob< -1||view.record.hiredJob>short.MaxValue||view.record.hiredCount<0||view.record.hiredCount>short.MaxValue||offers==null||offers.Any(row=>row==null||row.job<0||row.job>short.MaxValue)||offers.Select(row=>row.job).Distinct().Count()!=offers.Length)return null;
            return new NativeMercenaryWindow(view,offers,token);
        }
        public bool SameOwner(WorldView view)
        {var first=view?.commanders?.rows?.FirstOrDefault();return Available(view,Token)&&view.characterId==character&&view.currencySession==session&&view.record.map==baseline.map&&view.facility.npc==npc&&first!=null&&first.id==commanderIdentity&&first.nativeRecord==commander;}
        public bool Matches(WorldView view)=>SameOwner(view)&&view.record.hiredJob==baseline.hiredJob&&view.record.hiredCount==baseline.hiredCount
            &&(view.record.mercenary?.revision??0)==(baseline.mercenary?.revision??0)&&(view.record.growth?.revision??0)==(baseline.growth?.revision??0)
            &&(view.record.growth?.skills??Array.Empty<int>()).SequenceEqual(baseline.growth?.skills??Array.Empty<int>())
            &&Enumerable.Range(0,7).All(slot=>view.record.Equipment.FirstOrDefault(item=>item.slot==slot)?.item==baseline.Equipment.FirstOrDefault(item=>item.slot==slot)?.item);
        public NativeMercenaryRequest Act(bool hire,int selected,string request)
        {
            int job=PreviewJob,limit=0;
            if(hire)
            {
                if(job==-1){var offer=offers.FirstOrDefault(o=>o.job==selected);if(offer==null)return null;job=offer.job;limit=offer.limit;}
                else{limit=offers.FirstOrDefault(o=>o.job==job)?.limit??0;if(limit<=PreviewCount)return null;PreviewCount=unchecked((short)(PreviewCount+1));}
            }
            else
            {
                if(job==-1)return null;if(PreviewCount!=0)PreviewCount=unchecked((short)(PreviewCount-1));if(PreviewCount==0)PreviewJob=-1;
            }
            return new NativeMercenaryRequest{id=request,character=character,session=session,token=Token,npc=npc,map=baseline.map,commander=commander,job=job,limit=limit,hire=hire,packet=NativeMercenaryPackets.Action(hire,commander,job,limit)};
        }
    }
    public static class NativeMercenaryPackets
    {
        public static byte[] Action(bool hire,ushort commander,int job,int limit)=>hire
            ?new[]{(byte)0x4d,(byte)commander,(byte)(commander>>8),(byte)job,(byte)(job>>8),(byte)limit,(byte)(limit>>8)}
            :new[]{(byte)0x4e,(byte)commander,(byte)(commander>>8),(byte)job,(byte)(job>>8)};
    }
}
