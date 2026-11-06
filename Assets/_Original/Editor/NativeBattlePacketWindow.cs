using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DarkSaver.Original.Entry;
using DarkSaver.Original.Campaign;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Original.Editor
{
    // Explicit Editor test transport. No sample units, stats or policy are generated.
    public sealed class NativeBattlePacketWindow:EditorWindow
    {
        private LocalServerManager server;
        private string input="",message="";
        private Vector2 scroll;
        private bool existingLife=true;
        private long life=1;
        private bool knownLocalTeam;
        private int localTeam;
        private bool knownSourceTeam;
        private int sourceTeam;
        private NativeCodePage949 codec;
        private string displayedContext="";
        [MenuItem("DarkSaver/로컬 서버/원작 전투 패킷 검사")]
        public static void Open(){var window=GetWindow<NativeBattlePacketWindow>("원작 전투 패킷");window.minSize=new Vector2(520,360);}
        private void OnProjectChange(){codec=null;Repaint();}
        private void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            try{DrawContents();}finally{EditorGUILayout.EndScrollView();}
        }
        private void DrawContents()
        {
            EditorGUILayout.HelpBox("한 줄에 원본 메시지 하나를16진수로 넣으세요.50/51 여러 줄은 한 편성으로 수신합니다.0D 대상 이벤트·13 상태·15 즉시 이벤트·20 타 팀 승리 안내·31 처치 목록·53 XP·54 결과·60 팀 이름·61 종료·67 직접 효과·68 대상 재선정은 한 줄씩 지원합니다. 미리보기는 저장하지 않습니다.",MessageType.Info);
            if(server==null&&Application.isPlaying)server=UnityEngine.Object.FindAnyObjectByType<LocalServerManager>();
            server=(LocalServerManager)EditorGUILayout.ObjectField("로컬 서버",server,typeof(LocalServerManager),true);
            var view=server!=null?server.WorldSnapshot:null;
            string context=view==null?"":view.characterId+"/"+view.campaignSession;
            if(context!=displayedContext){message="";displayedContext=context;}
            EditorGUILayout.LabelField("캐릭터",view?.characterId??"Play 후 월드에 진입하세요.");
            EditorGUILayout.LabelField("맵 / 전투 revision",view==null?"—":view.record.map+" / "+(view.battle?.revision??0));
            EditorGUILayout.LabelField("팀 이름 revision",view?.battleTeamRevision.ToString(CultureInfo.InvariantCulture)??"—");
            EditorGUILayout.LabelField("처치 목록 revision",view?.resultKillRevision.ToString(CultureInfo.InvariantCulture)??"—");
            string edited=EditorGUILayout.TextArea(input,GUILayout.MinHeight(110));if(edited!=input){message="";input=edited;}
            existingLife=EditorGUILayout.Toggle("기존 개체는 현재 생명값",existingLife);
            life=EditorGUILayout.LongField("신규/수동 생명값",life);
            knownLocalTeam=EditorGUILayout.Toggle("현재 팀 식별값 확보",knownLocalTeam);
            if(knownLocalTeam)localTeam=EditorGUILayout.IntField("원본 현재 팀 ID",localTeam);
            knownSourceTeam=EditorGUILayout.Toggle("0D 대상 팀 식별값 별도 확보",knownSourceTeam);
            if(knownSourceTeam)sourceTeam=EditorGUILayout.IntField("0D 대상의 원본 팀 ID",sourceTeam);
            bool valid=TryPackets(input,out var packets,out string error);
            if(valid)
            {
                EditorGUILayout.LabelField("수신 전 미리보기",packets.Count+"개 메시지");
                foreach(var bytes in packets)Preview(bytes);
            }
            else if(input.Length>0)EditorGUILayout.HelpBox(error,MessageType.Error);
            EditorGUILayout.HelpBox("아래 버튼은 현재 Play 프로필의 실제 수신기를 호출합니다.0D·15·50/51·지휘관53·54·61과 결과표의31은 수신 상태를 저장하므로 별도 검증 프로필에서 사용하세요.0D는 현재 팀을 명시해야 하며 미지원 명령을 포함하면 이벤트 전체를 보류합니다.20·60은 세션 표시값만 바꿉니다.20은 완료·보상을 지급하지 않습니다. 미확정 수치·비용·배치는 자동 생성하지 않습니다.",MessageType.Info);
            bool needsLife=valid&&packets.Any(p=>p[1]==0x0d||p[1]==0x13||p[1]==0x50||p[1]==0x51||p[1]==0x53||p[1]==0x67||p[1]==0x68);
            using(new EditorGUI.DisabledScope(!valid||needsLife&&life<1||!Application.isPlaying||view==null))
                if(GUILayout.Button("현재 문맥으로 실제 수신기에 적용"))Apply(packets);
            if(message.Length>0)EditorGUILayout.HelpBox(message,MessageType.None);
        }
        private string Text(byte[] bytes)
        {
            if(codec==null){var asset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Original/Resources/OriginalCodePage949.json");if(asset!=null)codec=JsonUtility.FromJson<NativeCodePage949>(asset.text);}
            return codec!=null&&codec.TryCString(bytes,0,bytes.Length,out string text)?text:"[CP949 해독 불가]";
        }
        private void Preview(byte[] bytes)
        {
            if(OriginalNativeUnits.TryDecode(bytes,out var packet))
            {
                var s=packet.State();var f=s.fields;
                EditorGUILayout.HelpBox("5C "+packet.kind.ToString("X2")+" / 개체 "+packet.Unit+" / 병종 "+f[1]+" / 좌표 "+f[18]+","+f[19]+"\n표시 Lv."+s.Level+" / HP "+s.Hp+"/"+s.MaxHp+" / MP "+s.Mp+"/"+s.MaxMp+" / AP "+f[23]+"/"+f[22]+"\n공/방 "+s.Attack+"/"+s.Defense+" / XP "+s.Experience+"/"+s.NextExperience+"\n이름 "+Text(s.name)+" / 클래스 "+Text(s.className)+"\n마법 "+string.Join(",",s.Slots(false))+" / 기술 "+string.Join(",",s.Slots(true)),MessageType.None);
                if(packet.kind==0x50)EditorGUILayout.LabelField("축약50", "표시용 누락값0은 기존 계정 MP/XP 초기화가 아닙니다.");
            }
            else if(OriginalBattleExperience.TryDecode(bytes,out int unit,out uint xp))EditorGUILayout.LabelField("5C 53 절대 XP","개체 "+unit+" → "+xp);
            else if(OriginalResultKillPackets.TryDecode(bytes,out var kills))
            {
                EditorGUILayout.LabelField("5C 31 처치 병종 목록",kills.jobs.Length+"개 (배치·개체 소유권·보상 데이터 아님)");
                EditorGUILayout.HelpBox(string.Join(",",kills.jobs),MessageType.None);
            }
            else if(OriginalResultPackets.TryDecode(bytes,out var report))
            {
                EditorGUILayout.LabelField("5C 54 결과",OriginalResultPackets.Outcome(report.outcome)+" / 전리금 표시 "+report.spoils+" / 이벤트 "+report.eventCompleted+"/"+report.eventTotal);
                for(int i=0;i<report.rows.Length;i++)EditorGUILayout.LabelField("행 "+i+" / 병종 "+report.rows[i].Job,OriginalResultPackets.Growth(report.rows[i].Growth,report.rows[i].experience));
                EditorGUILayout.LabelField("표시 데이터", "이 메시지로 XP·돈·아이템·클래스를 지급하지 않습니다.");
            }
            else if(OriginalVictoryAnnouncement.TryDecode(bytes,out byte ignored,out var winner))
                EditorGUILayout.LabelField("5C 20 타 팀 승리 안내",Text(OriginalVictoryAnnouncement.CString(winner))+" / 선행 BYTE "+ignored+"(미사용). 송신 SCENECLEAR와 다른 수신 구조입니다.");
            else if(OriginalDirectedEvents.TryDecode(bytes,out int number,out int subject))
                EditorGUILayout.LabelField("5C 0D 대상 이벤트","이벤트 "+number+" / 대상 "+subject+" / 이벤트 -1은 대기열 재개, 대상 -1은 공통 명령만 실행합니다.");
            else if(OriginalCampaignScript.DecodeEvents(bytes,out var events))
                EditorGUILayout.LabelField("5C 15 동기 이벤트","번호 "+string.Join(",",events)+" / 대화·대기·개인 보상 요청 없이 원본 지도 변경만 반영합니다.");
            else if(OriginalScenarioScriptSuffix.TryDecode(bytes,out var suffix))
                EditorGUILayout.LabelField("5C 66 종료 대본 선택", "접미사 HEX "+(suffix.Length==0?"없음":((int)suffix[0]).ToString("X2"))+" / 다음 DEE 로드에 적용. 즉시 클리어·보상 없음.");
            else if(OriginalScenarioEnd.TryDecode(bytes,out byte endFlag))
                EditorGUILayout.LabelField("5C 61 종료 제어","flag "+endFlag+" / 선행54 결과표 필요. 안내→결과 확인→승리 종료 연출 순서입니다.");
            else if(OriginalBattleStatus.TryDecode(bytes,out int statusUnit,out uint flags))
                EditorGUILayout.LabelField("5C 13 상태", "개체 "+statusUnit+" / 수신 "+flags.ToString("X8")+" → 표시 "+OriginalNativeUnits.StatusFlags(flags).ToString("X8"));
            else if(bytes.Length>1&&bytes[1]==0x67&&OriginalNativeMagic.TryNotice(bytes,out var magic))
            {
                EditorGUILayout.LabelField("5C 67 직접 효과", "마법 "+magic.magic+" / 시전자 "+magic.source+" / 좌표 "+magic.x+","+magic.y);
                EditorGUILayout.LabelField("대상", string.Join(",",magic.targets));
                EditorGUILayout.LabelField("표시 데이터", "89의 같은 팀 억제는 적용하지 않습니다. 피해·MP/AP 비용은 만들지 않습니다.");
            }
            else if(OriginalBattleTeamNames.TryDecode(bytes,out var rows))
            {
                EditorGUILayout.LabelField("5C 60 팀 이름",rows.Length+"행 (전투창 검색은 앞9행)");
                for(int i=0;i<rows.Length;i++)EditorGUILayout.LabelField((i+1)+" / 팀 "+rows[i].team,Text(rows[i].name));
            }
            else if(OriginalMagicRedirect.TryDecode(bytes,out int spell,out int caster,out int x,out int y))
            {EditorGUILayout.LabelField("5C 68 대상 재선정", "마법 "+spell+" / 시전자 "+caster+" / 좌표 "+x+","+y);EditorGUILayout.LabelField("후속 동작","원본 대상 재선정→mode2 요청→효과. 비용·피해는 생성하지 않습니다.");}
        }
        private long Life(WorldView view,int unit)=>existingLife?view.battle?.Unit(unit)?.life??life:life;
        private void Apply(List<byte[]> packets)
        {
            if(!Application.isPlaying||server==null||server.WorldSnapshot==null){message="실행 중인 월드가 없습니다.";return;}
            var view=server.WorldSnapshot;WorldReply result;
            if(packets.All(p=>p[1]==0x50||p[1]==0x51))
            {
                var rows=packets.Select(p=>{OriginalNativeUnits.TryDecode(p,out var decoded);return new NativeUnitDelivery{packet=p,life=Life(view,decoded.Unit)};}).ToArray();
                result=server.Notify_BattleUnitPackets(new NativeBattleUnitsNotice{id=Guid.NewGuid().ToString("N"),character=view.characterId,session=view.campaignSession,approved=true,
                    expectedRevision=view.battle?.revision??0,expectedGrowthRevision=view.record.growth?.revision,packets=rows});
            }
            else
            {
                int unit=-1;if(packets[0][1]==0x53)OriginalBattleExperience.TryDecode(packets[0],out unit,out _);
                if(packets[0][1]==0x13)OriginalBattleStatus.TryDecode(packets[0],out unit,out _);
                if(packets[0][1]==0x0d)OriginalDirectedEvents.TryDecode(packets[0],out _,out unit);
                BattleLifeRecord[] targets=null;
                if(OriginalMagicRedirect.TryDecode(packets[0],out _,out int caster,out _,out _))
                {unit=caster;targets=(view.battle?.units??Array.Empty<TrainingUnit>()).Select(u=>new BattleLifeRecord{id=u.id,life=u.life}).ToArray();}
                if(packets[0][1]==0x67&&OriginalNativeMagic.TryNotice(packets[0],out var magic))
                {unit=magic.source;targets=magic.targets.Distinct().Select(id=>new BattleLifeRecord{id=id,life=view.battle?.Unit(id)?.life??0}).ToArray();}
                result=server.Notify_BattlePacket(new NativeBattleNotice{id=Guid.NewGuid().ToString("N"),character=view.characterId,session=view.campaignSession,approved=true,
                    expectedRevision=view.battle?.revision??0,expectedTeamRevision=view.battleTeamRevision,expectedGrowthRevision=view.record.growth?.revision,
                    expectedResultKillRevision=view.resultKillRevision,localTeam=knownLocalTeam?(int?)localTeam:null,
                    sourceTeam=packets[0][1]==0x0d&&knownSourceTeam?(int?)sourceTeam:null,
                    unitLife=unit>=0?Life(view,unit):0,targetLives=targets,packet=packets[0]});
            }
            message=result.Accepted?"수신 완료. 전투 revision "+(result.View?.battle?.revision??0)+" / 팀 이름 revision "+result.View?.battleTeamRevision:"수신 거부: "+result.Failure;
        }
        private static bool TryPackets(string text,out List<byte[]> packets,out string error)
        {
            packets=new List<byte[]>();error="메시지를 입력하세요.";
            foreach(string raw in (text??"").Split('\n'))
            {
                string row=raw.Split('#')[0].Trim();if(row.Length==0)continue;
                row=row.Replace("0x","").Replace("0X","");row=string.Concat(row.Where(c=>!char.IsWhiteSpace(c)&&c!=','));
                if(row.Length<4||row.Length%2!=0){error=(packets.Count+1)+"번째 메시지의16진수 길이가 잘못됐습니다.";return false;}
                var bytes=new byte[row.Length/2];for(int i=0;i<bytes.Length;i++)if(!byte.TryParse(row.Substring(i*2,2),NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture,out bytes[i])){error="16진수가 아닌 문자가 있습니다.";return false;}
                if(!OriginalVictoryAnnouncement.TryDecode(bytes,out _,out _)&&!OriginalDirectedEvents.TryDecode(bytes,out _,out _)&&!OriginalCampaignScript.DecodeEvents(bytes,out _)&&!OriginalScenarioEnd.TryDecode(bytes,out _)&&!OriginalMagicRedirect.TryDecode(bytes,out _,out _,out _,out _)&&!OriginalBattleStatus.TryDecode(bytes,out _,out _)&&!OriginalNativeUnits.TryDecode(bytes,out _)&&!OriginalBattleExperience.TryDecode(bytes,out _,out _)&&!OriginalResultPackets.TryDecode(bytes,out _)&&!OriginalResultKillPackets.TryDecode(bytes,out _)&&!OriginalBattleTeamNames.TryDecode(bytes,out _)&&!(bytes[1]==0x67&&OriginalNativeMagic.TryNotice(bytes,out _)))
                {error="0D/13/15/20/31/50/51/53/54/60/61/67/68 메시지의 길이·필드·문자열 범위를 확인하세요.";return false;}
                packets.Add(bytes);if(packets.Count>1000){error="한 번에 최대1,000개 개체 메시지를 지원합니다.";return false;}
            }
            if(packets.Count==0)return false;
            if(packets.Count>1&&packets.Any(p=>p[1]!=0x50&&p[1]!=0x51)){error="여러 줄 입력에는50/51 개체 메시지만 함께 넣을 수 있습니다.";return false;}
            error="";return true;
        }
    }
}
