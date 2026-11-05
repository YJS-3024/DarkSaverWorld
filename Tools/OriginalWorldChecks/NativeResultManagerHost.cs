using System;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    // The runner inserts the unchanged WorldRequest/context guard from production.
    // Only Unity lifetime and component fields are supplied here.
    public sealed partial class LocalServerManager
    {
        private readonly EntryAccountService service;
        public bool isActiveAndEnabled=true;
        public event Action<WorldReply> Notify_World,Notify_MapEntered;
        public LocalServerManager(EntryAccountService value){service=value;}
        public WorldView WorldSnapshot=>isActiveAndEnabled?service.WorldSnapshot():null;
    }
}
