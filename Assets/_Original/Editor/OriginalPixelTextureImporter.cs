using System;
using UnityEditor;
using UnityEngine;
namespace DarkSaver.Original.Editor
{
    // Original source pixels are already keyed/laid out by the asset converters.
    // Prevent Unity's platform defaults from introducing block artifacts or atlas resizing.
    public sealed class OriginalPixelTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/_Original/Art/",StringComparison.Ordinal)||!(assetImporter is TextureImporter importer))return;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.crunchedCompression=false;importer.maxTextureSize=16384;
            var defaults=importer.GetDefaultPlatformTextureSettings();defaults.maxTextureSize=16384;defaults.resizeAlgorithm=TextureResizeAlgorithm.Mitchell;
            defaults.format=TextureImporterFormat.Automatic;defaults.textureCompression=TextureImporterCompression.Uncompressed;defaults.crunchedCompression=false;
            importer.SetPlatformTextureSettings(defaults);
            foreach(string platform in new[]{"Standalone","Android","iPhone","WebGL"})
            {
                var settings=importer.GetPlatformTextureSettings(platform);if(!settings.overridden)continue;
                settings.maxTextureSize=16384;settings.format=TextureImporterFormat.Automatic;settings.textureCompression=TextureImporterCompression.Uncompressed;settings.crunchedCompression=false;
                importer.SetPlatformTextureSettings(settings);
            }
        }
    }
}
