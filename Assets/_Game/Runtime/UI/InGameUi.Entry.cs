using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DarkSaver.UI
{
    public sealed partial class InGameUi
    {
        public void EntryArtwork(string key,Rect rect,Texture2D texture,bool background=false)
        {
            PortraitTexture(key,rect,texture);
            if(background)elements[key].SendToBack();else elements[key].BringToFront();
        }
        // Reuse the retained callback/owner guards while replacing the paper theme entirely.
        public void EntryButton(string key,Rect rect,Texture2D texture,string tooltip,Action action,bool enabled=true)
        {
            Button(key,rect,"",action,enabled,tooltip:tooltip);
            var button=elements[key];
            button.style.backgroundImage=texture==null?StyleKeyword.None:new StyleBackground(texture);
            button.style.backgroundColor=Color.clear;
            button.style.borderTopWidth=button.style.borderBottomWidth=button.style.borderLeftWidth=button.style.borderRightWidth=0;
            button.style.paddingLeft=button.style.paddingRight=button.style.paddingTop=button.style.paddingBottom=0;
            button.BringToFront();
        }
        public void EntryInput(string key,Rect rect,string value,int maximum,Action<string> changed,bool password=false,Action submit=null)
        {
            bool isNew=!elements.ContainsKey(key);
            TextInput(key,rect,value,maximum,changed,password:password);
            var field=(TextField)elements[key];
            field.style.fontSize=12;
            field.style.marginLeft=field.style.marginRight=field.style.marginTop=field.style.marginBottom=0;
            field.style.paddingLeft=field.style.paddingRight=field.style.paddingTop=field.style.paddingBottom=0;
            var input=field.Q<VisualElement>(className:TextField.inputUssClassName);
            if(input!=null)
            {
                input.style.backgroundImage=StyleKeyword.None;
                input.style.backgroundColor= (Color)new Color32(189,206,247,255);
                input.style.color= (Color)new Color32(29,56,112,255);input.style.fontSize=12;
                input.style.borderTopWidth=input.style.borderBottomWidth=input.style.borderLeftWidth=input.style.borderRightWidth=0;
                input.style.paddingLeft=input.style.paddingRight=2;
                input.style.paddingTop=input.style.paddingBottom=0;
                input.style.marginLeft=input.style.marginRight=input.style.marginTop=input.style.marginBottom=0;
            }
            actions[key+"-submit"]=submit;
            if(isNew)field.RegisterCallback<KeyDownEvent>(e=>
            {
                if(e.keyCode!=KeyCode.Return && e.keyCode!=KeyCode.KeypadEnter)return;
                if(owner!=null && owner.isActiveAndEnabled && actions.TryGetValue(key+"-submit",out var callback))callback?.Invoke();
                e.StopPropagation();
            });
            field.BringToFront();
        }
    }
}
