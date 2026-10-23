using System;
using System.Globalization;
using DarkSaver.Magic;
using DarkSaver.Network;
using UnityEngine;

namespace DarkSaver.UI
{
    public sealed partial class TownServicesPanel
    {
        private sealed class AdjustmentMagicCell
        {
            public string key, tooltip, detail;
            public Sprite frame0, frame1;
            public Action select;
        }
        private DarkSaver.Core.NativeCharacterProfile sessionMagicBaseline;
        private System.Collections.Generic.List<int> sessionBattleMagic;
        private int sessionLearnedPage;
        private bool sessionBattleMagicEdited;
        public bool HasSessionBattleMagic => sessionMagicBaseline!=null;
        private bool HasBattleMagicChanges => sessionMagicBaseline!=null && sessionBattleMagicEdited &&
            (!sessionMagicBaseline.hasBattleMagicSelection || !System.Linq.Enumerable.SequenceEqual(sessionMagicBaseline.battleMagicIds,sessionBattleMagic));
        private void BeginSessionBattleMagic()
        {
            var profile=GetComponent<DarkSaver.Combat.BattleTransitionController>()?.CaptureCharacterProfile();
            var catalog=GetComponent<MagicCatalogPanel>()?.Catalog;
            if(profile==null || !profile.hasClassAbilities || catalog==null) return;
            foreach(int id in profile.learnedMagicIds) if(!catalog.TryGet(id,out _)) return;
            sessionMagicBaseline=profile;
            var initial=profile.hasBattleMagicSelection ? profile.battleMagicIds : profile.learnedMagicIds;
            sessionBattleMagic=new System.Collections.Generic.List<int>(initial.Length<=8 ? initial : Array.Empty<int>());
            sessionLearnedPage=0;sessionBattleMagicEdited=false; RebuildSessionMagicCells();
        }
        private void RebuildSessionMagicCells()
        {
            adjustmentMagicCells=new AdjustmentMagicCell[24];
            var catalog=GetComponent<MagicCatalogPanel>().Catalog;
            for(int list=0;list<2;list++) {
                var ids=list==0 ? sessionMagicBaseline.learnedMagicIds : sessionBattleMagic.ToArray();
                int offset=list==0 ? sessionLearnedPage*16 : 0;
                for(int index=0;index<Math.Min(list==0?16:8,ids.Length-offset);index++) {
                    catalog.TryGet(ids[offset+index],out var entry);int l=list,n=index;
                    adjustmentMagicCells[list*16+index]=new AdjustmentMagicCell {
                        key="character-adjustment-magic-"+list+"-"+index,tooltip=entry.Name,
                        detail=FormatCharacterAdjustmentMagicDetail(entry),
                        frame1=BattleIconFrames.FromMagicCoordinates(entry.IconX,entry.IconY,0),
                        frame0=BattleIconFrames.FromMagicCoordinates(entry.IconX,entry.IconY,GameConstants.BattleIconFrameSeconds),
                        select=()=>SelectCharacterAdjustmentMagic(l,n)
                    };
                }
            }
            CharacterAdjustmentSelectedMagicList=CharacterAdjustmentSelectedMagicIndex=-1;
        }
        private bool CanAddSelectedBattleMagic => HasSessionBattleMagic && CharacterAdjustmentSelectedMagicList==0
            && sessionBattleMagic.Count<8 && TryGetCharacterAdjustmentMagic(0,CharacterAdjustmentSelectedMagicIndex,out var id) && !sessionBattleMagic.Contains(id);
        private bool CanRemoveSelectedBattleMagic => HasSessionBattleMagic && CharacterAdjustmentSelectedMagicList==1
            && TryGetCharacterAdjustmentMagic(1,CharacterAdjustmentSelectedMagicIndex,out _);
        public bool AddSelectedBattleMagic()
        {
            if(!CanAddSelectedBattleMagic || !TryGetCharacterAdjustmentMagic(0,CharacterAdjustmentSelectedMagicIndex,out var id)) return false;
            sessionBattleMagic.Add(id);sessionBattleMagicEdited=true;RebuildSessionMagicCells();return true;
        }
        public bool RemoveSelectedBattleMagic()
        {
            if(!CanRemoveSelectedBattleMagic || !TryGetCharacterAdjustmentMagic(1,CharacterAdjustmentSelectedMagicIndex,out var id)) return false;
            sessionBattleMagic.Remove(id);sessionBattleMagicEdited=true;RebuildSessionMagicCells();return true;
        }
        private byte[] adjustmentMagicSnapshot;
        private AdjustmentMagicCell[] adjustmentMagicCells;
        public int CharacterAdjustmentSelectedMagicList { get; private set; }=-1;
        public int CharacterAdjustmentSelectedMagicIndex { get; private set; }=-1;
        public bool HasCharacterAdjustmentMagicSnapshot=>adjustmentMagicSnapshot!=null;
        public string CharacterAdjustmentMagicDetail=>CharacterAdjustmentSelectedMagicList>=0
            ?adjustmentMagicCells[CharacterAdjustmentSelectedMagicList*16+CharacterAdjustmentSelectedMagicIndex]?.detail:null;
        // Saver 41A4E0 uses the raw Team field (+118), not Attribute, for this label.
        public static string FormatCharacterAdjustmentMagicDetail(MagicCatalogEntry entry)
        {
            if(entry==null) return null;
            string values="";
            if(entry.Mp>0) values+="마법력 소모: "+entry.Mp.ToString(CultureInfo.InvariantCulture)+" ";
            if(entry.Range>0) values+="거리: "+entry.Range.ToString(CultureInfo.InvariantCulture)+" ";
            if(entry.Area>0) values+="범위: "+entry.Area.ToString(CultureInfo.InvariantCulture)+" ";
            return (entry.Name??"")+"\n마법 종류: "+(entry.Team==0x22?"공격 계열":"보조 계열")+"\n"+values;
        }
        private bool CanInspectAdjustmentMagic=>IsOpen && CanOpen && Service==TownService.CharacterInfo && IsCharacterAdjustmentOpen;

