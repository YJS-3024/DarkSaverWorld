using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DarkSaver.Tests
{
    // Open a fixture-owned scene asset: CreateScene is Play-only, and NewScene(Additive)
    // can be rejected by the Editor's current creation mode. Never save a user's scene.
    internal static class EditModeSceneFixture
    {
        private static readonly Dictionary<Scene,(string Folder,Scene Previous)> folders=new();
        public static Scene Create(string name)
        {
            string folder="Assets/_Game/Tests/EditMode/SceneFixture-"+Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(folder);
            string path=folder+"/"+name+".unity";
            try
            {
                File.WriteAllText(path,"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots: []\n");
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var previous=SceneManager.GetActiveScene();
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                if(!scene.IsValid() || !scene.isLoaded)throw new IOException("Fixture scene did not load.");
                folders.Add(scene,(folder,previous));return scene;
            }
            catch {AssetDatabase.DeleteAsset(folder);if(Directory.Exists(folder))Directory.Delete(folder,true);throw;}
        }
        public static void Close(Scene scene)
        {
            if(!folders.TryGetValue(scene,out var entry))throw new InvalidOperationException("Not a fixture-owned scene.");
            bool restoreActive=SceneManager.GetActiveScene()==scene;
            if(scene.IsValid() && scene.isLoaded && !EditorSceneManager.CloseScene(scene,true))
                throw new IOException("Fixture scene did not close; files retained.");
            folders.Remove(scene);
            if(restoreActive && entry.Previous.IsValid() && entry.Previous.isLoaded)SceneManager.SetActiveScene(entry.Previous);
            string folder=entry.Folder;
            AssetDatabase.DeleteAsset(folder);
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
    }
}
