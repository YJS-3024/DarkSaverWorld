using System;
using UnityEngine;

namespace DarkSaver.Jobs
{
    [Serializable]
    public sealed class JobCatalogEntry
    {
        public const int NumericColumnCount = 34;
        [SerializeField] private long[] numericValues;
        [SerializeField] private string horizontalTransitionText;
        [SerializeField] private string jobName;
        [SerializeField] private string description;
        [SerializeField] private string categoryLabel;

        public JobCatalogEntry(long[] values, string transitions, string name, string detail, string category)
        {
            if (values == null || values.Length != NumericColumnCount || values[0] < 0 || values[0] > int.MaxValue)
                throw new ArgumentException("Invalid job numeric record.");
            numericValues = (long[])values.Clone();
            horizontalTransitionText = transitions;
            jobName = name;
            description = detail;
            categoryLabel = category;
        }

        public int Id => (int)numericValues[0];
        public long SpriteId => numericValues[1];
        // Preserve lists and sentinel values; this is not an executable promotion rule.
        public string HorizontalTransitionText => horizontalTransitionText;
        public string Name => jobName;
        public string Description => description;
        public string CategoryLabel => categoryLabel;
        public long GetNumericValue(int sourceColumn) => numericValues[sourceColumn];
    }

    [CreateAssetMenu(menuName = "DarkSaver/Job Catalog")]
    public sealed class JobCatalog : ScriptableObject
    {
        [SerializeField] private JobCatalogEntry[] entries = Array.Empty<JobCatalogEntry>();
        public int Count => entries.Length;
        public JobCatalogEntry GetAt(int index) => entries[index];

        public void Initialize(JobCatalogEntry[] source)
        {
            if (source == null || source.Length == 0) throw new ArgumentException("Job catalog is empty.");
            var ordered = (JobCatalogEntry[])source.Clone();
            foreach (var row in ordered) if (row == null) throw new ArgumentException("Null job record.");
            Array.Sort(ordered, (a, b) => a.Id.CompareTo(b.Id));
            for (var i = 1; i < ordered.Length; i++)
                if (ordered[i - 1].Id == ordered[i].Id) throw new ArgumentException("Duplicate job ID.");
            entries = ordered;
        }

        public bool TryGet(int jobId, out JobCatalogEntry entry)
        {
            var low = 0; var high = entries.Length - 1;
            while (low <= high)
            {
                var middle = low + (high - low) / 2;
                var candidate = entries[middle];
                if (candidate.Id == jobId) { entry = candidate; return true; }
                if (candidate.Id < jobId) low = middle + 1;
                else high = middle - 1;
            }
            entry = null;
            return false;
        }
    }
}
