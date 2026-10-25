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
        public bool IsOpen { get; private set; }
        public int MatchCount => matches.Count;
        public MagicCatalogEntry SelectedMagic => catalog != null && catalog.TryGet(selectedId, out var entry) ? entry : null;

        public void Configure(MagicCatalog source, TopDownPlayerController controller)
        {
            if (source == null || controller == null) throw new ArgumentNullException("Catalog and player are required.");
            Close(); catalog = source; player = controller; Filter(string.Empty);
        }

        // Developer catalog UI was removed; gameplay uses native inventory/magic windows.
        public bool Open() => false;

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

        private void OnDisable() => Close();

    }
}
