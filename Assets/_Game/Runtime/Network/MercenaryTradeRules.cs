using System;
using DarkSaver.Core;
namespace DarkSaver.Network
{
    public static class MercenaryTradeRules
    {
        // Local server IDs only. Original units are allocated by the old server.
        public static bool TryHire(ushort owner,int jobId,int maximum,MercenaryOffer[] offers,NativeOwnedRoster roster,NativeInventory inventory,out NativeOwnedRoster changed,out NativeInventory money,out string reason)
        {
            changed=null;money=null;reason="고용 조건을 확인하세요.";
            if(owner!=100 || inventory?.IsValid!=true || roster!=null && !roster.IsValid)return false;
            var offer=offers==null?null:Array.Find(offers,o=>o.JobId==jobId);
            if(offer==null || offer.Maximum!=maximum || maximum<1 || maximum>short.MaxValue || offer.Price<0 || inventory.gold<offer.Price || offer.Hp<1 || offer.Mp< -1 || offer.Attack<0 || offer.Defense<0)return false;
            var list=new System.Collections.Generic.List<NativeOwnedUnitData>(roster?.Clone().units??Array.Empty<NativeOwnedUnitData>());int count=0;
            foreach(var u in list)if(u.hireOwnerId==owner){if(u.jobId!=jobId){reason="기존 용병을 모두 해고한 뒤 병종을 바꿀 수 있습니다.";return false;}count++;}
            if(count>=maximum){reason="고용 가능 인원이 가득 찼습니다.";return false;}
            int id=20000;while(id<=short.MaxValue && list.Exists(u=>u.instanceId==id))id++;if(id>short.MaxValue)return false;
            // Spell loadouts for magic soldiers are not invented from an MP value.
            if(offer.Mp!=-1){reason="이 병종의 원본 초기 마법 정보가 아직 없습니다.";return false;}
            list.Add(new NativeOwnedUnitData{persistentId=Guid.NewGuid().ToString("N"),instanceId=id,jobId=jobId,name=offer.Name,hp=offer.Hp,maxHp=offer.Hp,mp=0,maxMp=0,attack=offer.Attack,defense=offer.Defense,lifeState=0,teamId=0,recordEligible=true,finalStateConfirmed=true,learnedMagicIds=Array.Empty<short>(),usableSkillIds=Array.Empty<short>(),hireType=offer.Type,hireOwnerId=owner});
            changed=new NativeOwnedRoster{units=list.ToArray()};money=inventory.Clone();money.gold-=offer.Price;
            if(!changed.IsValid || !money.IsValid){changed=null;money=null;return false;}reason="용병을 고용했습니다.";return true;
        }
        public static bool TryDismiss(ushort owner,int jobId,NativeOwnedRoster roster,NativeInventory inventory,out NativeOwnedRoster changed,out string reason)
        {
            changed=null;reason="해고할 용병이 없습니다.";
            if(owner!=100 || roster?.IsValid!=true || inventory?.IsValid!=true)return false;
            var list=new System.Collections.Generic.List<NativeOwnedUnitData>(roster.Clone().units);int index=list.FindLastIndex(u=>u.hireOwnerId==owner && u.jobId==jobId);
            if(index<0)return false;list.RemoveAt(index);changed=new NativeOwnedRoster{units=list.ToArray()};reason="용병을 해고했습니다.";return changed.IsValid;
        }
    }
}
