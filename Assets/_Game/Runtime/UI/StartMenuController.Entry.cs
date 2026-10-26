using UnityEngine;

namespace DarkSaver.UI
{
    public sealed partial class StartMenuController
    {
        private Texture2D entryBackground,entryPanel,entryConnect,entryJoin,entryExit;
        private Camera entryCamera;
        private void EnsureEntryCamera()
        {
            if(entryCamera!=null)return;
            // Screen-space overlay canvases do not require a camera, but Game View reports
            // "No cameras rendering" without an active display camera underneath them.
            foreach(var camera in Camera.allCameras)
                if(camera.isActiveAndEnabled && camera.targetTexture==null && camera.targetDisplay==0)return;
            var background=new GameObject("Entry Background Camera",typeof(Camera));
            background.transform.SetParent(transform,false);
            background.transform.localPosition=new Vector3(0,0,-10);
            entryCamera=background.GetComponent<Camera>();
            entryCamera.clearFlags=CameraClearFlags.SolidColor;
            entryCamera.backgroundColor=Color.black;
            entryCamera.cullingMask=0;
            entryCamera.orthographic=true;
            entryCamera.depth=-100;
            entryCamera.allowHDR=false;
            entryCamera.allowMSAA=false;
            // Owned by Start Menu: Single-mode world entry unloads it with the menu.
            // It is deliberately untagged so it cannot replace the world's MainCamera.
        }
        private void LoadEntryArtwork()
        {
            if(entryBackground!=null)return;
            entryBackground=Resources.Load<Texture2D>("UI/Entry/Background");
            entryPanel=Resources.Load<Texture2D>("UI/Entry/LoginPanel");
            entryConnect=Resources.Load<Texture2D>("UI/Entry/Connect");
            entryJoin=Resources.Load<Texture2D>("UI/Entry/Join");
            entryExit=Resources.Load<Texture2D>("UI/Entry/Exit");
        }
        private void DrawNativeStart()
        {
            ui.EntryButton("new",new Rect(254,92,51,17),entryConnect,"시작",StartGame,!loading);
            ui.EntryButton("quit",new Rect(349,92,37,17),entryExit,"종료",Quit,!loading);
            DrawEntryMessage(new Rect(110,128,420,52));
        }
        private void DrawLogin(Rect unused)
        {
            // Saver.exe 0x40a517/0x40a51c and0x40b6ee/0x40b701: native origin226,279.
            // Use the BltFast-size native crop; DirectDraw has a slightly larger destination.
            const float x=226,y=279;
            ui.EntryArtwork("entry-login-panel",new Rect(x,y,197,87),entryPanel);
            // Remove unused billing/options controls from the original four-button strip.
            ui.SolidRect("entry-login-action-fill",new Rect(x+5,y+55,187,27),new Color32(247,247,255,255));
            ui.EntryInput("login-id",new Rect(x+74,y+13,108,14),loginUserId,20,v=>loginUserId=v,submit:()=>Login());
            ui.EntryInput("login-password",new Rect(x+74,y+36,108,14),loginPassword,64,v=>loginPassword=v,password:true,submit:()=>Login());
            ui.EntryButton("login",new Rect(x+13,y+60,51,17),entryConnect,"로그인",()=>Login(),!loading);
            ui.EntryButton("open-register",new Rect(x+68,y+60,36,17),entryJoin,"회원가입",()=>OpenRegistration(),!loading);
            ui.EntryButton("login-back",new Rect(x+148,y+60,37,17),entryExit,"시작 화면으로 돌아가기",()=>{loggingIn=false;loginPassword="";},!loading);
            if(loginFailed && loginCanRecover)
                ui.Button("login-recovery",new Rect(226,373,197,26),"캐릭터 선택으로 복구",()=>
                {authenticated=true;RefreshContinue();loggingIn=false;selecting=true;},!loading,fontSize:12);
            DrawEntryMessage(new Rect(160,218,320,52));
        }
        private void DrawEntryMessage(Rect rect)
        {
            if(string.IsNullOrEmpty(loadMessage))return;
            ui.SolidRect("entry-message-background",rect,new Color(0,0,0,.8f));
            ui.Label("entry-message",new Rect(rect.x+8,rect.y+4,rect.width-16,rect.height-8),loadMessage,12,color:Color.white);
        }
        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
    }
}
