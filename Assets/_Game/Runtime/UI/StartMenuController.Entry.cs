using UnityEngine;

namespace DarkSaver.UI
{
    public sealed partial class StartMenuController
    {
        private Texture2D entryBackground,entryPanel,entryAtlas;
        private bool entryOptions;
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
            entryAtlas=Resources.Load<Texture2D>("UI/Entry/LoginAtlas");
        }
        private void DrawLogin(Rect unused)
        {
            // Saver.exe 0x40a517/0x40a51c and0x40b6ee/0x40b701: native origin226,279.
            // Use the BltFast-size native crop; DirectDraw has a slightly larger destination.
            const float x=226,y=279;
            ui.EntryArtwork("entry-login-panel",new Rect(x,y,197,87),entryPanel);
            ui.EntryInput("login-id",new Rect(298,296,110,20),loginUserId,20,v=>loginUserId=v,submit:()=>Login());
            ui.EntryInput("login-password",new Rect(298,320,110,18),loginPassword,64,v=>loginPassword=v,password:true,submit:()=>Login());
            // BltFast destinations and native atlas patches from 0x40b630.
            ui.EntryAtlasButton("login",new Rect(239,339,55,19),entryAtlas,new Rect(10,379,55,19),new Rect(77,379,50,16),"접속",()=>Login(),!loading);
            ui.EntryAtlasButton("open-register",new Rect(294,339,38,19),entryAtlas,new Rect(209,55,38,19),new Rect(209,79,38,16),"가입",()=>OpenRegistration(),!loading);
            ui.EntryAtlasButton("login-options",new Rect(334,339,38,19),entryAtlas,new Rect(209,10,38,19),new Rect(209,34,35,16),"옵션",()=>entryOptions=!entryOptions,!loading);
            ui.EntryAtlasButton("quit",new Rect(374,339,38,19),entryAtlas,new Rect(252,54,38,19),new Rect(252,79,35,16),"종료",Quit,!loading);
            if(entryOptions)
            {
                ui.EntryAtlasArtwork("entry-options",new Rect(226,360,197,45),entryAtlas,new Rect(9,100,197,45));
                // Original billing and legacy mode services have no local counterpart.
                // Preserve their artwork without executing retired web/DirectDraw actions.
                ui.EntryAtlasButton("entry-billing",new Rect(240,378,35,20),entryAtlas,new Rect(10,149,35,20),new Rect(10,173,35,18),"과금 서비스는 사용하지 않습니다.",null,false);
                ui.EntryAtlasButton("entry-billing-info",new Rect(284,378,57,20),entryAtlas,new Rect(53,149,57,20),new Rect(53,173,57,18),"과금 서비스는 사용하지 않습니다.",null,false);
                ui.EntryAtlasButton("entry-mode",new Rect(350,378,57,20),entryAtlas,new Rect(119,149,57,20),new Rect(119,173,57,18),"원작 모드 전환 동작은 확인되지 않았습니다.",null,false);
            }
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
