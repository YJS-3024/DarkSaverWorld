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
        {var result=WorldRequest(apply);if(result.Accepted)Notify_GrowthChanged?.Invoke(result);return result;}
    }
}
