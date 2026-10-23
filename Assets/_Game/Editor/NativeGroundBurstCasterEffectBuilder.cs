using System.IO;
using System.Linq;
using DarkSaver.Combat;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Editor
{
    public static class NativeGroundBurstCasterEffectBuilder
    {
        const string Asset="Assets/_Game/Resources/Effects/NativeGroundBurstCasterEffects.asset";
        [MenuItem("DarkSaver/Convert/Native Ground Burst Caster")]
        public static void Build()
        {
            if(File.Exists(Asset))return;
            var entries=MagicCatalogConverter.Parse(File.ReadAllText("분석/Saver_1.2/converted/Magictbl.csv"))
                .Where(x=>x.PatternId==37 || x.PatternId==62).OrderBy(x=>x.Id).ToArray();
            if(entries.Length!=6 || entries.Where((x,i)=>x.Id!=10601+i).Any())throw new InvalidDataException("Unexpected ground burst caster mapping.");
            var source=Resources.Load<NativeTargetEffects>("Effects/NativeGroundExplosionEffects");
            var asset=ScriptableObject.CreateInstance<NativeGroundBurstCasterEffects>();
            asset.Initialize(entries.Select(x=>x.Id).ToArray(),entries.Select(x=>checked((int)x.Area)).ToArray(),source);
            AssetDatabase.CreateAsset(asset,Asset);AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
