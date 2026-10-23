using UnityEngine;
using DarkSaver.Combat;

namespace DarkSaver.UI
{
    public sealed partial class TownServicesPanel
    {
        private NativeBattleWindows characterInfoCatalog, characterInfoMinimizedCatalog;
        private NativeBattleWindows.Control characterInfoRestoreControl;
        // Internal window ID40 constructs scene23.lsc, NOT scene40.lsc (equipment repair).
        private NativeBattleWindows characterAdjustmentCatalog;
        private static readonly string[] CharacterAdjustmentControlKeys = BuildCharacterAdjustmentControlKeys();
        private static string[] BuildCharacterAdjustmentControlKeys()
        {
            var keys=new string[21];
            for(int i=0;i<keys.Length;i++) keys[i]=i==18?"character-adjustment-apply":"character-adjustment-control-"+i;
            return keys;
        }
        public bool IsCharacterAdjustmentOpen { get; private set; }
        private NativeBattleWindows.Window CharacterAdjustmentWindow()
        {
            if(characterAdjustmentCatalog==null) characterAdjustmentCatalog=Resources.Load<NativeBattleWindows>("UI/TownAbilityAdjustment/NativeTownAbilityAdjustment");
            return characterAdjustmentCatalog!=null ? characterAdjustmentCatalog.Find(23) : null;
        }
        public bool OpenCharacterAdjustment()
        {
            if(!IsOpen || !CanOpen || Service!=TownService.CharacterInfo || IsCharacterInfoMinimized
                || IsCharacterAdjustmentOpen || CharacterAdjustmentWindow()==null) return false;
            ui?.CancelItemDrags(); IsCharacterAdjustmentOpen=true; BeginSessionStatAllocation(); BeginSessionBattleMagic(); return true;
        }
        public void CloseCharacterAdjustment() { IsCharacterAdjustmentOpen=false; ClearCharacterAdjustmentMagic(); ClearCharacterAdjustmentStats(); ClearCharacterAdjustmentOptions(); }
        private void DrawCharacterAdjustment()
        {
            var window=CharacterAdjustmentWindow();
            if(window==null) { CloseCharacterAdjustment(); return; }
            var origin=new Vector2((Rendering.GameScreen.Width-540)*.5f,20);
            ui.NativeTexturePanel("character-adjustment-native",new Rect(origin,new Vector2(540,435)),window.background);
            var portrait=CharacterInfoPortrait;
            if(portrait!=null) ui.NativeTexturePanel("character-adjustment-portrait",new Rect(origin+new Vector2(20,47),new Vector2(64,64)),portrait);
            // Original point-text origins; width is a local clip guard, not a recovered RECT.
            ui.NativeBitmapText("character-adjustment-name",new Rect(origin+new Vector2(50,128),new Vector2(140,16)),CharacterInfoPlayerName,Color.black,alignment:TextAnchor.UpperLeft);
            ui.NativeBitmapText("character-adjustment-job",new Rect(origin+new Vector2(50,145),new Vector2(140,16)),CharacterInfoJobName,Color.black,alignment:TextAnchor.UpperLeft);
            DrawCharacterAdjustmentMagic(origin);
            DrawCharacterAdjustmentStats(origin);
            DrawCharacterAdjustmentOptions(origin,window);
            DrawCharacterAdjustmentStatControls(origin,window);
            for(int i=0;i<window.controls.Length;i++) {
                if(i==1 || i==20) continue;
                if(HasSessionBattleMagic && (i==2 || i==3)) {
                    bool add=i==3;
                    ui.NativeImageButton(CharacterAdjustmentControlKeys[i],origin+window.controls[i].rect.position,window.controls[i],
                        ()=>{if(add)AddSelectedBattleMagic();else RemoveSelectedBattleMagic();},add?CanAddSelectedBattleMagic:CanRemoveSelectedBattleMagic,add?"선택한 습득 마법을 전투 목록에 추가":"전투 목록에서 해제 (습득 유지)");
                    continue;
                }
                if(sessionStatBaseline!=null && (i==18 || i==19)) {
                    ui.NativeImageButton(CharacterAdjustmentControlKeys[i],origin+window.controls[i].rect.position,window.controls[i],
                        ()=>ConfirmCharacterStatAllocation(),CanConfirmSessionStats,"추가 능력치 배분 확정 (이후 환불 불가)");
                    continue;
                }
                if(HasCharacterAdjustmentStatPreview && i>=4 && i<=15) continue;
                if(HasCharacterAdjustmentOptions && (i==16 || i==17)) continue;
                ui.NativeImageButton(CharacterAdjustmentControlKeys[i],
                    origin+window.controls[i].rect.position,window.controls[i],()=>{},false,"원본 기능 미연결");
            }
            ui.NativeImageButton("character-adjustment-close",origin+window.controls[1].rect.position,window.controls[1],CloseCharacterAdjustment,true,"닫기");
            ui.NativeImageButton("character-adjustment-cancel",origin+window.controls[20].rect.position,window.controls[20],CloseCharacterAdjustment,true,"취소 (변경 없이 닫기)");
            ui.Panel("character-adjustment-note-panel",new Rect(origin.x,457,540,22));
            ui.Label("character-adjustment-note",new Rect(origin.x+5,459,530,18),sessionStatBaseline!=null?"능력치·전투 마법 편집 · 확인 전 취소 · 저장 필요 · 위: 습득 / 아래: 전투 사용":HasCharacterAdjustmentStatPreview?"능력치 증감은 창 내부 미리보기입니다. 확인/저장/서버 승인은 미연결입니다.":"원본 능력조정 창 · 장비/능력 배분은 미연결이며 실제 데이터는 변경하지 않습니다.",10);
        }
        private long? characterInfoGoldValue;
        private string characterInfoGoldText;
        private CharacterPortraitCatalog characterPortraits;
        public Texture2D CharacterInfoPortrait {
            get {
                var session=GetComponent<BattleTransitionController>();
                if(session==null || !session.HasCreatedCharacter) return null;
                if(characterPortraits==null) characterPortraits=Resources.Load<CharacterPortraitCatalog>("UI/CharacterPortraits/CharacterPortraits");
                return characterPortraits!=null && characterPortraits.TryGet(session.PlayerJobId,out var image)?image:null;
            }
        }
        public string CharacterInfoPlayerName => GetComponent<BattleTransitionController>()?.PlayerName ?? "미확인";
        public string CharacterInfoJobName {
            get {
                var session = GetComponent<BattleTransitionController>();
                return session != null && session.HasCreatedCharacter && progression != null
                    && progression.TryGet(session.PlayerJobId,1,out var row) && !string.IsNullOrWhiteSpace(row.jobName) ? row.jobName : "미확인";
            }
        }
        public string CharacterInfoGoldText {
            get {
                long? gold = GetComponent<BattleTransitionController>()?.CurrentGold;
                if (characterInfoGoldText == null || characterInfoGoldValue != gold) {
                    characterInfoGoldValue = gold;
                    characterInfoGoldText = gold?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "미확인";
                }
                return characterInfoGoldText;
            }
        }
        public bool IsCharacterInfoMinimized { get; private set; }
        private NativeBattleWindows.Window CharacterInfoWindow(bool minimized)
        {
            if (minimized) {
                if (characterInfoMinimizedCatalog == null) characterInfoMinimizedCatalog = Resources.Load<NativeBattleWindows>("UI/TownCharacterInfoMinimized/NativeTownCharacterInfoMinimized");
                return characterInfoMinimizedCatalog != null ? characterInfoMinimizedCatalog.Find(6) : null;
            }
            if (characterInfoCatalog == null) characterInfoCatalog = Resources.Load<NativeBattleWindows>("UI/TownCharacterInfo/NativeTownCharacterInfo");
            return characterInfoCatalog != null ? characterInfoCatalog.Find(6) : null;
        }
        public bool SetCharacterInfoMinimized(bool minimized)
        {
            if (!IsOpen || !CanOpen || Service != TownService.CharacterInfo || IsCharacterAdjustmentOpen || IsCharacterInfoMinimized == minimized
                || CharacterInfoWindow(false) == null || CharacterInfoWindow(true) == null) return false;
            ui?.CancelItemDrags(); IsCharacterInfoMinimized = minimized; return true;
        }
        private void DrawCharacterInfo()
        {
            if(IsCharacterAdjustmentOpen) { DrawCharacterAdjustment(); return; }
            var window = CharacterInfoWindow(IsCharacterInfoMinimized);
            if (window == null) {
                ui.Panel("character-info-unavailable", new Rect(150,100,340,120), "자기 정보");
                ui.Label("character-info-error", new Rect(162,132,316,40), "원본 자기 정보 UI 리소스를 확인하세요.");
                ui.Button("character-info-error-close", new Rect(162,178,316,30), "닫기", Close); return;
            }
            if (IsCharacterInfoMinimized) {
                if (characterInfoRestoreControl == null) characterInfoRestoreControl = new NativeBattleWindows.Control {
                    rect = new Rect(0,0,34,22), frames = new[] {window.background,window.background,window.background}
                };
                ui.NativeImageButton("character-info-restore",new Vector2((Rendering.GameScreen.Width-34)*.5f,190),
                    characterInfoRestoreControl,()=>SetCharacterInfoMinimized(false),true,"자기 정보 복원 (로컬 클릭)");
                return;
            }
            var origin = new Vector2((Rendering.GameScreen.Width-335)*.5f,64);
            ui.NativeTexturePanel("character-info-native",new Rect(origin,new Vector2(335,274)),window.background);
            var portrait=CharacterInfoPortrait;
            if(portrait!=null) ui.NativeTexturePanel("character-info-portrait",new Rect(origin+new Vector2(29,46),new Vector2(64,64)),portrait);
            ui.NativeImageButton("character-info-minimize",origin+window.controls[0].rect.position,window.controls[0],
                ()=>SetCharacterInfoMinimized(true),CharacterInfoWindow(true)!=null,"최소화");
            ui.NativeImageButton("character-info-close",origin+window.controls[1].rect.position,window.controls[1],Close,true,"닫기");
            ui.NativeImageButton("character-info-extra",origin+window.controls[2].rect.position,window.controls[2],()=>OpenCharacterAdjustment(),CharacterAdjustmentWindow()!=null,"능력조정 · 추가 능력치 배분");
            ui.NativeBitmapText("character-info-name",new Rect(origin+new Vector2(146,41),new Vector2(174,16)),CharacterInfoPlayerName,Color.black,alignment:TextAnchor.UpperLeft);
            ui.NativeBitmapText("character-info-job",new Rect(origin+new Vector2(146,58),new Vector2(174,16)),CharacterInfoJobName,Color.black,alignment:TextAnchor.UpperLeft);
            ui.NativeBitmapText("character-info-gold",new Rect(origin+new Vector2(146,124),new Vector2(172,16)),CharacterInfoGoldText,Color.black,alignment:TextAnchor.UpperLeft);
            DrawCharacterInfoValues(origin);
            ui.Panel("character-info-local-note",new Rect(origin.x,346,335,54)); ui.Raise("character-info-local-note");
            ui.Label("character-info-note",new Rect(origin.x+8,350,319,46),CharacterInfoValuesAreLocal?CharacterInfoLocalSummaryText:HasCharacterInfoValues?"전달된 원본 정보의 수치입니다. 실제 능력 변경·서버 승인은 미연결입니다.":"이름·직업·돈과 원본 직업 초상입니다. 나머지 수치·추가 기능은 미연결입니다.",12);
            ui.Raise("character-info-note");
            if(CharacterInfoValuesAreLocal && CharacterInfoHiresText!=null) {
                ui.Panel("character-info-hires-panel",new Rect(origin.x,400,335,28)); ui.Raise("character-info-hires-panel");
                ui.Button("character-info-hires",new Rect(origin.x+4,402,327,24),CharacterInfoHiresText,
                    ()=>BindSessionCharacterInfoValues(),tooltip:"클릭하여 갱신 · "+CharacterInfoHiresText,singleLine:true,fontSize:12); ui.Raise("character-info-hires");
            }
            ui.Button("character-info-back",new Rect(origin.x,CharacterInfoValuesAreLocal?438:410,335,32),"시설 목록 (임시)",()=>Open(TownService.Overview,player));
        }
    }
}
