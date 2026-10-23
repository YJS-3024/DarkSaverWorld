using System;
using System.Collections.Generic;
using System.IO;
using DarkSaver.Jobs;
using DarkSaver.UI;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeClassMenuIconBuilder
    {
        public const string Output="Assets/_Game/Resources/UI/ClassMenuIcons/ClassMenuIcons.asset";
        [MenuItem("DarkSaver/Art/Build native class menu icons")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("Editor must be idle");
            if(AssetDatabase.LoadAssetAtPath<ClassMenuIconCatalog>(Output)!=null) return;
            var source=AssetDatabase.LoadAssetAtPath<ClassArtworkCatalog>(ClassArtworkConverter.Output);
            if(source==null || source.Count!=70) throw new InvalidOperationException("Complete converted class artwork required");
            var icons=new List<ClassMenuIconCatalog.Entry>();
            var pending=new List<Sprite>();
            var catalog=ScriptableObject.CreateInstance<ClassMenuIconCatalog>();
            try {
                for(int id=100;id<=169;id++) {
                    if(!source.TryGet(id,out var row)) throw new InvalidOperationException("Missing class "+id);
                    var texture=row.GetFrame(0).texture;
                    if(texture.width!=96 || (texture.height!=128 && texture.height!=192)) throw new InvalidOperationException("Unexpected class sheet dimensions");
                    // Saver 41ACD0: top-left source rect (32,32)-(64,64), not a walking frame.
                    // ReadArcBmp2 preserves BMP dimensions; convert only the vertical coordinate origin.
                    var icon=Sprite.Create(texture,new Rect(32,texture.height-64,32,32),new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect);
                    icon.name="ClassMenu"+id; pending.Add(icon);
                    icons.Add(new ClassMenuIconCatalog.Entry {jobId=id,icon=icon});
                }
                catalog.Initialize(icons.ToArray());
                Directory.CreateDirectory(Path.GetDirectoryName(Output));
                AssetDatabase.CreateAsset(catalog,Output);
                foreach(var icon in pending) AssetDatabase.AddObjectToAsset(icon,catalog);
                EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            } finally {
                foreach(var icon in pending) if(icon!=null && !AssetDatabase.Contains(icon)) UnityEngine.Object.DestroyImmediate(icon);
                if(catalog!=null && !AssetDatabase.Contains(catalog)) UnityEngine.Object.DestroyImmediate(catalog);
            }
        }
    }
}
