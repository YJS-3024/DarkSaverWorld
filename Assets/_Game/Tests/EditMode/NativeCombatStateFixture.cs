using System.Reflection;
using DarkSaver.Combat;
using NUnit.Framework;
namespace DarkSaver.Tests
{
    // Explicit received-state test data. Never enables a production simulator,
    // manufactures time recovery, or establishes original HP/MP/AP defaults.
    internal static class NativeCombatStateFixture
    {
        internal static CombatUnit Unit(BattleSceneRoot battle,short id)
        {
            Assert.IsTrue(battle.TryGetUnit(id,out _));
            return ((CombatWorld)typeof(BattleSceneRoot).GetField("world",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle)).Get(id);
        }
        internal static void Initialize(BattleSceneRoot battle)
        {
            var actor=Unit(battle,100);actor.JobId=100;actor.Hp=actor.MaxHp=120;actor.Mp=actor.MaxMp=30;
            var enemy=Unit(battle,800);enemy.JobId=701;enemy.Hp=enemy.MaxHp=75;
            SetAp(battle,100,20,0);SetAp(battle,800,20,0);
        }
        internal static void SetAp(BattleSceneRoot battle,short id,int maximum,short ticks)
        {
            Assert.IsTrue(Unit(battle,id).ActionPoints.ConfigureMaximum(maximum));
            Assert.IsTrue(battle.ApplyTickMenu(id,ticks,0));
            Assert.IsTrue(battle.TryGetUnit(id,out var received));
            Assert.AreEqual(maximum,received.MaxAp);Assert.AreEqual(ticks,received.Ap);
        }
    }
}
