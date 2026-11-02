using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    // Unity component lifetime/transport boundary only; production RPC partials are compiled unchanged.
    public sealed partial class LocalServerManager
    {
        private readonly EntryAccountService service;
        public bool isActiveAndEnabled=true;
        public LocalServerManager(EntryAccountService value){service=value;}
        public WorldView WorldSnapshot=>isActiveAndEnabled?service.WorldSnapshot():null;
    }
}
