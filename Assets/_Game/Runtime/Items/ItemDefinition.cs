using System;
using UnityEngine;

namespace DarkSaver.Items
{
    // Editable, independent Unity data. Never reads an original table at runtime.
    [CreateAssetMenu(menuName = "DarkSaver/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private ItemCatalogEntry data;
        public ItemCatalogEntry Data => data;
        public void Initialize(ItemCatalogEntry source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var values = new long[ItemCatalogEntry.NumericColumnCount];
            for (int i = 0; i < values.Length; i++) values[i] = source.GetNumericValue(i);
            data = new ItemCatalogEntry(values, source.PriceNotation, source.DamageTargets,
                source.Name, source.Description, source.CategoryLabel);
        }
    }
}
