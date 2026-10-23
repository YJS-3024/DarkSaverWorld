using System;
using DarkSaver.Magic;
using DarkSaver.Items;
using DarkSaver.Player;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class MagicCatalogPanelTests
    {
        [TestCase(true)] [TestCase(false)]
        public void OpenCloseStopsMovementWithoutChangingEnabled(bool enabled)
        {
            WithPanel((panel, player, items) =>
            {
                player.enabled = enabled;
                player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.one;
                Assert.That(panel.Open(), Is.True); Assert.That(panel.Open(), Is.True);
                Assert.That(player.UiInputBlocked, Is.True);
                Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity, Is.EqualTo(Vector2.zero));
                Assert.That(player.TryMoveToWorld(Vector2.one), Is.False);
                panel.Close(); panel.Close();
                Assert.That(player.UiInputBlocked, Is.False);
                Assert.That(player.enabled, Is.EqualTo(enabled));
            });
        }

        [Test]
        public void NativeSearchAndSelectionKeepRawTickSeparate()
        {
            WithPanel((panel, player, items) =>
            {
                Assert.That(panel.MatchCount, Is.EqualTo(439));
                panel.Filter("파이어볼"); Assert.That(panel.SelectMagic(101), Is.True);
                Assert.That(panel.SelectedMagic.RawTick, Is.EqualTo(10));
                panel.Filter("201"); Assert.That(panel.SelectMagic(201), Is.True);
                Assert.That(panel.SelectedMagic.RawTick, Is.EqualTo(15));
                var detail = MagicCatalogPanel.DetailText(panel.SelectedMagic);
                Assert.That(detail, Does.Contain("TICK 원표: 15 (최종 AP 비용 아님)"));
                Assert.That(detail, Does.Contain("초 단위 미확정"));
                panel.Filter("no-such-magic-xyz");
                Assert.That(panel.SelectedMagic, Is.Null); Assert.That(panel.MatchCount, Is.Zero);
                Assert.That(panel.SelectMagic(101), Is.False);
                panel.Filter(null); Assert.That(panel.MatchCount, Is.EqualTo(439));
            });
        }

        [Test]
        public void SwitchingPanelsKeepsSingleInputOwner()
        {
            WithPanel((panel, player, items) =>
            {
                Assert.That(items.Open(), Is.True); Assert.That(panel.Open(), Is.True);
                Assert.That(items.IsOpen, Is.False); Assert.That(player.UiInputBlocked, Is.True);
                items.Close(); Assert.That(player.UiInputBlocked, Is.True);
                Assert.That(items.Open(), Is.True); Assert.That(panel.IsOpen, Is.False);
                Assert.That(player.UiInputBlocked, Is.True);
                panel.Close(); Assert.That(player.UiInputBlocked, Is.True);
                items.Close(); Assert.That(player.UiInputBlocked, Is.False);
            });
        }

        [Test]
        public void DisabledCannotOpenAndReconfigureReleasesOldPlayer()
        {
            WithPanel((panel, player, items) =>
            {
                panel.enabled = false;
                Assert.That(player.UiInputBlocked, Is.False); Assert.That(panel.Open(), Is.False);
                panel.enabled = true; panel.Open();
                var next = new GameObject("Magic next player");
                SceneManager.MoveGameObjectToScene(next, player.gameObject.scene);
                var controller = next.AddComponent<TopDownPlayerController>();
                panel.Configure(AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset"), controller);
                Assert.That(player.UiInputBlocked, Is.False); Assert.That(panel.IsOpen, Is.False);
                panel.Open(); Assert.That(controller.UiInputBlocked, Is.True);
                panel.Close(); Assert.That(controller.UiInputBlocked, Is.False);
            });
        }

        private static void WithPanel(Action<MagicCatalogPanel, TopDownPlayerController, ItemCatalogPanel> check)
        {
            var active = SceneManager.GetActiveScene(); var dirty = active.isDirty;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var playerGo = new GameObject("Magic test player"); SceneManager.MoveGameObjectToScene(playerGo, preview);
                var player = playerGo.AddComponent<TopDownPlayerController>();
                var panelGo = new GameObject("Magic test panels"); SceneManager.MoveGameObjectToScene(panelGo, preview);
                var panel = panelGo.AddComponent<MagicCatalogPanel>(); var items = panelGo.AddComponent<ItemCatalogPanel>();
                panel.Configure(AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset"), player);
                items.Configure(AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset"), player);
                check(panel, player, items); panel.Close(); items.Close();
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active)); Assert.That(active.isDirty, Is.EqualTo(dirty));
        }
    }
}
