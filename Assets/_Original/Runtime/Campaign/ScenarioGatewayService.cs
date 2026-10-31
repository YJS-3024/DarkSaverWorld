using System;
using System.Linq;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply JoinScenarioRoom(string room,string token,string request)
        {
            var f=Facility(FacilityKind.ScenarioGateway,token,request);if(f!=WorldFailure.None)return WorldResult(f);
            if(room!=world.Token)return WorldResult(WorldFailure.StaleRequest);
            var npc=world.Definition.Map(world.Record.map).npcs.First(n=>n.id==world.Npc);
            var gate=world.Definition.gateways?.gates.FirstOrDefault(g=>g.id==npc.gateway&&g.enabled);
            if(world.Definition.gateways?.applyToGame!=true||gate==null)return WorldResult(WorldFailure.RulesUnavailable);
            var next=world.Record.Copy();next.map=gate.targetMap;next.x=gate.spawnX;next.y=gate.spawnY;next.campaignPhase="intro";
            f=CommitWorld(next);if(f==WorldFailure.None)world.Relocate(next);return WorldResult(f);
        }
    }
}
