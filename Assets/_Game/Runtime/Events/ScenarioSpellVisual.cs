using DarkSaver.Combat;
using UnityEngine;
namespace DarkSaver.Events
{
    // Scripted presentation only: these effects never adjudicate damage or kills.
    public sealed class ScenarioSpellVisual : MonoBehaviour
    {
        private SpriteRenderer body,target;
        private Vector3 source,position;
        private NativeFireballEffects projectile;
        private NativeThunderEffects thunder;
        private NativeTargetEffects.Pattern ground;
        private float age;
        private NativeMagicPresentationCatalog.Entry audio;
        private bool impactPlayed,casterStopped;
        private int casterSound;
        private bool DelayedImpact=>projectile!=null || audio!=null && (audio.Family==6 || audio.Family==14 || audio.Family==15 || audio.Family==18 || audio.Family==19 || audio.Family==20);
        private void BeginAudio(int id)
        {
            Resources.Load<NativeMagicPresentationCatalog>("Effects/NativeMagicPresentationCatalog")?.TryGet(id,out audio);
            if(audio==null)return;
            if(audio.Family==0 || audio.Family==99){audio=null;return;}
            if(audio.CasterLifetime>0){casterSound=audio.Family==12?audio.Sound1:audio.Sound2;World.WorldAudioService.PlayEffect(casterSound);}
            if(projectile!=null)World.WorldAudioService.PlayEffect(audio.Sound2);
            if(!DelayedImpact){World.WorldAudioService.PlayEffect(audio.Sound1);impactPlayed=true;}
        }
        private void AdvanceAudio()
        {
            if(audio==null)return;int counter=NativeTargetEffects.CounterAt(age);
            if(!impactPlayed && counter>=10){if(projectile!=null)World.WorldAudioService.StopEffect(audio.Sound2);World.WorldAudioService.PlayEffect(audio.Sound1);impactPlayed=true;}
            if(!casterStopped && audio.CasterLifetime>0 && counter>=audio.CasterLifetime){World.WorldAudioService.StopEffect(casterSound);casterStopped=true;}
            if(counter>=9 && (audio.Family==3 || audio.Family==4 || audio.Family==13))World.WorldAudioService.StopEffect(audio.Sound1);
        }
        private void OnDestroy()
        {
            if(audio==null)return;
            if(!casterStopped)World.WorldAudioService.StopEffect(casterSound);
            if(projectile!=null && !impactPlayed)World.WorldAudioService.StopEffect(audio.Sound2);
            if(audio.Family==3 || audio.Family==4 || audio.Family==13)World.WorldAudioService.StopEffect(audio.Sound1);
        }
        public static bool Spawn(Transform parent,int id,Vector3 source,Vector3 position,SpriteRenderer target,int order)
        {
            NativeFireballEffects projectile=null;NativeThunderEffects thunder=null;NativeTargetEffects.Pattern ground=null;
            Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeProjectileEffects")?.TryGet(id,out projectile);
            var lightning=Resources.Load<NativeThunderEffects>("Effects/NativeThunderEffects");
            if(lightning!=null && lightning.Supports(id))thunder=lightning;
            Resources.Load<NativeTargetEffects>("Effects/NativeCoordinateGroundEffects")?.TryGet(id,out ground);
            if(projectile==null && thunder==null && ground==null)return false;
            var go=new GameObject("Scripted native magic "+id,typeof(SpriteRenderer));go.transform.SetParent(parent,false);
            var effect=go.AddComponent<ScenarioSpellVisual>();effect.body=go.GetComponent<SpriteRenderer>();effect.body.sortingOrder=order;
            effect.source=source;effect.position=position;effect.target=target;effect.projectile=projectile;effect.thunder=thunder;effect.ground=ground;effect.BeginAudio(id);return true;
        }
        private void Update()
        {
            if(Time.timeScale==0)return;age+=Time.unscaledDeltaTime;AdvanceAudio();
            var destination=target!=null?target.bounds.center:position;
            if(projectile!=null)
            {
                int counter=projectile.CounterForAge(age);if(counter<0){Destroy(gameObject);return;}
                if(counter<10)
                {
                    var delta=destination-source;int index=projectile.DirectionalTravel?NativeFireballEffects.DirectionFrame(delta):counter%projectile.ProjectileCount;
                    body.sprite=projectile.Projectile(index<0?0:index%projectile.ProjectileCount);
                    body.transform.position=projectile.LinearTravel?NativeFireballEffects.LinearPosition(source,destination,counter,Vector2.one)
                        :NativeFireballEffects.TravelPosition(source,destination,counter/10f,Vector3.up*(50f/32));
                }
                else {body.sprite=projectile.Impact(counter-10);body.transform.position=destination;}
            }
            else
            {
                int counter=NativeTargetEffects.CounterAt(age);
                if(thunder!=null){if(counter>=20){Destroy(gameObject);return;}body.sprite=thunder.Bolt(counter);}
                else {if(counter>=ground.LifetimeCounters){Destroy(gameObject);return;}body.sprite=ground.Frame(counter);}
                body.transform.position=destination;
            }
        }
    }
}
