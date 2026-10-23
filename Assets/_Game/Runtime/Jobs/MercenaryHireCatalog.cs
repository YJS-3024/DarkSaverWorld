using System;
using UnityEngine;

namespace DarkSaver.Jobs
{
    [Serializable] public sealed class MercenaryHireEntry
    {
        [SerializeField] private int skillId, level, maximum;
        [SerializeField] private string skillName;
        [SerializeField] private int[] jobs;
        public int SkillId => skillId;
        public int Level => level;
        public int Maximum => maximum;
        public string Name => skillName;
        public int[] CopyJobIds() => (int[])jobs.Clone();
        public bool ContainsJob(int jobId) => Array.IndexOf(jobs, jobId) >= 0;
        public MercenaryHireEntry(int id, string name, int rank, int count, int[] jobIds)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(name) || rank < 1 || count < 1 || jobIds == null || jobIds.Length == 0)
                throw new ArgumentException("Invalid hire skill");
            // Source21203/21603 repeat217. Preserve source order/duplicates verbatim.
            foreach (int job in jobIds) if (job <= 0) throw new ArgumentException("Invalid hire job list");
            skillId = id; skillName = name; level = rank; maximum = count; jobs = (int[])jobIds.Clone();
        }
    }
    [CreateAssetMenu(menuName = "DarkSaver/Mercenary Hire Catalog")]
    public sealed class MercenaryHireCatalog : ScriptableObject
    {
        [SerializeField] private MercenaryHireEntry[] entries = Array.Empty<MercenaryHireEntry>();
        public int Count => entries.Length;
        public void Initialize(MercenaryHireEntry[] source)
        {
            if (source == null || source.Length == 0) throw new ArgumentException("Empty hire catalog");
            var copy = (MercenaryHireEntry[])source.Clone(); var seen = new System.Collections.Generic.HashSet<int>();
            foreach (var entry in copy) if (entry == null || !seen.Add(entry.SkillId)) throw new ArgumentException("Duplicate hire skill");
            entries = copy;
        }
        public bool TryGet(int skillId, out MercenaryHireEntry entry)
        {
            foreach (var row in entries) if (row.SkillId == skillId) { entry = row; return true; }
            entry = null; return false;
        }
    }
}
