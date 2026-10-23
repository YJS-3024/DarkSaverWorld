using System;
using System.IO;
using System.Linq;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;
namespace DarkSaver.Editor
{
    public static class NativeMagicSoundBuilder
    {
        const string Output="Assets/_Game/Resources/Effects/NativeMagicSoundCatalog.asset",Folder="Assets/_Game/Art/Converted/Audio/Magic";
        public static void Build()
        {
            if(File.Exists(Output))return;
            var metadata=Resources.Load<NativeMagicPresentationCatalog>("Effects/NativeMagicPresentationCatalog");if(metadata==null)throw new InvalidDataException("Build metadata first.");
            var ids=Enumerable.Range(0,metadata.Count).SelectMany(i=>new[]{metadata.GetAt(i).Sound1,metadata.GetAt(i).Sound2}).Where(x=>x>0).Distinct().OrderBy(x=>x).ToArray();
            Directory.CreateDirectory(Folder);var clips=new AudioClip[ids.Length];
            for(int i=0;i<ids.Length;i++)
            {
                string path=Folder+"/Magic_"+ids[i].ToString("D2")+".wav";
                if(!File.Exists(path))
                {
                    var input=File.ReadAllBytes("분석/Saver_1.2/decoded/all_arc/gameres/sound/"+ids[i].ToString("D2")+".WAV");
                    byte[] pcm=null;int rate=0,channels=0,bits=0,format=0;
                    if(input.Length<12 || System.Text.Encoding.ASCII.GetString(input,0,4)!="RIFF" || System.Text.Encoding.ASCII.GetString(input,8,4)!="WAVE")throw new InvalidDataException("Invalid wave.");
                    for(int p=12;p+8<=input.Length;)
                    {
                        string tag=System.Text.Encoding.ASCII.GetString(input,p,4);int n=BitConverter.ToInt32(input,p+4),start=p+8;
                        if(n<0 || (long)start+n>input.Length)throw new InvalidDataException("Invalid wave chunk.");
                        if(tag=="fmt " && n>=16){format=BitConverter.ToUInt16(input,start);channels=BitConverter.ToUInt16(input,start+2);rate=BitConverter.ToInt32(input,start+4);bits=BitConverter.ToUInt16(input,start+14);}
                        if(tag=="data"){pcm=new byte[n];Array.Copy(input,start,pcm,0,n);}p=checked(start+n+(n&1));
                    }
                    if(format!=1 || channels!=1 || bits!=8 || rate!=22050 || pcm==null)throw new InvalidDataException("Unexpected sound encoding.");
                    // Exact unsigned8 -> signed16: no resampling, normalization or original-file copy.
                    using(var writer=new BinaryWriter(File.Create(path)))
                    {
                        void Tag(string value)=>writer.Write(System.Text.Encoding.ASCII.GetBytes(value));
                        Tag("RIFF");writer.Write(36+pcm.Length*2);Tag("WAVE");Tag("fmt ");writer.Write(16);writer.Write((short)1);writer.Write((short)channels);
                        writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);Tag("data");writer.Write(pcm.Length*2);
                        foreach(byte sample in pcm)writer.Write((short)((sample-128)*256));
                    }
                }
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);var settings=importer.defaultSampleSettings;
                settings.compressionFormat=AudioCompressionFormat.PCM;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
                importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.SaveAndReimport();clips[i]=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            var catalog=ScriptableObject.CreateInstance<NativeMagicSoundCatalog>();catalog.Initialize(ids,clips);AssetDatabase.CreateAsset(catalog,Output);AssetDatabase.SaveAssetIfDirty(catalog);
        }
    }
}
