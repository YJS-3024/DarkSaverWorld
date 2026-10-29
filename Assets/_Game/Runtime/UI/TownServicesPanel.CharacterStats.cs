using System;
using System.Globalization;
using DarkSaver.Network;
using DarkSaver.Core;
using DarkSaver.Combat;
using UnityEngine;

namespace DarkSaver.UI
{
    public sealed partial class TownServicesPanel
    {
        private string[] adjustmentStatTexts;
        private byte[] adjustmentPreviewCurrent,adjustmentPreviewBaseline;
        private Action[] adjustmentStatActions;
        private NativeCharacterProfile sessionStatBaseline;
        private int[] sessionStatDeltas;
        private bool[] sessionStatEnabled;
        public bool HasCharacterAdjustmentStatPreview=>adjustmentPreviewCurrent!=null || sessionStatBaseline!=null;
        private void EnsureAdjustmentStatActions()
        {
            if(adjustmentStatActions!=null) return;
            adjustmentStatActions=new Action[12];
            for(int i=0;i<12;i++) {
                int stat=i%6; var direction=i<6?CharacterStatAdjustmentDirection.Decrease:CharacterStatAdjustmentDirection.Increase;
                adjustmentStatActions[i]=()=>PreviewCharacterAdjustmentStat(stat,direction);
            }
        }
        private void BeginSessionStatAllocation()
        {
            ClearCharacterAdjustmentStats();
            var profile=GetComponent<BattleTransitionController>()?.CaptureCharacterProfile();
            if(profile==null || !profile.IsValid) return;
            sessionStatBaseline=profile; sessionStatDeltas=new int[4];
            EnsureAdjustmentStatActions(); RefreshSessionStatAllocation(profile);
        }
        private void RefreshSessionStatAllocation(NativeCharacterProfile projected)
        {
            adjustmentStatTexts=new string[6];
            adjustmentStatTexts[0]="체력: 배분 불가"; adjustmentStatTexts[1]="마력: 배분 불가";
            int[] totals={sessionStatBaseline.allocatedAttack,sessionStatBaseline.allocatedDefense,sessionStatBaseline.allocatedMagicAttack,sessionStatBaseline.allocatedMagicDefense};
            for(int i=0;i<4;i++) adjustmentStatTexts[i+2]="확정 추가:"+totals[i]+" / 이번:+"+sessionStatDeltas[i];
            CharacterAdjustmentPointsText=projected.unspentClassChangeStatPoints.ToString(CultureInfo.InvariantCulture);
            sessionStatEnabled=new bool[12];
            for(int i=0;i<12;i++) sessionStatEnabled[i]=TryPreviewSessionStat(i%6,i<6?CharacterStatAdjustmentDirection.Decrease:CharacterStatAdjustmentDirection.Increase,out _,out _);
        }
        private bool TryPreviewSessionStat(int stat,CharacterStatAdjustmentDirection direction,out int[] deltas,out NativeCharacterProfile projected)
        {
            deltas=null; projected=null;
            if(!CanInspectAdjustmentMagic || sessionStatBaseline==null || stat<2 || stat>5
                || (direction!=CharacterStatAdjustmentDirection.Increase && direction!=CharacterStatAdjustmentDirection.Decrease)
                || GetComponent<BattleTransitionController>()?.IsStatAllocationCurrent(sessionStatBaseline)!=true) return false;
            deltas=(int[])sessionStatDeltas.Clone(); int index=stat-2;
            if(direction==CharacterStatAdjustmentDirection.Decrease) {if(deltas[index]==0)return false; deltas[index]--;}
            else {if(deltas[index]==int.MaxValue)return false; deltas[index]++;}
            if(deltas[0]==0 && deltas[1]==0 && deltas[2]==0 && deltas[3]==0) {projected=sessionStatBaseline;return true;}
            return sessionStatBaseline.TryBuildStatAllocation(deltas[0],deltas[1],deltas[2],deltas[3],out projected);
        }
        public bool ConfirmCharacterStatAllocation()
        {
            if(!CanInspectAdjustmentMagic || sessionStatBaseline==null) return false;
            var session=GetComponent<BattleTransitionController>();
            if(session==null || !session.TryAllocateCharacterStats(sessionStatBaseline,sessionStatDeltas[0],sessionStatDeltas[1],sessionStatDeltas[2],sessionStatDeltas[3],out var reason,HasBattleMagicChanges?sessionBattleMagic.ToArray():null)) return false;
            SaveMessage=reason; CloseCharacterAdjustment(); BindSessionCharacterInfoValues(); return true;
        }
        private bool CanConfirmSessionStats=>CanInspectAdjustmentMagic && sessionStatBaseline!=null
            && GetComponent<BattleTransitionController>()?.IsStatAllocationCurrent(sessionStatBaseline)==true
            && (sessionStatDeltas[0]>0 || sessionStatDeltas[1]>0 || sessionStatDeltas[2]>0 || sessionStatDeltas[3]>0 || HasBattleMagicChanges);
        public bool BindCharacterAdjustmentStatPreview(ReadOnlySpan<byte> current,ReadOnlySpan<byte> baseline,CharacterAdjustmentValuesNotification costs)
            =>costs!=null && BindCharacterAdjustmentStatPreview(current,baseline);
        // Local policy comes from GameConstants, independently of historical per-stat server costs.
        public bool BindCharacterAdjustmentStatPreview(ReadOnlySpan<byte> current,ReadOnlySpan<byte> baseline)
        {
            if(!BindCharacterAdjustmentStatBars(current,baseline)) return false;
            adjustmentPreviewCurrent=current.ToArray(); adjustmentPreviewBaseline=baseline.ToArray();
            EnsureAdjustmentStatActions();
            return true;
        }
        public bool CanPreviewCharacterAdjustmentStat(int stat,CharacterStatAdjustmentDirection direction)=>inspectedWorldUnit!=null ? CanRequestWorldStat(stat,direction) : sessionStatBaseline!=null
            ? CanInspectAdjustmentMagic && (uint)stat<6 && sessionStatEnabled!=null
                && (direction==CharacterStatAdjustmentDirection.Increase || direction==CharacterStatAdjustmentDirection.Decrease)
                && sessionStatEnabled[stat+(direction==CharacterStatAdjustmentDirection.Increase?6:0)]
                && GetComponent<BattleTransitionController>()?.IsStatAllocationCurrent(sessionStatBaseline)==true
            : CanInspectAdjustmentMagic && OriginalCharacterInfoValues.TryPreviewStatAdjustment(adjustmentPreviewCurrent,adjustmentPreviewBaseline,GameConstants.GetClassStatPointCost(stat),stat,direction,out _,out _);
        public bool PreviewCharacterAdjustmentStat(int stat,CharacterStatAdjustmentDirection direction)
        {
            if(inspectedWorldUnit!=null)return CanRequestWorldStat(stat,direction) && SendWorldUnitRequest(new CharacterStatAdjustmentRequest(inspectedWorldUnit.RecordId,stat,direction));
            if(sessionStatBaseline!=null) {
                if(!TryPreviewSessionStat(stat,direction,out var deltas,out var projected))return false;
                sessionStatDeltas=deltas; RefreshSessionStatAllocation(projected);return true;
            }
            if(!CanInspectAdjustmentMagic || !OriginalCharacterInfoValues.TryPreviewStatAdjustment(adjustmentPreviewCurrent,adjustmentPreviewBaseline,GameConstants.GetClassStatPointCost(stat),stat,direction,out short value,out short points)) return false;
            var next=(byte[])adjustmentPreviewCurrent.Clone(); var baseline=adjustmentPreviewBaseline;
            int offset=0x16+stat*6;
            next[offset]=(byte)value; next[offset+1]=(byte)(value>>8); next[0x38]=(byte)points; next[0x39]=(byte)(points>>8);
            if(!BindCharacterAdjustmentStatBars(next,baseline)) return false;
            adjustmentPreviewCurrent=next; adjustmentPreviewBaseline=baseline;
            return true;
        }
        private int[] adjustmentCurrentBarWidths,adjustmentInitialBarWidths;
        private static readonly Color32[] AdjustmentStatColors={new(0,0,255,255),new(255,0,0,255),new(255,128,64,255),new(0,255,255,255),new(0,255,0,255),new(255,255,0,255)};
        private static readonly string[] AdjustmentBarKeys={"character-adjustment-bar-0","character-adjustment-bar-1","character-adjustment-bar-2","character-adjustment-bar-3","character-adjustment-bar-4","character-adjustment-bar-5"};
        private static readonly string[] AdjustmentGainKeys={"character-adjustment-gain-0","character-adjustment-gain-1","character-adjustment-gain-2","character-adjustment-gain-3","character-adjustment-gain-4","character-adjustment-gain-5"};
        public bool HasCharacterAdjustmentStatBars=>adjustmentCurrentBarWidths!=null;
        public bool BindCharacterAdjustmentStatBars(ReadOnlySpan<byte> current,ReadOnlySpan<byte> baseline)
        {
            if(!CanInspectAdjustmentMagic) return false;
            var widths=new int[6]; var initial=new int[6];
            for(int i=0;i<6;i++) if(!OriginalCharacterInfoValues.TryCalculateEquipmentStatBar(current,baseline,i,176,out widths[i],out initial[i])) return false;
            if(!BindCharacterAdjustmentStatsSnapshot(current)) return false;
            adjustmentCurrentBarWidths=widths; adjustmentInitialBarWidths=initial; return true;
        }
        private static readonly string[] AdjustmentStatKeys={"character-adjustment-stat-0","character-adjustment-stat-1","character-adjustment-stat-2","character-adjustment-stat-3","character-adjustment-stat-4","character-adjustment-stat-5"};
        public bool HasCharacterAdjustmentStatsSnapshot=>adjustmentStatTexts!=null;
        public string CharacterAdjustmentPointsText { get; private set; }
        public string CharacterAdjustmentStatText(int index)=>adjustmentStatTexts!=null && (uint)index<6?adjustmentStatTexts[index]:null;
        public bool BindCharacterAdjustmentStatsSnapshot(ReadOnlySpan<byte> payload)
        {
            if(!CanInspectAdjustmentMagic || !OriginalCharacterInfoValues.TryReadRemainingStatPoints(payload,out short points)) return false;
            var texts=new string[6];
            for(int i=0;i<6;i++) {
                if(!OriginalCharacterInfoValues.TryReadEquipmentStatTriplet(payload,i,out short before,out short after,out short limit)) return false;
                texts[i]=string.Format(CultureInfo.InvariantCulture,"장착전:{0}/장착후:{1}/한계치:{2}",before,after,limit);
            }
            adjustmentPreviewCurrent=adjustmentPreviewBaseline=null; sessionStatBaseline=null; sessionStatDeltas=null; sessionStatEnabled=null;
            adjustmentStatTexts=texts; CharacterAdjustmentPointsText=points.ToString(CultureInfo.InvariantCulture);
            adjustmentCurrentBarWidths=adjustmentInitialBarWidths=null; return true;
        }
        private void ClearCharacterAdjustmentStats() { adjustmentStatTexts=null; CharacterAdjustmentPointsText=null; adjustmentCurrentBarWidths=adjustmentInitialBarWidths=null; adjustmentPreviewCurrent=adjustmentPreviewBaseline=null; sessionStatBaseline=null; sessionStatDeltas=null; sessionStatEnabled=null; }
        private void DrawCharacterAdjustmentStatControls(Vector2 origin,NativeBattleWindows.Window window)
        {
            if(!HasCharacterAdjustmentStatPreview) return;
            for(int i=0;i<12;i++) {
                int control=i+4; var direction=i<6?CharacterStatAdjustmentDirection.Decrease:CharacterStatAdjustmentDirection.Increase;
                ui.NativeImageButton(CharacterAdjustmentControlKeys[control],origin+window.controls[control].rect.position,window.controls[control],adjustmentStatActions[i],
                    CanPreviewCharacterAdjustmentStat(i%6,direction),sessionStatBaseline!=null?"추가 능력치 배분 · 확인 전 조절 가능":"능력치 미리보기 · 저장/서버 승인 미연결");
            }
        }
        private void DrawCharacterAdjustmentStats(Vector2 origin)
        {
            if(adjustmentStatTexts==null) return;
            for(int i=0;i<6;i++) {
                if(adjustmentCurrentBarWidths!=null) {
                    int width=adjustmentCurrentBarWidths[i], initial=adjustmentInitialBarWidths[i];
                    if(width>0) ui.SolidRect(AdjustmentBarKeys[i],new Rect(origin+new Vector2(285,226+i*22),new Vector2(width,10)),AdjustmentStatColors[i]);
                    if(width>initial) ui.SolidRect(AdjustmentGainKeys[i],new Rect(origin+new Vector2(285+initial,226+i*22),new Vector2(width-initial,10)),Color.white);
                }
                // Original row begins at (285,226), stepping22. Local font/height for legibility.
                ui.Label(AdjustmentStatKeys[i],new Rect(origin+new Vector2(285,226+i*22),new Vector2(176,16)),
                    adjustmentStatTexts[i],10,alignment:TextAnchor.MiddleCenter,color:Color.black);
                ui.Raise(AdjustmentStatKeys[i]);
            }
            ui.NativeBitmapText("character-adjustment-points",new Rect(origin+new Vector2(484,365),new Vector2(40,16)),CharacterAdjustmentPointsText,Color.black);
        }
    }
}
