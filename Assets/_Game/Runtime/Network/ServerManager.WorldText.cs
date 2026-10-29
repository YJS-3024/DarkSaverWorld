using System;
namespace DarkSaver.Network
{
    public sealed partial class ServerManager
    {
        private IDisposable worldTextBinding,worldWhisperBinding;
        private WorldTextService worldTextService;
        private Func<WorldTextContext> worldTextOwner;
        public void BindLocalWorldTextService(Func<WorldTextContext> context)
        {
            if(worldTextBinding!=null)return;
            if(LocalServer.HasHandler(new Request_WorldText("")))return; // Preserve an explicitly installed authority.
            if(LocalServer.HasHandler(new Request_WorldWhisperSetting(true)))throw new InvalidOperationException("A whisper-setting handler already exists.");
            worldTextOwner=context;worldTextService=new WorldTextService(context);
            worldWhisperBinding=RegisterRequestHandler<Request_WorldWhisperSetting>(worldTextService.SetWhispers);
            worldTextBinding=RegisterRequestHandler<Request_WorldText>(request=>
            {
                var result=worldTextService.Handle(request);
                if(result.Accepted && result.Message!=null)SendToClient(result.Message);
                return result;
            });
        }
        public void ConfigureLocalWorldCommandHandler(Func<Request_WorldText,WorldTextContext,Response_WorldText> handler)
        {
            if(worldTextService==null)throw new InvalidOperationException("Bind the world text service first.");
            worldTextService.ConfigureCommandHandler(handler);
        }
        public void UnbindLocalWorldTextService(Func<WorldTextContext> owner){if(worldTextOwner==owner)ClearWorldTextBinding();}
        private void ClearWorldTextBinding(){worldTextBinding?.Dispose();worldWhisperBinding?.Dispose();worldTextBinding=worldWhisperBinding=null;worldTextService=null;worldTextOwner=null;}
    }
}
