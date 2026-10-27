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
            var origin=NativeEntryLayout.CreationOrigin;
            ui.EntryArtwork("creation-native",new Rect(origin,new Vector2(397,300)),creationBackground);
            if(portraits==null)portraits=Resources.Load<CharacterPortraitCatalog>("UI/CharacterPortraits/CharacterPortraits");
            int[] choices={100,101,103};
            for(int i=0;i<choices.Length;i++)
            {
                int job=choices[i];var card=NativeEntryLayout.Offset(NativeEntryLayout.CreationCard(i),origin);
                if(portraits!=null && portraits.TryGet(job,out var face))ui.PortraitTexture("creation-face-"+job,NativeEntryLayout.Offset(NativeEntryLayout.CreationFace(i),origin),face);
                ui.NativeRosterSelection("creation-job-"+job,card,()=>SelectStartingJob(job),!loading,NativeCharacterProfile.StartingJobName(job));
                if(selectedStartingJob==job)ui.SolidRect("creation-selected",new Rect(card.x+3,card.yMax-3,84,2),new Color(1,.83f,.22f));
            }
            ui.EntryInput("creation-name",NativeEntryLayout.Offset(NativeEntryLayout.CreationName,origin),characterName,GameConstants.CharacterNameMaxLength,v=>characterName=v,submit:()=>CreateCharacter(characterName,selectedStartingJob),textColor:new Color(.12f,.08f,.03f));
            ui.NativeRosterSelection("creation-confirm",new Rect(origin.x+300,origin.y+265,46,19),()=>CreateCharacter(characterName,selectedStartingJob),!loading,"생성하고 튜토리얼 시작");
            if(!string.IsNullOrEmpty(loadMessage))DrawEntryMessage(new Rect(120,origin.y+348,400,54));
        }
    }
}
