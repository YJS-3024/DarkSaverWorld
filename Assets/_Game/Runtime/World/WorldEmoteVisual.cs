using System;
using System.Collections.Generic;
using UnityEngine;
namespace DarkSaver.World
{
    public sealed class WorldEmoteVisual : MonoBehaviour
    {
        [Serializable] private sealed class Entry {public int id,limit,offsetX,offsetY;public string[] frames;public int[] delays;}
        [Serializable] private sealed class Data {public Entry[] entries;}
        private static Data data;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetData()=>data=null;
        private readonly List<Sprite> owned=new();
        private SpriteRenderer body;private Entry entry;private int counter;private float age;
        private static Entry Find(int id)
        {
            if(data==null){var asset=Resources.Load<TextAsset>("Effects/Emotes/Manifest");if(asset!=null)data=JsonUtility.FromJson<Data>(asset.text);}
            if(data?.entries!=null)foreach(var row in data.entries)if(row.id==id)return row;return null;
        }
        public static bool Has(int id)=>id>=29001 && id<=29015 && Find(id)!=null;
        public bool Play(int id)
        {
            Stop();var found=Find(id);if(found==null || found.frames==null || found.frames.Length==0 || found.delays==null || found.limit<1 || found.delays.Length!=found.frames.Length)return false;
            foreach(string path in found.frames)
            {
                var texture=Resources.Load<Texture2D>(path);if(texture==null){Stop();return false;}
                owned.Add(Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect));
            }
            if(body==null){var go=new GameObject("Emote");go.transform.SetParent(transform,false);body=go.AddComponent<SpriteRenderer>();}
            entry=found;counter=0;age=0;body.enabled=true;ShowFrame();return true;
        }
        private void ShowFrame()
        {
            body.sprite=owned[counter%owned.Count];body.transform.localPosition=new Vector3(entry.offsetX/32f,1+entry.offsetY/32f,0);
            body.sortingOrder=(GetComponent<SpriteRenderer>()?.sortingOrder ?? 20000)+2;
        }
        private void Update()
        {
            if(entry==null)return;
            if(DarkSaver.Events.Scenario01Controller.BlocksWorldActions || FindAnyObjectByType<WorldSceneCoordinator>()?.IsTravelling==true){Stop();return;}
            age+=Time.unscaledDeltaTime;
            if(age*1000>=entry.delays[counter%owned.Count]){age=0;counter++;if(counter>=entry.limit){Stop();return;}}
            ShowFrame();
        }
        private void Stop()
        {
            entry=null;if(body!=null){body.enabled=false;body.sprite=null;}
            foreach(var sprite in owned)if(sprite!=null){if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite);}owned.Clear();
        }
        private void OnDisable()=>Stop();private void OnDestroy()=>Stop();
    }
}
