using System;
using UnityEngine;

namespace DarkSaver.Items
{
    [Serializable]
    public sealed class ItemCatalogEntry
    {
        public const int NumericColumnCount = 41;
        public const int PriceColumn = 16;
        [SerializeField] private long[] numericValues;
        [SerializeField] private string priceNotation;
        [SerializeField] private string damageTargets;
        [SerializeField] private string itemName;
        [SerializeField] private string description;
        [SerializeField] private string categoryLabel;

        public ItemCatalogEntry(long[] values, string priceText, string targets, string name, string detail, string category)
        {
            if (values == null || values.Length != NumericColumnCount || values[0] < 0 || values[0] > int.MaxValue)
                throw new ArgumentException("Invalid item numeric record.");
            numericValues = (long[])values.Clone();
            priceNotation = priceText;
            damageTargets = targets;
            itemName = name;
            description = detail;
            categoryLabel = category;
        }

        public int Id => (int)numericValues[0];
        public string PriceNotation => priceNotation;
        public string DamageTargets => damageTargets;
        public string Name => itemName;
        public string Description => description;
        // Analysis catalog annotation only; not an equipment/use rule.
        public string CategoryLabel => categoryLabel;
        public long GetNumericValue(int sourceColumn) => numericValues[sourceColumn];
    }

    [CreateAssetMenu(menuName = "DarkSaver/Item Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [SerializeField] private ItemCatalogEntry[] entries = Array.Empty<ItemCatalogEntry>();
        public int Count => entries.Length;
        public ItemCatalogEntry GetAt(int index) => entries[index];

        public void Initialize(ItemCatalogEntry[] source)
        {
            if (source == null || source.Length == 0) throw new ArgumentException("Item catalog is empty.");
            var ordered = (ItemCatalogEntry[])source.Clone();
            foreach (var row in ordered) if (row == null) throw new ArgumentException("Null item record.");
            Array.Sort(ordered, (a, b) => a.Id.CompareTo(b.Id));
            for (var i = 1; i < ordered.Length; i++)
                if (ordered[i - 1].Id == ordered[i].Id) throw new ArgumentException("Duplicate item ID.");
            entries = ordered;
        }

        public bool TryGet(int itemId, out ItemCatalogEntry entry)
        {
            var low = 0; var high = entries.Length - 1;
            while (low <= high)
            {
                var middle = low + (high - low) / 2;
                var row = entries[middle];
                if (row.Id == itemId) { entry = row; return true; }
                if (row.Id < itemId) low = middle + 1; else high = middle - 1;
            }
            entry = null;
            return false;
        }
    }
}
