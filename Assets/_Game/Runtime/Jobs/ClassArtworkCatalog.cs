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
                get { if (!NativePlayerJobs.Contains(jobId) || frames == null || frames.Length != 12) return false;
                    foreach (var sprite in frames) if (sprite == null) return false; return true; }
            }
            [NonSerialized] private Sprite[] correctedFrames;
            public Sprite GetFrame(int index)
            {
                var source=frames[index];
                if(source==null)return null;
                var texture=source.texture;
                // Native player sheets have 3 columns of 32px cells. 192px sheets
                // contain extra attack rows; their four facing rows are still 32px high.
                if(texture==null || texture.width!=96 || texture.height<128)return source;
                var rect=new Rect(index%3*32,texture.height-(index/3+1)*32,32,32);
                if(source.rect==rect)return source;
                if(correctedFrames==null)correctedFrames=new Sprite[12];
                if(correctedFrames[index]==null)
                {
                    var frame=Sprite.Create(texture,rect,new Vector2(.5f,.1f),32,0,SpriteMeshType.FullRect);
                    frame.name=source.name;frame.hideFlags=HideFlags.HideAndDontSave;correctedFrames[index]=frame;
                }
                return correctedFrames[index];
            }
            internal Sprite[] Direction(int row)
            { var copy=new Sprite[3];for(int i=0;i<3;i++)copy[i]=GetFrame(row*3+i);return copy; }
            internal void ReleaseFrames()
            {
                if(correctedFrames==null)return;
                foreach(var frame in correctedFrames)if(frame!=null)
                {if(Application.isPlaying)UnityEngine.Object.Destroy(frame);else UnityEngine.Object.DestroyImmediate(frame);}
                correctedFrames=null;
            }
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
            ReleaseFrames();entries = copy;
        }
        private void ReleaseFrames(){foreach(var entry in entries)entry?.ReleaseFrames();}
        private void OnDisable()=>ReleaseFrames();
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
