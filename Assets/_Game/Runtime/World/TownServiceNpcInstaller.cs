using System;
using System.Collections.Generic;
using DarkSaver.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.World
{
    public sealed class TownServiceNpcInstaller : MonoBehaviour
    {
        private readonly List<Sprite> sprites=new();
        public void Build(string townKey)
        {
            if(!TownCatalog.TryGet(townKey,out var town) || gameObject.scene.name!=town.RuntimeScene)return;
            var asset=Resources.Load<TextAsset>("Town/ServiceNpcs");
            var data=asset==null?null:JsonUtility.FromJson<TownServiceNpcData>(asset.text);
            if(data==null || data.version!=1 || data.entries==null)throw new InvalidOperationException("Town service NPC resources unavailable.");
            foreach(var entry in data.entries)
            {
                if(entry.townKey!=townKey)continue;
                if(entry.x<0 || entry.y<0 || entry.x>=town.Width || entry.y>=town.Height || float.IsNaN(entry.x) || float.IsNaN(entry.y))throw new InvalidOperationException("Invalid town service NPC placement.");
                var texture=Resources.Load<Texture2D>(entry.texture);if(texture==null)throw new InvalidOperationException("Town service NPC artwork unavailable: "+entry.texture);
                var actor=new GameObject(entry.name);actor.transform.SetParent(transform,false);actor.transform.position=new Vector3(entry.x,entry.y,0);
                var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.1f),32,0,SpriteMeshType.FullRect);sprites.Add(sprite);
                var renderer=actor.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=20000-Mathf.RoundToInt(entry.y*10);
                var hit=actor.AddComponent<BoxCollider2D>();hit.isTrigger=true;hit.offset=new Vector2(0,.45f);hit.size=new Vector2(.8f,1.3f);
                actor.AddComponent<TownServiceNpc>().Configure(entry);actor.AddComponent<IndoorNpcLabel>().Configure(entry.name);
            }
        }
        private void OnDestroy(){foreach(var sprite in sprites)if(sprite!=null){if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite);}}
    }
}
