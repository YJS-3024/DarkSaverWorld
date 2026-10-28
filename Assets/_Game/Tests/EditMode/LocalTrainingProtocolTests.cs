using DarkSaver.Network;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class LocalTrainingProtocolTests
    {
        [Test] public void ContractsAndHandlerOwnership() => LocalTrainingProtocolChecks.Run((ok,name)=>Assert.IsTrue(ok,name));
        [Test] public void ManagerDispatchesTypedRejectionsAndUnbindsOnlyTrainingHandlers()
        {
            var go=new GameObject("local training manager fixture");
            try
            {
                var server=go.AddComponent<ServerManager>();server.BindLocalTrainingService();server.BindLocalTrainingService();
                ServerPacket received=null;server.ResponseReceived+=p=>received=p;
                Assert.IsFalse(server.TryRequest(new Request_EnterTraining(),out var response));
                Assert.IsInstanceOf<Response_EnterTraining>(response);Assert.AreSame(response,received);
                Assert.AreEqual(0,server.OriginalFrameCount); // No invented original training opcode.
                using(var other=server.RegisterRequestHandler<UnknownClientPacket>(_=>new LocalCommandResponse(true)))
                {
                    server.UnbindLocalTrainingService();
                    Assert.IsFalse(server.LocalServer.HasHandler(new Request_EnterTraining()));
                    Assert.IsTrue(server.TryRequest(new UnknownClientPacket(),out _));
                    server.BindLocalTrainingService();
                    var notice=new Notify_TrainingUnits(7,1,TrainingUnitUpdateKind.Initial,System.Array.Empty<DarkSaver.Combat.CombatUnitSnapshot>());
                    Assert.IsTrue(server.SendToClient(notice));Assert.AreSame(notice,received);
                }
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
