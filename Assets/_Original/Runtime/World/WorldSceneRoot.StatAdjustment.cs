using DarkSaver.Original.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private readonly Text[] statTexts=new Text[6];private readonly RectTransform[] statBars=new RectTransform[6],statChangedBars=new RectTransform[6];
        private Text statPoints;
        private void QueryAdjustmentData()
        {
            if(view.classOptionsRequest!=null||view.classChangeRequest!=null||view.statAdjustmentRequest!=null||view.magicLoadoutRequest!=null)return;
            if(server.ClassOptionsAuthority!=null&&view.classOptions==null&&view.statAdjustment?.Dirty!=true&&view.magicLoadout?.Dirty!=true)Handle(server.Request_ClassOptions());
            if(server.StatAdjustmentAuthority!=null&&view.statAdjustment==null)Handle(server.Request_StatAdjustmentState());
        }
        private void BuildStatEditor()
        {
            var colors=new[]{new Color32(0,0,255,255),new Color32(255,0,0,255),new Color32(255,128,64,255),new Color32(0,255,255,255),new Color32(0,255,0,255),new Color32(255,255,0,255)};
            for(int i=0;i<6;i++)
            {
                statBars[i]=Group(classFull,"Received attribute bar"+i,new Rect(285,226+i*22,0,10));var image=statBars[i].gameObject.AddComponent<Image>();image.color=colors[i];image.raycastTarget=false;
                statChangedBars[i]=Group(classFull,"Native white changed portion"+i,new Rect(285,226+i*22,0,10));var changed=statChangedBars[i].gameObject.AddComponent<Image>();changed.color=Color.white;changed.raycastTarget=false;
                // The source uses an8-point Gulim GDI font here, rather than its14px MXF renderer.
                //11 logical pixels is the96-DPI Unity adapter; OS raster/DPI equivalence needs Play.
                var bounds=Group(classFull,"Native GDI attribute clip"+i,new Rect(285,226+i*22,176,10));bounds.gameObject.AddComponent<RectMask2D>();
                statTexts[i]=Group(bounds,"Original attribute text",new Rect(0,0,176,10)).gameObject.AddComponent<Text>();statTexts[i].font=font;statTexts[i].fontSize=11;statTexts[i].text="—";statTexts[i].supportRichText=false;statTexts[i].color=Color.black;statTexts[i].raycastTarget=false;statTexts[i].horizontalOverflow=HorizontalWrapMode.Overflow;statTexts[i].verticalOverflow=VerticalWrapMode.Overflow;statTexts[i].alignment=TextAnchor.UpperCenter;
            }
            statPoints=ClassText("Received allocation points",new Rect(484,365,40,16),"—");
        }
        private void PaintStatEditor()
        {
            var draft=view.statAdjustment;
            for(int i=0;i<6;i++)
            {
                var value=draft?.values[i];statTexts[i].text=NativeClassPresentation.StatText(value);
                var bar=NativeClassPresentation.StatBar(draft?.initial[i],value);
                Place(statBars[i],new Rect(285,226+i*22,System.Math.Max(0,bar.Filled),10));
                Place(statChangedBars[i],new Rect(285+bar.Baseline,226+i*22,System.Math.Max(0,bar.Filled-bar.Baseline),10));
            }
            statPoints.text=draft?.points.ToString()??"—";
        }
    }
}
