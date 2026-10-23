using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        private enum SystemPage { Closed, Menu, Controls, Windows, Window, Minimap }
        private SystemPage systemPage;
        private int systemMenuClosedFrame = -1;
        private bool SystemMenuBlocksMapPointer => IsSystemMenuOpen || systemMenuClosedFrame == Time.frameCount;
        // User-confirmed native menu entries; layout and retreat flow remain local implementations.
        public bool IsSystemMenuOpen => isActiveAndEnabled && IsBound && battle != null
            && !battle.IsBattleFinished && systemPage != SystemPage.Closed;
        public Rect SystemMenuRect => IsSystemMenuOpen
            ? IsBattleWindowShellOpen ? BattleWindowShellRect : new Rect((Rendering.GameScreen.Width-300)*.5f,(Rendering.GameScreen.Height-380)*.5f,300,380) : default;

        public bool TryOpenSystemMenu()
        {
            if (!isActiveAndEnabled || !IsBound || battle == null || battle.IsBattleFinished) return false;
            CancelMagicTargeting(); menuUnitId = null; ActiveCommand = BattleMapCommand.Automatic;
            systemPage = SystemPage.Menu;
            LastInputMessage = "전투 메뉴: 전투 시간과 AP 회복은 계속 진행됩니다.";
            return true;
        }
        public void CloseSystemMenu()
        {
            if (systemPage != SystemPage.Closed) systemMenuClosedFrame = Time.frameCount;
            systemPage = SystemPage.Closed;
        }
        public bool TryShowControlHelp()
        {
            if (!IsSystemMenuOpen) return false;
            systemPage = SystemPage.Controls; return true;
        }
        public bool TryRestoreRosterFromSystemMenu()
        {
            if (!IsSystemMenuOpen || systemPage != SystemPage.Menu) return false;
            battle.OpenAlliedRoster(); CloseSystemMenu(); return true;
        }
        public bool TryRetreatImmediately()
        {
            if(!IsSystemMenuOpen || systemPage!=SystemPage.Menu) return false;
            return TryExitCurrentBattle();
        }
        private bool TryExitCurrentBattle()
        {
            var transition = FindAnyObjectByType<BattleTransitionController>();
            var battleScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Battle");
            if (transition == null || transition.IsTransitioning
                || !battleScene.isLoaded || gameObject.scene != battleScene)
            { LastInputMessage = "전투 복귀 연결을 사용할 수 없습니다."; return false; }
            CloseSystemMenu(); transition.ExitBattle(); return true;
        }
        // Shared by Escape/right click and regression checks. Never retreats.
        public bool TryHandleCancelInput(bool openSystemWhenIdle)
        {
            if (!isActiveAndEnabled || !IsBound || battle == null || battle.IsBattleFinished) return false;
            if (IsSystemMenuOpen)
            {
                if (IsBattleWindowShellOpen && shellOpenedFromActionMenu) { CloseSystemMenu(); return true; }
                if (systemPage == SystemPage.Menu) CloseSystemMenu(); else systemPage = SystemPage.Menu;
                return true;
            }
            if (IsToolTargeting || IsMagicTargeting || magicChoiceCaster.HasValue || menuUnitId.HasValue || ActiveCommand != BattleMapCommand.Automatic)
            {
                CancelMagicTargeting(); menuUnitId = null; ActiveCommand = BattleMapCommand.Automatic;
                LastInputMessage = "명령 선택 취소."; return true;
            }
            return openSystemWhenIdle && TryOpenSystemMenu();
        }
        public bool IsMinimapOpen => IsSystemMenuOpen && systemPage == SystemPage.Minimap;
        public bool TryShowMinimap()
        {
            if (!IsSystemMenuOpen || systemPage != SystemPage.Menu) return false;
            systemPage = SystemPage.Minimap; return true;
        }
        private void DrawMinimap(Rect r)
        {
            // Local overview of the bound map. Native minimap artwork/visibility rules remain unverified.
            ui.BattleLabel("minimap-note",new Rect(r.x+16,r.y+66,r.width-32,38),"미니맵 · 임시 표시\n파랑: 아군 / 빨강: 적군",12);
            ui.Raise("minimap-note");
            float scale = Mathf.Min((r.width-32)/Mathf.Max(1,bounds.size.x),190f/Mathf.Max(1,bounds.size.y));
            var area = new Rect(r.x+16,r.y+112,bounds.size.x*scale,bounds.size.y*scale);
            ui.SolidRect("minimap-ground",area,new Color(.12f,.16f,.13f));
            // Sample at most 48x32 cells; never create an element per tile on a large map.
            int sx=Mathf.Max(1,Mathf.CeilToInt(bounds.size.x/48f)),sy=Mathf.Max(1,Mathf.CeilToInt(bounds.size.y/32f));
            for(int y=bounds.yMin;y<bounds.yMax;y+=sy)
                for(int x=bounds.xMin;x<bounds.xMax;x+=sx)
                {
                    var cell=new Vector3Int(x,y,0);
                    if(ground == null || !ground.HasTile(cell)) continue;
                    var color=terrain != null && terrain.HasTile(cell) ? new Color(.43f,.38f,.24f) : new Color(.29f,.4f,.28f);
                    ui.SolidRect("minimap-tile-"+x+"-"+y,new Rect(area.x+(x-bounds.xMin)*scale,
                        area.y+(bounds.yMax-y-Mathf.Min(sy,bounds.yMax-y))*scale,
                        Mathf.Min(sx,bounds.xMax-x)*scale,Mathf.Min(sy,bounds.yMax-y)*scale),color);
                }
            foreach(var visual in visuals)
            {
                if(visual.Body == null || !visual.Body.enabled || !battle.TryGetUnit(visual.UnitId,out var unit) || !unit.Alive) continue;
                var cell=ground.WorldToCell(visual.Body.transform.position);
                if(!bounds.Contains(cell)) continue;
                ui.SolidRect("minimap-unit-"+unit.Id,new Rect(area.x+(cell.x-bounds.xMin+.5f)*scale-2,
                    area.y+(bounds.yMax-cell.y-.5f)*scale-2,4,4),unit.Enemy ? Color.red : Color.cyan);
            }
        }
        private void DrawSystemMenu()
        {
            if (!IsSystemMenuOpen) return;
            ui.ModalBlocker("system-modal-input");
            if (IsBattleWindowShellOpen) { DrawBattleWindowShell(); return; }
            var r = SystemMenuRect;
            ui.BattlePanel("system-panel",r,"전투 메뉴");
            ui.BattleLabel("system-note",new Rect(r.x+12,r.y+29,r.width-24,34),"전투는 계속 진행됩니다.",12);
            if (systemPage == SystemPage.Menu)
            {
                ui.Button("system-roster",new Rect(r.x+16,r.y+68,r.width-32,32),"정보창 보기",()=>TryRestoreRosterFromSystemMenu());
                ui.Button("system-alliance",new Rect(r.x+16,r.y+106,r.width-32,32),"동맹",()=>TryOpenBattleWindowShell(3));
                ui.Button("system-victory",new Rect(r.x+16,r.y+144,r.width-32,32),"승리조건",()=>TryOpenBattleWindowShell(5));
                ui.Button("system-controls",new Rect(r.x+16,r.y+182,r.width-32,32),"도움말",()=>TryOpenBattleWindowShell(7));
                ui.Button("system-minimap",new Rect(r.x+16,r.y+220,r.width-32,32),"맵정보",()=>TryShowMinimap());
                ui.Button("system-retreat",new Rect(r.x+16,r.y+258,r.width-32,32),"도망",()=>TryRetreatImmediately());
            }
            else if (systemPage == SystemPage.Windows) { }
            else if (systemPage == SystemPage.Minimap) { }
            else if (systemPage == SystemPage.Controls)
                ui.BattleLabel("system-help",new Rect(r.x+16,r.y+68,r.width-32,112),
                    "아군 클릭: 행동 메뉴\n이동·공격 선택 후 맵 클릭\n마법: 아이콘 선택 후 대상/방향 클릭\nEsc·우클릭: 선택 취소 / 이전 메뉴",13);
            ui.Button("system-back",new Rect(r.x+16,r.yMax-48,r.width-32,32),
                systemPage == SystemPage.Menu ? "닫기 (Esc)" : "이전 메뉴",()=>TryHandleCancelInput(false));
            // The retained blocker is raised every frame. Raise the retained
            // foreground too, otherwise the next frame's clicks hit the blocker.
            foreach (var key in new[] { "system-panel", "system-panel-title", "system-note", "system-roster",
                "system-controls", "system-alliance", "system-victory", "system-minimap", "system-retreat", "system-windows", "system-help", "system-back" })
                ui.Raise(key);
            if (systemPage == SystemPage.Windows) DrawBattleWindowList(r);
            if (systemPage == SystemPage.Minimap) DrawMinimap(r);
        }
    }
}
