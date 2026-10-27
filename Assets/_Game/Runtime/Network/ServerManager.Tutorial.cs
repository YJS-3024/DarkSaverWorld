using System;
using DarkSaver.Core;
namespace DarkSaver.Network
{
    public sealed partial class ServerManager
    {
        private TutorialServerBoundary tutorialService;
        private IDisposable tutorialBinding;
        public void BindLocalTutorialService(string savePath,NativeCharacterProfile profile)
        {
            if(tutorialService!=null)
            {
                if(!NativeAccountDirectory.SamePath(tutorialService.SavePath,savePath) || !tutorialService.Matches(profile))
                    throw new InvalidOperationException("Tutorial is already bound to another character.");
                return;
            }
            var service=new TutorialServerBoundary(savePath,profile,UnityEngine.Resources.Load<UnityEngine.TextAsset>("Tutorial/TUTORIAL")?.text);
            tutorialBinding=RegisterRequestHandler<TutorialProgressRequest>(service.Handle);tutorialService=service;
        }
        private void ClearTutorialBinding(){tutorialBinding?.Dispose();tutorialBinding=null;tutorialService=null;}
    }
}
