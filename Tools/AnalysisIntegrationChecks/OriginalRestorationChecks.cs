using System;
using System.IO;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Network;

// Exercises production policy before the archived single-player suites opt in.
internal static class OriginalRestorationChecks
{
    internal static void Run(Action<bool,string> check)
    {
        check(!OriginalRestorationPolicy.AllowSinglePlayerRules,"restoration: production has no single-player rules");
        check(OriginalRestorationPolicy.AllowsStaffEvidence("screenshot"),"restoration: observed staff retained");
        check(OriginalRestorationPolicy.AllowsStaffEvidence("user-confirmed"),"restoration: confirmed staff retained");
        check(!OriginalRestorationPolicy.AllowsStaffEvidence("user-authored"),"restoration: authored staff excluded");
        check(!OriginalRestorationPolicy.AllowsStaffEvidence(null),"restoration: missing staff evidence excluded");
        string directory=Path.Combine(Path.GetTempPath(),"ds-original-policy-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var paths=Enumerable.Range(0,10).Select(i=>Path.Combine(directory,"slot"+i+".json")).ToArray();
            var server=new CharacterServerBoundary(Path.Combine(directory,"history.json"),paths);
            check(!server.Login(new LoginRequest("","")).Accepted,"restoration: anonymous login does not bypass accounts");
            check(!server.Create(new CharacterCreateRequest("Original",100,paths[0])).Accepted,"restoration: create requires original initialization response");
            check(!Directory.EnumerateFiles(directory).Any(),"restoration: rejected character creation writes no files");
        }
        finally {Directory.Delete(directory,true);}

    }
}
