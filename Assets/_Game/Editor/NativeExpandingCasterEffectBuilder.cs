using System;
using System.IO;
using System.Linq;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeExpandingCasterEffectBuilder
    {
        const string Asset="Assets/_Game/Resources/Effects/NativeExpandingCasterEffects.asset";
        const string Image="Assets/_Game/Resources/Effects/NativeExpandingCasterEffects.png";
        [MenuItem("DarkSaver/Convert/Native Expanding Caster Effects")]
        public static void Build()
        {
            if(File.Exists(Asset))return;
            if(File.Exists(Image))throw new InvalidDataException("Existing expanding caster image is preserved.");
            int[] patterns={38,59,64},counts={3,4,3},starts={0,3,7};
            var rects=new RectInt[10];
            for(int i=0;i<3;i++)rects[i]=new RectInt(i*70,350,70,70);
            for(int i=0;i<4;i++)rects[3+i]=new RectInt(800+i*43,224,43,59);
            for(int i=0;i<3;i++)rects[7+i]=new RectInt(i*80,740,80,80);
            var magic=MagicCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/converted/Magictbl.csv"))
                .Where(x=>patterns.Contains(checked((int)x.PatternId))).OrderBy(x=>x.Id).ToArray();
            if(magic.Length!=9 || magic.Any(x=>!(x.Id>=6601 && x.Id<=6603 || x.Id>=10401 && x.Id<=10406)))throw new InvalidDataException("Unexpected expanding caster mapping.");
            var bytes=NativeThunderEffectBuilder.ReadBitmap("분석/Saver_1.2/decoded/all_arc/gameres/etc2/Fx2.bmp");
            int offset=BitConverter.ToInt32(bytes,10),width=rects.Sum(x=>x.width),height=80,dx=0;
            var pixels=new Color32[width*height];var slices=new SpriteMetaData[10];
            for(int i=0;i<rects.Length;i++)
            {
                var r=rects[i];
                for(int y=0;y<r.height;y++)for(int x=0;x<r.width;x++)
                {
                    int color=bytes[offset+(998-r.y-y)*1000+r.x+x],p=54+color*4;
                    byte b=bytes[p],g=bytes[p+1],red=bytes[p+2];
                    pixels[(height-1-y)*width+dx+x]=new Color32(red,g,b,(byte)((red|g|b)==0?0:255));
                }
                slices[i]=new SpriteMetaData{name="Expanding_"+i.ToString("D2"),rect=new Rect(dx,height-r.height,r.width,r.height),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};dx+=r.width;
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
            var entries=magic.Select(x=>{int p=Array.IndexOf(patterns,checked((int)x.PatternId));return new NativeExpandingCasterEffects.Entry(x.Id,checked((int)x.Area),sprites.Skip(starts[p]).Take(counts[p]).ToArray());}).ToArray();
            var asset=ScriptableObject.CreateInstance<NativeExpandingCasterEffects>();asset.Initialize(entries);
            AssetDatabase.CreateAsset(asset,Asset);AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
