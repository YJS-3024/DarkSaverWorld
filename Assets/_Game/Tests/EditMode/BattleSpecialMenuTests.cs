using System;
using DarkSaver.Combat;
using DarkSaver.Magic;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.Tests
{
    public sealed class BattleSpecialMenuTests
    {
        [TestCase(9999,false)] [TestCase(10000,true)] [TestCase(19999,true)]
        [TestCase(20000,false)] [TestCase(29999,false)] [TestCase(30000,true)]
        [TestCase(31999,true)] [TestCase(32000,false)] [TestCase(-1,false)]
        public void NativeSpecialIdBoundaries(int id, bool expected)
            => Assert.That(BattleMapPresentation.IsNativeSpecialMenuId(id), Is.EqualTo(expected));

        [Test] public void SpecialMenuStaysSeparateAndUsesExistingNativeRequestPath()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var floor = ScriptableObject.CreateInstance<Tile>();
            var catalog = ScriptableObject.CreateInstance<MagicCatalog>();
            GameObject Create(string name) { var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go,scene); return go; }
            try
            {
                var grid = Create("Grid").AddComponent<Grid>();
                var ground = Create("Ground").AddComponent<Tilemap>(); ground.transform.SetParent(grid.transform);
                var terrain = Create("Terrain").AddComponent<Tilemap>(); terrain.transform.SetParent(grid.transform);
                for (int x=0;x<4;x++) for(int y=0;y<4;y++) ground.SetTile(new Vector3Int(x,y,0),floor);
                var camera = Create("Camera").AddComponent<Camera>(); camera.orthographic = true;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Converted/Characters/Fighter100/Fighter100_Down_1.png");
                SpriteRenderer Body(string name,float x)
                { var body=Create(name).AddComponent<SpriteRenderer>();body.sprite=sprite;body.transform.position=new Vector3(x,2.5f,0);return body; }
                var ally=Body("Ally",1.5f); var enemy=Body("Enemy",2.5f);
                var root=Create("Battle"); root.AddComponent<BattleRequestSender>();
                Assert.That(root.AddComponent<BattlePacketReceiver>().Bind(),Is.True);
                var view=root.AddComponent<BattleMapPresentation>(); var battle=root.GetComponent<BattleSceneRoot>(); NativeCombatStateFixture.Initialize(battle,6);
                Assert.That(view.TryBind(ground,terrain,camera,new[]{new BattleMapVisual(100,ally),new BattleMapVisual(800,enemy)}),Is.True);
                battle.TrySelectUnit(100);
                MagicCatalogEntry Entry(int id)
                { var values=new long[MagicCatalogEntry.NumericColumnCount]; values[0]=id;values[2]=-1;values[3]=2;values[6]=values[7]=1;return new MagicCatalogEntry(values,"Explicit fixture"); }
                catalog.Initialize(new[]{Entry(701),Entry(10001),Entry(10002)});
                var normal=new MagicRequest(701,100,0,0,0,Array.Empty<short>());
                var special=new MagicRequest(10001,100,0,0,-123,Array.Empty<short>(),0x92,7);
                Assert.That(view.ConfigureMagicChoices(catalog,100,new[]{normal},_=>true,_=>1,null,null),Is.True);
                Assert.That(view.SpecialChoiceCount,Is.Zero);
                Assert.That(view.ConfigureSpecialChoices(catalog,100,new[]{special},_=>true,_=>1,null,null),Is.True);
                Assert.That(view.ConfigureSpecialChoices(catalog,100,new[]{normal},_=>true,_=>1,null,null),Is.False);
                Assert.That(view.ConfigureSpecialChoices(catalog,100,new[]{special,special},_=>true,_=>1,null,null),Is.False);
                Assert.That(view.SpecialChoiceCount,Is.EqualTo(1));
                view.TryOpenActionMenu(); Assert.That(view.TryChooseCommand(BattleMapCommand.Special),Is.True);
                Assert.That(view.TryGetDisplayedMagic(0,out var shown),Is.True);Assert.That(shown.Id,Is.EqualTo(10001));
                Assert.That(battle.PlayerAp,Is.EqualTo(6));
                view.CancelMagicTargeting();view.TryOpenActionMenu();Assert.That(view.TryChooseCommand(BattleMapCommand.Magic),Is.True);
                Assert.That(view.TryGetDisplayedMagic(0,out shown),Is.True);Assert.That(shown.Id,Is.EqualTo(701));
                view.CancelMagicTargeting();
                battle.ConfigureMagicApCost(10001,3);NativeCombatStateFixture.ReceiveFullTicks(battle);
                MagicRequest received=null;int count=0;
                root.GetComponent<ServerManager>().RequestReceived += packet=>{if(packet is MagicRequest command){received=command;count++;}};
                view.TryOpenActionMenu();view.TryChooseCommand(BattleMapCommand.Special);
                Assert.That(view.TrySelectDisplayedMagic(0),Is.True);
                Assert.That(received,Is.Null); Assert.That(battle.PlayerAp,Is.EqualTo(20));
                Assert.That(view.ConfigureSpecialChoices(catalog,100,Array.Empty<MagicRequest>(),_=>true,_=>1,null,null),Is.True);
                Assert.That(view.IsMagicTargeting,Is.False);Assert.That(view.SpecialChoiceCount,Is.Zero);
                Assert.That(view.ConfigureSpecialChoices(catalog,100,new[]{special},_=>true,_=>1,null,null),Is.True);
                view.TryOpenActionMenu();view.TryChooseCommand(BattleMapCommand.Special);
                Assert.That(view.TrySelectDisplayedMagic(0),Is.True);
                Assert.That(view.TryHandleWorldClick(enemy.transform.position),Is.True);
                Assert.That(count,Is.EqualTo(1)); Assert.That(received.MagicId,Is.EqualTo(10001));
                Assert.That(received.X,Is.EqualTo(-1));Assert.That(received.ComputedCost,Is.EqualTo(-123));
                Assert.That(received.OriginalOpcode,Is.EqualTo(0x92));Assert.That(received.Mode,Is.EqualTo(7));
                Assert.That(battle.PlayerAp,Is.EqualTo(17));Assert.That(view.IsMagicChoiceOpen,Is.False);
                // A later confirmed normal-only learned list must also block this special request.
                Assert.That(battle.ConfigurePlayerJob(100),Is.True);
                battle.TryGetUnit(100,out var actor);
                var draft=new BattleFormationMemberDraft {PersistentId="fixture",CommanderPersistentId="fixture",CommanderId=100,
                    Unit=NativeCombatStateFixture.Received(new CombatUnit {Id=100,JobId=100,Name="fixture",Hp=50,MaxHp=50,X=actor.X,Y=actor.Y}),
                    Position=new CombatGridCell(actor.X,actor.Y),TeamId=1,RecordEligible=true,IsOwned=true,FinalStateConfirmed=true,
                    LearnedMagicIds=new short[]{701}};
                Assert.That(BattleFormation.TryCreateSnapshot(new[]{draft},4,4,Array.Empty<short>(),out var formation,out _),Is.True);
                Assert.That(battle.ConfigureFormationLearnedMagic(formation),Is.True);
                Assert.That(view.ConfigureSpecialChoices(catalog,100,new[]{special},_=>true,_=>1,null,null),Is.False);
                view.TryOpenActionMenu();view.TryChooseCommand(BattleMapCommand.Special);
                Assert.That(view.TrySelectDisplayedMagic(0),Is.False);Assert.That(count,Is.EqualTo(1));
                // Confirmed ownership drives the two menus independently of request order.
                draft.LearnedMagicIds=new short[]{10002,701,10001};
                Assert.That(BattleFormation.TryCreateSnapshot(new[]{draft},4,4,Array.Empty<short>(),out formation,out _),Is.True);
                Assert.That(view.ConfigureFormationLearnedMagic(catalog,formation),Is.True);
                view.TryOpenActionMenu();Assert.That(view.TryChooseCommand(BattleMapCommand.Magic),Is.True);
                Assert.That(view.DisplayedMagicCount,Is.EqualTo(1));
                Assert.That(view.TryGetDisplayedMagic(0,out shown),Is.True);Assert.That(shown.Id,Is.EqualTo(701));
                view.CancelMagicTargeting();view.TryOpenActionMenu();
                Assert.That(view.TryChooseCommand(BattleMapCommand.Special),Is.True);
                Assert.That(view.SpecialChoiceCount,Is.EqualTo(2));
                Assert.That(view.TryGetDisplayedMagic(0,out shown),Is.True);Assert.That(shown.Id,Is.EqualTo(10002));
                Assert.That(view.TrySelectDisplayedMagic(0),Is.False); // No invented request/cost.
                Assert.That(view.TryGetDisplayedMagic(1,out shown),Is.True);Assert.That(shown.Id,Is.EqualTo(10001));
                NativeCombatStateFixture.ReceiveFullTicks(battle);
                Assert.That(view.TrySelectDisplayedMagic(1),Is.True); // Display index 1 -> request index 0.
                view.CancelMagicTargeting();Assert.That(count,Is.EqualTo(1));
                draft.LearnedMagicIds=new short[]{701};
                Assert.That(BattleFormation.TryCreateSnapshot(new[]{draft},4,4,Array.Empty<short>(),out formation,out _),Is.True);
                Assert.That(view.ConfigureFormationLearnedMagic(catalog,formation),Is.True);
                Assert.That(view.SpecialChoiceCount,Is.Zero); // Stale configured command cannot populate or cast from the empty window.
                view.TryOpenActionMenu(); Assert.That(view.TryChooseCommand(BattleMapCommand.Special), Is.True);
                Assert.That(view.IsBattleWindowShellOpen, Is.True);
                Assert.That(view.IsMagicChoiceOpen, Is.False);
                Assert.That(view.TrySelectDisplayedMagic(0), Is.False);
                Assert.That(count, Is.EqualTo(1));
                view.TryCloseBattleWindowShell();
                var profile = new DarkSaver.Core.NativeCharacterProfile { name="fixture",jobId=100,hasClassAbilities=true,
                    learnedMagicIds=new[]{701},usableSkillIds=new[]{10001,21106} };
                Assert.That(view.ConfigureCharacterLearnedMagic(catalog,profile),Is.True);
                Assert.That(battle.IsLearnedMagicAllowed(100,10001),Is.True);
                Assert.That(battle.IsLearnedMagicAllowed(100,10002),Is.False);
                Assert.That(battle.IsLearnedMagicAllowed(100,21106),Is.False);
                view.TryOpenActionMenu(); Assert.That(view.TryChooseCommand(BattleMapCommand.Special),Is.True);
                Assert.That(view.SpecialChoiceCount,Is.EqualTo(1));
                Assert.That(view.TryGetDisplayedMagic(0,out shown),Is.True); Assert.That(shown.Id,Is.EqualTo(10001));
                view.CancelMagicTargeting();
                profile.learnedMagicIds=new[]{9999};
                Assert.That(view.ConfigureCharacterLearnedMagic(catalog,profile),Is.False);
                Assert.That(battle.IsLearnedMagicAllowed(100,10001),Is.True); // Atomic failure.
            }
            finally
            {
                foreach(var root in scene.GetRootGameObjects())
                    foreach(var view in root.GetComponentsInChildren<BattleMapPresentation>(true)) view.ReleaseMagicPreviewResources();
                EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Object.DestroyImmediate(floor);UnityEngine.Object.DestroyImmediate(catalog);
            }
        }
    }
}
