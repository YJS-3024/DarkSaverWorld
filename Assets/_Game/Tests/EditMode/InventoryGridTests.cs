using System;
using DarkSaver.Combat;
using DarkSaver.Core;
using DarkSaver.Player;
using DarkSaver.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    [NUnit.Framework.Category("LegacySinglePlayer")]
    [NUnit.Framework.Ignore("싱글판 구현 삭제: 원작 서버 기반 검사로 교체 필요")]
    public sealed class InventoryGridTests
    {
        // Archived single-player behavior is tested only inside an explicit fixture.

        [Test]
        public void MinimizedInventoryAssetIsIndependentAndHasOriginalSize()
        {
            var catalog = Resources.Load<NativeBattleWindows>("UI/TownInventoryMinimized/NativeTownInventoryMinimized");
            Assert.That(catalog, Is.Not.Null);
            var window = catalog.Find(4);
            Assert.That(window.size, Is.EqualTo(new Vector2Int(29,30)));
            Assert.That(window.controls, Is.Empty);
            Assert.That(window.background.width, Is.EqualTo(29)); Assert.That(window.background.height, Is.EqualTo(30));
            foreach(var dependency in UnityEditor.AssetDatabase.GetDependencies(UnityEditor.AssetDatabase.GetAssetPath(catalog)))
                Assert.That(dependency.StartsWith("Assets/"), Is.True, dependency);
        }
        [Test]
        public void NativeEquipmentInventoryPreservesOriginalWindowAndControls()
        {
            var catalog = Resources.Load<NativeBattleWindows>("UI/TownInventory/NativeTownInventory");
            Assert.That(catalog, Is.Not.Null);
            var window = catalog.Find(4);
            Assert.That(window.size, Is.EqualTo(new Vector2Int(409,227)));
            Assert.That(window.background.width, Is.EqualTo(409)); Assert.That(window.background.height, Is.EqualTo(227));
            Assert.That(window.controls.Length, Is.EqualTo(3));
            int[] ids = {37,38,6}; Vector2[] positions = {new(360,16),new(377,16),new(343,191)};
            for(int i=0;i<ids.Length;i++) {
                Assert.That(window.controls[i].nativeId, Is.EqualTo(ids[i]));
                Assert.That(window.controls[i].rect.position, Is.EqualTo(positions[i]));
                Assert.That(window.controls[i].frames.Length, Is.EqualTo(3));
                foreach(var frame in window.controls[i].frames) Assert.That(frame, Is.Not.Null);
            }
            Assert.That(InventoryGridLayout.Cell(0,new Vector2(146,42)), Is.EqualTo(new Rect(146,42,32,32)));
            Assert.That(InventoryGridLayout.Cell(27,new Vector2(146,42)), Is.EqualTo(new Rect(356,147,32,32)));
            foreach(var dependency in UnityEditor.AssetDatabase.GetDependencies(UnityEditor.AssetDatabase.GetAssetPath(catalog)))
                Assert.That(dependency.StartsWith("Assets/"), Is.True, dependency);
        }

        [TestCase(0, 20, 40)]
        [TestCase(6, 230, 40)]
        [TestCase(7, 20, 75)]
        [TestCase(27, 230, 145)]
        public void OriginalSevenByFourGeometryIsPreserved(int slot, int x, int y)
            => Assert.That(InventoryGridLayout.Cell(slot, new Vector2(20, 40)), Is.EqualTo(new Rect(x, y, 32, 32)));

        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(28, 1)]
        [TestCase(29, 2)]
        [TestCase(56, 2)]
        [TestCase(57, 3)]
        [TestCase(10000, 358)]
        public void PageCountNeverImposesANewOwnershipLimit(int count, int pages)
            => Assert.That(InventoryGridLayout.PageCount(count), Is.EqualTo(pages));

        [Test]
        public void PagesAndEmptyCellsAreReadOnlyAndRejectOutOfRangeAccess()
        {
            var active = SceneManager.GetActiveScene();
            var town = SceneManager.GetSceneByName("Town_Runtime"); bool created = !town.isLoaded;
            if (created) { town = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); town.name = "Town_Runtime"; }
            var root = new GameObject("Inventory grid fixture"); SceneManager.MoveGameObjectToScene(root, town);
            try {
                var player = root.AddComponent<TopDownPlayerController>();
                var session = root.AddComponent<BattleTransitionController>();
                Assert.That(session.InitializeCharacter(new NativeCharacterProfile {name = "fixture", jobId = 100}), Is.True);
                var state = session.CaptureSaveState("Kaosia_Map", "Town_Runtime", Vector2.one);
                state.inventory.gold = 321; state.inventory.items = new NativeItemStack[29];
                for (int i = 0; i < 29; i++) state.inventory.items[i] = new NativeItemStack {itemId = i + 1, quantity = i + 1};
                Assert.That(session.RestoreSaveState(state), Is.True);
                var panel = root.AddComponent<TownServicesPanel>();
                Assert.That(panel.SetInventoryPage(1), Is.False);
                Assert.That(panel.Open(TownService.Inventory, player), Is.True);
                Assert.That(panel.InventoryPageCount, Is.EqualTo(2)); Assert.That(panel.InventoryPageIndex, Is.Zero);
                Assert.That(panel.TryGetInventoryCell(27, out int id, out int quantity), Is.True);
                Assert.That((id, quantity), Is.EqualTo((28, 28)));
                for (int i = 0; i < 32; i++) {
                    Assert.That(panel.SetInventoryPage(1), Is.True);
                    Assert.That(panel.TryGetInventoryCell(0, out id, out quantity), Is.True);
                    Assert.That((id, quantity), Is.EqualTo((29, 29)));
                    Assert.That(panel.TryGetInventoryCell(1, out _, out _), Is.False);
                    Assert.That(panel.SetInventoryPage(0), Is.True);
                }
                Assert.That(panel.SetInventoryPage(-1), Is.False); Assert.That(panel.SetInventoryPage(2), Is.False);
                Assert.That(panel.TryGetInventoryCell(-1, out _, out _), Is.False);
                Assert.That(panel.TryGetInventoryCell(28, out _, out _), Is.False);
                Assert.That(session.TryGetOwnedItemAt(29, out _, out _), Is.False);
                Assert.That(session.CurrentGold, Is.EqualTo(321)); Assert.That(session.OwnedItemTypeCount, Is.EqualTo(29));
                Assert.That(session.OwnedItemCount(29), Is.EqualTo(29));
                Assert.That(panel.SetInventoryMinimized(true), Is.True);
                Assert.That(panel.IsInventoryMinimized, Is.True);
                Assert.That(panel.SetInventoryMinimized(true), Is.False);
                Assert.That(player.UiInputBlocked, Is.True); // Preserve the existing modal policy.
                Assert.That(session.CurrentGold, Is.EqualTo(321));
                Assert.That(panel.SetInventoryMinimized(false), Is.True);
                Assert.That(panel.IsInventoryMinimized, Is.False);
                Assert.That(panel.SetInventoryMinimized(true), Is.True);
                panel.Close(); Assert.That(player.UiInputBlocked, Is.False);
                Assert.That(panel.IsInventoryMinimized, Is.False);
                Assert.That(panel.SetInventoryMinimized(false), Is.False);
                Assert.That(panel.TryGetInventoryCell(0, out _, out _), Is.False);
                Assert.That(panel.Open(TownService.Inventory, player), Is.True); Assert.That(panel.InventoryPageIndex, Is.Zero);
                panel.Close();
            } finally {
                UnityEngine.Object.DestroyImmediate(root);
                if (created) EditorSceneManager.CloseScene(town, true);
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
            }
        }
    }
}
