using System;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        // Dispatcher/component boundary only; production identity/Memos partials compile unchanged.
        private WorldReply WorldRequest(Func<EntryAccountService,WorldReply> apply)=>isActiveAndEnabled&&service!=null?apply(service):new WorldReply(WorldFailure.Unauthenticated);
    }
}
