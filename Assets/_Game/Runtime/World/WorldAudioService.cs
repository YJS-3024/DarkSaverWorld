using System.Collections.Generic;
using DarkSaver.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace DarkSaver.World
{
    // One world music owner. Scenario script music owns its own channel while active.
    public sealed class WorldAudioService : MonoBehaviour
    {
        private static WorldAudioService instance;
        private WorldAudioData data;
        private AudioSource music;
        private AudioListener ownedListener;
        private string playing;
        private bool dirty=true;
        private readonly Dictionary<int,AudioSource> sounds=new();
        private readonly HashSet<string> missing=new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()=>instance=null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if(instance!=null)return;
            var go=new GameObject("World audio");DontDestroyOnLoad(go);instance=go.AddComponent<WorldAudioService>();
        }
        private void Awake()
        {
            var asset=Resources.Load<TextAsset>("World/Audio/Manifest");data=asset==null?null:JsonUtility.FromJson<WorldAudioData>(asset.text);
            music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.spatialBlend=0;music.loop=true;music.volume=.4f;
            SceneManager.sceneLoaded+=Loaded;SceneManager.sceneUnloaded+=Unloaded;
        }
        private void Loaded(Scene scene,LoadSceneMode mode)=>dirty=true;
        private void Unloaded(Scene scene)=>dirty=true;
        private void LateUpdate()
        {
            if(FindAnyObjectByType<AudioListener>()==null)ownedListener=gameObject.AddComponent<AudioListener>();
            if(!dirty)return;dirty=false;
            if(ownedListener!=null)
                foreach(var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                    if(listener!=ownedListener && listener.isActiveAndEnabled){Destroy(ownedListener);ownedListener=null;break;}
            foreach(var scenario in ScenarioCatalog.All)
                if(SceneManager.GetSceneByName(scenario.RuntimeScene).isLoaded){StopMusic();return;}
            if(FindAnyObjectByType<WorldSceneCoordinator>()?.IsTravelling==true){dirty=true;return;}
            var region=TownSceneContext.Current ?? FieldSceneContext.Current;
            if(region==null || !SceneManager.GetSceneByName(region.MapScene).isLoaded){StopMusic();return;}
            var binding=data?.FindRegion(region.NativeMap);
            if(binding==null){StopMusic();return;}
            if(playing==binding.clip && music.isPlaying)return;
            var clip=Resources.Load<AudioClip>(binding.clip);
            if(clip==null){StopMusic();if(missing.Add(binding.clip))Debug.LogWarning("Missing world BGM: "+binding.clip);return;}
            music.Stop();music.clip=clip;music.Play();playing=binding.clip;
        }
        private void StopMusic(){music.Stop();music.clip=null;playing=null;}
        public static void PlayDefeat(int job)
        {if(instance!=null && instance.data!=null && instance.data.HasDeathSound(job))PlayEffect(1);}
        public static void StopEffect(int id)
        {if(instance!=null && instance.sounds.TryGetValue(id,out var source) && source!=null)source.Stop();}
        public static void PlayEffect(int id)
        {
            if(instance==null || id<=0)return;
            if(!instance.sounds.TryGetValue(id,out var source))
            {
                var path=instance.data?.FindSound(id);if(string.IsNullOrEmpty(path))return;
                var clip=Resources.Load<AudioClip>(path);if(clip==null)return;
                var go=new GameObject("Native sound "+id);go.transform.SetParent(instance.transform,false);
                source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=GameConstants.NativeMagicSoundVolume;source.clip=clip;
                instance.sounds.Add(id,source);
            }
            source.Stop();source.Play();
        }
        private void OnDestroy()
        {SceneManager.sceneLoaded-=Loaded;SceneManager.sceneUnloaded-=Unloaded;if(instance==this)instance=null;}
    }
}
