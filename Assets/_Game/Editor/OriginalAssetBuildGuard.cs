using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DarkSaver.Editor
{
    internal sealed class OriginalAssetBuildGuard : IPreprocessBuildWithReport
    {
        private static readonly string[] OriginalExtensions =
        {
            ".mrc", ".mrtc", ".arc", ".atr", ".grs", ".lsc", ".evt", ".srf", ".bmp"
        };

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Validate();

        [MenuItem("DarkSaver/Validate/No Original Assets")]
        private static void ValidateFromMenu()
        {
            Validate();
            Debug.Log("Original asset check passed: Assets contains no raw game files.");
        }

        private static void Validate()
        {
            var assets = Application.dataPath;
            var originalFolders = new[]
            {
                Path.Combine(assets, "_Game", "StreamingAssets", "OriginalData"),
                Path.Combine(assets, "Resources", "Original"),
                Path.Combine(assets, "Resources", "Tables", "Original")
            };
            var existingFolder = originalFolders.FirstOrDefault(Directory.Exists);
            if (existingFolder != null)
                throw new BuildFailedException($"Original data folder is inside Assets: {existingFolder}");

            var originalFile = Directory.EnumerateFiles(assets, "*", SearchOption.AllDirectories)
                .FirstOrDefault(path => OriginalExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase));
            if (originalFile != null)
                throw new BuildFailedException($"Raw game file is inside Assets: {originalFile}");
        }
    }
}
