using DarkSaver.Core;
using UnityEngine;

namespace DarkSaver.UI
{
    public sealed partial class StartMenuController
    {
        private int selectedStartingJob=NativeCharacterProfile.SupportedStartingJob;
        private Texture2D creationBackground;
        public int SelectedStartingJob=>selectedStartingJob;
        public bool SelectStartingJob(int job)
        {
            if(!creating || loading || !NativeCharacterProfile.IsStartingJob(job))return false;
            selectedStartingJob=job;return true;
        }
        private void DrawCharacterCreation(Rect unused)
        {
            if(creationBackground==null)creationBackground=Resources.Load<Texture2D>("UI/Entry/CharacterCreation");
            var origin=new Vector2((Rendering.GameScreen.Width-397)*.5f,64);
            ui.EntryArtwork("creation-native",new Rect(origin,new Vector2(397,300)),creationBackground);
            if(portraits==null)portraits=Resources.Load<CharacterPortraitCatalog>("UI/CharacterPortraits/CharacterPortraits");
            int[] choices={100,101,103};
            for(int i=0;i<choices.Length;i++)
            {
                int job=choices[i];float left=origin.x+25+i*116;
                if(portraits!=null && portraits.TryGet(job,out var face)){ui.PortraitTexture("creation-face-"+job,new Rect(left+8,origin.y+62,64,64),face);ui.Raise("creation-face-"+job);}
                ui.NativeRosterSelection("creation-job-"+job,new Rect(left,origin.y+53,88,179),()=>SelectStartingJob(job),!loading,NativeCharacterProfile.StartingJobName(job));
                ui.Raise("creation-job-"+job);
                if(selectedStartingJob==job)ui.SolidRect("creation-selected",new Rect(left+3,origin.y+232,82,2),new Color(1,.83f,.22f));
            }
            ui.TextInput("name",new Rect(origin.x+250,origin.y+235,124,21),characterName,GameConstants.CharacterNameMaxLength,v=>characterName=v);
            ui.Raise("name");
            ui.NativeRosterSelection("create",new Rect(origin.x+300,origin.y+265,46,19),()=>CreateCharacter(characterName,selectedStartingJob),!loading,"생성하고 튜토리얼 시작");
            ui.Raise("create");
            ui.NativeLibraryButton("back",origin+new Vector2(374,8),38,()=>creating=false,!loading,"캐릭터 선택으로 돌아가기");
            ui.Raise("back");
            ui.NativeBitmapText("creation-choice",new Rect(160,372,320,18),"선택: "+NativeCharacterProfile.StartingJobName(selectedStartingJob),Color.white,alignment:TextAnchor.MiddleCenter);
            if(!string.IsNullOrEmpty(loadMessage))ui.NativeBitmapText("creation-message",new Rect(120,398,400,42),loadMessage,Color.white,2);
        }
    }
}
