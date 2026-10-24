using System;
using System.Collections.Generic;
using System.Globalization;
using DarkSaver.Magic;
using DarkSaver.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.UI
{
    // Native read-only browser; not a learned-spell list or a casting command.
    [DisallowMultipleComponent]
    public sealed class MagicCatalogPanel : MonoBehaviour
    {
        [SerializeField] private MagicCatalog catalog;
        public MagicCatalog Catalog => catalog;
        [SerializeField] private TopDownPlayerController player;
        private readonly List<int> matches = new();
        private string query = string.Empty;
        private int selectedId = -1;
        private Vector2 listScroll;
        private Vector2 detailScroll;
        [SerializeField] private bool showDeveloperCatalog;
        public bool IsOpen { get; private set; }
        public int MatchCount => matches.Count;
        public MagicCatalogEntry SelectedMagic => catalog != null && catalog.TryGet(selectedId, out var entry) ? entry : null;

        public void Configure(MagicCatalog source, TopDownPlayerController controller)
        {
            if (source == null || controller == null) throw new ArgumentNullException("Catalog and player are required.");
            Close(); catalog = source; player = controller; Filter(string.Empty);
        }

        public bool Open()
        {
            if (FirstPlayTutorial.BlocksWorldActions) return false;
            if (!isActiveAndEnabled || catalog == null || player == null || SceneManager.GetSceneByName("Battle").isLoaded) return false;
            if (IsOpen) return true;
            GetComponent<TownServicesPanel>()?.Close();
            GetComponent<BattleFormationPanel>()?.Cancel();
            GetComponent<ItemCatalogPanel>()?.Close();
            Filter(query); IsOpen = true; player.SetUiInputBlocked(true);
            return true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (player != null) player.SetUiInputBlocked(false);
        }

        public void Filter(string text)
        {
            query = text ?? string.Empty;
            var search = query.Trim().Replace('_', ' ');
            matches.Clear();
            if (catalog != null) for (var i = 0; i < catalog.Count; i++)
            {
                var entry = catalog.GetAt(i);
                if (search.Length == 0 || entry.Id.ToString(CultureInfo.InvariantCulture).IndexOf(search, StringComparison.Ordinal) >= 0 ||
                    (entry.Name ?? string.Empty).Replace('_', ' ').IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    matches.Add(entry.Id);
            }
            if (!matches.Contains(selectedId)) selectedId = matches.Count > 0 ? matches[0] : -1;
            listScroll = detailScroll = Vector2.zero;
        }

        public bool SelectMagic(int id)
        {
            if (!matches.Contains(id)) return false;
            selectedId = id; detailScroll = Vector2.zero; return true;
        }

        public static string DisplayName(MagicCatalogEntry entry) => string.IsNullOrEmpty(entry.Name) ? "(이름 없음)" : entry.Name.Replace('_', ' ');
        public static string DetailText(MagicCatalogEntry entry) => entry == null ? "검색 결과가 없습니다." :
            $"[{entry.Id}] {DisplayName(entry)}\nMP 원표: {entry.Mp}\n거리 원표: {entry.Range} / 범위 원표: {entry.Area}\n" +
            $"시간 원표: {entry.RawTime} (초 단위 미확정)\nTICK 원표: {entry.RawTick} (최종 AP 비용 아님)\n" +
            $"속성: {entry.Attribute} / 팀: {entry.Team} / 유닛: {entry.Unit}\n" +
            $"EA: {entry.GetNumericValue(9)} / EB: {entry.GetNumericValue(10)} / EC: {entry.GetNumericValue(11)}\n" +
            $"LVPT: {entry.LevelPoints} / 패턴 ID: {entry.PatternId}\n아이콘 좌표: {entry.IconX}, {entry.IconY}";

        private void Update()
        {
            if (!showDeveloperCatalog) { Close(); return; }
            if (SceneManager.GetSceneByName("Battle").isLoaded) { Close(); return; }
            var items = GetComponent<ItemCatalogPanel>();
            if (GetComponent<BattleFormationPanel>()?.IsOpen == true) return;
            if (items != null && items.IsOpen) return;
            if (Input.GetKeyDown(KeyCode.M) && (!IsOpen || GUIUtility.keyboardControl == 0)) { if (IsOpen) Close(); else Open(); }
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }
        private void OnDisable() => Close();

        private void OnGUI()
        {
            if (!showDeveloperCatalog || !IsOpen || SceneManager.GetSceneByName("Battle").isLoaded) return;
            var width = Mathf.Min(760f, Rendering.GameScreen.Width - 24f); var height = Mathf.Min(520f, Rendering.GameScreen.Height - 24f);
            if (width < 300f || height < 160f) return;
            var area = new Rect((Rendering.GameScreen.Width - width) / 2f, (Rendering.GameScreen.Height - height) / 2f, width, height);
            var previous = GUI.color;
            GUI.color = new Color(.07f, .09f, .11f, 1f); GUI.DrawTexture(area, Texture2D.whiteTexture); GUI.color = previous;
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.BeginHorizontal(); GUILayout.Label("마법 카탈로그 — 조회 전용");
            if (GUILayout.Button("닫기 (Esc)", GUILayout.Width(110f))) Close();
            GUILayout.EndHorizontal();
            GUILayout.Label("습득·시전 목록이 아닙니다. AP 환산과 마법 효과는 미연결입니다.");
            GUILayout.BeginHorizontal(); GUILayout.Label("ID / 이름", GUILayout.Width(70f));
            var edited = GUILayout.TextField(query); if (edited != query) Filter(edited);
            GUILayout.Label($"{MatchCount} / {catalog.Count}", GUILayout.Width(90f)); GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.Width(width * .4f));
            foreach (var id in matches)
            {
                catalog.TryGet(id, out var entry);
                if (GUILayout.Button($"{(id == selectedId ? "> " : "")}{id}  {DisplayName(entry)}", GUILayout.Height(24f))) SelectMagic(id);
            }
            GUILayout.EndScrollView(); detailScroll = GUILayout.BeginScrollView(detailScroll);
            GUILayout.Label(DetailText(SelectedMagic), new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.EndScrollView(); GUILayout.EndHorizontal(); GUILayout.EndArea();
        }
    }
}
