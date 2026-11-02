using System;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    // Managed test host for real manager partial methods; no Unity lifetime/rendering is simulated.
    public sealed partial class LocalServerManager
    {
        private readonly EntryAccountService testService;
        public LocalServerManager(EntryAccountService service){testService=service;}
        private WorldReply WorldRequest(Func<EntryAccountService,WorldReply> apply)=>apply(testService);
    }
}
