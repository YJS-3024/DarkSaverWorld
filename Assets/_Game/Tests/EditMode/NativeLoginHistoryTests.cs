using System;
using System.IO;
using NUnit.Framework;
using DarkSaver.Core;

namespace DarkSaver.Tests
{
    public sealed class NativeLoginHistoryTests
    {
        [Test]
        public void OtherAccountsAndLegacyRecordsRequireExplicitCharacterSelection()
        {
            string directory=Path.Combine(Path.GetTempPath(),"ds-login-scope-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string history=Path.Combine(directory,"login.json"),slot=Path.Combine(directory,"slot.json");
                Assert.IsTrue(NativeLoginHistory.TryRemember(history,slot,out var reason,"first_account"),reason);
                Assert.IsTrue(NativeLoginHistory.TryRead(history,out var selected,out reason,"first_account"),reason);
                Assert.AreEqual(slot,selected);
                Assert.IsFalse(NativeLoginHistory.TryRead(history,out _,out _,"second_account"));
                Assert.IsFalse(NativeLoginHistory.TryRead(history,out _,out _));
                Assert.IsTrue(NativeLoginHistory.TryResolve(history,new[]{slot},out var resume,out var selection,out reason,"second_account"),reason);
                Assert.IsTrue(selection);Assert.IsNull(resume);
                File.WriteAllText(history,"{\"version\":1,\"savePath\":\""+slot.Replace("\\","\\\\")+"\"}");
                Assert.IsTrue(NativeLoginHistory.TryResolve(history,new[]{slot},out resume,out selection,out reason,"first_account"),reason);
                Assert.IsTrue(selection);Assert.IsNull(resume);
            }
            finally { Directory.Delete(directory,true); }
        }

        [Test]
        public void CreateReplaceAndFailedReplacementPreserveLastSelection()
        {
            string directory = Path.Combine(Path.GetTempPath(), "ds-login-unity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                string history = Path.Combine(directory, "login.json");
                string first = Path.Combine(directory, "slot-1.json");
                string second = Path.Combine(directory, "slot-2.json");
                Assert.IsTrue(NativeLoginHistory.TryRemember(history, first, out var reason), reason);
                Assert.IsTrue(NativeLoginHistory.TryRead(history, out var selected, out reason), reason);
                Assert.AreEqual(first, selected);
                Assert.IsTrue(NativeLoginHistory.TryRemember(history, second, out reason), reason);
                Assert.IsTrue(NativeLoginHistory.TryRead(history, out selected, out reason), reason);
                Assert.AreEqual(second, selected);
                byte[] previous = File.ReadAllBytes(history);
                using (var locked = new FileStream(history, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Assert.IsFalse(NativeLoginHistory.TryRemember(history, first, out reason));
                    Assert.IsNotEmpty(reason);
                }
                CollectionAssert.AreEqual(previous, File.ReadAllBytes(history));
                Assert.IsTrue(NativeLoginHistory.TryRead(history, out selected, out reason), reason);
                Assert.AreEqual(second, selected);
                Assert.IsEmpty(Directory.GetFiles(directory, "*.tmp-*"));
                Assert.IsTrue(NativeLoginHistory.TryRemember(history, first, out reason), reason);
                Assert.IsTrue(NativeLoginHistory.TryRead(history, out selected, out reason), reason);
                Assert.AreEqual(first, selected);
            }
            finally { Directory.Delete(directory, true); }
        }
    }
}