        // Read-only display binding for an explicitly supplied info payload, not a packet decoder,
        // entitlement grant or an implicit mapping from NativeCharacterProfile.learnedMagicIds.
        public bool BindCharacterAdjustmentMagicSnapshot(ReadOnlySpan<byte> payload)
        {
            if(!CanInspectAdjustmentMagic || !OriginalCharacterInfoValues.TryReadMagicCount(payload,0,out int first)
                || !OriginalCharacterInfoValues.TryReadMagicCount(payload,1,out int second)) return false;
            sessionMagicBaseline=null;sessionBattleMagic=null;
            var snapshot=payload.ToArray(); var cells=new AdjustmentMagicCell[24];
            var catalog=GetComponent<MagicCatalogPanel>()?.Catalog;
            for(int list=0;list<2;list++) for(int index=0;index<(list==0?first:second);index++) {
                OriginalCharacterInfoValues.TryReadMagicId(snapshot,list,index,out short id);
                var entry=catalog!=null && catalog.TryGet(id,out var found)?found:null;
                int capturedList=list,capturedIndex=index;
                cells[list*16+index]=new AdjustmentMagicCell {
                    key="character-adjustment-magic-"+list+"-"+index,
                    tooltip=entry!=null?entry.Name:"미확인 마법 #"+id,
                    detail=entry!=null?FormatCharacterAdjustmentMagicDetail(entry):"미확인 마법 #"+id,
                    frame1=entry!=null?BattleIconFrames.FromMagicCoordinates(entry.IconX,entry.IconY,0):null,
                    frame0=entry!=null?BattleIconFrames.FromMagicCoordinates(entry.IconX,entry.IconY,GameConstants.BattleIconFrameSeconds):null,
                    select=()=>SelectCharacterAdjustmentMagic(capturedList,capturedIndex)
                };
            }
            adjustmentMagicSnapshot=snapshot; adjustmentMagicCells=cells;
            CharacterAdjustmentSelectedMagicList=CharacterAdjustmentSelectedMagicIndex=-1; return true;
        }
        public bool TryGetCharacterAdjustmentMagic(int list,int index,out short id)
        {
            id=0;
            if(CanInspectAdjustmentMagic && sessionMagicBaseline!=null) {
                if(index<0 || list<0 || list>1) return false;
                int at=list==0 ? sessionLearnedPage*16+index : index;
                int count=list==0 ? sessionMagicBaseline.learnedMagicIds.Length : sessionBattleMagic.Count;
                if(index>=(list==0?16:8) || at>=count) return false;
                id=(short)(list==0 ? sessionMagicBaseline.learnedMagicIds[at] : sessionBattleMagic[at]);return true;
            }
            return CanInspectAdjustmentMagic && adjustmentMagicSnapshot!=null
                && OriginalCharacterInfoValues.TryReadMagicId(adjustmentMagicSnapshot,list,index,out id);
        }
        public bool SelectCharacterAdjustmentMagic(int list,int index)
        {
            if(!TryGetCharacterAdjustmentMagic(list,index,out _)) return false;
            CharacterAdjustmentSelectedMagicList=list; CharacterAdjustmentSelectedMagicIndex=index; return true;
        }
        private void ClearCharacterAdjustmentMagic()
        {
            adjustmentMagicSnapshot=null; adjustmentMagicCells=null;sessionMagicBaseline=null;sessionBattleMagic=null;sessionLearnedPage=0;sessionBattleMagicEdited=false;
            CharacterAdjustmentSelectedMagicList=CharacterAdjustmentSelectedMagicIndex=-1;
        }
        private void DrawCharacterAdjustmentMagic(Vector2 origin)
        {
            if(adjustmentMagicCells==null) return;
            if(sessionMagicBaseline!=null) {
                int pages=Math.Max(1,(sessionMagicBaseline.learnedMagicIds.Length+15)/16);
                ui.Button("character-magic-prev",new Rect(origin.x+208,origin.y+17,38,23),"<",()=>{sessionLearnedPage--;RebuildSessionMagicCells();},sessionLearnedPage>0);
                ui.Button("character-magic-next",new Rect(origin.x+430,origin.y+17,38,23),">",()=>{sessionLearnedPage++;RebuildSessionMagicCells();},sessionLearnedPage+1<pages);
            }
            int frame=BattleIconFrames.FrameAt(Time.unscaledTimeAsDouble);
            for(int list=0;list<2;list++) for(int index=0;index<OriginalCharacterInfoValues.MagicListCapacity(list);index++) {
                var cell=adjustmentMagicCells[list*16+index]; if(cell==null) continue;
                OriginalCharacterInfoValues.TryGetMagicCellOrigin(list,index,out int x,out int y);
                ui.ItemSlotButton(cell.key,new Rect(origin+new Vector2(x,y),new Vector2(32,32)),cell.select,
                    frame==0?cell.frame0:cell.frame1,1,
                    CharacterAdjustmentSelectedMagicList==list && CharacterAdjustmentSelectedMagicIndex==index,cell.tooltip);
            }
            if(CharacterAdjustmentSelectedMagicList>=0) {
                // Preserve three original text groups; the existing local rectangle is a clip guard.
                ui.NativeBitmapText("character-adjustment-magic-selection",new Rect(origin+new Vector2(177,160),new Vector2(345,37)),CharacterAdjustmentMagicDetail,Color.black,maximumLines:3,alignment:TextAnchor.UpperLeft,textScale:.8f);
            }
        }
    }
}
