using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Events;
using DarkSaver.Combat;
using UnityEngine;
namespace DarkSaver.UI
{
    public sealed partial class FirstPlayTutorial
    {
        private readonly Dictionary<int,OriginalTutorialTickCounter> practiceTicks=new();
        private readonly HashSet<string> minimizedWindows=new();
        private int inspectedActor;
        private CombatGridCell terrainPointer=new CombatGridCell(0,0);
        private RenderTexture terrainPreview;
        private Camera terrainPreviewCamera;
        private int PracticeTick(TutorialActor actor)
        {
            if(!practiceTicks.TryGetValue(actor.id,out var counter))practiceTicks.Add(actor.id,counter=new OriginalTutorialTickCounter());
            return counter.Read(actor.tick,actor.tickEpoch,GameConstants.BattleMaximumAp);
        }
        private void AdvancePracticeTicks(float seconds)
        {
            // Explicit CHARMENU/DO demonstration holds the source's successive values.
            // Outside that demonstration, the configured client display clock may advance.
            foreach(var a in progress.actors)
            {
                if(a.removed)continue;
                PracticeTick(a);
                if(script==null || !script.IsTickDemonstration(progress))
                    practiceTicks[a.id].Advance(seconds,GameConstants.BattleApRecoveryPerSecond,GameConstants.BattleMaximumAp);
            }
        }
        private void ClosePracticeWindow(string key)
        {
            if(Send("WindowClose",key:key))minimizedWindows.Remove(key);
        }
        private void DrawPracticeWindowButtons(string key,Rect rect,NativeBattleWindows.Window window)
        {
            // Original scene4 action table is indexed by control order, not by the F-key number.
            for(int i=0;i<window.controls.Length;i++)
            {
                var control=window.controls[i];int id=control.nativeId;
                if(key=="F12")
                {
                    string target=OriginalTutorialScript.WindowKeyForControl(id);
                    if(target!=null)ui.NativeImageButton("window-F12-control-"+i,rect.position+control.rect.position,control,
                        ()=>Send("WindowButton",actor:id),true,target=="F12"?"닫기":target);
                }
            }
        }
        private bool TryGetPracticeTerrain(out Sprite image,out int attack,out int defense,out int movement)
        {
            image=null;attack=defense=movement=0;if(!EnsurePracticeTerrain())return false;
            var actor=Array.Find(progress.actors,a=>a.id==inspectedActor && !a.removed)
                ?? Array.Find(progress.actors,a=>a.id==0);if(actor==null)return false;
            var bounds=rangeGround.cellBounds;var cell=terrainPointer;
            var native=new Vector3Int(cell.X,bounds.yMax-1-cell.Y,0);
            if(!bounds.Contains(native) || !rangeGround.HasTile(native)
                || !BattleMapPresentation.TryGetAttribute(rangeTerrain,bounds,cell,out var attribute))return false;
            // Native cursor tile, with last inspected unit's troop; dmSetChar jobs100..299 force0.
            int troop=actor.job>=100 && actor.job<=299?0:actor.job==301?2:actor.job==805?0:-1;
            if(!OriginalTutorialTerrain.TryGet(attribute,troop,out attack,out defense,out movement))return false;
            image=rangeGround.GetSprite(native);return true;
        }
        private void DrawPracticeTerrain(Rect rect)
        {
            if(!TryGetPracticeTerrain(out var image,out int attack,out int defense,out int movement))return;
            if(view!=null)
            {
                if(terrainPreview==null)
                {
                    terrainPreview=new RenderTexture(32,32,16){filterMode=FilterMode.Point,antiAliasing=1};
                    var go=new GameObject("Tutorial native terrain tile capture",typeof(Camera));go.transform.SetParent(transform,false);
                    terrainPreviewCamera=go.GetComponent<Camera>();terrainPreviewCamera.enabled=false;
                }
                // GameCtl0x100085DA..85FA captures the rendered32px cursor tile, including detail/unit layers.
                terrainPreviewCamera.CopyFrom(view);terrainPreviewCamera.enabled=false;terrainPreviewCamera.targetTexture=terrainPreview;
                terrainPreviewCamera.rect=new Rect(0,0,1,1);terrainPreviewCamera.aspect=1;terrainPreviewCamera.orthographic=true;
                terrainPreviewCamera.orthographicSize=.5f;terrainPreviewCamera.ResetProjectionMatrix();
                terrainPreviewCamera.transform.SetPositionAndRotation(new Vector3(terrainPointer.X+.5f,14.5f-terrainPointer.Y,view.transform.position.z),view.transform.rotation);
                terrainPreviewCamera.Render();ui.RenderTexture("tutorial-terrain-tile",new Rect(rect.x+10,rect.y+36,32,32),terrainPreview);
            }
            int[] values={attack,defense,movement};
            for(int i=0;i<3;i++)ui.NativeBitmapText("tutorial-terrain-value-"+i,new Rect(rect.x+81,rect.y+33+17*i,29,14),
                OriginalTutorialTerrain.Display(values[i]),new Color(8f/255,8f/255,8f/255));
        }
        private void ReleasePracticeTerrainPreview()
        {
            if(terrainPreviewCamera!=null){terrainPreviewCamera.targetTexture=null;ReleasePracticeObject(terrainPreviewCamera.gameObject);}terrainPreviewCamera=null;
            if(terrainPreview!=null){terrainPreview.Release();if(Application.isPlaying)Destroy(terrainPreview);else DestroyImmediate(terrainPreview);}terrainPreview=null;
        }
        private void DrawPracticeCompactStatus(TutorialActor data,GameObject actor)
        {
            var r=new Rect(5,180,120,85);ui.NativeStatusPanel("status-compact",r,1);
            ui.NativeStatusName("status-compact-name",new Rect(r.x+9,r.y+9,65,14),PracticeName(data));
            ui.NativeStatusCloseButton("status-compact-close",new Rect(r.x+95,r.y+9,15,15),()=>ClosePracticeWindow("F2"));
            ui.NativeRosterSelection("status-expand",new Rect(r.x+5,r.y+30,110,48),()=>minimizedWindows.Remove("F2"),true,"크게 보기");
            ui.NativeUnitPicture("status-compact-unit",new Rect(r.x+8,r.y+36,32,32),NativeBattleStatus.UnitPicture(actor.GetComponent<SpriteRenderer>(),data.job));
            int tick=PracticeTick(data);
            ui.NativeStatusGauge("status-compact-ap",new Vector2(r.x+65,r.y+34),tick,GameConstants.BattleMaximumAp,tick>=GameConstants.BattleMaximumAp?Time.frameCount%3:0,false,48);
            if(data.hp>=0)ui.NativeStatusGauge("status-compact-hp",new Vector2(r.x+65,r.y+48),data.hp,data.maxHp,1,true,48);
            if(data.mp>=0)ui.NativeStatusGauge("status-compact-mp",new Vector2(r.x+65,r.y+61),data.mp,data.maxMp,2,true,48);
        }
        private string PracticeName(TutorialActor actor)=>actor.id==0?session.PlayerName:actor.id==100?"교관":"";
    }
}
