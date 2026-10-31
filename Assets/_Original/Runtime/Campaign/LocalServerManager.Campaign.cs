using System;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        private NativeCampaignArt campaignArt;
        public event Action<WorldReply> Notify_FieldEntered;
        public event Action<WorldReply> Notify_ScenarioEntered;
        public event Action<CampaignCommand> Notify_ScenarioEffect;
        public void ConfigureCampaign(NativeCampaignArt value) { campaignArt = value; }
        public WorldReply Request_EnterMap(int map)
        {
            var reply = WorldRequest(s => s.EnterCampaignMap(map), true);
            if (reply.Accepted)
            { if (campaignArt?.Definition.Map(map)?.kind == "scenario") Notify_ScenarioEntered?.Invoke(reply); else Notify_FieldEntered?.Invoke(reply); }
            return reply;
        }
        public void PublishScenarioEffect(CampaignCommand command) { Notify_ScenarioEffect?.Invoke(command); }
        public WorldReply Request_ContinueScenario() => WorldRequest(s => s.ContinueCampaignPresentation());
        public WorldReply Request_ClearScenario() => WorldRequest(s => s.RequestCampaignClear());
        public WorldReply Request_CampaignUnitAction(int unit) => WorldRequest(s => s.CampaignUnitAction(unit));
        // Trusted local-server notification adapter, never called by map clicks.
        public WorldReply Receive_ScenarioClear(CampaignClearNotice notice) => WorldRequest(s => s.ReceiveCampaignClear(notice));
        private void FlushCampaignEffects()
        { if (service != null) foreach (var command in service.DrainCampaignEffects()) PublishScenarioEffect(command); }
    }
}
