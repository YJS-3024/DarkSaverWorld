using UnityEngine;
namespace DarkSaver.UI
{
    public sealed partial class StartMenuController
    {
        private Texture2D registrationPanel,registrationField;
        private NativeBattleWindows.Control registrationConfirm,registrationCancel;
        private static NativeBattleWindows.Control RegistrationButton(string name,int id)
        {
            var frames=new Texture2D[3];for(int i=0;i<3;i++)frames[i]=Resources.Load<Texture2D>("UI/Entry/Registration"+name+"_"+i);
            return new NativeBattleWindows.Control{nativeId=id,frames=frames,rect=new Rect(0,0,46,19)};
        }
        private void DrawRegistration(Rect unused)
        {
            // Original signup was a web page; this complete local form reuses native paper/button art.
            registrationPanel??=Resources.Load<Texture2D>("UI/Entry/RegistrationPanel");
            registrationField??=Resources.Load<Texture2D>("UI/Entry/RegistrationField");
            registrationConfirm??=RegistrationButton("Confirm",75);registrationCancel??=RegistrationButton("Cancel",53);
            var origin=new Vector2(121,90);var ink=new Color(.12f,.08f,.03f);
            ui.EntryArtwork("register-native",new Rect(origin,new Vector2(397,300)),registrationPanel);
            ui.NativeBitmapText("register-title",new Rect(origin.x+48,origin.y+32,300,20),"회원가입",ink);
            string[] keys={"register-id","register-password","register-confirm"};
            string[] labels={"아이디","비밀번호","비밀번호 확인"};
            string[] values={registrationId,registrationPassword,registrationConfirmation};
            for(int i=0;i<3;i++)
            {
                int row=i;float y=origin.y+78+45*i;
                ui.NativeBitmapText(keys[i]+"-label",new Rect(origin.x+42,y+4,94,16),labels[i],ink);
                var field=new Rect(origin.x+140,y,210,22);
                ui.EntryArtwork(keys[i]+"-field",field,registrationField);
                ui.EntryInput(keys[i],new Rect(field.x+3,field.y+1,field.width-6,field.height-2),values[i],i==0?20:64,v=>
                {if(row==0)registrationId=v;else if(row==1)registrationPassword=v;else registrationConfirmation=v;},
                    password:i>0,submit:i==2?()=>RegisterAccount(registrationId,registrationPassword,registrationConfirmation):()=>ui.MoveInputFocus(),textColor:ink);
            }
            string message=loadMessage;
            if(!string.IsNullOrEmpty(registrationConfirmation) && registrationPassword!=registrationConfirmation)message="비밀번호와 확인 입력이 다릅니다.";
            if(!string.IsNullOrEmpty(message))ui.NativeBitmapText("register-message",new Rect(origin.x+42,origin.y+211,310,44),message,ink,3);
            bool ready=!loading && !string.IsNullOrWhiteSpace(registrationId) && (registrationPassword?.Length??0)>=8 && registrationPassword==registrationConfirmation;
            ui.NativeImageButton("register-submit",origin+new Vector2(230,265),registrationConfirm,()=>RegisterAccount(registrationId,registrationPassword,registrationConfirmation),ready,"회원가입");
            ui.NativeImageButton("register-cancel",origin+new Vector2(291,265),registrationCancel,CancelRegistration,!loading,"취소");
        }
    }
}
