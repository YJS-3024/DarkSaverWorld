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
        private int selectedJob;
        [MenuItem("DarkSaver/로컬 서버/성장·전직 테스트")]
        public static void Open()=>GetWindow<LocalGrowthTestWindow>("성장·전직 테스트");
        private void OnGUI()
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
            amount=EditorGUILayout.TextField("일괄 지급 경험치",amount);
            bool parsed=ulong.TryParse(amount,NumberStyles.None,CultureInfo.InvariantCulture,out var xp)&&xp>0;
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
            if(message.Length>0)EditorGUILayout.HelpBox(message,MessageType.None);
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
