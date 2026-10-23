using System;
using DarkSaver.Combat;
using DarkSaver.Jobs;
using DarkSaver.Network;
using UnityEngine;

namespace DarkSaver.UI
{
    public sealed partial class TownServicesPanel
    {
        private CharacterAdjustmentOptionsNotification adjustmentOptions;
        private ClassMenuIconCatalog adjustmentClassIcons;
        public Sprite CharacterAdjustmentClassIcon(int jobId)
        {
            if(adjustmentClassIcons==null) adjustmentClassIcons=Resources.Load<ClassMenuIconCatalog>("UI/ClassMenuIcons/ClassMenuIcons");
            return adjustmentClassIcons!=null && adjustmentClassIcons.TryGet(jobId,out var icon)?icon:null;
        }
        private string[] adjustmentOptionNames,adjustmentOptionDetails;
        private string adjustmentLeftName,adjustmentRightName,adjustmentLeftDetail,adjustmentRightDetail;
        private Action inspectLeftClass,inspectRightClass,inspectOptionClass,previousClassOption,nextClassOption;
        public int CharacterAdjustmentOptionIndex { get; private set; }
        public int CharacterAdjustmentClassSlot { get; private set; }=-1; // 0:left,1:right,2:bottom
        public bool HasCharacterAdjustmentOptions=>adjustmentOptions!=null;
        public string CharacterAdjustmentClassDetail { get; private set; }
        public bool BindCharacterAdjustmentOptions(CharacterAdjustmentOptionsNotification options,JobCatalog catalog)
        {
            if(!CanInspectAdjustmentMagic || options==null || catalog==null || GetComponent<BattleTransitionController>()?.HasCreatedCharacter!=true) return false;
            inspectLeftClass??=()=>SelectCharacterAdjustmentClass(0); inspectRightClass??=()=>SelectCharacterAdjustmentClass(1);
            inspectOptionClass??=()=>SelectCharacterAdjustmentClass(2);
            previousClassOption??=()=>SetCharacterAdjustmentOptionIndex(CharacterAdjustmentOptionIndex-1);
            nextClassOption??=()=>SetCharacterAdjustmentOptionIndex(CharacterAdjustmentOptionIndex+1);
            string Name(short id)=>catalog.TryGet(id,out var row)?row.Name:"미확인 클래스 #"+id;
            string Detail(short id)=>catalog.TryGet(id,out var row)?row.Name+"\n"+(row.Description??"").Replace('_',' '):"미확인 클래스 #"+id;
            var names=new string[options.OptionCount]; var details=new string[options.OptionCount];
            for(int i=0;i<names.Length;i++) { options.TryGetOption(i,out short id); names[i]=Name(id); details[i]=Detail(id); }
            adjustmentOptions=options; adjustmentOptionNames=names; adjustmentOptionDetails=details;
            adjustmentLeftName=Name(options.ClassLookupValue1); adjustmentLeftDetail=Detail(options.ClassLookupValue1);
            adjustmentRightName=Name(options.ClassLookupValue0); adjustmentRightDetail=Detail(options.ClassLookupValue0);
            CharacterAdjustmentOptionIndex=0; CharacterAdjustmentClassSlot=-1; CharacterAdjustmentClassDetail=null;
            // Original87 chooses first lookup, then options, then second lookup.
            if(options.ClassLookupValue0!=-1) SelectCharacterAdjustmentClass(1);
            else if(options.OptionCount>0) SelectCharacterAdjustmentClass(2);
            else if(options.ClassLookupValue1!=-1) SelectCharacterAdjustmentClass(0);
            return true;
        }
        public bool TryGetCharacterAdjustmentClass(int slot,out short id)
        {
            id=-1;
            if(!CanInspectAdjustmentMagic || adjustmentOptions==null) return false;
            if(slot==0) id=adjustmentOptions.ClassLookupValue1;
            else if(slot==1) id=adjustmentOptions.ClassLookupValue0;
            else if(slot==2 && adjustmentOptions.TryGetOption(CharacterAdjustmentOptionIndex,out id)) return id!=-1;
            else return false;
            return id!=-1;
        }
        public bool SelectCharacterAdjustmentClass(int slot)
        {
            if(!TryGetCharacterAdjustmentClass(slot,out _)) return false;
            CharacterAdjustmentClassSlot=slot;
            CharacterAdjustmentClassDetail=slot==0?adjustmentLeftDetail:slot==1?adjustmentRightDetail:adjustmentOptionDetails[CharacterAdjustmentOptionIndex];
            return true;
        }
        public bool SetCharacterAdjustmentOptionIndex(int index)
        {
            if(!CanInspectAdjustmentMagic || adjustmentOptions==null || (uint)index>=(uint)adjustmentOptions.OptionCount) return false;
            CharacterAdjustmentOptionIndex=index;
            if(CharacterAdjustmentClassSlot==2 && !SelectCharacterAdjustmentClass(2)) { CharacterAdjustmentClassSlot=-1; CharacterAdjustmentClassDetail=null; }
            return true;
        }
        private void ClearCharacterAdjustmentOptions()
        {
            adjustmentOptions=null; adjustmentOptionNames=adjustmentOptionDetails=null;
            adjustmentLeftName=adjustmentRightName=adjustmentLeftDetail=adjustmentRightDetail=null;
            CharacterAdjustmentOptionIndex=0; CharacterAdjustmentClassSlot=-1; CharacterAdjustmentClassDetail=null;
        }
        private void DrawCharacterAdjustmentOptions(Vector2 origin,NativeBattleWindows.Window window)
        {
            var session=GetComponent<BattleTransitionController>();
            var current=session!=null && session.HasCreatedCharacter?CharacterAdjustmentClassIcon(session.PlayerJobId):null;
            if(current!=null) {
                ui.Portrait("character-adjustment-class-current",new Rect(origin+new Vector2(99,228),new Vector2(32,32)),current);
                ui.Raise("character-adjustment-class-current");
            }
            if(adjustmentOptions==null) return;
            if(TryGetCharacterAdjustmentClass(0,out short left)) ui.ItemSlotButton("character-adjustment-class-left",new Rect(origin+new Vector2(32,228),new Vector2(32,32)),inspectLeftClass,CharacterAdjustmentClassIcon(left),1,CharacterAdjustmentClassSlot==0,adjustmentLeftName);
            if(TryGetCharacterAdjustmentClass(1,out short right)) ui.ItemSlotButton("character-adjustment-class-right",new Rect(origin+new Vector2(167,228),new Vector2(32,32)),inspectRightClass,CharacterAdjustmentClassIcon(right),1,CharacterAdjustmentClassSlot==1,adjustmentRightName);
            if(TryGetCharacterAdjustmentClass(2,out short option)) ui.ItemSlotButton("character-adjustment-class-option",new Rect(origin+new Vector2(100,296),new Vector2(32,32)),inspectOptionClass,CharacterAdjustmentClassIcon(option),1,CharacterAdjustmentClassSlot==2,adjustmentOptionNames[CharacterAdjustmentOptionIndex]);
            ui.NativeImageButton("character-adjustment-option-prev",origin+window.controls[16].rect.position,window.controls[16],previousClassOption,CharacterAdjustmentOptionIndex>0,"이전 후보 (조회만)");
            ui.NativeImageButton("character-adjustment-option-next",origin+window.controls[17].rect.position,window.controls[17],nextClassOption,CharacterAdjustmentOptionIndex+1<adjustmentOptions.OptionCount,"다음 후보 (조회만)");
            if(CharacterAdjustmentClassDetail!=null) {
                ui.NativeBitmapText("character-adjustment-class-detail",new Rect(origin+new Vector2(30,349),new Vector2(171,39)),CharacterAdjustmentClassDetail,Color.black,maximumLines:2,alignment:TextAnchor.UpperLeft);
            }
        }
    }
}
