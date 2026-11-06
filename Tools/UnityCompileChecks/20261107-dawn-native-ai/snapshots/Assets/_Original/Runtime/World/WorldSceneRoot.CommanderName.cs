using DarkSaver.Original.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private long commanderNameGeneration;
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
            var fieldInput=commanderNameInput;
            fieldInput.onEndEdit.AddListener(value=>
            {
                if(!ReferenceEquals(fieldInput,commanderNameInput))return;
                if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))FinishCommanderName(true);
                else FinishCommanderName(false);
            });
            field.gameObject.SetActive(false);
        }
        private void BeginCommanderName()
        {
            if(!isActiveAndEnabled||server==null||commanderNameInput==null||commanderNameEdit!=null||!PersonalAvailable||BattleMap()||commanderNameWindow==null
                ||commanderNameWindow.Root==null||!commanderNameWindow.Root.gameObject.activeInHierarchy||!commanderNameWindow.Visible||commanderNameWindow.Minimized)return;
            var edit=server.Request_EditCommanderName();if(edit==null)return;
            var events=EventSystem.current;
            var previous=commanderNameLastTextFocus;
            if(previous==null||!previous.activeInHierarchy)previous=events!=null?events.currentSelectedGameObject:null;
            commanderNamePreviousFocus=previous!=null&&previous.activeInHierarchy?previous:null;commanderNameEdit=edit;commanderNameGeneration++;
            commanderNameInput.SetTextWithoutNotify(edit.OriginalName);commanderNameInput.gameObject.SetActive(true);
            commanderNameWindow.Root.SetAsLastSibling();commanderNameInput.transform.SetAsLastSibling();commanderNameInput.ActivateInputField();
        }
        private void FinishCommanderName(bool submit)
        {
            var edit=commanderNameEdit;if(edit==null)return;
            long generation=commanderNameGeneration;var input=commanderNameInput;var previous=commanderNamePreviousFocus;
            commanderNameEdit=null;commanderNamePreviousFocus=null;
            if(submit&&input!=null&&PersonalAvailable&&!BattleMap()&&server!=null)server.Request_SetCommanderName(edit,input.text);
            if(server!=null)server.Request_CancelCommanderName(edit);
            // A synchronous request callback may destroy the old field or start a new edit.
            if(generation!=commanderNameGeneration||commanderNameEdit!=null||!ReferenceEquals(input,commanderNameInput))return;
            if(input!=null)input.gameObject.SetActive(false);
            if(generation!=commanderNameGeneration||commanderNameEdit!=null)return;
            var current=server!=null?server.WorldSnapshot:null;var events=EventSystem.current;
            if(isActiveAndEnabled&&PersonalAvailable&&current!=null&&current.characterId==edit.Character&&current.commanderSession==edit.Session
                &&events!=null&&previous!=null&&previous.activeInHierarchy&&(input==null||previous!=input.gameObject))events.SetSelectedGameObject(previous);
        }
        private bool HandleCommanderNameInput()
        {
            if(commanderNameEdit==null)
            {
                // C# ?. and ?? do not recognize Unity's destroyed-object null state.
                if(commanderNameLastTextFocus==null||!commanderNameLastTextFocus.activeInHierarchy)commanderNameLastTextFocus=null;
                var events=EventSystem.current;var selected=events!=null?events.currentSelectedGameObject:null;
                if(selected!=null&&selected.activeInHierarchy)
                {var input=selected.GetComponent<InputField>();if(input!=null&&input.isFocused)commanderNameLastTextFocus=selected;}
                return false;
            }
            if(commanderNameInput==null||!commanderNameInput.gameObject.activeInHierarchy||commanderNameWindow==null||commanderNameWindow.Root==null)
            {FinishCommanderName(false);return false;}
            if(Input.GetKeyDown(KeyCode.Escape)||Input.GetMouseButtonDown(1))FinishCommanderName(false);
            return true;
        }
        private void PaintCommanderName()
        {
            if(commanderNameEdit!=null&&(commanderNameInput==null||!PersonalAvailable||BattleMap()||commanderNameWindow==null||commanderNameWindow.Root==null||!commanderNameWindow.Visible
                ||commanderNameWindow.Minimized||!commanderNameEdit.Matches(view)))FinishCommanderName(false);
        }
    }
}
