using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private readonly Dictionary<int,Texture2D> itemIcons=new Dictionary<int,Texture2D>();
        private bool itemReadWarning;
        private Texture2D ItemIcon(int picture)
        {
            if(picture<0||picture>=1000||art.items==null)return null;
            if(itemIcons.TryGetValue(picture,out var icon))return icon;
            if(!art.items.isReadable||art.items.width!=3200||art.items.height!=320)
            {
                if(!itemReadWarning){Debug.LogError("Original item atlas requires readable native3200x320 pixels. Reimport Assets/_Original/Art/World/Items.png.",this);itemReadWarning=true;}
                return null;
            }
            int x=picture%100*32,y=art.items.height-(picture/100+1)*32;
            icon=new Texture2D(32,32,TextureFormat.RGBA32,false){name="Original item picture"+picture,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            icon.SetPixels(art.items.GetPixels(x,y,32,32));icon.Apply(false,true);itemIcons.Add(picture,icon);return icon;
        }
        private void ClearItemIcons(){foreach(var icon in itemIcons.Values)if(icon!=null)Destroy(icon);itemIcons.Clear();}
    }
}
