using System.Collections.Generic;
using DarkSaver.Combat;
using UnityEngine;
namespace DarkSaver.UI
{
    public sealed partial class FirstPlayTutorial
    {
        private sealed class PracticeEffect
        {public SpriteRenderer body;public Vector3 source,target;public float age;public bool magic;}
        private readonly List<PracticeEffect> effects=new();
        private Sprite[] attackFrames;
        private NativeFireballEffects fireball;
        private void StartPracticeEffect(int actor,int target,bool magic)
        {
            if(!actors.TryGetValue(actor,out var from) || !actors.TryGetValue(target,out var to))return;
            var go=new GameObject("Tutorial native action effect",typeof(SpriteRenderer));go.transform.SetParent(transform,false);
            var effect=new PracticeEffect {body=go.GetComponent<SpriteRenderer>(),source=from.transform.position,target=to.transform.position,magic=magic};
            effect.body.sortingOrder=to.GetComponent<SpriteRenderer>().sortingOrder+1;effects.Add(effect);
            if(magic)
            {
                fireball??=Resources.Load<NativeFireballEffects>("Effects/NativeFireballEffects");
                var catalog=Resources.Load<NativeMagicPresentationCatalog>("Effects/NativeMagicPresentationCatalog");
                if(catalog!=null && catalog.TryGet(101,out var entry))DarkSaver.World.WorldAudioService.PlayEffect(entry.Sound1);
            }
            else attackFrames??=BattleAttackEffectFrames.Load();
        }
        private void TickPracticeEffects()
        {
            for(int i=effects.Count-1;i>=0;i--)
            {
                var e=effects[i];e.age+=Time.unscaledDeltaTime;
                int frame=e.magic?fireball?.CounterForAge(e.age) ?? -1:BattleAttackEffectFrames.FrameAt(e.age);
                if(frame<0){Destroy(e.body.gameObject);effects.RemoveAt(i);continue;}
                if(e.magic)
                {
                    e.body.enabled=fireball.DrawsAtCounter(frame);
                    e.body.sprite=frame<10?fireball.Projectile(frame%fireball.ProjectileCount):fireball.Impact(frame-10);
                    e.body.transform.position=frame<10?NativeFireballEffects.TravelPosition(e.source,e.target,frame*.1f,new Vector3(0,50f/32,0)):e.target;
                }
                else {e.body.sprite=attackFrames[frame];e.body.transform.position=e.target;}
            }
        }
        private void ClearPracticeEffects()
        {foreach(var e in effects)if(e.body!=null)Destroy(e.body.gameObject);effects.Clear();}
    }
}
