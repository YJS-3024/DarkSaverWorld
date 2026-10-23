using System;
using System.Collections.Generic;
using System.IO;
using DarkSaver.Jobs;
using DarkSaver.UI;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeMercenaryInfoIconBuilder
    {
        public const string Output="Assets/_Game/Resources/UI/MercenaryInfoIcons/MercenaryInfoIcons.asset";
        [MenuItem("DarkSaver/Art/Build native mercenary info icons")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("Editor must be idle");
            var jobs=AssetDatabase.LoadAssetAtPath<JobCatalog>(JobCatalogConverter.MercenaryOutput);
            if(jobs==null)throw new InvalidOperationException("Mercenary jobs missing");
            var entries=new List<MercenaryInfoIconCatalog.Entry>();
            for(int i=0;i<jobs.Count;i++) {
                var job=jobs.GetAt(i);
                if(job.CategoryLabel!="mercenary" || (job.SpriteId!=200 && job.SpriteId!=207))continue;
                // Original top-left crop (32,32)-(64,64) is the existing second-row middle cell.
                var icon=AssetDatabase.LoadAssetAtPath<Sprite>(MercenarySpriteConverter.FramePath((int)job.SpriteId,"Down",1));
                if(icon==null)throw new InvalidOperationException("Converted mercenary frame missing: "+job.Id);
                entries.Add(new MercenaryInfoIconCatalog.Entry {jobId=job.Id,sheetId=(int)job.SpriteId,icon=icon});
            }
            var existing=AssetDatabase.LoadAssetAtPath<MercenaryInfoIconCatalog>(Output);
            if(File.Exists(Output) && existing==null)throw new InvalidOperationException("Unrecognized existing icon asset; not overwritten");
            if(existing!=null) {
                if(existing.Count!=entries.Count)throw new InvalidOperationException("Existing mercenary icon count differs; not overwritten");
                foreach(var row in entries)if(!existing.TryGet(row.jobId,out var icon) || icon!=row.icon)
                    throw new InvalidOperationException("Existing mercenary icon differs: "+row.jobId);
                return;
            }
            var catalog=ScriptableObject.CreateInstance<MercenaryInfoIconCatalog>();
            try {
                catalog.Initialize(entries.ToArray());
                Directory.CreateDirectory(Path.GetDirectoryName(Output));
                AssetDatabase.CreateAsset(catalog,Output);
                AssetDatabase.SaveAssetIfDirty(catalog);
            } finally {if(catalog!=null && !AssetDatabase.Contains(catalog))UnityEngine.Object.DestroyImmediate(catalog);}
        }
    }
}
