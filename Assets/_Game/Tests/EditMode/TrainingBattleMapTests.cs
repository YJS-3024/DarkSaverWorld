using DarkSaver.Combat;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class TrainingBattleMapTests
    {
        [TestCase("Map_101_;",true)]
        [TestCase("Map_102_;",true)]
        [TestCase("Map_103_;",true)]
        [TestCase("Map_104_;",true)]
        [TestCase("Map_105_;",true)]
        [TestCase("Map_106_;",true)]
        [TestCase("Map_107_;",true)]
        [TestCase("Map_100_;",false)]
        [TestCase("Map_108_;",false)]
        [TestCase("Map_101",false)]
        [TestCase("Brugos01_Map",true)]
        [TestCase("Kaosia_Map",true)]
        [TestCase("World1010_Map",true)]
        [TestCase(null,false)]
        public void OnlyKnownMapsAreAccepted(string name,bool expected)
        {
            Assert.AreEqual(expected,BattleTransitionController.IsSupportedBattleMapScene(name));
        }
    }
}
