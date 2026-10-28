using NUnit.Framework;
namespace DarkSaver.Tests
{
    public sealed class TrainingResumeTests
    {
        [Test] public void TrainingResumeLocationAndStorageContracts()=>TrainingResumeChecks.Run((ok,name)=>Assert.IsTrue(ok,name));
    }
}
