using UnityEditor;
using UnityEngine;
using DarkSaver.Original.Entry;

namespace DarkSaver.Original.Editor
{
    public sealed class LocalEntryAccountWindow : EditorWindow
    {
        private string accountId = "", password = "", confirmation = "", message = "", resetBackup="";
        [MenuItem("DarkSaver/로컬 서버/계정 관리")]
        public static void Open() => GetWindow<LocalEntryAccountWindow>("로컬 서버 계정");
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("개발용 로컬 계정 등록·캐릭터 초기화 도구입니다. 원작 회원가입 화면과 별개입니다.", MessageType.Info);
            accountId = EditorGUILayout.TextField("아이디", accountId);
            password = EditorGUILayout.PasswordField("비밀번호", password);
            confirmation = EditorGUILayout.PasswordField("비밀번호 확인", confirmation);
            using (new EditorGUI.DisabledScope(password != confirmation || !LocalEntryPolicy.IsInput(accountId) || !LocalEntryPolicy.IsInput(password)))
                if (GUILayout.Button("로컬 계정 등록"))
                {
                    var service = new EntryAccountService(new LocalAccountFileStore(LocalServerManager.AccountPath));
                    message = service.ProvisionLocalAccount(accountId, password) ? "등록했습니다. Start 씬에서 로그인할 수 있습니다." : "등록하지 못했습니다. 중복 아이디 또는 저장 파일 상태를 확인하세요.";
                    password = confirmation = "";
                }
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("초기화는 계정 ID·비밀번호를 유지하고 캐릭터·튜토리얼·장비·경험치·진행을 제거합니다. 실행 전 같은 폴더에 백업합니다. Play를 종료한 상태에서 사용하세요.",MessageType.Warning);
            using(new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if(GUILayout.Button("로그인 생략 프로필의 캐릭터 초기화"))ResetCharacter(LocalServerManager.LocalProfilePath,LocalEntryPolicy.LocalProfileAccountId);
                using(new EditorGUI.DisabledScope(!LocalEntryPolicy.IsInput(accountId)))
                    if(GUILayout.Button("입력한 계정 ID의 캐릭터 초기화"))ResetCharacter(LocalServerManager.AccountPath,accountId);
            }
            if (message.Length > 0) EditorGUILayout.HelpBox(message, MessageType.None);
            if(resetBackup.Length>0)EditorGUILayout.LabelField("초기화 전 백업",resetBackup,EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("저장 위치", LocalServerManager.AccountPath, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("로그인 생략 프로필",LocalServerManager.LocalProfilePath,EditorStyles.wordWrappedLabel);
        }
        private void ResetCharacter(string path,string id)
        {
            if(Application.isPlaying){message="Play를 종료한 뒤 초기화하세요.";return;}
            if(!EditorUtility.DisplayDialog("캐릭터 초기화",id+"의 캐릭터와 모든 진행을 초기화할까요? 계정과 비밀번호는 유지하고 백업을 남깁니다.","백업 후 초기화","취소"))return;
            LocalCharacterReset.TryReset(path,id,out message,out var backup);resetBackup=backup??"";
            password=confirmation="";
        }
    }
}
