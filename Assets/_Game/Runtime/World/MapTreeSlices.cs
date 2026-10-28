using UnityEngine;
using DarkSaver.Rendering;
namespace DarkSaver.World
{
    // User-confirmed2x2 rule: bottom row behind the character, canopy in front.
    public sealed class MapTreeSlices:MonoBehaviour
    {
        private Sprite bottom,top;
        public void Configure(SpriteRenderer source,int index)
        {
            var s=source.sprite;var r=s.rect;float half=r.height*.5f;
            bottom=Sprite.Create(s.texture,new Rect(r.x,r.y,r.width,half),new Vector2(.5f,.5f),s.pixelsPerUnit,0,SpriteMeshType.FullRect);
            top=Sprite.Create(s.texture,new Rect(r.x,r.y+half,r.width,half),new Vector2(.5f,.5f),s.pixelsPerUnit,0,SpriteMeshType.FullRect);
            var center=source.bounds.center;float offset=source.bounds.size.y*.25f;
            void Part(string name,Sprite sprite,float y,int order)
            {
                var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.position=new Vector3(center.x,y,center.z);
                var body=go.AddComponent<SpriteRenderer>();body.sprite=sprite;body.sortingLayerID=source.sortingLayerID;
                body.sortingOrder=order;body.sharedMaterial=source.sharedMaterial;body.color=source.color;
            }
            Part("Tree bottom row",bottom,center.y-offset,OriginalRenderRules.DetailSortingOrder+index);
            Part("Tree canopy row",top,center.y+offset,OriginalRenderRules.ForegroundSortingOrder(index));source.enabled=false;
        }
        private void OnDestroy()
        {
            if(bottom!=null){if(Application.isPlaying)Destroy(bottom);else DestroyImmediate(bottom);}
            if(top!=null){if(Application.isPlaying)Destroy(top);else DestroyImmediate(top);}
        }
    }
}
