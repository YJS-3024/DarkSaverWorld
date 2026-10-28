using System;
using System.Collections.Generic;
using DarkSaver.Core;
using DarkSaver.Maps;
using DarkSaver.Rendering;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DarkSaver.World
{
    // Resource-only loading: runtime does not read the installation or MRC/GRS.
    public sealed class NativeTownScene : MonoBehaviour
    {
        [SerializeField] private string townKey;
        [SerializeField] private bool runtimeOnly;
        [Serializable] private sealed class ObjectRecord { public int x,y,index; public string texture; }
        [Serializable] private sealed class IndoorNpcRecord { public string name,texture; public int appearanceId,x,y; }
        [Serializable] private sealed class MapData
        { public int width,height,spawnX,spawnY; public int[] attributes, floorKeys; public ObjectRecord[] objects; public IndoorNpcRecord npc; public DarkSaver.Events.Scenario01Unit[] encounters; }
        private readonly List<UnityEngine.Object> owned=new();
        private string ResourceRoot => ScenarioCatalog.TryGet(townKey,out var scenario) ? "Scenarios/"+scenario.Key+"/Map" : (KaosiaFacilityCatalog.TryGet(townKey,out var facility) ? "Interiors/"+facility.Region.NativeMap : NativeInteriorCatalog.TryGet(townKey,out var interior) ? "Interiors/"+interior.NativeMap : ShopInteriorCatalog.TryGet(townKey,out var shop) ? "Interiors/"+shop.Region.NativeMap : (FieldCatalog.TryGet(townKey,out _) ? "Fields/" : "Towns/")+townKey);
        private bool built;
        private void Awake(){if(!string.IsNullOrEmpty(townKey))Build();}
        public bool Configure(string key,bool runtime){townKey=key;runtimeOnly=runtime;return Build();}
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
                var text=Resources.Load<TextAsset>(ResourceRoot+"/Map");
                var data=text==null?null:JsonUtility.FromJson<MapData>(text.text);
                // Unity creates an empty inline NPC even when the map JSON omits it.
                // Only an explicit NPC object authorizes spawning an interior actor.
                if (data != null && !System.Text.RegularExpressions.Regex.IsMatch(text.text, @"(?<!\\)""npc""\s*:\s*\{")) data.npc=null;
                if(data==null || data.width!=town.Width || data.height!=town.Height || data.attributes==null
                    || data.attributes.Length!=data.width*data.height || data.objects==null)throw new InvalidOperationException("Invalid town resources.");
                if(runtimeOnly) BuildRuntime(town,data); else BuildMap(town,data);
                built=true;return true;
            }
            catch(Exception ex){Debug.LogError("Region "+townKey+": "+ex.Message);return false;}
        }
        private T Own<T>(T value) where T:UnityEngine.Object {owned.Add(value);return value;}
        private GameObject Child(string name) {var value=new GameObject(name);value.transform.SetParent(transform,false);return value;}
        private Sprite FullSprite(Texture2D texture,Vector2 pivot)
        { if(texture==null)throw new InvalidOperationException("Missing town artwork.");return Own(Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),pivot,32,0,SpriteMeshType.FullRect)); }
        private void BuildMap(TownDefinition town,MapData data)
        {
            var grid=Child(town.Title);grid.AddComponent<Grid>();
            grid.AddComponent<MapSceneDefinition>().Configure(town.NativeMap,town.Title,"원본 지형/장식; 지역 연결은 싱글판 구성");
            Tilemap Layer(string name)
            { var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(grid.transform,false);go.GetComponent<TilemapRenderer>().enabled=false;return go.GetComponent<Tilemap>(); }
            var ground=Layer("Ground");var terrain=Layer("TerrainAttributes");
            var texture=Resources.Load<Texture2D>(ResourceRoot+"/Ground");
            var floor=Child("Native ground artwork").AddComponent<SpriteRenderer>();floor.sprite=FullSprite(texture,Vector2.zero);floor.sortingOrder=OriginalRenderRules.GroundSortingOrder;
            var attributeTiles=new Dictionary<int,Tile>();
            var floorTiles=new Dictionary<int,Tile>();
            bool field=FieldCatalog.TryGet(townKey,out _) || ScenarioCatalog.TryGet(townKey,out _);
            for(int y=0;y<data.height;y++)for(int x=0;x<data.width;x++)
            {
                var cell=new Vector3Int(x,data.height-1-y,0);
                int floorKey=data.floorKeys!=null && data.floorKeys.Length==data.width*data.height ? data.floorKeys[y*data.width+x] : y*data.width+x;
                if(!floorTiles.TryGetValue(floorKey,out var tile))
                {
                    tile=Own(ScriptableObject.CreateInstance<Tile>());tile.name="Native region floor";tile.colliderType=Tile.ColliderType.None;
                    tile.sprite=Own(Sprite.Create(texture,new Rect(x*32,(data.height-1-y)*32,32,32),new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect));
                    floorTiles.Add(floorKey,tile);
                }
                ground.SetTile(cell,tile);
                int attribute=data.attributes[y*data.width+x];if(attribute==0)continue;
                if(!attributeTiles.TryGetValue(attribute,out var terrainTile))
                { terrainTile=Own(ScriptableObject.CreateInstance<Tile>());terrainTile.name="Terrain_"+attribute;terrainTile.colliderType=(attribute==9 || field && (attribute==3 || attribute==6))?Tile.ColliderType.Grid:Tile.ColliderType.None;attributeTiles.Add(attribute,terrainTile); }
                terrain.SetTile(cell,terrainTile);
            }
            terrain.gameObject.AddComponent<TilemapCollider2D>();
            void Boundary(string name, Vector2 position, Vector2 size)
            {
                var wall=Child(name);wall.transform.localPosition=position;
                wall.AddComponent<BoxCollider2D>().size=size;
            }
            Boundary("West map limit",new Vector2(-.5f,data.height*.5f),new Vector2(1,data.height+2));
            Boundary("East map limit",new Vector2(data.width+.5f,data.height*.5f),new Vector2(1,data.height+2));
            Boundary("South map limit",new Vector2(data.width*.5f,-.5f),new Vector2(data.width+2,1));
            Boundary("North map limit",new Vector2(data.width*.5f,data.height+.5f),new Vector2(data.width+2,1));
            foreach(var record in data.objects)
            {
                var renderer=Child("Object_"+record.index.ToString("000")).AddComponent<SpriteRenderer>();
                renderer.sprite=FullSprite(Resources.Load<Texture2D>(ResourceRoot+"/"+record.texture),new Vector2(0,1));
                renderer.transform.localPosition=new Vector3(record.x,data.height-record.y,0);
                renderer.sortingOrder=OriginalRenderRules.WorldObjectSortingOrder(record.index,renderer.sprite.rect.height,renderer.bounds.min.y);
            }
        }
        private void BuildRuntime(TownDefinition town,MapData data)
{if(KaosiaOriginalPresentation.IsInterior(townKey,out var nativeMap))BuildOriginalStaff(nativeMap,data);}        private void BuildOriginalStaff(string nativeMap,MapData data)
        {
            foreach(var entry in KaosiaOriginalPresentation.LoadStaff().entries)
            {
                if(entry.nativeMap!=nativeMap || !OriginalRestorationPolicy.AllowsStaffEvidence(entry.evidence))continue;
                var texture=Resources.Load<Texture2D>(entry.texture);
                if(texture==null || entry.pixelX<0 || entry.pixelY<0
                    || entry.pixelX+texture.width>data.width*32 || entry.pixelY+texture.height>data.height*32)
                    throw new InvalidOperationException("Invalid original counter staff frame.");
                var actor=Child("Original counter staff "+entry.appearanceId+" "+entry.pixelX);
                // Match the photographed sprite rectangle directly. This does not assert a native feet coordinate.
                actor.transform.position=new Vector3((entry.pixelX+texture.width*.5f)/32f,
                    data.height-(entry.pixelY+texture.height*.5f)/32f,0);
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
                if(ShopInteriorCatalog.TryGet(townKey,out _) || townKey=="KaosiaTemple" || townKey=="KaosiaWarehouse")
                {
                    var hit=actor.AddComponent<BoxCollider2D>();hit.isTrigger=true;hit.size=Vector2.one;
                    var counter=actor.AddComponent<ShopCounter2D>();
                    // Authored indoor counters face down. Use the walkable row in front
                    // of the photographed staff, not a circular interaction radius.
                    var front = new List<Vector2Int>();
                    int staffX = Mathf.FloorToInt(actor.transform.position.x);
                    int nativeFrontY = entry.pixelY / 32 + 2;
                    for (int x = staffX - 1; x <= staffX + 1; x++)
                        if (x >= 0 && x < data.width && nativeFrontY < data.height
                            && data.attributes[nativeFrontY * data.width + x] != 9)
                            front.Add(new Vector2Int(x, data.height - 1 - nativeFrontY));
                    counter.ConfigureCounterFront(front.ToArray());
                }
            }
        }
        private void OnDestroy(){foreach(var item in owned)if(item!=null){if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}owned.Clear();}
    }
}
