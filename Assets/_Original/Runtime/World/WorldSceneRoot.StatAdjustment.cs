using UnityEngine;
using UnityEngine.UI;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private readonly Text[] statTexts=new Text[6];private readonly RectTransform[] statBars=new RectTransform[6];
        private Text statPoints;
        private void QueryAdjustmentData()
        {
            if(view.classOptionsRequest!=null||view.classChangeRequest!=null||view.statAdjustmentRequest!=null||view.magicLoadoutRequest!=null)return;
            if(server.ClassOptionsAuthority!=null&&view.classOptions==null&&view.statAdjustment?.Dirty!=true&&view.magicLoadout?.Dirty!=true)Handle(server.Request_ClassOptions());
            if(server.StatAdjustmentAuthority!=null&&view.statAdjustment==null)Handle(server.Request_StatAdjustmentState());
            if(server.MagicLoadoutAuthority!=null&&view.magicLoadout==null)Handle(server.Request_MagicLoadoutState());
        }
        private void BuildStatEditor()
        {
            var colors=new[]{new Color32(0,0,255,255),new Color32(255,0,0,255),new Color32(255,128,64,255),new Color32(0,255,255,255),new Color32(0,255,0,255),new Color32(255,255,0,255)};
            for(int i=0;i<6;i++)
            {
                statBars[i]=Group(classFull,"Received attribute bar"+i,new Rect(285,226+i*22,0,10));var image=statBars[i].gameObject.AddComponent<Image>();image.color=colors[i];image.raycastTarget=false;
                statTexts[i]=ClassText("Attribute triplet"+i,new Rect(285,226+i*22,176,16),"—");statTexts[i].fontSize=10;statTexts[i].alignment=TextAnchor.MiddleCenter;
            }
            statPoints=ClassText("Received allocation points",new Rect(484,365,40,16),"—");
        }
        private void PaintStatEditor()
        {
            var draft=view.statAdjustment;
            for(int i=0;i<6;i++)
            {
                var value=draft?.values[i];statTexts[i].text=value==null?"—":value.before+"/"+value.after+"/"+value.limit;
                int width=value!=null&&value.before>=0&&value.limit>0?(int)System.Math.Min(176,((long)value.before*176+value.limit/2)/value.limit):0;
                Place(statBars[i],new Rect(285,226+i*22,width,10));
            }
            statPoints.text=draft?.points.ToString()??"—";
        }
    }
}
