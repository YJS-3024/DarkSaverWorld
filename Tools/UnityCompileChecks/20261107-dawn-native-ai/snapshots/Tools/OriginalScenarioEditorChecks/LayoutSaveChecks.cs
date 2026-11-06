using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DarkSaver.Original.Editor.Scenarios;
using DarkSaver.Original.Entry;
namespace UnityEngine
{
    public class ScriptableObject {public static T CreateInstance<T>()where T:new()=>new T();}
    public class TextAsset {public string text;}
}
namespace UnityEditor
{
    internal enum ImportAssetOptions {ForceUpdate}
    internal static class EditorUtility {public static void SetDirty(object value){}}
    internal static class AssetDatabase
    {
        public static string Failure;private static readonly Dictionary<string,object> assets=new Dictionary<string,object>();
        public static T LoadAssetAtPath<T>(string path)where T:class
        {if(typeof(T)==typeof(UnityEngine.TextAsset))return File.Exists(path)?new UnityEngine.TextAsset{text=File.ReadAllText(path)}as T:null;return assets.TryGetValue(path,out var value)?value as T:null;}
        public static void CreateAsset(object value,string path){assets[path]=value;}
        public static void ImportAsset(string path,ImportAssetOptions options){if(Failure=="import")throw new IOException("fixture import failure");}
        public static void SaveAssets(){if(Failure=="catalog")throw new IOException("fixture catalog failure");}
        public static void Refresh(){if(Failure=="refresh")throw new IOException("fixture refresh failure");}
    }
}
internal sealed partial class LayoutSaveHost
{
    public string BasePath,LayoutFolder,DecodedFolder,OriginalRosterPath,mapKey="101",savedHash,loadError="",message="";
    public bool dirty=true,dirtyGateways,hasUnsavedChanges=true,layoutReadOnly;
    public ScenarioSource source;public ScenarioLayout document;
    private readonly Dictionary<int,int> jobs=new Dictionary<int,int>{{401,401}};
    private string native;
    private sealed class MapReference {public string nativePath;}
    private MapReference MapInfo(string key)=>new MapReference{nativePath=native};
    private string ReadOptional(string path)=>File.Exists(path)?File.ReadAllText(path):null;
    public LayoutSaveHost(string root)
    {
        BasePath=root+"/";LayoutFolder=root+"/layouts";DecodedFolder=root+"/decoded/";OriginalRosterPath=root+"/absent-roster.json";
        Directory.CreateDirectory(root);native=root+"/native.bytes";File.WriteAllBytes(native,new byte[]{1,2,3});
        source=ScenarioLayoutData.ReadSource(mapKey,20,15,File.ReadAllBytes(native),null,null,null,null);
        document=source.layout.Copy();document.units=new[]{new ScenarioPlacement{unit=400,job=401,x=2,y=2}};
    }
    public bool Run()=>Save();
}
internal static class LayoutSaveChecks
{
    public static int Run()
    {
        int checks=0;void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
        string root=Path.Combine(Path.GetTempPath(),"ds-layout-save-"+Guid.NewGuid().ToString("N"));
        try
        {
            foreach(string failure in new[]{"copy","import","catalog"})
            {
                var host=new LayoutSaveHost(Path.Combine(root,failure));string export=host.BasePath+"Resources/TrainingTestLayouts/101.json";
                if(failure=="copy")Directory.CreateDirectory(export);else UnityEditor.AssetDatabase.Failure=failure;
                Check(!host.Run(),"export failure is reported "+failure);
                var saved=File.ReadAllBytes(Path.Combine(host.LayoutFolder,"101.json"));
                Check(host.savedHash==ScenarioLayoutData.Hash(saved)&&ScenarioLayoutData.Decode(saved).units.Length==1,"successful editor save is retained on export failure "+failure);
                Check(host.dirty&&host.hasUnsavedChanges,"partial training export keeps retry marker "+failure);
                Check(host.message.Contains("편집 배치")&&host.message.Contains("내보내기"),"partial save distinguishes disk and Player export "+failure);
                if(failure=="copy")Directory.Delete(export);UnityEditor.AssetDatabase.Failure=null;
                Check(host.Run()&&!host.dirty&&!host.hasUnsavedChanges,"retry exports using updated disk hash "+failure);
                Check(File.ReadAllBytes(export).AsSpan().SequenceEqual(saved),"Player export matches saved bytes "+failure);
                var catalog=UnityEditor.AssetDatabase.LoadAssetAtPath<TrainingTestLayoutCatalog>(host.BasePath+"Resources/TrainingTestLayoutCatalog.asset");
                Check(catalog.Find(101)?.text==File.ReadAllText(export),"catalog points to exported training asset "+failure);
            }
            var refresh=new LayoutSaveHost(Path.Combine(root,"refresh")){dirtyGateways=true};UnityEditor.AssetDatabase.Failure="refresh";
            Check(refresh.Run()&&!refresh.dirty&&refresh.hasUnsavedChanges,"refresh-only failure preserves gateway edits and reports successful disk save");
            Check(refresh.message.Contains("새로고침"),"refresh-only warning remains visible");UnityEditor.AssetDatabase.Failure=null;
            var external=new LayoutSaveHost(Path.Combine(root,"external"));Directory.CreateDirectory(external.LayoutFolder);string externalPath=Path.Combine(external.LayoutFolder,"101.json");File.WriteAllText(externalPath,"external");
            Check(!external.Run()&&File.ReadAllText(externalPath)=="external"&&external.dirty,"primary external file guard preserves user data");
        }
        finally {UnityEditor.AssetDatabase.Failure=null;if(Directory.Exists(root))Directory.Delete(root,true);}
        return checks;
    }
}
