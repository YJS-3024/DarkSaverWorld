using System;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Network;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace DarkSaver.Tests
{
    public sealed class BattleSystemMenuTests
    {
        [Test]
        public void NativeToolMenuPagesSixItemsWithoutConsumingInventoryOrSendingRequests()
        {
            WithBattle((battle,view,enemy)=> {
                var catalog=AssetDatabase.LoadAssetAtPath<DarkSaver.Items.ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset");
                var rows=new System.Collections.Generic.List<BattleToolDisplayEntry>();
                for(int i=0;i<catalog.Count && rows.Count<13;i++)if(catalog.GetAt(i).GetNumericValue(3)==7)rows.Add(new BattleToolDisplayEntry(catalog.GetAt(i).Id,3));
                Assert.AreEqual(13,rows.Count);Assert.IsTrue(view.ConfigureToolDisplay(catalog,100,rows));
                Assert.IsTrue(view.TryOpenBattleWindowShell(BattleMapPresentation.ToolsShell));
                int packets=0;view.GetComponent<ServerManager>().RequestReceived+=_=>packets++;
                Assert.IsFalse(view.TryPageToolDisplay(false));Assert.IsTrue(view.TryPageToolDisplay(true));Assert.AreEqual(6,view.ToolDisplayOffset);
                Assert.IsTrue(view.TryPageToolDisplay(true));Assert.AreEqual(7,view.ToolDisplayOffset);Assert.IsFalse(view.TryPageToolDisplay(true));
                Assert.IsTrue(view.TryPageToolDisplay(false));Assert.AreEqual(1,view.ToolDisplayOffset);
                var draw=typeof(BattleMapPresentation).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);draw.Invoke(view,null);
                var root=view.GetComponentsInChildren<UIDocument>().Single(d=>d.rootVisualElement.Q("system-modal-input")!=null).rootVisualElement;
                for(int i=0;i<6;i++){Assert.IsFalse(root.Q("tool-row-"+i).enabledSelf);Assert.IsNotNull(root.Q("tool-row-"+i+"-icon"));Assert.AreEqual(1,root.Q("tool-row-"+i+"-icon").style.opacity.value);}
                Assert.AreEqual(new Vector2(96,96),view.SystemMenuRect.size);Assert.AreEqual(0,packets);
                Assert.IsTrue(view.TryCloseBattleWindowShell());Assert.IsFalse(view.TryPageToolDisplay(true));
                Assert.IsTrue(view.TryOpenBattleWindowShell(BattleMapPresentation.ToolsShell));Assert.AreEqual(0,view.ToolDisplayOffset);
            });
        }

        [Test]
        public void NativeTroopTriangleUsesRealIconsAndSelectingOrderClosesMenu()
        {
            WithBattle((battle,view,enemy)=> {
                battle.SetTemporaryLocalRules(true);
                Assert.IsTrue(view.TryOpenActionMenu());Assert.IsTrue(view.TryChooseCommand(BattleMapCommand.Troop));
                var draw=typeof(BattleMapPresentation).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                draw.Invoke(view,null);
                var root=view.GetComponentsInChildren<UIDocument>().Single(d=>d.rootVisualElement.Q("system-modal-input")!=null).rootVisualElement;
                foreach(int id in new[]{1,2,8})Assert.IsTrue(root.Q("troop-order-"+id).enabledInHierarchy);
                Assert.AreEqual(new Vector2(80,96),view.SystemMenuRect.size);
                for(int i=0;i<3;i++)Assert.IsNotNull(DarkSaver.UI.BattleIconFrames.Magic(1,i,0));
                Assert.IsTrue(view.TryChooseTroopOrder(BattleSceneRoot.TroopOrder.AutomaticDefense));
                Assert.IsFalse(view.IsSystemMenuOpen);Assert.IsFalse(view.IsActionMenuOpen);
                Assert.IsTrue(battle.TryGetTroopOrder(101,out var order));Assert.AreEqual(BattleSceneRoot.TroopOrder.AutomaticDefense,order);
                Assert.IsTrue(view.TryOpenActionMenu());Assert.IsTrue(view.TryChooseCommand(BattleMapCommand.Troop));
                Assert.IsTrue(battle.TrySelectUnit(101));
                Assert.IsFalse(view.TryChooseTroopOrder(BattleSceneRoot.TroopOrder.AutomaticAttack));
                Assert.IsTrue(view.TryCloseBattleWindowShell());
                Assert.IsTrue(battle.TryGetTroopOrder(100,out order));Assert.AreEqual(BattleSceneRoot.TroopOrder.AutomaticDefense,order);
            },withSoldier:true);
        }

        [Test]
        public void TroopMenuChangesOnlySelectedGroupAndCancelLeavesModeUnchanged()
        {
            WithBattle((battle,view,enemy)=> {
                // Group registration must precede grid binding; this fixture has no soldiers.
                Assert.IsTrue(view.TryOpenBattleWindowShell(BattleMapPresentation.TroopShell));
                Assert.IsFalse(view.TryChooseTroopOrder(BattleSceneRoot.TroopOrder.AutomaticAttack));
                var draw=typeof(BattleMapPresentation).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                draw.Invoke(view,null);
                var root=view.GetComponentsInChildren<UIDocument>().Single(d=>d.rootVisualElement.Q("system-modal-input")!=null).rootVisualElement;
                foreach(int id in new[]{1,2,8})Assert.IsFalse(root.Q("troop-order-"+id).enabledInHierarchy);
                Assert.IsTrue(view.TryCloseBattleWindowShell());
                Assert.IsTrue(battle.TryGetTroopOrder(100,out var order));Assert.AreEqual(BattleSceneRoot.TroopOrder.Direct,order);
                Assert.IsFalse(view.TryChooseTroopOrder(BattleSceneRoot.TroopOrder.AutomaticDefense));
            });
        }

        [Test]
        public void SessionInventoryToolRowsPreserveRealQuantitiesWithoutGrantingAlliesItems()
        {
            WithBattle((battle,view,enemy)=> {
                var session=view.gameObject.AddComponent<BattleTransitionController>();
                Assert.IsTrue(session.InitializeCharacter(new DarkSaver.Core.NativeCharacterProfile {name="검증",jobId=100}));
                var catalog=AssetDatabase.LoadAssetAtPath<DarkSaver.Items.ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset");
                int item=300,weapon=1;Assert.IsTrue(catalog.TryGet(item,out var consumable));Assert.AreEqual(7,consumable.GetNumericValue(3));
                for(int i=0;i<catalog.Count;i++)if(catalog.GetAt(i).Id>0 && catalog.GetAt(i).GetNumericValue(3)!=7){weapon=catalog.GetAt(i).Id;break;}
                var controller=view.gameObject.AddComponent<DarkSaver.Player.TopDownPlayerController>();
                view.gameObject.AddComponent<DarkSaver.UI.ItemCatalogPanel>().Configure(catalog,controller);
                var save=session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                save.inventory.items=new[]{new DarkSaver.Core.NativeItemStack {itemId=weapon,quantity=2},new DarkSaver.Core.NativeItemStack {itemId=item,quantity=3}};
                Assert.IsTrue(session.RestoreSaveState(save));string before=JsonUtility.ToJson(session.CaptureInventory());
                Assert.IsTrue(view.TryBindSessionToolDisplay(session));Assert.IsTrue(view.TryGetToolDisplay(100,out var rows));
                Assert.AreEqual(1,rows.Count);Assert.AreEqual(item,rows[0].ItemId);Assert.AreEqual(3,rows[0].Count);
                Assert.IsFalse(view.TryGetToolDisplay(101,out _));
                var detached=session.CaptureInventory();detached.items[1].quantity=99;Assert.AreEqual(3,rows[0].Count);
                Assert.AreEqual(before,JsonUtility.ToJson(session.CaptureInventory()));
                save.inventory.items=new[]{new DarkSaver.Core.NativeItemStack {itemId=weapon,quantity=2}};
                Assert.IsTrue(session.RestoreSaveState(save));Assert.IsTrue(view.TryBindSessionToolDisplay(session));
                Assert.IsTrue(view.TryGetToolDisplay(100,out rows));Assert.AreEqual(0,rows.Count);
                save.inventory.items=new[]{new DarkSaver.Core.NativeItemStack {itemId=999999,quantity=1}};
                Assert.IsTrue(session.RestoreSaveState(save));Assert.IsFalse(view.TryBindSessionToolDisplay(session));
                Assert.IsTrue(view.TryGetToolDisplay(100,out rows));Assert.AreEqual(0,rows.Count);
            });
        }

        [Test]
        public void AlliedNamesAndConvertedOriginalHelpHaveRealContentWithoutRequests()
        {
            WithBattle((battle,view,enemy)=> {
                int packets=0;view.GetComponent<ServerManager>().RequestReceived += _=>packets++;
                var draw=typeof(BattleMapPresentation).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                Assert.IsTrue(view.TryOpenBattleWindowShell(3));draw.Invoke(view,null);
                var root=view.GetComponentsInChildren<UIDocument>().Single(d=>d.rootVisualElement.Q("system-modal-input")!=null).rootVisualElement;
                Assert.AreEqual(battle.GetAlliedRosterCommanders()[0].Name,root.Q("alliance-name-0").tooltip);
                Assert.IsTrue(view.TryOpenBattleWindowShell(7));draw.Invoke(view,null);
                var text=Resources.Load<TextAsset>("UI/BattleWindows/NativeBattleHelp");Assert.IsNotNull(text);Assert.Greater(text.text.Length,100);
                Assert.AreEqual(text.text,root.Q<ScrollView>("native-battle-help-scroll").tooltip);
                Assert.AreEqual(0,packets);
            });
        }

        [Test]
        public void NativeWindowSizeChangesPreserveIdentityAndBlockMapCommands()
        {
            WithBattle((battle,view,enemy) =>
            {
                int packets=0;view.GetComponent<ServerManager>().RequestReceived += _=>packets++;
                int hp=battle.PlayerHp,ap=battle.PlayerAp;
                foreach(int id in new[]{0,3,BattleMapPresentation.EmptyMagicShell,BattleMapPresentation.EmptySpecialShell})
                {
                    Assert.IsTrue(view.TryOpenBattleWindowShell(id));
                    var original=view.SystemMenuRect;
                    Assert.IsTrue(view.TryMinimizeBattleWindow()); Assert.IsTrue(view.IsBattleWindowMinimized);
                    Assert.AreEqual(id,view.BattleWindowShellId); Assert.Less(view.SystemMenuRect.width,original.width);
                    Assert.AreEqual(original.center.x,view.SystemMenuRect.center.x,.01f);
                    Assert.AreEqual(original.center.y,view.SystemMenuRect.center.y,.01f);
                    Assert.IsTrue(view.TryBlockMapPointer(Vector2.zero)); Assert.IsFalse(view.TryHandleWorldClick(enemy.transform.position));
                    Assert.IsFalse(view.TryMinimizeBattleWindow()); Assert.IsTrue(view.TryRestoreBattleWindow());
                    Assert.AreEqual(original,view.SystemMenuRect); Assert.IsFalse(view.TryRestoreBattleWindow());
                    Assert.IsTrue(view.TryMinimizeBattleWindow()); Assert.IsTrue(view.TryCloseBattleWindowShell());
                    Assert.IsFalse(view.IsBattleWindowMinimized);Assert.IsTrue(view.TryOpenBattleWindowShell(id));Assert.IsFalse(view.IsBattleWindowMinimized);
                }
                Assert.IsTrue(view.TryOpenBattleWindowShell(5)); Assert.IsFalse(view.TryMinimizeBattleWindow());
                Assert.AreEqual(hp,battle.PlayerHp); Assert.AreEqual(ap,battle.PlayerAp);Assert.AreEqual(0,packets);
            });
        }

        [Test]
        public void NativeMenuEntriesOpenOverviewAndReturnWithoutGameplayRequests()
        {
            WithBattle((battle,view,enemy) =>
            {
                int packets=0; view.GetComponent<ServerManager>().RequestReceived += _=>packets++;
                var hp=battle.PlayerHp; var ap=battle.PlayerAp;
                Assert.IsTrue(view.TryOpenSystemMenu());
                typeof(BattleMapPresentation).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(view,null);
                var root=view.GetComponentsInChildren<UIDocument>().Single(d=>d.rootVisualElement.Q("system-modal-input")!=null).rootVisualElement;
                foreach(var pair in new[]{("system-roster","정보창 보기"),("system-alliance","동맹"),("system-victory","승리조건"),("system-controls","도움말"),("system-minimap","맵정보"),("system-retreat","도망")})
                    Assert.AreEqual(pair.Item2,root.Q<Button>(pair.Item1).text);
                Assert.IsNull(root.Q<Button>("system-windows"));
                Assert.IsTrue(view.TryShowMinimap()); Assert.IsTrue(view.IsMinimapOpen);
                typeof(BattleMapPresentation).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(view,null);
                Assert.IsNotNull(root.Q("minimap-ground"));
                Assert.IsTrue(view.TryBlockMapPointer(Vector2.zero));
                Assert.IsFalse(view.TryHandleWorldClick(enemy.transform.position));
                Assert.IsTrue(view.TryHandleCancelInput(false)); Assert.IsFalse(view.IsMinimapOpen); Assert.IsTrue(view.IsSystemMenuOpen);
                foreach(int id in new[]{3,5,7})
                {
                    Assert.IsTrue(view.TryOpenBattleWindowShell(id)); Assert.AreEqual(id,view.BattleWindowShellId);
                    Assert.IsTrue(view.TryCloseBattleWindowShell()); Assert.IsFalse(view.IsBattleWindowShellOpen);
                }
                Assert.AreEqual(hp,battle.PlayerHp);Assert.AreEqual(ap,battle.PlayerAp);Assert.AreEqual(0,packets);
            });
        }
        [Test]
        public void SystemControlsStayAboveTheRetainedBlockerAndRequestImmediateRetreat()
        {
            WithBattle((battle,view,enemy) =>
            {
                Assert.IsTrue(view.TryOpenSystemMenu());
                for(int frame=0;frame<2;frame++)
                {
                    typeof(BattleMapPresentation).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(view, null);
                    var document=view.GetComponentsInChildren<UIDocument>().Single(d=>d.rootVisualElement.Q("system-modal-input")!=null);
                    var blocker=document.rootVisualElement.Q("system-modal-input");
                    var button=document.rootVisualElement.Q<Button>("system-retreat");
                    Assert.AreSame(blocker.parent,button.parent);
                    Assert.Greater(button.parent.IndexOf(button),blocker.parent.IndexOf(blocker));
                }
                var target=view.GetComponentsInChildren<UIDocument>().Select(d=>d.rootVisualElement.Q<Button>("system-retreat")).First(b=>b!=null);
                using var submit=NavigationSubmitEvent.GetPooled();submit.target=target;target.SendEvent(submit);
                Assert.IsFalse(view.TryRetreatImmediately()); // No owning live Battle transition.
                Assert.IsTrue(view.IsSystemMenuOpen);
            });
        }

        [Test]
        public void CancelAndSystemMenuDoNotRetreatSpendApOrLeakMapInput()
        {
            WithBattle((battle,view,enemy) =>
            {
                int packets=0;view.GetComponent<ServerManager>().RequestReceived += _=>packets++;
                var hp=battle.PlayerHp;var ap=battle.PlayerAp;var timeScale=Time.timeScale;
                Assert.IsTrue(view.TryOpenActionMenu());
                Assert.IsTrue(view.TryHandleCancelInput(true));
                Assert.IsFalse(view.IsActionMenuOpen);Assert.IsFalse(view.IsSystemMenuOpen);
                Assert.IsTrue(view.TryHandleCancelInput(true));Assert.IsTrue(view.IsSystemMenuOpen);
                Assert.IsTrue(view.TryBlockMapPointer(Vector2.zero));
                Assert.IsFalse(view.TryHandleWorldClick(enemy.transform.position));
                Assert.IsFalse(view.TryChooseBasicCommand(BattleMapCommand.Rest));
                Assert.IsFalse(view.TryOpenActionMenu());
                Assert.AreEqual(hp,battle.PlayerHp);Assert.AreEqual(ap,battle.PlayerAp);Assert.AreEqual(0,packets);
                Assert.AreEqual(timeScale,Time.timeScale);
                battle.AdvanceActionPoints(1);Assert.AreEqual(ap+1,battle.PlayerAp);
                battle.CloseAlliedRoster();Assert.IsTrue(view.TryRestoreRosterFromSystemMenu());
                Assert.IsTrue(battle.IsAlliedRosterVisible);Assert.IsFalse(view.IsSystemMenuOpen);
                Assert.IsTrue(view.TryOpenActionMenu());Assert.IsTrue(view.TryChooseCommand(BattleMapCommand.System));
                Assert.IsTrue(view.TryShowControlHelp());Assert.IsFalse(view.TryRetreatImmediately());
                Assert.IsTrue(view.TryHandleCancelInput(false));Assert.IsFalse(view.TryRetreatImmediately());
                Assert.IsTrue(view.IsSystemMenuOpen);Assert.IsTrue(view.TryHandleCancelInput(false));
                Assert.IsFalse(view.IsSystemMenuOpen);Assert.IsFalse(view.TryHandleCancelInput(false));
                Assert.IsTrue(view.TryBlockMapPointer(Vector2.zero)); // Closing click cannot hit the map in this frame.
                Assert.IsFalse(view.TryHandleWorldClick(enemy.transform.position));
                Assert.AreEqual(0,packets);Assert.AreEqual(hp,battle.PlayerHp);Assert.AreEqual(ap+1,battle.PlayerAp);
                Assert.IsTrue(view.TryOpenSystemMenu());view.SuspendPresentation();
                Assert.IsFalse(view.IsSystemMenuOpen);
            });
        }

        [Test]
        public void BattleOutcomeClosesSystemMenuWithoutAcknowledgingOrRetreating()
        {
            WithBattle((battle,view,enemy) =>
            {
                Assert.IsTrue(view.TryOpenSystemMenu());Assert.IsFalse(view.TryRetreatImmediately());
                Assert.IsTrue(battle.ApplyHp(100,0));view.RefreshVisualState();
                Assert.IsTrue(battle.IsBattleFinished);Assert.IsFalse(battle.OutcomeAcknowledged);
                Assert.IsFalse(view.IsSystemMenuOpen);Assert.AreEqual(default(Rect),view.SystemMenuRect);
                Assert.IsFalse(view.TryRetreatImmediately());Assert.IsFalse(view.TryOpenSystemMenu());
            });
        }

        [Test]
        public void EmptyWindowShellsAreModalAndDoNotSendGameplayRequests()
        {
            WithBattle((battle, view, enemy) =>
            {
                int packets = 0;
                view.GetComponent<ServerManager>().RequestReceived += _ => packets++;
                int hp = battle.PlayerHp, ap = battle.PlayerAp;
                var catalog = Resources.Load<DarkSaver.UI.NativeBattleWindows>("UI/BattleWindows/NativeBattleWindows");
                Assert.IsNotNull(catalog); Assert.AreEqual(9, catalog.windows.Length);
                Assert.IsFalse(view.TryOpenBattleWindowShell(999));
                foreach (int id in new[] { 0, 3, 4, 5, 6, 7, 8, 17, 28, 1000, 1001, 1002 })
                {
                    Assert.IsTrue(view.TryOpenBattleWindowShell(id), "Window " + id);
                    Assert.IsTrue(view.IsBattleWindowShellOpen);
                    Assert.AreEqual(id, view.BattleWindowShellId);
                    Assert.IsTrue(view.TryBlockMapPointer(Vector2.zero));
                    Assert.IsFalse(view.TryHandleWorldClick(enemy.transform.position));
                    Assert.IsFalse(view.TryChooseBasicCommand(BattleMapCommand.Attack));
                    var draw = typeof(BattleMapPresentation).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    draw.Invoke(view, null); draw.Invoke(view, null);
                    var documents = view.GetComponentsInChildren<UIDocument>();
                    var blocker = documents.Select(d => d.rootVisualElement.Q<VisualElement>("system-modal-input")).First(e => e != null);
                    string key = id < 1000 ? "native-window-" + id : id == BattleMapPresentation.TroopShell ? "troop-order-1" : id == BattleMapPresentation.ToolsShell ? "tool-status" : "shell-panel";
                    var panel = documents.Select(d => d.rootVisualElement.Q<VisualElement>(key)).First(e => e != null);
                    Assert.Greater(panel.parent.IndexOf(panel), blocker.parent.IndexOf(blocker));
                    if (id < 1000)
                    {
                        var window = catalog.Find(id);
                        Assert.AreEqual(window.size.x, panel.style.width.value.value);
                        for (int c = 0; c < window.controls.Length; c++)
                        {
                            var control = window.controls[c];
                            Assert.AreEqual(3, control.frames.Length);
                            var button = panel.parent.Q<Button>(key + "-control-" + c);
                            bool closes = control.nativeId == 19 || control.nativeId == 38 || control.nativeId == 66;
                            Assert.AreEqual(closes || (control.nativeId == 37 && window.minimizedBackground != null), button.enabledSelf);
                            Assert.Greater(button.parent.IndexOf(button), blocker.parent.IndexOf(blocker));
                        }
                    }
                    Assert.IsTrue(view.TryHandleCancelInput(false));
                    Assert.IsFalse(view.IsBattleWindowShellOpen);
                    view.CloseSystemMenu();
                }
                Assert.AreEqual(0, packets); Assert.AreEqual(hp, battle.PlayerHp); Assert.AreEqual(ap, battle.PlayerAp);
                Assert.IsTrue(view.TryOpenBattleWindowShell(5));
                view.SuspendPresentation(); Assert.IsFalse(view.IsBattleWindowShellOpen);
            });
        }

        [Test]
        public void ToolAndTroopCommandsOpenOnlyTheirEmptyWindows()
        {
            WithBattle((battle, view, enemy) =>
            {
                int packets = 0;
                view.GetComponent<ServerManager>().RequestReceived += _ => packets++;
                int hp = battle.PlayerHp, mp = battle.PlayerMp, ap = battle.PlayerAp;
                Assert.IsFalse(view.TryChooseCommand(BattleMapCommand.Tools));
                foreach (int index in new[] { 6, 7 })
                {
                    Assert.IsTrue(view.TryOpenActionMenu());
                    Assert.IsTrue(view.TryChooseCommand(index == 6 ? BattleMapCommand.Troop : BattleMapCommand.Tools));
                    Assert.IsTrue(view.IsBattleWindowShellOpen);
                    Assert.AreEqual(index == 6 ? BattleMapPresentation.TroopShell : BattleMapPresentation.ToolsShell, view.BattleWindowShellId);
                    Assert.IsFalse(view.IsActionMenuOpen);
                    Assert.IsTrue(view.TryBlockMapPointer(Vector2.zero));
                    Assert.IsFalse(view.TryHandleWorldClick(enemy.transform.position));
                    if (index == 6) Assert.IsTrue(view.TryHandleCancelInput(false));
                    else Assert.IsTrue(view.TryCloseBattleWindowShell());
                    Assert.IsFalse(view.IsSystemMenuOpen);
                    Assert.IsTrue(view.TryBlockMapPointer(Vector2.zero));
                }
                Assert.AreEqual(hp, battle.PlayerHp); Assert.AreEqual(mp, battle.PlayerMp);
                Assert.AreEqual(ap, battle.PlayerAp); Assert.AreEqual(0, packets);
            });
        }

        [Test]
        public void EmptyMagicAndSpecialWindowsCannotGrantOrCastSkills()
        {
            WithBattle((battle, view, enemy) =>
            {
                int packets = 0;
                view.GetComponent<ServerManager>().RequestReceived += _ => packets++;
                int hp = battle.PlayerHp, mp = battle.PlayerMp, ap = battle.PlayerAp;
                foreach (var command in new[] { BattleMapCommand.Magic, BattleMapCommand.Special })
                {
                    Assert.IsFalse(view.TryChooseCommand(command));
                    Assert.IsTrue(view.TryOpenActionMenu());
                    Assert.IsTrue(view.TryChooseCommand(command));
                    Assert.IsTrue(view.IsBattleWindowShellOpen);
                    Assert.AreEqual(command == BattleMapCommand.Magic ? BattleMapPresentation.EmptyMagicShell
                        : BattleMapPresentation.EmptySpecialShell, view.BattleWindowShellId);
                    Assert.IsFalse(view.IsMagicChoiceOpen);
                    Assert.IsFalse(view.IsMagicTargeting);
                    Assert.IsFalse(view.TrySelectDisplayedMagic(0));
                    Assert.IsFalse(view.TrySelectMagicChoice(0));
                    Assert.IsTrue(view.TryBlockMapPointer(Vector2.zero));
                    Assert.IsFalse(view.TryHandleWorldClick(enemy.transform.position));
                    Assert.IsTrue(view.TryHandleCancelInput(true));
                    Assert.IsFalse(view.IsSystemMenuOpen);
                    Assert.IsTrue(view.TryBlockMapPointer(Vector2.zero));
                }
                Assert.AreEqual(0, packets); Assert.AreEqual(hp, battle.PlayerHp);
                Assert.AreEqual(mp, battle.PlayerMp); Assert.AreEqual(ap, battle.PlayerAp);
                Assert.AreEqual(0, view.MagicChoiceCount); Assert.AreEqual(0, view.SpecialChoiceCount);
            });
        }

        [Test]
        public void OutcomeListsReadBattleUnitsWithoutAwardingExperience()
        {
            WithBattle((battle, view, enemy) =>
            {
                Assert.IsTrue(battle.RestorePlayerExperience(123));
                battle.TryGetUnit(100, out var ally); battle.TryGetUnit(800, out var foe);
                Assert.AreEqual(string.Empty, battle.OutcomeAllies);
                Assert.AreEqual(string.Empty, battle.OutcomeEnemies);
                Assert.AreEqual(string.Empty, battle.OutcomeSummary);
                int packets = 0;
                view.GetComponent<ServerManager>().RequestReceived += _ => packets++;
                Assert.IsTrue(battle.ApplyHp(800, 0));
                Assert.IsTrue(battle.ApplyDeath(800, true));
                Assert.IsTrue(battle.IsBattleFinished);
                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(ally.Name, battle.OutcomeAllies);
                    Assert.AreEqual(foe.Name, battle.OutcomeEnemies); // Defeated units remain in the report.
                    Assert.AreEqual("승리", battle.OutcomeSummary);
                    Assert.AreEqual(string.Empty, battle.OutcomeGrantedExperience);
                    Assert.AreEqual(123u, battle.PlayerExperience);
                    Assert.IsFalse(battle.OutcomeAcknowledged);
                }
                Assert.AreEqual(434f, battle.OutcomeRect.width);
                Assert.AreEqual(344f, battle.OutcomeRect.height);
                Assert.AreEqual(0, packets);
            });
        }

        [Test]
        public void VictoryConditionTextFollowsConfiguredGoalWithoutChangingBattleState()
        {
            WithBattle((battle, view, enemy) =>
            {
                int hp = battle.PlayerHp, mp = battle.PlayerMp, ap = battle.PlayerAp, packets = 0;
                uint experience = battle.PlayerExperience;
                view.GetComponent<ServerManager>().RequestReceived += _ => packets++;
                Assert.AreEqual("적 전원 처치 (로컬 규칙)", battle.VictoryConditionDescription);
                var root = new UnityEditor.SerializedObject(battle);
                var ids = root.FindProperty("victoryTargetInstanceIds"); ids.arraySize = 1;
                ids.GetArrayElementAtIndex(0).intValue = 800; root.ApplyModifiedPropertiesWithoutUndo();
                battle.TryGetUnit(800, out var foe);
                Assert.AreEqual(foe.Name + " 처치", battle.VictoryConditionDescription);
                root.Update(); ids = root.FindProperty("victoryTargetInstanceIds");
                ids.GetArrayElementAtIndex(0).intValue = 999; root.ApplyModifiedPropertiesWithoutUndo();
                Assert.AreEqual(string.Empty, battle.VictoryConditionDescription);
                Assert.IsFalse(battle.VictoryAchieved);
                var objective = ScriptableObject.CreateInstance<BattleObjectiveDefinition>();
                try
                {
                    var settings = new UnityEditor.SerializedObject(objective);
                    var targets = settings.FindProperty("targetInstanceIds"); targets.arraySize = 1;
                    targets.GetArrayElementAtIndex(0).intValue = 800;
                    settings.FindProperty("description").stringValue = "확인된 목표 설명";
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    Assert.IsTrue(battle.ConfigureObjective(objective));
                    Assert.AreEqual("확인된 목표 설명", battle.VictoryConditionDescription);
                    settings.Update(); settings.FindProperty("description").stringValue = string.Empty;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    Assert.AreEqual(string.Empty, battle.VictoryConditionDescription);
                    Assert.IsFalse(battle.VictoryAchieved);
                    Assert.AreEqual(hp, battle.PlayerHp); Assert.AreEqual(mp, battle.PlayerMp);
                    Assert.AreEqual(ap, battle.PlayerAp); Assert.AreEqual(experience, battle.PlayerExperience);
                    Assert.IsFalse(battle.OutcomeAcknowledged); Assert.AreEqual(0, packets);
                }
                finally { battle.ConfigureObjective(null); UnityEngine.Object.DestroyImmediate(objective); }
            });
        }

        [Test]
        public void SelectedTerrainUsesConfirmedCellAndNeverGuessesUnknownMovement()
        {
            WithBattle((battle, view, enemy) =>
            {
                Assert.IsTrue(view.TryGetSelectedTerrain(out var attribute, out var movement, out _));
                Assert.AreEqual(0, attribute); Assert.IsNull(movement); // No troop catalog: do not assume cost 1.
            });
            WithBattle((battle, view, enemy) =>
            {
                var maps = enemy.gameObject.scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Tilemap>()).ToArray();
                var ground = maps.Single(map => map.name == "Ground");
                var attributes = maps.Single(map => map.name == "Terrain");
                battle.TryGetSelectedUnit(out var unit);
                var cell = new Vector3Int(unit.X, ground.cellBounds.yMax - 1 - unit.Y, 0);
                Assert.IsTrue(view.TryGetSelectedTerrain(out var attribute, out var movement, out _));
                Assert.AreEqual(0, attribute);
                Assert.AreEqual(1, movement);
                int hp = battle.PlayerHp, ap = battle.PlayerAp, mp = battle.PlayerMp, packets = 0;
                view.GetComponent<ServerManager>().RequestReceived += _ => packets++;
                var tile = ScriptableObject.CreateInstance<Tile>();
                try
                {
                    tile.name = "Terrain_04"; attributes.SetTile(cell, tile);
                    Assert.IsTrue(view.TryGetSelectedTerrain(out attribute, out movement, out _));
                    Assert.AreEqual(4, attribute); Assert.AreEqual(2, movement);
                    // A tweened visual elsewhere must not change the inspected confirmed grid cell.
                    var body = maps[0].gameObject.scene.GetRootGameObjects().Single(go => go.name == "Ally");
                    body.transform.position += Vector3.right;
                    Assert.IsTrue(view.TryGetSelectedTerrain(out attribute, out movement, out _));
                    Assert.AreEqual(4, attribute);
                    tile.name = "Terrain_06";
                    Assert.IsTrue(view.TryGetSelectedTerrain(out attribute, out movement, out _));
                    Assert.AreEqual(CombatTerrainTraversal.ImpassableValue, movement);
                    foreach (string invalid in new[] { "Unknown", "Terrain_27", "Terrain_999" })
                    {
                        tile.name = invalid;
                        Assert.IsFalse(view.TryGetSelectedTerrain(out attribute, out movement, out var image));
                        Assert.AreEqual(-1, attribute); Assert.IsNull(movement); Assert.IsNull(image);
                    }
                    attributes.SetTile(cell, null); ground.SetTile(cell, null);
                    Assert.IsFalse(view.TryGetSelectedTerrain(out _, out movement, out _)); Assert.IsNull(movement);
                    Assert.AreEqual(hp, battle.PlayerHp); Assert.AreEqual(ap, battle.PlayerAp);
                    Assert.AreEqual(mp, battle.PlayerMp); Assert.AreEqual(0, packets);
                }
                finally { attributes.SetTile(cell, null); UnityEngine.Object.DestroyImmediate(tile); }
            }, configureTerrain: true);
        }

        [Test]
        public void ToolDisplaySeparatesUnknownEmptyAndExplicitRowsWithoutInventoryMutations()
        {
            WithBattle((battle, view, enemy) =>
            {
                var catalog = ScriptableObject.CreateInstance<DarkSaver.Items.ItemCatalog>();
                try
                {
                    var a = new long[DarkSaver.Items.ItemCatalogEntry.NumericColumnCount]; a[0] = 501;
                    var b = new long[DarkSaver.Items.ItemCatalogEntry.NumericColumnCount]; b[0] = 502;
                    catalog.Initialize(new[] { new DarkSaver.Items.ItemCatalogEntry(a, "", "", "표시_검", "fixture", "fixture"),
                        new DarkSaver.Items.ItemCatalogEntry(b, "", "", "표시_약", "", "fixture") });
                    int hp = battle.PlayerHp, ap = battle.PlayerAp, mp = battle.PlayerMp, packets = 0;
                    view.GetComponent<ServerManager>().RequestReceived += _ => packets++;
                    Assert.IsFalse(view.TryGetToolDisplay(100, out var display)); Assert.AreEqual(0, display.Count);
                    Assert.IsTrue(view.ConfigureToolDisplay(null, 100, Array.Empty<BattleToolDisplayEntry>()));
                    Assert.IsTrue(view.TryGetToolDisplay(100, out display)); Assert.AreEqual(0, display.Count);
                    var rows = new[] { new BattleToolDisplayEntry(501, 2), new BattleToolDisplayEntry(501, 1), new BattleToolDisplayEntry(502, 3) };
                    Assert.IsTrue(view.ConfigureToolDisplay(catalog, 100, rows));
                    rows[0] = new BattleToolDisplayEntry(502, 99);
                    Assert.IsTrue(view.TryGetToolDisplay(100, out display));
                    Assert.AreEqual(3, display.Count); Assert.AreEqual(501, display[0].ItemId); Assert.AreEqual(2, display[0].Count);
                    Assert.IsTrue(view.TryGetToolDescription(100, 0, out var description)); Assert.AreEqual("fixture", description);
                    Assert.IsTrue(view.TryGetToolDescription(100, 2, out description)); Assert.AreEqual("", description);
                    Assert.IsFalse(view.TryGetToolDescription(100, -1, out _));
                    Assert.IsFalse(view.TryGetToolDescription(100, 3, out _));
                    Assert.IsFalse(view.TryGetToolDescription(800, 0, out _));
                    var list = (System.Collections.Generic.IList<BattleToolDisplayEntry>)display;
                    Assert.Throws<NotSupportedException>(() => list[0] = new BattleToolDisplayEntry(502, 50));
                    Assert.IsFalse(view.ConfigureToolDisplay(catalog, 100, null));
                    Assert.IsFalse(view.ConfigureToolDisplay(null, 100, new[] { new BattleToolDisplayEntry(501, 1) }));
                    foreach (var invalid in new[] { new BattleToolDisplayEntry(501, 0), new BattleToolDisplayEntry(501, -1), new BattleToolDisplayEntry(999, 1) })
                        Assert.IsFalse(view.ConfigureToolDisplay(catalog, 100, new[] { invalid }));
                    Assert.IsFalse(view.ConfigureToolDisplay(catalog, 800, rows));
                    Assert.IsFalse(view.TryGetToolDisplay(800, out _));
                    Assert.IsTrue(view.TryGetToolDisplay(100, out display)); Assert.AreEqual(3, display.Count); Assert.AreEqual(2, display[0].Count);
                    Assert.IsTrue(view.TryOpenActionMenu()); Assert.IsTrue(view.TryChooseCommand(BattleMapCommand.Tools));
                    Assert.IsTrue(view.TryCloseBattleWindowShell());
                    Assert.AreEqual(hp, battle.PlayerHp); Assert.AreEqual(ap, battle.PlayerAp);
                    Assert.AreEqual(mp, battle.PlayerMp); Assert.AreEqual(0, packets);
                    view.SuspendPresentation(); Assert.IsFalse(view.TryGetToolDisplay(100, out _));
                    Assert.IsFalse(view.TryGetToolDescription(100, 0, out _));
                }
                finally { UnityEngine.Object.DestroyImmediate(catalog); }
            });
        }

        private static void WithBattle(Action<BattleSceneRoot,BattleMapPresentation,SpriteRenderer> check, bool configureTerrain = false, bool withSoldier = false)
        {
            var scene=EditorSceneManager.NewPreviewScene();var floor=ScriptableObject.CreateInstance<Tile>();
            GameObject Create(string name){var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);return go;}
            try
            {
                var grid=Create("Grid").AddComponent<Grid>();
                var ground=Create("Ground").AddComponent<Tilemap>();ground.transform.SetParent(grid.transform);
                var terrain=Create("Terrain").AddComponent<Tilemap>();terrain.transform.SetParent(grid.transform);
                for(int x=0;x<4;x++)for(int y=0;y<4;y++)ground.SetTile(new Vector3Int(x,y,0),floor);
                var camera=Create("Camera").AddComponent<Camera>();camera.orthographic=true;
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Converted/Characters/Fighter100/Fighter100_Down_1.png");
                SpriteRenderer Body(string name,float x){var body=Create(name).AddComponent<SpriteRenderer>();body.sprite=sprite;body.transform.position=new Vector3(x,2.5f,0);return body;}
                var ally=Body("Ally",1.5f);var enemy=Body("Enemy",2.5f);
                var root=Create("Battle");root.AddComponent<BattleRequestSender>();
                Assert.IsTrue(root.AddComponent<BattlePacketReceiver>().Bind());
                var view=root.AddComponent<BattleMapPresentation>();var battle=root.GetComponent<BattleSceneRoot>();
                if (configureTerrain)
                {
                    var jobs = UnityEditor.AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
                    Assert.IsTrue(battle.ConfigureTraversalCatalogs(new[] { jobs }));
                    Assert.IsTrue(battle.ConfigurePlayerJob(100));
                }
                var bodies=new System.Collections.Generic.List<BattleMapVisual> {new BattleMapVisual(100,ally),new BattleMapVisual(800,enemy)};
                if(withSoldier)
                {
                    Assert.IsTrue(battle.TryRegisterGroup(new[] { new CombatUnit {Id=101,Name="Soldier",Hp=50,MaxHp=50,Mp=0,MaxMp=0} }));
                    bodies.Add(new BattleMapVisual(101,Body("Soldier",.5f)));
                }
                Assert.IsTrue(view.TryBind(ground,terrain,camera,bodies));
                Assert.IsTrue(battle.TrySelectUnit(100));check(battle,view,enemy);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Object.DestroyImmediate(floor); }
        }
    }
}
