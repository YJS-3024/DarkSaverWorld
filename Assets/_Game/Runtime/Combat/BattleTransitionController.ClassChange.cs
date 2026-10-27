using DarkSaver.Core;
using DarkSaver.Jobs;
using DarkSaver.Magic;
using DarkSaver.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Combat
{
    public sealed partial class BattleTransitionController
    {
        [SerializeField] private ClassArtworkCatalog classArtwork;
        public bool TryGetPlayerPhysicalStats(out int baseAttack,out int baseDefense,out int attack,out int defense)
        {
            baseAttack=baseDefense=attack=defense=0;
            if(!OriginalRestorationPolicy.AllowSinglePlayerRules)return false;
            if(character==null || !character.IsValid) return false;
            JobCatalogEntry found=null;
            foreach(var catalog in formationCatalogs ?? System.Array.Empty<JobCatalog>()) {
                if(catalog==null || !catalog.TryGet(character.jobId,out var row))continue;
                if(found!=null)return false;
                found=row;
            }
            if(found==null || found.CategoryLabel!="player_job")return false;
            long a=found.GetNumericValue(6),d=found.GetNumericValue(8);
            if(a<0 || a>int.MaxValue || d<0 || d>int.MaxValue
                || a+character.allocatedAttack>int.MaxValue || d+character.allocatedDefense>int.MaxValue)return false;
            baseAttack=(int)a; baseDefense=(int)d;
            attack=baseAttack+character.allocatedAttack; defense=baseDefense+character.allocatedDefense;
            if(character.HasEquippedItems)
            {
                if(!DarkSaver.Items.UnresolvedEquipmentServerRules.TryResolve(character,found,GetComponent<DarkSaver.UI.ItemCatalogPanel>()?.Catalog,out var stats))return false;
                attack=stats.Attack;defense=stats.Defense;
            }
            return true;
        }
        public bool IsStatAllocationCurrent(NativeCharacterProfile baseline)
        {
            // UI polling must not allocate validity HashSets every frame. Commit validates the profile.
            if(baseline==null || character==null) return false;
            return baseline.HasSameEquipment(character) && baseline.name==character.name && baseline.jobId==character.jobId
                && baseline.hasLevels==character.hasLevels && baseline.baseLevel==character.baseLevel && baseline.jobLevel==character.jobLevel
                && baseline.unspentClassChangeStatPoints==character.unspentClassChangeStatPoints
                && baseline.allocatedAttack==character.allocatedAttack && baseline.allocatedDefense==character.allocatedDefense
                && baseline.allocatedMagicAttack==character.allocatedMagicAttack && baseline.allocatedMagicDefense==character.allocatedMagicDefense;
        }

        public bool TryAllocateCharacterStats(NativeCharacterProfile baseline,int attack,int defense,int magicAttack,int magicDefense,out string reason,int[] battleMagic=null)
{reason=DarkSaver.Core.OriginalRestorationPolicy.PendingServerRule;return false;}        public bool ConfigureClassArtwork(ClassArtworkCatalog catalog)
        {
            if (catalog == null || transitioning || activeBattle != null) return false;
            classArtwork = catalog; return true;
        }

        public bool TryRestoreCharacterArtwork(TopDownPlayerController player)
        {
            if (character == null || player == null || player.GetComponent<DirectionalSpriteAnimator>() is not DirectionalSpriteAnimator animator) return false;
            if (classArtwork != null) return classArtwork.TryApply(character.jobId, animator);
            var sprite = player.GetComponent<SpriteRenderer>()?.sprite;
            return character.jobId == 100 && animator.HasCompleteDirections && sprite != null && sprite.name.StartsWith("Fighter100_");
        }

        private bool PrepareClassChange(ExperienceProgressionTable table, MagicCatalog magic, TopDownPlayerController player,
            out NativeCharacterProfile changed, out int maximumHp, out int maximumMp, out string reason)
        {
            changed = null; maximumHp = maximumMp = 0;
            reason = "마을의 생존 캐릭터와 확인된 레벨·전직 데이터가 필요합니다.";
            if (!isActiveAndEnabled || transitioning || activeBattle != null || !CanEnterBattle
                || SceneManager.GetSceneByName(BattleSceneName).isLoaded || !DarkSaver.World.TownSceneContext.IsReady
                || FindAnyObjectByType<DarkSaver.World.WorldSceneCoordinator>()?.IsTravelling == true
                || GetComponent<DarkSaver.UI.BattleFormationPanel>()?.IsOpen == true
                || player == null || !player.isActiveAndEnabled || character == null
                || !character.TryBuildEligibleClassChange(table, magic, out var candidate)) return false;
            if (classArtwork == null || !classArtwork.TryGet(candidate.jobId, out _)
                || player.GetComponent<DirectionalSpriteAnimator>() == null || player.GetComponent<SpriteRenderer>() == null)
            { reason = "다음 클래스의 확인된 외형이 연결되지 않았습니다."; return false; }
            JobCatalogEntry entry = null;
            foreach (var catalog in formationCatalogs)
                if (catalog != null && catalog.TryGet(candidate.jobId, out var row))
                { if (entry != null) { reason = "다음 클래스 능력치가 중복됩니다."; return false; } entry = row; }
            if (entry == null || entry.CategoryLabel != "player_job") return false;
            long hp = entry.GetNumericValue(2), mp = entry.GetNumericValue(4);
            if (hp <= 0 || hp > int.MaxValue || mp < 0 || mp > int.MaxValue
                || entry.GetNumericValue(6) < 0 || entry.GetNumericValue(6) > int.MaxValue
                || entry.GetNumericValue(8) < 0 || entry.GetNumericValue(8) > int.MaxValue) return false;
            if(candidate.HasEquippedItems)
            {
                if(!DarkSaver.Items.UnresolvedEquipmentServerRules.TryResolve(candidate,entry,GetComponent<DarkSaver.UI.ItemCatalogPanel>()?.Catalog,out var stats))
                {reason="다음 클래스에서 사용할 수 없는 장비를 해제하세요.";return false;}
                hp=stats.Hp;mp=stats.Mp;
            }
            changed = candidate; maximumHp = (int)hp; maximumMp = (int)mp; reason = string.Empty; return true;
        }

        public bool CanChangeClass(ExperienceProgressionTable table, MagicCatalog magic, TopDownPlayerController player, out string reason)
{reason=DarkSaver.Core.OriginalRestorationPolicy.PendingServerRule;return false;}
        public bool TryChangeClass(ExperienceProgressionTable table, MagicCatalog magic, TopDownPlayerController player, out string reason)
{reason=DarkSaver.Core.OriginalRestorationPolicy.PendingServerRule;return false;}    }
}
