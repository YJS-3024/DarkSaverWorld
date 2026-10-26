using System;
using System.IO;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Network;
using NUnit.Framework;
namespace DarkSaver.Tests
{
    public sealed class NativeMultiAccountTests
    {
        [Test]public void MultipleAccountsKeepCredentialsSlotsHistoriesAndLegacyFilesIsolated()
            =>NativeMultiAccountChecks.Run((ok,message)=>Assert.That(ok,Is.True,message));
        [Test]public void CharacterCreationQueuesOnlyTheAuthenticatedAccountSlot()
        {
            string directory=Path.Combine(Path.GetTempPath(),"ds-multi-create-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            try
            {
                var slots=Enumerable.Range(0,10).Select(i=>Path.Combine(directory,"legacy"+i+".json")).ToArray();
                var server=new LocalCharacterServer(Path.Combine(directory,"login.json"),slots);
                Assert.IsTrue(server.Register(new AccountRegisterRequest("first_user","fixture-pass-1","fixture-pass-1")).Accepted);
                Assert.IsTrue(server.Register(new AccountRegisterRequest("second_user","fixture-pass-2","fixture-pass-2")).Accepted);
                var second=(LoginResponse)server.Login(new LoginRequest("second_user","fixture-pass-2"));var path=second.CaptureSlotPaths()[9];
                Assert.IsFalse(server.Create(new CharacterCreateRequest("Other",100,slots[0])).Accepted);
                Assert.IsTrue(server.Create(new CharacterCreateRequest("Beta",103,path)).Accepted);
                Assert.IsTrue(NativeCharacterProfile.TakePending(out var profile,out var queued));Assert.AreEqual(path,queued);Assert.AreEqual(103,profile.jobId);
                var registry=new NativeAccountDirectory(Path.Combine(directory,"native-account.json"));
                Assert.IsTrue(NativeLoginHistory.TryRead(registry.HistoryPath("second_user",Path.Combine(directory,"login.json")),out var remembered,out _,"second_user"));Assert.AreEqual(path,remembered);
                var first=(LoginResponse)server.Login(new LoginRequest("first_user","fixture-pass-1"));Assert.IsTrue(first.Accepted);Assert.IsTrue(first.RequiresCharacterSelection);
                Assert.IsFalse(NativeCharacterProfile.TakePending(out _,out _));Assert.IsFalse(File.Exists(slots[0]));Assert.IsFalse(File.Exists(path));
            }
            finally{NativeSaveStore.ClearResume();NativeCharacterProfile.ClearPending();Directory.Delete(directory,true);}
        }
    }
}
