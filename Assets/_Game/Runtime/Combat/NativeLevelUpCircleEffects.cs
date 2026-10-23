using System;
using UnityEngine;

namespace DarkSaver.Combat
{
    // userunit.dll 10008B70: cumulative ground-circle pieces, separate from global white fill.
    public sealed class NativeLevelUpCircleEffects : ScriptableObject
    {
        public static bool IsFlashCounter(int counter)=>counter>12 && counter<=22 && (counter&1)==0;
        [SerializeField] int magicId;
        [SerializeField] Sprite[] pieces = Array.Empty<Sprite>();
        [SerializeField] Vector2[] pixelOffsets = Array.Empty<Vector2>();
        public bool Supports(int id) => id == magicId && pieces.Length == 12;
        public void Initialize(int id, Sprite[] source, Vector2[] offsets)
        {
            if(id <= 0 || source == null || source.Length != 12 || Array.Exists(source,x=>x==null)
                || offsets == null || offsets.Length != 12) throw new ArgumentException("Invalid level-up circle data.");
            magicId=id; pieces=(Sprite[])source.Clone(); pixelOffsets=(Vector2[])offsets.Clone();
        }
        public static int VisibleCount(int counter) => counter <= 0 || counter >= 22 ? 0 : Math.Min(counter,12);
        public Sprite Piece(int index) => index >= 0 && index < pieces.Length ? pieces[index] : null;
        public Vector2 Offset(int index) => index >= 0 && index < pixelOffsets.Length ? pixelOffsets[index] : Vector2.zero;
    }
}
