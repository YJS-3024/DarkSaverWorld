using System.Reflection;
using DarkSaver.Combat;
using NUnit.Framework;
namespace DarkSaver.Tests
{
    // Explicit received-state test data. Never enables a production simulator,
    // manufactures time recovery, or establishes original HP/MP/AP defaults.
    internal static class NativeCombatStateFixture
    {
        internal static CombatUnit Received(CombatUnit unit,int current=6,int maximum=20)
        {
            Assert.IsNotNull(unit);Assert.IsTrue(unit.ActionPoints.ConfigureMaximum(maximum));
            Assert.IsTrue(unit.ActionPoints.ApplyAbsolute(current));return unit;
        }
        internal static bool ReceiveFullTicks(BattleSceneRoot battle)
        {
            var world=(CombatWorld)typeof(BattleSceneRoot).GetField("world",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle);
            foreach(var unit in world.Units){if(!unit.Alive)continue;int max=unit.ActionPoints.HasConfirmedMaximum?unit.ActionPoints.Maximum:20;SetAp(battle,unit.Id,max,(short)max);}
            return true;
        }
        internal static CombatUnit Unit(BattleSceneRoot battle,short id)
        {
            Assert.IsTrue(battle.TryGetUnit(id,out _));
            return ((CombatWorld)typeof(BattleSceneRoot).GetField("world",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle)).Get(id);
        }
        internal static void Initialize(BattleSceneRoot battle,int ticks=0)
        {
            var actor=Unit(battle,100);actor.JobId=100;actor.Hp=actor.MaxHp=120;actor.Mp=actor.MaxMp=30;
            var enemy=Unit(battle,800);enemy.JobId=701;enemy.Hp=enemy.MaxHp=75;
            Received(actor,ticks);Received(enemy,ticks);
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
