using System;
using System.Collections.Generic;
using System.IO;
using DarkSaver.Combat;
using DarkSaver.Jobs;
using DarkSaver.Magic;
using DarkSaver.Network;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace DarkSaver.EditorTools
{
    // Display fixture only: no grants, profile changes, or original encounter mutations.
    internal static class NativeRosterVisualFixture
    {
        static GameObject root;
        static Tile tile;
        static BattleSceneRoot preview, original;
        static Sprite portrait;
        static MagicCatalog directionCatalog;
        static int step;
        static int rosterSelectStep;
        static bool captured;
        public static bool IsActive => step != 0;
        public static bool TargetEffectsOnly
        {
            get => SessionState.GetBool("DarkSaver.NativeTargetEffectsOnly", false);
            set => SessionState.SetBool("DarkSaver.NativeTargetEffectsOnly", value);
        }

        public static bool Tick(BattleSceneRoot source, BattleMapPresentation sourceView, string output)
        {
            if (step == 0)
            {
                original = source; portrait = sourceView.GetUnitPortrait(100);
                if (portrait == null) throw new InvalidOperationException("Roster fixture portrait missing.");
                original.gameObject.SetActive(false);
                Build(2);
                if (TargetEffectsOnly)
                {
                    TargetEffectsOnly = false; preview.TrySelectUnit(100); preview.TryInspectUnit(100); step = SessionState.GetInt("DarkSaver.NativeTargetEffectStart",50);SessionState.SetInt("DarkSaver.NativeTargetEffectStart",50);
                }
                else step++;
                return true;
            }
            string name = step <= 3 ? "commanders-" + (step + 1)
                : step == 4 ? "commanders-minimized" : step == 5 ? "commanders-restored"
                : step == 6 ? "commanders-closed" : step == 7 ? "commanders-diagnostic"
                : step == 8 ? "commanders-diagnostic-restored" : step == 9 ? "commanders-selected"
                : step == 10 ? "magic-pointer-preview" : step == 11 ? "special-action-menu" : step == 12 ? "special-menu"
                : step == 13 ? "system-action-menu" : step == 14 ? "system-menu" : step == 15 ? "system-menu-closed"
                : step == 16 ? "battle-window-list"
                : step == 29 ? "tools-action-menu" : step == 30 ? "tools-from-action-icon"
                : step == 31 ? "troop-action-menu" : step == 32 ? "troop-from-action-icon"
                : step == 33 ? "empty-magic-action-menu" : step == 34 ? "empty-magic-from-action-icon"
                : step == 35 ? "empty-special-action-menu" : step == 36 ? "empty-special-from-action-icon"
                : step == 37 ? "tools-confirmed-empty" : step == 38 ? "tools-explicit-display-fixture"
                : step == 39 ? "tools-other-unit-unknown"
                : step == 40 ? "heal-action-menu" : step == 41 ? "heal-learned-menu"
                : step == 42 ? "heal-self-result" : step == 43 ? "heal-ally-action-menu"
                : step == 44 ? "heal-ally-menu" : step == 45 ? "heal-ally-result"
                : step == 46 ? "buff-before-hover" : step == 47 ? "buff-hover-active"
                : step == 48 ? "buff-hover-expired" : step == 49 ? "buff-hover-left"
                : step == 50 ? "plasma-right-native" : step == 51 ? "plasma-left-native" : step == 52 ? "plasma-up-native" : step == 53 ? "plasma-down-native"
                : step == 54 ? "shared-charge-native" : step == 55 ? "meteor-area-native" : step == 56 ? "earthquake-area-native"
                : step == 57 ? "level-up-white-native" : step == 58 ? "level-up-after-white-native"
                : "battle-window-" + ShellIds[step - 17];
            // CaptureScreenshot completes at end of frame: do not change the fixture on this tick.
            if (!captured)
            {
                if(step>=50 && step<=53)ShowNativeTargetForCapture(10505,output,49-step);
                if(step>=54 && step<=58)ShowNativeTargetForCapture((short)(step==54?3801:step==55?1001:step==56?901:4001),output);
                if(step==57 || step==58)preview.GetComponent<BattleMapPresentation>().AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*(step==57?8:9));
                if (step == 10)
                {
                    var directionView = preview.GetComponent<BattleMapPresentation>();
                    if (!directionView.RequiresMagicDirection || directionView.MagicDirectionRect != default(Rect)
                        || directionView.MagicPreviewCellCount <= 0)
                        throw new InvalidOperationException("Pointer magic preview or hidden direction panel is invalid.");
                }
                ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png"));
                captured = true; return true;
            }
            captured = false;
            if (step < 3) { DestroyPreview(); Build(step + 2); }
            else if (step == 3)
            {
                Click("native-roster-minimize", output);
                if (!preview.IsAlliedRosterMinimized) throw new InvalidOperationException("Minimize pointer click failed.");
            }
            else if (step == 4)
            {
                Click("restore-collapsed-roster", output);
                if (preview.IsAlliedRosterMinimized) throw new InvalidOperationException("Restore pointer click failed.");
            }
            else if (step == 5)
            {
                Click("native-roster-close", output);
                if (preview.IsAlliedRosterVisible || preview.AlliedRosterRect != default(Rect))
                    throw new InvalidOperationException("Close pointer click failed.");
            }
            else if (step == 6) preview.SetPrototypeHudVisible(true);
            else if (step == 7)
            {
                Click("restore-roster", output);
                if (!preview.IsAlliedRosterVisible) throw new InvalidOperationException("Diagnostic restore click failed.");
            }
            else if (step == 8)
            {
                if(rosterSelectStep==0) {
                    Click("native-roster-100",output);
                    if(preview.SelectedUnitId!=100 || preview.IsAlliedRosterVisible)throw new InvalidOperationException("Exclusive commander information selection failed");
                    preview.OpenAlliedRoster();rosterSelectStep=1;return true;
                }
                preview.TryGetUnit(400, out var before);
                Click("native-roster-400", output);
                preview.TryGetUnit(400, out var after);
                if (preview.SelectedUnitId != 400 || before.Ap != after.Ap)
                    throw new InvalidOperationException("Fourth commander click failed or consumed AP.");
                preview.SetPrototypeHudVisible(false);
            }
            else if (step == 9)
            {
                // Explicit geometry fixture only: not a learned-magic grant and never cast here.
                directionCatalog = ScriptableObject.CreateInstance<MagicCatalog>();
                var values = new long[MagicCatalogEntry.NumericColumnCount];
                values[0] = 701; values[2] = -1; values[3] = 2; values[6] = values[7] = 1;
                directionCatalog.Initialize(new[]{new MagicCatalogEntry(values,"Direction display fixture")});
                var directionView = preview.GetComponent<BattleMapPresentation>();
                if (!directionView.TryBeginNativeMagicTargeting(directionCatalog,
                    new MagicRequest(701,400,0,0,0,Array.Empty<short>()), _ => true, _ => 1, null, null)
                    || !directionView.TryPreviewMagicDirectionAt(new Vector3(7.5f,2.5f,0)))
                    throw new InvalidOperationException("Pointer direction fixture setup failed.");
            }
            else if (step == 10)
            {
                // Catalog art display only. This is NOT this character's confirmed learned-special list.
                var nativeCatalog = AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
                var commands = new List<MagicRequest>(); var lines = new List<string>();
                for (int i=0;i<nativeCatalog.Count && commands.Count<9;i++)
                {
                    var entry=nativeCatalog.GetAt(i);
                    if (!BattleMapPresentation.IsNativeSpecialMenuId(entry.Id)) continue;
                    commands.Add(new MagicRequest((short)entry.Id,400,0,0,0,Array.Empty<short>()));
                    lines.Add(entry.Id+" "+entry.Name+" icon="+entry.IconX+","+entry.IconY);
                }
                var specialView=preview.GetComponent<BattleMapPresentation>(); specialView.CancelMagicTargeting();
                if (commands.Count==0 || !specialView.ConfigureSpecialChoices(nativeCatalog,400,commands,_=>true,_=>1,null,null)
                    || !specialView.TryOpenActionMenu()) throw new InvalidOperationException("Special art fixture setup failed.");
                File.WriteAllLines(Path.Combine(output,"special-menu-fixture.txt"),lines);
            }
            else if (step == 11)
            {
                preview.TryGetUnit(400,out var beforeSpecial);
                Click("radial-5",output);
                var specialView=preview.GetComponent<BattleMapPresentation>();
                preview.TryGetUnit(400,out var afterSpecial);
                if (!specialView.IsMagicChoiceOpen || specialView.DisplayedMagicCount!=specialView.SpecialChoiceCount
                    || specialView.DisplayedMagicCount==0 || afterSpecial.Ap!=beforeSpecial.Ap)
                    throw new InvalidOperationException("Special action icon did not open its separate menu freely.");
            }
            else if (step == 12)
            {
                var systemView=preview.GetComponent<BattleMapPresentation>();systemView.CancelMagicTargeting();
                if(!systemView.TryOpenActionMenu())throw new InvalidOperationException("System action fixture cannot open menu.");
            }
            else if (step == 13)
            {
                preview.TryGetUnit(400,out var beforeSystem);Click("radial-2",output);preview.TryGetUnit(400,out var afterSystem);
                if(!preview.GetComponent<BattleMapPresentation>().IsSystemMenuOpen || beforeSystem.Ap!=afterSystem.Ap)
                    throw new InvalidOperationException("System action click failed or consumed AP.");
            }
            else if (step == 14)
            {
                // Immediate retreat is tested in the real owning scene, never from this display fixture.
                Click("system-back",output);
                if(preview.GetComponent<BattleMapPresentation>().IsSystemMenuOpen)
                    throw new InvalidOperationException("System menu close failed.");
            }
            else if (step == 15)
            {
                var systemView=preview.GetComponent<BattleMapPresentation>();
                if(systemView.IsSystemMenuOpen)
                    throw new InvalidOperationException("Retreat cancellation failed.");
                File.AppendAllText(Path.Combine(output,"roster-pointer-events.txt"),"Isolated menu closed without requesting world return"+Environment.NewLine);
                if (!systemView.TryOpenSystemMenu() || !systemView.TryShowBattleWindowList())
                    throw new InvalidOperationException("Window shell browser failed.");
            }
            else if (step == 16) Click("system-window-0", output);
            else if (step == 29 || step == 31)
            {
                var shellView = preview.GetComponent<BattleMapPresentation>();
                preview.TryGetUnit(400, out var before);
                Click(step == 29 ? "radial-7" : "radial-6", output);
                preview.TryGetUnit(400, out var after);
                int expected = step == 29 ? BattleMapPresentation.ToolsShell : BattleMapPresentation.TroopShell;
                if (!shellView.IsBattleWindowShellOpen || shellView.BattleWindowShellId != expected || before.Ap != after.Ap)
                    throw new InvalidOperationException("Action icon failed to open empty shell without AP cost.");
            }
            else if (step == 30 || step == 32)
            {
                var shellView = preview.GetComponent<BattleMapPresentation>();
                Click("shell-close", output);
                if (shellView.IsSystemMenuOpen) throw new InvalidOperationException("Action shell close did not return directly to battle.");
                if (step == 32)
                {
                    DestroyPreview(); Build(4);
                    shellView = preview.GetComponent<BattleMapPresentation>();
                }
                if (!shellView.TryOpenActionMenu()) throw new InvalidOperationException("Troop action fixture menu failed.");
            }
            else if (step == 33 || step == 35)
            {
                var skillView = preview.GetComponent<BattleMapPresentation>();
                preview.TryGetUnit(400, out var before);
                Click(step == 33 ? "radial-4" : "radial-5", output);
                preview.TryGetUnit(400, out var after);
                int id = step == 33 ? BattleMapPresentation.EmptyMagicShell : BattleMapPresentation.EmptySpecialShell;
                if (!skillView.IsBattleWindowShellOpen || skillView.BattleWindowShellId != id
                    || skillView.IsMagicChoiceOpen || skillView.IsMagicTargeting || before.Ap != after.Ap || before.Mp != after.Mp)
                    throw new InvalidOperationException("Empty skill icon changed resources or enabled casting.");
            }
            else if (step == 34 || step == 36)
            {
                var skillView = preview.GetComponent<BattleMapPresentation>();
                int id = skillView.BattleWindowShellId;
                var window = Resources.Load<DarkSaver.UI.NativeBattleWindows>("UI/BattleWindows/NativeBattleWindows").Find(0);
                int close = Array.FindIndex(window.controls, c => c.nativeId == 38);
                Click("native-window-" + id + "-control-" + close, output);
                if (skillView.IsSystemMenuOpen) throw new InvalidOperationException("Empty skill window did not close to battle.");
                if (step == 36)
                {
                    if (!skillView.ConfigureToolDisplay(null, 400, Array.Empty<BattleToolDisplayEntry>())
                        || !skillView.TryOpenActionMenu() || !skillView.TryChooseCommand(BattleMapCommand.Tools))
                        throw new InvalidOperationException("Confirmed empty tool fixture failed.");
                }
                else if (!skillView.TryOpenActionMenu()) throw new InvalidOperationException("Empty special menu fixture failed.");
            }
            else if (step == 37)
            {
                CheckToolDisplay("보유 아이템이 없습니다.", false);
                var catalog = AssetDatabase.LoadAssetAtPath<DarkSaver.Items.ItemCatalog>("Assets/_Game/Runtime/Items/ItemCatalog.asset");
                if (catalog == null || catalog.Count < 2 || !preview.GetComponent<BattleMapPresentation>().ConfigureToolDisplay(catalog, 400,
                    new[] { new BattleToolDisplayEntry(catalog.GetAt(0).Id, 2), new BattleToolDisplayEntry(catalog.GetAt(1).Id, 3) }))
                    throw new InvalidOperationException("Tool display-only fixture failed. No gameplay inventory was granted.");
                File.AppendAllText(Path.Combine(output, "roster-pointer-events.txt"), "Tool rows are isolated UI input only; no item ownership/use/save mutation" + Environment.NewLine);
            }
            else if (step == 38)
            {
                CheckToolDisplay(null, true);
                if (!preview.TrySelectUnit(100)) throw new InvalidOperationException("Other-unit tool isolation failed.");
            }
            else if (step == 39)
            {
                CheckToolDisplay("소지품 정보를 받지 못했습니다.", false);
                Click("shell-close", output);
                if (preview.GetComponent<BattleMapPresentation>().IsSystemMenuOpen)
                    throw new InvalidOperationException("Tool display close failed.");
                DestroyPreview(); Build(2);
                preview.SetTemporaryLocalRules(true); preview.TrySelectUnit(100);
                var healView = preview.GetComponent<BattleMapPresentation>();
                var member = new BattleFormationMemberDraft { PersistentId = "isolated-healer", CommanderPersistentId = "isolated-healer",
                    CommanderId = 100, Unit = new CombatUnit { Id = 100, JobId = 100, Name = "격리 힐 검증", Hp = 50, MaxHp = 50, X = 1, Y = 17 },
                    IsOwned = true, FinalStateConfirmed = true, Position = new CombatGridCell(1, 17), TeamId = 1,
                    RecordEligible = true, LearnedMagicIds = new short[] { 2401 } };
                var catalog = AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
                if (!BattleFormation.TryCreateSnapshot(new[] { member }, 20, 20, Array.Empty<short>(), out var formation, out var formationReason))
                    throw new InvalidOperationException("Isolated heal formation failed: " + formationReason);
                if (!healView.ConfigureFormationLearnedMagic(catalog, formation)) throw new InvalidOperationException("Isolated heal learned setup failed.");
                preview.ApplyHp(100, 110); preview.AdvanceActionPoints(20);
                if (!healView.TryOpenActionMenu()) throw new InvalidOperationException("Heal action menu failed.");
                File.AppendAllText(Path.Combine(output, "roster-pointer-events.txt"), "Heal learned list is isolated fixture input, not a profile grant." + Environment.NewLine);
            }
            else if (step == 42)
            {
                if (preview.PlayerHp != 120 || preview.PlayerMp != 23)
                    throw new InvalidOperationException("Self heal values or one-time costs failed.");
                preview.AdvanceActionPoints(20);
                if (!preview.GetComponent<BattleMapPresentation>().TryOpenActionMenu()) throw new InvalidOperationException("Second heal menu failed.");
            }
            else if (step == 40 || step == 43)
            {
                Click("radial-4", output);
            }
            else if (step == 41 || step == 44)
            {
                var healView = preview.GetComponent<BattleMapPresentation>();
                if (healView.DisplayedMagicCount != 1 || !healView.TryGetDisplayedMagic(0, out var spell) || spell.Id != 2401)
                    throw new InvalidOperationException("Heal list includes an unlearned entry.");
                Click("spell-0", output);
                var target = root.transform.Find(step == 41 ? "Fixture body 100" : "Fixture body 200");
                if (!healView.IsMagicTargeting || !healView.TryHandleWorldClick(target.position) || healView.IsMagicTargeting)
                    throw new InvalidOperationException("Heal menu-to-target cast failed: " + healView.LastInputMessage);
                if (preview.PlayerAp != 6 || preview.PlayerMp != (step == 41 ? 23 : 16))
                    throw new InvalidOperationException("Heal immediate AP/MP cost failed.");
            }
            else if (step == 45)
            {
                if (!preview.TryGetUnit(200, out var ally) || ally.Hp != 80 || preview.PlayerMp != 16)
                    throw new InvalidOperationException("Ally heal values or one-time costs failed.");
                if (preview.GetComponent<BattleMapPresentation>().ActiveMagicEffectCount != 0)
                    throw new InvalidOperationException("Unknown heal effect was replaced by an attack effect.");
                File.AppendAllText(Path.Combine(output, "roster-pointer-events.txt"), "PASS: learned heal menu -> self HP+10/MP-7/AP-4 -> ally HP+20/MP-7/AP-4; no invented heal effect." + Environment.NewLine);
                var member = new BattleFormationMemberDraft { PersistentId = "isolated-buffer", CommanderPersistentId = "isolated-buffer",
                    CommanderId = 200, Unit = new CombatUnit { Id = 200, JobId = 100, Name = "격리 버프 검증", Hp = 80, MaxHp = 100, X = 2, Y = 17 },
                    IsOwned = true, FinalStateConfirmed = true, Position = new CombatGridCell(2, 17), TeamId = 1,
                    RecordEligible = true, LearnedMagicIds = new short[] { 2101 } };
                var catalog = AssetDatabase.LoadAssetAtPath<MagicCatalog>("Assets/_Game/Runtime/Magic/MagicCatalog.asset");
                if (!BattleFormation.TryCreateSnapshot(new[] { member }, 20, 20, Array.Empty<short>(), out var formation, out var why)
                    || !preview.GetComponent<BattleMapPresentation>().ConfigureFormationLearnedMagic(catalog, formation))
                    throw new InvalidOperationException("Buff fixture learned setup: " + why);
                preview.AdvanceActionPoints(20);
                if (!preview.TryStartMagicRequest(2101, 200, new short[] { 100 }, out why)) throw new InvalidOperationException("Buff fixture cast: " + why);
                preview.TryInspectUnit(100);
            }
            else if (step == 46)
            {
                HoverStat(true);
            }
            else if (step == 47)
            {
                CheckStatDetails(true, true);
                preview.AdvanceActionPoints(GameConstants.BattleTemporaryBuffSeconds);
            }
            else if (step == 48)
            {
                CheckStatDetails(true, false);
                HoverStat(false);
            }
            else if (step == 49)
            {
                CheckStatDetails(false, false);
                File.AppendAllText(Path.Combine(output, "roster-pointer-events.txt"), "PASS: runtime PointerEnter/Leave -> active buff details -> expired base details -> hidden; no profile grant." + Environment.NewLine);
            }
            else if (step >=50 && step<=57)
            {
                preview.GetComponent<BattleMapPresentation>().AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 1000);
            }
            else if (step == 58)
            {
                File.AppendAllText(Path.Combine(output, "roster-pointer-events.txt"), "PASS: requested native magic preview subset completed; no gameplay cost or skill grant. Captured files identify the verified effects." + Environment.NewLine);
                Dispose(); return false;
            }
            else
            {
                var systemView = preview.GetComponent<BattleMapPresentation>();
                int id = ShellIds[step - 17];
                if (!systemView.IsBattleWindowShellOpen || systemView.BattleWindowShellId != id)
                    throw new InvalidOperationException("Wrong shell window " + id);
                if (id >= 1000) Click("shell-close", output);
                else
                {
                    var window = Resources.Load<DarkSaver.UI.NativeBattleWindows>("UI/BattleWindows/NativeBattleWindows").Find(id);
                    int close = Array.FindIndex(window.controls, c => c.nativeId == 38 || c.nativeId == 19 || c.nativeId == 66);
                    if (close < 0) throw new InvalidOperationException("Missing native close button " + id);
                    Click("native-window-" + id + "-control-" + close, output);
                }
                if (systemView.IsBattleWindowShellOpen) throw new InvalidOperationException("Shell close failed " + id);
                if (step - 17 == ShellIds.Length - 1)
                {
                    systemView.CloseSystemMenu();
                    if (!systemView.TryOpenActionMenu()) throw new InvalidOperationException("Tools action fixture menu failed.");
                }
                else if (!systemView.TryOpenBattleWindowShell(ShellIds[step - 16]))
                    throw new InvalidOperationException("Next shell failed.");
            }
            step++; return true;
        }

        static readonly int[] ShellIds = { 0, 3, 4, 5, 6, 7, 8, 17, 28, 1000, 1001, 1002 };

        static void CheckToolDisplay(string status, bool hasRows)
        {
            foreach (var document in preview.GetComponentsInChildren<UIDocument>())
            {
                var use = document.rootVisualElement.Q<Button>("tool-use");
                if (use == null) continue;
                if (use.enabledSelf) throw new InvalidOperationException("Unconnected item use became enabled.");
                var label = document.rootVisualElement.Q<Label>("tool-status");
                var row = document.rootVisualElement.Q<Label>("tool-row-0");
                if (hasRows)
                {
                    if (!preview.TryGetSelectedUnit(out var selected)
                        || !preview.GetComponent<BattleMapPresentation>().TryGetToolDescription(selected.Id, 0, out var description)
                        || row == null || row.tooltip != row.text + "\n" + (string.IsNullOrWhiteSpace(description) ? "설명 데이터 없음" : description))
                        throw new InvalidOperationException("Tool description tooltip differs from source catalog.");
                    if (row == null || row.style.display.value != DisplayStyle.Flex
                        || label != null && label.style.display.value != DisplayStyle.None)
                        throw new InvalidOperationException("Tool rows not visible or old status leaked.");
                }
                else if (label == null || label.text != status || label.style.display.value != DisplayStyle.Flex
                    || row != null && row.style.display.value != DisplayStyle.None)
                    throw new InvalidOperationException("Unknown/empty tool state or owner isolation failed.");
                return;
            }
            throw new InvalidOperationException("Tool display controls missing.");
        }

        static void Click(string key, string output)
        {
            Button button = null;
            foreach (var document in root.GetComponentsInChildren<UIDocument>())
            {
                button = document.rootVisualElement.Q<Button>(key);
                if (button != null) break;
            }
            if (button == null || button.panel == null || !button.enabledInHierarchy
                || button.resolvedStyle.display == DisplayStyle.None || button.worldBound.width <= 0)
                throw new InvalidOperationException("Pointer fixture button not ready: " + key);
            var point = button.worldBound.center;
            var picked = button.panel.Pick(point);
            if (picked != button && !button.Contains(picked))
                throw new InvalidOperationException("Pointer target obstructed: " + key);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = point }))
                button.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = point }))
                button.SendEvent(up);
            File.AppendAllText(Path.Combine(output, "roster-pointer-events.txt"), "Dispatched pointer down/up: " + key + Environment.NewLine);
        }

        static void ShowNativeTargetForCapture(short id, string output,int beamDirection=-1)
        {
            var receiver = preview.GetComponent<BattlePacketReceiver>(); var view = preview.GetComponent<BattleMapPresentation>();
            var before = (preview.PlayerHp, preview.PlayerMp, preview.PlayerAp, preview.EnemyHp);
            bool directional = id == 10806 || id == 10801 || id == 17101;
            bool delayed = id == 10101;
            bool salamander = id==10701 || id==10706;
            bool expanding=id==10401 || id==10406 || id==6601;
            bool thrust=id==10101 || id==10301 || id==10505;
            bool coordinate=id==1001 || id==901,shared=id==3801;
            if(thrust)delayed=false;
            bool groundEffect = id == 10601 || id == 10606;
            bool crossTrust = id == 10201 || id == 10205;
            bool levelUpCircle = id == 4001;
            short targetId = (short)(directional || delayed || groundEffect || salamander || expanding || coordinate || shared ? 200 : 100);
            if (receiver == null || !receiver.TryApplyPacket(new MagicEffectNotification(id, 100, (short)(thrust?beamDirection:coordinate?3:salamander?7:0), (short)(coordinate?17:salamander?16:0), 0, new short[] { targetId })))
                throw new InvalidOperationException("Native target display notification failed.");
            view.AdvanceMagicEffects(0);
            if (directional) view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);
            if (delayed) view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds * 11);
            if (crossTrust) view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*3);
            if (levelUpCircle) view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*6);
            if (salamander) view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*2);
            if(thrust)view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*5);
            if(groundEffect)view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*4);
            if(expanding)view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*6);
            if(id==901 || shared)view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds*4);
            if(id==1001)view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);
            var body = preview.transform.Find("Native Target Effect 100")?.GetComponent<SpriteRenderer>();
            if (id == 2801) body = preview.transform.Find("Native Fireball 100")?.GetComponent<SpriteRenderer>();
            if (directional) body = preview.transform.Find("Native Fireball 200")?.GetComponent<SpriteRenderer>();
            if (delayed) body = preview.transform.Find("Native Target Effect 200")?.GetComponent<SpriteRenderer>();
            if (groundEffect) body = preview.transform.Find("Native Target Effect 200")?.GetComponent<SpriteRenderer>();
            if (crossTrust)
            {
                body = null;
                var crossRoot = preview.transform.Find("Native Cross Trust Caster 100");
                if(crossRoot!=null) foreach(var part in crossRoot.GetComponentsInChildren<SpriteRenderer>()) if(part.enabled){body=part;break;}
            }
            if(levelUpCircle)
            {
                body=null;var circleRoot=preview.transform.Find("Native Level Up Circle 100");
                if(circleRoot!=null)foreach(var part in circleRoot.GetComponentsInChildren<SpriteRenderer>())if(part.enabled){body=part;break;}
            }
            if(salamander)body=preview.transform.Find("Native Salamander Caster 100/Head")?.GetComponent<SpriteRenderer>();
            if(thrust)body=preview.transform.Find("Native Thrust Beam 100/Body")?.GetComponent<SpriteRenderer>();
            if(shared)body=preview.transform.Find("Native Thunder Caster 100")?.GetComponent<SpriteRenderer>();
            if(coordinate)
            {body=null;foreach(var part in preview.GetComponentsInChildren<SpriteRenderer>())if(part.enabled && part.name.StartsWith("Native Coordinate Area "+(id==1001?4:13)+" ")){body=part;break;}}
            if(id==1001)for(int retry=0;body==null && retry<4;retry++)
            {
                view.AdvanceMagicEffects(GameConstants.BattleMagicEffectCounterSeconds);
                foreach(var part in preview.GetComponentsInChildren<SpriteRenderer>())if(part.enabled && part.name.StartsWith("Native Coordinate Area 4 ")){body=part;break;}
            }
            if(expanding)body=preview.transform.Find("Native Expanding Caster 100")?.GetComponent<SpriteRenderer>();
            if(groundEffect)
            {
                body=null;var burstRoot=preview.transform.Find("Native Ground Burst Caster 100");
                if(burstRoot!=null)foreach(var part in burstRoot.GetComponentsInChildren<SpriteRenderer>())if(part.enabled){body=part;break;}
            }
            if (body == null || !body.enabled || body.sprite == null || before != (preview.PlayerHp, preview.PlayerMp, preview.PlayerAp, preview.EnemyHp))
                throw new InvalidOperationException("Native magic "+id+": body="+(body==null?"missing":body.name)+", enabled="+(body!=null && body.enabled)+", sprite="+(body!=null && body.sprite!=null)+", resourcesChanged="+(before!=(preview.PlayerHp,preview.PlayerMp,preview.PlayerAp,preview.EnemyHp))+", active="+view.ActiveMagicEffectCount);
        }

        static VisualElement StatElement(string key)
        {
            foreach (var document in preview.GetComponentsInChildren<UIDocument>())
            {
                var element = document.rootVisualElement.Q<VisualElement>(key);
                if (element != null) return element;
            }
            throw new InvalidOperationException("Stat fixture element missing: " + key);
        }
        static void HoverStat(bool enter)
        {
            var target = StatElement("inspect-attack-tooltip");
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = target.worldBound.center }))
            {
                if (enter) { using (var ev = PointerEnterEvent.GetPooled(move)) { ev.target = target; target.SendEvent(ev); } }
                else { using (var ev = PointerLeaveEvent.GetPooled(move)) { ev.target = target; target.SendEvent(ev); } }
            }
        }
        static void CheckStatDetails(bool visible, bool buff)
        {
            var panel = StatElement("native-stat-detail");
            var label = (Label)StatElement("native-stat-detail-text");
            if ((panel.style.display.value == DisplayStyle.Flex) != visible
                || visible && (label.text.Contains("임시 버프") != buff || !label.text.Contains("기본"))
                || panel.pickingMode != PickingMode.Ignore)
                throw new InvalidOperationException("Runtime buff detail visibility/update failed.");
        }

        static void Build(int count)
        {
            root = new GameObject("Isolated native roster display fixture");
            var grid = new GameObject("Fixture Grid"); grid.transform.SetParent(root.transform); grid.AddComponent<Grid>();
            Tilemap Map(string name)
            {
                var go = new GameObject(name); go.layer = 30;
                go.transform.SetParent(grid.transform); return go.AddComponent<Tilemap>();
            }
            var ground = Map("Fixture Ground"); var terrain = Map("Fixture Terrain");
            tile = ScriptableObject.CreateInstance<Tile>();
            for (int x = 0; x < 20; x++) for (int y = 0; y < 20; y++) ground.SetTile(new Vector3Int(x,y,0), tile);
            var cameraObject = new GameObject("Fixture Camera"); cameraObject.transform.SetParent(root.transform);
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true;
            camera.orthographicSize = GameConstants.WorldCameraOrthographicSize;
            camera.depth = 100; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.055f,.07f,.085f); camera.cullingMask = 1 << 30;
            camera.transform.position = new Vector3(4,3,-10);
            var battleObject = new GameObject("Fixture Battle"); battleObject.transform.SetParent(root.transform);
            preview = battleObject.AddComponent<BattleSceneRoot>();
            var jobs = AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
            if (!preview.ConfigureTraversalCatalogs(new[]{jobs}) || !preview.ConfigurePlayerJob(100))
                throw new InvalidOperationException("Fixture traversal setup failed.");
            var visuals = new List<BattleMapVisual>();
            void Body(short id, int x, int y)
            {
                var go = new GameObject("Fixture body " + id); go.layer = 30; go.transform.SetParent(root.transform);
                go.transform.position = new Vector3(x+.5f,y+.5f,0);
                var body = go.AddComponent<SpriteRenderer>(); body.sprite = portrait;
                visuals.Add(new BattleMapVisual(id,body));
            }
            Body(100,1,2); Body(800,19,19);
            for (int i = 1; i < count; i++)
            {
                short id = (short)((i+1)*100);
                if (!preview.TryRegisterGroup(new[]{new CombatUnit {Id=id,JobId=100,Name="검증 대장 " + (i+1),
                    Hp=50+i*10,MaxHp=100,Mp=5+i,MaxMp=20}})) throw new InvalidOperationException("Fixture registration failed.");
                Body(id,i+1,2);
            }
            battleObject.AddComponent<BattleRequestSender>();
            if (!battleObject.AddComponent<BattlePacketReceiver>().Bind()) throw new InvalidOperationException("Fixture packet binding failed.");
            var view = battleObject.AddComponent<BattleMapPresentation>();
            if (!view.TryBind(ground,terrain,camera,visuals)) throw new InvalidOperationException("Fixture binding failed.");
            if (preview.GetAlliedRosterCommanders().Count != count) throw new InvalidOperationException("Fixture roster count mismatch.");
            foreach (var commander in preview.GetAlliedRosterCommanders())
            {
                int ap = commander.Ap;
                if (!preview.TrySelectRosterUnit(commander.Id) || !preview.TryGetUnit(commander.Id,out var selected) || selected.Ap != ap)
                    throw new InvalidOperationException("Fixture roster selection changed AP or failed.");
            }
            preview.OpenAlliedRoster();rosterSelectStep=0;
        }

        static void DestroyPreview()
        {
            if (root != null) { root.SetActive(false); UnityEngine.Object.Destroy(root); }
            if (tile != null) UnityEngine.Object.Destroy(tile);
            root = null; tile = null; preview = null;
        }
        public static void Dispose()
        {
            DestroyPreview();
            if (directionCatalog != null) UnityEngine.Object.Destroy(directionCatalog);
            directionCatalog = null;
            if (original != null) original.gameObject.SetActive(true);
            original = null; portrait = null; step = 0; captured = false; TargetEffectsOnly = false;
        }
    }
}
