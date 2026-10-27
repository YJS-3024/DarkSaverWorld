using System;
using System.IO;
using System.Linq;
using DarkSaver.Network;
namespace DarkSaver.Tests
{
    // Shared production-service checks. Temp storage only; no existing account/save is used.
    public static class LocalAuthenticationChecks
    {
        public static void Run(Action<bool,string> check)
        {
            string directory=Path.Combine(Path.GetTempPath(),"ds-auth-service-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var slots=Enumerable.Range(0,10).Select(i=>Path.Combine(directory,"slot"+i+".json")).ToArray();
                File.WriteAllText(slots[0],"preserved character fixture");byte[] character=File.ReadAllBytes(slots[0]);
                string history=Path.Combine(directory,"history.json");
                var service=new CharacterServerBoundary(history,slots);var dispatcher=new LocalServerDispatcher();
                using(var register=dispatcher.Register<AccountRegisterRequest>(service.Register))
                using(var login=dispatcher.Register<LoginRequest>(service.Login))
                using(var status=dispatcher.Register<LoginStatusRequest>(service.Status))
                using(var logout=dispatcher.Register<LogoutRequest>(service.Logout))
                {
                    LocalCommandResponse Request(ClientPacket packet)
                    {dispatcher.Record(packet);check(dispatcher.TryDispatch(packet,out var result),"auth request dispatched");return result;}
                    check(!Request(new LoginRequest("","")).Accepted,"anonymous login rejected");
                    check(!Request(new AccountRegisterRequest("alpha_01","auth-password-a","different-confirmation")).Accepted,"password confirmation mismatch rejected");
                    check(!Request(new AccountRegisterRequest("../escape","auth-password-a","auth-password-a")).Accepted,"invalid ID rejected before path selection");
                    check(!File.Exists(Path.Combine(directory,"native-account.json")),"failed signup writes no account");
                    var created=Request(new AccountRegisterRequest(" Alpha_01 ","auth-password-a","auth-password-a")) as AccountRegisterResponse;
                    check(created!=null && created.Accepted && created.UserId=="alpha_01","registration returns normalized ID");
                    var account=Path.Combine(directory,"native-account.json");byte[] first=File.ReadAllBytes(account);
                    check(!File.ReadAllText(account).Contains("auth-password-a"),"account stores no plaintext password");
                    check(!Request(new AccountRegisterRequest("ALPHA_01","replacement-password","replacement-password")).Accepted,"duplicate registration cannot replace credentials");
                    check(first.SequenceEqual(File.ReadAllBytes(account)),"duplicate signup preserves existing account bytes");
                    check(Request(new AccountRegisterRequest("beta_02","auth-password-b","auth-password-b")).Accepted,"second account registration succeeds");
                    check(first.SequenceEqual(File.ReadAllBytes(account)),"second account preserves first account bytes");
                    var accepted=Request(new LoginRequest("ALPHA_01","auth-password-a")) as LoginResponse;
                    check(accepted!=null && accepted.Accepted && accepted.Authenticated && accepted.AccountId=="alpha_01","correct login authenticates normalized account");
                    check(accepted.RequiresCharacterSelection && accepted.ResumePath==null,"login does not auto-resume or enter world");
                    var own=accepted.CaptureSlotPaths();own[0]="mutated-response-path";
                    check(accepted.CaptureSlotPaths()[0]==slots[0],"login response slot paths are defensive copies");
                    check(!Request(new LoginRequest("alpha_01","wrong-password")).Accepted,"wrong password rejected");
                    var failed=Request(new LoginStatusRequest()) as LoginResponse;
                    check(failed!=null && !failed.Authenticated && failed.AccountId==null && failed.CaptureSlotPaths()==null,"failed login revokes previous identity and paths");
                    check(!Request(new LoginRequest("missing_03","auth-password-a")).Accepted,"unknown account rejected");
                    var beta=Request(new LoginRequest("beta_02","auth-password-b")) as LoginResponse;
                    check(beta!=null && beta.Authenticated && beta.AccountId=="beta_02","account switch authenticates second account");
                    check(beta.CaptureSlotPaths().Length==10 && beta.CaptureSlotPaths().All(p=>p.StartsWith(Path.Combine(directory,"accounts","beta_02"))),"second account paths remain isolated");
                    check(Request(new LogoutRequest()).Accepted,"logout packet accepted");
                    var signedOut=Request(new LoginStatusRequest()) as LoginResponse;
                    check(!signedOut.Authenticated && signedOut.AccountId==null,"logout clears identity");
                    var restarted=new CharacterServerBoundary(history,slots);
                    check(((LoginResponse)restarted.Login(new LoginRequest("alpha_01","auth-password-a"))).Authenticated,"credentials persist across service restart");
                    check(!restarted.Create(new CharacterCreateRequest("test",100,slots[1])).Accepted,"authentication never enables character initialization");
                    check(!File.Exists(history) && !File.Exists(slots[1]),"authentication creates no login-history or character save");
                    check(character.SequenceEqual(File.ReadAllBytes(slots[0])),"existing character fixture remains byte-identical");
                    check(dispatcher.CaptureTrace().All(t=>t.PacketType=="LoginRequest" || t.PacketType=="LogoutRequest" || t.PacketType=="LoginStatusRequest" || t.PacketType=="AccountRegisterRequest"),"packet trace retains message type only");
                }
                check(!dispatcher.HasHandler(new LoginRequest()),"handler disposal releases ownership");
                var blocked=Path.Combine(directory,"blocked");File.WriteAllText(blocked,"preserve file");
                var unavailable=new CharacterServerBoundary(Path.Combine(blocked,"history.json"),slots);
                var refused=unavailable.Register(new AccountRegisterRequest("gamma_03","auth-password-c","auth-password-c"));
                check(!refused.Accepted && !string.IsNullOrEmpty(refused.Reason),"storage write failure returns rejection");
                check(File.ReadAllText(blocked)=="preserve file","failed storage write preserves blocker");
            }
            finally {Directory.Delete(directory,true);}
        }
    }
}
