using UnityEditor;
using UnityEngine;

namespace DarkSaver.Prototype.Editor
{
    internal sealed class OriginalPixelArtImporter : AssetPostprocessor
    {
        private const string OriginalAssetRoot = "Assets/Resources/Original/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(OriginalAssetRoot))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
