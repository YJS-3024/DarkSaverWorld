using System;
using DarkSaver.Network;
namespace DarkSaver.Tests
{
    public static class TrainingVisitChecks
    {
        public static void Run(Action<bool,string> check)
        {
            var config=new TrainingTravelSettings {version=1,mapId=101,doorX=8.5f,doorY=23.5f,returnX=8,returnY=21};
            var state=new TrainingTravelContext("Kaosia",8.5f,23.5f,true,false);
            var service=new TrainingTravelService(config,()=>state);
            config.mapId=107;config.returnX=42;
            var dispatcher=new LocalServerDispatcher();
            using(var binding=new TrainingProtocolBinding(dispatcher,service))
            {
                check(!service.Retreat(new Request_RetreatTraining(1)).Accepted,"cannot retreat before entry");
                check(!service.Ready(new Request_ReadyTraining(0,true)).Accepted,"cannot ready missing session");
                foreach(var bad in new[]{new TrainingTravelContext("Kaosia",8.5f,23.5f,false,false),new TrainingTravelContext("Kaosia",8.5f,23.5f,true,true),
                    new TrainingTravelContext("Elsewhere",8.5f,23.5f,true,false),new TrainingTravelContext("Kaosia",20,20,true,false),new TrainingTravelContext("Kaosia",float.NaN,23.5f,true,false)})
                {state=bad;check(!service.Enter(new Request_EnterTraining()).Accepted && service.SessionId==0,"unauthorized entry preserves inactive session");}
                state=new TrainingTravelContext("Kaosia",8.5f,23.5f,true,false);
                check(dispatcher.TryDispatch(new Request_EnterTraining(),out var raw) && raw is Response_EnterTraining && raw.Accepted,"entry flows through registered authority");
                var entered=(Response_EnterTraining)raw;int id=entered.SessionId;
                check(entered.MapId==101 && entered.X==10 && entered.Y==7,"server chooses central native grid cell from detached config");
                check(entered.X+.5f==10.5f && 15-1-entered.Y+.5f==7.5f,"central native cell maps to Unity cell center");
                check(!service.Enter(new Request_EnterTraining()).Accepted && service.SessionId==id,"duplicate entry preserves pending token");
                check(!service.State(new Request_GetTrainingState(id)).Accepted,"not active before successful loading");
                check(!service.Ready(new Request_ReadyTraining(id+1,true)).Accepted && service.SessionId==id,"stale load reply cannot finalize session");
                check(!service.Ready(new Request_ReadyTraining(id,false)).Accepted && service.SessionId==0,"failed entry loading rolls back without active visit");
                entered=service.Enter(new Request_EnterTraining());id=entered.SessionId;
                check(entered.Accepted && id>0,"entry can retry after failed loading");
                state=new TrainingTravelContext("Kaosia",8.5f,23.5f,false,true);
                check(!service.Ready(new Request_ReadyTraining(id,true)).Accepted && service.SessionId==0,"lost character context cannot complete entry");
                state=new TrainingTravelContext("Kaosia",8.5f,23.5f,true,false);
                entered=service.Enter(new Request_EnterTraining());id=entered.SessionId;
                check(service.Ready(new Request_ReadyTraining(id,true)).Accepted,"loading completion activates visit");
                check(!service.Ready(new Request_ReadyTraining(id,true)).Accepted && service.SessionId==id,"duplicate loading completion does not repeat entry");
                state=new TrainingTravelContext("Training101",10.5f,7.5f,true,false);
                var snapshot=service.State(new Request_GetTrainingState(id));
                check(snapshot.Accepted && snapshot.Units.Count==0,"visit never invents monsters or stat records");
                check(!service.Retreat(new Request_RetreatTraining(id+1)).Accepted,"stale retreat rejected");
                state=new TrainingTravelContext("Training101",10.5f,7.5f,true,true);
                check(!service.Retreat(new Request_RetreatTraining(id)).Accepted,"transition-blocked retreat rejected");
                state=new TrainingTravelContext("Training101",10.5f,7.5f,true,false);
                var exit=service.Retreat(new Request_RetreatTraining(id));
                check(exit.Accepted && exit.MapScene=="Kaosia_Map" && exit.RuntimeScene=="Town_Runtime" && exit.X==8 && exit.Y==21,"server supplies existing training entrance return cell");
                check(exit.X+.5f==8.5f && 43-1-exit.Y+.5f==21.5f,"return native cell maps to existing spawn center");
                check(!service.Retreat(new Request_RetreatTraining(id)).Accepted,"duplicate retreat blocked while destination loads");
                check(!service.Ready(new Request_ReadyTraining(id,false)).Accepted && service.SessionId==id,"failed town load restores active training session");
                check(service.State(new Request_GetTrainingState(id)).Accepted,"training state remains queryable after failed retreat");
                check(service.Retreat(new Request_RetreatTraining(id)).Accepted && service.Ready(new Request_ReadyTraining(id,true)).Accepted,"retreat retry commits only after town readiness");
                check(service.SessionId==0 && !service.State(new Request_GetTrainingState(id)).Accepted,"committed retreat closes training session");
                check(!service.Ready(new Request_ReadyTraining(id,true)).Accepted,"late completion cannot reopen ended session");
                state=new TrainingTravelContext("Kaosia",8.5f,23.5f,true,false);
                check(service.Enter(new Request_EnterTraining()).SessionId>id,"reentry receives a fresh token");
            }
            check(!dispatcher.HasHandler(new Request_RetreatTraining(1)),"visit authority ownership released");
        }
    }
}
