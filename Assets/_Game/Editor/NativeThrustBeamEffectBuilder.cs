using System;
using System.IO;
using System.Linq;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeThrustBeamEffectBuilder
    {
        const string Asset="Assets/_Game/Resources/Effects/NativeThrustBeamEffects.asset";
        const string Image="Assets/_Game/Resources/Effects/NativeThrustBeamEffects.png";
        [MenuItem("DarkSaver/Convert/Native Thrust Caster Beam")]
        public static void Build()=>BuildCatalog(false);
        [MenuItem("DarkSaver/Convert/Native Double Thrust Caster Beam")]
        public static void BuildDouble()=>BuildCatalog(true);
        [MenuItem("DarkSaver/Convert/Native Plasma Caster Beam")]
        public static void BuildPlasma()
        {
            const string output="Assets/_Game/Resources/Effects/NativePlasmaBeamEffects.asset";
            if(File.Exists(output))return;
            var entries=MagicCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/converted/Magictbl.csv")).Where(x=>x.PatternId==46).OrderBy(x=>x.Id).ToArray();
            if(entries.Length!=5 || entries.Where((x,i)=>x.Id!=10501+i).Any())throw new InvalidDataException("Unexpected plasma mapping.");
            var asset=ScriptableObject.CreateInstance<NativeThrustBeamEffects>();
            asset.InitializePlasma(entries.Select(x=>x.Id).ToArray(),entries.Select(x=>checked((int)x.Area*5)).ToArray(),
                Resources.Load<NativeThrustBeamEffects>("Effects/NativeThrustBeamEffects"),Resources.Load<NativeThrustBeamEffects>("Effects/NativeDoubleThrustBeamEffects"));
            AssetDatabase.CreateAsset(asset,output);AssetDatabase.SaveAssetIfDirty(asset);
        }
        static void BuildCatalog(bool doubleArms)
        {
            string outputAsset=doubleArms?"Assets/_Game/Resources/Effects/NativeDoubleThrustBeamEffects.asset":Asset;
            string outputImage=doubleArms?"Assets/_Game/Resources/Effects/NativeDoubleThrustBeamEffects.png":Image;
            if(File.Exists(outputAsset))return;
            if(File.Exists(outputImage))throw new InvalidDataException("Existing beam image is preserved.");
            var entries=MagicCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/converted/Magictbl.csv")).Where(x=>x.PatternId==(doubleArms?39:14) && x.Area>0).OrderBy(x=>x.Id).ToArray();
            if(entries.Length!=6 || entries.Where((x,i)=>x.Id!=(doubleArms?10301:10101)+i).Any())throw new InvalidDataException("Unexpected thrust beam mapping.");
            var rects=new[]{new RectInt(281,377,10,22),new RectInt(291,377,2,22),new RectInt(293,377,18,22),
                new RectInt(281,377,22,10),new RectInt(281,387,22,2),new RectInt(281,389,22,10)};
            if(doubleArms)for(int i=0;i<rects.Length;i++)rects[i]=new RectInt(rects[i].x+70,rects[i].y+30,rects[i].width,rects[i].height);
            var bytes=NativeThunderEffectBuilder.ReadBitmap("분석/Saver_1.2/decoded/all_arc/gameres/etc/Fx.bmp");
            int offset=BitConverter.ToInt32(bytes,10),width=rects.Sum(x=>x.width),height=22,dx=0;
            // Stretching is equivalent to strip repetition only while both source columns/rows match.
            for(int y=0;y<22;y++)if(bytes[offset+(998-rects[1].y-y)*1000+rects[1].x]!=bytes[offset+(998-rects[1].y-y)*1000+rects[1].x+1])throw new InvalidDataException("Horizontal beam strip is not uniform.");
            for(int x=0;x<22;x++)if(bytes[offset+(998-rects[4].y)*1000+rects[4].x+x]!=bytes[offset+(997-rects[4].y)*1000+rects[4].x+x])throw new InvalidDataException("Vertical beam strip is not uniform.");
            var pixels=new Color32[width*height];var slices=new SpriteMetaData[6];
            for(int i=0;i<6;i++)
            {
                var r=rects[i];
                for(int y=0;y<r.height;y++)for(int x=0;x<r.width;x++)
                {
                    int color=bytes[offset+(998-r.y-y)*1000+r.x+x],p=54+color*4;
                    byte b=bytes[p],g=bytes[p+1],red=bytes[p+2];
                    pixels[(height-1-y)*width+dx+x]=new Color32(red,g,b,(byte)((red|g|b)==0?0:255));
                }
                slices[i]=new SpriteMetaData{name="Beam_"+i,rect=new Rect(dx,height-r.height,r.width,r.height),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
                dx+=r.width;
            }
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
            try{texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(outputImage,texture.EncodeToPNG());}
            finally{UnityEngine.Object.DestroyImmediate(texture);}
            AssetDatabase.ImportAsset(outputImage,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(outputImage);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet=slices;
#pragma warning restore CS0618
            importer.SaveAndReimport();
            var sprites=AssetDatabase.LoadAllAssetsAtPath(outputImage).OfType<Sprite>().OrderBy(x=>x.name).ToArray();
            var asset=ScriptableObject.CreateInstance<NativeThrustBeamEffects>();
            asset.Initialize(entries.Select(x=>x.Id).ToArray(),entries.Select(x=>checked((int)x.Area*5)).ToArray(),sprites,doubleArms);
            AssetDatabase.CreateAsset(asset,outputAsset);AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
