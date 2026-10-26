using DarkSaver.Player;
using DarkSaver.UI;
using DarkSaver.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    public sealed class TownInteractionTests
    {
        [Test]
        public void OriginalInfoHeaderUsesTwoSignedLevelsAndTwoUnsignedExperienceValues()
        {
            var payload=new byte[724]; payload[8]=0xff; payload[9]=0x7f; payload[10]=0; payload[11]=0x80;
            for(int i=12;i<16;i++) payload[i]=0xff;
            payload[19]=0x80;
            Assert.That(DarkSaver.Network.OriginalCharacterInfoValues.TryReadHeader(payload,out short first,out short second,out uint xp,out uint target),Is.True);
            Assert.That(first,Is.EqualTo(short.MinValue)); Assert.That(second,Is.EqualTo(short.MaxValue));
            Assert.That(xp,Is.EqualTo(uint.MaxValue)); Assert.That(target,Is.EqualTo(0x80000000u));
            Assert.That(DarkSaver.Network.OriginalCharacterInfoValues.TryReadHeader(new byte[732],out first,out second,out xp,out target),Is.False);
            Assert.That(first,Is.Zero); Assert.That(second,Is.Zero); Assert.That(xp,Is.Zero); Assert.That(target,Is.Zero);
        }
        [Test]
        public void CharacterAdjustmentMagicDetailPreservesOriginalRawFieldRules()
        {
            var values=new long[16]; values[0]=1;
            values[1]=34; values[2]=3; values[3]=2; values[4]=7; values[6]=34;
            var entry=new DarkSaver.Magic.MagicCatalogEntry(values,"검증 마법");
            Assert.That(TownServicesPanel.FormatCharacterAdjustmentMagicDetail(entry),Is.EqualTo("검증 마법\n마법 종류: 공격 계열\n마법력 소모: 7 거리: 3 범위: 2 "));
            values[6]=0; values[2]=0; values[3]=-1; values[4]=0;
            Assert.That(TownServicesPanel.FormatCharacterAdjustmentMagicDetail(new DarkSaver.Magic.MagicCatalogEntry(values,"보조")),Is.EqualTo("보조\n마법 종류: 보조 계열\n"));
            Assert.That(TownServicesPanel.FormatCharacterAdjustmentMagicDetail(null),Is.Null);
        }
        [Test]
        public void ClassMenuIconsReuseConvertedTexturesAndOriginalFixedCrop()
        {
            var icons=Resources.Load<ClassMenuIconCatalog>("UI/ClassMenuIcons/ClassMenuIcons");
            var source=UnityEditor.AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.ClassArtworkCatalog>("Assets/_Game/Runtime/Jobs/ClassArtworkCatalog.asset");
            Assert.That(icons,Is.Not.Null); Assert.That(icons.Count,Is.EqualTo(73));
            for(int id=100;id<=172;id++) {
                Assert.That(source.TryGet(id,out var row),Is.True);
                Assert.That(icons.TryGet(id,out var icon),Is.True);
                Assert.That(icon.texture,Is.SameAs(row.GetFrame(0).texture));
                Assert.That(icon.rect,Is.EqualTo(new Rect(32,icon.texture.height-64,32,32)));
            }
            Assert.That(icons.TryGet(-1,out _),Is.False); Assert.That(icons.TryGet(173,out _),Is.False);
            var invalid=ScriptableObject.CreateInstance<ClassMenuIconCatalog>();
            try {
                Assert.Throws<System.ArgumentException>(()=>invalid.Initialize(new[]{new ClassMenuIconCatalog.Entry {jobId=100,icon=source.TryGet(100,out var e)?e.GetFrame(3):null}}));
                icons.TryGet(100,out var icon);
                Assert.Throws<System.ArgumentException>(()=>invalid.Initialize(new[]{new ClassMenuIconCatalog.Entry {jobId=100,icon=icon},new ClassMenuIconCatalog.Entry {jobId=100,icon=icon}}));
            } finally { Object.DestroyImmediate(invalid); }
        }
        [Test]
        public void CharacterInfoReadsSessionWithoutInferringUnknownValuesOrMutatingSave()
        {
            var root = new GameObject("Character info data fixture");
            var table = ScriptableObject.CreateInstance<DarkSaver.Combat.ExperienceProgressionTable>();
            try {
                var session = root.AddComponent<DarkSaver.Combat.BattleTransitionController>();
                var panel = root.AddComponent<TownServicesPanel>();
                Assert.That(panel.CharacterInfoPlayerName, Is.EqualTo("미확인"));
                Assert.That(panel.CharacterInfoJobName, Is.EqualTo("미확인"));
                Assert.That(panel.CharacterInfoGoldText, Is.EqualTo("미확인"));
                Assert.That(panel.CharacterInfoPortrait, Is.Null);
                Assert.That(session.InitializeCharacter(new DarkSaver.Core.NativeCharacterProfile {name="정보검증",jobId=100}), Is.True);
                table.Initialize(new[] {new DarkSaver.Combat.ExperienceLevelEntry {jobId=100,level=1,jobName="파이터"}});
                panel.ConfigureProgression(table);
                Assert.That(panel.CharacterInfoPlayerName, Is.EqualTo("정보검증")); Assert.That(panel.CharacterInfoJobName, Is.EqualTo("파이터"));
                var portraits=Resources.Load<CharacterPortraitCatalog>("UI/CharacterPortraits/CharacterPortraits");
                Assert.That(portraits.TryGet(100,out var image), Is.True); Assert.That(panel.CharacterInfoPortrait, Is.SameAs(image));
                var state = session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one);
                state.hasInventory=true; state.inventory=new DarkSaver.Core.NativeInventory {gold=long.MaxValue};
                Assert.That(session.RestoreSaveState(state), Is.True);
                string before=JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one));
                string gold=panel.CharacterInfoGoldText;
                Assert.That(gold, Is.EqualTo("9223372036854775807"));
                for(int i=0;i<32;i++) Assert.That(panel.CharacterInfoGoldText, Is.SameAs(gold));
                Assert.That(JsonUtility.ToJson(session.CaptureSaveState("Kaosia_Map","Town_Runtime",Vector2.one)), Is.EqualTo(before));
                state.inventory.gold=0; Assert.That(session.RestoreSaveState(state), Is.True);
                Assert.That(panel.CharacterInfoGoldText, Is.EqualTo("0"));
                panel.ConfigureProgression(null); Assert.That(panel.CharacterInfoJobName, Is.EqualTo("미확인"));
            } finally { Object.DestroyImmediate(root); Object.DestroyImmediate(table); }
        }
        [Test]
        public void CharacterInfoLifecyclePreservesExistingServiceIdsAndReleasesModalInput()
        {
            var before = SceneManager.GetActiveScene();
            var town = SceneManager.GetSceneByName("Town_Runtime"); bool created = !town.isLoaded;
            if(created) { town=SceneManager.CreateScene("Town_Runtime"); }
            var root = new GameObject("Character info fixture"); SceneManager.MoveGameObjectToScene(root,town);
            try {
                Assert.That((int)TownService.Inventory, Is.EqualTo(5)); Assert.That((int)TownService.CharacterInfo, Is.EqualTo(6));
                var player = root.AddComponent<TopDownPlayerController>(); var panel = root.AddComponent<TownServicesPanel>();
                var session=root.AddComponent<DarkSaver.Combat.BattleTransitionController>();
                Assert.That(session.InitializeCharacter(new DarkSaver.Core.NativeCharacterProfile{name="후보검증",jobId=100}),Is.True);
                Assert.That(panel.SetCharacterInfoMinimized(true), Is.False);
                Assert.That(panel.OpenCharacterAdjustment(),Is.False);
                Assert.That(panel.Open(TownService.CharacterInfo,player), Is.True); Assert.That(player.UiInputBlocked, Is.True);
                Assert.That(panel.BindCharacterInfoWeaponName("비무장"),Is.True);
                Assert.That(panel.BindCharacterInfoWeaponName(null),Is.False); Assert.That(panel.CharacterInfoWeaponName,Is.EqualTo("비무장"));
                Assert.That(panel.BindCharacterInfoWeaponName(""),Is.True); Assert.That(panel.CharacterInfoWeaponName,Is.Empty);
                Assert.That(panel.BindCharacterInfoWeaponName("검증 무기"),Is.True);
                Assert.That(panel.HasCharacterInfoDemographics,Is.False);
                foreach(var pair in new[]{(0,"중성"),(1,"여"),(2,"남")}) {
                    Assert.That(panel.BindCharacterInfoDemographics(25,pair.Item1),Is.True);
                    Assert.That(panel.CharacterInfoAgeText,Is.EqualTo("25")); Assert.That(panel.CharacterInfoGenderText,Is.EqualTo(pair.Item2));
                }
                Assert.That(panel.BindCharacterInfoDemographics(99,-1),Is.False);
                Assert.That(panel.BindCharacterInfoDemographics(99,3),Is.False);
                Assert.That(panel.CharacterInfoAgeText,Is.EqualTo("25")); Assert.That(panel.CharacterInfoGenderText,Is.EqualTo("남"));
                var infoValues=new byte[724];
                for(int c=0;c<2;c++) for(int row=0;row<8;row++) {
                    DarkSaver.Network.OriginalCharacterInfoValues.TryGetPayloadOffset(c,row,out int offset);
                    short value=(short)(c==0?-32768+row:32767-row);
                    infoValues[offset]=(byte)value; infoValues[offset+1]=(byte)(value>>8);
                }
                infoValues[8]=7; infoValues[10]=12; infoValues[12]=0xff; infoValues[13]=0xff; infoValues[14]=0xff; infoValues[15]=0xff; infoValues[16]=9;
                infoValues[0x96]=105; infoValues[0x98]=3;
                Assert.That(panel.BindCharacterInfoValues(infoValues),Is.True);
                Assert.That(panel.CharacterInfoAuxiliaryClassId,Is.EqualTo(105)); Assert.That(panel.CharacterInfoAuxiliaryCountText,Is.EqualTo("X 3"));
                Assert.That(panel.CharacterInfoLevelText,Is.EqualTo("12(7)")); Assert.That(panel.CharacterInfoExperienceText,Is.EqualTo("4294967295/9"));
                infoValues[0x16]=0; infoValues[0x17]=0;
                Assert.That(panel.CharacterInfoValueText(0,0),Is.EqualTo("-32768"));
                Assert.That(panel.CharacterInfoValueText(1,7),Is.EqualTo("32760"));
                Assert.That(panel.BindCharacterInfoValues(new byte[732]),Is.False);
                Assert.That(panel.CharacterInfoAuxiliaryClassId,Is.EqualTo(105));
                Assert.That(panel.CharacterInfoValueText(0,0),Is.EqualTo("-32768"));
                Assert.That(panel.CharacterInfoValueText(2,0),Is.Null);
                infoValues[0x96]=infoValues[0x97]=0xff; infoValues[0x98]=infoValues[0x99]=0;
                Assert.That(panel.BindCharacterInfoValues(infoValues),Is.True);
                Assert.That(panel.CharacterInfoAuxiliaryClassId,Is.EqualTo(-1)); Assert.That(panel.CharacterInfoAuxiliaryCountText,Is.EqualTo("X 0"));
                Assert.That(panel.OpenCharacterAdjustment(),Is.True); Assert.That(panel.OpenCharacterAdjustment(),Is.False);
                var optionBytes=new byte[]{0x87,0,0,105,0,0xff,0xff,2,0,110,0,115,0};
                Assert.That(DarkSaver.Network.CharacterAdjustmentOptionsNotification.TryReadOriginalPrefix(optionBytes,out var options,out _),Is.True);
                var jobs=UnityEditor.AssetDatabase.LoadAssetAtPath<DarkSaver.Jobs.JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
                string profileBefore=JsonUtility.ToJson(session.CaptureCharacterProfile());
                Assert.That(panel.BindCharacterAdjustmentOptions(options,jobs),Is.True);
                Assert.That(panel.CharacterAdjustmentClassSlot,Is.EqualTo(1));
                Assert.That(panel.TryGetCharacterAdjustmentClass(0,out _),Is.False);
                Assert.That(panel.TryGetCharacterAdjustmentClass(1,out short classId),Is.True); Assert.That(classId,Is.EqualTo(105));
                Assert.That(panel.SelectCharacterAdjustmentClass(2),Is.True);
                Assert.That(panel.SetCharacterAdjustmentOptionIndex(1),Is.True);
                Assert.That(panel.TryGetCharacterAdjustmentClass(2,out classId),Is.True); Assert.That(classId,Is.EqualTo(115));
                Assert.That(panel.SetCharacterAdjustmentOptionIndex(2),Is.False); Assert.That(panel.SetCharacterAdjustmentOptionIndex(-1),Is.False);
                Assert.That(panel.CharacterAdjustmentOptionIndex,Is.EqualTo(1));
                Assert.That(panel.BindCharacterAdjustmentOptions(options,null),Is.False); Assert.That(panel.CharacterAdjustmentOptionIndex,Is.EqualTo(1));
                Assert.That(JsonUtility.ToJson(session.CaptureCharacterProfile()),Is.EqualTo(profileBefore));
                var magicPayload=new byte[724]; magicPayload[0x40]=1; magicPayload[0x42]=101; magicPayload[0x6a]=1; magicPayload[0x6c]=102;
                Assert.That(panel.BindCharacterAdjustmentMagicSnapshot(magicPayload),Is.True);
                magicPayload[0x14]=10; magicPayload[0x16]=12; magicPayload[0x18]=20; magicPayload[0x38]=9;
                Assert.That(panel.BindCharacterAdjustmentStatsSnapshot(magicPayload),Is.True);
                Assert.That(panel.CharacterAdjustmentStatText(0),Is.EqualTo("장착전:10/장착후:12/한계치:20"));
                Assert.That(panel.CharacterAdjustmentPointsText,Is.EqualTo("9"));
                magicPayload[0x14]=99;
                Assert.That(panel.CharacterAdjustmentStatText(0),Is.EqualTo("장착전:10/장착후:12/한계치:20"));
                Assert.That(panel.BindCharacterAdjustmentStatsSnapshot(new byte[728]),Is.False);
                Assert.That(panel.CharacterAdjustmentPointsText,Is.EqualTo("9"));
                Assert.That(panel.HasCharacterAdjustmentStatBars,Is.False);
                var barPayload=new byte[724]; for(int i=0;i<6;i++) {barPayload[0x14+i*6]=10; barPayload[0x18+i*6]=20;}
                var initialBars=(byte[])barPayload.Clone(); initialBars[0x14]=8;
                Assert.That(panel.BindCharacterAdjustmentStatBars(barPayload,initialBars),Is.True);
                Assert.That(panel.HasCharacterAdjustmentStatBars,Is.True);
                Assert.That(panel.BindCharacterAdjustmentStatBars(new byte[724],initialBars),Is.False);
                Assert.That(panel.HasCharacterAdjustmentStatBars,Is.True);
                var previewData=new byte[724];
                for(int i=0;i<6;i++) {previewData[0x14+i*6]=10; previewData[0x16+i*6]=12; previewData[0x18+i*6]=20;}
                previewData[0x38]=3; var previewBaseline=(byte[])previewData.Clone();
                var costBytes=new byte[]{0x8b,2,0,2,0,2,0,2,0,2,0,2,0};
                Assert.That(DarkSaver.Network.CharacterAdjustmentValuesNotification.TryReadOriginalPrefix(costBytes,out var costs,out _),Is.True);
                Assert.That(panel.BindCharacterAdjustmentStatPreview(previewData,previewBaseline,costs),Is.True);
                previewData[0x16]=99; previewBaseline[0x16]=99;
                for(int i=0;i<6;i++) {
                    Assert.That(panel.CanPreviewCharacterAdjustmentStat(i,DarkSaver.Network.CharacterStatAdjustmentDirection.Decrease),Is.False);
                    if(i<2) {
                        Assert.That(panel.CanPreviewCharacterAdjustmentStat(i,DarkSaver.Network.CharacterStatAdjustmentDirection.Increase),Is.False);
                        Assert.That(panel.PreviewCharacterAdjustmentStat(i,DarkSaver.Network.CharacterStatAdjustmentDirection.Increase),Is.False);
                        Assert.That(panel.CharacterAdjustmentPointsText,Is.EqualTo("3")); continue;
                    }
                    int cost=GameConstants.GetClassStatPointCost(i);
                    Assert.That(panel.PreviewCharacterAdjustmentStat(i,DarkSaver.Network.CharacterStatAdjustmentDirection.Increase),Is.True);
                    Assert.That(panel.CharacterAdjustmentStatText(i),Is.EqualTo("장착전:10/장착후:13/한계치:20"));
                    Assert.That(panel.CharacterAdjustmentPointsText,Is.EqualTo((3-cost).ToString()));
                    for(int count=1;count<3/cost;count++) Assert.That(panel.PreviewCharacterAdjustmentStat(i,DarkSaver.Network.CharacterStatAdjustmentDirection.Increase),Is.True);
                    Assert.That(panel.PreviewCharacterAdjustmentStat(i,DarkSaver.Network.CharacterStatAdjustmentDirection.Increase),Is.False);
                    for(int count=0;count<3/cost;count++) Assert.That(panel.PreviewCharacterAdjustmentStat(i,DarkSaver.Network.CharacterStatAdjustmentDirection.Decrease),Is.True);
                    Assert.That(panel.CharacterAdjustmentPointsText,Is.EqualTo("3"));
                    Assert.That(panel.PreviewCharacterAdjustmentStat(i,DarkSaver.Network.CharacterStatAdjustmentDirection.Decrease),Is.False);
                }
                Assert.That(panel.BindCharacterAdjustmentStatPreview(new byte[1],new byte[1],costs),Is.False);
                Assert.That(panel.HasCharacterAdjustmentStatPreview,Is.True);
                Assert.That(panel.PreviewCharacterAdjustmentStat(-1,DarkSaver.Network.CharacterStatAdjustmentDirection.Increase),Is.False);
                Assert.That(JsonUtility.ToJson(session.CaptureCharacterProfile()),Is.EqualTo(profileBefore));
                magicPayload[0x42]=0;
                Assert.That(panel.TryGetCharacterAdjustmentMagic(0,0,out short magicId),Is.True); Assert.That(magicId,Is.EqualTo(101));
                Assert.That(panel.SelectCharacterAdjustmentMagic(1,0),Is.True);
                Assert.That(panel.CharacterAdjustmentSelectedMagicList,Is.EqualTo(1));
                Assert.That(panel.CharacterAdjustmentMagicDetail,Is.EqualTo("미확인 마법 #102"));
                Assert.That(panel.SelectCharacterAdjustmentMagic(1,1),Is.False);
                magicPayload[0x40]=17;
                Assert.That(panel.BindCharacterAdjustmentMagicSnapshot(magicPayload),Is.False);
                Assert.That(panel.TryGetCharacterAdjustmentMagic(0,0,out magicId),Is.True); Assert.That(magicId,Is.EqualTo(101));
                Assert.That(panel.CharacterAdjustmentSelectedMagicList,Is.EqualTo(1));
                Assert.That(panel.SetCharacterInfoMinimized(true),Is.False); Assert.That(player.UiInputBlocked,Is.True);
                panel.CloseCharacterAdjustment(); Assert.That(panel.IsOpen,Is.True); Assert.That(panel.IsCharacterAdjustmentOpen,Is.False);
                Assert.That(panel.HasCharacterInfoDemographics,Is.True);
                Assert.That(panel.HasCharacterInfoValues,Is.True);
                Assert.That(panel.HasCharacterAdjustmentMagicSnapshot,Is.False); Assert.That(panel.CharacterAdjustmentSelectedMagicIndex,Is.EqualTo(-1));
                Assert.That(panel.CharacterAdjustmentMagicDetail,Is.Null);
                Assert.That(panel.HasCharacterAdjustmentStatsSnapshot,Is.False); Assert.That(panel.CharacterAdjustmentPointsText,Is.Null);
                Assert.That(panel.HasCharacterAdjustmentOptions,Is.False); Assert.That(panel.CharacterAdjustmentClassDetail,Is.Null);
                Assert.That(panel.HasCharacterAdjustmentStatBars,Is.False);
                Assert.That(panel.BindCharacterAdjustmentMagicSnapshot(new byte[724]),Is.False);
                Assert.That(panel.HasCharacterAdjustmentStatPreview,Is.False);
                Assert.That(panel.SetCharacterInfoMinimized(true), Is.True); Assert.That(panel.SetCharacterInfoMinimized(true), Is.False);
                Assert.That(panel.OpenCharacterAdjustment(),Is.False);
                Assert.That(player.UiInputBlocked, Is.True);
                Assert.That(panel.SetCharacterInfoMinimized(false), Is.True);
                Assert.That(panel.SetCharacterInfoMinimized(true), Is.True);
                Assert.That(panel.Open(TownService.Inventory,player), Is.True); Assert.That(panel.IsCharacterInfoMinimized, Is.False);
                Assert.That(panel.HasCharacterInfoValues,Is.False);
                Assert.That(panel.CharacterInfoWeaponName,Is.Null); Assert.That(panel.CharacterInfoAuxiliaryCountText,Is.Null); Assert.That(panel.CharacterInfoAuxiliaryClassId,Is.EqualTo(-1));
                Assert.That(panel.BindCharacterInfoWeaponName("검증 무기"),Is.False);
                Assert.That(panel.CharacterInfoLevelText,Is.Null); Assert.That(panel.CharacterInfoExperienceText,Is.Null);
                Assert.That(panel.HasCharacterInfoDemographics,Is.False); Assert.That(panel.CharacterInfoGenderText,Is.Null);
                Assert.That(panel.BindCharacterInfoDemographics(25,2),Is.False);
                Assert.That(panel.BindCharacterInfoValues(infoValues),Is.False);
                Assert.That(panel.SetCharacterInfoMinimized(true), Is.False);
                Assert.That(panel.Open(TownService.CharacterInfo,player), Is.True);
                Assert.That(panel.SetCharacterInfoMinimized(true), Is.True); panel.Close();
                Assert.That(player.UiInputBlocked, Is.False); Assert.That(panel.IsCharacterInfoMinimized, Is.False);
                Assert.That(panel.Open(TownService.CharacterInfo,player), Is.True); Assert.That(panel.IsCharacterInfoMinimized, Is.False);
                Assert.That(panel.OpenCharacterAdjustment(),Is.True);
                Assert.That(panel.BindCharacterAdjustmentMagicSnapshot(new byte[724]),Is.True);
                Assert.That(panel.BindCharacterAdjustmentStatsSnapshot(new byte[724]),Is.True);
                panel.enabled=false;
                // Plain EditMode NUnit does not run MonoBehaviour lifecycle messages.
                typeof(TownServicesPanel).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(panel,null);
                Assert.That(panel.IsOpen, Is.False); Assert.That(player.UiInputBlocked, Is.False);
                Assert.That(panel.IsCharacterAdjustmentOpen,Is.False);
                Assert.That(panel.HasCharacterAdjustmentMagicSnapshot,Is.False);
                Assert.That(panel.HasCharacterAdjustmentStatsSnapshot,Is.False);
            } finally {
                Object.DestroyImmediate(root);
                if(created) EditorSceneManager.CloseScene(town,true);
                if(before.IsValid() && before.isLoaded) SceneManager.SetActiveScene(before);
            }
        }
        [Test]
        public void TownFacilitiesDialogueDistanceAndModalLifecycleWorkTogether()
        {
            var before = SceneManager.GetActiveScene();
            var town = SceneManager.GetSceneByName("Town_Runtime");
            bool createdTown = !town.isLoaded;
            if (createdTown)
            {
                town = SceneManager.CreateScene("Town_Runtime");
            }
            var root = new GameObject("Town interaction test"); SceneManager.MoveGameObjectToScene(root, town);
            var actor = new GameObject("Test player"); actor.transform.SetParent(root.transform);
            var npcObject = new GameObject("Test NPC"); npcObject.transform.SetParent(root.transform);
            try
            {
                var player = actor.AddComponent<TopDownPlayerController>();
                var panel = root.AddComponent<TownServicesPanel>();
                Assert.That(panel.Open(TownService.Inventory, player), Is.True);
                Assert.That(player.UiInputBlocked, Is.True);
                Assert.That(player.TryMoveToWorld(Vector2.one), Is.False);
                foreach (var service in new[] {TownService.Shop, TownService.ClassChange, TownService.Mercenary})
                { Assert.That(panel.Open(service, player), Is.True); Assert.That(panel.Service, Is.EqualTo(service)); }
                // UI state belongs to one facility visit, not the next transaction.
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(TownServicesPanel).GetField("shopQuantity", flags).SetValue(panel, "99");
                typeof(TownServicesPanel).GetField("shopPage", flags).SetValue(panel, 7);
                typeof(TownServicesPanel).GetField("selectedInventoryId", flags).SetValue(panel, 123);
                typeof(TownServicesPanel).GetField("pendingClassId", flags).SetValue(panel, 105);
                Assert.That(panel.Open((TownService)999, player), Is.False);
                Assert.That(typeof(TownServicesPanel).GetField("shopQuantity", flags).GetValue(panel), Is.EqualTo("99"));
                Assert.That(panel.Open(TownService.Mercenary, player), Is.True);
                Assert.That(typeof(TownServicesPanel).GetField("shopQuantity", flags).GetValue(panel), Is.EqualTo("1"));
                foreach (string field in new[] { "shopPage", "selectedInventoryId", "pendingClassId" })
                    Assert.That(typeof(TownServicesPanel).GetField(field, flags).GetValue(panel), Is.EqualTo(field == "selectedInventoryId" ? -1 : 0));
                Assert.That(panel.Open((TownService)999, player), Is.False);
                Assert.That(panel.OpenDialogue("", new[] {"line"}, player), Is.False);
                Assert.That(panel.Service, Is.EqualTo(TownService.Mercenary));
                panel.Close(); Assert.That(player.UiInputBlocked, Is.False);

                var formation = root.AddComponent<BattleFormationPanel>();
                formation.Configure(null, 0, 0, null, player);
                Assert.That(formation.ReturnToTown(), Is.False);
                Assert.That(formation.Open(), Is.True);
                panel.enabled = false;
                Assert.That(formation.CanReturnToTown, Is.False);
                Assert.That(formation.ReturnToTown(), Is.False);
                Assert.That(formation.IsOpen, Is.True);
                Assert.That(player.UiInputBlocked, Is.True);
                panel.enabled = true;
                Assert.That(formation.ReturnToTown(), Is.True);
                Assert.That(formation.IsOpen, Is.False);
                Assert.That(panel.IsOpen, Is.False);
                Assert.That(player.UiInputBlocked, Is.False);
                Assert.That(formation.ConfirmedSnapshot, Is.Null);
                panel.Close(); Assert.That(player.UiInputBlocked, Is.False);

                var npc = npcObject.AddComponent<TownNpcInteraction>();
                var lines = new[] {"첫 대화", "두 번째 대화"}; npc.Configure("검증 NPC", lines); lines[0] = "외부 변경";
                npcObject.transform.position = new Vector3(GameConstants.TownNpcInteractionDistance + 1, 0);
                Assert.That(npc.TryInteract(player, panel), Is.False);
                npcObject.transform.position = Vector3.zero;
                Assert.That(npc.TryInteract(player, panel), Is.True);
                Assert.That(panel.Speaker, Is.EqualTo("검증 NPC")); Assert.That(panel.CurrentLine, Is.EqualTo("첫 대화"));
                Assert.That(npc.TryInteract(player, panel), Is.False);
                Assert.That(panel.NextLine(), Is.True); Assert.That(panel.CurrentLine, Is.EqualTo("두 번째 대화"));
                Assert.That(panel.NextLine(), Is.True); Assert.That(panel.IsOpen, Is.False);
                Assert.That(player.UiInputBlocked, Is.False);
                player.SetUiInputBlocked(true);
                Assert.That(panel.Open(TownService.Inventory, player), Is.False); // Do not steal another system's lock.
                player.SetUiInputBlocked(false);
                Assert.That(panel.Open(TownService.Inventory, player), Is.True);
                panel.enabled = false;
                // Direct EditMode invocation does not dispatch PlayMode lifecycle messages.
                typeof(TownServicesPanel).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(panel, null);
                Assert.That(player.UiInputBlocked, Is.False);
                panel.enabled = true;
                formation.Configure(null, 0, 0, null, player);
                Assert.That(panel.Open(TownService.Inventory, player), Is.True);
                Assert.That(formation.Open(), Is.True); Assert.That(panel.IsOpen, Is.False);
                formation.Cancel(); Assert.That(player.UiInputBlocked, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (createdTown) EditorSceneManager.CloseScene(town, true);
                if (before.IsValid() && before.isLoaded) SceneManager.SetActiveScene(before);
            }
        }

        [Test]
        public void DisabledFacilitiesRejectWithoutAcquiringInputLock()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Outside town test"); SceneManager.MoveGameObjectToScene(root, scene);
                var player = root.AddComponent<TopDownPlayerController>(); var panel = root.AddComponent<TownServicesPanel>();
                panel.enabled = false;
                Assert.That(panel.Open(TownService.Inventory, player), Is.False);
                Assert.That(player.UiInputBlocked, Is.False);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
