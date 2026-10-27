using System;
using System.Linq;
using DarkSaver.Core;
using DarkSaver.Events;
using UnityEngine;
namespace DarkSaver.UI
{
    public sealed partial class FirstPlayTutorial
    {
        private NativeBattleWindows windows;
        private void LateUpdate()
        {
            if(session==null || !session.HasPendingTutorial || leaving){ui?.Hide();return;}
            InGameUi.Ensure(ref ui,this,140);ui.Begin();
            if(!ready)
            {
                ui.ModalBlocker("loading-block");ui.SolidRect("loading",new Rect(0,0,640,480),Color.black);
                if(!string.IsNullOrEmpty(error))ui.Label("load-error",new Rect(40,200,560,80),error,color:Color.white);
                ui.End();return;
            }
            Reconcile();PresentPracticeEffects();var command=script.Current(progress);
            if(progress.highlightX>=0)
            {
                var screen=DarkSaver.Rendering.GameScreen.ScreenToGui(view.WorldToScreenPoint(World(progress.highlightX,progress.highlightY)));
                ui.SolidRect("destination",new Rect(screen.x-16,screen.y-16,32,32),new Color(1,1,1,.35f));
            }
            DrawRanges();DrawDialogs();DrawActions();
            DrawDialogue(command);
            if(!string.IsNullOrEmpty(error))ui.Label("request-error",new Rect(40,450,560,28),error,12,color:Color.white);
            ui.End();
        }
        private void DrawDialogue(EventCommand command)
        {
            int cursor=progress.step;
            if(!string.IsNullOrEmpty(progress.message))
            {
                int mode=progress.messageMode;var rect=new Rect(40,ScenarioDialogueLayout.Top(mode),562,ScenarioDialogueLayout.Height(mode));
                ui.NativeTexturePanel("dialogue",rect,Resources.Load<Texture2D>("Tutorial/Dialogue"));

                var portrait=Resources.Load<Texture2D>("UI/CharacterPortraits/"+progress.portrait);
                ui.PortraitTexture("portrait",new Rect(rect.x+20,rect.y+20,64,64),portrait);
                var lines=ScenarioDialogueLayout.Page(mode,progress.message,0).Split('\n');
                for(int i=0;i<lines.Length && i<3;i++)ui.NativeBitmapText("dialogue-line-"+i,new Rect(150,rect.y+23+i*20,439,14),lines[i],Color.white);
                if(command.Name=="WAITMOUSEBUTTON")ui.TransparentButton("dialogue-ack",rect,()=>Advance(cursor));
            }
        }
        private void DrawActions()
        {
            var command=script.Current(progress);int cursor=progress.step;
            var actor=Array.Find(progress.actors,a=>a.id==(openMenu>=0?openMenu:progress.topMenu));if(actor==null)return;

            var layer=ui.OverlayLayer("tutorial-actions-layer",1);
            var cell=ActorRect(actor.id);var r=new Rect(cell.center.x-48,cell.center.y-48,96,96);
            if(subMenu=="Magic")
            {
                ui.ActionButton("fireball",new Rect(r.x+32,r.y,32,32),"",()=>
                {
                    if(cursor!=progress.step)return;
                    // Reopening a selected spell changes presentation only; do not advance stage1 twice.
                    if(command.Arguments[0]=="2" || Send("Magic",actor:actor.id,stage:1,expected:cursor))
                    {subMenu="";magicArmed=true;openMenu=-1;}
                },
                    progress.mouseMenu && command.Name=="WAITMAGICMENU" && (command.Arguments[0]=="1" || command.Arguments[0]=="2"),"파이어볼",FireballIcon(),parent:layer,dimDisabledIcon:false);return;
            }
            if(subMenu=="Order")
            {
                for(int i=0;i<3;i++)
                {
                    int stage=i==0?1:i==2?2:-1;int index=i;
                    ui.ActionButton("order-"+i,new Rect(r.x+(i==0?32:i==1?0:64),r.y+(i==0?0:64),32,32),"",()=>
                    {if(stage>0 && Send("Order",actor:actor.id,stage:stage,expected:cursor)){openMenu=-1;subMenu="";}},
                        progress.mouseMenu && command.Name=="WAITORDERMENU" && command.Arguments[0]==stage.ToString(),
                        new[]{"직접 명령","자동 공격","자동 방어"}[index],BattleIconFrames.Magic(1,i,Time.unscaledTimeAsDouble),parent:layer,dimDisabledIcon:false);
                }
                return;
            }
            int[] bits={16,1,2,8,4,128,64,32};string[] names={"이동","공격","특수기","휴식","마법","설정","용병에게 명령","도구"};
            for(int i=0;i<8;i++)
            {
                if((actor.mask & bits[i])==0 || !NativeBattleActionMenu.AllowsIndex(actor.id,i))continue;
                int index=i;bool enabled=progress.mouseMenu && (actor.mask & bits[i])!=0;
                ui.ActionButton("action-"+i,NativeBattleActionMenu.ButtonRect(r,i),"",()=>ChooseAction(index,actor.id,cursor),enabled,names[i],
                    (actor.mask & bits[i])!=0?BattleIconFrames.Action(NativeBattleActionMenu.IconColumn(i),Time.unscaledTimeAsDouble):null,parent:layer,dimDisabledIcon:false);
            }
        }
        private void ChooseAction(int index,int actor,int cursor)
        {
            if(cursor!=progress.step || !progress.mouseMenu || !NativeBattleActionMenu.AllowsIndex(actor,index))return;
            switch(index)
            {
                case 0:if(script.Current(progress).Name=="WAITMOVEPOS" || Send("MoveMenu",actor:actor,expected:cursor))
                    {movementArmed=true;openMenu=-1;}break;
                case 1:if(script.Current(progress).Name=="WAITATTACKCHAR" || Send("AttackMenu",actor:actor,expected:cursor))
                    {attackArmed=true;openMenu=-1;}break;
                case 3:if(Send("Rest",actor:actor,expected:cursor))openMenu=-1;break;
                case 4:
                    var current=script.Current(progress);
                    if(current.Name=="WAITMAGICMENU" && (current.Arguments[0]=="1" || current.Arguments[0]=="2")
                        || Send("Magic",actor:actor,stage:0,expected:cursor))subMenu="Magic";
                    break;
                case 6:
                    var order=script.Current(progress);
                    if(cursor!=progress.step)return;
                    // Stage1/2 wait for an order choice, not a second stage0 request.
                    if(order.Name=="WAITORDERMENU" && int.TryParse(order.Arguments[1],out var orderActor) && orderActor==actor
                        && (order.Arguments[0]=="1" || order.Arguments[0]=="2"))subMenu="Order";
                    else if(Send("Order",actor:actor,stage:0,expected:cursor))subMenu="Order";
                    break;
            }
        }
        private Sprite FireballIcon()
        {
            var catalog=session.GetComponent<MagicCatalogPanel>()?.Catalog;
            return catalog!=null && catalog.TryGet(101,out var entry)?BattleIconFrames.FromMagicCoordinates(entry.IconX,entry.IconY,Time.unscaledTimeAsDouble):null;
        }
        private void DrawDialogs()
        {
            minimizedWindows.RemoveWhere(key=>!progress.dialogs.Contains(key));
            windows??=Resources.Load<NativeBattleWindows>("UI/BattleWindows/NativeBattleWindows");
            foreach(string key in progress.dialogs.ToArray())
            {
                if(key=="F2"){DrawStatus();continue;}
                if(key=="F5"){DrawRoster();continue;}
                int id=key=="F1"?7:key=="F3"?3:key=="F4"?5:key=="F6"?0:key=="F8"?6:key=="F9"?28:key=="F12"?4:-1;
                var window=windows?.Find(id);
                if(window==null)continue; // F7 falls through the recovered GameCtl key dispatcher; do not invent a player list.
                // GameCtl early0x10002f20 / late0x10002fb0 key dispatcher: low/high words are native X/Y.
                var position=key=="F1"?new Vector2(10,10):key=="F3"?new Vector2(450,302):key=="F4"||key=="F9"?new Vector2(228,186):key=="F6"?new Vector2(515,5):key=="F8"?new Vector2(512,136):Vector2.zero;
                var rect=new Rect(position,window.size);
                if(minimizedWindows.Contains(key) && window.minimizedBackground!=null)
                {
                    ui.NativeMinimizedWindow("window-"+key+"-minimized",new Rect(position,window.minimizedSize),window.minimizedBackground,()=>minimizedWindows.Remove(key));
                    continue;
                }
                if(key=="F12")ui.NativeTexturePanel("window-F12",rect,window.background);
                else ui.NativeBattleWindow("window-"+key,rect,window,()=>ClosePracticeWindow(key),
                    window.minimizedBackground!=null?()=>minimizedWindows.Add(key):null);
                DrawPracticeWindowButtons(key,rect,window);
                if(key=="F8")DrawPracticeTerrain(rect);
                if(key=="F6")DrawMinimap(rect);
                if(key=="F4")ui.NativeBitmapText("victory",new Rect(rect.x+10,rect.y+28,rect.width-20,40),progress.victory,Color.black,3);
                if(key=="F1")ui.NativeBattleWindowText("help",new Rect(rect.x+10,rect.y+28,rect.width-20,rect.height-35),Resources.Load<TextAsset>("UI/BattleWindows/NativeBattleHelp")?.text ?? "");
                if(key=="F3")
                {
                    ui.NativeBitmapText("alliance-self",new Rect(rect.x+12,rect.y+51,105,17),session.PlayerName,Color.black);
                    ui.NativeBitmapText("alliance-teacher",new Rect(rect.x+12,rect.y+70,105,17),"교관",Color.black);
                    // The second row is the teacher; only this explicit wait accepts toggling it.
                    var control=window.controls.Where(c=>c.nativeId==47).Skip(1).FirstOrDefault();
                    if(control!=null)ui.NativeImageButton("alliance-teacher-toggle",rect.position+control.rect.position,control,
                        ()=>Send("Ally",expected:progress.step),script.Current(progress).Name=="WAITALLYBUTTON","동맹");
                }
            }
        }
        private void DrawRoster()
        {
            var members=progress.actors.Where(a=>a.id>=0 && a.id<100 && a.id%10==0 && !a.removed).OrderBy(a=>a.id).Take(4).ToArray();
            if(members.Length==0)return;
            var rect=new Rect(5,231,120,44+41*members.Length);
            if(minimizedWindows.Contains("F5"))
            {
                var collapsed=new Rect(rect.x,rect.y,33,22);ui.NativeStatusPanel("roster-collapsed",collapsed,-1);
                ui.NativeRosterSelection("roster-restore",collapsed,()=>minimizedWindows.Remove("F5"),true,"부대창 복원");return;
            }
            ui.NativeStatusPanel("roster",rect,members.Length);
            ui.NativeStatusName("roster-name",new Rect(rect.x+9,rect.y+9,65,14),PracticeName(members[0]));
            ui.NativeStatusCloseButton("roster-minimize",new Rect(rect.x+77,rect.y+9,15,15),()=>minimizedWindows.Add("F5"),true);
            ui.NativeStatusCloseButton("roster-close",new Rect(rect.x+95,rect.y+9,15,15),()=>ClosePracticeWindow("F5"));
            for(int i=0;i<members.Length;i++)
            {
                var data=members[i];int id=data.id;float y=rect.y+34+41*i;string key="roster-"+id;
                // Native DlgLButtonDown gates roster row selection while TUTORIAL is active.
                // Keep the row as presentation rather than adding a command/selection shortcut.
                if(actors.TryGetValue(id,out var actor))ui.NativeUnitPicture(key+"-unit",new Rect(rect.x+8,y+2,32,32),NativeBattleStatus.UnitPicture(actor.GetComponent<SpriteRenderer>(),data.job));
                int tick=PracticeTick(data);bool full=tick>=GameConstants.BattleMaximumAp;
                ui.NativeStatusGauge(key+"-ap",new Vector2(rect.x+65,y+(full?1:0)),tick,GameConstants.BattleMaximumAp,full?Time.frameCount%3:0,false,48);
                if(data.hp>=0)ui.NativeStatusGauge(key+"-hp",new Vector2(rect.x+65,y+14),data.hp,data.maxHp,1,true,48);
                if(data.mp>=0)ui.NativeStatusGauge(key+"-mp",new Vector2(rect.x+65,y+27),data.mp,data.maxMp,2,true,48);
            }
        }
        private RenderTexture minimap;
        private Camera minimapCamera;
        private void DrawMinimap(Rect r)
        {
            if(minimap==null)
            {
                minimap=new RenderTexture(110,83,16);minimap.filterMode=FilterMode.Point;
                var go=new GameObject("Tutorial minimap camera",typeof(Camera));go.transform.SetParent(transform,false);
                minimapCamera=go.GetComponent<Camera>();minimapCamera.enabled=false;minimapCamera.orthographic=true;minimapCamera.orthographicSize=7.5f;
                minimapCamera.aspect=20f/15;minimapCamera.transform.position=new Vector3(10,7.5f,view.transform.position.z);minimapCamera.targetTexture=minimap;
            }
            minimapCamera.Render();ui.RenderTexture("minimap",new Rect(r.x+6,r.y+28,110,83),minimap);
        }
        private void DrawStatus()
        {
            int id=hovered<0?inspectedActor:hovered;if(!actors.TryGetValue(id,out var actor))return;
            if(hovered>=0)inspectedActor=hovered;
            var data=Array.Find(progress.actors,a=>a.id==id);if(data==null)return;
            if(minimizedWindows.Contains("F2")){DrawPracticeCompactStatus(data,actor);return;}
            var rect=new Rect(5,180,120,213);ui.NativeStatusPanel("status",rect);
            ui.NativeStatusCloseButton("status-close",new Rect(rect.x+96,rect.y+6,15,15),()=>ClosePracticeWindow("F2"));
            ui.NativeStatusCloseButton("status-minimize",new Rect(rect.x+78,rect.y+6,15,15),()=>minimizedWindows.Add("F2"),true);
            ui.NativeUnitPicture("status-unit",new Rect(rect.x+44,rect.y+79,32,32),NativeBattleStatus.UnitPicture(actor.GetComponent<SpriteRenderer>(),data.job));
            ui.NativeStatusName("status-name",new Rect(rect.x+9,rect.y+7,68,14),id==0?session.PlayerName:id==100?"교관":"");
            // Preserve literal CHARMENU ticks (including99/999); only the drawn strip saturates.
            // Same native AP row/position as the ordinary full status window; no new numeric label.
            ui.NativeStatusGauge("practice-ap",new Vector2(rect.x+30,rect.y+143),PracticeTick(data),GameConstants.BattleMaximumAp,
                PracticeTick(data)>=GameConstants.BattleMaximumAp?Time.frameCount%3:0,false);
            // Script HP values are practice state, never written to permanent vitals.
            if(data.hp>=0)ui.NativeStatusGauge("practice-hp",new Vector2(rect.x+30,rect.y+156),data.hp,data.maxHp,1);
            if(data.mp>=0)ui.NativeStatusGauge("practice-mp",new Vector2(rect.x+30,rect.y+169),data.mp,data.maxMp,2);
        }
    }
}
