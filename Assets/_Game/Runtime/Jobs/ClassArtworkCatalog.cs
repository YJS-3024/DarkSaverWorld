using System;
using DarkSaver.Player;
using UnityEngine;

namespace DarkSaver.Jobs
{
    [CreateAssetMenu(menuName = "DarkSaver/Class Artwork Catalog")]
    public sealed class ClassArtworkCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            [SerializeField] private int jobId;
            [SerializeField] private Sprite[] frames;
            public int JobId => jobId;
            public Entry(int id, Sprite[] source) { jobId = id; frames = source == null ? null : (Sprite[])source.Clone(); }
            public bool IsValid {
                get { if (jobId < 100 || jobId > 169 || frames == null || frames.Length != 12) return false;
                    foreach (var sprite in frames) if (sprite == null) return false; return true; }
            }
            public Sprite GetFrame(int index) => frames[index];
            internal Sprite[] Direction(int row) { var copy = new Sprite[3]; Array.Copy(frames, row * 3, copy, 0, 3); return copy; }
        }
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();
        [SerializeField] private ClassArtworkCatalog otherTeamColors;
        [SerializeField] private ClassArtworkCatalog alliedTeamColors;
        public void ConfigureRelationColors(ClassArtworkCatalog other, ClassArtworkCatalog ally)
        {
            if (other == null || ally == null || other == this || ally == this || other == ally)
                throw new ArgumentException("Distinct relation color catalogs required");
            otherTeamColors = other; alliedTeamColors = ally;
        }
        // Explicit confirmed relation: do not infer ownership or alliance from a unit ID.
        public bool TryApplyRelation(int jobId, DirectionalSpriteAnimator target, bool sameTeam, bool allied)
        {
            var source = sameTeam ? this : allied ? alliedTeamColors : otherTeamColors;
            return source != null && source.TryApply(jobId, target);
        }
        public int Count => entries.Length;
        public void Initialize(Entry[] source)
        {
            if (source == null || source.Length == 0) throw new ArgumentException("Empty class artwork");
            var copy = (Entry[])source.Clone(); Array.Sort(copy, (a, b) => (a?.JobId ?? 0).CompareTo(b?.JobId ?? 0));
            for (int i = 0; i < copy.Length; i++)
                if (copy[i] == null || !copy[i].IsValid || i > 0 && copy[i - 1].JobId == copy[i].JobId)
                    throw new ArgumentException("Invalid/duplicate class artwork");
            entries = copy;
        }
        public bool TryGet(int jobId, out Entry entry)
        { foreach (var row in entries) if (row.JobId == jobId && row.IsValid) { entry = row; return true; } entry = null; return false; }
        public bool TryApply(int jobId, DirectionalSpriteAnimator target)
        {
            if (target == null || !TryGet(jobId, out var row)) return false;
            var facing = target.Facing;
            target.Configure(row.Direction(0), row.Direction(1), row.Direction(2), row.Direction(3));
            target.SetMovingPresentation(false); target.TrySetFacing(facing); target.PresentFrame(0); return true;
        }
    }
}
