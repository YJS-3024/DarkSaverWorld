using System;
using DarkSaver.Combat;
using DarkSaver.Network;
using DarkSaver.Player;
using DarkSaver.Jobs;
using DarkSaver.Magic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.Tests
{
    public sealed class BattleMapPresentationTests
    {
        [Test]
        public void BindingAlignsFreeWalkingBodiesBeforeTheirFirstGridStep()
        {
            WithMap(f=>{
                f.Ally.transform.position=new Vector3(1.11f,2.22f,0);
                Assert.IsTrue(f.Bind());
                Assert.AreEqual(new Vector3(1.5f,2.5f,0),f.Ally.transform.position);
                Assert.IsTrue(f.Battle.TryGetUnit(100,out var unit));
                Assert.AreEqual(1,unit.X);Assert.AreEqual(1,unit.Y);
            });
        }
        [Test]
        public void EncounterDeathDoesNotInventScenarioOrTrainingCompletion()
        {
            WithMap(f=>{Assert.IsTrue(f.Bind());f.Battle.SetScenarioOutcomeDeferred(true);
                Assert.IsTrue(f.Battle.ApplyHp(800,0));Assert.IsTrue(f.Battle.ApplyDeath(800,false));
                Assert.IsFalse(f.Battle.IsBattleFinished);Assert.IsFalse(f.Battle.ShowsEncounterOutcome);Assert.IsFalse(f.Battle.TryAcknowledgeOutcome());
                f.Battle.SetScenarioOutcomeDeferred(false);Assert.IsFalse(f.Battle.ShowsEncounterOutcome);Assert.AreEqual(0,f.Battle.PlayerExperience);});
        }
        [TestCase(5,2,BattleMagicDirection.Right)]
        [TestCase(-5,2,BattleMagicDirection.Left)]
        [TestCase(2,-5,BattleMagicDirection.Up)]
        [TestCase(2,5,BattleMagicDirection.Down)]
        [TestCase(3,3,BattleMagicDirection.Down)]
        [TestCase(-3,-3,BattleMagicDirection.Up)]
        [TestCase(0,0,BattleMagicDirection.Up)]
        public void PointerDirectionUsesNativeDominantAxisAndVerticalTie(float x, float y, BattleMagicDirection expected)
        {
            Assert.That(BattleMapPresentation.DirectionFromPointerDelta(new Vector2(x,y)), Is.EqualTo(expected));
        }

        [Test]
        public void BattleMagicMenuUsesConfirmedSubsetIncludingEmptySelection()
        {
            WithMap(f=> {
                Assert.IsTrue(f.Battle.ConfigurePlayerJob(100));Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));
                var catalog=AssetDatabase.LoadAssetAtPath<DarkSaver.Magic.MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
                var profile=new DarkSaver.Core.NativeCharacterProfile {name="검증",jobId=f.Battle.PlayerJobId,hasClassAbilities=true,
                    learnedMagicIds=new[]{101,102},usableSkillIds=System.Array.Empty<int>()};
                Assert.IsTrue(profile.TryBuildBattleMagicSelection(new[]{102},out var selected));
                Assert.IsTrue(f.View.ConfigureCharacterLearnedMagic(catalog,selected));Assert.AreEqual(1,f.View.DisplayedMagicCount);
                Assert.IsTrue(f.View.TryGetDisplayedMagic(0,out var magic));Assert.AreEqual(102,magic.Id);
                Assert.IsTrue(selected.TryBuildBattleMagicSelection(System.Array.Empty<int>(),out var empty));
                Assert.IsTrue(f.View.ConfigureCharacterLearnedMagic(catalog,empty));Assert.AreEqual(0,f.View.DisplayedMagicCount);
                CollectionAssert.AreEqual(new[]{101,102},empty.learnedMagicIds);
            });
        }

        [Test] public void MenuAnchorUsesSpriteCellCenterInsteadOfFootPivot()
        {
            var go = new GameObject("Menu anchor test");
            var texture = new Texture2D(32,32);
            var sprite = Sprite.Create(texture, new Rect(0,0,32,32), new Vector2(.25f,.1f), 32);
            try
            {
                var body = go.AddComponent<SpriteRenderer>(); body.sprite = sprite;
                go.transform.position = new Vector3(3,4,0);
                go.transform.localScale = new Vector3(2,3,1);
                Assert.That(Vector3.Distance(BattleMapPresentation.GetUnitMenuAnchor(body),
                    new Vector3(3.5f,5.2f,0)), Is.LessThan(.0001f));
                body.flipX = body.flipY = true;
                Assert.That(Vector3.Distance(BattleMapPresentation.GetUnitMenuAnchor(body),
                    new Vector3(2.5f,2.8f,0)), Is.LessThan(.0001f));
                body.sprite = null;
                Assert.That(BattleMapPresentation.GetUnitMenuAnchor(body), Is.EqualTo(go.transform.position));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void PrototypeHudCannotBeReenabledOrBlockNativeMapInput()
        {
            WithMap(f => {
                Assert.That(f.Bind(), Is.True);
                Assert.That(f.Battle.IsPrototypeHudVisible, Is.False);
                Assert.That(f.Battle.BattleHudRect, Is.EqualTo(default(Rect)));
                Assert.That(f.View.InputPanelRect, Is.EqualTo(default(Rect)));
                Assert.That(f.View.GetBasicCommandButtonRect(0), Is.EqualTo(default(Rect)));
                var point = Rendering.GameScreen.GuiToScreen(new Vector2(Rendering.GameScreen.Width - 130, Rendering.GameScreen.Height - 150));
                Assert.That(f.View.TryBlockMapPointer(point), Is.False);
                int ap = f.Battle.PlayerAp;
                f.Battle.SetPrototypeHudVisible(true);
                Assert.That(f.Battle.IsPrototypeHudVisible, Is.False);
                Assert.That(f.View.TryBlockMapPointer(point), Is.False);
                Assert.That(f.View.InputPanelRect, Is.EqualTo(default(Rect)));
                f.Battle.SetPrototypeHudVisible(false);
                Assert.That(f.View.TryBlockMapPointer(point), Is.False);
                Assert.That(f.Battle.PlayerAp, Is.EqualTo(ap));
                Assert.That(f.Battle.TrySelectUnit(100), Is.True);
                NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                Assert.That(f.View.TryOpenActionMenu(), Is.True);
                Assert.That(f.View.IsActionMenuOpen, Is.True);
            });
        }

        [Test]
        public void BasicCommandsRequireReceivedCostsAndNeverManufactureRestRecovery()
        {
            WithMap(f=>{
                Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));
                Assert.IsFalse(f.View.CanChooseBasicCommand(BattleMapCommand.Move,out _));
                Assert.IsFalse(f.View.CanChooseBasicCommand(BattleMapCommand.Rest,out _));
                Assert.IsTrue(f.Battle.ConfigureActionPointCosts(2,6));Assert.IsFalse(f.View.CanChooseBasicCommand(BattleMapCommand.Attack,out _));
                NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                Assert.IsTrue(f.View.TryChooseBasicCommand(BattleMapCommand.Attack));Assert.AreEqual(20,f.Battle.PlayerAp);
                f.View.SuspendPresentation();Assert.IsFalse(f.View.TryChooseBasicCommand(BattleMapCommand.Attack));
                Assert.AreEqual(120,f.Battle.PlayerHp);Assert.AreEqual(30,f.Battle.PlayerMp);
            });
        }

        [Test]
        public void ConfirmedLearnedMagicDisplaysWithoutInventingRequestsAndRejectsUnlearnedCommands()
        {
            WithMap(f => {
                Assert.That(f.Bind(), Is.True); Assert.That(f.Battle.ConfigurePlayerJob(100), Is.True);
                Assert.That(f.Battle.TrySelectUnit(100), Is.True);
                var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                BattleFormationSnapshot Formation(short magicId)
                {
                    var draft = new BattleFormationMemberDraft { PersistentId = "known-100", CommanderPersistentId = "known-100",
                        CommanderId = 100, Unit = NativeCombatStateFixture.Received(new CombatUnit { Id = 100, JobId = 100, Name = "Known caster", Hp = 50, MaxHp = 50 }),
                        IsOwned = true, FinalStateConfirmed = true, Position = new CombatGridCell(0, 0), TeamId = 1,
                        RecordEligible = true, LearnedMagicIds = new[] { magicId } };
                    Assert.That(BattleFormation.TryCreateSnapshot(new[] { draft }, 4, 4, Array.Empty<short>(), out var formation, out var reason), Is.True, reason);
                    return formation;
                }
                try
                {
                    var one = new long[MagicCatalogEntry.NumericColumnCount]; one[0] = 101; one[2] = 3; one[3] = 1; one[7] = 1;
                    var two = (long[])one.Clone(); two[0] = 102;
                    catalog.Initialize(new[] { new MagicCatalogEntry(one, "Known spell"), new MagicCatalogEntry(two, "Not learned") });
                    var requests = 0; f.Server.RequestReceived += _ => requests++;
                    Assert.That(f.View.ConfigureFormationLearnedMagic(catalog, Formation(101)), Is.True);
                    Assert.That(f.View.DisplayedMagicCount, Is.EqualTo(1)); Assert.That(f.View.MagicChoiceCount, Is.Zero);
                    Assert.That(f.Battle.CanStartMagicRequest(102, 100, Array.Empty<short>(), out var unlearnedReason), Is.False);
                    Assert.That(unlearnedReason, Does.Contain("습득"));
                    Assert.That(f.View.TryGetDisplayedMagic(0, out var entry), Is.True); Assert.That(entry.Id, Is.EqualTo(101));
                    Assert.That(f.View.TryOpenActionMenu(), Is.True); Assert.That(f.View.TryChooseCommand(BattleMapCommand.Magic), Is.True);
                    Assert.That(f.View.IsMagicChoiceOpen, Is.True); Assert.That(f.View.TrySelectDisplayedMagic(0), Is.False);
                    Assert.That(f.View.ConfigureFormationLearnedMagic(null, Formation(101)), Is.False);
                    Assert.That(f.View.ConfigureFormationLearnedMagic(catalog, Formation(999)), Is.False);
                    Assert.That(f.View.DisplayedMagicCount, Is.EqualTo(1)); Assert.That(f.View.IsMagicChoiceOpen, Is.True);
                    f.Battle.SetTemporaryLocalRules(true);
                    Assert.That(f.Battle.TemporaryLocalRulesEnabled, Is.False);
                    Assert.That(f.View.TrySelectDisplayedMagic(0), Is.False);
                    Assert.That(f.View.SelectedMagicId, Is.Null); Assert.That(requests, Is.Zero);
                    Assert.That(f.Battle.PlayerAp, Is.EqualTo(6)); Assert.That(f.Battle.PlayerMp, Is.EqualTo(30));
                    f.View.CancelMagicTargeting(); f.Battle.SetTemporaryLocalRules(false);
                    Assert.That(f.View.ConfigureMagicChoices(catalog, 100,
                        new[] { new MagicRequest(102, 100, 0, 0, 12345, Array.Empty<short>()) }, _ => true, _ => 1, 1, null), Is.False);
                    Assert.That(f.View.ConfigureMagicChoices(catalog, 100,
                        new[] { new MagicRequest(101, 100, 0, 0, 12345, Array.Empty<short>()) }, _ => true, _ => 1, 1, null), Is.True);
                    Assert.That(f.View.TryOpenActionMenu(), Is.True); Assert.That(f.View.TryChooseCommand(BattleMapCommand.Magic), Is.True);
                    Assert.That(f.View.TrySelectDisplayedMagic(0), Is.True);
                    Assert.That(f.View.SelectedMagicId, Is.EqualTo(101)); Assert.That(requests, Is.Zero);
                    Assert.That(f.Battle.PlayerAp, Is.EqualTo(6)); Assert.That(f.Battle.PlayerMp, Is.EqualTo(30));
                }
                finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [Test]
        public void MagicPreviewRendersNativeGeometryWithoutPacketsOrCost()
            => RenderMagicPreviewCheck(null);

        public void RenderMagicPreviewCheck(string outputPath)
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100);
                var data = new long[MagicCatalogEntry.NumericColumnCount]; data[0] = 101; data[2] = 3; data[3] = 1; data[7] = 1;
                var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                var target = new RenderTexture(256, 256, 24); var previous = RenderTexture.active;
                try
                {
                    catalog.Initialize(new[] { new MagicCatalogEntry(data, "Geometry only") });
                    var calls = 0; f.Server.RequestReceived += _ => calls++;
                    Assert.That(f.View.TryBeginMagicTargeting(catalog, new MagicRequest(101, 100, 0, 0, 0, Array.Empty<short>()), true), Is.True);
                    Assert.That(f.View.TryPreviewMagicAt(new CombatGridCell(2, 1)), Is.True);
                    Assert.That(f.View.MagicPreviewCellCount, Is.EqualTo(5));
                    var renderer = f.Ground.GetComponentInChildren<MeshRenderer>();
                    Assert.That(renderer, Is.Not.Null); Assert.That(renderer.enabled, Is.True);
                    Assert.That(renderer.sharedMaterial.color, Is.EqualTo(DarkSaver.GameConstants.BattleMagicPreviewColor));
                    f.Camera.transform.position = new Vector3(2, 2, -10); f.Camera.orthographicSize = 2.5f;
                    f.Camera.clearFlags = CameraClearFlags.SolidColor; f.Camera.backgroundColor = Color.black;
                    f.Camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(f.Ground.gameObject.scene);
                    f.Camera.targetTexture = target; f.Camera.Render(); RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); texture.Apply();
                    var blue = 0;
                    foreach (var pixel in texture.GetPixels32()) if (pixel.b > pixel.r + 10 && pixel.b > pixel.g + 5) blue++;
                    Assert.That(blue, Is.GreaterThan(100));
                    if (!string.IsNullOrEmpty(outputPath)) System.IO.File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                    Assert.That(calls, Is.Zero); Assert.That(f.Battle.PlayerAp, Is.EqualTo(6)); Assert.That(f.Battle.PlayerMp, Is.EqualTo(30));
                    Assert.That(f.Battle.EnemyHp, Is.EqualTo(75)); Assert.That(f.Battle.IsMovementInProgress(100), Is.False);
                    f.View.CancelMagicTargeting(); Assert.That(f.View.MagicPreviewCellCount, Is.Zero);
                    Assert.That(renderer.enabled, Is.False); Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.Zero);
                }
                finally
                {
                    f.Camera.targetTexture = null; RenderTexture.active = previous;
                    UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(catalog);
                }
            });
        }

        [Test]
        public void MagicPreviewClearsOnInvalidGroupDeathAndSuspend()
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100);
                var data = new long[MagicCatalogEntry.NumericColumnCount]; data[0] = 101; data[2] = 3; data[3] = 1; data[7] = 1;
                var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                try
                {
                    void Begin() { Assert.That(f.View.TryBeginMagicTargeting(catalog, new MagicRequest(101, 100, 0, 0, 0, Array.Empty<short>()), true), Is.True); }
                    catalog.Initialize(new[] { new MagicCatalogEntry(data, "Preview") }); Begin();
                    Assert.That(f.View.TryPreviewMagicAt(new CombatGridCell(2, 1)), Is.True);
                    Assert.That(f.View.TryPreviewMagicAt(new CombatGridCell(-1, 0)), Is.False); Assert.That(f.View.MagicPreviewCellCount, Is.Zero);
                    data[7] = 8; catalog.Initialize(new[] { new MagicCatalogEntry(data, "Group has no geometry") }); Begin();
                    Assert.That(f.View.TryPreviewMagicAt(new CombatGridCell(2, 1)), Is.False); Assert.That(f.View.MagicPreviewCellCount, Is.Zero);
                    data[7] = 1; catalog.Initialize(new[] { new MagicCatalogEntry(data, "Preview") }); Begin();
                    Assert.That(f.View.TryPreviewMagicAt(new CombatGridCell(2, 1)), Is.True);
                    f.View.SuspendPresentation(); Assert.That(f.View.MagicPreviewCellCount, Is.Zero);
                    Assert.That(f.View.ResumePresentation(), Is.True); Assert.That(f.View.MagicPreviewCellCount, Is.Zero);
                    Begin(); Assert.That(f.View.TryPreviewMagicAt(new CombatGridCell(2, 1)), Is.True);
                    f.Battle.ApplyDeath(100, false); f.View.RefreshVisualState();
                    Assert.That(f.View.IsMagicTargeting, Is.False); Assert.That(f.View.MagicPreviewCellCount, Is.Zero);
                }
                finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [Test]
        public void MagicPreviewUsesTransformedGroundAndReleasesOwnedResources()
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100);
                var data = new long[MagicCatalogEntry.NumericColumnCount]; data[0] = 101; data[2] = -1; data[3] = 2; data[7] = 1;
                var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                try
                {
                    catalog.Initialize(new[] { new MagicCatalogEntry(data, "Explicit direction") });
                    f.Ground.transform.parent.position = new Vector3(7, -3, 0);
                    f.Ground.transform.parent.rotation = Quaternion.Euler(0, 0, 17);
                    f.Ground.transform.parent.localScale = new Vector3(2, 1.5f, 1);
                    Assert.That(f.View.TryBeginMagicTargeting(catalog, new MagicRequest(101, 100, 0, 0, 0, Array.Empty<short>()), true), Is.True);
                    Assert.That(f.View.TryPreviewMagicAt(new CombatGridCell(-1, 0)), Is.True);
                    Assert.That(f.View.MagicPreviewCellCount, Is.EqualTo(2));
                    var renderer = f.Ground.GetComponentInChildren<MeshRenderer>(); var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                    var material = renderer.sharedMaterial;
                    Assert.That(Vector3.Distance(renderer.transform.TransformPoint(mesh.vertices[0]),
                        f.Ground.CellToWorld(new Vector3Int(2, 2, 0))), Is.LessThan(.0001f));
                    f.View.ReleaseMagicPreviewResources();
                    Assert.That(renderer == null && mesh == null && material == null, Is.True);
                }
                finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [Test]
        public void NativeGroupMagicClickBuildsOriginalListAndKeepsIndependentVitals()
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100); f.Battle.ConfigureMagicApCost(701, 3); NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var data = new long[MagicCatalogEntry.NumericColumnCount]; data[0] = 701; data[7] = 8;
                var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                try
                {
                    catalog.Initialize(new[] { new MagicCatalogEntry(data, "Native group") });
                    MagicRequest received = null; f.Server.RequestReceived += packet => received = packet as MagicRequest;
                    Assert.That(f.View.TryBeginNativeMagicTargeting(catalog,
                        new MagicRequest(701, 100, 0, 0, -123, Array.Empty<short>(), 0x92, 7),
                        id => id == 800, null, null, null), Is.True);
                    Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.True);
                    Assert.That(received.TargetUnitIds, Is.EqualTo(new short[] { 800 }));
                    Assert.That(received.X, Is.EqualTo(-1)); Assert.That(received.Y, Is.EqualTo(-1));
                    Assert.That(received.ComputedCost, Is.EqualTo(-123)); Assert.That(received.Mode, Is.EqualTo(7));
                    Assert.That(f.Battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 3)); Assert.That(f.Battle.PlayerMp, Is.EqualTo(30));
                    Assert.That(f.Battle.EnemyHp, Is.EqualTo(75)); Assert.That(f.View.IsMagicTargeting, Is.False);
                }
                finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [Test]
        public void NativeMagicUnknownTeamNeverDispatchesOrFallsThroughToMovement()
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100); f.Battle.ConfigureMagicApCost(101, 3); NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var data = new long[MagicCatalogEntry.NumericColumnCount]; data[0] = 101; data[2] = 3; data[6] = 3; data[7] = 1;
                var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                try
                {
                    catalog.Initialize(new[] { new MagicCatalogEntry(data, "Native unknown team") });
                    var calls = 0; f.Server.RequestReceived += _ => calls++;
                    Assert.That(f.View.TryBeginNativeMagicTargeting(catalog,
                        new MagicRequest(101, 100, 0, 0, 0, Array.Empty<short>()), _ => true, _ => null, 0, new int[8]), Is.True);
                    Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.False);
                    Assert.That(calls, Is.Zero); Assert.That(f.Battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp));
                    Assert.That(f.Battle.IsMovementInProgress(100), Is.False); Assert.That(f.View.IsMagicTargeting, Is.True);
                    f.View.CancelMagicTargeting();
                    Assert.That(f.View.TrySendSelectedNativeMagic(new CombatGridCell(-1, 0)), Is.False);
                }
                finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [Test]
        public void SpecialMagicAreaAcceptsNativeDirectionSentinelsWithoutDispatching()
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100);
                Assert.That(f.Battle.TryGetSelectedUnit(out var caster), Is.True);
                var values = new long[MagicCatalogEntry.NumericColumnCount];
                values[0] = 101; values[2] = -1; values[3] = 2; values[7] = 1;
                var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                try
                {
                    catalog.Initialize(new[] { new MagicCatalogEntry(values, "Native direction") });
                    Assert.That(f.View.TryGetMagicArea(catalog, 101, new CombatGridCell(-1, 0), out var cells), Is.True);
                    Assert.That(cells, Is.EqualTo(new[] { new CombatGridCell(caster.X + 1, caster.Y),
                        new CombatGridCell(caster.X + 2, caster.Y) }));
                    Assert.That(((System.Collections.Generic.IList<CombatGridCell>)cells).IsReadOnly, Is.True);
                    Assert.That(f.Battle.PlayerAp, Is.EqualTo(6)); Assert.That(f.Battle.PlayerMp, Is.EqualTo(30));
                    Assert.That(f.Battle.IsMovementInProgress(100), Is.False);
                    values[7] = 8; catalog.Initialize(new[] { new MagicCatalogEntry(values, "Group is separate") });
                    Assert.That(f.View.TryGetMagicArea(catalog, 101, new CombatGridCell(-1, 0), out var unsupported), Is.False);
                    Assert.That(unsupported.Count, Is.Zero);
                }
                finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [Test]
        public void StandardMagicAreaQueryUsesNativeCellOrderWithoutCastingOrMoving()
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100);
                var values = new long[MagicCatalogEntry.NumericColumnCount];
                values[0] = 101; values[2] = 3; values[3] = 1; values[7] = 1;
                var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                try
                {
                    catalog.Initialize(new[] { new MagicCatalogEntry(values, "Native standard area") });
                    Assert.That(f.View.TryGetStandardMagicArea(catalog, 101, f.Enemy.transform.position, out var cells), Is.True);
                    Assert.That(cells, Is.EqualTo(new[] { new CombatGridCell(1, 1), new CombatGridCell(2, 0),
                        new CombatGridCell(2, 1), new CombatGridCell(2, 2), new CombatGridCell(3, 1) }));
                    Assert.That(((System.Collections.Generic.IList<CombatGridCell>)cells).IsReadOnly, Is.True);
                    Assert.That(f.Battle.PlayerAp, Is.EqualTo(6)); Assert.That(f.Battle.PlayerMp, Is.EqualTo(30));
                    Assert.That(f.Battle.EnemyHp, Is.EqualTo(75)); Assert.That(f.Battle.LastAttackPresentation, Is.Null);
                    Assert.That(f.Battle.IsMovementInProgress(100), Is.False);
                    values[7] = 8;
                    catalog.Initialize(new[] { new MagicCatalogEntry(values, "Unresolved group target") });
                    Assert.That(f.View.TryGetStandardMagicArea(catalog, 101, f.Enemy.transform.position, out var unsupported), Is.False);
                    Assert.That(unsupported.Count, Is.Zero);
                }
                finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [Test]
        public void UnitMagicClickPreservesCommandMetadataAndWaitsForIndependentMp()
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100); f.Battle.ConfigureMagicApCost(201, 3);
                NativeCombatStateFixture.ReceiveFullTicks(f.Battle); var calls = 0;
                f.Server.RequestReceived += request => {
                    calls++; var packet = request as MagicRequest; Assert.That(packet, Is.Not.Null);
                    Assert.That(packet.MagicId, Is.EqualTo(201)); Assert.That(packet.CasterUnitId, Is.EqualTo(100));
                    Assert.That(packet.X, Is.EqualTo(2)); Assert.That(packet.Y, Is.EqualTo(1));
                    Assert.That(packet.TargetUnitIds, Is.EqualTo(new short[] { 800 }));
                    Assert.That(packet.ComputedCost, Is.EqualTo(-123)); Assert.That(packet.Mode, Is.EqualTo(7));
                    Assert.That(packet.OriginalOpcode, Is.EqualTo(0x92));
                };
                var catalog = AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
                Assert.That(f.View.TryBeginMagicTargeting(catalog,
                    new MagicRequest(201, 100, 0, 0, -123, Array.Empty<short>(), 0x92, 7), false), Is.True);
                Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.True);
                Assert.That(calls, Is.EqualTo(1)); Assert.That(f.View.IsMagicTargeting, Is.False);
                Assert.That(f.Battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 3)); Assert.That(f.Battle.PlayerMp, Is.EqualTo(30));
                Assert.That(f.Battle.EnemyHp, Is.EqualTo(75)); Assert.That(f.Receiver.LastMagicEffect, Is.Null);
            });
        }

        [Test]
        public void MissingDamageRuleCannotBeEnabledByExplicitSpellUi()
        {
            WithMap(f=>{
                Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var before=(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp);
                f.Battle.SetTemporaryLocalRules(true);Assert.IsFalse(f.Battle.TemporaryLocalRulesEnabled);
                Assert.IsFalse(f.Battle.ConfigureTemporaryDamageMagic(201,7,6,10,3));
                int sent=0;f.Server.RequestReceived+=_=>sent++;
                f.View.TryHandleWorldClick(f.Enemy.transform.position);
                Assert.AreEqual(before,(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp));
                Assert.AreEqual(0,sent);Assert.IsNull(f.Battle.LastAttackPresentation);
            });
        }

        [Test]
        public void MissingHealResponseCannotBeProducedBySelfClick()
        {
            WithMap(f=>{
                Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var before=(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp);
                f.Battle.SetTemporaryLocalRules(true);Assert.IsFalse(f.Battle.TemporaryLocalRulesEnabled);
                Assert.IsFalse(f.Battle.ConfigureTemporaryHealingMagic(2401,7,3,20));
                int sent=0;f.Server.RequestReceived+=_=>sent++;
                f.View.TryHandleWorldClick(f.Enemy.transform.position);
                Assert.AreEqual(before,(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp));
                Assert.AreEqual(0,sent);Assert.IsNull(f.Battle.LastAttackPresentation);
            });
        }

        [Test]
        public void MissingBuffResponseCannotAlterAttackThroughUi()
        {
            WithMap(f=>{
                Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var before=(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp);
                f.Battle.SetTemporaryLocalRules(true);Assert.IsFalse(f.Battle.TemporaryLocalRulesEnabled);
                Assert.IsFalse(f.Battle.ConfigureTemporaryBuffMagic(10101,3,5,20,0,30));
                int sent=0;f.Server.RequestReceived+=_=>sent++;
                f.View.TryHandleWorldClick(f.Enemy.transform.position);
                Assert.AreEqual(before,(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp));
                Assert.AreEqual(0,sent);Assert.IsNull(f.Battle.LastAttackPresentation);
            });
        }

        [Test]
        public void CatalogMetadataCannotReenableUnreceivedMagicResolution()
        {
            WithMap(f=>{
                Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));
                var catalog=AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
                Assert.IsNotNull(catalog);Assert.Greater(catalog.Count,4);
                var before=(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp);
                for(int repeat=0;repeat<2;repeat++)
                {
                    f.Battle.SetTemporaryLocalRules(true);Assert.IsFalse(f.Battle.TemporaryLocalRulesEnabled);
                    Assert.AreEqual(0,UnresolvedMagicExecution.Configure(f.Battle,catalog));
                    for(int i=0;i<catalog.Count;i++)
                    {
                        var entry=catalog.GetAt(i);
                        Assert.IsFalse(UnresolvedMagicExecution.TryClassify(entry,out _,out _),entry.Name);
                        Assert.IsFalse(f.Battle.TryGetTemporaryMagicMpCost(entry.Id,out _),entry.Name);
                    }
                }
                Assert.AreEqual(0,f.View.DisplayedMagicCount);
                Assert.AreEqual(before,(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp));
            });
        }

        [Test]
        public void UnknownCatalogEntryCannotBeClassifiedAsLocalDamage()
        {
            WithMap(f=>{
                Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var before=(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp);
                f.Battle.SetTemporaryLocalRules(true);Assert.IsFalse(f.Battle.TemporaryLocalRulesEnabled);
                Assert.IsFalse(f.Battle.ConfigureTemporaryDamageMagic(30901,7,6,10,3));
                int sent=0;f.Server.RequestReceived+=_=>sent++;
                f.View.TryHandleWorldClick(f.Enemy.transform.position);
                Assert.AreEqual(before,(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp));
                Assert.AreEqual(0,sent);Assert.IsNull(f.Battle.LastAttackPresentation);
            });
        }

        [Test]
        public void PositionMagicClickNeverFallsThroughToMovementOrAttack()
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100); f.Battle.ConfigureMagicApCost(201, 3);
                NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var catalog = AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
                MagicRequest received = null; f.Server.RequestReceived += packet => received = packet as MagicRequest;
                Assert.That(f.View.TryBeginMagicTargeting(catalog,
                    new MagicRequest(201, 100, 0, 0, 41, Array.Empty<short>()), true), Is.True);
                Assert.That(f.View.TryHandleWorldClick(new Vector3(.5f, .5f)), Is.True);
                Assert.That(received, Is.Not.Null); Assert.That(received.TargetCount, Is.Zero);
                Assert.That(received.X, Is.EqualTo(0)); Assert.That(received.Y, Is.EqualTo(3));
                Assert.That(f.Battle.IsMovementInProgress(100), Is.False);
                Assert.That(f.Battle.LastAttackPresentation, Is.Null);
            });
        }

        [Test]
        public void MagicTargetingFailureAndCancellationNeverSpendOrInventCosts()
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100); NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var catalog = AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
                var command = new MagicRequest(201, 100, 0, 0, 0, Array.Empty<short>());
                Assert.That(f.View.TryBeginMagicTargeting(catalog, command, false), Is.True);
                Assert.That(f.View.TryHandleWorldClick(new Vector3(.5f, .5f)), Is.False);
                Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.False); // AP cost unknown.
                Assert.That(f.View.IsMagicTargeting, Is.True); Assert.That(f.Battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp));
                f.Battle.ConfigureMagicApCost(201, 3);
                Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.False); // No handler.
                Assert.That(f.Battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp));
                f.View.CancelMagicTargeting(); f.View.CancelMagicTargeting();
                Assert.That(f.View.IsMagicTargeting, Is.False); Assert.That(f.Battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp));
                Assert.That(f.View.TryBeginMagicTargeting(catalog,
                    new MagicRequest(201, 100, 0, 0, 0, new short[] { 800, 800 }), false), Is.False);
                Assert.That(f.View.TryBeginMagicTargeting(catalog,
                    new MagicRequest(short.MaxValue, 100, 0, 0, 0, Array.Empty<short>()), false), Is.False);
                Assert.That(f.View.TryBeginMagicTargeting(catalog,
                    new MagicRequest(201, 800, 0, 0, 0, Array.Empty<short>()), false), Is.False);
            });
        }

        [Test]
        public void CasterDeathAndPresentationSuspensionCancelPendingMagic()
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100);
                var catalog = AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
                var command = new MagicRequest(201, 100, 0, 0, 0, Array.Empty<short>());
                Assert.That(f.View.TryBeginMagicTargeting(catalog, command, false), Is.True);
                f.View.SuspendPresentation(); Assert.That(f.View.IsMagicTargeting, Is.False);
                f.View.ResumePresentation(); Assert.That(f.View.TryBeginMagicTargeting(catalog, command, false), Is.True);
                f.Battle.ApplyDeath(100, true); f.View.RefreshVisualState();
                Assert.That(f.View.IsMagicTargeting, Is.False); Assert.That(f.Battle.PlayerAp, Is.EqualTo(6));
            });
        }

        [Test]
        public void BindingUsesNativeBodiesAndWorldCellsWithoutEnablingUnresolvedMovement()
        {
            WithMap(f => {
                Assert.That(f.Bind(), Is.True); Assert.That(f.View.IsBound, Is.True);
                Assert.That(f.Battle.MapPresentationActive, Is.True);
                Assert.That(f.View.TryWorldToCell(f.Ally.transform.position, out var cell), Is.True);
                Assert.That(cell.X, Is.EqualTo(1)); Assert.That(cell.Y, Is.EqualTo(1));
                f.Battle.TryGetUnit(100, out var unit); Assert.That(unit.X, Is.EqualTo(1)); Assert.That(unit.Y, Is.EqualTo(1));
                Assert.That(f.Battle.SelectedUnitId, Is.Null);
                Assert.That(f.View.TryHandleWorldClick(f.Ally.transform.position), Is.True);
                Assert.That(f.Battle.SelectedUnitId, Is.EqualTo(100));
                Assert.That(f.Battle.CanSelectedUnitTraverse(new CombatGridCell(1, 2)), Is.False);
                Assert.That(f.Battle.PlayerHp, Is.EqualTo(120)); Assert.That(f.Battle.PlayerMp, Is.EqualTo(30));
                Assert.That(f.Battle.PlayerAp, Is.EqualTo(6)); Assert.That(f.Battle.LastAttackPresentation, Is.Null);
                Assert.That(f.View.TryHandleWorldClick(new Vector3(.5f, .5f)), Is.False);
                Assert.That(f.Battle.PlayerAp, Is.EqualTo(6)); Assert.That(f.Bind(), Is.False);
            });
        }

        [Test]
        public void EnemyClickDispatchesSelectedActorWithNativeAttributesAndOnlyIndependentVitals()
        {
            WithMap(f => {
                f.Bind(); f.View.TryHandleWorldClick(f.Ally.transform.position);
                f.Battle.ConfigureActionPointCosts(2, 6); NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var calls = 0;
                f.Server.RequestReceived += packet => {
                    calls++; var request = packet as AttackRequest; Assert.That(request, Is.Not.Null);
                    Assert.That(request.AttackerUnitId, Is.EqualTo(100)); Assert.That(request.TargetUnitId, Is.EqualTo(800));
                    Assert.That(request.AttackerTileAttribute, Is.EqualTo(6)); Assert.That(request.TargetTileAttribute, Is.EqualTo(9));
                    Assert.That(f.Battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 2)); Assert.That(f.Battle.EnemyHp, Is.EqualTo(75));
                    Assert.That(f.Battle.LastAttackPresentation.Value.IsAnticipation, Is.True);
                    f.Server.SendToClient(new HpSetNotification(800, 50));
                    f.Server.SendToClient(new MpSetNotification(100, 7));
                };
                Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.True);
                Assert.That(calls, Is.EqualTo(1)); Assert.That(f.Battle.EnemyHp, Is.EqualTo(50));
                Assert.That(f.Battle.PlayerHp, Is.EqualTo(120)); Assert.That(f.Battle.PlayerMp, Is.EqualTo(7));
                Assert.That(f.Battle.PlayerExperience, Is.EqualTo(0)); Assert.That(f.Battle.SelectedUnitId, Is.EqualTo(100));
            });
        }

        [Test]
        public void MissingHandlerUnconfiguredCostOrInvalidTerrainNeverSpendsAp()
        {
            WithMap(f => {
                NativeCombatStateFixture.Initialize(f.Battle);Assert.IsTrue(f.Bind()); Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.False);
                f.View.TryHandleWorldClick(f.Ally.transform.position); NativeCombatStateFixture.SetAp(f.Battle,100,20,20);
                Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.False); // Unknown cost.
                Assert.IsTrue(f.Battle.ConfigureActionPointCosts(2, 6));
                Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.False); // No handler.
                Assert.That(f.Battle.PlayerAp, Is.EqualTo(20)); Assert.That(f.Battle.LastAttackPresentation, Is.Null);
                f.ActorTerrain.name = "unknown"; var calls = 0; f.Server.RequestReceived += _ => calls++;
                Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.False);
                Assert.That(calls, Is.EqualTo(0)); Assert.That(f.Battle.PlayerAp, Is.EqualTo(20));
            });
        }

        [Test]
        public void InvalidOrOverlappingBindingsDoNotChangeCoordinatesOrBodyVisibility()
        {
            WithMap(f => {
                f.Enemy.transform.position = f.Ally.transform.position;
                Assert.That(f.Bind(), Is.False); Assert.That(f.Battle.GridConfigured, Is.False);
                Assert.That(f.Battle.MapPresentationActive, Is.False);
                Assert.That(f.Ally.enabled && f.Enemy.enabled, Is.True);
                f.Battle.TryGetUnit(100, out var player); Assert.That(player.X, Is.EqualTo(0));
                f.Enemy.transform.position = new Vector3(2.5f, 2.5f);
                f.Ground.SetTile(new Vector3Int(1, 2, 0), null);
                Assert.That(f.Bind(), Is.False); Assert.That(f.Battle.GridConfigured, Is.False);
                Assert.That(f.View.TryHandleWorldClick(f.Ally.transform.position), Is.False);
            });
        }

        [Test]
        public void RemovedVisualsStayHiddenWhenDisablingAndResumingPresentation()
        {
            WithMap(f => {
                f.Bind(); f.Receiver.TryApplyPacket(new UnitRemoveNotification(800)); f.View.RefreshVisualState();
                Assert.That(f.Enemy.enabled, Is.False); Assert.That(f.Ally.enabled, Is.True);
                f.View.enabled = false;
                f.View.SuspendPresentation(); // Preview scenes do not run normal Play lifecycle callbacks.
                Assert.That(f.Enemy.enabled, Is.False); Assert.That(f.Battle.MapPresentationActive, Is.False);
                Assert.That(f.View.TryHandleWorldClick(f.Ally.transform.position), Is.False);
                f.View.enabled = true; Assert.That(f.View.ResumePresentation(), Is.True); f.View.RefreshVisualState();
                Assert.That(f.View.IsBound && f.Battle.MapPresentationActive, Is.True);
                Assert.That(f.Enemy.enabled, Is.False);
            });
        }

        [Test]
        public void OriginallyHiddenBodiesStayHiddenAndCorpseNeverUsesInventedFrames()
        {
            WithMap(f => {
                f.Ally.enabled = false; f.Bind(); f.View.RefreshVisualState();
                Assert.That(f.Ally.enabled, Is.False);
                var sprite = f.Enemy.sprite;
                Assert.That(f.Receiver.TryApplyPacket(new UnitCorpseNotification(800)), Is.True);
                f.View.RefreshVisualState(); Assert.That(f.Enemy.enabled, Is.False);
                Assert.That(f.Enemy.sprite, Is.SameAs(sprite)); // No walking-to-corpse frame substitution.
                f.View.enabled = false; f.View.SuspendPresentation(); Assert.That(f.Ally.enabled, Is.False);
            });
        }

        [Test]
        public void ExistingHudAndTerrainZeroAreExplicitNativeInputs()
        {
            WithMap(f => {
                f.Bind(); f.Terrain.SetTile(new Vector3Int(1, 2, 0), null);
                f.Terrain.SetTile(new Vector3Int(2, 2, 0), null);
                f.View.TryHandleWorldClick(f.Ally.transform.position);
                f.Battle.ConfigureActionPointCosts(2, 6); NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                f.Server.RequestReceived += packet => { var request = (AttackRequest)packet;
                    Assert.That(request.TargetTileAttribute, Is.EqualTo(0)); Assert.That(request.AttackerTileAttribute, Is.EqualTo(0)); };
                Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.True);
                f.Battle.SetPrototypeHudVisible(true);
                var panel = f.Battle.BattleHudRect;
                Assert.That(panel,Is.EqualTo(default(Rect)));
                Assert.That(f.Battle.IsPrototypeHudVisible,Is.False);
                Assert.That(f.View.TryWorldToCell(new Vector3(-1, -1), out _), Is.False);
            });
        }

        [Test]
        public void AdditionalSoldierClickSendsItsInstanceInsteadOfAlwaysUsingPlayer100()
        {
            WithMap(f => {
                Assert.That(f.Battle.TryRegisterGroup(new[] { NativeCombatStateFixture.Received(new CombatUnit {
                    Id = 101, JobId = 100, Hp = 50, MaxHp = 50, Mp = 10, MaxMp = 10 }) }), Is.True);
                var go = new GameObject("Explicit soldier fixture"); SceneManager.MoveGameObjectToScene(go, f.Ally.gameObject.scene);
                var body = go.AddComponent<SpriteRenderer>(); body.sprite = f.Ally.sprite;
                body.transform.position = new Vector3(.5f, 2.5f);
                Assert.That(f.View.TryBind(f.Ground, f.Terrain, f.Camera, new[] {
                    new BattleMapVisual(100, f.Ally), new BattleMapVisual(800, f.Enemy), new BattleMapVisual(101, body) }), Is.True);
                Assert.That(f.View.TryHandleWorldClick(body.transform.position), Is.True);
                Assert.That(f.Battle.SelectedUnitId, Is.EqualTo(101));
                f.Battle.ConfigureActionPointCosts(2, 6); NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                f.Server.RequestReceived += packet => Assert.That(((AttackRequest)packet).AttackerUnitId, Is.EqualTo(101));
                Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.True);
                f.Battle.TryGetUnit(101, out var unit); Assert.That(unit.Ap, Is.EqualTo(GameConstants.BattleMaximumAp - 2));
                Assert.That(f.Battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp));
            });
        }

        [Test]
        public void AttackFacingUsesBoundInstancesWithoutChangingVitalsAndSuspensionUnsubscribes()
        {
            WithMap(f => {
                var ally = f.Ally.gameObject.AddComponent<DirectionalSpriteAnimator>();
                var enemy = f.Enemy.gameObject.AddComponent<DirectionalSpriteAnimator>();
                Assert.That(f.Bind(), Is.True);
                Assert.That(f.Battle.ApplyAttackPresentation(800, 100, 123), Is.True);
                Assert.That(ally.Facing, Is.EqualTo(Vector2.right));
                Assert.That(enemy.Facing, Is.EqualTo(Vector2.left));
                Assert.That(f.Battle.PlayerHp, Is.EqualTo(120)); Assert.That(f.Battle.EnemyHp, Is.EqualTo(75));
                Assert.That(f.Battle.PlayerAp, Is.EqualTo(6)); Assert.That(f.Battle.PlayerExperience, Is.EqualTo(0));
                f.View.SuspendPresentation();
                Assert.That(ally.Facing, Is.EqualTo(Vector2.down)); Assert.That(enemy.Facing, Is.EqualTo(Vector2.down));
                f.Battle.ApplyAttackPresentation(800, 100, 123);
                Assert.That(ally.Facing, Is.EqualTo(Vector2.down));
                Assert.That(f.View.ResumePresentation(), Is.True); Assert.That(f.View.ResumePresentation(), Is.True);
                Assert.That(ally.Facing, Is.EqualTo(Vector2.down)); // Never replay stale attack on resume.
                f.Battle.ApplyAttackPresentation(800, 100, 123);
                Assert.That(ally.Facing, Is.EqualTo(Vector2.right));
                f.Receiver.TryApplyPacket(new UnitCorpseNotification(800));
                enemy.TrySetFacing(Vector2.up); f.Battle.ApplyAttackPresentation(800, 100, 123);
                Assert.That(enemy.Facing, Is.EqualTo(Vector2.up)); // No corpse rotation/resurrection.
            });
        }

        [Test]
        public void ScreenFacingUsesExistingCorrectedRowsAndNeverInventsMissingFrames()
        {
            WithMap(f => {
                var animator = f.Ally.gameObject.AddComponent<DirectionalSpriteAnimator>();
                var down = f.Ally.sprite;
                var left = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Converted/Characters/Fighter100/Fighter100_Left_1.png");
                var right = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Converted/Characters/Fighter100/Fighter100_Right_1.png");
                Assert.That(left, Is.Not.Null); Assert.That(right, Is.Not.Null);
                animator.Configure(null, new[] { down }, new[] { left }, new[] { right });
                Assert.That(animator.TrySetFacing(new Vector2(1, 1)), Is.True);
                animator.PresentFrame(0); AssertNativeFrame(f.Ally.sprite, left);
                Assert.That(animator.TrySetFacing(Vector2.left), Is.True);
                animator.PresentFrame(0); AssertNativeFrame(f.Ally.sprite, right);
                Assert.That(animator.TrySetFacing(Vector2.zero), Is.False);
                Assert.That(animator.TrySetFacing(new Vector2(float.NaN, 0)), Is.False);
                Assert.That(animator.Facing, Is.EqualTo(Vector2.left));
                animator.TrySetFacing(Vector2.up); animator.PresentFrame(0);
                AssertNativeFrame(f.Ally.sprite, right); // Missing row preserves existing sprite.
            });
        }

        [Test]
        public void BindingStopsOnlyVisualWalkingAndRestoresBorrowedMotionOnSuspend()
        {
            WithMap(f => {
                var animator = f.Ally.gameObject.AddComponent<DirectionalSpriteAnimator>();
                var frame0 = f.Ally.sprite;
                var frame1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Converted/Characters/Fighter100/Fighter100_Left_0.png");
                Assert.That(frame1, Is.Not.Null);
                animator.Configure(null, null, new[] { frame0, frame1, frame0 }, null);
                animator.SetMotion(Vector2.right); animator.PresentFrame(150);
                Assert.That(animator.IsMoving, Is.True); AssertNativeFrame(f.Ally.sprite, frame1);
                var position = f.Ally.transform.position;
                Assert.That(f.Bind(), Is.True);
                Assert.That(animator.IsMoving, Is.False); Assert.That(animator.Facing, Is.EqualTo(Vector2.right));
                animator.PresentFrame(150); AssertNativeFrame(f.Ally.sprite, frame0);
                animator.SetMotion(Vector2.right); f.View.RefreshVisualState();
                Assert.That(animator.IsMoving, Is.False); Assert.That(f.Ally.transform.position, Is.EqualTo(position));
                Assert.That(f.Battle.PlayerHp, Is.EqualTo(120)); Assert.That(f.Battle.PlayerAp, Is.EqualTo(6));
                f.View.SuspendPresentation(); Assert.That(animator.IsMoving, Is.True);
                animator.PresentFrame(150); AssertNativeFrame(f.Ally.sprite, frame1);
                Assert.That(f.View.ResumePresentation(), Is.True); Assert.That(animator.IsMoving, Is.False);
                f.View.SuspendPresentation(); Assert.That(animator.IsMoving, Is.True);
            });
        }

        [Test]
        public void RejectedBindingDoesNotChangeMotionAndStationaryBindingsStayStationary()
        {
            WithMap(f => {
                var animator = f.Ally.gameObject.AddComponent<DirectionalSpriteAnimator>();
                animator.SetMotion(Vector2.left); f.Enemy.transform.position = f.Ally.transform.position;
                Assert.That(f.Bind(), Is.False); Assert.That(animator.IsMoving, Is.True);
                Assert.That(animator.Facing, Is.EqualTo(Vector2.left));
                f.Enemy.transform.position = new Vector3(2.5f, 2.5f); animator.SetMotion(Vector2.zero);
                Assert.That(f.Bind(), Is.True); f.View.SuspendPresentation();
                Assert.That(animator.IsMoving, Is.False); Assert.That(animator.Facing, Is.EqualTo(Vector2.left));
            });
        }

        [Test]
        public void NativeJobCatalogBindsTerrainAndApprovedDiagonalsWithoutExecutingClicks()
        {
            WithMap(f => {
                var jobs = AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
                Assert.That(f.Battle.ConfigureTraversalCatalogs(new[] { jobs }), Is.True);
                Assert.That(f.Battle.ConfigurePlayerJob(100), Is.True); Assert.That(f.Bind(), Is.True);
                Assert.That(f.Battle.TryGetTroopColumn(100, out var troop), Is.True); Assert.That(troop, Is.EqualTo(0));
                var water = new CombatGridCell(1, 1); var floor = new CombatGridCell(1, 2);
                Assert.That(f.View.TryGetMovementAtCell(100, water, out var value), Is.True); Assert.That(value, Is.EqualTo(99));
                Assert.That(f.View.TryGetMovementAtCell(100, floor, out value), Is.True); Assert.That(value, Is.EqualTo(1));
                Assert.That(f.View.TryHandleWorldClick(f.Ally.transform.position), Is.True);
                Assert.That(f.Battle.CanSelectedUnitTraverse(floor), Is.True); // Traversal query, not executing a step.
                Assert.That(f.View.TryGetReachableMovementDestinations(out var destinations), Is.True);
                CollectionAssert.Contains(destinations, floor); CollectionAssert.DoesNotContain(destinations, water);
                foreach (var destination in destinations)
                    Assert.That(f.Battle.TryFindSelectedMovementPath(destination, out _), Is.True);
                Assert.That(f.View.TryFindWorldMovementPath(new Vector3(.5f, 1.5f), out var path), Is.True);
                Assert.That(path.Count, Is.EqualTo(1)); Assert.That(path[0], Is.EqualTo(new CombatGridCell(0, 2)));
                var ground = f.Ally.gameObject.scene.GetRootGameObjects();
                Tilemap nativeGround = null;
                foreach (var root in ground)
                    foreach (var map in root.GetComponentsInChildren<Tilemap>())
                        if (map.name == "Ground") nativeGround = map;
                Assert.That(nativeGround, Is.Not.Null);
                nativeGround.SetTile(new Vector3Int(0, 2, 0), null); // Native side cell (0,1).
                Assert.That(f.View.TryFindWorldMovementPath(new Vector3(.5f, 1.5f), out path), Is.True);
                Assert.That(path.Count, Is.EqualTo(2)); // No diagonal cutting through the missing side.
                Assert.That(f.View.TryHandleWorldClick(new Vector3(1.5f, 1.5f)), Is.False);
                f.Battle.TryGetUnit(100, out var unit); Assert.That(unit.X, Is.EqualTo(1)); Assert.That(unit.Y, Is.EqualTo(1));
                Assert.That(unit.Ap, Is.EqualTo(6)); Assert.That(f.Battle.ConfigureTraversalCatalogs(new[] { jobs }), Is.False);
                f.ActorTerrain.name = "unknown";
                Assert.That(f.View.TryGetMovementAtCell(100, water, out value), Is.False);
                Assert.That(f.View.TryGetMovementAtCell(100, new CombatGridCell(-1, 0), out value), Is.False);
                f.Ground.SetTile(new Vector3Int(1, 1, 0), null);
                Assert.That(f.View.TryGetMovementAtCell(100, floor, out value), Is.False);
                f.View.SuspendPresentation(); Assert.That(f.View.TryGetMovementAtCell(100, water, out value), Is.False);
                Assert.That(f.View.TryGetReachableMovementDestinations(out destinations), Is.False);
                Assert.That(destinations, Is.Empty);
            });
        }

        [Test]
        public void JobBindingRejectsAmbiguousSpecialOrMissingRowsInsteadOfAssumingInfantry()
        {
            WithMap(f => {
                var a = ScriptableObject.CreateInstance<JobCatalog>(); var b = ScriptableObject.CreateInstance<JobCatalog>();
                try {
                    long[] Row(int troop, long special) { var row = new long[34]; row[0] = 100; row[22] = troop; row[23] = special; return row; }
                    a.Initialize(new[] { new JobCatalogEntry(Row(1, 0), null, "fixture", null, null) });
                    b.Initialize(new[] { new JobCatalogEntry(Row(0, 0), null, "duplicate", null, null) });
                    f.Battle.ConfigurePlayerJob(100);
                    var input = new[] { a }; Assert.That(f.Battle.ConfigureTraversalCatalogs(input), Is.True); input[0] = b;
                    Assert.That(f.Battle.TryGetTerrainMovement(100, 6, out var value), Is.True); Assert.That(value, Is.EqualTo(1));
                    Assert.That(f.Battle.ConfigureTraversalCatalogs(new JobCatalog[] { a, null }), Is.False);
                    Assert.That(f.Battle.ConfigureTraversalCatalogs(new[] { a, a }), Is.False);
                    Assert.That(f.Battle.TryGetTroopColumn(100, out var troop), Is.True); Assert.That(troop, Is.EqualTo(1));
                    Assert.That(f.Battle.ConfigureTraversalCatalogs(new[] { a, b }), Is.True);
                    Assert.That(f.Battle.TryGetTroopColumn(100, out troop), Is.False);
                    f.Battle.ConfigureTraversalCatalogs(new[] { a });
                    a.Initialize(new[] { new JobCatalogEntry(Row(1, 1), null, "special", null, null) });
                    Assert.That(f.Battle.TryGetTroopColumn(100, out troop), Is.False);
                    a.Initialize(new[] { new JobCatalogEntry(Row(10, 0), null, "invalid", null, null) });
                    Assert.That(f.Battle.TryGetTroopColumn(100, out troop), Is.False);
                    Assert.That(f.Battle.TryGetTroopColumn(800, out troop), Is.False);
                    Assert.That(f.Battle.TryGetTroopColumn(701, out troop), Is.False); // Job ID is not a registered instance.
                } finally { UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b); }
            });
        }

        [Test]
        public void EmptyClickStartsOnePaidNativeRouteAndMovesBorrowedBodyRequiresReceivedRoute()
        {
            WithMap(f=>{Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var position=f.Ally.transform.position;int ap=f.Battle.PlayerAp;
                Assert.IsFalse(f.Battle.TryMoveTo(100,new CombatGridCell(0,0),out _));Assert.IsTrue(f.Battle.AdvanceMovement(1));
                Assert.AreEqual(position,f.Ally.transform.position);Assert.AreEqual(ap,f.Battle.PlayerAp);
                f.View.SuspendPresentation();Assert.AreEqual(position,f.Ally.transform.position);Assert.IsFalse(f.Battle.IsMovementInProgress(100));});
        }

        [Test]
        public void KnownTerrainPathCannotAuthorizeUnreceivedMovementOrApCharge()
        {
            WithMap(f=>{
                var jobs=AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
                Assert.IsTrue(f.Battle.ConfigureTraversalCatalogs(new[]{jobs}));Assert.IsTrue(f.Battle.ConfigurePlayerJob(100));
                Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                Assert.IsTrue(f.Battle.ConfigureMovementBudget(100,2));
                Assert.IsTrue(f.View.TryFindWorldMovementPath(new Vector3(.5f,1.5f),out var path));
                CollectionAssert.AreEqual(new[]{new CombatGridCell(0,2)},path);
                var before=f.Ally.transform.position;
                Assert.IsFalse(f.View.TryHandleWorldClick(new Vector3(.5f,1.5f)));
                Assert.IsTrue(f.Battle.AdvanceMovement(10));f.View.RefreshVisualState();
                Assert.AreEqual(before,f.Ally.transform.position);Assert.AreEqual(20,f.Battle.PlayerAp);
            });
        }

        [Test]
        public void OriginalDiagonalOffsetsKeepModelAtOriginAndSuspendRestoresItRequiresReceivedRoute()
        {
            WithMap(f=>{Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var position=f.Ally.transform.position;int ap=f.Battle.PlayerAp;
                Assert.IsFalse(f.Battle.TryMoveTo(100,new CombatGridCell(0,0),out _));Assert.IsTrue(f.Battle.AdvanceMovement(1));
                Assert.AreEqual(position,f.Ally.transform.position);Assert.AreEqual(ap,f.Battle.PlayerAp);
                f.View.SuspendPresentation();Assert.AreEqual(position,f.Ally.transform.position);Assert.IsFalse(f.Battle.IsMovementInProgress(100));});
        }

        [Test]
        public void HpZeroCorpseAndRemovalRestoreUnconfirmedDiagonalOffsetsWithoutAnotherTickRequiresReceivedRoute()
        {
            WithMap(f=>{Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var position=f.Ally.transform.position;int ap=f.Battle.PlayerAp;
                Assert.IsFalse(f.Battle.TryMoveTo(100,new CombatGridCell(0,0),out _));Assert.IsTrue(f.Battle.AdvanceMovement(1));
                Assert.AreEqual(position,f.Ally.transform.position);Assert.AreEqual(ap,f.Battle.PlayerAp);
                f.View.SuspendPresentation();Assert.AreEqual(position,f.Ally.transform.position);Assert.IsFalse(f.Battle.IsMovementInProgress(100));});
        }

        [Test]
        public void HpRecoveryStartsFreshMovementAfterOffsetRestoreWithoutRefundOrLateCommitRequiresReceivedRoute()
        {
            WithMap(f=>{Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var position=f.Ally.transform.position;int ap=f.Battle.PlayerAp;
                Assert.IsFalse(f.Battle.TryMoveTo(100,new CombatGridCell(0,0),out _));Assert.IsTrue(f.Battle.AdvanceMovement(1));
                Assert.AreEqual(position,f.Ally.transform.position);Assert.AreEqual(ap,f.Battle.PlayerAp);
                f.View.SuspendPresentation();Assert.AreEqual(position,f.Ally.transform.position);Assert.IsFalse(f.Battle.IsMovementInProgress(100));});
        }

        [Test]
        public void ConfirmedNativeStepSynchronizesBoundBodyButNeverTreatsClickAsMovement()
        {
            WithMap(f => {
                var physics = f.Ally.gameObject.AddComponent<Rigidbody2D>();
                var hiddenPhysics = f.Enemy.gameObject.AddComponent<Rigidbody2D>(); hiddenPhysics.simulated = false;
                Assert.That(physics.simulated, Is.True);
                var jobs = AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
                f.Battle.ConfigureTraversalCatalogs(new[] { jobs }); f.Battle.ConfigurePlayerJob(100);
                Assert.That(f.View.TryBind(f.Ground, f.Terrain, f.Camera, new[] {
                    new BattleMapVisual(100, f.Ally), new BattleMapVisual(800, f.Enemy) },
                    (a, b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1), Is.True);
                Assert.That(physics.simulated, Is.False);
                Assert.That(hiddenPhysics.simulated, Is.False);
                var from = new CombatGridCell(1, 1); var to = new CombatGridCell(1, 2);
                Assert.That(f.View.TryHandleWorldClick(new Vector3(1.5f, 1.5f)), Is.False);
                Assert.That(f.Ally.transform.position, Is.EqualTo(new Vector3(1.5f, 2.5f)));
                Assert.That(f.Battle.TryReserveMovementStep(100, to, out var ticket), Is.True);
                Assert.That(ticket.From, Is.EqualTo(from)); Assert.That(f.Battle.TryApplyReservedMovementStep(ticket), Is.True);
                Assert.That(f.Ally.transform.position, Is.EqualTo(new Vector3(1.5f, 1.5f)));
                Assert.That(f.View.TryWorldToCell(f.Ally.transform.position, out var cell), Is.True); Assert.That(cell, Is.EqualTo(to));
                Assert.That(f.View.TryHandleWorldClick(f.Ally.transform.position), Is.True);
                Assert.That(f.Battle.PlayerAp, Is.EqualTo(6)); Assert.That(f.Battle.PlayerHp, Is.EqualTo(120));
                f.View.SuspendPresentation(); var next = new CombatGridCell(2, 2);
                Assert.That(physics.simulated, Is.True);
                Assert.That(hiddenPhysics.simulated, Is.False);
                f.Battle.TryReserveMovementStep(100, next, out ticket);
                Assert.That(f.Battle.TryApplyReservedMovementStep(ticket), Is.True);
                Assert.That(f.Ally.transform.position, Is.EqualTo(new Vector3(1.5f, 1.5f))); // No subscribed view after suspend.
                Assert.That(f.Battle.ApplyDeath(100, true), Is.True);
                Assert.That(f.View.ResumePresentation(), Is.True);
                Assert.That(f.Ally.transform.position, Is.EqualTo(new Vector3(2.5f, 1.5f)));
                Assert.That(physics.simulated, Is.False);
                f.View.SuspendPresentation(); Assert.That(physics.simulated, Is.True);
            });
        }

        [Test]
        public void InformationSizeChangesKeepLiveTargetAndGameplayState()
        {
            WithMap(f => {
                int calls=0;f.Server.RequestReceived += _=>calls++;
                Assert.IsTrue(f.Bind()); Assert.IsTrue(f.Battle.TrySelectUnit(100)); f.Battle.ClearUnitInspection();
                Assert.IsFalse(f.Battle.TrySetUnitInformationCompact(true));
                f.Battle.OpenAlliedRoster();Assert.IsTrue(f.Battle.IsAlliedRosterVisible);
                Assert.IsFalse(f.Battle.TryInspectUnit(999));Assert.IsTrue(f.Battle.IsAlliedRosterVisible);
                Assert.IsTrue(f.Battle.TryInspectUnit(800));Assert.IsFalse(f.Battle.IsAlliedRosterVisible);
                Assert.AreEqual(default(Rect),f.Battle.AlliedRosterRect);
                var original=f.Battle.UnitInfoRect;int hp=f.Battle.PlayerHp,ap=f.Battle.PlayerAp;
                Assert.IsTrue(f.Battle.TrySetUnitInformationCompact(true));Assert.IsTrue(f.Battle.IsUnitInformationCompact);
                Assert.Less(f.Battle.UnitInfoRect.height,original.height);Assert.AreEqual(original.yMax,f.Battle.UnitInfoRect.yMax);
                Assert.IsFalse(f.Battle.TrySetUnitInformationCompact(true));
                Assert.IsTrue(f.Battle.TryGetInspectedUnit(out var before));Assert.AreEqual(800,before.Id);
                Assert.IsTrue(f.Battle.IsPointerOverBattleHud(Rendering.GameScreen.GuiToScreen(f.Battle.UnitInfoRect.center)));
                Assert.IsTrue(f.Battle.ApplyTickMenu(800,(short)(before.Ap+1),0));
                Assert.IsTrue(f.Battle.ApplyTickMenu(100,(short)(ap+1),0));
                Assert.IsTrue(f.Battle.TryGetInspectedUnit(out var after));
                Assert.AreEqual(before.Ap+1,after.Ap);Assert.AreEqual(100,f.Battle.SelectedUnitId);
                Assert.IsTrue(f.Battle.TrySetUnitInformationCompact(false));Assert.AreEqual(original,f.Battle.UnitInfoRect);
                Assert.AreEqual(hp,f.Battle.PlayerHp);Assert.AreEqual(ap+1,f.Battle.PlayerAp);Assert.AreEqual(0,calls);
                f.Battle.OpenAlliedRoster();Assert.IsTrue(f.Battle.IsAlliedRosterVisible);
                Assert.IsFalse(f.Battle.TryGetInspectedUnit(out _));Assert.IsFalse(f.Battle.IsUnitInformationCompact);
                Assert.IsTrue(f.Battle.TryInspectUnit(100));Assert.IsFalse(f.Battle.IsAlliedRosterVisible);
                f.Battle.ClearUnitInspection();Assert.IsFalse(f.Battle.IsUnitInformationCompact);
            });
        }

        [Test]
        public void UnitInspectionShowsIndependentStatsWithoutSelectingOrSending()
        {
            WithMap(f => {
                var calls = 0; f.Server.RequestReceived += _ => calls++;
                Assert.That(f.Bind(), Is.True); Assert.That(f.Battle.TrySelectUnit(100), Is.True);
                Assert.That(f.Battle.TryInspectUnit(800), Is.True);
                Assert.That(f.Battle.TryGetInspectedUnit(out var before), Is.True);
                Assert.That(before.Id, Is.EqualTo(800)); Assert.That(before.Attack, Is.EqualTo(18));
                Assert.That(before.Defense, Is.EqualTo(5)); Assert.That(f.Battle.SelectedUnitId, Is.EqualTo(100));
                Assert.That(f.View.GetUnitPortrait(800), Is.SameAs(f.Enemy.sprite));
                Assert.That(f.View.GetUnitPortrait(999), Is.Null);
                Assert.That(f.Battle.TryInspectUnit(999), Is.False);
                Assert.That(f.Battle.TryGetInspectedUnit(out var unchanged), Is.True); Assert.That(unchanged.Id, Is.EqualTo(800));
                Assert.That(calls, Is.Zero); Assert.That(f.Battle.PlayerAp, Is.EqualTo(6));
                Assert.That(f.Battle.ApplyTickMenu(800,7,0), Is.True);
                Assert.That(f.Battle.TryGetInspectedUnit(out var after), Is.True);
                Assert.That(after.Ap, Is.EqualTo(7)); Assert.That(before.Ap, Is.EqualTo(6));
                var point = f.Battle.UnitInfoRect.center;
                Assert.That(f.Battle.IsPointerOverBattleHud(Rendering.GameScreen.GuiToScreen(point)), Is.True);
                f.Battle.ClearUnitInspection(); Assert.That(f.Battle.TryGetInspectedUnit(out _), Is.False);
            });
        }

        [Test]
        public void UnitInspectionTracksMapClicksAndClearsOnDeathOrSuspend()
        {
            WithMap(f => {
                var calls = 0; f.Server.RequestReceived += _ => calls++;
                Assert.That(f.Bind(), Is.True);
                Assert.That(f.View.TryHandleWorldClick(f.Ally.transform.position), Is.True);
                Assert.That(f.Battle.TryGetInspectedUnit(out var selected), Is.True); Assert.That(selected.Id, Is.EqualTo(100));
                f.View.TryHandleWorldClick(f.Enemy.transform.position);
                Assert.That(f.Battle.TryGetInspectedUnit(out var enemy), Is.True); Assert.That(enemy.Id, Is.EqualTo(800));
                Assert.That(calls, Is.Zero); Assert.That(f.Battle.SelectedUnitId, Is.EqualTo(100));
                Assert.That(f.Battle.ApplyDeath(800, false), Is.True);
                Assert.That(f.Battle.TryGetInspectedUnit(out _), Is.False); Assert.That(f.Battle.TryInspectUnit(800), Is.False);
                Assert.That(f.Battle.IsBattleFinished, Is.False); // Removal is not a received battle-end notification.
                Assert.That(f.Battle.TrySelectUnit(100), Is.True);
                f.View.SuspendPresentation(); Assert.That(f.Battle.TryGetInspectedUnit(out _), Is.False);
                Assert.That(f.View.ResumePresentation(), Is.True); Assert.That(f.Battle.TryGetInspectedUnit(out _), Is.False);
            });
        }

        [Test]
        public void ActionMenuSeparatesConfirmedAttackSelectionFromUnresolvedMovement()
        {
            WithMap(f=>{Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                Assert.IsTrue(f.Battle.ConfigureActionPointCosts(2,6));Assert.IsTrue(f.View.TryOpenActionMenu());
                Assert.IsFalse(f.View.TryChooseCommand(BattleMapCommand.Move));Assert.IsTrue(f.View.TryChooseCommand(BattleMapCommand.Attack));
                Assert.AreEqual(20,f.Battle.PlayerAp);Assert.IsNull(f.Battle.LastAttackPresentation);Assert.IsFalse(f.Battle.IsMovementInProgress(100));});
        }

        [Test]
        public void ActionMenuCannotManufactureAnUnreceivedRestResult()
        {
            WithMap(f=>{Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));Assert.IsTrue(f.View.TryOpenActionMenu());
                var before=(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp);
                Assert.IsFalse(f.View.TryChooseCommand(BattleMapCommand.Rest));
                Assert.AreEqual(before,(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp));Assert.IsFalse(f.Battle.IsMovementInProgress(100));});
        }

        [Test]
        public void ActionMenuBlocksMapPointerAndSuspensionResetsCommands()
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100);Assert.IsTrue(f.Battle.ConfigureActionPointCosts(2,6));
                f.Camera.transform.position = new Vector3(2, 2, -10); f.Camera.orthographicSize = 3;
                Assert.That(f.View.TryOpenActionMenu(), Is.True);
                Assert.That(f.View.TryGetActionMenuRect(out var rect), Is.True);
                Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0)); Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0));
                Assert.That(f.Battle.IsPointerOverBattleHud(Rendering.GameScreen.GuiToScreen(rect.center)), Is.True);
                f.View.TryChooseCommand(BattleMapCommand.Attack); f.View.SuspendPresentation();
                Assert.That(f.View.IsActionMenuOpen, Is.False); Assert.That(f.View.ActiveCommand, Is.EqualTo(BattleMapCommand.Automatic));
                f.View.ResumePresentation(); Assert.That(f.View.IsActionMenuOpen, Is.False);
            });
        }

        [Test]
        public void CorpseAndHpPacketsRemainSeparateFromOutcomeAndRewardAuthority()
        {
            WithMap(f=>{Assert.IsTrue(f.Bind());uint before=f.Battle.PlayerExperience;
                Assert.IsTrue(f.Battle.ApplyDeath(800,true));Assert.AreEqual(75,f.Battle.EnemyHp);
                Assert.IsFalse(f.Battle.IsBattleFinished);Assert.IsFalse(f.Battle.ShowsEncounterOutcome);Assert.IsFalse(f.Battle.TryAcknowledgeOutcome());
                Assert.IsTrue(f.Battle.ApplyHp(800,0));Assert.AreEqual(before,f.Battle.PlayerExperience);Assert.IsFalse(f.Battle.IsBattleFinished);});
        }

        [Test]
        public void ExplicitMagicChoiceMenuPreservesNativeMetadataAndWaitsForTargetClick()
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100); f.Battle.ConfigureMagicApCost(701, 3); NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var values = new long[MagicCatalogEntry.NumericColumnCount]; values[0] = 701; values[7] = 8;
                var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                try {
                    catalog.Initialize(new[] { new MagicCatalogEntry(values, "Explicit magic") });
                    var command = new MagicRequest(701, 100, 0, 0, -123, Array.Empty<short>(), 0x92, 7);
                    var input = new[] { command }; MagicRequest received = null;
                    f.Server.RequestReceived += packet => received = packet as MagicRequest;
                    Assert.That(f.View.ConfigureMagicChoices(catalog, 100, input, id => id == 800, null, null, null), Is.True);
                    input[0] = null; Assert.That(f.View.MagicChoiceCount, Is.EqualTo(1));
                    f.View.TryOpenActionMenu(); Assert.That(f.View.TryChooseCommand(BattleMapCommand.Magic), Is.True);
                    Assert.That(f.View.IsMagicChoiceOpen, Is.True);
                    Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.False);
                    Assert.That(f.View.TrySelectMagicChoice(1), Is.False);
                    Assert.That(f.View.TrySelectMagicChoice(0), Is.True);
                    Assert.That(f.View.IsMagicChoiceOpen, Is.False); Assert.That(f.View.IsMagicTargeting, Is.True);
                    Assert.That(received, Is.Null); Assert.That(f.Battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp));
                    Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.True);
                    Assert.That(received.TargetUnitIds, Is.EqualTo(new short[] { 800 }));
                    Assert.That(received.ComputedCost, Is.EqualTo(-123)); Assert.That(received.Mode, Is.EqualTo(7));
                    Assert.That(received.OriginalOpcode, Is.EqualTo(0x92)); Assert.That(f.Battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 3));
                    Assert.That(f.Battle.PlayerMp, Is.EqualTo(30)); Assert.That(f.Battle.EnemyHp, Is.EqualTo(75));
                } finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [Test]
        public void MagicChoicesRejectInvalidReplacementAndCancelWithoutInventingAvailability()
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100);
                var values = new long[MagicCatalogEntry.NumericColumnCount]; values[0] = 701; values[7] = 8;
                var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                try {
                    catalog.Initialize(new[] { new MagicCatalogEntry(values, "Explicit magic") });
                    var command = new MagicRequest(701, 100, 0, 0, 0, Array.Empty<short>());
                    Assert.That(f.View.ConfigureMagicChoices(catalog, 100, new[] { command }, _ => true, null, null, null), Is.True);
                    Assert.That(f.View.ConfigureMagicChoices(catalog, 100, new[] { command, command }, _ => true, null, null, null), Is.False);
                    Assert.That(f.View.MagicChoiceCount, Is.EqualTo(1));
                    f.View.TryOpenActionMenu(); f.View.TryChooseCommand(BattleMapCommand.Magic);
                    f.View.CancelMagicTargeting(); Assert.That(f.View.IsMagicChoiceOpen, Is.False);
                    Assert.That(f.View.TrySelectMagicChoice(0), Is.False);
                    f.View.TryOpenActionMenu(); f.View.TryChooseCommand(BattleMapCommand.Magic);
                    f.View.SuspendPresentation(); f.View.ResumePresentation(); Assert.That(f.View.IsMagicChoiceOpen, Is.False);
                    Assert.That(f.View.MagicChoiceCount, Is.EqualTo(1));
                    Assert.That(f.View.ConfigureMagicChoices(catalog, 100, Array.Empty<MagicRequest>(), _ => true, null, null, null), Is.True);
                    Assert.That(f.View.MagicChoiceCount, Is.Zero);
                    Assert.That(f.Battle.PlayerAp, Is.EqualTo(6)); Assert.That(f.Battle.PlayerMp, Is.EqualTo(30));
                } finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [Test]
        public void MagicDirectionPreviewAndOptionalDiagnosticsDoNotSpendWhenApIsNotReady()
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100); var calls = 0; f.Server.RequestReceived += _ => calls++;
                var values = new long[MagicCatalogEntry.NumericColumnCount]; values[0] = 701; values[2] = -1; values[3] = 2; values[6] = 1; values[7] = 1;
                var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                try {
                    catalog.Initialize(new[] { new MagicCatalogEntry(values, "Explicit direction") });
                    Assert.That(f.View.TryBeginNativeMagicTargeting(catalog, new MagicRequest(701, 100, 0, 0, 0, Array.Empty<short>()),
                        _ => true, _ => 1, null, null), Is.True);
                    Assert.That(f.View.RequiresMagicDirection, Is.True); Assert.That(f.View.TryConfirmMagicDirection(), Is.False);
                    var directions = new[] { BattleMagicDirection.Up, BattleMagicDirection.Down, BattleMagicDirection.Left, BattleMagicDirection.Right };
                    var sentinels = new[] { -3, -4, -2, -1 }; var counts = new[] { 1, 2, 1, 2 };
                    for (var i = 0; i < 4; i++) {
                        Assert.That(f.View.TryChooseMagicDirection(directions[i]), Is.True);
                        Assert.That(f.View.SelectedMagicDirectionCommand.Value, Is.EqualTo(new CombatGridCell(sentinels[i], 0)));
                        Assert.That(f.View.MagicPreviewCellCount, Is.EqualTo(counts[i]));
                    }
                    Assert.That(f.View.TryChooseMagicDirection((BattleMagicDirection)999), Is.False);
                    Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.False);
                    Assert.That(calls, Is.Zero); Assert.That(f.Battle.PlayerAp, Is.EqualTo(6)); Assert.That(f.Battle.PlayerMp, Is.EqualTo(30));
                    Assert.That(f.View.MagicDirectionRect, Is.EqualTo(default(Rect)));
                    f.Battle.SetPrototypeHudVisible(true);
                    Assert.That(f.Battle.IsPrototypeHudVisible,Is.False);
                    Assert.That(f.View.MagicDirectionRect,Is.EqualTo(default(Rect)));
                    f.View.CancelMagicTargeting(); Assert.That(f.View.SelectedMagicDirectionCommand, Is.Null);
                    Assert.That(f.View.RequiresMagicDirection, Is.False); Assert.That(f.View.TryConfirmMagicDirection(), Is.False);
                } finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MagicDirectionConfirmsNativePacketOnceAndSuspendDropsSelection(bool clickFloorHole)
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100); f.Battle.ConfigureMagicApCost(701, 3); NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var values = new long[MagicCatalogEntry.NumericColumnCount]; values[0] = 701; values[2] = -1; values[3] = 2; values[6] = 1; values[7] = 1;
                var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                try {
                    catalog.Initialize(new[] { new MagicCatalogEntry(values, "Explicit direction") });
                    var command = new MagicRequest(701, 100, 0, 0, -123, Array.Empty<short>(), 0x92, 7);
                    MagicRequest received = null; var calls = 0; f.Server.RequestReceived += packet => { received = packet as MagicRequest; calls++; };
                    f.View.TryBeginNativeMagicTargeting(catalog, command, _ => true, _ => 1, null, null);
                    Assert.That(f.View.TryPreviewMagicDirectionAt(f.Enemy.transform.position), Is.True);
                    Assert.That(f.Battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp)); Assert.That(received, Is.Null);
                    var click = clickFloorHole ? new Vector3(3.5f, 2.5f) : new Vector3(10, 2.5f);
                    if (clickFloorHole) f.Ground.SetTile(new Vector3Int(3, 2, 0), null);
                    Assert.That(f.View.TryWorldToCell(click, out _), Is.False);
                    Assert.That(f.View.TryHandleWorldClick(click), Is.True);
                    Assert.That(received.X, Is.EqualTo(-1)); Assert.That(received.Y, Is.Zero);
                    Assert.That(received.TargetUnitIds, Is.EqualTo(new short[] { 800 }));
                    Assert.That(received.ComputedCost, Is.EqualTo(-123)); Assert.That(received.OriginalOpcode, Is.EqualTo(0x92));
                    Assert.That(received.Mode, Is.EqualTo(7)); Assert.That(f.Battle.PlayerAp, Is.EqualTo(GameConstants.BattleMaximumAp - 3));
                    Assert.That(f.Battle.PlayerMp, Is.EqualTo(30)); Assert.That(f.Battle.EnemyHp, Is.EqualTo(75));
                    Assert.That(f.View.SelectedMagicDirectionCommand, Is.Null); Assert.That(f.View.TryConfirmMagicDirection(), Is.False);
                    Assert.That(calls, Is.EqualTo(1));
                    f.View.TryBeginNativeMagicTargeting(catalog, command, _ => true, _ => 1, null, null);
                    f.View.TryChooseMagicDirection(BattleMagicDirection.Down); f.View.SuspendPresentation(); f.View.ResumePresentation();
                    Assert.That(f.View.SelectedMagicDirectionCommand, Is.Null); Assert.That(f.View.TryConfirmMagicDirection(), Is.False);
                } finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [TestCase(2)]
        [TestCase(-1)]
        public void RepeatedDirectionPreviewReusesGeometryAndClearsOnCancelOrFinish(int area)
        {
            WithMap(f => {
                f.Bind(); f.Battle.TrySelectUnit(100);
                var values = new long[MagicCatalogEntry.NumericColumnCount];
                values[0] = 701; values[2] = -1; values[3] = area; values[6] = 1; values[7] = 1;
                var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                try {
                    catalog.Initialize(new[] { new MagicCatalogEntry(values, "Preview lifecycle") });
                    var command = new MagicRequest(701, 100, 0, 0, 0, Array.Empty<short>());
                    Assert.That(f.View.TryBeginNativeMagicTargeting(catalog, command, _ => true, _ => 1, null, null), Is.True);
                    for (int i = 0; i < 100; i++)
                        Assert.That(f.View.TryChooseMagicDirection(i % 2 == 0 ? BattleMagicDirection.Left : BattleMagicDirection.Right), Is.True);
                    var preview = f.Ground.transform.Find("MagicGeometryPreview");
                    if (area >= 0) {
                        Assert.That(preview, Is.Not.Null);
                        var mesh = preview.GetComponent<MeshFilter>().sharedMesh;
                        Assert.That(mesh.vertexCount, Is.EqualTo(f.View.MagicPreviewCellCount * 4));
                        Assert.That(mesh.triangles.Length, Is.EqualTo(f.View.MagicPreviewCellCount * 6));
                        f.View.TryChooseMagicDirection(BattleMagicDirection.Left);
                        Assert.That(preview.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
                    } else {
                        Assert.That(preview, Is.Null); Assert.That(f.View.MagicPreviewCellCount, Is.Zero);
                        var cached = typeof(BattleMapPresentation).GetField("previewCommand",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        Assert.That(cached.GetValue(f.View), Is.EqualTo(new CombatGridCell(-1, 0)));
                    }
                    Assert.That(f.View.TryHandleCancelInput(false), Is.True);
                    Assert.That(f.View.MagicPreviewCellCount, Is.Zero); Assert.That(f.View.SelectedMagicDirectionCommand, Is.Null);
                    Assert.That(f.View.TryBeginNativeMagicTargeting(catalog, command, _ => true, _ => 1, null, null), Is.True);
                    f.View.TryChooseMagicDirection(BattleMagicDirection.Right);
                    f.View.CancelMagicTargeting(); // Explicit client cancellation; corpse alone cannot imply encounter completion.
                    f.Battle.ApplyDeath(800, true); f.View.RefreshVisualState();
                    Assert.That(f.Battle.IsBattleFinished, Is.False);
                    Assert.That(f.View.MagicPreviewCellCount, Is.Zero); Assert.That(f.View.IsMagicTargeting, Is.False);
                    Assert.That(f.View.TryConfirmMagicDirection(), Is.False);
                    Assert.That(f.Battle.PlayerAp, Is.EqualTo(6)); Assert.That(f.Battle.PlayerMp, Is.EqualTo(30));
                } finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [Test]
        public void LearnedAreaDoesNotEnableRemovedDamageSimulator()
        {
            WithMap(f=>{
                Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var before=(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp);
                f.Battle.SetTemporaryLocalRules(true);Assert.IsFalse(f.Battle.TemporaryLocalRulesEnabled);
                Assert.IsFalse(f.Battle.ConfigureTemporaryDamageMagic(701,7,6,10,3));
                int sent=0;f.Server.RequestReceived+=_=>sent++;
                f.View.TryHandleWorldClick(f.Enemy.transform.position);
                Assert.AreEqual(before,(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp));
                Assert.AreEqual(0,sent);Assert.IsNull(f.Battle.LastAttackPresentation);
            });
        }

        [Test]
        public void DirectionSelectionDoesNotEnableUnresolvedLocalCast()
        {
            WithMap(f=>{
                Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var before=(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp);
                f.Battle.SetTemporaryLocalRules(true);Assert.IsFalse(f.Battle.TemporaryLocalRulesEnabled);
                Assert.IsFalse(f.Battle.ConfigureTemporaryDamageMagic(201,7,6,10,3));
                int sent=0;f.Server.RequestReceived+=_=>sent++;
                f.View.TryHandleWorldClick(f.Enemy.transform.position);
                Assert.AreEqual(before,(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp));
                Assert.AreEqual(0,sent);Assert.IsNull(f.Battle.LastAttackPresentation);
            });
        }

        [Test]
        public void MissingGroupMetadataNeverInventsLocalEffect()
        {
            WithMap(f=>{
                Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var before=(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp);
                f.Battle.SetTemporaryLocalRules(true);Assert.IsFalse(f.Battle.TemporaryLocalRulesEnabled);
                Assert.IsFalse(f.Battle.ConfigureTemporaryDamageMagic(701,7,6,10,3));
                int sent=0;f.Server.RequestReceived+=_=>sent++;
                f.View.TryHandleWorldClick(f.Enemy.transform.position);
                Assert.AreEqual(before,(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp));
                Assert.AreEqual(0,sent);Assert.IsNull(f.Battle.LastAttackPresentation);
            });
        }

        private static void ConfigureLearnedFixture(Fixture f, MagicCatalog catalog, short id)
        {
            Assert.That(f.Battle.ConfigurePlayerJob(100), Is.True);
            var member = new BattleFormationMemberDraft { PersistentId = "area-fixture", CommanderPersistentId = "area-fixture", CommanderId = 100,
                Unit = NativeCombatStateFixture.Received(new CombatUnit { Id = 100, JobId = 100, Name = "Learned area fixture", X = 1, Y = 1,
                    Hp = 120, MaxHp = 120, Mp = 30, MaxMp = 30 }),
                IsOwned = true, FinalStateConfirmed = true, Position = new CombatGridCell(1, 1), TeamId = 1,
                RecordEligible = true, LearnedMagicIds = new[] { id } };
            Assert.That(BattleFormation.TryCreateSnapshot(new[] { member }, 4, 4, Array.Empty<short>(), out var formation, out var reason), Is.True, reason);
            Assert.That(f.View.ConfigureFormationLearnedMagic(catalog, formation), Is.True);
        }

        [Test]
        public void NativeAreaGeometryDoesNotAuthorizeLocalDamage()
            => RenderTemporaryMultiTargetAreaCheck(null);

        public void RenderTemporaryMultiTargetAreaCheck(string outputPath)
        {
            WithMap(f=>{
                Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var before=(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp);
                f.Battle.SetTemporaryLocalRules(true);Assert.IsFalse(f.Battle.TemporaryLocalRulesEnabled);
                Assert.IsFalse(f.Battle.ConfigureTemporaryDamageMagic(701,7,6,10,3));
                int sent=0;f.Server.RequestReceived+=_=>sent++;
                f.View.TryHandleWorldClick(f.Enemy.transform.position);
                Assert.AreEqual(before,(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp));
                Assert.AreEqual(0,sent);Assert.IsNull(f.Battle.LastAttackPresentation);
            });
        }

        [Test]
        public void CoordinateMagicMenuKeepsPacketAnchorAndCleansEffectsOnSuspend()
        {
            foreach (short magicId in new short[] { 901, 1001 })
                WithMap(f => {
                    Assert.That(f.Bind(), Is.True); f.Battle.TrySelectUnit(100);
                    f.Battle.ConfigureMagicApCost(magicId, 3); // Fixture-only cost and availability.
                    NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                    var data = new long[MagicCatalogEntry.NumericColumnCount];
                    data[0] = magicId; data[2] = 3; data[3] = 1; data[6] = 1; data[7] = 1;
                    var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                    try {
                        catalog.Initialize(new[] { new MagicCatalogEntry(data, "Coordinate integration fixture") });
                        var command = new MagicRequest(magicId, 100, 0, 0, -123, Array.Empty<short>());
                        Assert.That(f.View.ConfigureMagicChoices(catalog, 100, new[] { command }, id => id == 800, _ => 1, null, null), Is.True);
                        MagicRequest received = null; var calls = 0;
                        f.Server.RequestReceived += p => { received = p as MagicRequest; calls++; };
                        Assert.That(f.View.TryOpenActionMenu(), Is.True);
                        Assert.That(f.View.TryChooseCommand(BattleMapCommand.Magic), Is.True);
                        Assert.That(f.View.TrySelectMagicChoice(0), Is.True);
                        Assert.That(f.View.TryHandleWorldClick(f.Enemy.transform.position), Is.True);
                        Assert.That((received.X, received.Y), Is.EqualTo(((short)2, (short)1)));
                        Assert.That(received.TargetUnitIds, Is.EqualTo(new short[] { 800 }));
                        Assert.That(f.View.ActiveMagicEffectCount, Is.Zero);
                        var vitals = (f.Battle.PlayerHp, f.Battle.PlayerMp, f.Battle.EnemyHp, f.Battle.PlayerAp);
                        Assert.That(f.Server.SendToClient(new MagicEffectNotification(magicId, 100, received.X, received.Y, 0,
                            new System.Collections.Generic.List<short>(received.TargetUnitIds).ToArray())), Is.True);
                        bool visible = false;
                        for (int step = 0; step < 8 && !visible; step++) {
                            f.View.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);
                            foreach (var body in f.View.GetComponentsInChildren<SpriteRenderer>())
                                if (body.name.StartsWith("Native Coordinate Area ") && body.enabled && body.sprite != null) visible = true;
                        }
                        Assert.That(visible, Is.True, "Coordinate effect " + magicId);
                        Assert.That((f.Battle.PlayerHp, f.Battle.PlayerMp, f.Battle.EnemyHp, f.Battle.PlayerAp), Is.EqualTo(vitals));
                        Assert.That(calls, Is.EqualTo(1));
                        f.View.SuspendPresentation(); f.View.ResumePresentation();
                        Assert.That(f.View.ActiveMagicEffectCount, Is.Zero);
                        foreach (var body in f.View.GetComponentsInChildren<SpriteRenderer>())
                            if (body.name.StartsWith("Native Coordinate Area ")) Assert.That(body.enabled, Is.False);
                    } finally { UnityEngine.Object.DestroyImmediate(catalog); }
                });
        }

        [TestCase(901)]
        [TestCase(1001)]
        public void CoordinateEffectsReleaseExpiredCellsAndRecreateAfterSuspend(int magicId)
        {
            WithMap(f => {
                f.Bind();
                int CoordinateBodies() {
                    int count = 0;
                    foreach (var body in f.View.GetComponentsInChildren<SpriteRenderer>())
                        if (body.name.StartsWith("Native Coordinate Area ")) count++;
                    return count;
                }
                for (int cast = 0; cast < 64; cast++) {
                    Assert.That(f.Server.SendToClient(new MagicEffectNotification((short)magicId, 100,
                        (short)(cast % 4), (short)((cast / 4) % 4), 0, new short[] { 800, 800 })), Is.True);
                    Assert.That(f.View.RetainedCoordinateAreaCount, Is.EqualTo(1));
                    Assert.That(CoordinateBodies(), Is.EqualTo(magicId == 1001 ? 7 : 1));
                    f.View.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 1000);
                    Assert.That(f.View.RetainedCoordinateAreaCount, Is.Zero);
                    Assert.That(CoordinateBodies(), Is.Zero);
                }
                f.Server.SendToClient(new MagicEffectNotification((short)magicId, 100, 2, 1, 0, new short[] { 800 }));
                f.View.SuspendPresentation();
                Assert.That(f.View.RetainedCoordinateAreaCount, Is.Zero); Assert.That(CoordinateBodies(), Is.Zero);
                f.View.ResumePresentation();
                f.Server.SendToClient(new MagicEffectNotification((short)magicId, 100, 2, 1, 0, new short[] { 800 }));
                Assert.That(f.View.RetainedCoordinateAreaCount, Is.EqualTo(1));
                f.Battle.ApplyDeath(800, false); f.View.AdvanceMagicEffects(0);
                Assert.That(f.View.RetainedCoordinateAreaCount, Is.Zero); Assert.That(CoordinateBodies(), Is.Zero);
                Assert.That(f.Battle.PlayerMp, Is.EqualTo(30)); Assert.That(f.Battle.PlayerAp, Is.EqualTo(6));
            });
        }

        [TestCase(-1)]
        [TestCase(-2)]
        [TestCase(-3)]
        [TestCase(-4)]
        public void PlasmaDirectionAloneCannotCreateAnUnreceivedEffect(int direction)
        {
            WithMap(f=>{
                Assert.IsTrue(f.Bind());Assert.IsTrue(f.Battle.TrySelectUnit(100));NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var before=(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp);
                f.Battle.SetTemporaryLocalRules(true);Assert.IsFalse(f.Battle.TemporaryLocalRulesEnabled);
                Assert.IsFalse(f.Battle.ConfigureTemporaryDamageMagic(10505,7,6,10,3));
                int sent=0;f.Server.RequestReceived+=_=>sent++;
                f.View.TryHandleWorldClick(f.Enemy.transform.position);
                Assert.AreEqual(before,(f.Battle.PlayerHp,f.Battle.PlayerMp,f.Battle.PlayerAp,f.Battle.EnemyHp));
                Assert.AreEqual(0,sent);Assert.IsNull(f.Battle.LastAttackPresentation);
            });
        }

        [Test]
        public void PlasmaMenuRequestWaitsForIndependentEffectWithoutDoubleSpending()
        {
            WithMap(f => {
                Assert.That(f.Bind(), Is.True); f.Battle.TrySelectUnit(100);
                f.Battle.ConfigureMagicApCost(10505, 3); // Fixture cost only, not an original-data claim.
                NativeCombatStateFixture.ReceiveFullTicks(f.Battle);
                var values = new long[MagicCatalogEntry.NumericColumnCount];
                values[0] = 10505; values[2] = -1; values[3] = 2; values[6] = 1; values[7] = 1;
                var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
                try {
                    catalog.Initialize(new[] { new MagicCatalogEntry(values, "Explicit integration fixture") });
                    var command = new MagicRequest(10505, 100, 0, 0, -123, Array.Empty<short>(), 0x92, 7);
                    Assert.That(f.View.ConfigureMagicChoices(catalog, 100, new[] { command }, _ => true, _ => 1, null, null), Is.True);
                    MagicRequest received = null; var calls = 0;
                    f.Server.RequestReceived += p => { received = p as MagicRequest; calls++; };
                    Assert.That(f.View.TryOpenActionMenu(), Is.True);
                    Assert.That(f.View.TryChooseCommand(BattleMapCommand.Magic), Is.True);
                    Assert.That(f.View.TrySelectMagicChoice(0), Is.True);
                    Assert.That(f.View.TryChooseMagicDirection(BattleMagicDirection.Right), Is.True);
                    Assert.That(f.View.TryConfirmMagicDirection(), Is.True);
                    Assert.That(received.X, Is.EqualTo(-1));
                    Assert.That(received.TargetUnitIds, Is.EqualTo(new short[] { 800 }));
                    Assert.That(received.ComputedCost, Is.EqualTo(-123));
                    Assert.That(f.View.ActiveMagicEffectCount, Is.Zero);
                    var vitals = (f.Battle.PlayerHp, f.Battle.PlayerMp, f.Battle.EnemyHp, f.Battle.PlayerAp);
                    Assert.That(vitals.Item4, Is.EqualTo(GameConstants.BattleMaximumAp - 3));
                    Assert.That(f.Server.SendToClient(new MagicEffectNotification(received.MagicId,
                        received.CasterUnitId, received.X, received.Y, 0,
                        new System.Collections.Generic.List<short>(received.TargetUnitIds).ToArray())), Is.True);
                    Assert.That(f.View.ActiveMagicEffectCount, Is.GreaterThan(0));
                    Assert.That((f.Battle.PlayerHp, f.Battle.PlayerMp, f.Battle.EnemyHp, f.Battle.PlayerAp), Is.EqualTo(vitals));
                    Assert.That(f.View.TryConfirmMagicDirection(), Is.False);
                    Assert.That(calls, Is.EqualTo(1));
                    f.View.SuspendPresentation(); Assert.That(f.View.ActiveMagicEffectCount, Is.Zero);
                } finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        [Test]
        public void PreparedBodiesBindTwoGroupsWithIndependentSelectionAndImmediateRemoval()
        {
            WithPreparedFormation((f, formation, art) => {
                Assert.That(f.View.TryBindPreparedFormation(formation, f.Ground, f.Terrain, f.Camera,
                    new[] { new BattleMapVisual(100, f.Ally), new BattleMapVisual(800, f.Enemy) }, art), Is.True);
                Assert.That(f.View.CreatedFormationBodyCount, Is.EqualTo(3));
                Assert.That(f.Battle.TrySelectUnit(209), Is.True);
                Assert.That(f.Battle.TryFindSelectedMovementPath(new CombatGridCell(2, 0), out var path), Is.True);
                Assert.That(path.Count, Is.GreaterThan(0));
                Assert.That(f.Battle.TrySelectUnit(300), Is.True);
                var node = f.View.transform.Find("Formation unit 209"); Assert.That(node, Is.Not.Null);
                Assert.That(node.GetComponent<TopDownPlayerController>(), Is.Null);
                Assert.That(node.GetComponent<DirectionalSpriteAnimator>().HasCompleteDirections, Is.True);
                Assert.That(f.Battle.ApplyDeath(209, false), Is.True); f.View.RefreshVisualState();
                Assert.That(node.GetComponent<SpriteRenderer>().enabled, Is.False);
                Assert.That(f.Battle.TryGetGridOccupant(new CombatGridCell(1, 0), out _), Is.False);
                Assert.That(f.Battle.TryCaptureFormationReturn(formation, out var returned, out _), Is.True);
                Assert.That(returned.Members[1].Unit.LifeState, Is.EqualTo(UnitLifeState.Removed));
                Assert.That(formation.Members[1].Unit.LifeState, Is.EqualTo(UnitLifeState.Active));
                Assert.That(f.Ally.GetComponent<DirectionalSpriteAnimator>().IsMoving, Is.False);
            });
        }

        [Test]
        public void PreparedBodyBindingFailureRemovesOnlyNewBodiesAndCanRetry()
        {
            WithPreparedFormation((f, formation, art) => {
                Assert.That(f.View.TryBindPreparedFormation(formation, f.Ground, f.Terrain, f.Camera,
                    new[] { new BattleMapVisual(100, f.Ally), new BattleMapVisual(800, null) }, art), Is.False);
                Assert.That(f.View.CreatedFormationBodyCount, Is.Zero); Assert.That(f.View.transform.childCount, Is.Zero);
                Assert.That(f.Battle.GridConfigured, Is.False); Assert.That(f.Ally.enabled, Is.True);
                Assert.That(f.View.TryBindPreparedFormation(formation, f.Ground, f.Terrain, f.Camera,
                    new[] { new BattleMapVisual(100, f.Ally), new BattleMapVisual(800, f.Enemy) },
                    new System.Collections.Generic.Dictionary<int, DirectionalSpriteAnimator>()), Is.False);
                Assert.That(f.View.TryBindPreparedFormation(formation, f.Ground, f.Terrain, f.Camera,
                    new[] { new BattleMapVisual(100, f.Ally), new BattleMapVisual(800, f.Enemy) }, art), Is.True);
                var generated = f.View.transform.Find("Formation unit 200").gameObject;
                // Non-ExecuteAlways preview components have no runtime Awake/OnDestroy lifecycle.
                // Exercise explicit release here; automatic lifecycle is checked in Play separately.
                f.View.ReleaseCreatedFormationBodies();
                UnityEngine.Object.DestroyImmediate(f.View); Assert.That(generated == null, Is.True);
                Assert.That(f.Ally != null && f.Enemy != null, Is.True);
            });
        }

        [Test]
        public void PrimarySoldierAliasCreatesSelectableNativeBodyAndReturnsItsSourceIdentity()
        {
            WithMap(f => {
                var jobs = AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
                var mercenaries = AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/MercenaryJobCatalog.asset");
                Assert.That(f.Battle.ConfigureTraversalCatalogs(new[] {jobs, mercenaries}), Is.True);
                var member = new BattleFormationMemberDraft {
                    PersistentId = "hired-test", OwnedInstanceId = 20000, CommanderId = 100,
                    CommanderPersistentId = BattleFormation.PrimaryCommanderPersistentId,
                    Unit = NativeCombatStateFixture.Received(new CombatUnit {Id = 101, JobId = 200, Name = "soldier", X = 0, Y = 0, Hp = 140, MaxHp = 140, Attack = 80, Defense = 71}),
                    IsOwned = true, FinalStateConfirmed = true, Position = new CombatGridCell(0, 0), TeamId = 1,
                    RecordEligible = false, LearnedMagicIds = Array.Empty<short>() };
                Assert.That(BattleFormation.TryCreateSnapshot(new[] {member}, 4, 4, new short[] {100, 800}, out var formation, out var reason, true), Is.True, reason);
                Assert.That(f.Battle.TryRegisterAlliedFormation(formation, out reason), Is.True, reason);
                var template = f.Ally.gameObject.AddComponent<DirectionalSpriteAnimator>();
                DarkSaver.Editor.MercenarySpriteConverter.ConfigureTemplate(200, template);
                Assert.That(f.View.TryBindPreparedFormation(formation, f.Ground, f.Terrain, f.Camera,
                    new[] {new BattleMapVisual(100, f.Ally), new BattleMapVisual(800, f.Enemy)},
                    new System.Collections.Generic.Dictionary<int, DirectionalSpriteAnimator> {[200] = template}), Is.True);
                Assert.That(f.View.CreatedFormationBodyCount, Is.EqualTo(1));
                Assert.That(f.Battle.TrySelectUnit(101), Is.True);
                Assert.That(f.Battle.TryFindSelectedMovementPath(new CombatGridCell(1, 0), out _), Is.True);
                var body = f.View.transform.Find("Formation unit 101");
                Assert.That(body.GetComponent<DirectionalSpriteAnimator>().HasCompleteDirections, Is.True);
                Assert.That(body.GetComponent<SpriteRenderer>().sprite.name, Does.StartWith("Sheet200_"));
                Assert.That(f.Battle.TryCaptureFormationReturn(formation, out var returned, out reason), Is.True, reason);
                Assert.That(returned.Members[0].OwnedInstanceId, Is.EqualTo(20000));
            });
        }

        private static void WithPreparedFormation(Action<Fixture, BattleFormationSnapshot,
            System.Collections.Generic.IReadOnlyDictionary<int, DirectionalSpriteAnimator>> check)
        {
            WithMap(f => {
                var jobs = ScriptableObject.CreateInstance<JobCatalog>();
                try {
                    var values = new long[JobCatalogEntry.NumericColumnCount]; values[0] = 100; values[22] = 1;
                    jobs.Initialize(new[] { new JobCatalogEntry(values, "", "fixture", "", "") });
                    Assert.That(f.Battle.ConfigureTraversalCatalogs(new[] { jobs }), Is.True);
                    var ids = new short[] { 200, 209, 300 }; var draft = new System.Collections.Generic.List<BattleFormationMemberDraft>();
                    for (var i = 0; i < ids.Length; i++) { var id = ids[i]; var leader = (short)(id / 10 * 10); var x = i == 1 ? 1 : 0; var y = i == 2 ? 1 : 0;
                        draft.Add(new BattleFormationMemberDraft { PersistentId = id.ToString(), CommanderId = leader, CommanderPersistentId = leader.ToString(),
                            Unit = NativeCombatStateFixture.Received(new CombatUnit { Id = id, JobId = 100, Name = "fixture", X = x, Y = y, Hp = 40, MaxHp = 50, Mp = 2, MaxMp = 10, Attack = 0, Defense = 0 }),
                            IsOwned = true, FinalStateConfirmed = true, Position = new CombatGridCell(x, y), TeamId = 1, RecordEligible = true, LearnedMagicIds = Array.Empty<short>() });
                    }
                    Assert.That(BattleFormation.TryCreateSnapshot(draft, 4, 4, new short[] { 100, 800 }, out var formation, out _), Is.True);
                    Assert.That(f.Battle.TryRegisterAlliedFormation(formation, out _), Is.True);
                    var animator = f.Ally.gameObject.AddComponent<DirectionalSpriteAnimator>(); var frames = new[] { f.Ally.sprite };
                    animator.Configure(frames, frames, frames, frames);
                    check(f, formation, new System.Collections.Generic.Dictionary<int, DirectionalSpriteAnimator> { [100] = animator });
                } finally { UnityEngine.Object.DestroyImmediate(jobs); }
            });
        }

        private static void AssertNativeFrame(Sprite shown,Sprite source)
        {
            Assert.IsNotNull(shown);Assert.AreSame(source.texture,shown.texture);
            Assert.AreEqual(source.rect,shown.rect);Assert.AreEqual(source.pixelsPerUnit,shown.pixelsPerUnit);
            Assert.That(shown.bounds.center.sqrMagnitude,Is.LessThan(.00001f));
        }

        private sealed class Fixture
        {
            public BattleMapPresentation View; public BattleSceneRoot Battle; public ServerManager Server;
            public BattlePacketReceiver Receiver; public Tilemap Ground, Terrain;
            public Camera Camera; public SpriteRenderer Ally, Enemy; public Tile ActorTerrain;
            public bool Bind() => View.TryBind(Ground, Terrain, Camera, new[] { new BattleMapVisual(100, Ally), new BattleMapVisual(800, Enemy) });
        }

        private static void WithMap(Action<Fixture> check)
        {
            var scene = EditorSceneManager.NewPreviewScene(); var tiles = new System.Collections.Generic.List<Tile>();
            try {
                GameObject Create(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); return go; }
                var floor = ScriptableObject.CreateInstance<Tile>(); tiles.Add(floor);
                var grid = Create("Grid").AddComponent<Grid>();
                var ground = Create("Ground").AddComponent<Tilemap>(); ground.transform.SetParent(grid.transform, false);
                for (var x = 0; x < 4; x++) for (var y = 0; y < 4; y++) ground.SetTile(new Vector3Int(x, y, 0), floor);
                var terrain = Create("TerrainAttributes").AddComponent<Tilemap>(); terrain.transform.SetParent(grid.transform, false);
                var actorTerrain = ScriptableObject.CreateInstance<Tile>(); actorTerrain.name = "Terrain_06"; tiles.Add(actorTerrain);
                var enemyTerrain = ScriptableObject.CreateInstance<Tile>(); enemyTerrain.name = "Terrain_09"; tiles.Add(enemyTerrain);
                terrain.SetTile(new Vector3Int(1, 2, 0), actorTerrain); terrain.SetTile(new Vector3Int(2, 2, 0), enemyTerrain);
                var camera = Create("Camera").AddComponent<Camera>(); camera.orthographic = true;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Converted/Characters/Fighter100/Fighter100_Down_1.png");
                Assert.That(sprite, Is.Not.Null);
                var ally = Create("Ally").AddComponent<SpriteRenderer>(); ally.sprite = sprite; ally.transform.position = new Vector3(1.5f, 2.5f);
                var enemy = Create("Enemy").AddComponent<SpriteRenderer>(); enemy.sprite = sprite; enemy.transform.position = new Vector3(2.5f, 2.5f);
                var root = Create("Battle"); root.AddComponent<BattleRequestSender>();
                var receiver = root.AddComponent<BattlePacketReceiver>(); NativeCombatStateFixture.Initialize(root.GetComponent<BattleSceneRoot>(),6); Assert.That(receiver.Bind(), Is.True);
                var receivedEnemy=NativeCombatStateFixture.Unit(root.GetComponent<BattleSceneRoot>(),800);
                receivedEnemy.Attack=18;receivedEnemy.Defense=5;
                var view = root.AddComponent<BattleMapPresentation>();
                check(new Fixture { View = view, Battle = root.GetComponent<BattleSceneRoot>(), Server = root.GetComponent<ServerManager>(),
                    Receiver = receiver, Ground = ground, Terrain = terrain, Camera = camera, Ally = ally, Enemy = enemy, ActorTerrain = actorTerrain });
            } finally {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var view in root.GetComponentsInChildren<BattleMapPresentation>(true)) view.ReleaseMagicPreviewResources();
                EditorSceneManager.ClosePreviewScene(scene);
                foreach (var tile in tiles) UnityEngine.Object.DestroyImmediate(tile);
            }
        }
    }
}
