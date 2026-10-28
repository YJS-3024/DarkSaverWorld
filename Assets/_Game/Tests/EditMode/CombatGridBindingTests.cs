using System;
using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class CombatGridBindingTests
    {
        [Test]
        public void InvalidLayoutRejectsEntireInitialSetupWithoutGuessingCoordinates()
        {
            WithBattle((battle, unusedReceiver) => {
                var overlap = new[] { Place(100, 0, 0), Place(800, 0, 0) };
                Assert.That(Configure(battle, overlap), Is.False);
                Assert.That(Configure(battle, new[] { Place(100, 1, 1) }), Is.False);
                Assert.That(Configure(battle, new[] { Place(100, 1, 1), Place(999, 2, 2) }), Is.False);
                Assert.That(Configure(battle, new[] { Place(100, 1, 1), Place(800, 4, 4) }), Is.False);
                Assert.That(battle.TryConfigureGrid(4, 4, Layout(), (_, cell) => cell.X != 3, Cardinal), Is.False);
                Assert.That(battle.GridConfigured, Is.False);
                battle.TryGetUnit(100, out var player); battle.TryGetUnit(800, out var enemy);
                Assert.That(player.X, Is.EqualTo(0)); Assert.That(player.Y, Is.EqualTo(0));
                Assert.That(enemy.X, Is.EqualTo(0)); Assert.That(enemy.Y, Is.EqualTo(0));
                Assert.That(battle.TrySelectUnit(100), Is.False);
                Assert.That(battle.SelectedUnitId, Is.Null);
                Assert.That(Configure(battle, Layout()), Is.True);
                Assert.That(Configure(battle, Layout()), Is.False);
            });
        }

        [Test]
        public void ExplicitLayoutAndCellSelectionDoNotStartActionsOrFreezeAp()
        {
            WithBattle((battle, unusedReceiver) => {
                Assert.That(battle.TryRegisterGroup(new[] { Soldier() }), Is.True);
                var layout = new[] { Place(100, 1, 1), Place(101, 2, 1), Place(800, 3, 3) };
                Assert.That(Configure(battle, layout), Is.True);
                Assert.That(battle.SelectedUnitId, Is.Null);
                var selected = new List<short?>(); battle.UnitSelectionChanged += selected.Add;
                Assert.That(battle.TrySelectAtCell(Cell(2, 1)), Is.True);
                Assert.That(battle.TrySelectUnit(101), Is.True);
                Assert.That(selected.Count, Is.EqualTo(1)); Assert.That(selected[0], Is.EqualTo(101));
                Assert.That(battle.TryGetSelectedUnit(out var snapshot), Is.True);
                Assert.That(snapshot.Id, Is.EqualTo(101)); Assert.That(snapshot.X, Is.EqualTo(2));
                Assert.That(snapshot.Y, Is.EqualTo(1)); Assert.That(snapshot.Ap, Is.EqualTo(6));
                Assert.That(battle.CanSelectedUnitTraverse(Cell(1, 1)), Is.True);
                Assert.That(battle.CanSelectedUnitTraverse(Cell(3, 3)), Is.False);
                Assert.That(NativeCombatStateFixture.ReceiveFullTicks(battle), Is.True);
                battle.TryGetSelectedUnit(out var after); Assert.That(after.Ap, Is.EqualTo(after.MaxAp));
                Assert.That(snapshot.Ap, Is.EqualTo(6)); // Retained snapshot is not a mutable view.
                Assert.That(after.Hp, Is.EqualTo(50)); Assert.That(after.Mp, Is.EqualTo(10));
                Assert.That(after.Experience, Is.EqualTo(0)); Assert.That(battle.LastAttackPresentation, Is.Null);
                Assert.That(battle.PlayerAttackApCost, Is.EqualTo(0));
                battle.ClearUnitSelection(); battle.ClearUnitSelection();
                Assert.That(selected.Count, Is.EqualTo(2)); Assert.That(selected[1], Is.Null);
            });
        }

        [Test]
        public void InvalidSelectionPreservesPreviousAndRejectsEnemyEmptyOrDisabledInput()
        {
            WithBattle((battle, unusedReceiver) => {
                Configure(battle, Layout()); battle.TrySelectUnit(100);
                Assert.That(battle.TrySelectUnit(800), Is.False);
                Assert.That(battle.TrySelectUnit(999), Is.False);
                Assert.That(battle.TrySelectAtCell(Cell(2, 2)), Is.False);
                Assert.That(battle.TrySelectAtCell(Cell(-1, -1)), Is.False);
                Assert.That(battle.SelectedUnitId, Is.EqualTo(100));
                battle.enabled = false;
                Assert.That(battle.TrySelectUnit(100), Is.False);
                Assert.That(battle.TryGetSelectedUnit(out _), Is.False);
                Assert.That(battle.CanSelectedUnitTraverse(Cell(1, 2)), Is.False);
            });
        }

        [Test]
        public void HpZeroDeselectsButKeepsOccupancyUntilIndependentRemoval()
        {
            WithBattle((battle, receiver) => {
                Configure(battle, Layout()); battle.TrySelectUnit(100);
                var count = 0; battle.UnitSelectionChanged += selectedId => { Assert.That(selectedId, Is.Null); count++; };
                Assert.That(receiver.TryApplyPacket(new HpSetNotification(100, 0)), Is.True);
                Assert.That(battle.SelectedUnitId, Is.Null); Assert.That(count, Is.EqualTo(1));
                Assert.That(battle.TryGetGridOccupant(Cell(1, 1), out var id), Is.True); Assert.That(id, Is.EqualTo(100));
                Assert.That(battle.PlayerLifeState, Is.EqualTo(UnitLifeState.Active));
                Assert.That(battle.TrySelectUnit(100), Is.False);
                Assert.That(receiver.TryApplyPacket(new UnitRemoveNotification(100)), Is.True);
                Assert.That(battle.TryGetGridOccupant(Cell(1, 1), out _), Is.False);
                Assert.That(count, Is.EqualTo(1)); Assert.That(battle.PlayerLifeState, Is.EqualTo(UnitLifeState.Removed));
            });
        }

        [Test]
        public void DeathNotificationsReleaseOnlyThatInstanceWithoutWaitingForRemoval()
        {
            WithBattle((battle, receiver) => {
                battle.TryRegisterGroup(new[] { Soldier() });
                Configure(battle, new[] { Place(100, 1, 1), Place(101, 2, 1), Place(800, 3, 3) });
                battle.TrySelectUnit(101);
                Assert.That(receiver.TryApplyPacket(new UnitCorpseNotification(100)), Is.True);
                Assert.That(battle.SelectedUnitId, Is.EqualTo(101));
                Assert.That(battle.TryGetGridOccupant(Cell(1, 1), out _), Is.False);
                Assert.That(battle.CanSelectedUnitTraverse(Cell(1, 1)), Is.True);
                Assert.That(receiver.TryApplyPacket(new UnitRemoveNotification(100)), Is.True);
                Assert.That(battle.TryGetGridOccupant(Cell(1, 1), out _), Is.False);
                Assert.That(battle.CanSelectedUnitTraverse(Cell(1, 1)), Is.True);
                Assert.That(battle.TryGetGridOccupant(Cell(2, 1), out var id), Is.True); Assert.That(id, Is.EqualTo(101));
                Assert.That(receiver.TryApplyPacket(new UnitRemoveNotification(100)), Is.True);
                Assert.That(battle.SelectedUnitId, Is.EqualTo(101));
                Assert.That(receiver.TryApplyPacket(new UnitCorpseNotification(101)), Is.True);
                Assert.That(battle.SelectedUnitId, Is.Null);
            });
        }

        [Test]
        public void GridSetupPreventsLegacyRegistrationOrEnemyReplacementDesynchronizingOccupancy()
        {
            WithBattle((battle, unusedReceiver) => {
                Configure(battle, Layout());
                Assert.That(battle.TryRegisterGroup(new[] { Soldier() }), Is.False);
                Assert.That(battle.TryGetUnit(101, out _), Is.False);
                var enemy = AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>("Assets/_Game/Runtime/Combat/Brugos01Kisra.asset");
                Assert.That(enemy, Is.Not.Null); Assert.That(battle.ConfigureEnemy(enemy), Is.False);
                Assert.That(battle.TryGetUnit(800, out _), Is.True); Assert.That(battle.TryGetUnit(700, out _), Is.False);
                Assert.That(battle.TryGetGridOccupant(Cell(3, 3), out var id), Is.True); Assert.That(id, Is.EqualTo(800));
                Assert.That(battle.ConfigureEnemy(null), Is.True); // Existing no-op contract.
            });
        }

        [Test]
        public void BatchPlacementStaysAtomicIfLaterPolicyRejectsOrThrows()
        {
            var world = new CombatWorld(); world.Add(NativeCombatStateFixture.Received(new CombatUnit { Id = 100, Hp = 50 })); world.Add(NativeCombatStateFixture.Received(new CombatUnit { Id = 800, Hp = 50 }));
            var grid = new CombatOccupancyGrid(world, 4, 4, (unit, _) => unit.Id != 800, Cardinal);
            Assert.That(grid.TryPlaceUnits(Layout()), Is.False);
            Assert.That(grid.TryGetPosition(100, out _), Is.False); Assert.That(world.Get(100).X, Is.EqualTo(0));
            grid = new CombatOccupancyGrid(world, 4, 4, (unit, _) => unit.Id == 800
                ? throw new InvalidOperationException("fixture") : true, Cardinal);
            Assert.Throws<InvalidOperationException>(() => grid.TryPlaceUnits(Layout()));
            Assert.That(grid.TryGetPosition(100, out _), Is.False); Assert.That(world.Get(100).X, Is.EqualTo(0));
            Assert.That(grid.TryPlaceUnits(null), Is.False);
            Assert.That(grid.TryPlaceUnits(Array.Empty<CombatGridPlacement>()), Is.False);
        }

        private static CombatUnit Soldier() => NativeCombatStateFixture.Received(new CombatUnit { Id = 101, JobId = 100, Hp = 50, MaxHp = 50, Mp = 10, MaxMp = 10 });
        private static CombatGridCell Cell(int x, int y) => new CombatGridCell(x, y);
        private static CombatGridPlacement Place(short id, int x, int y) => new CombatGridPlacement(id, Cell(x, y));
        private static CombatGridPlacement[] Layout() => new[] { Place(100, 1, 1), Place(800, 3, 3) };
        private static bool Cardinal(CombatGridCell a, CombatGridCell b) => Math.Abs((long)a.X - b.X) + Math.Abs((long)a.Y - b.Y) == 1;
        private static bool Configure(BattleSceneRoot battle, CombatGridPlacement[] layout) => battle.TryConfigureGrid(4, 4, layout, (_, _) => true, Cardinal);
        private static void WithBattle(Action<BattleSceneRoot, BattlePacketReceiver> check)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try {
                var root = new GameObject("Grid binding check"); SceneManager.MoveGameObjectToScene(root, scene);
                var receiver = root.AddComponent<BattlePacketReceiver>(); NativeCombatStateFixture.Initialize(root.GetComponent<BattleSceneRoot>(),6);
                check(root.GetComponent<BattleSceneRoot>(), receiver);
            } finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
