using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeMagicPresentationCatalogBuilder
    {
        const string Output="Assets/_Game/Resources/Effects/NativeMagicPresentationCatalog.asset";
        [MenuItem("DarkSaver/Convert/Native Magic Presentation Metadata")]
        public static void Build()
        {

            var patterns=new Dictionary<int,int[]>();
            foreach(var line in File.ReadLines("분석/Saver_1.2/converted/MagicPtn.csv"))
            {
                var m=Regex.Matches(line,"\"([^\"]*)\"");if(m.Count==0 || !int.TryParse(m[0].Groups[1].Value,out int id))continue;
                if(m.Count!=19)throw new InvalidDataException("Invalid magic pattern columns: "+id);
                var n=new int[19];for(int i=0;i<19;i++)if(i!=1 && !int.TryParse(m[i].Groups[1].Value,out n[i]))throw new InvalidDataException("Invalid magic pattern integer: "+id+"/"+i);patterns.Add(id,n);
            }
            var entries=new List<NativeMagicPresentationCatalog.Entry>();
            foreach(var e in MagicCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/converted/Magictbl.csv")))
            {
                if(!patterns.TryGetValue(checked((int)e.PatternId),out var p))throw new InvalidDataException("Missing magic pattern "+e.Id);
                entries.Add(new NativeMagicPresentationCatalog.Entry(e.Id,p[0],p[16],checked((int)e.Area),p[8],p[15],p[17],p[18]));
            }
            var asset=AssetDatabase.LoadAssetAtPath<NativeMagicPresentationCatalog>(Output);
            if(asset==null)
            {if(File.Exists(Output))throw new InvalidDataException("Existing magic metadata has an unexpected type.");asset=ScriptableObject.CreateInstance<NativeMagicPresentationCatalog>();asset.Initialize(entries.ToArray());AssetDatabase.CreateAsset(asset,Output);}
            else {asset.Initialize(entries.ToArray());EditorUtility.SetDirty(asset);}
            AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
