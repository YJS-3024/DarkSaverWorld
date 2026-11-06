using System;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    // Managed test host for real manager partial methods; no Unity lifetime/rendering is simulated.
    public sealed partial class LocalServerManager
    {
        private readonly EntryAccountService testService;
        public event Action<WorldReply> Notify_MapEntered,Notify_ScenarioExited;
        public LocalServerManager(EntryAccountService service){testService=service;}
        public WorldView WorldSnapshot=>testService.WorldSnapshot();
        private WorldReply WorldRequest(Func<EntryAccountService,WorldReply> apply)=>apply(testService);
    }
}
