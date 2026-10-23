using System;
using System.Collections.Generic;
using System.Globalization;
using DarkSaver.Items;
using DarkSaver.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.UI
{
    // Read-only native data browser, not the player's inventory or an equipment system.
    [DisallowMultipleComponent]
    public sealed class ItemCatalogPanel : MonoBehaviour
    {
        [SerializeField] private ItemCatalog catalog;
        [SerializeField] private TopDownPlayerController player;
        private readonly List<int> matches = new();
        private string query = string.Empty;
        private int selectedId = -1;
        private Vector2 listScroll;
        private Vector2 detailScroll;
        [SerializeField] private bool showDeveloperCatalog;

        public bool IsOpen { get; private set; }
        public ItemCatalog Catalog => catalog;
        public int MatchCount => matches.Count;
        public ItemCatalogEntry SelectedItem => catalog != null && catalog.TryGet(selectedId, out var item) ? item : null;

        public void Configure(ItemCatalog source, TopDownPlayerController controller)
        {
            if (source == null || controller == null) throw new ArgumentNullException("Catalog and player are required.");
            Close();
            catalog = source;
            player = controller;
            Filter(string.Empty);
        }

        public bool Open()
        {
            if (!isActiveAndEnabled || catalog == null || player == null || SceneManager.GetSceneByName("Battle").isLoaded) return false;
            if (IsOpen) return true;
            GetComponent<TownServicesPanel>()?.Close();
            GetComponent<BattleFormationPanel>()?.Cancel();
            GetComponent<MagicCatalogPanel>()?.Close();
            Filter(query);
            IsOpen = true;
            player.SetUiInputBlocked(true);
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
            if (catalog != null)
                for (var i = 0; i < catalog.Count; i++)
                {
                    var item = catalog.GetAt(i);
                    if (search.Length == 0 || item.Id.ToString(CultureInfo.InvariantCulture).IndexOf(search, StringComparison.Ordinal) >= 0 ||
                        (item.Name ?? string.Empty).Replace('_', ' ').IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                        matches.Add(item.Id);
                }
            if (!matches.Contains(selectedId)) selectedId = matches.Count > 0 ? matches[0] : -1;
            listScroll = detailScroll = Vector2.zero;
        }

        public bool SelectItem(int id)
        {
            if (!matches.Contains(id)) return false;
            selectedId = id;
            detailScroll = Vector2.zero;
            return true;
        }

        public static string DisplayName(ItemCatalogEntry item) => string.IsNullOrEmpty(item.Name) ? "(이름 없음)" : item.Name.Replace('_', ' ');

        private void Update()
        {
            if (!showDeveloperCatalog) { Close(); return; }
            if (SceneManager.GetSceneByName("Battle").isLoaded) { Close(); return; }
            var magic = GetComponent<MagicCatalogPanel>();
            if (GetComponent<BattleFormationPanel>()?.IsOpen == true) return;
            if (magic != null && magic.IsOpen) return;
            if (Input.GetKeyDown(KeyCode.I) && (!IsOpen || GUIUtility.keyboardControl == 0)) { if (IsOpen) Close(); else Open(); }
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        private void OnDisable() => Close();

        private void OnGUI()
        {
            if (!showDeveloperCatalog || !IsOpen || SceneManager.GetSceneByName("Battle").isLoaded) return;
            var width = Mathf.Min(760f, Rendering.GameScreen.Width - 24f);
            var height = Mathf.Min(520f, Rendering.GameScreen.Height - 24f);
            if (width < 300f || height < 160f) return;
            var area = new Rect((Rendering.GameScreen.Width - width) / 2f, (Rendering.GameScreen.Height - height) / 2f, width, height);
            var previousColor = GUI.color;
            GUI.color = new Color(.07f, .09f, .11f, 1f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("아이템 카탈로그 — 조회 전용");
            if (GUILayout.Button("닫기 (Esc)", GUILayout.Width(110f))) Close();
            GUILayout.EndHorizontal();
            GUILayout.Label("보유 아이템이 아닙니다. 장착·사용·구매 효과는 연결되지 않았습니다.");
            GUILayout.BeginHorizontal();
            GUILayout.Label("ID / 이름", GUILayout.Width(70f));
            var edited = GUILayout.TextField(query);
            if (edited != query) Filter(edited);
            GUILayout.Label($"{MatchCount} / {catalog.Count}", GUILayout.Width(90f));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.Width(width * .4f));
            foreach (var id in matches)
            {
                catalog.TryGet(id, out var item);
                if (GUILayout.Button($"{(id == selectedId ? "> " : "")}{id}  {DisplayName(item)}", GUILayout.Height(24f))) SelectItem(id);
            }
            GUILayout.EndScrollView();
            detailScroll = GUILayout.BeginScrollView(detailScroll);
            var selected = SelectedItem;
            var wrapped = new GUIStyle(GUI.skin.label) { wordWrap = true };
            if (selected == null) GUILayout.Label("검색 결과가 없습니다.");
            else
            {
                GUILayout.Label($"[{selected.Id}] {DisplayName(selected)}", wrapped);
                GUILayout.Label($"분석 분류: {selected.CategoryLabel}\n가격 데이터: {selected.GetNumericValue(ItemCatalogEntry.PriceColumn).ToString("N0", CultureInfo.InvariantCulture)}\n원문 표기: {selected.PriceNotation}", wrapped);
                GUILayout.Label($"장착 위치 값: {selected.GetNumericValue(3)} / 요구 레벨 값: {selected.GetNumericValue(4)}\n내구 값: {selected.GetNumericValue(5)}\n체력 값: {selected.GetNumericValue(19)}\n공격 값: {selected.GetNumericValue(20)} / 방어 값: {selected.GetNumericValue(21)}", wrapped);
                GUILayout.Label((selected.Description ?? string.Empty).Replace('_', ' '), wrapped);
                GUILayout.Label($"데미지 병종 원문: {selected.DamageTargets}\n표시한 값은 게임 규칙이나 실제 효과를 확정하지 않습니다.", wrapped);
            }
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
