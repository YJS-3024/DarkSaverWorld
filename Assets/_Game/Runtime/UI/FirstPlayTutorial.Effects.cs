using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Player;
using UnityEngine;
namespace DarkSaver.UI
{
    public sealed partial class FirstPlayTutorial
    {
        private sealed class PracticeEffect
        {
            public SpriteRenderer body;
            public Vector3 source,target,offset;
            public int owner,other;
            public float age;
            public bool magic,impact,soundedImpact;
            public NativeMagicPresentationCatalog.Entry sound;
        }
        private sealed class PracticeDeath {public float age;public SpriteRenderer effect;}
        private readonly List<PracticeEffect> effects=new();
        private readonly Dictionary<int,PracticeDeath> deaths=new();
        private readonly Dictionary<int,AudioSource> practiceSounds=new();
        private Sprite[] attackFrames;
        private NativeFireballEffects fireball;
        private CombatDeathEffectCatalog deathCatalog;
        private NativeMagicSoundCatalog soundCatalog;

        private void StartPracticeEffect(int actor,int target,bool magic)
        {
            if(!actors.TryGetValue(actor,out var from) || !actors.TryGetValue(target,out var to))return;
            if(magic)
            {
                fireball??=Resources.Load<NativeFireballEffects>("Effects/NativeFireballEffects");
                if(fireball==null || !fireball.HasValidFrames)return;
                var e=CreatePracticeEffect(target,actor,true);
                e.source=from.transform.position;e.target=to.transform.position;
                var catalog=Resources.Load<NativeMagicPresentationCatalog>("Effects/NativeMagicPresentationCatalog");
                if(catalog!=null && catalog.TryGet(101,out e.sound))PlayPracticeSound(e.sound.Sound2);
            }
            else
            {
                attackFrames??=BattleAttackEffectFrames.Load();
                if(System.Array.Exists(attackFrames,s=>s==null))return;
                // AttackChar sets 0x04 on both records, not an independently simulated counterattack.
                var delta=to.transform.position-from.transform.position;
                if(Mathf.Abs(delta.x)+Mathf.Abs(delta.y)<3)
                    CreatePracticeEffect(actor,target,false).offset=delta*.5f;
                CreatePracticeEffect(target,actor,false).impact=true;
            }
        }
        private PracticeEffect CreatePracticeEffect(int owner,int other,bool magic)
        {
            // Native per-record action state is replaced, rather than accumulating duplicate effects.
            for(int i=effects.Count-1;i>=0;i--)
                if(effects[i].owner==owner && effects[i].magic==magic)ReleasePracticeEffect(i);
            var go=new GameObject("Tutorial native action effect",typeof(SpriteRenderer));go.transform.SetParent(transform,false);
            var e=new PracticeEffect{body=go.GetComponent<SpriteRenderer>(),owner=owner,other=other,magic=magic};
            effects.Add(e);return e;
        }
        private bool HasPracticeMagic(int actor)=>effects.Exists(e=>e.magic && e.owner==actor);
        private void SeedPracticeDeaths()
        {
            // Reconnection restores the script state, not an invented replay of a previous death.
            if(progress!=null)foreach(var a in progress.actors)if(a.dead)deaths[a.id]=new PracticeDeath{age=float.PositiveInfinity};
        }
        private bool IsPracticeCorpseVisible(int actor)
        {
            if(!deaths.TryGetValue(actor,out var death)){death=new PracticeDeath();deaths.Add(actor,death);}
            return HasPracticeMagic(actor) || death.age<GameConstants.BattleDeathCounterSeconds*19;
        }
        private void TickPracticeEffects()=>AdvancePracticeEffects(Time.unscaledDeltaTime);
        private void AdvancePracticeEffects(float seconds)
        {
            if(seconds<0 || float.IsNaN(seconds) || float.IsInfinity(seconds))return;
            // Native 0x80 && !0x40 priority: magic holds the corpse counter; physical FX do not.
            // Advance existing deaths before expiring magic, so expiry cannot consume its whole travel time.
            if(progress!=null)foreach(var a in progress.actors)
                if(a.dead && !a.removed && !HasPracticeMagic(a.id))
                {
                    IsPracticeCorpseVisible(a.id);deaths[a.id].age+=seconds;
                }
            for(int i=effects.Count-1;i>=0;i--)
            {
                var e=effects[i];
                var owner=System.Array.Find(progress.actors,a=>a.id==e.owner);
                if(owner==null || owner.removed){ReleasePracticeEffect(i);continue;}
                e.age+=seconds;
                int frame=e.magic?fireball.CounterForAge(e.age):BattleAttackEffectFrames.FrameAt(e.age);
                if(e.magic && !e.soundedImpact && (frame>=10 || frame<0))
                {
                    if(e.sound!=null){StopPracticeSound(e.sound.Sound2);PlayPracticeSound(e.sound.Sound1);}
                    e.soundedImpact=true;
                }
                if(frame<0)ReleasePracticeEffect(i);
            }
        }
        private void PresentPracticeEffects()
        {
            foreach(var e in effects)
            {
                var state=System.Array.Find(progress.actors,a=>a.id==e.owner);
                bool corpse=state==null || state.removed || state.dead && !HasPracticeMagic(e.owner);
                e.body.enabled=!corpse;
                if(corpse || !actors.TryGetValue(e.owner,out var owner))continue;
                var body=owner.GetComponent<SpriteRenderer>();
                e.body.sortingLayerID=body.sortingLayerID;e.body.sortingOrder=body.sortingOrder+1;
                if(e.magic)
                {
                    int counter=fireball.CounterForAge(e.age);if(counter<0){e.body.enabled=false;continue;}
                    e.body.enabled=fireball.DrawsAtCounter(counter);
                    e.body.sprite=counter<10?fireball.Projectile(counter%fireball.ProjectileCount):fireball.Impact(counter-10);
                    e.body.transform.position=counter<10?NativeFireballEffects.TravelPosition(e.source,e.target,counter*.1f,new Vector3(0,50f/32,0)):e.target;
                }
                else
                {
                    if(actors.TryGetValue(e.other,out var other))
                    {
                        var anim=owner.GetComponent<DirectionalSpriteAnimator>();
                        anim.TrySetFacing(other.transform.position-owner.transform.position);anim.PresentFrame((long)(Time.realtimeSinceStartupAsDouble*1000d));
                    }
                    int frame=BattleAttackEffectFrames.FrameAt(e.age);if(frame<0){e.body.enabled=false;continue;}
                    e.body.sprite=attackFrames[frame];
                    e.body.transform.position=(e.impact?BattleMapPresentation.GetUnitMenuAnchor(body):owner.transform.position)+e.offset;
                }
            }
            foreach(var d in deaths.Values)if(d.effect!=null)d.effect.enabled=false;
            foreach(var a in progress.actors)
            {
                if(!a.dead || a.removed || !actors.TryGetValue(a.id,out var actor))continue;
                IsPracticeCorpseVisible(a.id);var d=deaths[a.id];
                if(HasPracticeMagic(a.id)){if(d.effect!=null)d.effect.enabled=false;continue;}
                int counter=float.IsPositiveInfinity(d.age)?19:Mathf.FloorToInt(d.age/GameConstants.BattleDeathCounterSeconds);
                var body=actor.GetComponent<SpriteRenderer>();
                if(!CombatDeathPresentation.TryGetFrame(counter,out var frame))
                {actor.SetActive(false);if(d.effect!=null)d.effect.enabled=false;continue;}
                // Native first eight counters read body row32, outside this 4-row tutorial sheet.
                // Do not invent a fallen pose or rotate a living sprite into one.
                body.enabled=frame.BodyRow<4;
                if(body.enabled)body.sprite=Resources.Load<Sprite>($"Tutorial/Characters/{a.job}_0_{new[]{"Up","Down","Left","Right"}[frame.BodyRow]}_0");
                deathCatalog??=Resources.Load<CombatDeathEffectCatalog>("Effects/CombatDeathEffectCatalog");
                if(deathCatalog==null || !deathCatalog.TryGetEffect(counter,out var sprite))continue;
                if(d.effect==null)
                {
                    var go=new GameObject("Tutorial native corpse effect",typeof(SpriteRenderer));go.transform.SetParent(transform,false);
                    d.effect=go.GetComponent<SpriteRenderer>();
                }
                d.effect.enabled=true;d.effect.sprite=sprite;d.effect.sortingLayerID=body.sortingLayerID;
                d.effect.sortingOrder=body.sortingOrder+1;
                // Catalog uses top-left pivot; actor native tile is centered in Unity.
                d.effect.transform.position=actor.transform.position+new Vector3(-.5f,.5f,0);
            }
        }
        private void PlayPracticeSound(int id)
        {
            if(id<=0)return;
            soundCatalog??=Resources.Load<NativeMagicSoundCatalog>("Effects/NativeMagicSoundCatalog");
            var clip=soundCatalog!=null?soundCatalog.Get(id):null;if(clip==null)return;
            if(!practiceSounds.TryGetValue(id,out var source))
            {
                var go=new GameObject("Tutorial native magic sound "+id);go.transform.SetParent(transform,false);
                source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.clip=clip;practiceSounds.Add(id,source);
            }
            source.volume=Mathf.Clamp01(GameConstants.NativeMagicSoundVolume);source.Stop();source.time=0;
            if(Application.isPlaying)source.Play();
        }
        private void StopPracticeSound(int id){if(practiceSounds.TryGetValue(id,out var source) && source!=null)source.Stop();}
        private void ReleasePracticeEffect(int index)
        {
            var e=effects[index];if(e.magic && e.sound!=null)StopPracticeSound(e.sound.Sound2);
            if(e.body!=null)ReleasePracticeObject(e.body.gameObject);effects.RemoveAt(index);
        }
        private static void ReleasePracticeObject(GameObject go)
        {if(Application.isPlaying)Destroy(go);else DestroyImmediate(go);}
        private void ClearPracticeEffects()
        {
            for(int i=effects.Count-1;i>=0;i--)ReleasePracticeEffect(i);
            foreach(var d in deaths.Values)if(d.effect!=null)ReleasePracticeObject(d.effect.gameObject);deaths.Clear();
            foreach(var s in practiceSounds.Values)if(s!=null){s.Stop();ReleasePracticeObject(s.gameObject);}practiceSounds.Clear();
        }
    }
}
