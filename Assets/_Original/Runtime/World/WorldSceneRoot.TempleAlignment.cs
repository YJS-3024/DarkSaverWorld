using DarkSaver.Original.Tutorial;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private string alignmentRequestedToken,alignmentShownQuote;
        private Texture2D alignmentArtwork;private WorldWindowDefinition alignmentLayout;
        private void PaintTempleAlignment()
        {
            var f=view.facility;
            if(alignmentRequestedToken!=f.token)
            {
                ClearPanel();alignmentRequestedToken=f.token;alignmentShownQuote=null;
                server.Request_TempleAlignment(f.token);
            }
            var offer=server.TempleAlignment(f.token);
            if(offer==null)
            {
                if(panel!=null)ClearPanel();
                return;
            }
            if(panel!=null&&alignmentShownQuote==offer.id)return;
            ClearPanel();
            if(alignmentArtwork==null)alignmentArtwork=Resources.Load<Texture2D>("OriginalTempleAlignment");
            if(alignmentLayout==null)
            {var raw=Resources.Load<TextAsset>("OriginalTempleAlignmentLayout");if(raw!=null)alignmentLayout=JsonUtility.FromJson<WorldWindowDefinition>(raw.text);}
            if(alignmentArtwork==null||alignmentLayout==null){Debug.LogError("Original alignment window63 artwork is missing.",this);return;}
            panelToken=f.token;panelDefinition=alignmentLayout;alignmentShownQuote=offer.id;
            panel=Group(worldWindows,"Original alignment63",new Rect(56,202,alignmentLayout.width,alignmentLayout.height));
            panel.gameObject.AddComponent<Image>().color=Color.clear;
            Picture(panel,"Original scene29",new Rect(0,0,alignmentLayout.width,alignmentLayout.height),alignmentArtwork);
            var label=Label(panel,"Original alignment question",new Rect(8,offer.fee==0?19:12,512,28),offer.Text,TextAnchor.UpperCenter);label.color=Color.black;label.fontSize=14;
            for(int index=0;index<alignmentLayout.buttons.Length;index++)
            {
                int choice=index;var b=alignmentLayout.buttons[index];string token=f.token,id=offer.id;
                var hit=Hit(panel,"Original alignment control"+index,new Rect(b.x,b.y,b.width,b.height),()=>
                {
                    if(server.WorldSnapshot?.facility?.token!=token)return;
                    if(choice==0&&!server.Request_ChangeTempleAlignment(token,id))return;
                    server.Request_CancelTempleAlignment(token);
                    if(server.WorldSnapshot?.facility?.token==token)Handle(server.Request_CloseFacility());
                });
                var image=Picture((RectTransform)hit.transform,"Original button",new Rect(0,0,b.width,b.height),art.buttons);
                hit.gameObject.AddComponent<DarkSaver.Original.Entry.NativeEntryButton>().Configure(image,art.buttons,new Rect(b.sx,b.sy,b.width,b.height),new Rect(b.sx+b.width,b.sy,b.width,b.height),new Rect(b.sx+2*b.width,b.sy,b.width,b.height));
            }
        }
    }
}
