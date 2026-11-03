using DarkSaver.Original.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private InputField commanderNameInput;private PersonalWindow commanderNameWindow;
        private NativeCommanderNameEdit commanderNameEdit;private GameObject commanderNamePreviousFocus,commanderNameLastTextFocus;
        private void BuildCommanderName(PersonalWindow panel)
        {
            commanderNameWindow=panel;
            var hit=Group(panel.Full,"Native F3 name double release",new Rect(144,39,88,16));
            hit.gameObject.AddComponent<Image>().color=Color.clear;
            hit.gameObject.AddComponent<NativeWindowClickInput>().Configure(panel.Clicks,BeginCommanderName);
            var field=Group(panel.Full,"Native F3 name edit",new Rect(144,39,88,16));
            var background=field.gameObject.AddComponent<Image>();background.color=Color.white;
            var text=Group(field,"Name edit text",new Rect(2,0,84,16)).gameObject.AddComponent<Text>();
            text.font=font;text.fontSize=14;text.color=Color.black;text.alignment=TextAnchor.MiddleLeft;text.supportRichText=false;text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Truncate;
            commanderNameInput=field.gameObject.AddComponent<InputField>();commanderNameInput.targetGraphic=background;commanderNameInput.textComponent=text;
            commanderNameInput.transition=Selectable.Transition.None;commanderNameInput.lineType=InputField.LineType.SingleLine;commanderNameInput.contentType=InputField.ContentType.Standard;
            // No guessed12-byte name rule; the original request's exact byte length is preserved.
            commanderNameInput.onEndEdit.AddListener(value=>
            {
                if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))FinishCommanderName(true);
                else FinishCommanderName(false);
            });
            field.gameObject.SetActive(false);
        }
        private void BeginCommanderName()
        {
            if(commanderNameEdit!=null||!PersonalAvailable||BattleMap()||commanderNameWindow==null||!commanderNameWindow.Visible||commanderNameWindow.Minimized)return;
            var edit=server.Request_EditCommanderName();if(edit==null)return;
            commanderNamePreviousFocus=commanderNameLastTextFocus??EventSystem.current?.currentSelectedGameObject;commanderNameEdit=edit;
            commanderNameInput.SetTextWithoutNotify(edit.OriginalName);commanderNameInput.gameObject.SetActive(true);
            commanderNameWindow.Root.SetAsLastSibling();commanderNameInput.transform.SetAsLastSibling();commanderNameInput.ActivateInputField();
        }
        private void FinishCommanderName(bool submit)
        {
            var edit=commanderNameEdit;if(edit==null)return;commanderNameEdit=null;
            if(submit&&PersonalAvailable&&!BattleMap())server.Request_SetCommanderName(edit,commanderNameInput.text);
            server.Request_CancelCommanderName(edit);
            var previous=commanderNamePreviousFocus;commanderNamePreviousFocus=null;
            commanderNameInput.gameObject.SetActive(false);
            if(previous!=null&&previous.activeInHierarchy&&previous!=commanderNameInput.gameObject)EventSystem.current?.SetSelectedGameObject(previous);
        }
        private bool HandleCommanderNameInput()
        {
            if(commanderNameEdit==null)
            {var selected=EventSystem.current?.currentSelectedGameObject;if(selected?.GetComponent<InputField>()?.isFocused==true)commanderNameLastTextFocus=selected;return false;}
            if(Input.GetKeyDown(KeyCode.Escape)||Input.GetMouseButtonDown(1))FinishCommanderName(false);
            return true;
        }
        private void PaintCommanderName()
        {
            if(commanderNameEdit!=null&&(!PersonalAvailable||BattleMap()||commanderNameWindow==null||!commanderNameWindow.Visible
                ||commanderNameWindow.Minimized||!commanderNameEdit.Matches(view)))FinishCommanderName(false);
        }
    }
}
