using System;
using System.Collections.Generic;
using System.IO;
using DarkSaver.UI;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeCharacterPortraitBuilder
    {
        public const string Output="Assets/_Game/Resources/UI/CharacterPortraits/CharacterPortraits.asset";
        const string Source="분석/Saver_Late/decoded_art/gameres/";
        static int Read(byte[] data,int offset)=>BitConverter.ToInt32(data,offset);
        [MenuItem("DarkSaver/UI/Build Native Character Portraits")]
        public static void Build()
        {
            if(File.Exists(Output)) throw new InvalidOperationException("Portrait catalog already exists");
            var sources=new Dictionary<int,string>();
            for(int job=100;job<=172;job++) for(int archive=0;archive<4;archive++) {
                string path=Source+(archive==0?"face":"face"+archive)+".arc/"+job+".bmp";
                if(File.Exists(path)) { sources.Add(job,path); break; }
            }
            string directory=Path.GetDirectoryName(Output);
            var images=new List<string> {directory+"/NoFace.png"};
            foreach(var source in sources) images.Add(directory+"/"+source.Key+".png");
            foreach(string path in images) if(File.Exists(path)) throw new InvalidOperationException("Portrait image already exists: "+path);
            // Validate all inputs before writing; original archives are never loaded/executed.
            var pixels=new Dictionary<int,Color32[]>();
            foreach(var source in sources) pixels.Add(source.Key,Decode(source.Value));
            var fallback=Decode("분석/Saver_1.2/decoded/all_arc/gameres/etc/noface.bmp");
            Directory.CreateDirectory(directory);
            Save(images[0],fallback);
            foreach(var image in pixels) Save(directory+"/"+image.Key+".png",image.Value);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.StartAssetEditing();
            try {
                foreach(string path in images) {
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                    importer.mipmapEnabled=false; importer.filterMode=FilterMode.Point;
                    importer.textureCompression=TextureImporterCompression.Uncompressed; importer.isReadable=false;
                    AssetDatabase.WriteImportSettingsIfDirty(path); AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                }
            } finally { AssetDatabase.StopAssetEditing(); }
            var entries=new List<CharacterPortraitCatalog.Entry>();
            foreach(var source in sources) entries.Add(new CharacterPortraitCatalog.Entry {jobId=source.Key,image=AssetDatabase.LoadAssetAtPath<Texture2D>(directory+"/"+source.Key+".png")});
            var catalog=ScriptableObject.CreateInstance<CharacterPortraitCatalog>();
            catalog.Initialize(entries.ToArray(),AssetDatabase.LoadAssetAtPath<Texture2D>(images[0]));
            AssetDatabase.CreateAsset(catalog,Output); AssetDatabase.SaveAssets();
            Debug.Log("Built native character portraits: "+entries.Count+" plus original noface");
        }
        static void Save(string path,Color32[] pixels)
        {
            var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);
            try { texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
        static Color32[] Decode(string path)
        {
            var data=File.ReadAllBytes(path);
            if(data.Length<54 || data[0]!='B' || data[1]!='M') throw new InvalidDataException(path);
            int height=Read(data,22),bits=BitConverter.ToInt16(data,28),offset=Read(data,10),palette=14+Read(data,14);
            if(Read(data,18)!=64 || Math.Abs((long)height)!=64 || (bits!=8 && bits!=24) || Read(data,30)!=0)
                throw new InvalidDataException("Unexpected portrait format: "+path);
            int stride=((64*bits+31)/32)*4;
            if(offset<54 || (long)offset+stride*64>data.Length || (bits==8 && (palette<54 || (long)palette+1024>offset))) throw new InvalidDataException(path);
            var pixels=new Color32[4096];
            for(int y=0;y<64;y++) for(int x=0;x<64;x++) {
                int row=height>0?63-y:y,position=offset+row*stride+x*(bits/8);
                int color=bits==8?palette+data[position]*4:position;
                // Original GetFace/BltFast uses opaque copy, including black pixels.
                pixels[(63-y)*64+x]=new Color32(data[color+2],data[color+1],data[color],255);
            }
            return pixels;
        }
    }
}
