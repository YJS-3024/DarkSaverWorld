using System.Collections.Generic;
using UnityEngine;
namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        sealed class MagicTargetAudio{public NativeMagicPresentationCatalog.Entry Entry;public bool Travel,Impact;public int Epoch=-1;}
        sealed class MagicCasterAudio{public NativeMagicPresentationCatalog.Entry Entry;public float Age;public int Sound;public bool Started,Ended;}
        readonly Dictionary<short,MagicTargetAudio> magicTargetAudio=new();
        readonly Dictionary<short,MagicCasterAudio> magicCasterAudio=new();
        readonly Dictionary<int,AudioSource> magicSoundSources=new();
        NativeMagicSoundCatalog magicSoundCatalog;
        public int NativeMagicSoundStartCount{get;private set;}
        public int LastNativeMagicSoundId{get;private set;}
        void PlayMagicSound(int id)
        {
            if(id<=0)return;magicSoundCatalog??=Resources.Load<NativeMagicSoundCatalog>("Effects/NativeMagicSoundCatalog");
            var clip=magicSoundCatalog!=null?magicSoundCatalog.Get(id):null;if(clip==null)return;
            if(!magicSoundSources.TryGetValue(id,out var source))
            {
                var go=new GameObject("Native Magic Sound "+id);go.transform.SetParent(transform,false);source=go.AddComponent<AudioSource>();
                source.playOnAwake=false;source.spatialBlend=0;source.loop=false;source.clip=clip;magicSoundSources.Add(id,source);
            }
            source.volume=Mathf.Clamp01(GameConstants.NativeMagicSoundVolume);
            source.Stop();source.time=0;if(Application.isPlaying)source.Play();NativeMagicSoundStartCount++;LastNativeMagicSoundId=id;
        }
        void StopMagicSound(int id){if(magicSoundSources.TryGetValue(id,out var source) && source!=null)source.Stop();}
        void BeginMagicAudio(int id,short caster,IReadOnlyList<short> targets)
        {
            presentationCatalog??=Resources.Load<NativeMagicPresentationCatalog>("Effects/NativeMagicPresentationCatalog");
            if(magicCasterAudio.TryGetValue(caster,out var old))StopMagicSound(old.Sound);magicCasterAudio.Remove(caster);
            if(!IsBound || presentationCatalog==null || !presentationCatalog.TryGet(id,out var entry) || entry.Family==0 || entry.Family==99)return;
            if(entry.CasterLifetime>0)magicCasterAudio[caster]=new MagicCasterAudio{Entry=entry,Sound=entry.Family==12?entry.Sound1:entry.Sound2};
            foreach(short target in targets)if(entry.Family!=16 && visuals.Find(v=>v.UnitId==target).Body!=null)magicTargetAudio[target]=new MagicTargetAudio{Entry=entry};
        }
        void AdvanceMagicTargetAudio(short target,int counter,int epoch=-1)
        {
            if(counter<0 || !magicTargetAudio.TryGetValue(target,out var state))return;
            int family=state.Entry.Family;bool projectile=family==1 || family==10 || family==11 || family==24;
            int impact=projectile || family==6 || family==14 || family==15 || family==18 || family==19 || family==20?10:0;
            if(counter==0 && (family==2 || family==12) && state.Impact && epoch>=0 && epoch!=state.Epoch)PlayMagicSound(state.Entry.Sound1);
            state.Epoch=epoch;
            if(projectile && !state.Travel){PlayMagicSound(state.Entry.Sound2);state.Travel=true;}
            if(counter>=impact && !state.Impact){if(projectile)StopMagicSound(state.Entry.Sound2);PlayMagicSound(state.Entry.Sound1);state.Impact=true;}
            if((family==3 || family==4 || family==13) && counter>=9)StopMagicSound(state.Entry.Sound1);
        }
        void StopMagicTargetAudio(short id)
        {
            if(magicTargetAudio.TryGetValue(id,out var state) && (state.Entry.Family==3 || state.Entry.Family==4 || state.Entry.Family==13))StopMagicSound(state.Entry.Sound1);
            magicTargetAudio.Remove(id);
        }
        void AdvanceMagicCasterAudio(float seconds)
        {
            foreach(var pair in magicCasterAudio)
            {
                var state=pair.Value;if(state.Ended)continue;state.Age+=seconds;int counter=NativeTargetEffects.CounterAt(state.Age);
                if(!IsBound || battle.IsBattleFinished || counter<0 || counter>=state.Entry.CasterLifetime
                    || !battle.TryGetUnit(pair.Key,out var unit) || unit.LifeState==UnitLifeState.Removed
                    || state.Entry.Family==12 && groundBurstCasters.TryGetValue(pair.Key,out var burst) && !burst.Playing)
                {StopMagicSound(state.Sound);state.Ended=true;continue;}
                if(!state.Started){PlayMagicSound(state.Sound);state.Started=true;}
            }
        }
        void SuspendMagicAudio(){foreach(var source in magicSoundSources.Values)if(source!=null)source.Stop();magicCasterAudio.Clear();magicTargetAudio.Clear();}
        void ReleaseMagicAudio(){SuspendMagicAudio();foreach(var source in magicSoundSources.Values)if(source!=null)ReleasePreview(source.gameObject);magicSoundSources.Clear();}
    }
}
