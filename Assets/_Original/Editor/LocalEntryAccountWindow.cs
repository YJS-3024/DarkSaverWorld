using UnityEditor;
using UnityEngine;
using DarkSaver.Original.Entry;

namespace DarkSaver.Original.Editor
{
    public sealed class LocalEntryAccountWindow : EditorWindow
    {
        private string accountId = "", password = "", confirmation = "", message = "";
        [MenuItem("DarkSaver/로컬 서버/계정 관리")]
        public static void Open() => GetWindow<LocalEntryAccountWindow>("로컬 서버 계정");
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("로컬 서버 검증 계정 등록입니다. 원작의 웹 회원가입 화면을 재현한 창이 아닙니다. 이전 게임의 계정·저장 파일은 읽거나 수정하지 않습니다.", MessageType.Info);
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
            if (message.Length > 0) EditorGUILayout.HelpBox(message, MessageType.None);
            EditorGUILayout.LabelField("저장 위치", LocalServerManager.AccountPath, EditorStyles.wordWrappedLabel);
        }
    }
}
