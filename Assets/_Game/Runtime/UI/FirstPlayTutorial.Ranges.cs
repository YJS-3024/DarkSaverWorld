using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
namespace DarkSaver.UI
{
    public sealed partial class FirstPlayTutorial
    {
        private Tilemap rangeGround,rangeTerrain;
        private readonly List<SpriteRenderer> rangeBodies=new();
        private readonly Dictionary<string,Sprite> rangeSprites=new();
        private string rangeSignature;
        public int RangeCellCount {get;private set;}
        private void DrawRanges()
        {
            var mode=TutorialPresentationRules.RangeMode(progress.activeCommand,movementArmed,attackArmed,magicArmed,
                progress.actors.Any(a=>a.id==0 && a.moving) || openMenu>=0 || progress.topMenu>=0 || !string.IsNullOrEmpty(subMenu));
            if(mode==TutorialRangeMode.None){HideRanges();return;}
            if(!EnsurePracticeTerrain()){HideRanges();return;}
            var catalog=session.GetComponent<MagicCatalogPanel>()?.Catalog;
            DarkSaver.Magic.MagicCatalogEntry magic=null;
            if(mode==TutorialRangeMode.Magic && (catalog==null || !catalog.TryGet(101,out magic) || magic.Range<0 || magic.Range>short.MaxValue))
            {HideRanges();return;}
            var floor=rangeGround.GetComponent<TilemapRenderer>();
            int rangeOrder=(floor==null?-100:floor.sortingOrder);
            // Range masks must cover the flat Detail tiles as well as Ground.
            // Keep the native map/actor positions and foreground objects unchanged.
            var parent=rangeGround.transform.parent;
            if(parent!=null)
                foreach(var mapRenderer in parent.GetComponentsInChildren<TilemapRenderer>(true))
                    if(mapRenderer.enabled && mapRenderer.name=="Detail"
                        && mapRenderer.sortingLayerID==(floor==null?0:floor.sortingLayerID))
                        rangeOrder=Math.Max(rangeOrder,mapRenderer.sortingOrder);
            rangeOrder++;
            string signature=mode+":"+rangeOrder+":"+progress.ally+":"+(magic?.Range ?? -1)+":"+string.Join(";",progress.actors.Select(a=>$"{a.id},{a.x},{a.y},{a.dead},{a.removed}"));
            if(signature==rangeSignature)return;
            rangeSignature=null;HideRanges();
            var actor=Array.Find(progress.actors,a=>a.id==0);if(actor==null)return;
            var bounds=rangeGround.cellBounds;int width=bounds.size.x,height=bounds.size.y;
            int? Cost(CombatGridCell cell)
            {
                if(!rangeGround.HasTile(new Vector3Int(cell.X,bounds.yMax-1-cell.Y,0))
                    || !BattleMapPresentation.TryGetAttribute(rangeTerrain,bounds,cell,out var attribute)
                    || !CombatTerrainTraversal.TryGetMovementValue(attribute,OriginalTutorialRanges.TroopColumn,out int cost))return null;
                return cost;
            }
            int? Occupant(CombatGridCell cell)
            {var unit=Array.Find(progress.actors,a=>!a.dead && !a.removed && a.x==cell.X && a.y==cell.Y);return unit?.id;}
            IEnumerable<(int,int)> area=null;
            if(mode!=TutorialRangeMode.Magic)
            {
                if(!CombatTerrainTraversal.TryBuildMovementArea(width,height,new CombatGridCell(actor.x,actor.y),
                    mode==TutorialRangeMode.Move?OriginalTutorialRanges.MovementBudget:OriginalTutorialRanges.AttackRadius,
                    actor.id,Cost,mode==TutorialRangeMode.Move?Occupant:_=>(int?)null,out var scores))return;
                area=scores.Keys.Select(c=>(c.X,c.Y));
            }
            // Source tutorial groups: own0, instructor1 (SETALLYINFO), practice monsters4.
            var relations=Enumerable.Repeat(4,8).ToArray();relations[0]=2;relations[1]=progress.ally==0?2:4;
            bool Eligible(DarkSaver.Core.TutorialActor unit)=>CombatMagicTargets.TryAllowsTeam(mode==TutorialRangeMode.Magic?magic.Team:2,
                actor.id/100,unit.id/100,actor.id/100,relations,out bool allowed) && allowed;
            if(!OriginalTutorialRanges.TryBuild(progress,actor.id,mode,width,height,area,(int)(magic?.Range ?? 0),Eligible,out var cells))return;
            for(int i=0;i<cells.Length;i++)
            {
                var cell=cells[i];string kind=cell.Target?"Target":mode.ToString();
                if(!rangeSprites.TryGetValue(kind,out var sprite))
                {
                    var texture=Resources.Load<Texture2D>("Tutorial/Ranges/"+kind);
                    if(texture==null){HideRanges();return;}
                    sprite=Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect);rangeSprites.Add(kind,sprite);
                }
                if(i==rangeBodies.Count)
                {var go=new GameObject("Tutorial range cell",typeof(SpriteRenderer));go.transform.SetParent(transform,false);rangeBodies.Add(go.GetComponent<SpriteRenderer>());}
                var body=rangeBodies[i];body.sprite=sprite;body.transform.position=World(cell.X,cell.Y);
                body.sortingLayerID=floor==null?0:floor.sortingLayerID;body.sortingOrder=rangeOrder;body.enabled=true;
            }
            RangeCellCount=cells.Length;rangeSignature=signature;
        }
        private bool EnsurePracticeTerrain()
        {
            if(rangeGround!=null && rangeTerrain!=null)return true;
            var scene=SceneManager.GetSceneByName(TutorialMap);if(!scene.isLoaded)return false;
            var maps=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Tilemap>(true)).ToArray();
            rangeGround=maps.FirstOrDefault(t=>t.name=="Ground");rangeTerrain=maps.FirstOrDefault(t=>t.name=="TerrainAttributes");
            return rangeGround!=null && rangeTerrain!=null;
        }
        private void HideRanges(){foreach(var body in rangeBodies)if(body!=null)body.enabled=false;RangeCellCount=0;rangeSignature=null;}
        private void ReleaseRanges()
        {
            foreach(var body in rangeBodies)if(body!=null)Destroy(body.gameObject);rangeBodies.Clear();
            foreach(var sprite in rangeSprites.Values)if(sprite!=null)Destroy(sprite);rangeSprites.Clear();HideRanges();rangeGround=rangeTerrain=null;
        }
    }
}
