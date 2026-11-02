using System;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Campaign
{
    // Managed host supplies asset binding only. The production manager partial is compiled unchanged.
    public sealed class NativeCampaignArt { public CampaignDefinition Definition; }
}
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        private readonly EntryAccountService service;
        public bool isActiveAndEnabled=true;
        public LocalServerManager(EntryAccountService value){service=value;}
        public WorldView WorldSnapshot=>service.WorldSnapshot();
        private WorldReply WorldRequest(Func<EntryAccountService,WorldReply> apply,bool entered=false)=>apply(service);
        public WorldReply Notify_BattleUnits(TrainingUnitsNotice notice)=>service.ReceiveBattleUnits(notice);
        public void TestAdvance(double ms){service.AdvanceWorld(ms);PumpScenarioFlow();FlushCampaignEffects();}
    }
}
