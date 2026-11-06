using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public event System.Action<WorldReply> Notify_GrowthChanged;
        public WorldReply Request_CharacterGrowth()=>WorldRequest(s=>s.GetCharacterGrowth());
        public WorldReply Notify_CharacterExperience(CharacterExperienceNotice notice)=>GrowthResult(s=>s.ReceiveCharacterExperience(notice));
        public WorldReply Notify_CharacterGrowth(CharacterGrowthNotice notice)=>GrowthResult(s=>s.ReceiveCharacterGrowth(notice));
        private WorldReply GrowthResult(System.Func<EntryAccountService,WorldReply> apply)
        {
            var result=WorldRequest(apply);if(IsCurrentGrowthReply(result))Notify_GrowthChanged?.Invoke(result);return result;
        }
        private bool IsCurrentGrowthReply(WorldReply result)
        {
            var current=WorldSnapshot;var expected=result?.View;
            return result?.Accepted==true&&current!=null&&expected!=null&&current.characterId==expected.characterId
                &&current.commanderSession==expected.commanderSession&&current.campaignSession==expected.campaignSession&&current.growthSession==expected.growthSession
                &&current.record.map==expected.record.map&&current.record.growth?.revision==expected.record.growth?.revision&&current.battle?.revision==expected.battle?.revision
                &&CommanderAdjustmentTarget.Same(current.adjustmentTarget,expected.adjustmentTarget)&&current.record.roster?.revision==expected.record.roster?.revision
                &&current.adjustmentProfile?.growth?.revision==expected.adjustmentProfile?.growth?.revision;
        }
    }
}
