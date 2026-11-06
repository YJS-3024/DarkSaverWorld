using DarkSaver.Original.UI;
using UnityEngine;
using UnityEngine.UI;
namespace DarkSaver.Original.World
{
    public sealed partial class WorldSceneRoot
    {
        private InputField bankAmountInput;private NativeBankAmountEdit bankAmountEdit;
        private void OpenBankAmount(bool deposit)
        {
            var facility=view?.facility;if(facility?.kind!=FacilityKind.Bank)return;
            if(!server.BankBalanceReady(facility.token))return;
            if(deposit&&!view.record.goldKnown){ShowFailure(WorldFailure.RulesUnavailable);return;}
            if(!NativeBankLayout.CanOpenAmount(deposit,true,view.record.gold,view.record.bank))
            {
                ShowClientMessage(deposit?"당신은 저축할 돈이 없습니다.":"당신은 저축한 돈이 없습니다.");return;
            }
            CloseNotice();bankAmountEdit=server.Request_EditBankAmount(deposit,facility.token);if(bankAmountEdit==null)return;
            var backdrop=NoticeBlocker();notice=NativeWindow(17,backdrop,_=>ConfirmBankAmount(),35);
            var prompt=Label(notice,"Original bank amount prompt",new Rect(0,16,240,14),deposit?"얼마를 맡기시겠습니까?":"얼마를 찾으시겠습니까?",TextAnchor.UpperCenter);prompt.color=Color.black;
            var field=Group(notice,"Original bank amount edit",new Rect(57,47,59,15));var background=field.gameObject.AddComponent<Image>();background.color=Color.clear;
            var text=Group(field,"Amount text",new Rect(0,0,59,15)).gameObject.AddComponent<Text>();text.font=font;text.fontSize=14;text.color=Color.white;text.alignment=TextAnchor.MiddleLeft;text.supportRichText=false;text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Truncate;
            bankAmountInput=field.gameObject.AddComponent<InputField>();bankAmountInput.targetGraphic=background;bankAmountInput.textComponent=text;bankAmountInput.lineType=InputField.LineType.SingleLine;bankAmountInput.transition=Selectable.Transition.None;bankAmountInput.contentType=InputField.ContentType.Standard;
            bankAmountInput.onEndEdit.AddListener(_=>{if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))ConfirmBankAmount();});
            bankAmountInput.SetTextWithoutNotify("");bankAmountInput.ActivateInputField();
        }
        private void ConfirmBankAmount()
        {
            var edit=bankAmountEdit;if(edit==null||bankAmountInput==null)return;string text=bankAmountInput.text;
            bankAmountEdit=null;bankAmountInput=null;CloseNotice();var result=server.Request_BankAmount(edit,text,RequestId());server.Request_CancelBankAmount(edit);Handle(result);
        }
        private void CancelBankAmount()
        {var edit=bankAmountEdit;bankAmountEdit=null;bankAmountInput=null;if(edit!=null)server?.Request_CancelBankAmount(edit);}
        private bool HandleBankAmountInput()
        {
            if(bankAmountEdit==null)return false;
            if(Input.GetKeyDown(KeyCode.Escape)||Input.GetMouseButtonDown(1))CloseNotice();return true;
        }
        private void PaintBankAmount()
        {
            if(bankAmountEdit!=null&&(view?.facility?.token!=bankAmountEdit.Query.token||view.moving||view.pendingPortal!=null
                ||view.currencySession!=bankAmountEdit.Query.session||(view.record.currency?.revision??0)!=bankAmountEdit.Revision))CloseNotice();
        }
    }
}
