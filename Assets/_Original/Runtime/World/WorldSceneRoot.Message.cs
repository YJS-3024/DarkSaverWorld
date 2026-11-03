using DarkSaver.Original.Tutorial;
using DarkSaver.Original.UI;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private readonly NativeWorldMessageState clientMessageState=new NativeWorldMessageState();private RectTransform clientMessageRoot;private Texture2D clientMessageTexture;
        private void ShowClientMessage(string text,bool paragraph=false)
        {
            if(clientMessageTexture==null)
            {
                var source=Resources.Load<TextAsset>("OriginalWorldNotice");
                if(!NativeWorldMessageLayout.TryImage(source?.bytes,out var rgba)){Debug.LogError("Original scene28 message artwork is missing.",this);return;}
                clientMessageTexture=new Texture2D(NativeWorldMessageLayout.Width,NativeWorldMessageLayout.Height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                clientMessageTexture.LoadRawTextureData(NativeWorldMessageLayout.TexturePixels(rgba));clientMessageTexture.Apply(false,true);
            }
            if(clientMessageRoot!=null){clientMessageRoot.gameObject.SetActive(false);Destroy(clientMessageRoot.gameObject);}
            clientMessageState.Show(text,paragraph);clientMessageRoot=Group(overlay,"Original message44",new Rect(0,0,640,480));var blocker=clientMessageRoot.gameObject.AddComponent<Image>();blocker.color=Color.clear;
            clientMessageRoot.gameObject.AddComponent<NativeWorldMessageInput>().Configure(DismissClientMessage);
            var spec=new WaitingWindowDefinition{id=NativeWorldMessageLayout.Window,width=NativeWorldMessageLayout.Width,height=NativeWorldMessageLayout.Height};var at=OriginalWindowPreferences.LoadState(spec).Position;
            var frame=Group(clientMessageRoot,"Wlib scene28",new Rect(at.X,at.Y,NativeWorldMessageLayout.Width,NativeWorldMessageLayout.Height));
            Picture(frame,"Original scene28 artwork",new Rect(0,0,NativeWorldMessageLayout.Width,NativeWorldMessageLayout.Height),clientMessageTexture);
            var label=Label(frame,"Original message",paragraph?new Rect(NativeWorldMessageLayout.TextX,NativeWorldMessageLayout.TextY,NativeWorldMessageLayout.TextWidth,NativeWorldMessageLayout.TextHeight):new Rect(0,NativeWorldMessageLayout.LineY,640,14),clientMessageState.Text);label.color=Color.white;
            if(paragraph)((NativeBitmapText)label).NativeParagraphWrap=true;else{label.horizontalOverflow=HorizontalWrapMode.Overflow;AnchorPersonalLine(label,NativeWorldMessageLayout.LineX,NativeWorldMessageLayout.LineY,6);}
        }
        private void HandleNativeRejection(WorldReply reply,string text)
        {if(reply.View!=null)view=reply.View;previousFailure=reply.Failure;ShowClientMessage(text);}
        private void DismissClientMessage()
        {
            if(!clientMessageState.ConsumeLeftDown(Time.frameCount))return;
            if(clientMessageRoot!=null)clientMessageRoot.gameObject.SetActive(false);
        }
        private bool HandleClientMessageInput()=>clientMessageState.ConsumedIn(Time.frameCount)||clientMessageState.Visible&&Input.GetMouseButtonDown(0);
        private void ClearClientMessage()
        {clientMessageState.Clear();if(clientMessageRoot!=null){clientMessageRoot.gameObject.SetActive(false);Destroy(clientMessageRoot.gameObject);}clientMessageRoot=null;}
    }
}
