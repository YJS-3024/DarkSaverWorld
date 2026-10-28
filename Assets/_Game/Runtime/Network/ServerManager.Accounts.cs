using DarkSaver.Core;
using System;
using System.Collections.Generic;
namespace DarkSaver.Network
{
    public sealed partial class ServerManager
    {
        private CharacterServerBoundary accountService;
        private readonly List<IDisposable> accountBindings=new();
        public bool HasLocalAccountService=>accountService!=null;
        // One service per manager. UI controllers submit packets; they do not
        // authenticate credentials or write account files themselves.
        private static NativeSaveData PrepareTrainingResumeForLogin(NativeSaveData source)
        {
            if(!NativeTrainingResume.IsTraining(source))return source;
            var asset=UnityEngine.Resources.Load<UnityEngine.TextAsset>("World/TrainingTravel");
            var settings=asset==null?null:UnityEngine.JsonUtility.FromJson<TrainingTravelSettings>(asset.text);
            return settings!=null && settings.IsValid && NativeTrainingResume.TryRedirect(source,settings.returnX+.5f,43-1-settings.returnY+.5f,out var redirected)
                ? redirected:null;
        }
        public CharacterServerBoundary BindLocalAccountService(string loginHistoryPath=null,string[] slotPaths=null)
        {
            if(accountService!=null)
            {
                if(!accountService.MatchesConfiguration(loginHistoryPath,slotPaths))
                    throw new InvalidOperationException("Account service is already bound to a different storage location.");
                return accountService;
            }
            if(LocalServer.HasHandler(new LoginRequest()) || LocalServer.HasHandler(new LogoutRequest())
                || LocalServer.HasHandler(new LoginStatusRequest())
                || LocalServer.HasHandler(new AccountRegisterRequest(null,null,null))
                || LocalServer.HasHandler(new ServerListRequest()) || LocalServer.HasHandler(new ServerSelectRequest(0))
                || LocalServer.HasHandler(new CharacterListRequest()) || LocalServer.HasHandler(new CharacterCreateRequest())
                || LocalServer.HasHandler(new CharacterSelectRequest(null)))
                throw new InvalidOperationException("Account request handlers already have an owner.");
            var service=new CharacterServerBoundary(loginHistoryPath,slotPaths,PrepareTrainingResumeForLogin);
            try
            {
                accountBindings.Add(RegisterRequestHandler<LoginRequest>(service.Login));
                accountBindings.Add(RegisterRequestHandler<LogoutRequest>(service.Logout));
                accountBindings.Add(RegisterRequestHandler<AccountRegisterRequest>(service.Register));
                accountBindings.Add(RegisterRequestHandler<LoginStatusRequest>(service.Status));
                accountBindings.Add(RegisterRequestHandler<ServerListRequest>(service.ListServers));
                accountBindings.Add(RegisterRequestHandler<ServerSelectRequest>(service.SelectServer));
                accountBindings.Add(RegisterRequestHandler<CharacterListRequest>(service.List));
                accountBindings.Add(RegisterRequestHandler<CharacterCreateRequest>(service.Create));
                accountBindings.Add(RegisterRequestHandler<CharacterSelectRequest>(service.Select));
                accountService=service;return service;
            }
            catch {ClearAccountBindings();throw;}
        }
        private void ClearAccountBindings()
        {foreach(var binding in accountBindings)binding.Dispose();accountBindings.Clear();accountService=null;}
    }
}
