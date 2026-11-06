using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Editor.Scenarios
{
    public sealed partial class ScenarioEditorWindow
    {
        private const string AiCatalogPath=BasePath+"Data/Campaign/NativeAiRules.json";
        [SerializeField] private bool showNativeAi;
        private string aiMapKey="",aiError="",aiSource="";
        private long aiStamp=long.MinValue,aiSize=-1;
        private NativeAiTextCatalog aiCatalog;
        private int[][] aiRows=Array.Empty<int[]>();
        private int aiRowIndex,aiLastSelected=-1;
        private long aiMagicStamp=long.MinValue,aiMagicSize=-1;
        private int[] aiMagicIds;
        private string aiMagicError="";
        private void ReadAiMagicCatalog()
        {
            try
            {
                var file=new FileInfo(BasePath+"Data/Combat/MagicMenu.json");
                long stamp=file.Exists?file.LastWriteTimeUtc.Ticks:0,size=file.Exists?file.Length:0;
                if(stamp==aiMagicStamp&&size==aiMagicSize)return;
                aiMagicStamp=stamp;aiMagicSize=size;aiMagicIds=null;aiMagicError="";
                var catalog=file.Exists?JsonUtility.FromJson<MagicMenuCatalog>(File.ReadAllText(file.FullName)):null;
                if(catalog?.entries==null)aiMagicError="마법표를 읽지 못해 원문 AI의 마법 참조를 확인할 수 없습니다.";
                else aiMagicIds=catalog.entries.Where(r=>r!=null).Select(r=>r.id).ToArray();
            }
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException)
            {aiMagicIds=null;aiMagicError="AI 마법 참조 확인 실패: "+e.Message;}
        }
        private void DrawAiMagicReferences()
        {
            ReadAiMagicCatalog();
            if(aiMagicError.Length>0){EditorGUILayout.HelpBox(aiMagicError,MessageType.Warning);return;}
            var missing=aiRows.SelectMany(row=>Enumerable.Range(0,Math.Max(0,Math.Min(NativeAiRuleTable.MaximumActions,row[7])))
                .Where(i=>row[9+i*3]==2&&!aiMagicIds.Contains(row[10+i*3]))
                .Select(i=>"unit "+row[0]+": MAGIC "+row[10+i*3])).Distinct().ToArray();
            if(missing.Length>0)EditorGUILayout.HelpBox("원문 AI가 마법표에 없는 번호를 참조합니다: "+string.Join(", ",missing)+". 원문은 보존하며 다른 마법으로 자동 치환하지 않습니다.",MessageType.Warning);
        }
        private void ReadNativeAi()
        {
            var info=new FileInfo(AiCatalogPath);long stamp=info.Exists?info.LastWriteTimeUtc.Ticks:0,size=info.Exists?info.Length:0;
            if(aiMapKey==mapKey&&aiStamp==stamp&&aiSize==size)return;
            try
            {
                if(aiStamp!=stamp||aiSize!=size)aiCatalog=info.Exists?JsonUtility.FromJson<NativeAiTextCatalog>(File.ReadAllText(AiCatalogPath)):null;
                aiSource=aiCatalog?.Source(mapKey);aiRows=Array.Empty<int[]>();aiError="";aiRowIndex=0;aiLastSelected=-1;
                if(aiSource==null)aiError=info.Exists?"이 맵의 원본 AI 파일이 확보되지 않았습니다.":"원본 AI 카탈로그가 연결되지 않았습니다.";
                else if(!NativeAiRuleTable.TryLoad(aiSource,null,out var table,out var error))aiError=error;
                else aiRows=Enumerable.Range(0,NativeAiRuleTable.MaximumRows).Select(table.Row).Where(r=>r[0]>=0&&r[0]!=999).ToArray();
            }
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException)
            {aiRows=Array.Empty<int[]>();aiError="원본 AI 읽기 실패: "+e.Message;}
            aiMapKey=mapKey;aiStamp=stamp;aiSize=size;
        }
        private static string AiOrder(int mode)=>mode==2?"ODNORMAL (2)":mode==4?"ODATTACK (4)":mode==8?"ODDEFENCE (8)":"상태 "+mode;
        private static string AiRegion(int[] row)=>row[2]==0?"AREA("+row[3]+", "+row[4]+", "+row[5]+")":row[2]==1?"SOILDER("+row[6]+")":"활동 영역 미지정";
        private static string AiActions(int[] row)
        {
            return string.Join(" · ",Enumerable.Range(0,Math.Max(0,Math.Min(NativeAiRuleTable.MaximumActions,row[7]))).Select(i=>
            {int at=8+i*3;return(row[at+1]==0?"REST":row[at+1]==1?"ATTACK":row[at+1]==2?"MAGIC "+row[at+2]:"동작 코드 "+row[at+1])+" [가중치 "+row[at]+"]";}));
        }
        private void DrawNativeAiRules()
        {
            GUILayout.Space(5);showNativeAi=EditorGUILayout.Foldout(showNativeAi,"원본 AI 규칙",true);if(!showNativeAi)return;
            ReadNativeAi();
            EditorGUILayout.HelpBox("초기 위치는 SET/POS·사용자 배치에서 설정합니다. 아래 AREA는 이동 판단의 활동 영역입니다. 최초 파일 로드 결과를 읽기 전용으로 표시합니다.",MessageType.Info);
            if(aiError.Length>0){EditorGUILayout.HelpBox(aiError,MessageType.Warning);return;}
            if(aiRows.Length==0){EditorGUILayout.LabelField("파일은 있으나 유효 AI 행은0개입니다.");return;}
            DrawAiMagicReferences();
            if(selected!=aiLastSelected)
            {
                aiLastSelected=selected;int head=selected<0?-1:selected/10*10;int index=Array.FindIndex(aiRows,r=>r[0]==head);if(index>=0)aiRowIndex=index;
            }
            aiRowIndex=EditorGUILayout.Popup("unit 규칙 "+aiRows.Length+"행",Math.Max(0,Math.Min(aiRowIndex,aiRows.Length-1)),aiRows.Select(r=>"unit "+r[0]+" · "+AiOrder(r[1])).ToArray());
            var row=aiRows[aiRowIndex];EditorGUILayout.LabelField("활동 영역",AiRegion(row));
            EditorGUILayout.HelpBox(row[7]==0?"일반 행동 목록이 비어 있습니다.":AiActions(row),MessageType.None);
            EditorGUILayout.LabelField("선택 기준","rand % 100 ≤ 누적 가중치",EditorStyles.miniLabel);
            if(selected>=0)EditorGUILayout.LabelField("선택 개체",selected%10==0?"unit "+selected+" · 지휘관 AI":"unit "+selected+" · 지휘관 "+selected/10*10+"의 용병 AI",EditorStyles.miniLabel);
            using(new EditorGUI.DisabledScope(row[2]!=0||row[3]<0||row[4]<0||row[3]>=document.width||row[4]>=document.height))
                if(GUILayout.Button("AREA 중심 보기"))FocusTile(row[3],row[4]);
            if(GUILayout.Button("원본 AI 텍스트 선택"))
            {
                var asset=AssetDatabase.LoadAssetAtPath<TextAsset>(DecodedFolder+mapKey+"set.arc/"+mapKey+".ai.txt");
                if(asset!=null){Selection.activeObject=asset;EditorGUIUtility.PingObject(asset);}
            }
        }
    }
}
