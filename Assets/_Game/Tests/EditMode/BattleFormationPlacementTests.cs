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
    public sealed class BattleFormationPlacementTests
    {
        private static BattleFormationMemberDraft Member(short id) => new()
        {
            PersistentId = id.ToString(), IsOwned = true, FinalStateConfirmed = true,
            Unit = NativeCombatStateFixture.Received(new CombatUnit { Id = id, JobId = 100, Name = "fixture", X = 7, Y = 7,
                Hp = 40, MaxHp = 50, Mp = 2, MaxMp = 10, Attack = 0, Defense = 0 }),
            TeamId = 1, RecordEligible = true, LearnedMagicIds = Array.Empty<short>()
        };

        [Test]
        public void ExplicitPlacementIsDetachedAndConfirmedPositionSurvivesReopen()
        {
            WithPanel((panel, player) => {
                var leader = Member(200); leader.Unit.ActionPoints.ApplyAbsolute(10);
                panel.Configure(new[] { leader }, 0, 0, null, player); panel.Open(); panel.Assign("200", "200");
                Assert.That(panel.ConfigurePlacementContext(10, 10, Array.Empty<short>(), (job, cell) => job == 100), Is.True);
                Assert.That(panel.TrySetPosition("200", new CombatGridCell(1, 2)), Is.True);
                Assert.That(leader.Unit.X, Is.EqualTo(7)); Assert.That(leader.Unit.Y, Is.EqualTo(7));
                Assert.That(leader.Position, Is.Null); Assert.That(leader.Unit.ActionPoints.Current, Is.EqualTo(10));
                Assert.That(panel.Confirm(), Is.True); var saved = panel.ConfirmedSnapshot;
                Assert.That(saved.Members[0].Position, Is.EqualTo(new CombatGridCell(1, 2)));
                Assert.That(saved.Members[0].Unit.Ap, Is.EqualTo(10)); Assert.That(saved.Members[0].Unit.Mp, Is.EqualTo(2));
                panel.Open(); Assert.That(panel.Confirm(), Is.True);
                Assert.That(panel.ConfirmedSnapshot.Members[0].Unit.Ap, Is.EqualTo(10));
                Assert.That(panel.ConfirmedSnapshot.Members[0].Position, Is.EqualTo(new CombatGridCell(1, 2)));
            });
        }

        [Test]
        public void MissingContextBoundsBlockedFloorAndDuplicatePositionsAreRejected()
        {
            WithPanel((panel, player) => {
                panel.Configure(new[] { Member(200), Member(209) }, 0, 0, null, player); panel.Open();
                panel.Assign("200", "200"); panel.Assign("209", "200");
                Assert.That(panel.TrySetPosition("200", new CombatGridCell(1, 1)), Is.False);
                Assert.That(panel.ConfigurePlacementContext(10, 10, null, (_, _) => true), Is.False);
                Assert.That(panel.ConfigurePlacementContext(10, 10, Array.Empty<short>(), null), Is.False);
                panel.ConfigurePlacementContext(10, 10, Array.Empty<short>(), (_, cell) => cell.X != 3);
                Assert.That(panel.TrySetPosition("200", new CombatGridCell(-1, 0)), Is.False);
                Assert.That(panel.TrySetPosition("200", new CombatGridCell(10, 0)), Is.False);
                Assert.That(panel.TrySetPosition("200", new CombatGridCell(3, 0)), Is.False);
                Assert.That(panel.TrySetPosition("missing", new CombatGridCell(1, 1)), Is.False);
                Assert.That(panel.TrySetPosition("200", new CombatGridCell(1, 1)), Is.True);
                Assert.That(panel.TrySetPosition("209", new CombatGridCell(1, 1)), Is.False);
                Assert.That(panel.Confirm(), Is.False); Assert.That(panel.ConfirmedSnapshot, Is.Null);
                Assert.That(panel.TrySetPosition("209", new CombatGridCell(2, 1)), Is.True);
                Assert.That(panel.Confirm(), Is.True);
            });
        }

        [Test]
        public void ConfirmationRechecksTraversalAndMapChangeInvalidatesOldCoordinates()
        {
            WithPanel((panel, player) => {
                bool allows = true;
                panel.Configure(new[] { Member(200) }, 0, 0, null, player); panel.Open(); panel.Assign("200", "200");
                panel.ConfigurePlacementContext(10, 10, Array.Empty<short>(), (_, _) => allows);
                panel.TrySetPosition("200", new CombatGridCell(2, 1)); allows = false;
                Assert.That(panel.Confirm(), Is.False); Assert.That(panel.ConfirmedSnapshot, Is.Null);
                allows = true; Assert.That(panel.Confirm(), Is.True); panel.Open();
                Assert.That(panel.ConfigurePlacementContext(10, 10, Array.Empty<short>(), (_, _) => true), Is.True);
                Assert.That(panel.ConfirmedSnapshot, Is.Null); Assert.That(panel.Confirm(), Is.False);
                panel.TrySetPosition("200", new CombatGridCell(3, 1)); Assert.That(panel.Confirm(), Is.True);
                Assert.That(panel.ConfirmedSnapshot.Members[0].Unit.Ap, Is.EqualTo(10));
                Assert.That(panel.ConfirmedSnapshot.Members[0].Position, Is.EqualTo(new CombatGridCell(3, 1)));
            });
        }

        [Test]
        public void ReservedIdAndFailingTraversalDoNotConfirmOrMutateSupplier()
        {
            WithPanel((panel, player) => {
                var leader = Member(200); panel.Configure(new[] { leader }, 0, 0, null, player); panel.Open(); panel.Assign("200", "200");
                panel.ConfigurePlacementContext(10, 10, new short[] { 200 }, (_, _) => true);
                panel.TrySetPosition("200", new CombatGridCell(1, 1)); Assert.That(panel.Confirm(), Is.False);
                Assert.That(panel.ConfirmedSnapshot, Is.Null);
                panel.ConfigurePlacementContext(10, 10, Array.Empty<short>(), (_, _) => throw new InvalidOperationException("unknown traversal"));
                Assert.That(panel.TrySetPosition("200", new CombatGridCell(1, 1)), Is.False);
                Assert.That(leader.Unit.X, Is.EqualTo(7)); Assert.That(leader.Unit.Mp, Is.EqualTo(2));
                Assert.That(leader.Position, Is.Null);
            });
        }

        [Test]
        public void NativeMapPlacementUsesFloorTerrainAndUnambiguousJobColumn()
        {
            WithNativeMap((panel, ground, terrain, catalog, floor, attribute) => {
                var center = new CombatGridCell(1, 1); var area = ground.cellBounds;
                Assert.That(BattleMapPresentation.TryGetPlacementMovement(ground, terrain, area, new[] { catalog }, 100, center, out var value), Is.True);
                Assert.That(value, Is.EqualTo(1));
                Assert.That(BattleMapPresentation.TryGetPlacementMovement(ground, terrain, area, new[] { catalog }, 100, new CombatGridCell(1, 0), out _), Is.False);
                Assert.That(BattleMapPresentation.TryGetPlacementMovement(ground, terrain, area, new[] { catalog }, 999, center, out _), Is.False);
                Assert.That(BattleMapPresentation.TryGetPlacementMovement(ground, terrain, area, new[] { catalog, catalog }, 100, center, out _), Is.False);
                attribute.name = "Terrain_6"; terrain.SetTile(new Vector3Int(1, 1, 0), attribute);
                Assert.That(panel.TrySetPosition("200", center), Is.False);
                InitializeTroop(catalog, 1, 0);
                Assert.That(panel.TrySetPosition("200", center), Is.True);
                Assert.That(panel.Confirm(), Is.True);
                Assert.That(panel.Open(), Is.True); attribute.name = "unresolved";
                Assert.That(panel.Confirm(), Is.False);
                InitializeTroop(catalog, 1, 1); attribute.name = "Terrain_6";
                Assert.That(panel.TrySetPosition("200", center), Is.False);
            });
        }

        [Test]
        public void NativeMapChangeRejectsConfirmationInsteadOfReinterpretingAxis()
        {
            WithNativeMap((panel, ground, terrain, catalog, floor, attribute) => {
                Assert.That(panel.TrySetPosition("200", new CombatGridCell(0, 2)), Is.True);
                Assert.That(panel.Confirm(), Is.True); Assert.That(panel.Open(), Is.True);
                ground.SetTile(new Vector3Int(0, 3, 0), floor);
                Assert.That(panel.Confirm(), Is.False);
                Assert.That(panel.TrySetPosition("200", new CombatGridCell(0, 2)), Is.False);
                Assert.That(panel.ConfigureMapPlacement(ground, terrain, new[] { catalog }, Array.Empty<short>()), Is.True);
                Assert.That(panel.Confirm(), Is.False); // New context invalidates old native coordinates.
                Assert.That(panel.TrySetPosition("200", new CombatGridCell(0, 3)), Is.True);
                Assert.That(panel.Confirm(), Is.True);
                ground.enabled = false; Assert.That(panel.Open(), Is.True); Assert.That(panel.Confirm(), Is.False);
            });
        }

        private static void InitializeTroop(DarkSaver.Jobs.JobCatalog catalog, int troop, int special)
        {
            var values = new long[DarkSaver.Jobs.JobCatalogEntry.NumericColumnCount]; values[0] = 100; values[22] = troop; values[23] = special;
            catalog.Initialize(new[] { new DarkSaver.Jobs.JobCatalogEntry(values, "", "fixture", "", "") });
        }

        private static void WithNativeMap(Action<BattleFormationPanel, UnityEngine.Tilemaps.Tilemap,
            UnityEngine.Tilemaps.Tilemap, DarkSaver.Jobs.JobCatalog, UnityEngine.Tilemaps.Tile, UnityEngine.Tilemaps.Tile> action)
        {
            WithPanel((panel, player) => {
                var gridGo = new GameObject("Native placement grid", typeof(Grid)); SceneManager.MoveGameObjectToScene(gridGo, panel.gameObject.scene);
                var floorGo = new GameObject("Ground", typeof(UnityEngine.Tilemaps.Tilemap)); floorGo.transform.SetParent(gridGo.transform);
                var attrGo = new GameObject("TerrainAttributes", typeof(UnityEngine.Tilemaps.Tilemap)); attrGo.transform.SetParent(gridGo.transform);
                var ground = floorGo.GetComponent<UnityEngine.Tilemaps.Tilemap>(); var terrain = attrGo.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                var catalog = ScriptableObject.CreateInstance<DarkSaver.Jobs.JobCatalog>(); var floor = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                var attribute = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                try {
                    InitializeTroop(catalog, 0, 0); ground.SetTile(Vector3Int.zero, floor); ground.SetTile(new Vector3Int(2, 2, 0), floor);
                    ground.SetTile(new Vector3Int(1, 1, 0), floor);
                    panel.Configure(new[] { Member(200) }, 0, 0, null, player); Assert.That(panel.Open(), Is.True); Assert.That(panel.Assign("200", "200"), Is.True);
                    Assert.That(panel.ConfigureMapPlacement(ground, terrain, new[] { catalog }, Array.Empty<short>()), Is.True);
                    action(panel, ground, terrain, catalog, floor, attribute);
                } finally { UnityEngine.Object.DestroyImmediate(catalog); UnityEngine.Object.DestroyImmediate(floor); UnityEngine.Object.DestroyImmediate(attribute); }
            });
        }

        private static void WithPanel(Action<BattleFormationPanel, TopDownPlayerController> action)
        {
            var active = SceneManager.GetActiveScene(); var dirty = active.isDirty; var preview = EditorSceneManager.NewPreviewScene();
            try {
                var go = new GameObject("Formation placement test"); SceneManager.MoveGameObjectToScene(go, preview);
                var panel = go.AddComponent<BattleFormationPanel>(); var playerGo = new GameObject("Placement input test");
                SceneManager.MoveGameObjectToScene(playerGo, preview); var player = playerGo.AddComponent<TopDownPlayerController>();
                try { action(panel, player); } finally { panel.Cancel(); }
            } finally { EditorSceneManager.ClosePreviewScene(preview); }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active)); Assert.That(active.isDirty, Is.EqualTo(dirty));
        }
    }
}
