using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.UI;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Tutorial;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private sealed class BattleDialog
        {
            public WaitingWindowDefinition Spec;public RectTransform Root,Full,Mini;
            public bool Minimized;public int HelpOffset;public readonly NativeWindowClickClock Clicks=new NativeWindowClickClock();
            public Text[] Lines;public RawImage Map;public int ReadyPhase;public RectTransform TerrainPreview;
            public readonly List<RawImage> PreviewImages=new List<RawImage>();public readonly List<Text> PreviewText=new List<Text>();public readonly List<RawImage> Viewport=new List<RawImage>();
            public readonly List<RawImage> Markers=new List<RawImage>();
            public readonly List<BattleDialogUnit> Units=new List<BattleDialogUnit>();
        }
        private sealed class BattleDialogUnit
        {
            public int Id,Width,ApY;public bool FollowsSelection,Troop;public RectTransform Root;public RawImage Body,Ap,Hp,Mp,Experience;public Text Name,Class,Level;
            public RawImage[] HpDigits,MpDigits,ExperienceDigits,AttackDigits,DefenseDigits,Conditions;
        }
        // Native menu clock() is a client UI clock, independent of server/world progression.
        private static double BattleIconClock=>Time.unscaledTimeAsDouble*1000.0;
        private NativeBattleWindowArt battleWindowArt;
        private readonly Dictionary<int,BattleDialog> battleDialogs=new Dictionary<int,BattleDialog>();
        private int battleDialogMap=-1;private string battleDialogSession;
        private string[] battleHelpLines;
        private readonly NativeBattleCompactClickClock battleCompactClock=new NativeBattleCompactClickClock();
        private int? battleCameraX,battleCameraY;private int battleStatusUnit=-1;private Vector2? battleStatusMouse;
        private readonly List<RawImage> battleTerrainSources=new List<RawImage>();private readonly List<Text> battleTerrainText=new List<Text>();
        private void EnsureBattleWindowArt()
        {if(battleWindowArt==null)battleWindowArt=Resources.Load<NativeBattleWindowArt>("OriginalBattleWindows");}
        private int ActiveBattleCommanders=>Enumerable.Range(0,4).Count(i=>{var u=view.battle?.Unit(i*10);return u!=null&&u.nativeActive!=false&&u.nativeEnabled!=false;});
        private void ShowTrainingOptions()
        {
            if(!BattleMap()||view.presentation?.completed==false||notice!=null||clientMessageState.Visible)return;
            if(battleDialogs.ContainsKey(4))CloseBattleDialog(4);else OpenBattleDialog(4);
        }
        private int BattleDialogRequest(int message,int scene,int x,int y)
        {
            if(!BattleMap()||view.presentation?.completed==false)return 0;
            if(message==NativeBattleOptions.Exists)return battleDialogs.ContainsKey(scene)?1:0;
            if(message==NativeBattleOptions.Open)OpenBattleDialog(scene);
            else if(message==NativeBattleOptions.Close)CloseBattleDialog(scene);
            else if(message==NativeBattleOptions.Position)
            {
                if(!NativeBattleOptions.PositionAllowed(scene,false))return -1;
                if(battleDialogs.TryGetValue(scene,out var p))p.Root.anchoredPosition=new Vector2(x,-y);
            }
            return 0;
        }
        private BattleDialog OpenBattleDialog(int scene)
        {
            if(battleDialogs.TryGetValue(scene,out var existing))return existing;
            EnsureBattleWindowArt();int index=battleWindowArt==null?-1:battleWindowArt.Index(scene);if(index<0)return null;
            var spec=battleWindowArt.Catalog.windows[index];var saved=OriginalWindowPreferences.LoadState(spec);
            var p=new BattleDialog{Spec=spec,Minimized=saved.Minimized};
            p.Root=Group(worldWindows,"Original Glib scene"+scene,new Rect(saved.Position.X,saved.Position.Y,spec.width,spec.height));
            p.Full=Group(p.Root,"Native full panel",new Rect(0,0,spec.width,spec.height));
            p.Full.gameObject.AddComponent<Image>().color=Color.clear;
            p.Full.gameObject.AddComponent<NativeWindowClickInput>().Configure(p.Clicks);
            Picture(p.Full,"Original Glib pixels",new Rect(0,0,spec.width,spec.height),battleWindowArt.full[index]);
            battleDialogs.Add(scene,p);
            if(spec.dragWidth>0&&spec.dragHeight>0)
            {
                var region=new Rect(spec.dragX,spec.dragY,spec.dragWidth,spec.dragHeight);var title=scene==9?p.Full:Hit(p.Full,"Native title",region,()=>p.Root.SetAsLastSibling()).transform;
                if(scene!=9)title.gameObject.AddComponent<NativeWindowClickInput>().Configure(p.Clicks);
                title.gameObject.AddComponent<NativeWindowDragInput>().Configure(screen,p.Root,region,()=>NativeWindowDragAllowed);
            }
            for(int i=0;i<spec.buttons.Length;i++)
            {
                int ordinal=i;var b=spec.buttons[i];var hit=Hit(p.Full,"Original control"+i,new Rect(b.x,b.y,b.width,b.height),()=>BattleDialogControl(scene,ordinal));
                if(scene==7&&i<2||scene==3&&i>=6){hit.onClick.RemoveAllListeners();hit.gameObject.AddComponent<NativeRepeatButton>().Configure(()=>BattleDialogControl(scene,ordinal));}
                var image=Picture((RectTransform)hit.transform,"Original button states",new Rect(0,0,b.width,b.height),art.battle.buttons);
                if(scene==3&&i>=2&&i<=5)
                {hit.interactable=false;image.uvRect=NativeEntryLayout.Uv(art.battle.buttons,new Rect(b.sx+(i==2?b.width:0),b.sy,b.width,b.height));}
                else hit.gameObject.AddComponent<NativeEntryButton>().Configure(image,art.battle.buttons,new Rect(b.sx,b.sy,b.width,b.height),new Rect(b.sx+b.width,b.sy,b.width,b.height),new Rect(b.sx+2*b.width,b.sy,b.width,b.height));
            }
            if(battleWindowArt.minimized[index]!=null)
            {
                var restore=Hit(p.Root,"Restore native minimized window",new Rect(0,0,spec.minWidth,spec.minHeight),()=>{});restore.gameObject.AddComponent<NativeWindowClickInput>().Configure(p.Clicks,()=>{p.Minimized=false;PaintBattleDialogState(p);});p.Mini=(RectTransform)restore.transform;
                Picture(p.Mini,"Native minimized pixels",new Rect(0,0,spec.minWidth,spec.minHeight),battleWindowArt.minimized[index]);
                restore.gameObject.AddComponent<NativeWindowDragInput>().Configure(screen,p.Root,new Rect(spec.minDragX,spec.minDragY,spec.minDragWidth,spec.minDragHeight),()=>NativeWindowDragAllowed);
            }
            BuildBattleDialogContents(p);PaintBattleDialogState(p);return p;
        }
        private void PaintBattleDialogState(BattleDialog p)
        {
            p.Full.gameObject.SetActive(!p.Minimized);if(p.Mini!=null)p.Mini.gameObject.SetActive(p.Minimized);
            p.Root.sizeDelta=new Vector2(p.Minimized?p.Spec.minWidth:p.Spec.width,p.Minimized?p.Spec.minHeight:p.Spec.height);
        }
        private void CloseBattleDialog(int scene)
        {
            if(!battleDialogs.TryGetValue(scene,out var p))return;SaveBattleDialog(p);battleDialogs.Remove(scene);if(p.Root!=null){p.Root.gameObject.SetActive(false);Destroy(p.Root.gameObject);}
        }
        private void SaveBattleDialog(BattleDialog p)
        {if(p.Root==null)return;OriginalWindowPreferences.Save(p.Spec,Mathf.RoundToInt(p.Root.anchoredPosition.x),Mathf.RoundToInt(-p.Root.anchoredPosition.y),minimized:p.Minimized);}
        private void ResetBattleDialogs()
        {foreach(int scene in battleDialogs.Keys.ToArray())CloseBattleDialog(scene);battleCameraX=battleCameraY=null;battleDialogMap=-1;battleDialogSession=null;battleStatusUnit=-1;battleStatusMouse=null;}
        private void BattleDialogControl(int scene,int control)
        {
            if(!battleDialogs.TryGetValue(scene,out var p)||!BattleMap()||notice!=null||clientMessageState.Visible)return;
            p.Root.SetAsLastSibling();
            if(scene==4){NativeBattleOptions.Activate(control,ActiveBattleCommanders,BattleDialogRequest);return;}
            if(scene==1){if(control==0){OpenBattleDialog(9);CloseBattleDialog(1);}else if(control==1)CloseBattleDialog(1);return;}
            if(scene==0||scene==3||scene>=24&&scene<=27)
            {if(control==0){p.Minimized=true;PaintBattleDialogState(p);}else if(control==1)CloseBattleDialog(scene);return;}
            if(scene==7){if(control==2)CloseBattleDialog(7);else{p.HelpOffset=NativeBattleOptions.ScrollHelp(p.HelpOffset,control,battleHelpLines.Length);PaintBattleHelp(p);}return;}
            if(scene==28)
            {
                string session=view.campaignSession;int map=view.record.map;string visit=view.record.scenarioVisit?.id;CloseBattleDialog(28);
                if(control==0&&server.WorldSnapshot?.campaignSession==session&&server.WorldSnapshot.record.map==map)
                    Handle(DisplayMap(map).training?server.Request_EscapeTraining():server.Request_EscapeScenario(visit));
                return;
            }
            if(control==0)CloseBattleDialog(scene);
        }
        private Text BattleText(RectTransform root,string name,Rect rect,string value="")
        {var text=Label(root,name,rect,value);text.color=new Color32(8,8,8,255);text.horizontalOverflow=HorizontalWrapMode.Overflow;return text;}
        private void BuildBattleDialogContents(BattleDialog p)
        {
            int scene=p.Spec.scene;
            if(scene==7)
            {
                if(battleHelpLines==null)
                {string text=(art.battle.help?.text??"").Replace("\r\n","\n");if(text.EndsWith("\n"))text=text.Substring(0,text.Length-1);battleHelpLines=text.Length==0?Array.Empty<string>():text.Split('\n').Take(100).ToArray();}
                p.Lines=Enumerable.Range(0,19).Select(i=>BattleText(p.Full,"Original help line"+i,new Rect(50,41+i*20,532,16))).ToArray();PaintBattleHelp(p);
            }
            else if(scene==5)
            {var labels=NativeBattleOptions.Briefing(battleWindowArt.Briefing(view.record.map));p.Lines=new[]{BattleText(p.Full,"V1",new Rect(10,31,170,16),labels[0]),BattleText(p.Full,"V2",new Rect(10,51,170,16),labels[1])};}
            else if(scene==3)
            {p.Lines=new[]{BattleText(p.Full,"Received own player",new Rect(20,55,103,16),character.name)};p.Lines[0].color=Color.white;}
            else if(scene==0)
            {
                p.Map=Picture(p.Full,"Native hmap stretched100x100",new Rect(11,25,100,100),battleWindowArt.Minimap(view.record.map));p.Map.color=p.Map.texture==null?Color.black:Color.white;
                p.Full.gameObject.AddComponent<NativeBattleWindowPointer>().Configure((x,y,clock)=>
                {if(notice!=null||!BattleMap())return;var map=DisplayMap(mapId).map;if(NativeBattleMinimap.Camera(x,y,map.width,map.height,out int left,out int top)){battleCameraX=left;battleCameraY=top;}});
            }
            else if(scene==6)
            {
                p.TerrainPreview=Group(p.Full,"Native framebuffer tile preview",new Rect(10,36,32,32));p.TerrainPreview.gameObject.AddComponent<RectMask2D>();
                p.Lines=Enumerable.Range(0,3).Select(i=>BattleText(p.Full,"Native tile attribute"+i,new Rect(81,33+i*17,32,16))).ToArray();
            }
            else if(scene==1||scene==9||scene>=24&&scene<=27)
            {
                if(scene>=24){BattleText(p.Full,"Native player name",new Rect(9,9,103,16),character.name);for(int i=0;i<4;i++)p.Units.Add(BuildBattleDialogUnit(p,i*10,false,false,34));}
                else p.Units.Add(BuildBattleDialogUnit(p,0,scene==1,scene==9,0));
                if(scene==9)p.Full.gameObject.AddComponent<NativeBattleWindowPointer>().Configure((x,y,clock)=>{if(notice!=null)return;if(battleCompactClock.Press(clock)){CloseBattleDialog(9);OpenBattleDialog(1);}});
            }
        }
        private void UpdateBattleStatusHover()
        {
            if(!BattleMap()||mapRoot==null||view.battle==null||view.presentation?.completed==false)return;
            Vector2 mouse=Input.mousePosition;if(battleStatusMouse.HasValue&&battleStatusMouse.Value==mouse)return;battleStatusMouse=mouse;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(screen,mouse,null,out var screenPoint)||screenPoint.x<0||screenPoint.x>=640||screenPoint.y>0||screenPoint.y<=-480)return;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRoot,mouse,null,out var point))return;
            int x=Mathf.FloorToInt(point.x/32),y=Mathf.FloorToInt(-point.y/32);var unit=view.battle.units.FirstOrDefault(u=>u.x==x&&u.y==y&&u.nativeActive!=false&&u.nativeEnabled!=false&&u.hp>0);
            battleStatusUnit=NativeBattleStatusSelection.Hover(battleStatusUnit,unit?.id??-1);
        }
        private void PaintBattleHelp(BattleDialog p)
        {for(int i=0;i<p.Lines.Length;i++){int row=p.HelpOffset+i;p.Lines[i].text=row>=0&&row<battleHelpLines.Length?battleHelpLines[row]:"";}}
        private void PaintBattleDialogs()
        {
            bool battle=BattleMap(),visible=battle&&view.presentation?.completed!=false;
            if(battleDialogMap!=view.record.map||battleDialogSession!=view.campaignSession){ResetBattleDialogs();battleDialogMap=view.record.map;battleDialogSession=view.campaignSession;}
            UpdateBattleStatusHover();
            foreach(var p in battleDialogs.Values)
            {
                p.Root.gameObject.SetActive(visible);if(!visible||p.Minimized)continue;
                p.ReadyPhase=(p.ReadyPhase+1)%3;int row=0;
                foreach(var unit in p.Units)
                {
                    if(p.Spec.scene>=24){var source=view.battle?.Unit(unit.Id);bool active=source!=null&&source.nativeActive!=false&&source.nativeEnabled!=false;unit.Root.gameObject.SetActive(active);if(!active)continue;unit.Root.anchoredPosition=new Vector2(0,-41*row++);}
                    PaintBattleDialogUnit(unit,p.ReadyPhase);
                }
                if(p.Spec.scene==0)PaintBattleMinimap(p);
                if(p.Spec.scene==6)PaintBattleTerrain(p);
            }
            if(battleDialogs.TryGetValue(1,out var status))status.Root.SetAsLastSibling();else if(battleDialogs.TryGetValue(9,out var compact))compact.Root.SetAsLastSibling();
        }
        private void PaintBattleMinimap(BattleDialog p)
        {
            var map=DisplayMap(mapId).map;int index=0;
            foreach(var unit in view.battle?.units??Array.Empty<TrainingUnit>())
            {
                if(unit.nativeActive==false||unit.nativeEnabled==false)continue;
                int? palette=unit.id>=400?unit.id/100:(unit.id<50?(int?)art.ownPalette:unit.nativePalette);if(!palette.HasValue||palette<0||palette>9)continue;
                if(index==p.Markers.Count)p.Markers.Add(Picture(p.Full,"Native3px marker",new Rect(0,0,3,3),battleWindowArt.markers));
                var image=p.Markers[index++];image.gameObject.SetActive(true);Place((RectTransform)image.transform,new Rect(11+unit.x*100/map.width,25+unit.y*100/map.height,3,3));image.uvRect=NativeEntryLayout.Uv(battleWindowArt.markers,new Rect(192+palette.Value*3,0,3,3));
            }
            while(index<p.Markers.Count)p.Markers[index++].gameObject.SetActive(false);
            while(p.Viewport.Count<4)p.Viewport.Add(Picture(p.Full,"Native minimap viewport edge",new Rect(0,0,1,1),art.battle.board,new Rect(100,400,1,1)));
            int left=Mathf.FloorToInt(-mapRoot.anchoredPosition.x/32),top=Mathf.FloorToInt(mapRoot.anchoredPosition.y/32);
            int x=11+left*100/map.width,y=25+top*100/map.height,right=11+(left+18)*100/map.width,bottom=25+(top+13)*100/map.height;
            var edges=new[]{new Rect(x,y,right-x,1),new Rect(x,y,1,bottom-y),new Rect(x,bottom,right-x,1),new Rect(right,y,1,bottom-y)};
            for(int i=0;i<4;i++){Place((RectTransform)p.Viewport[i].transform,edges[i]);p.Viewport[i].transform.SetAsLastSibling();}
        }
        private void PaintBattleTerrain(BattleDialog p)
        {
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRoot,Input.mousePosition,null,out var point))return;
            var tile=new TutorialTile(Mathf.FloorToInt(point.x/32),Mathf.FloorToInt(-point.y/32));var map=DisplayMap(mapId).map;
            if(!map.Contains(tile))return;
            PaintBattleTilePreview(p,tile);
            int? kind=OriginalMemberCommands.MovementType(view.battle?.Unit(NativeBattleStatusSelection.Display(battleStatusUnit,0)),definition.combatCatalog);
            for(int i=0;i<3;i++){int value=kind.HasValue&&map.Contains(tile)?map.Terrain(tile,kind.Value*3+i):99;p.Lines[i].text=Math.Abs((long)value)==99?"**":value.ToString(System.Globalization.CultureInfo.InvariantCulture);}
        }
        private Rect BattlePreviewRect(RectTransform source,TutorialTile tile)
        {
            var top=mapRoot.InverseTransformPoint(source.TransformPoint(new Vector3(source.rect.xMin,source.rect.yMax)));
            var bottom=mapRoot.InverseTransformPoint(source.TransformPoint(new Vector3(source.rect.xMax,source.rect.yMin)));
            return new Rect(top.x-tile.X*32,-top.y-tile.Y*32,bottom.x-top.x,top.y-bottom.y);
        }
        private void PaintBattleTilePreview(BattleDialog p,TutorialTile tile)
        {
            int index=0;mapRoot.GetComponentsInChildren(false,battleTerrainSources);
            foreach(var source in battleTerrainSources)
            {
                if(!source.enabled||source.texture==null)continue;var rect=BattlePreviewRect((RectTransform)source.transform,tile);
                if(rect.xMax<=0||rect.yMax<=0||rect.xMin>=32||rect.yMin>=32)continue;
                if(index==p.PreviewImages.Count)p.PreviewImages.Add(Picture(p.TerrainPreview,"Native map pass",rect,source.texture));
                var image=p.PreviewImages[index++];image.gameObject.SetActive(true);Place((RectTransform)image.transform,rect);image.texture=source.texture;image.uvRect=source.uvRect;image.color=source.color;
            }
            while(index<p.PreviewImages.Count)p.PreviewImages[index++].gameObject.SetActive(false);
            index=0;mapRoot.GetComponentsInChildren(false,battleTerrainText);
            foreach(var source in battleTerrainText)
            {
                if(!source.enabled||string.IsNullOrEmpty(source.text))continue;var rect=BattlePreviewRect((RectTransform)source.transform,tile);
                if(rect.xMax<=0||rect.yMax<=0||rect.xMin>=32||rect.yMin>=32)continue;
                if(index==p.PreviewText.Count)p.PreviewText.Add(Label(p.TerrainPreview,"Native map text",rect,""));
                var label=p.PreviewText[index++];label.gameObject.SetActive(true);Place((RectTransform)label.transform,rect);label.text=source.text;label.color=source.color;label.alignment=source.alignment;label.horizontalOverflow=source.horizontalOverflow;label.verticalOverflow=source.verticalOverflow;label.transform.SetAsLastSibling();
            }
            while(index<p.PreviewText.Count)p.PreviewText[index++].gameObject.SetActive(false);
        }
    }
}
