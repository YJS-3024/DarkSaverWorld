using System;
using DarkSaver.Combat;
using DarkSaver.Core;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    // Pure received-state contracts: no scene, time, authority or fabricated HP response.
    public sealed class NativeLifecycleStateTests
    {
        [TestCase(UnitLifeState.Corpse)]
        [TestCase(UnitLifeState.Removed)]
        public void DeathBeforeHpResponseSurvivesReturnAndSaveValidationWithoutRevival(UnitLifeState life)
        {
            var unit=new CombatUnit {Id=200,JobId=100,Name="received",Hp=40,MaxHp=50,Mp=2,MaxMp=10,X=1,Y=1};
            Assert.IsTrue(unit.ActionPoints.ConfigureMaximum(20));Assert.IsTrue(unit.ActionPoints.ApplyAbsolute(6));
            var draft=new BattleFormationMemberDraft {PersistentId="owned-200",CommanderPersistentId="owned-200",
                CommanderId=200,Unit=unit,Position=new CombatGridCell(1,1),IsOwned=true,
                FinalStateConfirmed=true,TeamId=1,RecordEligible=true,LearnedMagicIds=Array.Empty<short>()};
            Assert.IsTrue(BattleFormation.TryCreateSnapshot(new[]{draft},4,4,Array.Empty<short>(),out var deployment,out _));
            var owner=new NativeOwnedRoster {units=new[]{new NativeOwnedUnitData {persistentId="owned-200",instanceId=200,
                jobId=100,name="received",hp=40,maxHp=50,mp=2,maxMp=10,teamId=1,recordEligible=true,finalStateConfirmed=true,learnedMagicIds=Array.Empty<short>()}}};
            unit.LifeState=life;
            var world=new CombatWorld();world.Add(unit);
            Assert.IsTrue(world.TryGetSnapshot(200,out var observed));
            Assert.IsFalse(unit.Alive);
            Assert.IsTrue(BattleFormationReturn.TryCapture(deployment,new[]{observed},out var returned,out var reason),reason);
            Assert.IsTrue(owner.TryApplyObservedReturn(returned,out var updated,out reason),reason);
            Assert.AreEqual(40,updated.units[0].hp);Assert.AreEqual((int)life,updated.units[0].lifeState);
            Assert.AreEqual(0,owner.units[0].lifeState);Assert.AreEqual(40,deployment.Members[0].Unit.Hp);
            var checkpoint=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.RosterVersion,
                mapScene="Kaosia_Map",runtimeScene="Town_Runtime",hasOwnedRoster=true,ownedRoster=updated,
                hasVitals=true,hp=37,maxHp=120,mp=5,maxMp=30,lifeState=(int)life,clearedScenarios=Array.Empty<string>()};
            Assert.IsTrue(NativeSaveStore.IsValid(checkpoint));
            Assert.IsFalse(observed.Alive);
            // Removed/corpse records cannot be deployed even with positive last-received HP.
            Assert.IsFalse(BattleFormation.TryCreateSnapshot(new[]{draft},4,4,Array.Empty<short>(),out _,out _));
            var bad=updated.Clone();bad.units[0].lifeState=3;Assert.IsFalse(bad.IsValid);
            checkpoint.lifeState=-1;Assert.IsFalse(NativeSaveStore.IsValid(checkpoint));
            checkpoint.lifeState=(int)life;checkpoint.hp=checkpoint.maxHp+1;Assert.IsFalse(NativeSaveStore.IsValid(checkpoint));
        }
    }
}
