using System;
using System.Linq;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply ContinueCampaignPresentation()
        { var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f); if (world.Presentation == null) return WorldResult(WorldFailure.InvalidStage); world.Presentation.Continue(); return WorldResult(WorldFailure.None); }
        public CampaignCommand[] DrainCampaignEffects()
        { if (world == null) return Array.Empty<CampaignCommand>(); var result = world.Effects.ToArray(); world.Effects.Clear(); return result; }
        public WorldReply RequestCampaignClear()
        { var f = ReadyWorld(); return WorldResult(f == WorldFailure.None ? WorldFailure.RulesUnavailable : f); }
        public WorldReply ReceiveCampaignClear(CampaignClearNotice notice)
        {
            var f=ReadyWorld();if(f!=WorldFailure.None)return WorldResult(f);
            var map=world.Definition.campaign?.Map(world.Record.map);
            if(notice==null||!notice.approved||notice.map!=world.Record.map||notice.session!=world.CampaignSession||!Guid.TryParseExact(notice.id,"N",out _)||world.CampaignNotices.Contains(notice.id))return WorldResult(WorldFailure.StaleRequest);
            if(map?.kind!="scenario"||world.Moving||world.PresentationLocked||world.Record.campaignPhase=="outro"||world.Record.campaignPhase=="complete")return WorldResult(WorldFailure.InvalidStage);
            var next=world.Record.Copy();next.campaignPhase="outro";f=CommitWorld(next);
            if(f==WorldFailure.None){world.Record=next;world.CampaignNotices.Add(notice.id);world.SetPresentation(map.outro);}
            return WorldResult(f);
        }
        public WorldReply CampaignUnitAction(int unit)
        {
            var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f);
            if (world.PresentationLocked) return WorldResult(WorldFailure.Busy);
            return WorldResult(world.Definition.campaign?.Map(world.Record.map)?.units.Any(u => u.unit == unit) == true ? WorldFailure.RulesUnavailable : WorldFailure.InvalidInput);
        }
        public WorldReply EnterCampaignMap(int target)
        {
            var f = ReadyWorld(); if (f != WorldFailure.None) return WorldResult(f);
            var map = world.Definition.campaign?.Map(target); if (map == null) return WorldResult(WorldFailure.InvalidInput);
            if (world.Moving || world.Kind != FacilityKind.None || world.PresentationLocked) return WorldResult(WorldFailure.Busy);
            var link = world.Definition.campaign.connections.FirstOrDefault(c => c.fromMap == world.Record.map && c.toMap == target
                && !string.IsNullOrWhiteSpace(c.evidence) && c.cells.Any(p => p.x == world.Record.x && p.y == world.Record.y));
            if (link == null) return WorldResult(WorldFailure.RulesUnavailable);
            var next = world.Record.Copy(); next.map = target; next.x = link.x; next.y = link.y; next.campaignPhase = map.kind == "scenario" ? "intro" : null;
            if (!world.Definition.Map(target).map.Contains(new DarkSaver.Original.Tutorial.TutorialTile(next.x, next.y))) return WorldResult(WorldFailure.InvalidInput);
            f = CommitWorld(next); if (f == WorldFailure.None) world.Relocate(next); return WorldResult(f);
        }
    }
}
