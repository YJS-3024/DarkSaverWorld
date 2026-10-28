using System;
using DarkSaver.Combat;
using DarkSaver.Player;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class BattleFormationPanelTests
    {
        [Test]
        public void StatusReadsVitalsAndAssignmentWithoutChangingOwnedUnits()
        {
            WithPanel((panel, player, hud) => {
                var leader = Member(100);
                panel.Configure(new[] { leader }, 20, 20, Array.Empty<short>(), player);
                panel.Open();
                Assert.That(panel.MemberStatus("100"), Is.EqualTo("HP 40/50 · MP 0/10 · 대기"));
                Assert.That(panel.MemberStatus("missing"), Is.EqualTo("상태 미확인"));
                Assert.That(panel.Assign("100", "100"), Is.True);
                Assert.That(panel.MemberStatus("100"), Does.EndWith("출전 배정"));
                leader.Unit.Hp = 0;
                Assert.That(panel.MemberStatus("100"), Does.EndWith("출전 불가"));
                Assert.That(leader.Unit.Mp, Is.Zero);
                Assert.That(leader.CommanderId, Is.Null);
                panel.Cancel();
                Assert.That(panel.MemberStatus("100"), Does.EndWith("출전 불가"));
            });
        }

        [Test]
        public void EmptyRosterOpensWithoutInventingUnitsAndBlocksPointer()
        {
            WithPanel((panel, player, hud) => {
                Assert.That(panel.Open(), Is.True);
                Assert.That(panel.OwnedCount, Is.Zero); Assert.That(panel.AssignedCount, Is.Zero);
                Assert.That(panel.Message, Does.Contain("보유"));
                Assert.That(panel.Confirm(), Is.False); Assert.That(panel.ConfirmedSnapshot, Is.Null);
                Assert.That(player.UiInputBlocked, Is.True);
                Assert.That(hud.BlocksWorldPointer(new Vector2(-10f, -10f)), Is.True);
                panel.Cancel(); Assert.That(player.UiInputBlocked, Is.False);
                Assert.That(panel.IsOpen, Is.False); Assert.That(panel.AssignedCount, Is.Zero);
            });
        }

        [Test]
        public void DraftAssignmentCancelAndCommanderRemovalNeverMutateOwnership()
        {
            WithPanel((panel, player, hud) => {
                var leader = Member(100); var soldier = Member(109);
                panel.Configure(new[] { leader, soldier }, 20, 20, Array.Empty<short>(), player);
                panel.Open(); Assert.That(panel.Assign("109", "100"), Is.False);
                Assert.That(panel.Assign("100", "100"), Is.True);
                Assert.That(panel.Assign("109", "100"), Is.True);
                Assert.That(panel.Assign("109", "100"), Is.False);
                Assert.That(panel.AssignedCount, Is.EqualTo(2));
                Assert.That(soldier.CommanderId, Is.Null); Assert.That(leader.CommanderId, Is.Null);
                Assert.That(panel.Unassign("100"), Is.True); Assert.That(panel.AssignedCount, Is.Zero);
                panel.Assign("100", "100"); panel.Cancel(); panel.Open();
                Assert.That(panel.AssignedCount, Is.Zero); Assert.That(panel.OwnedCount, Is.EqualTo(2));
                Assert.That(soldier.Unit.ActionPoints.Current, Is.EqualTo(6));
            });
        }

        [Test]
        public void ConfirmationPreservesPreviousSnapshotOnFailureAndCancel()
        {
            WithPanel((panel, player, hud) => {
                var leader = Member(100); var soldier = Member(109);
                panel.Configure(new[] { leader, soldier }, 20, 20, Array.Empty<short>(), player);
                panel.Open(); panel.Assign("100", "100"); panel.Assign("109", "100");
                Assert.That(panel.Confirm(), Is.True); var saved = panel.ConfirmedSnapshot;
                Assert.That(saved.Members.Count, Is.EqualTo(2)); Assert.That(panel.IsOpen, Is.False);
                panel.Open(); Assert.That(panel.AssignedCount, Is.EqualTo(2));
                soldier.Unit.Hp = 0;
                Assert.That(panel.Confirm(), Is.False); Assert.That(panel.IsOpen, Is.True);
                Assert.That(panel.ConfirmedSnapshot, Is.SameAs(saved)); Assert.That(saved.Members[1].Unit.Hp, Is.EqualTo(40));
                panel.Unassign("100"); panel.Cancel();
                Assert.That(panel.ConfirmedSnapshot, Is.SameAs(saved));
                Assert.That(leader.CommanderId, Is.Null); Assert.That(player.UiInputBlocked, Is.False);
            });
        }

        [Test]
        public void InvalidOwnersIdsGroupsAndLimitsAreRejectedWithoutPartialAssignment()
        {
            WithPanel((panel, player, hud) => {
                var members = new[] { Member(100), Member(101), Member(102), Member(103), Member(108), Member(109),
                    Member(200), Member(300), Member(400), Member(500), Member(600), Member(601) };
                members[9].IsOwned = false; members[10].FinalStateConfirmed = false;
                members[11].PersistentId = "100";
                panel.Configure(members, 20, 20, Array.Empty<short>(), player); panel.Open();
                Assert.That(panel.Assign("100", "100"), Is.False); // Duplicate persistent ID is ambiguous.
                Assert.That(panel.Assign("500", "500"), Is.False); Assert.That(panel.Assign("600", "600"), Is.False);
                members[11].PersistentId = "601";
                panel.Configure(members, 20, 20, Array.Empty<short>(), player); panel.Open();
                panel.Assign("100", "100");
                foreach (var id in new[] { "101", "102", "103", "108" }) Assert.That(panel.Assign(id, "100"), Is.True);
                Assert.That(panel.Assign("109", "100"), Is.False);
                Assert.That(panel.Assign("200", "100"), Is.False);
                Assert.That(panel.Assign("200", "200"), Is.True); Assert.That(panel.Assign("300", "300"), Is.True);
                Assert.That(panel.Assign("400", "400"), Is.False); Assert.That(panel.AssignedCount, Is.EqualTo(7));
            });
        }

        [Test]
        public void MissingDeploymentContextAndReservedIdsDoNotConfirm()
        {
            WithPanel((panel, player, hud) => {
                var leader = Member(100);
                panel.Configure(new[] { leader }, 0, 0, null, player); panel.Open(); panel.Assign("100", "100");
                Assert.That(panel.Confirm(), Is.False); Assert.That(panel.ConfirmedSnapshot, Is.Null);
                panel.Configure(new[] { leader }, 20, 20, new short[] { 100 }, player); panel.Open(); panel.Assign("100", "100");
                Assert.That(panel.Confirm(), Is.False); Assert.That(panel.ConfirmedSnapshot, Is.Null);
                leader.Position = null;
                panel.Configure(new[] { leader }, 20, 20, Array.Empty<short>(), player); panel.Open(); panel.Assign("100", "100");
                Assert.That(panel.Confirm(), Is.False); Assert.That(leader.Position, Is.Null);
            });
        }

        [Test]
        public void InputBlockingIsRestoredOnCancelAndReconfigure()
        {
            WithPanel((panel, player, hud) => {
                player.SetUiInputBlocked(true); panel.Open(); panel.Cancel(); Assert.That(player.UiInputBlocked, Is.True);
                player.SetUiInputBlocked(false); panel.Open(); panel.Cancel(); panel.enabled = false;
                Assert.That(panel.Open(), Is.False); panel.enabled = true;
                panel.Open(); panel.Configure(null, 0, 0, null, player);
                Assert.That(panel.IsOpen, Is.False); Assert.That(player.UiInputBlocked, Is.False);
                Assert.That(panel.ConfirmedSnapshot, Is.Null);
            });
        }

        private static BattleFormationMemberDraft Member(short id) => new()
        {
            PersistentId = id.ToString(), IsOwned = true, FinalStateConfirmed = true,
            Unit = NativeCombatStateFixture.Received(new CombatUnit { Id = id, JobId = 100, Name = "fixture", X = id % 20, Y = id / 20,
                Hp = 40, MaxHp = 50, Mp = 0, MaxMp = 10, Attack = 0, Defense = 0 }),
            Position = new CombatGridCell(id % 20, id / 20), TeamId = 1, RecordEligible = true,
            LearnedMagicIds = Array.Empty<short>()
        };

        private static void WithPanel(Action<BattleFormationPanel, TopDownPlayerController, WorldSessionHud> action)
        {
            var active = SceneManager.GetActiveScene(); var dirty = active.isDirty;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Formation UI test"); SceneManager.MoveGameObjectToScene(go, preview);
                var hud = go.AddComponent<WorldSessionHud>(); var panel = go.GetComponent<BattleFormationPanel>();
                var playerGo = new GameObject("Formation input test"); SceneManager.MoveGameObjectToScene(playerGo, preview);
                var player = playerGo.AddComponent<TopDownPlayerController>();
                panel.Configure(null, 0, 0, null, player);
                action(panel, player, hud); panel.Cancel();
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active)); Assert.That(active.isDirty, Is.EqualTo(dirty));
        }
    }
}
