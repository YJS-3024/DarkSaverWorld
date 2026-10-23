using System;
using UnityEngine;
namespace DarkSaver.Combat
{
    public sealed class NativeMagicSoundCatalog : ScriptableObject
    {
        [SerializeField] int[] ids=Array.Empty<int>();[SerializeField] AudioClip[] clips=Array.Empty<AudioClip>();
        public int Count=>ids.Length;
        public void Initialize(int[] soundIds,AudioClip[] sounds)
        {
            if(soundIds==null || sounds==null || soundIds.Length!=sounds.Length || Array.Exists(sounds,x=>x==null))throw new ArgumentException();
            ids=(int[])soundIds.Clone();clips=(AudioClip[])sounds.Clone();
        }
        public AudioClip Get(int id){int index=Array.IndexOf(ids,id);return index<0?null:clips[index];}
    }
}
