using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.UI;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private sealed class UtilityWindow
        {
            public WaitingWindowDefinition Spec;public RectTransform Root;
            public readonly List<(Button hit,RawImage image,WaitingButton spec)> Controls=new List<(Button,RawImage,WaitingButton)>();
            public readonly List<(Text sender,Text date,Text body)> Memos=new List<(Text,Text,Text)>();
            public long MemoRevision;public int MemoPage;public string MemoSession;public NativeMemo[] MemoRows;
            public bool[] Settings;public Text FontName;public RectTransform HelpContent,HelpThumb;public int HelpOffset,HelpMaximum,HelpLines;
        }
        private readonly Dictionary<int,UtilityWindow> utilities=new Dictionary<int,UtilityWindow>();
        private void ResetTransientClientWindows()
        {
            ClearClientMessage();FinishCommanderName(false);commanderNameLastTextFocus=null;SavePersonalPositions();
            foreach(var p in utilities.Values){SaveUtilityPosition(p);p.Root.gameObject.SetActive(false);Destroy(p.Root.gameObject);}utilities.Clear();
            foreach(var p in personalWindows.Values)
            {p.Visible=false;var saved=OriginalWindowPreferences.LoadState(p.Spec);p.Minimized=saved.Minimized;p.Expanded=saved.Expanded;var at=saved.Position;Place(p.Root,new Rect(at.X,at.Y,p.Spec.width,p.Expanded?401:p.Spec.height));}
            worldMessages.Clear();ResetWorldChat();ClearCommanderDrag();if(personalGhost!=null)Destroy(personalGhost.gameObject);personalGhost=null;
            ForgetClassWindow();ResetBattleDialogs();showingActions=false;trainingMode=null;trainingActionUnit=0;
        }
        private void ReceiveWhisperReception(bool allowed)
        {
            OriginalClientPreferences.Current.Whisper=allowed;
            if(utilities.TryGetValue(27,out var p)){p.Settings=OriginalClientPreferences.Current.PanelStates();PaintUtilitySettings(p);}
        }
        private UtilityWindow CreateUtility(int id)
        {
            var list=art.UtilityLayout?.windows;int index=list==null?-1:Array.FindIndex(list,w=>w.id==id);
            if(index<0||art.utilityWindows?.ElementAtOrDefault(index)==null)return null;
            var spec=list[index];var placement=OriginalWindowPreferences.Load(spec);var p=new UtilityWindow{Spec=spec,Root=Group(worldWindows,"Native world scene"+spec.scene,new Rect(placement.X,placement.Y,spec.width,spec.height))};
            p.Root.gameObject.AddComponent<Image>().color=Color.clear;Picture(p.Root,"Original wlib scene"+spec.scene,new Rect(0,0,spec.width,spec.height),art.utilityWindows[index]);
            if(spec.dragWidth>0&&spec.dragHeight>0)
            {
            var drag=Hit(p.Root,"Original window title",new Rect(spec.dragX,spec.dragY,spec.dragWidth,spec.dragHeight),()=>p.Root.SetAsLastSibling());
            drag.gameObject.AddComponent<NativeWindowDragInput>().Configure(screen,p.Root,new Rect(spec.dragX,spec.dragY,spec.dragWidth,spec.dragHeight),()=>NativeWindowDragAllowed);
            }
            for(int i=0;i<spec.buttons.Length;i++)
            {
                int control=i;var b=spec.buttons[i];var hit=Hit(p.Root,"Original control"+i,new Rect(b.x,b.y,b.width,b.height),()=>{if(id!=33||control>1)UtilityControl(id,control);});
                if(id==33&&control<2)hit.gameObject.AddComponent<NativeRepeatButton>().Configure(()=>UtilityControl(id,control));
                var image=Picture((RectTransform)hit.transform,"Original states",new Rect(0,0,b.width,b.height),art.battle.waitingButtons);
                hit.gameObject.AddComponent<NativeEntryButton>().Configure(image,art.battle.waitingButtons,new Rect(b.sx,b.sy,b.width,b.height),new Rect(b.sx+b.width,b.sy,b.width,b.height),new Rect(b.sx+2*b.width,b.sy,b.width,b.height));
                p.Controls.Add((hit,image,b));
            }
            utilities[id]=p;
            if(id==27)
            {if(server.WhisperReception.HasValue)OriginalClientPreferences.Current.Whisper=server.WhisperReception.Value;p.Settings=OriginalClientPreferences.Current.PanelStates();p.FontName=Label(p.Root,"Original font name",new Rect(55,165,42,15),"");PaintUtilitySettings(p);}
            if(id==81)
            {
                var bounds=Group(p.Root,"Original memo row bounds",new Rect(0,23,590,230));bounds.gameObject.AddComponent<RectMask2D>();
                for(int row=0;row<10;row++)
                {
                    int y=4+row*23;var sender=Label(bounds,"Sender"+row,new Rect(4,y,75,16),"");sender.color=Color.white;
                    var date=Label(bounds,"Sent date"+row,new Rect(90,y,75,16),"");date.color=Color.white;
                    var body=Label(bounds,"Memo body"+row,new Rect(175,y,420,16),"");body.color=Color.white;
                    ((NativeBitmapText)sender).NativeParagraphWrap=((NativeBitmapText)date).NativeParagraphWrap=((NativeBitmapText)body).NativeParagraphWrap=true;
                    p.Memos.Add((sender,date,body));
                }
                bounds.SetSiblingIndex(1);server.Request_ViewMemos();PaintMemos(p);
            }
            if(id==33)
            {
                var mask=Group(p.Root,"Original help text rectangle",new Rect(37,53,535,378));mask.gameObject.AddComponent<RectMask2D>();
                string text=(art.battle.help?.text??"").Replace("\r\n","\n");if(text.EndsWith("\n"))text=text.Substring(0,text.Length-1);var font=Resources.Load<NativeBitmapFont>("OriginalBitmapFont");
                int height=font?.Ready==true?NativeTextMetrics.ParagraphHeight(text,font.Mapping,535,true):378;p.HelpLines=Math.Max(27,height/14);p.HelpMaximum=Math.Max(0,p.HelpLines-27);
                p.HelpContent=Group(mask,"Original help lines",new Rect(0,0,535,Math.Max(378,height)));
                var label=Label(p.HelpContent,"Original set.arc/help.txt",new Rect(0,0,535,Math.Max(378,height)),text);label.verticalOverflow=VerticalWrapMode.Overflow;
                p.HelpThumb=Group(p.Root,"Original help scrollbar thumb",new Rect(591,74,18,343));var thumb=p.HelpThumb.gameObject.AddComponent<Image>();thumb.raycastTarget=false;thumb.color=new Color32(80,32,0,255);PaintHelpScroll(p);
            }
            return p;
        }
        private void PaintUtilities()
        {foreach(var p in utilities.Values){bool visible=!BattleMap()&&(view.presentation==null||view.presentation.completed);p.Root.gameObject.SetActive(visible);if(visible&&p.Spec.id==81)PaintMemos(p);}}
        private void ToggleUtility(int id)
        {
            if(!PersonalAvailable||BattleMap())return;
            if(utilities.ContainsKey(id)){CloseUtility(id);return;}
            if(CreateUtility(id)==null)ShowFailure(WorldFailure.RulesUnavailable);
        }
        private void CloseUtility(int id)
        {if(utilities.TryGetValue(id,out var p)){SaveUtilityPosition(p);utilities.Remove(id);p.Root.gameObject.SetActive(false);Destroy(p.Root.gameObject);}}
        private void SaveUtilityPosition(UtilityWindow p)
        {if(p?.Root!=null)OriginalWindowPreferences.Save(p.Spec,Mathf.RoundToInt(p.Root.anchoredPosition.x),Mathf.RoundToInt(-p.Root.anchoredPosition.y));}
        private void SaveUtilityPositions(){foreach(var p in utilities.Values)SaveUtilityPosition(p);}
        private void UtilityControl(int id,int control)
        {
            if(!utilities.TryGetValue(id,out var p)||!PersonalAvailable)return;p.Root.SetAsLastSibling();
            if(id==27)
            {
                if(control==0){CloseUtility(id);return;}var settings=OriginalClientPreferences.Current;
                if(control==7){settings.CycleFont(NativeBitmapText.SetVariant);OriginalClientPreferences.Save();PaintUtilitySettings(p);return;}
                bool value=control==6||!p.Settings[control];if(!settings.Toggle(control,value))return;p.Settings[control]=value;
                if(control==1)SetOriginalMusicEnabled(settings.Music);
                if(control==4)server.Request_WhisperReception(settings.Whisper);
                OriginalClientPreferences.Save();PaintUtilitySettings(p);return;
            }
            if(id==81)
            {
                if(control==2){CloseUtility(id);return;}
                if(control<2){server.Request_MemoPage(control);PaintMemos(p);return;}
                int row=control-3,index=p.MemoPage*10+row;
                if(row>=0&&row<10&&p.MemoRows!=null&&index<p.MemoRows.Length)server.Request_DeleteMemo(row,p.MemoRevision,p.MemoRows[index].id,p.MemoSession,p.MemoPage);
                PaintMemos(p);return;
            }
            if(id==33)
            {
                if(control==2){CloseUtility(id);return;}
                p.HelpOffset=Mathf.Clamp(p.HelpOffset+(control==0?-1:1),0,p.HelpMaximum);
                PaintHelpScroll(p);
            }
        }
        private void PaintMemos(UtilityWindow p)
        {
            if(p.MemoRows!=null&&!server.MemoViewChanged(p.MemoSession,p.MemoRevision,p.MemoPage))return;
            var state=server.MemoState;if(state==null)return;var rows=state.rows;
            p.MemoRows=rows;p.MemoRevision=state.revision;p.MemoSession=state.session;p.MemoPage=state.page;int start=state.page*10;
            for(int row=0;row<10;row++)
            {
                var memo=rows.ElementAtOrDefault(start+row);var labels=p.Memos[row];
                labels.sender.text=memo?.sender??"";labels.date.text=memo?.date??"";labels.body.text=memo?.body??"";
            }
        }
        private void PaintHelpScroll(UtilityWindow p)
        {
            p.HelpContent.anchoredPosition=new Vector2(0,p.HelpOffset*14);
            int height=(27*343+p.HelpLines/2)/p.HelpLines;
            int top=p.HelpMaximum==0?0:(343-height)*p.HelpOffset/p.HelpMaximum;
            Place(p.HelpThumb,new Rect(591,74+top,18,height));
        }
        private void PaintUtilitySettings(UtilityWindow p)
        {
            for(int index=1;index<=6;index++)
            {
                var c=p.Controls[index];var b=c.spec;var normal=new Rect(b.sx+(p.Settings[index]?b.width:0),b.sy,b.width,b.height);var hover=new Rect(b.sx+b.width,b.sy,b.width,b.height);
                c.hit.GetComponent<NativeEntryButton>().Configure(c.image,art.battle.waitingButtons,normal,hover,hover);
            }
            p.FontName.text=OriginalClientPreferences.Current.Font==0?"굴림":"솔체";
        }
    }
}
