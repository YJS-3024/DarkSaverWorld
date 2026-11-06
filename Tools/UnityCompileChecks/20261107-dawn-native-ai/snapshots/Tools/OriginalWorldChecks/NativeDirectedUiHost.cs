using System;
using System.Text.Json;
using System.Linq;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.World;
namespace UnityEngine
{
    public static class Time {public static int frameCount;}
    public sealed class CanvasGroup {public bool blocksRaycasts=true,interactable=true;}
    public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}}
    public sealed class GameObject {public bool active;public void SetActive(bool value){active=value;}}
    public sealed class RectTransform
    {
        public readonly GameObject gameObject=new GameObject();public readonly CanvasGroup group=new CanvasGroup();public Rect rect;public int raised;
        public T GetComponent<T>()where T:class=>group as T;public void SetAsLastSibling(){raised++;}
    }
}
namespace UnityEngine.UI {public sealed class Text {public string text;}public sealed class RawImage {public object texture;public bool enabled;}}
namespace DarkSaver.Original.World
{
    // Only the frame clock and transport are boundaries. The production pointer
    // acknowledgement body and cutscene predicate are extracted unchanged.
    public sealed partial class WorldSceneRoot
    {
        public WorldView view;public InputServer server;
        private int nativeMessagePointerFrame=-1;
        private void Handle(WorldReply reply){view=reply.View;}
        public void Click()=>AcknowledgeDirectedPointer();
        public bool Locked=>CampaignCutscene;
        public UnityEngine.RectTransform campaignDialogue=new UnityEngine.RectTransform(),campaignPlainMessage=new UnityEngine.RectTransform(),campaignPortraitMessage=new UnityEngine.RectTransform();
        public UnityEngine.UI.Text campaignPlainText=new UnityEngine.UI.Text();public UnityEngine.UI.RawImage campaignFace=new UnityEngine.UI.RawImage();
        public readonly System.Collections.Generic.List<UnityEngine.UI.Text> campaignMessageLines=Enumerable.Range(0,3).Select(_=>new UnityEngine.UI.Text()).ToList();
        public sealed class Character {public string name="Commander";}public readonly Character character=new Character();
        public sealed class Art {public int face;public object Face(int job){face=job;return null;}}public readonly Art campaign=new Art();
        private static void Place(UnityEngine.RectTransform root,UnityEngine.Rect rect){root.rect=rect;}
        private void AnchorPersonalLine(UnityEngine.UI.Text text,int x,int y,int flags){if(x!=280||y!=23||flags!=6)throw new Exception("native centered message anchor");}
        public void PaintMessage(bool presenting)=>PaintExtractedMessage(presenting);

    }
    public sealed class InputServer
    {
        public CampaignPresentation presentation;public NativeCampaignDialogue shared;public int continues;
        public WorldReply Request_ContinueScenario(){continues++;if(presentation!=null)presentation.Continue();else shared?.Continue();return new WorldReply(WorldFailure.None,new WorldView{presentation=presentation?.Snapshot(),nativeMessage=shared?.Snapshot()});}
    }
}
internal static class NativeDirectedUiEntry
{
    static int checks;static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
    public static void Main()
    {
        var p=new CampaignPresentation("MESSAGE 1 709 0 A a|b|c|d|e|f|g\nEND",100,1,native:new CampaignNativeExecution{subject=0,team=0,localTeam=()=>0});p.Advance(0);
        var root=new WorldSceneRoot{server=new InputServer{presentation=p},view=new WorldView{presentation=p.Snapshot()}};
        Check(!root.Locked,"native mode1 message is not a cutscene lock");
        UnityEngine.Time.frameCount=1;root.Click();root.Click();Check(root.server.continues==1&&root.view.presentation.messageFirstLine==3,"one pointer event routed through nested callbacks advances one page per frame");
        UnityEngine.Time.frameCount=2;root.Click();Check(root.server.continues==2&&root.view.presentation.messageFirstLine==6,"following frame can acknowledge the next source page");
        UnityEngine.Time.frameCount=3;root.Click();Check(root.server.continues==3&&!root.view.presentation.waitingMessage&&!root.view.presentation.completed,"last page closes without executing native END in the same pointer callback");
        UnityEngine.Time.frameCount=4;root.Click();Check(root.server.continues==3,"empty native message does not send another continuation");p.Advance(0);root.view.presentation=p.Snapshot();Check(!root.Locked,"completed native event is unlocked");
        var legacy=new CampaignPresentation("MESSAGE 1 709 0 A held\nEND",100,1);legacy.Advance(0);root.server.presentation=legacy;root.view.presentation=legacy.Snapshot();
        Check(root.Locked,"existing mode2-style presentation retains its global lock");root.Click();Check(root.server.continues==3&&root.view.presentation.waitingMessage,"native pointer helper does not consume an existing cutscene message");
        root.view.presentation=null;Check(!root.Locked,"no presentation does not lock");root.Click();Check(root.server.continues==3,"null presentation is a safe no-op");
        var empty=new CampaignPresentation("MESSAGE 0 0 ME 0 %S\nEND",100,1,native:new CampaignNativeExecution{subject=0,team=0,localTeam=()=>0,message=_=>new CampaignMessage{text=""}});
        empty.Advance(0);empty.Advance(10001);Check(empty.Snapshot().waitingMessage&&empty.Snapshot().message=="","original empty plain frame still waits for a click and does not auto-expire");
        empty.Continue();empty.Advance(0);Check(empty.Snapshot().completed,"empty source message can be dismissed and does not deadlock END");
        root.server.presentation=null;root.server.shared=new NativeCampaignDialogue();root.server.shared.Show("Other님이 승리하셨습니다.",0);
        root.view=new WorldView{nativeMessage=root.server.shared.Snapshot()};UnityEngine.Time.frameCount=5;
        Check(!root.Locked,"standalone foreign-team notice is not a cutscene");root.Click();root.Click();
        Check(root.server.continues==4&&!root.view.nativeMessage.visible,"actual pointer helper closes a shared notice without presentation and deduplicates same-frame callbacks");
        UnityEngine.Time.frameCount=6;root.Click();Check(root.server.continues==4,"closed standalone notice sends no continuation");
        root.view=new WorldView{nativeMessage=new NativeCampaignMessageView{visible=true,text="Other님이 승리하셨습니다.",mode=0}};root.PaintMessage(false);
        Check(root.campaignDialogue.gameObject.active&&root.campaignPlainMessage.gameObject.active&&!root.campaignPortraitMessage.gameObject.active,"actual paint body displays a standalone20 without active presentation");
        var rect=root.campaignDialogue.rect;Check(rect.x==40&&rect.y==130&&rect.width==562&&rect.height==66,"actual plain rounded frame placement");
        Check(!root.campaignDialogue.group.blocksRaycasts&&!root.campaignDialogue.group.interactable&&root.campaignPlainText.text=="Other님이 승리하셨습니다.","native shared notice stays nonmodal and displays actual received text");
        root.view.presentation=new CampaignPresentationView{message="old",messageMode=2,face=709,waitingMessage=true};root.PaintMessage(true);
        Check(root.campaignPlainText.text=="Other님이 승리하셨습니다."&&root.campaignPlainMessage.gameObject.active,"shared notice replaces an older view rather than composing both");
        root.view.nativeMessage=new NativeCampaignMessageView{visible=true,text="a|b|c|d|e",mode=1,face=125,firstLine=3};root.PaintMessage(false);
        Check(root.campaignPortraitMessage.gameObject.active&&!root.campaignPlainMessage.gameObject.active&&root.campaignDialogue.rect.y==50,"shared portrait returns to original top location");
        Check(root.campaignMessageLines.Select(x=>x.text).SequenceEqual(new[]{"d","e",""})&&root.campaign.face==125,"actual paint selects current shared page/portrait, never old view values");
        root.view.nativeMessage=new NativeCampaignMessageView{visible=true,text="%s",mode=0};root.PaintMessage(false);
        Check(root.campaignPlainText.text=="%s","already resolved shared text cannot be formatted again with commander name");
        root.view.nativeMessage.text="";root.PaintMessage(false);Check(root.campaignDialogue.gameObject.active&&root.campaignPlainText.text=="","empty native notice still renders its acknowledgeable frame");
        root.view.nativeMessage.visible=false;root.view.presentation=null;root.PaintMessage(false);Check(!root.campaignDialogue.gameObject.active,"closed global notice does not keep an orphan UI open");
        root.view.presentation=new CampaignPresentationView{message="Hello %s",messageMode=2};root.PaintMessage(true);
        Check(root.campaignDialogue.gameObject.active&&root.campaignDialogue.rect.y==300&&root.campaignPlainText.text=="Hello Commander","legacy presentation retains its own placement/format path");
        Check(root.campaignDialogue.group.blocksRaycasts&&root.campaignDialogue.group.interactable,"legacy cutscene retains input blocker");
        root.PaintMessage(false);Check(!root.campaignDialogue.gameObject.active,"inactive legacy view is not resurrected after native close");
        Console.WriteLine(JsonSerializer.Serialize(new{assertions=checks,unityPlay=false,scope="Extracted production message painting, pointer acknowledgement and cutscene predicate with real CampaignPresentation pages, supplied Unity frame clock and transport. Not actual UGUI raycasts, keyboard or OS clicks."}));
    }
}
