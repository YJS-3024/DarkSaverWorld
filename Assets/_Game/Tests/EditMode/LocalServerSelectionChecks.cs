using System;
using System.IO;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Network;
namespace DarkSaver.Tests
{
    public static class LocalServerSelectionChecks
    {
        public static void Run(Action<bool,string> check)
        {
            string directory=Path.Combine(Path.GetTempPath(),"ds-lobby-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            try
            {
                var paths=Enumerable.Range(0,10).Select(i=>Path.Combine(directory,"slot"+i+".json")).ToArray();
                var save=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.Version,
                    mapScene="Kaosia_Map",runtimeScene="Town_Runtime",clearedScenarios=Array.Empty<string>(),
                    hasCharacter=true,character=new NativeCharacterProfile {name="Original",jobId=100}};
                check(new NativeSaveStore(paths[0]).TrySave(save,out _),"existing character fixture prepared");
                byte[] original=File.ReadAllBytes(paths[0]);File.WriteAllText(paths[1],"corrupt fixture");
                var service=new CharacterServerBoundary(Path.Combine(directory,"history.json"),paths);
                check(!service.ListServers(new ServerListRequest()).Accepted,"unauthenticated server list rejected");
                check(!service.SelectServer(new ServerSelectRequest(1)).Accepted,"unauthenticated server selection rejected");
                check(!service.List(new CharacterListRequest()).Accepted,"unauthenticated character list rejected");
                check(service.Register(new AccountRegisterRequest("alpha_01","lobby-password-a","lobby-password-a")).Accepted,"lobby account registration succeeds");
                check(service.Register(new AccountRegisterRequest("beta_02","lobby-password-b","lobby-password-b")).Accepted,"second lobby account registration succeeds");
                check(service.Login(new LoginRequest("alpha_01","lobby-password-a")).Accepted,"lobby login succeeds");
                check(!service.List(new CharacterListRequest()).Accepted,"server selection required before character listing");
                var response=(ServerListResponse)service.ListServers(new ServerListRequest());var servers=response.CaptureServers();
                check(response.Accepted && response.AccountId=="alpha_01" && servers.Length==1,"server list has exactly one authenticated entry");
                check(servers[0].Id==1 && servers[0].Name=="로컬 서버" && servers[0].Available,"local server metadata supplied");
                servers[0]=null;check(response.CaptureServers()[0]!=null,"server response array is a defensive copy");
                check(!service.SelectServer(new ServerSelectRequest(999)).Accepted && service.SelectedServerId==0,"invalid server cannot advance selection");
                var selected=(ServerSelectResponse)service.SelectServer(new ServerSelectRequest(1));
                check(selected.Accepted && selected.ServerId==1 && selected.AccountId=="alpha_01","local server selection response accepted");
                var list=(CharacterListResponse)service.List(new CharacterListRequest());
                check(list.Accepted && list.SlotCount==10 && list.ServerId==1 && list.AccountId=="alpha_01","next stage returns ten account-scoped character slots");
                check(list.IsOccupied(0) && list.CaptureProfile(0).name=="Original","existing character appears in selected account");
                var copy=list.CaptureProfile(0);copy.name="modified";
                check(list.CaptureProfile(0).name=="Original","character response profile is a defensive copy");
                check(list.IsOccupied(1) && list.CaptureProfile(1)==null,"corrupt occupied slot is not presented as empty");
                check(!list.IsOccupied(2) && list.CaptureProfile(2)==null,"empty slot remains empty without initialization");
                check(!service.SelectServer(new ServerSelectRequest(-1)).Accepted && service.SelectedServerId==1,"rejected selection preserves accepted state");
                check(service.SelectServer(new ServerSelectRequest(1)).Accepted,"same local selection is idempotent");
                check(service.Login(new LoginRequest("beta_02","lobby-password-b")).Accepted && service.SelectedServerId==0,"new login resets server selection");
                check(!service.List(new CharacterListRequest()).Accepted,"new account cannot inherit previous server phase");
                check(service.SelectServer(new ServerSelectRequest(1)).Accepted,"second account chooses local server");
                var beta=(CharacterListResponse)service.List(new CharacterListRequest());
                check(beta.AccountId=="beta_02" && Enumerable.Range(0,10).All(i=>!beta.IsOccupied(i) && beta.CaptureProfile(i)==null),"second account sees no first-account characters");
                check(!service.Login(new LoginRequest("beta_02","wrong-password")).Accepted && service.SelectedServerId==0,"failed login clears selected server");
                check(!service.ListServers(new ServerListRequest()).Accepted && !service.List(new CharacterListRequest()).Accepted,"failed login revokes lobby and character access");
                service.Login(new LoginRequest("alpha_01","lobby-password-a"));service.SelectServer(new ServerSelectRequest(1));service.Logout(new LogoutRequest());
                check(service.SelectedServerId==0 && !service.SelectServer(new ServerSelectRequest(1)).Accepted,"logout clears lobby access");
                check(original.SequenceEqual(File.ReadAllBytes(paths[0])) && File.ReadAllText(paths[1])=="corrupt fixture","lobby and character listing never rewrite existing saves");
                check(!File.Exists(paths[2]) && !File.Exists(Path.Combine(directory,"history.json")),"selection creates no character or auto-resume history");
            }
            finally {Directory.Delete(directory,true);}
        }
    }
}
