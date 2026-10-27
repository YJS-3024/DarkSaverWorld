using System;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.UI
{
    public sealed partial class InGameUi
    {
        private Font entryFont;
        private sealed class EntryButtonState { public bool hover,pressed; public RawImage image; }
        private readonly System.Collections.Generic.Dictionary<string,EntryButtonState> entryButtonStates=new();
        public void EntryAtlasArtwork(string key,Rect rect,Texture2D atlas,Rect source)
        {
            EntryArtwork(key,rect,atlas);
            var image=Find<RawImage>(key);
            image.uvRect=new Rect(source.x/atlas.width,1-source.yMax/atlas.height,source.width/atlas.width,source.height/atlas.height);
        }
        // The panel already contains idle buttons. Only hover/press patches are overlaid.
        public void EntryAtlasButton(string key,Rect rect,Texture2D atlas,Rect hover,Rect pressed,string tooltip,Action action,bool enabled=true)
        {
            Button(key,rect,"",action,enabled,tooltip:tooltip);
            if(!entryButtonStates.TryGetValue(key,out var state))
            {
                state=new EntryButtonState();entryButtonStates.Add(key,state);var captured=state;
                state.image=Graphic(NewRect("NativeState",elements[key],new Rect(0,0,rect.width,rect.height)),atlas,Color.clear);
                var events=Events(key);
                void Paint()
                {
                    var image=captured.image;var source=captured.pressed?pressed:hover;
                    image.color=Find<Button>(key).interactable && captured.hover?Color.white:Color.clear;
                    Place(image.rectTransform,new Rect(0,0,source.width,source.height));
                    image.uvRect=new Rect(source.x/atlas.width,1-source.yMax/atlas.height,source.width/atlas.width,source.height/atlas.height);
                }
                events.Enter+=()=>{captured.hover=true;Paint();};
                events.Exit+=()=>{captured.hover=captured.pressed=false;Paint();};
                events.Down+=e=>{if(e.button==UnityEngine.EventSystems.PointerEventData.InputButton.Left){captured.pressed=true;Paint();}};
                events.Up+=e=>{if(e.button==UnityEngine.EventSystems.PointerEventData.InputButton.Left){captured.pressed=false;Paint();}};
            }
            if(!enabled)state.hover=state.pressed=false;
            Graphic(elements[key],null,Color.clear,true);
            var raw=state.image;raw.color=enabled && state.hover?Color.white:Color.clear;
            var frame=state.pressed?pressed:hover;
            Place(raw.rectTransform,new Rect(0,0,frame.width,frame.height));
            raw.uvRect=new Rect(frame.x/atlas.width,1-frame.yMax/atlas.height,frame.width/atlas.width,frame.height/atlas.height);
            Find<Button>(key).transition=Selectable.Transition.None;Find<Outline>(key).enabled=false;Raise(key);
        }
        public void EntryArtwork(string key,Rect rect,Texture2D texture,bool background=false)
        {PortraitTexture(key,rect,texture);if(background)elements[key].SetAsFirstSibling();else Raise(key);}
        public void TransparentButton(string key,Rect rect,Action action)
        {EntryButton(key,rect,null,"",action);Graphic(elements[key],null,Color.clear,true);}
        public void EntryButton(string key,Rect rect,Texture2D texture,string tooltip,Action action,bool enabled=true)
        {Button(key,rect,"",action,enabled,tooltip:tooltip);Graphic(elements[key],texture,Color.white,true);Find<UnityEngine.UI.Button>(key).transition=Selectable.Transition.None;Find<Outline>(key).enabled=false;Raise(key);}
        public void EntryInput(string key,Rect rect,string value,int maximum,Action<string> changed,bool password=false,Action submit=null,Vector2? nativeTextOrigin=null,Color? textColor=null)
        {
            bool fresh=!elements.ContainsKey(key);TextInput(key,rect,value,maximum,changed,password:password);
            Graphic(elements[key],null,Color.clear,true);
            if(entryFont==null)entryFont=Font.CreateDynamicFontFromOSFont(new[]{"굴림","Gulim","Malgun Gothic","Arial"},12);
            var field=Find<InputField>(key);field.textComponent.font=entryFont;
            field.textComponent.fontSize=12;field.textComponent.fontStyle=FontStyle.Bold;field.textComponent.color=textColor??Color.white;
            field.textComponent.alignment=TextAnchor.MiddleLeft;
            if(nativeTextOrigin.HasValue)
            {
                var origin=nativeTextOrigin.Value;
                Place(field.textComponent.rectTransform,new Rect(origin.x-rect.x-rect.width*.5f,origin.y-rect.y,rect.width,20));
                field.textComponent.alignment=TextAnchor.UpperCenter;
                // Native hit bounds and glyph origin are independent; include the two-pixel text overhang.
                var mask=field.GetComponent<RectMask2D>();if(mask!=null)mask.padding=new Vector4(0,0,0,-2);
            }
            field.customCaretColor=true;field.caretColor=textColor??Color.white;
            actions[key+"-submit"]=submit;
            Events(key).Submit=()=>{if(Active(key) && actions.TryGetValue(key+"-submit",out var callback))callback?.Invoke();};
            if(fresh)field.onEndEdit.AddListener(_=>{if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))Events(key).Submit?.Invoke();});
            Raise(key);
        }
    }
}
