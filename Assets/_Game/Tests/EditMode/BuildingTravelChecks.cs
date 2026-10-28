using System;
using System.Linq;
using DarkSaver.Network;
namespace DarkSaver.Tests
{
    public static class BuildingTravelChecks
    {
        public static void Run(Action<bool,string> check,BuildingTravelRoutes table)
        {
            check(table.IsValid,"building route resource valid");
            foreach(var r in table.routes)
            {
                var service=new BuildingTravelService(table);
                var context=new BuildingTravelContext(r.fromKey,r.doorX,r.doorY,true,false);
                var request=new MapEnterRequest(r.toKey,r.spawnId);
                check(!service.Enter(request,new BuildingTravelContext(r.fromKey,r.doorX,r.doorY,false,false)).Accepted,"unauthenticated door rejected "+r.toKey);
                check(!service.Enter(request,new BuildingTravelContext(r.fromKey,r.doorX,r.doorY,true,true)).Accepted,"blocked door rejected "+r.toKey);
                check(!service.Enter(request,new BuildingTravelContext(r.fromKey,r.doorX+2,r.doorY,true,false)).Accepted,"remote door rejected "+r.toKey);
                check(!service.Enter(request,new BuildingTravelContext(r.fromKey,float.NaN,r.doorY,true,false)).Accepted,"invalid position rejected");
                check(!service.Enter(new MapEnterRequest("WorldField1010",r.spawnId),context).Accepted,"field not reenabled");
                check(!service.Enter(new MapEnterRequest(r.toKey,"forged"),context).Accepted,"spawn forgery rejected");
                check(!service.Enter(request,new BuildingTravelContext("another-region",r.doorX,r.doorY,true,false)).Accepted,"source mismatch rejected");
                check(!service.HasPending,"rejections have no pending state");
                var started=service.Enter(request,context);
                check(started.Accepted && started.Stage==MapEnterStage.Started && started.TransferId>0,"door accepted "+r.fromKey+" to "+r.toKey);
                check(started.Map==r.map && started.Runtime==r.runtime && started.X==r.x && started.Y==r.y,"server retains canonical arrival");
                check(!service.Enter(request,context).Accepted,"duplicate in-flight entry rejected");
                check(!service.Move(new WorldMoveRequest(context.X+.05f,context.Y),context,.2f,(x,y)=>true).Accepted,"movement stopped during transfer");
                check(!service.Complete(new MapEnterCompleteRequest(started.TransferId+1,true)).Accepted && service.HasPending,"wrong completion cannot consume transfer");
                check(!service.Complete(new MapEnterCompleteRequest(started.TransferId,false)).Accepted && !service.HasPending,"load failure cancels transfer");
                var retry=service.Enter(request,context);
                check(retry.Accepted && retry.TransferId!=started.TransferId,"retry has fresh identity");
                check(!service.Complete(new MapEnterCompleteRequest(started.TransferId,true)).Accepted && service.HasPending,"stale completion cannot consume retry");
                var ready=service.Complete(new MapEnterCompleteRequest(retry.TransferId,true));
                check(ready.Accepted && ready.Stage==MapEnterStage.Ready && ready.TransferId==retry.TransferId && ready.Map==r.map && ready.X==r.x && ready.Y==r.y,"ready preserves server map and coordinates");
                check(!service.Complete(new MapEnterCompleteRequest(retry.TransferId,true)).Accepted && !service.HasPending,"completion replay rejected");
                var moved=service.Move(new WorldMoveRequest(context.X+.05f,context.Y),context,.2f,(x,y)=>true);
                check(moved.Accepted && moved.X==context.X+.05f,"scoped walking response");
                check(!service.Move(new WorldMoveRequest(context.X+2,context.Y),context,.2f,(x,y)=>true).Accepted,"walking cannot teleport");
                check(!service.Move(new WorldMoveRequest(context.X+.05f,context.Y),context,.2f,(x,y)=>false).Accepted,"blocked terrain rejects walking");
                check(!service.Move(new WorldMoveRequest(float.PositiveInfinity,context.Y),context,.2f,(x,y)=>true).Accepted,"nonfinite move rejected");
            }
            var copied=new BuildingTravelRoutes{version=1,routes=table.routes.Select(r=>r.Clone()).ToArray()};
            var owner=new BuildingTravelService(copied);var original=copied.routes[0].Clone();copied.routes[0].x+=1;
            var response=owner.Enter(new MapEnterRequest(original.toKey,original.spawnId),new BuildingTravelContext(original.fromKey,original.doorX,original.doorY,true,false));
            check(response.X==original.x,"client installer cannot mutate server route table");
            copied.routes[0].map="foreign";check(!copied.IsValid,"foreign map resource rejected");
            copied.routes[0]=copied.routes[1].Clone();check(!copied.IsValid,"duplicate route rejected");
        }
    }
}
