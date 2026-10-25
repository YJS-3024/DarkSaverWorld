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

        private void OnDisable() => Close();

    }
}
