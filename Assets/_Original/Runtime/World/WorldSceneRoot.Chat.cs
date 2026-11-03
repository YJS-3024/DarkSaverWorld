using System.Collections.Generic;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Tutorial;
using DarkSaver.Original.UI;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private RectTransform chatPanel;private InputField chatInput;private bool chatRefocus;
        private readonly NativeWorldChatComposer chatComposer=new NativeWorldChatComposer();
        private readonly List<(NativeEntryButton button,RawImage image,DarkSaver.Original.Tutorial.WaitingButton spec)> chatModes=new List<(NativeEntryButton,RawImage,DarkSaver.Original.Tutorial.WaitingButton)>();
        private void BuildWorldChat()
        {
            var spec=art.ChatLayout;if(spec==null||art.chatWindow==null)return;
            chatPanel=Group(worldWindows,"Original world chat input17",new Rect(spec.x,spec.y,spec.width,spec.height));
            chatPanel.gameObject.SetActive(false);
            chatPanel.gameObject.AddComponent<Image>().color=Color.clear;Picture(chatPanel,"Original scene1 frame",new Rect(0,0,spec.width,spec.height),art.chatWindow);
            var title=Hit(chatPanel,"Original input title",new Rect(spec.dragX,spec.dragY,spec.dragWidth,spec.dragHeight),()=>{});
            title.gameObject.AddComponent<NativeWindowDragInput>().Configure(screen,chatPanel,new Rect(spec.dragX,spec.dragY,spec.dragWidth,spec.dragHeight),()=>NativeWindowDragAllowed);
            // 470700's source rectangle is18,7..461,26. OS edit/IME/caret rendering is a uGUI adapter.
            var field=Group(chatPanel,"Original shared edit rectangle",new Rect(18,7,443,19));var graphic=field.gameObject.AddComponent<Image>();graphic.color=Color.clear;
            var text=Group(field,"Edit text",new Rect(0,0,443,19)).gameObject.AddComponent<Text>();text.font=font;text.fontSize=14;text.color=Color.white;text.alignment=TextAnchor.MiddleLeft;text.supportRichText=false;text.raycastTarget=false;
            chatInput=field.gameObject.AddComponent<InputField>();chatInput.targetGraphic=graphic;chatInput.textComponent=text;chatInput.transition=Selectable.Transition.None;chatInput.lineType=InputField.LineType.SingleLine;chatInput.contentType=InputField.ContentType.Standard;
            chatInput.onEndEdit.AddListener(value=>{if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))SubmitWorldChat(value);});
            foreach(var b in spec.buttons)
            {
                int control=chatModes.Count;var hit=Hit(chatPanel,"Original chat mode"+control,new Rect(b.x,b.y,b.width,b.height),()=>{if(PersonalAvailable){chatComposer.Toggle(control);PaintChatModes();}});
                var image=Picture((RectTransform)hit.transform,"Original mode states",new Rect(0,0,b.width,b.height),art.battle.waitingButtons);var button=hit.gameObject.AddComponent<NativeEntryButton>();chatModes.Add((button,image,b));
            }
            PaintChatModes();
        }
        private void PaintChatModes()
        {
            for(int i=0;i<chatModes.Count;i++)
            {
                var row=chatModes[i];var b=row.spec;var checkedRect=new Rect(b.sx+b.width,b.sy,b.width,b.height);
                row.button.Configure(row.image,art.battle.waitingButtons,new Rect(b.sx+(chatComposer.Checked(i)?b.width:0),b.sy,b.width,b.height),checkedRect,checkedRect);
            }
        }
        private void SubmitWorldChat(string value)
        {
            if(!PersonalAvailable||BattleMap()||chatInput==null)return;chatRefocus=true;
            if(value!=chatInput.text||!server.Request_WorldText(value,chatComposer.Channel))return;
            chatInput.SetTextWithoutNotify("");
        }
        private void ResetWorldChat()
        {chatComposer.Reset();chatRefocus=true;if(chatInput!=null)chatInput.SetTextWithoutNotify("");PaintChatModes();ClearPrivateWindows();}
        private void PaintWorldChat()
        {
            PaintPrivateChats();if(chatPanel==null)return;bool visible=!BattleMap()&&(view.presentation==null||view.presentation.completed);chatPanel.gameObject.SetActive(visible);
            chatInput.interactable=visible&&PersonalAvailable;
            if(!visible){chatRefocus=false;return;}
            if(chatRefocus&&chatInput.interactable){chatRefocus=false;chatInput.ActivateInputField();}
        }
    }
}
