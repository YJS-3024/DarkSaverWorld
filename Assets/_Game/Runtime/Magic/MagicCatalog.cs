using System;
using UnityEngine;

namespace DarkSaver.Magic
{
    [Serializable]
    public sealed class MagicCatalogEntry
    {
        public const int NumericColumnCount = 16;
        [SerializeField] private long[] numericValues;
        [SerializeField] private string magicName;

        public MagicCatalogEntry(long[] values, string name)
        {
            if (values == null || values.Length != NumericColumnCount || values[0] < 0 || values[0] > int.MaxValue)
                throw new ArgumentException("Invalid magic numeric record.");
            numericValues = (long[])values.Clone();
            magicName = name;
        }

        public int Id => (int)numericValues[0];
        public string Name => magicName;
        public long Attribute => GetNumericValue(2);
        public long Range => GetNumericValue(3);
        public long Area => GetNumericValue(4);
        public long Mp => GetNumericValue(5);
        public long RawTime => GetNumericValue(6);
        public long Team => GetNumericValue(7);
        public long Unit => GetNumericValue(8);
        // Original table values, not inferred AP costs or Unity seconds.
        public long RawTick => GetNumericValue(12);
        public long LevelPoints => GetNumericValue(13);
        public long PatternId => GetNumericValue(14);
        public long IconX => GetNumericValue(15);
        public long IconY => GetNumericValue(16);

        public long GetNumericValue(int sourceColumn)
        {
            if (sourceColumn < 0 || sourceColumn > 16 || sourceColumn == 1)
                throw new ArgumentOutOfRangeException(nameof(sourceColumn));
            return numericValues[sourceColumn == 0 ? 0 : sourceColumn - 1];
        }
    }

    [CreateAssetMenu(menuName = "DarkSaver/Magic Catalog")]
    public sealed class MagicCatalog : ScriptableObject
    {
        [SerializeField] private MagicCatalogEntry[] entries = Array.Empty<MagicCatalogEntry>();
        public int Count => entries.Length;
        public MagicCatalogEntry GetAt(int index) => entries[index];

        // Learned spells are retained by explicit catalog ID. A class change adds knowledge;
        // name/level text never authorizes deleting or replacing a previously learned entry.
        public bool TryMergeLearnedMagic(System.Collections.Generic.IReadOnlyList<int> previous,
            System.Collections.Generic.IReadOnlyList<int> added, out int[] result)
        {
            result = Array.Empty<int>();
            if (previous == null || added == null) return false;
            var ids = new System.Collections.Generic.List<int>();
            var seen = new System.Collections.Generic.HashSet<int>();
            foreach (var list in new[] {previous, added}) foreach (int id in list)
            {
                if (id <= 0 || id >= 10000 || !TryGet(id, out _)) return false;
                if (seen.Add(id)) ids.Add(id);
            }
            result = ids.ToArray(); return true;
        }
        public void Initialize(MagicCatalogEntry[] source)
        {
            if (source == null || source.Length == 0) throw new ArgumentException("Magic catalog is empty.");
            var ordered = (MagicCatalogEntry[])source.Clone();
            foreach (var row in ordered) if (row == null) throw new ArgumentException("Null magic record.");
            Array.Sort(ordered, (a, b) => a.Id.CompareTo(b.Id));
            for (var i = 1; i < ordered.Length; i++)
                if (ordered[i - 1].Id == ordered[i].Id) throw new ArgumentException("Duplicate magic ID.");
            entries = ordered;
        }

        public bool TryGet(int magicId, out MagicCatalogEntry entry)
        {
            var low = 0; var high = entries.Length - 1;
            while (low <= high)
            {
                var middle = low + (high - low) / 2;
                var candidate = entries[middle];
                if (candidate.Id == magicId) { entry = candidate; return true; }
                if (candidate.Id < magicId) low = middle + 1;
                else high = middle - 1;
            }
            entry = null;
            return false;
        }
    }
}
