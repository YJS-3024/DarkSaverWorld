using System;
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
    public sealed class ItemCatalogPanelTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void OpenCloseStopsMovementWithoutChangingControllerEnabled(bool enabled)
        {
            WithPanel((panel, player, catalog) =>
            {
                player.enabled = enabled;
                var body = player.GetComponent<Rigidbody2D>();
                body.linearVelocity = Vector2.one;
                Assert.That(panel.Open(), Is.True);
                Assert.That(panel.Open(), Is.True);
                Assert.That(player.UiInputBlocked, Is.True);
                Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
                Assert.That(player.TryMoveToWorld(Vector2.one), Is.False);
                Assert.That(player.enabled, Is.EqualTo(enabled));
                panel.Close(); panel.Close();
                Assert.That(panel.IsOpen, Is.False);
                Assert.That(player.UiInputBlocked, Is.False);
                Assert.That(player.enabled, Is.EqualTo(enabled));
            });
        }

        [Test]
        public void FilterSelectPreservesLargeValuesAndNativeNames()
        {
            WithPanel((panel, player, catalog) =>
            {
                Assert.That(panel.MatchCount, Is.EqualTo(1180));
                Assert.That(panel.SelectedItem.Id, Is.Zero);
                panel.Filter("1245");
                Assert.That(panel.MatchCount, Is.EqualTo(1));
                Assert.That(panel.SelectItem(1245), Is.True);
                Assert.That(panel.SelectedItem.GetNumericValue(ItemCatalogEntry.PriceColumn), Is.EqualTo(100000000000L));
                Assert.That(panel.SelectedItem.PriceNotation, Is.EqualTo("1E+11"));
                panel.Filter("닥세 사이다세트");
                Assert.That(panel.SelectItem(1538), Is.True);
                Assert.That(ItemCatalogPanel.DisplayName(panel.SelectedItem), Is.EqualTo("닥세 사이다세트"));
                Assert.That(panel.SelectedItem.Name, Is.EqualTo("닥세_사이다세트"));
                Assert.That(catalog.Count, Is.EqualTo(1180));
            });
        }

        [Test]
        public void MissingFilterClearsSelectionAndEmptyNamesRemainInCatalog()
        {
            WithPanel((panel, player, catalog) =>
            {
                panel.Filter("no-such-item-xyz");
                Assert.That(panel.MatchCount, Is.Zero);
                Assert.That(panel.SelectedItem, Is.Null);
                Assert.That(panel.SelectItem(0), Is.False);
                panel.Filter(null);
                Assert.That(panel.SelectItem(0), Is.True);
                for (var i = 0; i < catalog.Count; i++)
                    if (catalog.GetAt(i).Name == string.Empty)
                    {
                        Assert.That(ItemCatalogPanel.DisplayName(catalog.GetAt(i)), Is.EqualTo("(이름 없음)"));
                        Assert.That(catalog.GetAt(i).Name, Is.Empty);
                        return;
                    }
                Assert.Fail("Expected original empty name.");
            });
        }

        [Test]
        public void ReconfigureReleasesOldPlayerBeforeUsingNewPlayer()
        {
            WithPanel((panel, player, catalog) =>
            {
                Assert.That(panel.Open(), Is.True);
                var next = new GameObject("Next player");
                SceneManager.MoveGameObjectToScene(next, player.gameObject.scene);
                var controller = next.AddComponent<TopDownPlayerController>();
                panel.Configure(catalog, controller);
                Assert.That(player.UiInputBlocked, Is.False);
                Assert.That(panel.IsOpen, Is.False);
                Assert.That(panel.Open(), Is.True);
                Assert.That(controller.UiInputBlocked, Is.True);
                panel.Close();
                Assert.That(controller.UiInputBlocked, Is.False);
            });
        }

        [Test]
        public void DisabledPanelCannotAcquireInputLock()
        {
            WithPanel((panel, player, catalog) =>
            {
                panel.enabled = false;
                Assert.That(panel.Open(), Is.False);
                Assert.That(player.UiInputBlocked, Is.False);
            });
        }

        private static void WithPanel(Action<ItemCatalogPanel, TopDownPlayerController, ItemCatalog> check)
        {
            var active = SceneManager.GetActiveScene();
            var dirty = active.isDirty;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset");
                Assert.That(catalog, Is.Not.Null);
                var playerGo = new GameObject("Catalog test player");
                SceneManager.MoveGameObjectToScene(playerGo, preview);
                var player = playerGo.AddComponent<TopDownPlayerController>();
                var panelGo = new GameObject("Catalog test panel");
                SceneManager.MoveGameObjectToScene(panelGo, preview);
                var panel = panelGo.AddComponent<ItemCatalogPanel>();
                panel.Configure(catalog, player);
                check(panel, player, catalog);
                panel.Close();
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(active.isDirty, Is.EqualTo(dirty));
        }
    }
}
