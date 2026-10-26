using System;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.UI
{
    public sealed partial class InGameUi
    {
        private NativeBattleWindows libraryButtons;
        private sealed class LibraryButtonState
        {
            public Texture2D[] frames;
            public bool hover,pressed;
        }
        private readonly System.Collections.Generic.Dictionary<string,LibraryButtonState> libraryButtonStates=new();
        public void NativeLibraryButton(string key,Vector2 position,int nativeId,Action action,bool enabled,string tooltip)
        {
            libraryButtons??=Resources.Load<NativeBattleWindows>("UI/BattleWindows/NativeBattleWindows");NativeBattleWindows.Control control=null;
            if(libraryButtons!=null)foreach(var window in libraryButtons.windows){foreach(var candidate in window.controls)if(candidate.nativeId==nativeId){control=candidate;break;}if(control!=null)break;}
            if(control==null)throw new InvalidOperationException("Missing native control "+nativeId);NativeImageButton(key,position,control,action,enabled,tooltip);
        }
        public void NativeTexturePanel(string key,Rect rect,Texture2D texture){PortraitTexture(key,rect,texture);Raise(key);}
        public void NativeImageButton(string key,Vector2 position,NativeBattleWindows.Control control,Action action,bool enabled,string tooltip)
        {
            if(control?.frames==null || control.frames.Length!=3)throw new ArgumentException("Native button needs three frames.");
            bool returning=!elements.TryGetValue(key,out var prior)||!prior.gameObject.activeSelf;
            Button(key,new Rect(position,control.rect.size),"",action,enabled,tooltip:tooltip);
            if(!libraryButtonStates.TryGetValue(key,out var state))
            {
                state=new LibraryButtonState();libraryButtonStates.Add(key,state);var captured=state;var events=Events(key);
                void Paint(){if(captured.frames!=null)Graphic(elements[key],captured.frames[captured.pressed?2:captured.hover?1:0],Color.white,true);}
                events.Enter+=()=>{captured.hover=Find<UnityEngine.UI.Button>(key).interactable;Paint();};
                events.Exit+=()=>{captured.hover=captured.pressed=false;Paint();};
                events.Down+=e=>{if(e.button==UnityEngine.EventSystems.PointerEventData.InputButton.Left)captured.pressed=Find<UnityEngine.UI.Button>(key).interactable;Paint();};
                events.Up+=e=>{captured.pressed=false;Paint();};
            }
            if(!enabled || returning || state.frames!=control.frames)state.hover=state.pressed=false;
            Find<UnityEngine.UI.Button>(key).transition=Selectable.Transition.None;Find<Outline>(key).enabled=false;
            state.frames=control.frames;Graphic(elements[key],state.frames[state.pressed?2:state.hover?1:0],Color.white,true);Raise(key);
        }
        public void NativeStatusCloseButton(string key,Rect rect,Action close,bool minimize=false)
        {
            Button(key,rect,"",close,tooltip:minimize?"최소화":"닫기");
            if(!libraryButtonStates.TryGetValue(key,out var state))
            {
                state=new LibraryButtonState();libraryButtonStates.Add(key,state);var captured=state;var events=Events(key);
                void Paint()=>PortraitButtonSprite(key,NativeBattleStatus.CloseButton(NativeBattleStatus.CloseButtonState(captured.hover,captured.pressed),minimize));
                events.Enter+=()=>{captured.hover=true;Paint();};events.Exit+=()=>{captured.hover=captured.pressed=false;Paint();};
                events.Down+=e=>{if(e.button==UnityEngine.EventSystems.PointerEventData.InputButton.Left){captured.pressed=true;Paint();}};events.Up+=e=>{if(e.button==UnityEngine.EventSystems.PointerEventData.InputButton.Left){captured.pressed=false;Paint();}};
            }
            Find<Outline>(key).enabled=false;Find<UnityEngine.UI.Button>(key).transition=Selectable.Transition.None;
            PortraitButtonSprite(key,NativeBattleStatus.CloseButton(NativeBattleStatus.CloseButtonState(state.hover,state.pressed),minimize));
        }
        private void PortraitButtonSprite(string key,Sprite sprite)
        {var image=Graphic(elements[key],sprite.texture,Color.white,true);var r=sprite.textureRect;image.uvRect=new Rect(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height);}
        public void NativeBattleWindowText(string key,Rect rect,string text)
        {
            var content=Scroll(key+"-scroll",rect,rect.height);
            Label(key,new Rect(0,0,rect.width,rect.height),text,14,content,TextAnchor.UpperLeft);
            var label=Find<Text>(key);float height=Mathf.Max(rect.height,label.preferredHeight);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
            label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
            Events(key).Tooltip=text??"";Events(key+"-scroll").Tooltip=text??"";Raise(key+"-scroll");
        }
        public void NativeMinimizedWindow(string key,Rect rect,Texture2D background,Action restore)
        {EntryButton(key,rect,background,"크게 보기",restore);}
        public void NativeBattleWindow(string key,Rect rect,NativeBattleWindows.Window window,Action close,Action minimize=null)
        {
            NativeTexturePanel(key,rect,window.background);
            for(int i=0;i<window.controls.Length;i++){var control=window.controls[i];bool closes=control.nativeId==19||control.nativeId==38||control.nativeId==66;bool minimizes=control.nativeId==37&&minimize!=null;NativeImageButton(key+"-control-"+i,rect.position+control.rect.position,control,closes?close:minimizes?minimize:null,closes||minimizes,closes?"닫기":minimizes?"작게 보기":"내용/기능 연결 대기");}
        }
    }
}
