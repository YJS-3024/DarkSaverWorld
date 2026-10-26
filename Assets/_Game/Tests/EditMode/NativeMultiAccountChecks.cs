using System;
using System.IO;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Network;
namespace DarkSaver.Tests
{
    // Shared by real Unity JsonUtility tests and the managed production-rule host.
    public static class NativeMultiAccountChecks
    {
        public static void Run(Action<bool,string> check)
        {
            var directory=Path.Combine(Path.GetTempPath(),"ds-multi-account-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            try
            {
                string history=Path.Combine(directory,"login.json"),legacyAccount=Path.Combine(directory,"native-account.json");
                var slots=Enumerable.Range(0,10).Select(i=>Path.Combine(directory,"slot-"+i+".json")).ToArray();
                var oldSave=Save("Alpha",11,101);var store=new NativeSaveStore(slots[0]);
                check(store.TrySave(oldSave,out _),"legacy character fixture saved");var oldBytes=File.ReadAllBytes(slots[0]);
                var server=new LocalCharacterServer(history,slots);
                check(!server.List(new CharacterListRequest()).Accepted,"character listing requires authentication");
                check(server.Register(new AccountRegisterRequest("alpha_01","alpha-password","alpha-password")).Accepted,"first account registers");
                var oldAccountBytes=File.ReadAllBytes(legacyAccount);
                check(server.Register(new AccountRegisterRequest("beta_02","beta-password","beta-password")).Accepted,"second account registers on same installation");
                check(server.Register(new AccountRegisterRequest("gamma_03","gamma-password","gamma-password")).Accepted,"third account registers on same installation");
                check(!server.Register(new AccountRegisterRequest(" ALPHA_01 ","other-password","other-password")).Accepted,"case-normalized legacy duplicate is rejected");
                check(!server.Register(new AccountRegisterRequest("BETA_02","other-password","other-password")).Accepted,"case-normalized scoped duplicate is rejected");
                check(!server.Register(new AccountRegisterRequest("../other","other-password","other-password")).Accepted,"path traversal ID rejected");
                check(!server.Register(new AccountRegisterRequest("new_user","sample-password","different-password")).Accepted,"mismatched confirmation rejected");
                check(oldBytes.SequenceEqual(File.ReadAllBytes(slots[0])) && oldAccountBytes.SequenceEqual(File.ReadAllBytes(legacyAccount)),"registration preserves legacy account and character bytes");
                var alpha=(LoginResponse)server.Login(new LoginRequest("ALPHA_01","alpha-password"));
                check(alpha.Accepted && alpha.Authenticated && alpha.RequiresCharacterSelection,"legacy account logs into selection");
                check(alpha.CaptureSlotPaths().SequenceEqual(slots),"first account keeps historical slot paths");
                check(((CharacterListResponse)server.List(new CharacterListRequest())).CaptureProfile(0).name=="Alpha","legacy character belongs to original account");
                var beta=(LoginResponse)server.Login(new LoginRequest(" BETA_02 ","beta-password"));
                var betaSlots=beta.CaptureSlotPaths();
                check(beta.Accepted && betaSlots.Length==10 && betaSlots.Distinct().Count()==10 && !betaSlots.Intersect(slots).Any(),"second account has ten isolated slots");
                var leaked=beta.CaptureSlotPaths();leaked[0]=slots[0];check(beta.CaptureSlotPaths()[0]==betaSlots[0],"response slot paths are detached");
                var empty=(CharacterListResponse)server.List(new CharacterListRequest());
                check(Enumerable.Range(0,10).All(i=>!empty.IsOccupied(i) && empty.CaptureProfile(i)==null),"second account cannot see legacy characters");
                check(!server.Select(new CharacterSelectRequest(slots[0])).Accepted,"cross-account selection rejected");
                check(!server.Create(new CharacterCreateRequest("Intruder",100,slots[1])).Accepted && !File.Exists(slots[1]),"cross-account creation rejected before staging");
                check(!server.Select(new CharacterSelectRequest(Path.Combine(directory,"accounts","beta_02","..","gamma_03","native-save-v1.json"))).Accepted,"normalized sibling-account path rejected");
                check(new NativeSaveStore(betaSlots[0]).TrySave(Save("Beta",22,202),out _),"second account owns independent character state");
                check(server.Select(new CharacterSelectRequest(betaSlots[0])).Accepted,"second account selects its own character");
                check(NativeSaveStore.TakeResume(out var resumed,out var selected) && selected==betaSlots[0] && resumed.character.name=="Beta" && resumed.inventory.gold==202 && resumed.worldX==22,"second account resumes only its own state");
                check(!server.Login(new LoginRequest("alpha_01","beta-password")).Accepted,"another account password cannot authenticate original account");
                check(!server.List(new CharacterListRequest()).Accepted && !server.Select(new CharacterSelectRequest(betaSlots[0])).Accepted,"failed login revokes prior account access");
                check(!server.Login(new LoginRequest()).Accepted,"registered accounts cannot fall back to anonymous access");
                beta=(LoginResponse)new LocalCharacterServer(history,slots).Login(new LoginRequest("beta_02","beta-password"));
                check(beta.Accepted && !beta.RequiresCharacterSelection && beta.ResumePath==betaSlots[0],"second account history survives restart");
                alpha=(LoginResponse)server.Login(new LoginRequest("alpha_01","alpha-password"));
                check(alpha.Accepted && alpha.RequiresCharacterSelection && alpha.ResumePath==null,"second account selection cannot become original account history");
                check(server.Select(new CharacterSelectRequest(slots[0])).Accepted,"original account retains selection");NativeSaveStore.ClearResume();
                var alphaAgain=(LoginResponse)new LocalCharacterServer(history,slots).Login(new LoginRequest("alpha_01","alpha-password"));
                check(alphaAgain.Accepted && alphaAgain.ResumePath==slots[0],"original account history survives restart");
                var registry=new NativeAccountDirectory(legacyAccount);
                var gamma=(LoginResponse)new LocalCharacterServer(history,slots).Login(new LoginRequest("gamma_03","gamma-password"));
                check(gamma.Accepted && gamma.RequiresCharacterSelection && gamma.CaptureSlotPaths().Length==10 && !gamma.CaptureSlotPaths().Intersect(betaSlots).Any(),"third account has independent history and slots");
                var betaAccount=Path.Combine(registry.AccountFolder("beta_02"),"native-account.json");string credential=File.ReadAllText(betaAccount);
                check(!credential.Contains("beta-password"),"new account stores no plaintext password");
                var betaHistory=registry.HistoryPath("beta_02",history);
                check(NativeLoginHistory.TryRemember(betaHistory,slots[0],out _,"beta_02"),"foreign-history test fixture written");
                var poisoned=(LoginResponse)server.Login(new LoginRequest("beta_02","beta-password"));
                check(!poisoned.Accepted && poisoned.Authenticated && poisoned.ResumePath==null,"foreign resume path rejected after correct authentication");
                check(((CharacterListResponse)server.List(new CharacterListRequest())).CaptureProfile(0).name=="Beta","recovery selection still lists only authenticated account slots");
                check(NativeLoginHistory.TryRemember(betaHistory,betaSlots[0],out _,"beta_02"),"scoped history fixture restored");
                var brokenBytes=File.ReadAllBytes(betaAccount);File.WriteAllText(betaAccount,"corrupt");
                check(!server.Login(new LoginRequest("beta_02","beta-password")).Accepted,"corrupt scoped account rejects login");
                check(!server.Register(new AccountRegisterRequest("beta_02","replacement-password","replacement-password")).Accepted && File.ReadAllText(betaAccount)=="corrupt","corrupt scoped credential is never overwritten");
                File.WriteAllBytes(betaAccount,brokenBytes);
                check(server.Login(new LoginRequest("beta_02","beta-password")).Accepted,"restored credential works without altering character files");
                check(oldBytes.SequenceEqual(File.ReadAllBytes(slots[0])) && oldAccountBytes.SequenceEqual(File.ReadAllBytes(legacyAccount)),"switching/invalid paths/history/corrupt tests leave original files unchanged");
                check(server.Logout(new LogoutRequest()).Accepted && !server.List(new CharacterListRequest()).Accepted,"explicit account switch logs out previous account");
                var status=(LoginResponse)server.Status(new LoginStatusRequest());
                check(!status.Authenticated && status.ResumePath==null && status.CaptureSlotPaths()==null,"logged-out status does not expose account slot/resume paths");
                check(!Directory.EnumerateFiles(directory,"*.tmp-*",SearchOption.AllDirectories).Any(),"no registration/history temporary files remain");
            }
            finally{NativeSaveStore.ClearResume();NativeCharacterProfile.ClearPending();Directory.Delete(directory,true);}
        }
        private static NativeSaveData Save(string name,float x,long gold)=>new NativeSaveData
        {format=NativeSaveStore.Format,version=NativeSaveStore.Version,mapScene="Kaosia_Map",runtimeScene="Town_Runtime",worldX=x,worldY=21,clearedScenarios=Array.Empty<string>(),hasCharacter=true,
            character=new NativeCharacterProfile {name=name,jobId=100,tutorial=new NativeTutorialProgress {step=NativeTutorialProgress.Complete}},hasInventory=true,inventory=new NativeInventory {gold=gold}};
    }
}
