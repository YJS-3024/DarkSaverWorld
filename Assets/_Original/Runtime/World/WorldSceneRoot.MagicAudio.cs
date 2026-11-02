using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private readonly MagicSoundPlayback magicSoundPlayback=new MagicSoundPlayback();
        private readonly DeathSoundPlayback deathSoundPlayback=new DeathSoundPlayback();
        private readonly Dictionary<int,AudioSource> magicSoundSources=new Dictionary<int,AudioSource>();
        private void PaintMagicAudio(BattleMagicEffect[] effects)
        {
            foreach(var cue in magicSoundPlayback.Update(effects))PlayNativeSound(cue);
        }
        private void PaintDeathAudio(BattleDeathEffect[] effects)
        {foreach(var cue in deathSoundPlayback.Update(effects))PlayNativeSound(cue);}
        private void PlayNativeSound(MagicSoundCue cue)
        {
            if(cue.stop){if(magicSoundSources.TryGetValue(cue.sound,out var playing))playing.Stop();return;}
            if(!DarkSaver.Original.UI.OriginalClientPreferences.Current.Sound)return;
            var clip=art.MagicSound(cue.sound);if(clip==null)return;
            if(!magicSoundSources.TryGetValue(cue.sound,out var audio))
            {var node=new GameObject("Native sound "+cue.sound);node.transform.SetParent(transform,false);audio=node.AddComponent<AudioSource>();audio.playOnAwake=false;audio.loop=false;audio.spatialBlend=0;magicSoundSources.Add(cue.sound,audio);}
            audio.Stop();audio.clip=clip;audio.time=0;audio.Play();
        }
        private void StopMagicAudio()
        {
            foreach(var audio in magicSoundSources.Values)if(audio!=null){audio.Stop();Destroy(audio.gameObject);}
            magicSoundSources.Clear();magicSoundPlayback.Clear();
            deathSoundPlayback.Clear();
        }
    }
}
