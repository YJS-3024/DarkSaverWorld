using DarkSaver.UI;
using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        private NativeBattleWindows nativeWindows;
        private int shellWindowId;
        private TextAsset nativeBattleHelp;
        private bool shellMinimized;
        public bool IsBattleWindowMinimized => IsBattleWindowShellOpen && shellMinimized;
        public bool TryMinimizeBattleWindow()
        {
            if (!IsBattleWindowShellOpen || shellMinimized || ShellLayout == null || ShellLayout.minimizedBackground == null) return false;
            shellMinimized = true; return true;
        }
        public bool TryRestoreBattleWindow()
        {
            if (!IsBattleWindowMinimized) return false;
            shellMinimized = false; return true;
        }
        private bool shellOpenedFromActionMenu;
        private short? troopOrderUnitId;
        public const int OptionsShell = 1000, ToolsShell = 1001, TroopShell = 1002;
        public const int EmptyMagicShell = 1003, EmptySpecialShell = 1004;
        private bool IsEmptySkillShell => shellWindowId == EmptyMagicShell || shellWindowId == EmptySpecialShell;
        private NativeBattleWindows.Window ShellLayout => nativeWindows != null ? nativeWindows.Find(IsEmptySkillShell ? 0 : shellWindowId) : null;
        public bool IsBattleWindowShellOpen => IsSystemMenuOpen && systemPage == SystemPage.Window;
        public int BattleWindowShellId => IsBattleWindowShellOpen ? shellWindowId : -1;

        public bool TryOpenBattleWindowShell(int id, bool fromActionMenu = false)
        {
            if (fromActionMenu && !IsActionMenuOpen) return false;
            if (nativeWindows == null) nativeWindows = Resources.Load<NativeBattleWindows>("UI/BattleWindows/NativeBattleWindows");
            if ((id < OptionsShell || id > EmptySpecialShell) && (nativeWindows == null || nativeWindows.Find(id) == null)) return false;
            if ((id == EmptyMagicShell || id == EmptySpecialShell) && (nativeWindows == null || nativeWindows.Find(0) == null)) return false;
            if (!TryOpenSystemMenu()) return false;
            shellMinimized = false; shellWindowId = id; systemPage = SystemPage.Window;
            if(id==ToolsShell){toolDisplayOffset=0;if(toolSession!=null)TryBindSessionToolDisplay(toolSession);}
            troopOrderUnitId = id == TroopShell && battle.TryGetSelectedUnit(out var issuer) ? issuer.Id : (short?)null;
            shellOpenedFromActionMenu = fromActionMenu;
            LastInputMessage = id == TroopShell ? "용병 명령을 선택하세요. Esc/우클릭: 취소." : "UI 틀 미리보기 · 내용과 실행 기능은 연결하지 않았습니다.";
            return true;
        }
        public bool TryCloseBattleWindowShell()
        {
            if (!IsBattleWindowShellOpen) return false;
            if (shellOpenedFromActionMenu) CloseSystemMenu();
            else systemPage = SystemPage.Menu;
            return true;
        }
        public bool TryShowBattleWindowList()
        {
            if (!IsSystemMenuOpen) return false;
            systemPage = SystemPage.Windows; return true;
        }
        private Rect BattleWindowShellRect
        {
            get
            {
                var window = ShellLayout;
                if ((shellWindowId == TroopShell || shellWindowId == ToolsShell) && view != null && battle.TryGetSelectedUnit(out var selected))
                {
                    var body = visuals.Find(v => v.UnitId == selected.Id).Body;
                    if (body != null)
                    {
                        var screen = view.WorldToScreenPoint(GetUnitMenuAnchor(body));
                        var blocked = new System.Collections.Generic.List<Rect> {battle.BattleHudRect,battle.AlliedRosterRect};
                        if (battle.TryGetInspectedUnit(out _)) blocked.Add(battle.UnitInfoRect);
                        if (screen.z > 0 && TryPlaceActionMenuRect(Rendering.GameScreen.ScreenToGui(screen),
                            new Vector2(Rendering.GameScreen.Width,Rendering.GameScreen.Height),blocked,out var bounds))
                            return shellWindowId == TroopShell ? new Rect(bounds.x+8,bounds.y,80,96) : bounds;
                    }
                }
                var size = window != null ? (Vector2)(shellMinimized ? window.minimizedSize : window.size)
                    : shellWindowId == TroopShell ? new Vector2(80, 96) : shellWindowId == ToolsShell ? new Vector2(96,96) : new Vector2(320, 240);
                return new Rect((Rendering.GameScreen.Width - size.x) * .5f,
                    Mathf.Max(0, (Rendering.GameScreen.Height - 24 - size.y) * .5f), size.x, size.y);
            }
        }
        private void DrawBattleWindowList(Rect r)
        {
            int[] ids = { 0, 3, 4, 5, 6, 7, 8, 17, 28, OptionsShell, ToolsShell, TroopShell };
            string[] names = { "마법 빈 창", "동맹", "미식별 목록", "승리 조건", "지형 정보", "원본 도움말", "결과 보고", "경험치 결과", "패배 확인", "옵션 (임시 틀)", "도구", "용병 명령" };
            for (int i = 0; i < ids.Length; i++)
            {
                int id = ids[i];
                ui.Button("system-window-" + id, new Rect(r.x + 12 + i % 2 * 140, r.y + 66 + i / 2 * 28, 136, 26),
                    names[i], () => TryOpenBattleWindowShell(id), fontSize: 12);
                ui.Raise("system-window-" + id);
            }
        }
        private void DrawAlliedWindowNames(Rect r)
        {
            var commanders=battle.GetAlliedRosterCommanders();
            for(int i=0;i<Mathf.Min(4,commanders.Count);i++)
            {
                ui.NativeBitmapText("alliance-name-"+i,new Rect(r.x+12,r.y+51+19*i,105,17),commanders[i].Name,
                    new Color(.17f,.09f,.035f),alignment:TextAnchor.UpperLeft);
                ui.Raise("alliance-name-"+i);
            }
        }
        public bool TryChooseTroopOrder(BattleSceneRoot.TroopOrder order)
        {
            if (!isActiveAndEnabled || !IsBound || !IsBattleWindowShellOpen || shellWindowId != TroopShell
                || !battle.TryGetSelectedUnit(out var unit) || troopOrderUnitId != unit.Id || !battle.TrySetTroopOrder(unit.Id, order)) return false;
            LastInputMessage = "용병 명령을 변경했습니다.";
            CloseSystemMenu();
            return true;
        }
        private void DrawTroopOrders(Rect r)
        {
            bool selected = battle.TryGetSelectedUnit(out var unit);
            bool enabled = selected && troopOrderUnitId == unit.Id && battle.TemporaryLocalRulesEnabled && !battle.IsBattleFinished
                && battle.TryGetGroup(unit.Id, out var group) && group.CommanderAlive && group.SoldierCount > 0;
            var current = BattleSceneRoot.TroopOrder.Direct;
            if (selected) battle.TryGetTroopOrder(unit.Id, out current);
            var orders = new[] { BattleSceneRoot.TroopOrder.Direct, BattleSceneRoot.TroopOrder.AutomaticAttack, BattleSceneRoot.TroopOrder.AutomaticDefense };
            string[] labels = { "직접 명령", "자동 공격", "자동 방어" };
            for (int i = 0; i < orders.Length; i++)
            {
                var order = orders[i];
                var button = new Rect(r.x + (i == 0 ? 24 : i == 1 ? 0 : 48), r.y + (i == 0 ? 0 : 64), 32, 32);
                ui.ActionButton("troop-order-" + (int)order, button, labels[i], () => TryChooseTroopOrder(order), enabled,
                    labels[i] + (current == order ? " (현재 명령)" : ""), BattleIconFrames.Magic(1, i, Time.unscaledTimeAsDouble));
                ui.Raise("troop-order-" + (int)order);
            }
        }
        private void DrawBattleWindowShell()
        {
            var r = BattleWindowShellRect;
            var window = ShellLayout;
            if (window != null && shellMinimized)
            {
                ui.NativeMinimizedWindow("native-window-minimized", r, window.minimizedBackground, () => TryRestoreBattleWindow());
                return;
            }
            if (window != null)
            {
                ui.NativeBattleWindow("native-window-" + shellWindowId, r, window, () => TryCloseBattleWindowShell(),
                    window.minimizedBackground != null ? () => TryMinimizeBattleWindow() : null);
                if (shellWindowId == 5)
                    ui.NativeBattleWindowText("victory-condition", new Rect(r.x + 10, r.y + 28, r.width - 20, r.height - 33),
                        battle.VictoryConditionDescription);
                if (shellWindowId == 6) DrawSelectedTerrainInfo(r);
                if (shellWindowId == 3) DrawAlliedWindowNames(r);
                if (shellWindowId == 7) {
                    if(nativeBattleHelp==null) nativeBattleHelp=Resources.Load<TextAsset>("UI/BattleWindows/NativeBattleHelp");
                    var up=System.Array.Find(window.controls,c=>c.nativeId==40);
                    var down=System.Array.Find(window.controls,c=>c.nativeId==39);
                    if(up!=null && down!=null)
                        ui.NativeBitmapScrollText("native-battle-help",new Rect(r.x+14,r.y+up.rect.y,
                            up.rect.xMax-14,down.rect.yMax-up.rect.y),
                            nativeBattleHelp!=null ? nativeBattleHelp.text : "도움말 자산을 찾을 수 없습니다.",up,down);
                }
                if (IsEmptySkillShell)
                {
                    ui.BattleLabel("empty-skill-title", new Rect(r.x + 9, r.y + 3, 76, 20),
                        shellWindowId == EmptySpecialShell ? "특수기" : "마법", 12, singleLine: true);
                    ui.Raise("empty-skill-title");
                }
            }
            else if (shellWindowId == TroopShell) DrawTroopOrders(r);
            else if (shellWindowId == ToolsShell) DrawToolDisplay(r);
            else
            {
                string title = shellWindowId == ToolsShell ? "도구" : shellWindowId == TroopShell ? "부대 지휘" : "옵션";
                ui.BattlePanel("shell-panel", r, title + (shellWindowId == TroopShell ? "" : " · 임시 UI 틀"));
                // Empty regions only: do not fabricate inventory, orders, or option values.
                ui.BattlePanel("shell-content", new Rect(r.x + 12, r.y + 34, r.width - 24, r.height - 88));
                ui.Button("shell-close", new Rect(r.x + r.width - 92, r.yMax - 42, 80, 28), "닫기", () => TryCloseBattleWindowShell());
                foreach (var key in new[] { "shell-panel", "shell-panel-title", "shell-content", "shell-close" }) ui.Raise(key);
            }
            ui.Label("shell-note", new Rect(0, Rendering.GameScreen.Height - 22, Rendering.GameScreen.Width, 20),
                (shellWindowId == ToolsShell ? "사용품 선택 → 효과 대상 지정 · Esc/우클릭: 취소"
                    : shellWindowId == TroopShell ? "선택한 부대의 용병 명령"
                    : shellWindowId == 3 ? "현재 로컬 아군 지휘관 · 동맹 변경 기능 미연결"
                    : shellWindowId == 7 ? "원본 help.txt 내용 · 온라인 운영 규정 포함"
                    : shellWindowId == 6 ? SelectedTerrainNote : shellWindowId == 5 ? "현재 전투의 승리 조건 · 안내 전용"
                    : IsEmptySkillShell ? "표시할 습득 목록 없음 · 임시 빈 창" : "UI 틀 · 내용/기능 미연결")
                    + " · Esc/우클릭: " + (shellOpenedFromActionMenu ? "창 닫기" : "전투 메뉴로"), 12);
            ui.Raise("shell-note");
        }
    }
}
