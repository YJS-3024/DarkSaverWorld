using System;
using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Network;

namespace DarkSaver.Tests
{
    public static class LocalTrainingProtocolChecks
    {
        private sealed class Authority : ITrainingServerAuthority
        {
            public int Calls;
            public Response_EnterTraining Enter(Request_EnterTraining p) { Calls++; return new Response_EnterTraining(7,101,3,4); }
            public Response_ReadyTraining Ready(Request_ReadyTraining p) { Calls++; return new Response_ReadyTraining(p.SessionId,p.Loaded); }
            public Response_GetTrainingState State(Request_GetTrainingState p) { Calls++; return new Response_GetTrainingState(p.SessionId,101,1,Array.Empty<CombatUnitSnapshot>()); }
            public Response_RetreatTraining Retreat(Request_RetreatTraining p) { Calls++; return new Response_RetreatTraining(p.SessionId,"Fixture_Map","Fixture_Runtime",8,9); }
        }
        public static void Run(Action<bool,string> check)
        {
            void Rejects(Action action,string name)
            { bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}check(rejected,name); }
            var dispatcher=new LocalServerDispatcher();
            var requests=new ClientPacket[] {new Request_EnterTraining(),new Request_ReadyTraining(7,true),new Request_GetTrainingState(7),new Request_RetreatTraining(7)};
            using(var binding=new TrainingProtocolBinding(dispatcher))
            {
                foreach(var request in requests)
                {check(dispatcher.TryDispatch(request,out var response) && !response.Accepted && !string.IsNullOrWhiteSpace(response.Reason),"missing authority returns explicit rejection "+request.GetType().Name);}
                check(dispatcher.TryDispatch(requests[0],out var entry) && entry is Response_EnterTraining,"entry rejects with typed response");
                check(dispatcher.TryDispatch(requests[1],out var ready) && ready is Response_ReadyTraining,"ready rejects with typed response");
                check(dispatcher.TryDispatch(requests[2],out var state) && state is Response_GetTrainingState,"state rejects with typed response");
                check(dispatcher.TryDispatch(requests[3],out var exit) && exit is Response_RetreatTraining,"retreat rejects with typed response");
            }
            foreach(var request in requests)check(!dispatcher.HasHandler(request),"dispose releases training owner "+request.GetType().Name);
            var authority=new Authority();
            using(var binding=new TrainingProtocolBinding(dispatcher,authority))
            {
                foreach(var request in requests)check(dispatcher.TryDispatch(request,out var response) && response.Accepted,"configured authority handles "+request.GetType().Name);
                check(authority.Calls==4,"authority receives each valid request once");
                foreach(var request in new ClientPacket[]{new Request_ReadyTraining(0,true),new Request_GetTrainingState(-1),new Request_RetreatTraining(0)})
                    check(dispatcher.TryDispatch(request,out var response) && !response.Accepted,"invalid session never reaches authority "+request.GetType().Name);
                check(authority.Calls==4,"invalid sessions have no authority side effects");
                check(dispatcher.TryDispatch(new Request_ReadyTraining(7,false),out var failedLoad) && !failedLoad.Accepted,"failed load is not fabricated as ready");
            }
            var other=dispatcher.Register<Request_GetTrainingState>(_=>new Response_GetTrainingState("another owner"));
            bool collision=false;try{new TrainingProtocolBinding(dispatcher,authority);}catch(InvalidOperationException){collision=true;}
            check(collision,"duplicate authority rejected");
            check(!dispatcher.HasHandler(requests[0]) && !dispatcher.HasHandler(requests[1]) && !dispatcher.HasHandler(requests[3]),"failed bind rolls back partial handlers");
            check(dispatcher.TryDispatch(requests[2],out var retained) && retained.Reason=="another owner","failed bind preserves unrelated owner");other.Dispose();

            var world=new CombatWorld();var unit=new CombatUnit{Id=400,JobId=302,Name="explicit fixture",Enemy=true,X=2,Y=3,Hp=17,MaxHp=75,Mp=2,MaxMp=10};
            unit.ActionPoints.ApplyAbsolute(23);world.Add(unit);world.TryGetSnapshot(400,out var first);
            var source=new[]{first};var statePacket=new Response_GetTrainingState(7,101,2,source);
            var spawn=new Notify_TrainingUnits(7,3,TrainingUnitUpdateKind.Respawn,source);
            unit.Hp=1;world.TryGetSnapshot(400,out var changed);source[0]=changed;
            check(statePacket.Units[0].Hp==17 && spawn.Units[0].Hp==17,"packet records are detached from array and live unit");
            check(spawn.Units[0].Ap==23 && spawn.Units[0].MaxAp==0,"unknown AP maximum is not replaced by a faction cap");
            bool immutable=false;try{((IList<CombatUnitSnapshot>)spawn.Units).Clear();}catch(NotSupportedException){immutable=true;}
            check(immutable,"published unit collection is read-only");
            Rejects(()=>new Notify_TrainingUnits(7,4,TrainingUnitUpdateKind.Spawn,new[]{first,first}),"duplicate unit IDs rejected atomically");
            Rejects(()=>new Notify_TrainingUnits(7,4,TrainingUnitUpdateKind.Spawn,Array.Empty<CombatUnitSnapshot>()),"spawn cannot omit records");
            Rejects(()=>new Notify_TrainingUnits(7,4,(TrainingUnitUpdateKind)99,new[]{first}),"unknown spawn kind rejected");
            Rejects(()=>new Response_GetTrainingState(7,101,1,new CombatUnitSnapshot[1]),"uninitialized unit record rejected");
            Rejects(()=>new Response_GetTrainingState(7,108,1,Array.Empty<CombatUnitSnapshot>()),"unknown training map rejected");
            Rejects(()=>new Response_GetTrainingState(7,101,0,Array.Empty<CombatUnitSnapshot>()),"missing revision rejected");
            Rejects(()=>new Response_EnterTraining(0,101,1,1),"missing entry session rejected");
            Rejects(()=>new Response_EnterTraining(7,101,20,1),"out of map entry rejected");
            Rejects(()=>new Response_RetreatTraining(7,null,"Fixture_Runtime",1,1),"retreat requires explicit destination");
            check(new Notify_TrainingUnits(7,4,TrainingUnitUpdateKind.Initial,Array.Empty<CombatUnitSnapshot>()).Units.Count==0,"explicit empty initial roster is preserved");
            unit.LifeState=UnitLifeState.Corpse;world.TryGetSnapshot(400,out var corpse);
            check(new Notify_TrainingUnits(7,5,TrainingUnitUpdateKind.Initial,new[]{corpse}).Units[0].Hp==1,"corpse notification does not infer HP zero");
            check(new Request_EnterTraining().Direction==LocalPacketDirection.ClientToServer && spawn.Direction==LocalPacketDirection.ServerToClient,"request and push directions are explicit");
        }
    }
}
