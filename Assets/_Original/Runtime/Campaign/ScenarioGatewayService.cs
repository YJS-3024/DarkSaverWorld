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
            var npc=world.Map(world.Record.map).npcs.First(n=>n.id==world.Npc);
            var gate=world.Definition.gateways?.gates.FirstOrDefault(g=>g.id==npc.gateway&&g.enabled);
            if(world.Definition.gateways?.applyToGame!=true||gate==null)return WorldResult(WorldFailure.RulesUnavailable);
            var arrival=gate.Portal("scenario-gateway-"+gate.id).ArrivalAt(gate.x,gate.y);
            var next=world.Record.Copy();next.map=gate.targetMap;next.x=arrival.x;next.y=arrival.y;next.campaignPhase="intro";
            f=PrepareScenarioVisit(next);if(f!=WorldFailure.None)return WorldResult(f);
            f=CommitWorld(next);if(f==WorldFailure.None)world.Relocate(next);return WorldResult(f);
        }
    }
}
