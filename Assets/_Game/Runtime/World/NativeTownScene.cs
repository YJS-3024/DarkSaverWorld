using System;
using System.Collections.Generic;
using DarkSaver.Core;
using DarkSaver.Maps;
using DarkSaver.Rendering;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DarkSaver.World
{
    // Saved scene geometry and independent NPC data; never generates map tiles at runtime.
    public sealed class NativeTownScene : MonoBehaviour
    {
        [SerializeField] private string townKey;
        [SerializeField] private bool runtimeOnly;
        [SerializeField] private bool mapTilesBaked;
        private readonly List<UnityEngine.Object> owned=new();
        private bool built;
        private void Awake(){if(!string.IsNullOrEmpty(townKey))Build();}
        public bool Configure(string key,bool runtime)=>Configure(key,runtime,false);
        public bool Configure(string key,bool runtime,bool baked){townKey=key;runtimeOnly=runtime;mapTilesBaked=baked;return Build();}
        public bool Build()
        {
            if(built)return true;
            try
            {
                if(!WorldRouteContent.Resolve(townKey,out var town))
                {
                    if(!ScenarioCatalog.TryGet(townKey,out var scenario))throw new InvalidOperationException("Unknown generated region.");
                    town=new TownDefinition("Scenario"+scenario.Key,scenario.Title,scenario.MapScene,scenario.RuntimeScene,scenario.Key,scenario.Width,scenario.Height,false);
                }
                // Geometry belongs to the saved scene. No runtime JSON or regeneration fallback.
                if(runtimeOnly) BuildRuntime(town);
                else ValidateBakedMap(town);
                built=true;return true;
            }
            catch(Exception ex){Debug.LogError("Region "+townKey+": "+ex.Message);return false;}
        }
        // Saved map geometry is the editable source of truth. Never regenerate it
        // over scene edits or create a second grid when entering Play mode.
        private void ValidateBakedMap(TownDefinition town)
        {
            Tilemap ground=null;bool detail=false,terrain=false;
            foreach(var layer in GetComponentsInChildren<Tilemap>(true))
            {
                if(layer.name=="Ground")ground=layer;
                else if(layer.name=="Detail")detail=true;
                else if(layer.name=="TerrainAttributes")terrain=true;
            }
            if(ground==null || !detail || !terrain)throw new InvalidOperationException("Saved map Tilemap layers are missing.");
            if(ground.GetUsedTilesCount()==0 || ground.origin!=Vector3Int.zero
                || ground.size.x!=town.Width || ground.size.y!=town.Height)
                throw new InvalidOperationException("Saved map ground dimensions do not match the region.");
        }
        private T Own<T>(T value) where T:UnityEngine.Object {owned.Add(value);return value;}
        private GameObject Child(string name) {var value=new GameObject(name);value.transform.SetParent(transform,false);return value;}
        private Sprite FullSprite(Texture2D texture,Vector2 pivot)
        { if(texture==null)throw new InvalidOperationException("Missing town artwork.");return Own(Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),pivot,32,0,SpriteMeshType.FullRect)); }
        private void BuildRuntime(TownDefinition town)
        {
            if(KaosiaOriginalPresentation.IsInterior(townKey,out var nativeMap))BuildOriginalStaff(nativeMap,town);
        }
        private void BuildOriginalStaff(string nativeMap,TownDefinition town)
        {
            foreach(var entry in KaosiaOriginalPresentation.LoadStaff().entries)
            {
                if(entry.nativeMap!=nativeMap || !OriginalRestorationPolicy.AllowsStaffEvidence(entry.evidence))continue;
                var texture=Resources.Load<Texture2D>(entry.texture);
                if(texture==null || entry.pixelX<0 || entry.pixelY<0
                    || entry.pixelX+texture.width>town.Width*32 || entry.pixelY+texture.height>town.Height*32)
                    throw new InvalidOperationException("Invalid original counter staff frame.");
                var actor=Child("Original counter staff "+entry.appearanceId+" "+entry.pixelX);
                // Match the photographed sprite rectangle directly. This does not assert a native feet coordinate.
                actor.transform.position=new Vector3((entry.pixelX+texture.width*.5f)/32f,
                    town.Height-(entry.pixelY+texture.height*.5f)/32f,0);
                var renderer=actor.AddComponent<SpriteRenderer>();renderer.sprite=FullSprite(texture,new Vector2(.5f,.5f));
                renderer.sortingOrder=20000-Mathf.RoundToInt(actor.transform.position.y*10);
                var idle=new Sprite[3];bool complete=true;
                for(int i=0;i<3;i++)
                {
                    var frame=Resources.Load<Texture2D>("Interiors/Npcs/Original/"+entry.appearanceId+"_Down"+i);
                    if(frame==null){complete=false;break;}idle[i]=FullSprite(frame,new Vector2(.5f,.5f));
                }
                if(complete)actor.AddComponent<DarkSaver.Player.DirectionalSpriteAnimator>().Configure(idle,idle,idle,idle);
                actor.AddComponent<OriginalTownStaff>().Configure(entry);
                // Counters retain original terrain. Staff need not stand on player-walkable tiles.
                if(ShopInteriorCatalog.TryGet(townKey,out _) || townKey=="KaosiaTemple" || townKey=="KaosiaWarehouse" || townKey=="Interior2900")
                {
                    var hit=actor.AddComponent<BoxCollider2D>();hit.isTrigger=true;hit.size=Vector2.one;
                    var counter=actor.AddComponent<ShopCounter2D>();
                    // The counter owns its explicit access cells; map geometry is not a runtime data file.
                    if(entry.counterFrontCells==null || entry.counterFrontCells.Length==0)
                        throw new InvalidOperationException("Missing counter access cells.");
                    var front = new List<Vector2Int>();var seen=new HashSet<int>();
                    foreach(int cell in entry.counterFrontCells)
                    {
                        if(cell<0 || cell>=town.Width*town.Height || !seen.Add(cell))
                            throw new InvalidOperationException("Invalid counter access cells.");
                        front.Add(new Vector2Int(cell%town.Width,cell/town.Width));
                    }
                    counter.ConfigureCounterFront(front.ToArray());
                }
            }
        }
        private void OnDestroy(){foreach(var item in owned)if(item!=null){if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}owned.Clear();}
    }
}
