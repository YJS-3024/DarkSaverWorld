using System;
using System.Globalization;
using System.Linq;
using DarkSaver.Original.Entry;
using DarkSaver.Original.World;
using UnityEditor;
using UnityEngine;
namespace DarkSaver.Original.Editor
{
    // Editor-only explicit test controls. No production XP/level/promotion policy.
    public sealed class LocalGrowthTestWindow:EditorWindow
    {
        private LocalServerManager server;
        private string amount="1000000",message="";
        private CharacterGrowthCatalog catalog;
        private int selectedJob,inspectedBaseJob;
        private OriginalCombatCatalog combatCatalog;
        private Vector2 statsScroll,windowScroll;
        private bool showLevelPreview;
        [MenuItem("DarkSaver/로컬 서버/성장·전직 테스트")]
        public static void Open()=>GetWindow<LocalGrowthTestWindow>("성장·전직 테스트");
        private void OnProjectChange(){catalog=null;combatCatalog=null;Repaint();}
        private void OnGUI()
        {
            windowScroll=EditorGUILayout.BeginScrollView(windowScroll);
            try {DrawContents();}finally{EditorGUILayout.EndScrollView();}
        }
        private void DrawContents()
        {
            EditorGUILayout.HelpBox("Play 중 현재 캐릭터에 적용하고 기존 저장 경로로 저장합니다. 별도 테스트 프로필에서 사용하세요. 경험치 지급과 테스트 전직은 원작 운영 규칙과 별개입니다.",MessageType.Info);
            if(server==null&&Application.isPlaying)server=UnityEngine.Object.FindAnyObjectByType<LocalServerManager>();
            server=(LocalServerManager)EditorGUILayout.ObjectField("로컬 서버",server,typeof(LocalServerManager),true);
            var v=server!=null?server.WorldSnapshot:null;
            if(catalog==null)
            {var asset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Original/Data/Growth/CharacterGrowth.json");if(asset!=null)catalog=JsonUtility.FromJson<CharacterGrowthCatalog>(asset.text);}
            EditorGUILayout.LabelField("대상 캐릭터",v?.characterId??"Play 후 마을에 진입하세요.");
            EditorGUILayout.LabelField("현재 직업",v?.record.growth==null?"성장 정보 없음":catalog?.Job(v.record.growth.job)?.name??v.record.growth.job.ToString());
            EditorGUILayout.LabelField("경험치",v?.record.growth?.experience.ToString(CultureInfo.InvariantCulture)??"—");
            var currentGrowth=v?.record.growth;var growthRow=currentGrowth==null?null:catalog?.Row(currentGrowth.job,currentGrowth.classLevel);
            EditorGUILayout.LabelField("원표 요구 경험치 (참고)",growthRow?.requiredExperience.ToString(CultureInfo.InvariantCulture)??"—");
            EditorGUILayout.LabelField("현재 직업의 원본 단수",currentGrowth==null?"—":catalog?.Job(currentGrowth.job)?.stage.ToString(CultureInfo.InvariantCulture)??"—");
            amount=EditorGUILayout.TextField("일괄 지급 경험치",amount);
            bool parsed=ulong.TryParse(amount,NumberStyles.None,CultureInfo.InvariantCulture,out var xp)&&xp>0;
            showLevelPreview=EditorGUILayout.Foldout(showLevelPreview,"자동 레벨 계산 비교 (읽기 전용)",true);
            if(showLevelPreview)
            {
                EditorGUILayout.HelpBox("현재 캐릭터와 입력한 경험치로 두 해석의 계산 결과를 비교합니다. 어느 방식도 게임에 활성화하거나 저장하지 않습니다. 클래스 체인지는 항상 별도 수동 처리입니다.",MessageType.Info);
                foreach(var basis in new[]{LocalExperienceBasis.SpendPerLevel,LocalExperienceBasis.Cumulative})
                {
                    string label=basis==LocalExperienceBasis.SpendPerLevel?"필요량 차감·초과 이월":"경험치 누적 유지";
                    if(parsed&&LocalLevelProgression.TryGrant(catalog,currentGrowth,xp,basis,out var after))
                        EditorGUILayout.HelpBox(label+"\n직업 "+after.job+" / 클래스 Lv."+after.classLevel+" / 전체 Lv."+after.totalLevel+" / XP "+after.experience.ToString(CultureInfo.InvariantCulture),MessageType.None);
                    else EditorGUILayout.LabelField(label,"성장 정보·원표·양수 경험치를 확인하세요.");
                }
            }
            bool ready=Application.isPlaying&&server!=null&&v!=null&&!v.moving&&v.pendingPortal==null&&v.facility==null&&v.battle==null&&v.record.campaignPhase==null&&!(v.record.map>=101&&v.record.map<=107)&&(v.presentation==null||v.presentation.completed);
            using(new EditorGUI.DisabledScope(!ready||!parsed||v?.record.growth==null))
                if(GUILayout.Button("현재 캐릭터에 경험치 일괄 지급"))Grant(xp);
            EditorGUILayout.HelpBox("경험치만 지급하며 자동 레벨업·전직·회복은 하지 않습니다. 성장 정보가 없으면 아래 테스트 초기화로 먼저 생성할 수 있습니다.",MessageType.None);
            if(catalog?.jobs!=null)
            {
                selectedJob=Mathf.Clamp(selectedJob,0,catalog.jobs.Length-1);
                selectedJob=EditorGUILayout.Popup("테스트 직업",selectedJob,catalog.jobs.Select(j=>j.name+" ["+j.id+"]").ToArray());
                using(new EditorGUI.DisabledScope(!ready))
                    if(GUILayout.Button(v?.record.growth==null?"선택 직업의 테스트 성장 정보 생성":"선택 직업으로 테스트 전직 적용"))ApplyClass(catalog.jobs[selectedJob].id);
            }
            using(new EditorGUI.DisabledScope(!ready||v?.record.growth==null||v?.record.ghost==true))
                if(GUILayout.Button("테스트 전용: 현재 지휘관 HP/MP 수동 회복"))RecoverForTest();
            EditorGUILayout.HelpBox("수동 회복은 이 Editor 버튼을 눌렀을 때만 저장됩니다. 확인된 최대 HP/MP를 사용하고 미확정이면 원본 기본표를 사용합니다. 경험치·돈·장비를 유지하며 유령 상태에서는 사용할 수 없습니다. 신전 운영 규칙과 별개입니다.",MessageType.None);
            DrawBaseStats(v);
            if(message.Length>0)EditorGUILayout.HelpBox(message,MessageType.None);
        }
        private void DrawBaseStats(WorldView view)
        {
            if(combatCatalog==null)
            {var asset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Original/Data/Combat/OriginalCombat.json");if(asset!=null)combatCatalog=JsonUtility.FromJson<OriginalCombatCatalog>(asset.text);}
            if(combatCatalog?.jobs==null||combatCatalog.jobs.Length==0)return;
            inspectedBaseJob=Mathf.Clamp(inspectedBaseJob,0,combatCatalog.jobs.Length-1);
            inspectedBaseJob=EditorGUILayout.Popup("기본표 조회 (전체 병종)",inspectedBaseJob,combatCatalog.jobs.Select(j=>j.name+" ["+j.job+"] "+j.category).ToArray());
            var job=combatCatalog.jobs[inspectedBaseJob];
            if(job?.baseStats!=null)
            {
                var b=job.baseStats;
                EditorGUILayout.Space();EditorGUILayout.LabelField("선택 직업의 원본 기본값",job.name+" ["+job.job+"]");
                EditorGUILayout.LabelField("HP / MP",b.hp+" / "+b.mp);
                EditorGUILayout.LabelField("공격 / 방어 / 마공 / 마방",b.attack+" / "+b.defense+" / "+b.magicAttack+" / "+b.magicDefense);
                EditorGUILayout.LabelField("속도 / 이동 / 명중 / 회피",b.speed+" / "+b.movement+" / "+b.accuracy+" / "+b.evasion);
                EditorGUILayout.LabelField("크리티컬 / 마명 / 마회",b.critical+" / "+b.magicAccuracy+" / "+b.magicEvasion);
                EditorGUILayout.LabelField("HP / MP 한계",b.hpLimit+" / "+b.mpLimit);
                EditorGUILayout.LabelField("공/방/마공/마방 한계",b.attackLimit+" / "+b.defenseLimit+" / "+b.magicAttackLimit+" / "+b.magicDefenseLimit);
                EditorGUILayout.LabelField("지휘범위 / 지공 / 지방",job.rawCommandRange+" / "+b.commandAttack+" / "+b.commandDefense);
                EditorGUILayout.LabelField("병종 / 특병종 / 원본 사거리",job.rawMovementType+" / "+job.rawSpecialMovementType+" / "+job.rawAttackRange);
                EditorGUILayout.LabelField("이동/공격/휴식/마법 틱",job.rawMoveTick+" / "+job.rawAttackTick+" / "+job.rawRestTick+" / "+job.rawMagicTick);
                EditorGUILayout.HelpBox("원본 -1은 미사용 값입니다. 현재 레벨·분배·장비를 계산한 최종값과 구분합니다. 이 표시는 저장을 변경하지 않습니다.",MessageType.None);
            }
            if(view?.training?.units==null)return;
            EditorGUILayout.LabelField("수련장 실제 전투 스냅샷 (읽기 전용)");
            statsScroll=EditorGUILayout.BeginScrollView(statsScroll,GUILayout.Height(180));
            foreach(var unit in view.training.units)
            {
                var name=combatCatalog?.Job(unit.job)?.name??unit.job.ToString();var stats=unit.effectiveStats;
                EditorGUILayout.LabelField("#"+unit.id+" "+name+" ("+unit.x+","+unit.y+")", "HP "+unit.hp+"/"+unit.maxHp+" MP "+unit.mp+"/"+unit.maxMp);
                EditorGUILayout.LabelField("공/방/마공/마방",stats==null?"미확인":stats.attack+" / "+stats.defense+" / "+stats.magicAttack+" / "+stats.magicDefense);
            }
            EditorGUILayout.EndScrollView();
            if(Application.isPlaying)Repaint();
        }
        private void RecoverForTest()
        {
            var v=server?.WorldSnapshot;var old=v?.record.growth;
            if(v==null||old==null||v.record.ghost||v.moving||v.pendingPortal!=null||v.facility!=null||v.battle!=null
                ||v.record.campaignPhase!=null||v.record.map>=101&&v.record.map<=107||v.presentation?.completed==false||old.revision==long.MaxValue)
            {message="마을/필드에서 유령이 아닌 지휘관만 테스트 회복할 수 있습니다.";return;}
            var b=combatCatalog?.Job(old.job)?.baseStats;
            if(!v.record.healthKnown&&b?.Valid!=true){message="원본 기본 스탯이 없습니다.";return;}
            int hp=v.record.healthKnown?v.record.maxHp:b.hp,mp=v.record.healthKnown?v.record.maxMp:Math.Max(0,b.mp);
            if(v.record.healthKnown&&v.record.hp==hp&&v.record.mp==mp){message="이미 HP/MP가 가득 차 있습니다.";return;}
            string id=Guid.NewGuid().ToString("N");var state=old.Copy();state.revision++;state.lastNotice=id;
            var reply=server.Notify_CharacterGrowth(new CharacterGrowthNotice {approved=true,id=id,character=v.characterId,session=v.growthSession,
                expectedRevision=old.revision,operation=CharacterGrowthOperation.Snapshot,state=state,health=new CharacterGrowthHealth {hp=hp,maxHp=hp,mp=mp,maxMp=mp}});
            message=reply.Accepted?"테스트 HP/MP를 회복하고 저장했습니다. XP·돈·장비는 유지했습니다.":"테스트 회복 거부: "+reply.Failure;
        }
        private void Grant(ulong xp)
        {
            var v=server.WorldSnapshot;var old=v?.record.growth;if(old==null){message="성장 정보가 없습니다.";return;}
            if(ulong.MaxValue-old.experience<xp){message="경험치 범위를 초과합니다. 지급하지 않았습니다.";return;}
            var reply=server.Notify_CharacterExperience(new CharacterExperienceNotice{character=v.characterId,session=v.growthSession,id=Guid.NewGuid().ToString("N"),approved=true,expectedRevision=old.revision,experience=old.experience+xp});
            message=reply.Accepted?"경험치를 지급하고 저장했습니다.":"지급 거부: "+reply.Failure;
        }
        private void ApplyClass(int job)
        {
            var v=server.WorldSnapshot;if(v==null)return;var old=v.record.growth;var row=catalog.Row(job,1);
            if(row==null||!row.abilitiesResolved||old?.revision==long.MaxValue){message="초기 성장표가 없거나 변경 가능한 상태가 아닙니다.";return;}
            string id=Guid.NewGuid().ToString("N");var state=old?.Copy()??new CharacterGrowthRecord();
            state.job=job;state.classLevel=1;state.totalLevel=Math.Max(1,old?.totalLevel??1);state.revision=(old?.revision??0)+1;state.lastNotice=id;
            state.magic=(int[])row.magic.Clone();state.skills=(int[])row.skills.Clone();state.nativeLevels=null;state.nextExperience=null;state.magicLevel=null;state.points=null;state.stats=null;
            var reply=server.Notify_CharacterGrowth(new CharacterGrowthNotice{character=v.characterId,session=v.growthSession,id=id,approved=true,expectedRevision=old?.revision??0,state=state,
                operation=old!=null&&old.job!=job?CharacterGrowthOperation.ClassChanged:CharacterGrowthOperation.Snapshot});
            message=reply.Accepted?"테스트 직업 상태를 저장했습니다." : "전직 거부: "+reply.Failure;
            if(reply.Accepted)message="테스트 직업·원본 초기 마법/기술을 저장했습니다. HP·MP·돈은 변경하지 않았습니다.";
        }
    }
}
