using System;
using System.IO;
using System.Linq;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeLevelUpCircleEffectBuilder
    {
        const string Asset="Assets/_Game/Resources/Effects/NativeLevelUpCircleEffects.asset";
        const string Image="Assets/_Game/Resources/Effects/NativeLevelUpCircleEffects.png";
        [MenuItem("DarkSaver/Convert/Native Level Up Ground Circle")]
        public static void Build()
        {
            if(File.Exists(Asset))return;
            if(File.Exists(Image))throw new InvalidDataException("Existing circle image is preserved.");
            var entries=MagicCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/converted/Magictbl.csv")).Where(x=>x.PatternId==45).ToArray();
            if(entries.Length!=1 || entries[0].Id!=4001)throw new InvalidDataException("Unexpected level-up circle mapping.");
            var rects=new[]{new RectInt(0,560,72,23),new RectInt(72,560,26,51),new RectInt(98,560,83,34),new RectInt(181,560,86,34),
                new RectInt(267,560,26,51),new RectInt(293,560,73,22),new RectInt(360,560,144,5),new RectInt(366,565,75,81),
                new RectInt(441,565,71,83),new RectInt(366,648,159,6),new RectInt(513,565,84,72),new RectInt(366,654,82,73)};
            var destinations=new[]{new Vector2Int(367,231),new Vector2Int(431,253),new Vector2Int(367,304),new Vector2Int(281,304),
                new Vector2Int(275,253),new Vector2Int(294,231),new Vector2Int(296,253),new Vector2Int(362,254),
                new Vector2Int(298,251),new Vector2Int(286,303),new Vector2Int(284,233),new Vector2Int(361,232)};
            var bytes=NativeThunderEffectBuilder.ReadBitmap("분석/Saver_1.2/decoded/all_arc/gameres/etc2/Fx2.bmp");
            int offset=BitConverter.ToInt32(bytes,10),width=rects.Max(x=>x.width),height=rects.Sum(x=>x.height),dy=0;
            var pixels=new Color32[width*height]; var slices=new SpriteMetaData[12]; var offsets=new Vector2[12];
            for(int i=0;i<12;i++)
            {
                var r=rects[i];
                for(int y=0;y<r.height;y++)for(int x=0;x<r.width;x++)
                {
                    int color=bytes[offset+(998-r.y-y)*1000+r.x+x],p=54+color*4;
                    byte b=bytes[p],g=bytes[p+1],red=bytes[p+2];
                    pixels[(height-1-dy-y)*width+x]=new Color32(red,g,b,(byte)((red|g|b)==0?0:255));
                }
                slices[i]=new SpriteMetaData{name="Circle_"+i.ToString("D2"),rect=new Rect(0,height-dy-r.height,r.width,r.height),alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f)};
                offsets[i]=new Vector2(destinations[i].x-349+r.width*.5f-16,-(destinations[i].y-259+r.height*.5f-16));
                dy+=r.height;
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
            var asset=ScriptableObject.CreateInstance<NativeLevelUpCircleEffects>();asset.Initialize(4001,sprites,offsets);
            AssetDatabase.CreateAsset(asset,Asset);AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
