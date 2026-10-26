using DarkSaver.Core;
using DarkSaver.Items;
using DarkSaver.Jobs;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        public int EquipmentItemAt(int slot)=>character?.EquipmentItemAt(slot) ?? -1;
        private JobCatalogEntry FindEquipmentJob(int id)
        {
            JobCatalogEntry result=null;
            foreach(var catalog in formationCatalogs)
                if(catalog!=null && catalog.TryGet(id,out var row)){if(result!=null)return null;result=row;}
            return result;
        }
        public bool TryChangeEquipment(int slot,int itemId,NativeCharacterProfile baseline,out string reason)
        {
            if(!processingSessionRequest)return SubmitSession(new DarkSaver.Network.LocalEquipmentChangeRequest(slot,itemId,baseline),out reason);
            reason="최신 장비와 현재 월드 상태를 확인하세요.";
            if(!isActiveAndEnabled || transitioning || activeBattle!=null || !CanEnterBattle || character==null || inventory==null
                || !DarkSaver.World.PersonalEquipmentContext.IsReady || DarkSaver.Events.Scenario01Controller.BlocksWorldActions || DarkSaver.UI.FirstPlayTutorial.BlocksWorldActions || SceneManager.GetSceneByName(BattleSceneName).isLoaded
                || FindAnyObjectByType<DarkSaver.World.WorldSceneCoordinator>()?.IsTravelling==true
                || GetComponent<DarkSaver.UI.BattleFormationPanel>()?.IsOpen==true || baseline==null
                || JsonUtility.ToJson(baseline)!=JsonUtility.ToJson(character))return false;
            var items=GetComponent<DarkSaver.UI.ItemCatalogPanel>()?.Catalog;var job=FindEquipmentJob(character.jobId);
            NativeCharacterProfile candidate;
            if(slot== -1 && itemId==0){candidate=character.Clone();candidate.equipmentItemIds=new int[7];}
            else
            {
                if(!character.TryBuildEquipmentSlot(slot,itemId,inventory,out candidate))return false;
                if(itemId>0 && (items==null || !items.TryGet(itemId,out var item)
                    || !SinglePlayerEquipmentRules.IsEligible(candidate,job,items,slot,item)))
                {reason="위치·병종·베이스 레벨 조건을 확인하세요.";return false;}
            }
            if(character.HasSameEquipment(candidate)){reason="이미 같은 장비 상태입니다.";return false;}
            if(!candidate.IsEquipmentOwned(inventory) || !SinglePlayerEquipmentRules.TryResolve(candidate,job,items,out var stats))
            {reason="장비 수치나 다른 장착 조건을 확인하세요.";return false;}
            character=candidate;
            if(hasPlayerVitals)
            {playerMaxHp=stats.Hp;playerMaxMp=stats.Mp;playerHp=System.Math.Min(playerHp,playerMaxHp);playerMp=System.Math.Min(playerMp,playerMaxMp);}
            reason=itemId>0?"장착했습니다. 창을 닫고 F5로 저장하세요.":"해제했습니다. 창을 닫고 F5로 저장하세요.";
            return true;
        }
        public string KnownEquipmentWeaponName()
        {
            if(character==null)return null;int first=character.EquipmentItemAt(2),second=character.EquipmentItemAt(3);
            if(first<0 && second<0)return null;
            var items=GetComponent<DarkSaver.UI.ItemCatalogPanel>()?.Catalog;
            string Name(int id)=>id>0 && items!=null && items.TryGet(id,out var item)?(item.Name??"?").Replace('_',' '):null;
            string a=Name(first),b=Name(second);
            if(a!=null && b!=null)return a+" / "+b;
            if(a!=null || b!=null)return (a??b)+((first<0 || second<0)?" / 일부 미확인":"");
            return first==0 && second==0?"비무장":null;
        }
    }
}
