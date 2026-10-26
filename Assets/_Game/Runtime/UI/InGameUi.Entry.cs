using System;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.UI
{
    public sealed partial class InGameUi
    {
        public void EntryArtwork(string key,Rect rect,Texture2D texture,bool background=false)
        {PortraitTexture(key,rect,texture);if(background)elements[key].SetAsFirstSibling();else Raise(key);}
        public void EntryButton(string key,Rect rect,Texture2D texture,string tooltip,Action action,bool enabled=true)
        {Button(key,rect,"",action,enabled,tooltip:tooltip);Graphic(elements[key],texture,Color.white,true);Find<UnityEngine.UI.Button>(key).transition=Selectable.Transition.None;Find<Outline>(key).enabled=false;Raise(key);}
        public void EntryInput(string key,Rect rect,string value,int maximum,Action<string> changed,bool password=false,Action submit=null)
        {
            bool fresh=!elements.ContainsKey(key);TextInput(key,rect,value,maximum,changed,password:password);
            Graphic(elements[key],null,new Color32(189,206,247,255),true);
            var field=Find<InputField>(key);field.textComponent.fontSize=12;field.textComponent.color=new Color32(29,56,112,255);
            actions[key+"-submit"]=submit;
            Events(key).Submit=()=>{if(Active(key) && actions.TryGetValue(key+"-submit",out var callback))callback?.Invoke();};
            if(fresh)field.onEndEdit.AddListener(_=>{if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))Events(key).Submit?.Invoke();});
            Raise(key);
        }
    }
}
