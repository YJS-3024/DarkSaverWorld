using System;
using System.IO;
using System.Linq;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeSalamanderEffectBuilder
    {
        const string Asset="Assets/_Game/Resources/Effects/NativeSalamanderEffects.asset";
        const string Image="Assets/_Game/Resources/Effects/NativeSalamanderEffects.png";
        [MenuItem("DarkSaver/Convert/Native Salamander Caster")]
        public static void Build()
        {
            if(File.Exists(Asset))return;
            if(File.Exists(Image))throw new InvalidDataException("Existing salamander image is preserved.");
            var entries=MagicCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/converted/Magictbl.csv")).Where(x=>x.PatternId==47).OrderBy(x=>x.Id).ToArray();
            if(entries.Length!=6 || entries.Where((x,i)=>x.Id!=10701+i).Any())throw new InvalidDataException("Unexpected salamander mapping.");
            var trail=Resources.Load<NativeCrossTrustEffects>("Effects/NativeCrossTrustEffects");
            if(trail==null)throw new InvalidDataException("Convert the shared cross-trust flames first.");
            var rects=new[]{new RectInt(618,648,69,72),new RectInt(687,648,69,72),new RectInt(756,648,69,72),
                new RectInt(825,648,69,72),new RectInt(618,576,69,72),new RectInt(687,576,69,72)};
            var bytes=NativeThunderEffectBuilder.ReadBitmap("분석/Saver_1.2/decoded/all_arc/gameres/etc2/Fx2.bmp");
            int offset=BitConverter.ToInt32(bytes,10),width=414,height=72;
            var pixels=new Color32[width*height];var slices=new SpriteMetaData[6];
            for(int i=0;i<6;i++)
            {
                var r=rects[i];
                for(int y=0;y<72;y++)for(int x=0;x<69;x++)
                {
                    int color=bytes[offset+(998-r.y-y)*1000+r.x+x],p=54+color*4;
                    byte b=bytes[p],g=bytes[p+1],red=bytes[p+2];
                    pixels[(height-1-y)*width+i*69+x]=new Color32(red,g,b,(byte)((red|g|b)==0?0:255));
                }
                slices[i]=new SpriteMetaData{name="Salamander_"+i,rect=new Rect(i*69,0,69,72),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
            }
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
            try{texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(Image,texture.EncodeToPNG());}
            finally{UnityEngine.Object.DestroyImmediate(texture);}
            AssetDatabase.ImportAsset(Image,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(Image);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
#pragma warning disable CS0618
            importer.spritesheet=slices;
#pragma warning restore CS0618
            importer.SaveAndReimport();
            var sprites=AssetDatabase.LoadAllAssetsAtPath(Image).OfType<Sprite>().OrderBy(x=>x.name).ToArray();
            var asset=ScriptableObject.CreateInstance<NativeSalamanderEffects>();
            asset.Initialize(entries.Select(x=>x.Id).ToArray(),entries.Select(x=>checked((int)x.Area*2)).ToArray(),Enumerable.Range(0,3).Select(trail.Frame).ToArray(),sprites);
            AssetDatabase.CreateAsset(asset,Asset);AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
