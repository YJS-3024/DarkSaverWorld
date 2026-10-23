using DarkSaver.Combat;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class CombatActionPointsTests
    {
        [Test]
        public void StartsAtSixAndRequiresMaximumEvenWhenCostIsAffordable()
        {
            var ap = new CombatActionPoints();
            Assert.That(ap.Current, Is.EqualTo(6)); Assert.That(ap.Maximum, Is.EqualTo(GameConstants.BattleAllyMaximumAp));
            Assert.That(ap.CanStart(2), Is.False); Assert.That(ap.TrySpend(2), Is.False);
            ap.Advance(3f); Assert.That(ap.Current, Is.EqualTo(9)); Assert.That(ap.CanStart(2), Is.False);
            ap.Advance(1f); Assert.That(ap.CanStart(2), Is.True);
            Assert.That(ap.TrySpend(2), Is.True); Assert.That(ap.Current, Is.EqualTo(8));
            Assert.That(ap.TrySpend(2), Is.False);
        }

        [Test]
        public void FractionalFramesAndLargeTimeStepsRecoverWithoutOverflow()
        {
            var ap = new CombatActionPoints();
            ap.Advance(.25f); ap.Advance(.25f); ap.Advance(.25f);
            Assert.That(ap.Current, Is.EqualTo(6));
            ap.Advance(.25f); Assert.That(ap.Current, Is.EqualTo(7));
            Assert.That(ap.Advance(float.MaxValue), Is.True); Assert.That(ap.Current, Is.EqualTo(GameConstants.BattleAllyMaximumAp));
        }

        [Test]
        public void TimeAtMaximumCannotBeBankedForNextAction()
        {
            var ap = new CombatActionPoints(); ap.Advance(100.5f);
            Assert.That(ap.TrySpend(6), Is.True);
            ap.Advance(.5f); Assert.That(ap.Current, Is.EqualTo(4));
            ap.Advance(.5f); Assert.That(ap.Current, Is.EqualTo(5));
        }

        [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidTimeDoesNotMutateRecovery(float seconds)
        {
            var ap = new CombatActionPoints(); ap.Advance(.5f);
            Assert.That(ap.Advance(seconds), Is.False); Assert.That(ap.Current, Is.EqualTo(6));
            ap.Advance(.5f); Assert.That(ap.Current, Is.EqualTo(7));
        }

        [TestCase(-1)] [TestCase(0)] [TestCase(GameConstants.BattleAllyMaximumAp + 1)]
        public void InvalidCostCannotSpendEvenAtMaximum(int cost)
        {
            var ap = new CombatActionPoints(); ap.Advance(14f);
            Assert.That(ap.CanStart(cost), Is.False); Assert.That(ap.TrySpend(cost), Is.False);
            Assert.That(ap.Current, Is.EqualTo(GameConstants.BattleAllyMaximumAp));
        }

        [Test]
        public void InsufficientApDoesNotNotifyOrChangeHpMpAndCosts()
        {
            var world = World(); var flow = new CombatFlow(world, new LocalCombatAuthority(), true);
            flow.ConfigureActionPointCosts(2, 6); var events = 0;
            flow.ActionStarted += _ => events++; flow.ResultApplied += _ => events++;
            var request = new CombatRequest(1, 2, CombatActionType.Magic, 101);
            Assert.That(flow.TryExecute(request, out _, out var reason), Is.False);
            Assert.That(reason, Does.Contain("AP"));
            Assert.That((world.Get(1).Hp, world.Get(1).Mp, world.Get(2).Hp, world.Get(1).ActionPoints.Current, events),
                Is.EqualTo((100, 30, 100, 6, 0)));
            flow.AdvanceActionPoints(14f);
            flow.ActionStarted += _ => Assert.That(world.Get(1).ActionPoints.Current, Is.EqualTo(4));
            Assert.That(flow.TryExecute(request, out _, out _), Is.True);
            Assert.That(world.Get(1).Mp, Is.EqualTo(20)); Assert.That(events, Is.EqualTo(2));
        }

        [Test]
        public void InvalidRequestsDoNotSpendFullAp()
        {
            var world = World(); var flow = new CombatFlow(world, new LocalCombatAuthority(), true);
            flow.ConfigureActionPointCosts(2, 6); flow.AdvanceActionPoints(14f); world.Get(1).Mp = 0;
            var events = 0; flow.ActionStarted += _ => events++;
            Assert.That(flow.TryExecute(new CombatRequest(1, 2, CombatActionType.Magic, 101), out _, out _), Is.False);
            Assert.That(flow.TryExecute(new CombatRequest(1, 2, CombatActionType.Skill), out _, out _), Is.False);
            Assert.That(flow.TryExecute(new CombatRequest(1, 99, CombatActionType.Attack), out _, out _), Is.False);
            Assert.That(world.Get(1).ActionPoints.Current, Is.EqualTo(GameConstants.BattleAllyMaximumAp)); Assert.That(events, Is.Zero);
        }

        [Test]
        public void RecoveryIsSameForBothSidesButDoesNotRecoverDeadUnitsOrMp()
        {
            var world = World(); var flow = new CombatFlow(world, new LocalCombatAuthority(), true);
            flow.AdvanceActionPoints(1.5f);
            Assert.That(world.Get(1).ActionPoints.Current, Is.EqualTo(7));
            Assert.That(world.Get(2).ActionPoints.Current, Is.EqualTo(7));
            world.Get(2).Hp = 0; world.Get(2).LifeState = UnitLifeState.Corpse;
            flow.AdvanceActionPoints(2.5f);
            Assert.That(world.Get(1).ActionPoints.Current, Is.EqualTo(10));
            Assert.That(world.Get(2).ActionPoints.Current, Is.EqualTo(7)); Assert.That(world.Get(1).Mp, Is.EqualTo(30));
            Assert.That(flow.AdvanceActionPoints(float.NaN), Is.False);
        }

        [Test]
        public void UnconfiguredCostsFailClosedWithoutUsingDemonstrationValues()
        {
            var world = World(); var flow = new CombatFlow(world, new LocalCombatAuthority(), true);
            flow.AdvanceActionPoints(14f);
            Assert.That(flow.TryExecute(new CombatRequest(1, 2, CombatActionType.Attack), out _, out var reason), Is.False);
            Assert.That(reason, Does.Contain("설정"));
            Assert.That(world.Get(1).ActionPoints.Current, Is.EqualTo(GameConstants.BattleAllyMaximumAp)); Assert.That(world.Get(2).Hp, Is.EqualTo(100));
        }

        [Test]
        public void AttackCostsAreActorSpecificAndMagicCostsAreIdSpecific()
        {
            var world = World(); var flow = new CombatFlow(world, new LocalCombatAuthority(), true);
            Assert.That(flow.ConfigureAttackApCost(1, 3), Is.True); Assert.That(flow.ConfigureAttackApCost(2, 7), Is.True);
            Assert.That(flow.ConfigureMagicApCost(101, 5), Is.True); Assert.That(flow.ConfigureMagicApCost(102, 8), Is.True);
            Assert.That(flow.GetAttackApCost(1), Is.EqualTo(3)); Assert.That(flow.GetAttackApCost(2), Is.EqualTo(7));
            Assert.That(flow.GetMagicApCost(101), Is.EqualTo(5)); Assert.That(flow.GetMagicApCost(102), Is.EqualTo(8));
            flow.AdvanceActionPoints(14f);
            Assert.That(flow.TryExecute(new CombatRequest(1, 2, CombatActionType.Attack), out _, out _), Is.True);
            Assert.That(flow.TryExecute(new CombatRequest(2, 1, CombatActionType.Attack), out _, out _), Is.True);
            Assert.That(world.Get(1).ActionPoints.Current, Is.EqualTo(7)); Assert.That(world.Get(2).ActionPoints.Current, Is.EqualTo(13));
            Assert.That(flow.ConfigureAttackApCost(99, 4), Is.False);
            Assert.That(flow.ConfigureActionPointCosts(2, 21), Is.False);
            Assert.That(flow.ConfigureMagicApCost(101, 0), Is.False);
            Assert.That(flow.GetAttackApCost(1), Is.EqualTo(3)); Assert.That(flow.GetMagicApCost(101), Is.EqualTo(5));
        }

        private static CombatWorld World()
        {
            var world = new CombatWorld();
            world.Add(new CombatUnit { Id = 1, Hp = 100, MaxHp = 100, Mp = 30, MaxMp = 30, Attack = 26, Defense = 7 });
            world.Add(new CombatUnit { Id = 2, Enemy = true, Hp = 100, MaxHp = 100, Attack = 18, Defense = 10 });
            return world;
        }

        [Test]
        public void FactionMaximumsRecoverIndependentlyAndValidateActorSpecificCosts()
        {
            var world = new CombatWorld();
            world.Add(new CombatUnit { Id = 1, Hp = 100, MaxHp = 100, Mp = 30, MaxMp = 30, Attack = 26, Defense = 7 });
            world.Add(new CombatUnit { Id = 2, Enemy = true, Hp = 100, MaxHp = 100, Mp = 30, MaxMp = 30, Attack = 18, Defense = 10 });
            var ally = world.Get(1).ActionPoints; var enemy = world.Get(2).ActionPoints;
            Assert.That((ally.Maximum, enemy.Maximum), Is.EqualTo((10, 20)));
            ally.Advance(4); enemy.Advance(4);
            Assert.That((ally.Current, enemy.Current), Is.EqualTo((10, 10)));
            Assert.That(ally.CanStart(4), Is.True); Assert.That(enemy.CanStart(6), Is.False);
            enemy.Advance(10); Assert.That(enemy.Current, Is.EqualTo(20));
            Assert.That(enemy.CanStart(6), Is.True);
            var flow = new CombatFlow(world, new LocalCombatAuthority(), true);
            Assert.That(flow.ConfigureAttackApCost(1, 11), Is.False);
            Assert.That(flow.ConfigureAttackApCost(1, 2), Is.True);
            Assert.That(flow.ConfigureAttackApCost(2, 11), Is.True);
            Assert.That(flow.ConfigureActionPointCosts(12, 6), Is.False);
            Assert.That(flow.GetAttackApCost(1), Is.EqualTo(2)); Assert.That(flow.GetAttackApCost(2), Is.EqualTo(11));
            Assert.That(flow.ConfigureMagicApCost(101, 15), Is.True); // Explicit fixture cost, not original formula.
            Assert.That(flow.CanStartAction(new CombatRequest(1, 2, CombatActionType.Magic, 101), out _), Is.False);
            Assert.That(flow.CanStartAction(new CombatRequest(2, 1, CombatActionType.Magic, 101), out _), Is.True);
            Assert.That(ally.TrySpend(4), Is.True); Assert.That(enemy.TrySpend(6), Is.True);
            Assert.That((ally.Current, enemy.Current), Is.EqualTo((6, 14)));
            ally.ApplyAbsolute(int.MaxValue); enemy.ApplyAbsolute(int.MaxValue);
            Assert.That((ally.Current, enemy.Current), Is.EqualTo((10, 20)));
            world.TryGetSnapshot(1, out var a); world.TryGetSnapshot(2, out var e);
            Assert.That((a.MaxAp, e.MaxAp), Is.EqualTo((10, 20)));
        }

        [Test]
        public void TwentyIsSpendableButRawMagicTickAboveMaximumIsNotClamped()
        {
            var world = new CombatWorld();
            var ap = new CombatActionPoints(enemy: true);
            ap.ApplyAbsolute(int.MaxValue);
            Assert.That(ap.Current, Is.EqualTo(20));
            Assert.That(ap.CanStart(11), Is.True);
            Assert.That(ap.TrySpend(20), Is.True);
            Assert.That(ap.Current, Is.Zero);
            world.Add(new CombatUnit { Id = 1, Hp = 100, MaxHp = 100 });
            var flow = new CombatFlow(world, new LocalCombatAuthority(), true);
            Assert.That(flow.ConfigureMagicApCost(201, 25), Is.False);
            Assert.That(flow.GetMagicApCost(201), Is.Zero);
        }
    }
}
