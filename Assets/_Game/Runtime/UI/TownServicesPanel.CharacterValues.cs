using System;
using System.Globalization;
using System.Collections.Generic;
using DarkSaver.Combat;
using DarkSaver.Network;
using UnityEngine;

namespace DarkSaver.UI
{
    public sealed partial class TownServicesPanel
    {
        private string[] characterInfoValues;
        private string localEquipmentWeaponName;
        private static readonly string[] CharacterInfoValueKeys=BuildCharacterInfoValueKeys();
        private static string[] BuildCharacterInfoValueKeys()
        {
            var keys=new string[16];
            for(int c=0;c<2;c++) for(int r=0;r<8;r++) keys[c*8+r]="character-info-value-"+c+"-"+r;
            return keys;
        }
        public bool HasCharacterInfoValues=>characterInfoValues!=null;
        public bool CharacterInfoValuesAreLocal { get; private set; }
        public string CharacterInfoPhysicalBreakdown { get; private set; }
        public string CharacterInfoVitalsText { get; private set; }
        public string CharacterInfoProgressStateText { get; private set; }
        public string CharacterInfoLocalSummaryText { get; private set; }
        // Native session values, not an emulated original server response.
        public bool BindSessionCharacterInfoValues()
        {
            var session=GetComponent<BattleTransitionController>();
            if(!IsOpen || !CanOpen || Service!=TownService.CharacterInfo || session==null
                || !session.TryGetPlayerPhysicalStats(out int baseAttack,out int baseDefense,out int attack,out int defense))return false;
            var values=new string[16];
            var profile=session.CaptureCharacterProfile();
            string level=profile.hasLevels?profile.jobLevel.ToString(CultureInfo.InvariantCulture)+"("+profile.baseLevel.ToString(CultureInfo.InvariantCulture)+")":null;
            string xp=session.PlayerExperience.ToString(CultureInfo.InvariantCulture),state="목표 미확인";
            string target="?";
            if(profile.hasLevels) {
                if(progression!=null && progression.TryGet(profile.jobId,profile.jobLevel,out var row)) {
                    if(row.nextJobId!=profile.jobId) {target="-";state="전직 대기";}
                    else if(profile.jobLevel>=DarkSaver.Jobs.NativePlayerJobs.MaximumLevel(profile.jobId)) {target="-";state="최고 직업 레벨";}
                    else if(row.requiredXp>0) {target=row.requiredXp.ToString(CultureInfo.InvariantCulture);state="다음 직업 레벨";}
                }
            }
            string vitals=null;
            if(session.HasPlayerVitals) {
                // Same maximum-stat rows as the original equipment-after fields, not current HP/MP.
                values[0]=session.PlayerMaxHp.ToString(CultureInfo.InvariantCulture);
                values[1]=session.PlayerMaxMp.ToString(CultureInfo.InvariantCulture);
                vitals="HP "+session.PlayerHp+"/"+session.PlayerMaxHp+" · MP "+session.PlayerMp+"/"+session.PlayerMaxMp;
            }
            // Display offsets 0x22/0x28 are also the attack/defense adjustment-after fields.
            values[2]=attack.ToString(CultureInfo.InvariantCulture); values[3]=defense.ToString(CultureInfo.InvariantCulture);
            string breakdown="공격 "+baseAttack+" + "+(attack-baseAttack)+" / 방어 "+baseDefense+" + "+(defense-baseDefense)+"\n직업 기본값 + 확정 배분 (장비 보정 제외)";
            if(profile.HasEquippedItems)breakdown="공격 "+baseAttack+" + 배분 "+profile.allocatedAttack+" + 장비 "+(attack-baseAttack-profile.allocatedAttack)
                +" / 방어 "+baseDefense+" + 배분 "+profile.allocatedDefense+" + 장비 "+(defense-baseDefense-profile.allocatedDefense)
                +"\n확인된 장비만 로컬 합산 · 미확인 장비 제외";
            ClearCharacterInfoValues(); characterInfoValues=values;
            CharacterInfoValuesAreLocal=true; CharacterInfoPhysicalBreakdown=breakdown;
            localEquipmentWeaponName=session.KnownEquipmentWeaponName();
            CharacterInfoLevelText=level; CharacterInfoExperienceText=xp+"/"+target;
            CharacterInfoProgressStateText=state; CharacterInfoVitalsText=vitals;
            BindSessionCharacterInfoHires(session);
            CharacterInfoLocalSummaryText=breakdown+"\n"+(vitals??"HP·MP 미확인")+" · "+state;return true;
        }
        public string CharacterInfoHiresText { get; private set; }
        private void BindSessionCharacterInfoHires(BattleTransitionController session)
        {
            var roster=session.CaptureOwnedRoster();
            if(roster==null) {CharacterInfoHiresText="보유 부대 정보 미확인";return;}
            var counts=new SortedDictionary<int,int>();
            int total=0,unknown=0; string hireType=null; bool mixedTypes=false;
            foreach(var unit in roster.units) {
                if(unit.hireOwnerId!=100 || string.IsNullOrEmpty(unit.hireType)) {unknown++;continue;}
                if(hireType==null)hireType=unit.hireType;else if(hireType!=unit.hireType)mixedTypes=true;
                counts.TryGetValue(unit.jobId,out int count); counts[unit.jobId]=count+1; total++;
            }
            var parts=new List<string>();
            foreach(var pair in counts) {
                string name=mercenaryJobs!=null && mercenaryJobs.TryGet(pair.Key,out var job) && job.CategoryLabel=="mercenary"
                    ?job.Name.Replace('_',' '):"병종 ID "+pair.Key.ToString(CultureInfo.InvariantCulture);
                parts.Add(name+" × "+pair.Value.ToString(CultureInfo.InvariantCulture));
            }
            // User-confirmed mercenary slot. Only a complete, unambiguous owned group supplies local fields.
            if(unknown==0 && !mixedTypes) {
                if(counts.Count==0)CharacterInfoAuxiliaryCountText="X 0";
                else if(counts.Count==1)foreach(var pair in counts)if(pair.Key<=short.MaxValue) {
                    CharacterInfoAuxiliaryClassId=(short)pair.Key;
                    CharacterInfoAuxiliaryCountText="X "+pair.Value.ToString(CultureInfo.InvariantCulture);
                }
            }
            CharacterInfoHiresText="보유 고용 용병 "+total.ToString(CultureInfo.InvariantCulture)+"명";
            if(parts.Count>0) CharacterInfoHiresText+=": "+string.Join(" · ",parts);
            if(mixedTypes) CharacterInfoHiresText+=" · 고용 타입 혼재";
            if(unknown>0) CharacterInfoHiresText+=" · 고용 정보 미확인 "+unknown.ToString(CultureInfo.InvariantCulture)+"명";
        }
        public string CharacterInfoLevelText { get; private set; }
        public string CharacterInfoExperienceText { get; private set; }
        public string CharacterInfoAgeText { get; private set; }
        public string CharacterInfoGenderText { get; private set; }
        public bool HasCharacterInfoDemographics=>CharacterInfoAgeText!=null;
        public string CharacterInfoWeaponName { get; private set; }
        public string CharacterInfoWeaponDisplayText => inspectedWorldUnit!=null ? WorldWeaponName : CharacterInfoWeaponName ?? (CharacterInfoValuesAreLocal?(localEquipmentWeaponName??"미확인"):null);
        public short CharacterInfoAuxiliaryClassId { get; private set; }=-1;
        public string CharacterInfoAuxiliaryCountText { get; private set; }
        public bool BindCharacterInfoWeaponName(string originalName)
        {
            if(!IsOpen || !CanOpen || Service!=TownService.CharacterInfo || GetComponent<BattleTransitionController>()?.HasCreatedCharacter!=true || originalName==null) return false;
            CharacterInfoWeaponName=originalName; return true;
        }
        // Separate state structure at the original window+B0, not the 724-byte info payload.
        public bool BindCharacterInfoDemographics(int originalAgeValue,int originalGenderValue)
        {
            if(!IsOpen || !CanOpen || Service!=TownService.CharacterInfo || GetComponent<BattleTransitionController>()?.HasCreatedCharacter!=true
                || (uint)originalGenderValue>2) return false;
            CharacterInfoAgeText=originalAgeValue.ToString(CultureInfo.InvariantCulture);
            CharacterInfoGenderText=originalGenderValue==0?"중성":originalGenderValue==1?"여":"남"; return true;
        }
        public string CharacterInfoValueText(int column,int row)=>characterInfoValues!=null && (uint)column<2 && (uint)row<8?characterInfoValues[column*8+row]:null;
        // Explicit original info payload only; never infer these fields from unrelated battle records.
        public bool BindCharacterInfoValues(ReadOnlySpan<byte> payload)
        {
            if(!IsOpen || !CanOpen || Service!=TownService.CharacterInfo || GetComponent<BattleTransitionController>()?.HasCreatedCharacter!=true
                || payload.Length!=OriginalCharacterInfoValues.PayloadSize) return false;
            var values=new string[16];
            for(int c=0;c<2;c++) for(int r=0;r<8;r++) {
                if(!OriginalCharacterInfoValues.TryRead(payload,c,r,out short value)) return false;
                values[c*8+r]=value.ToString(CultureInfo.InvariantCulture);
            }
            if(!OriginalCharacterInfoValues.TryReadHeader(payload,out short first,out short second,out uint xp,out uint target)) return false;
            CharacterInfoLevelText=first.ToString(CultureInfo.InvariantCulture)+"("+second.ToString(CultureInfo.InvariantCulture)+")";
            CharacterInfoExperienceText=xp.ToString(CultureInfo.InvariantCulture)+"/"+target.ToString(CultureInfo.InvariantCulture);
            CharacterInfoAuxiliaryClassId=unchecked((short)(payload[0x96] | payload[0x97]<<8));
            short count=unchecked((short)(payload[0x98] | payload[0x99]<<8));
            CharacterInfoAuxiliaryCountText="X "+count.ToString(CultureInfo.InvariantCulture);
            characterInfoValues=values; CharacterInfoValuesAreLocal=false; CharacterInfoPhysicalBreakdown=null;
            CharacterInfoVitalsText=CharacterInfoProgressStateText=CharacterInfoLocalSummaryText=CharacterInfoHiresText=null;return true;
        }
        private void ClearCharacterInfoValues() { localEquipmentWeaponName=null; CharacterInfoHiresText=null; characterInfoValues=null; CharacterInfoValuesAreLocal=false; CharacterInfoPhysicalBreakdown=null; CharacterInfoVitalsText=CharacterInfoProgressStateText=CharacterInfoLocalSummaryText=null; CharacterInfoLevelText=CharacterInfoExperienceText=null; CharacterInfoAgeText=CharacterInfoGenderText=null; CharacterInfoWeaponName=CharacterInfoAuxiliaryCountText=null; CharacterInfoAuxiliaryClassId=-1; }
        private MercenaryInfoIconCatalog characterInfoMercenaryIcons;
        public Sprite CharacterInfoMercenaryIcon(int jobId)
        {
            if(characterInfoMercenaryIcons==null) characterInfoMercenaryIcons=Resources.Load<MercenaryInfoIconCatalog>("UI/MercenaryInfoIcons/MercenaryInfoIcons");
            return characterInfoMercenaryIcons!=null && characterInfoMercenaryIcons.TryGet(jobId,out var icon)?icon:null;
        }
        private void DrawCharacterInfoValues(Vector2 origin)
        {
            string weaponText=CharacterInfoWeaponDisplayText;
            if(weaponText!=null) {
                if(inspectedWorldUnit!=null && CharacterInfoWindow(false)?.background!=null)
                    ui.NativeTexturePatch("character-info-weapon-clear",new Rect(origin+new Vector2(14,145),new Vector2(80,16)),CharacterInfoWindow(false).background,new Rect(14,111,80,16));
                bool unknown=inspectedWorldUnit==null && CharacterInfoValuesAreLocal && CharacterInfoWeaponName==null;
                // Cover the static "unarmed" background when no equipment state is available.
                if(unknown) {
                    ui.Panel("character-info-weapon-unknown",new Rect(origin+new Vector2(14,127),new Vector2(80,36)));ui.Raise("character-info-weapon-unknown");
                    ui.NativeBitmapText("character-info-weapon-label",new Rect(origin+new Vector2(14,128),new Vector2(80,16)),"무기",Color.white,alignment:TextAnchor.MiddleCenter);
                    ui.Raise("character-info-weapon-label");
                }
                ui.NativeBitmapText("character-info-weapon-name",new Rect(origin+new Vector2(14,145),new Vector2(80,16)),weaponText,unknown?Color.white:Color.black,alignment:TextAnchor.MiddleCenter);
                ui.Raise("character-info-weapon-name");
            }
            if(HasCharacterInfoDemographics) {
                ui.NativeBitmapText("character-info-age",new Rect(origin+new Vector2(146,107),new Vector2(60,16)),CharacterInfoAgeText,Color.black,alignment:TextAnchor.UpperLeft);
                ui.NativeBitmapText("character-info-gender",new Rect(origin+new Vector2(248,107),new Vector2(60,16)),CharacterInfoGenderText,Color.black,alignment:TextAnchor.UpperLeft);
            }
            if(characterInfoValues==null) return;
            if(CharacterInfoAuxiliaryClassId!=-1) {
                var icon=CharacterInfoMercenaryIcon(CharacterInfoAuxiliaryClassId) ?? CharacterAdjustmentClassIcon(CharacterInfoAuxiliaryClassId);
                if(icon!=null) { ui.Portrait("character-info-auxiliary-icon",new Rect(origin+new Vector2(29,178),new Vector2(32,32)),icon); ui.Raise("character-info-auxiliary-icon"); }
            }
            if(CharacterInfoAuxiliaryCountText!=null) ui.NativeBitmapText("character-info-auxiliary-count",new Rect(origin+new Vector2(73,186),new Vector2(30,16)),CharacterInfoAuxiliaryCountText,Color.black,alignment:TextAnchor.MiddleCenter);
            if(CharacterInfoLevelText!=null) ui.NativeBitmapText("character-info-level",new Rect(origin+new Vector2(146,73),new Vector2(174,16)),CharacterInfoLevelText,Color.black,alignment:TextAnchor.UpperLeft);
            if(CharacterInfoExperienceText!=null) ui.NativeBitmapText("character-info-experience",new Rect(origin+new Vector2(146,90),new Vector2(174,16)),CharacterInfoExperienceText,Color.black,alignment:TextAnchor.UpperLeft);
            for(int c=0;c<2;c++) for(int r=0;r<8;r++)
                if(characterInfoValues[c*8+r]!=null)
                ui.NativeBitmapText(CharacterInfoValueKeys[c*8+r],new Rect(origin+new Vector2(c==0?157:260,146+r*14),new Vector2(c==0?57:58,14)),
                    characterInfoValues[c*8+r],Color.black,alignment:TextAnchor.MiddleCenter);
        }
    }
}
