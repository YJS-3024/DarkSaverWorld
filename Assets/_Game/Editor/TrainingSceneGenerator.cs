using System;
using System.IO;
using System.Linq;
using DarkSaver.Combat;
using DarkSaver.Jobs;
using DarkSaver.Player;
using DarkSaver.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace DarkSaver.EditorTools
{
    // User-approved thematic placements, NOT recovered native training spawns.
    public static class TrainingSceneGenerator
    {
        public static int MonsterJob(int mapId) => mapId switch
        { 101 => 302, 102 => 301, 103 => 305, 104 => 310, 105 => 336, 106 => 308, 107 => 339,
            _ => throw new ArgumentOutOfRangeException(nameof(mapId)) };
        public static string RuntimePath(int id) => $"Assets/_Game/Scenes/Runtime/Training{id}_Runtime.unity";
        public static string EntryPath(int id) => $"Assets/_Game/Scenes/Entry/Training{id}_Play.unity";
        public static string EnemyPath(int id) => $"Assets/_Game/Runtime/Combat/Training/Training{id}_Enemy.asset";
        private static string ArtPath(int sheet) => $"Assets/_Game/Art/Converted/Monsters/Training/Monster{sheet}.png";

        [MenuItem("DarkSaver/Scenes/Create Themed Training Encounters")]
        public static void Generate()
        {
            if (!DarkSaver.Core.OriginalRestorationPolicy.AllowSinglePlayerRules)
                throw new NotSupportedException("원작 훈련장 생성 정보 없이 테마 몬스터를 자동 배치할 수 없습니다.");
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Generate training scenes only in idle Edit mode.");
            var previous = EditorSceneManager.GetSceneManagerSetup();
            for (int i=0;i<SceneManager.sceneCount;i++)
                if (SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                    throw new InvalidOperationException("Save or close dirty/untitled scenes before generation.");
            Directory.CreateDirectory("Assets/_Game/Art/Converted/Monsters/Training");
            Directory.CreateDirectory("Assets/_Game/Runtime/Combat/Training");
            AssetDatabase.Refresh();
            var jobs = AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/JobCatalog.asset");
            var monsters = AssetDatabase.LoadAssetAtPath<JobCatalog>("Assets/_Game/Runtime/Jobs/MonsterJobCatalog.asset");
            if (jobs == null || monsters == null) throw new InvalidOperationException("Missing converted job catalogs.");
            try
            {
                for (int id=101;id<=107;id++)
                {
                    // Do not regenerate an existing placement, which may have user edits.
                    if (File.Exists(RuntimePath(id)) && File.Exists(EntryPath(id))) continue;
                    if (File.Exists(RuntimePath(id)) || File.Exists(EntryPath(id)))
                        throw new InvalidOperationException("Partial training scenes already exist: " + id);
                    int job = MonsterJob(id);
                    if (!monsters.TryGet(job,out var entry)) throw new InvalidOperationException("Missing monster " + job);
                    int sheet = checked((int)entry.GetNumericValue(1));
                    ConvertSheet(sheet);
                    var scene = EditorSceneManager.OpenPreviewScene($"Assets/_Game/Scenes/Maps/Map_{id}_;.unity");
                    CombatGridCell spawn, target;
                    Vector3 spawnWorld, targetWorld;
                    try
                    {
                        Tilemap ground=null,terrain=null;
                        foreach(var root in scene.GetRootGameObjects())
                        foreach(var map in root.GetComponentsInChildren<Tilemap>())
                        { if(map.name=="Ground")ground=map;if(map.name=="TerrainAttributes")terrain=map; }
                        if (ground == null || terrain == null || !TrainingBattleVerificationSetup.TryFindFixtureCells(
                            ground,terrain,new[]{jobs,monsters},job,out spawn,out target))
                            throw new InvalidOperationException("No passable placement lane for training " + id);
                        spawnWorld=ground.GetCellCenterWorld(new Vector3Int(spawn.X,ground.cellBounds.yMax-1-spawn.Y,0));
                        targetWorld=ground.GetCellCenterWorld(new Vector3Int(target.X,ground.cellBounds.yMax-1-target.Y,0));
                    }
                    finally { EditorSceneManager.ClosePreviewScene(scene); }
                    var definition=AssetDatabase.LoadAssetAtPath<BattleEnemyDefinition>(EnemyPath(id));
                    if (definition == null)
                    {
                        definition=ScriptableObject.CreateInstance<BattleEnemyDefinition>();
                        var data=new SerializedObject(definition);
                        data.FindProperty("instanceId").intValue=800;
                        data.FindProperty("jobId").intValue=job;
                        data.FindProperty("displayName").stringValue=entry.Name.Replace('_',' ');
                        data.FindProperty("scriptPosition").vector2IntValue=new Vector2Int(target.X,target.Y);
                        data.FindProperty("useJobCatalogBaseStats").boolValue=true;
                        data.FindProperty("jobCatalog").objectReferenceValue=monsters;
                        data.FindProperty("useNativeBaseDefeatExperience").boolValue=false;
                        data.FindProperty("victoryObjective").objectReferenceValue=null;
                        data.ApplyModifiedPropertiesWithoutUndo();
                        AssetDatabase.CreateAsset(definition,EnemyPath(id));
                    }
                    var generated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                    try
                    {
                        SceneManager.SetActiveScene(generated);
                        var spawnObject=new GameObject("Spawn TrainingEntry",typeof(SceneSpawnPoint));
                        spawnObject.transform.position=spawnWorld;
                        spawnObject.GetComponent<SceneSpawnPoint>().Configure("TrainingEntry");
                        var encounter=new GameObject("Themed placement - "+entry.Name+" (not native spawn)",
                            typeof(SpriteRenderer),typeof(BoxCollider2D),typeof(BattleEncounter2D),typeof(DirectionalSpriteAnimator));
                        encounter.transform.position=targetWorld;
                        var frames=AssetDatabase.LoadAllAssetsAtPath(ArtPath(sheet)).OfType<Sprite>().ToArray();
                        Sprite[] Direction(string direction) => Enumerable.Range(0,3).Select(n=>frames.Single(s=>s.name==direction+"_"+n)).ToArray();
                        encounter.GetComponent<SpriteRenderer>().sprite=Direction("Down")[1];
                        encounter.GetComponent<SpriteRenderer>().sortingOrder=20000-Mathf.RoundToInt(targetWorld.y*10);
                        encounter.GetComponent<BoxCollider2D>().isTrigger=true;
                        encounter.GetComponent<BoxCollider2D>().size=Vector2.one;
                        encounter.GetComponent<BattleEncounter2D>().ConfigureEnemy(definition);
                        encounter.GetComponent<DirectionalSpriteAnimator>().Configure(Direction("Up"),Direction("Down"),Direction("Left"),Direction("Right"));
                        if(!EditorSceneManager.SaveScene(generated,RuntimePath(id)))throw new IOException("Cannot save training runtime.");
                    }
                    finally { EditorSceneManager.CloseScene(generated,true); }
                    generated=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                    try
                    {
                        SceneManager.SetActiveScene(generated);
                        var bootstrap=new GameObject("Training "+id+" Bootstrap",typeof(AdditiveSceneBootstrap)).GetComponent<AdditiveSceneBootstrap>();
                        bootstrap.Configure("GameCore","Map_"+id+"_;","Training"+id+"_Runtime");
                        bootstrap.ConfigureEntryPosition(spawnWorld);
                        if(!EditorSceneManager.SaveScene(generated,EntryPath(id)))throw new IOException("Cannot save training entry.");
                    }
                    finally { EditorSceneManager.CloseScene(generated,true); }
                    Debug.Log($"Training{id}: themed {entry.Name} job={job}, spawn={spawn.X},{spawn.Y}, enemy={target.X},{target.Y}. Not original placement.");
                }
                var paths=Enumerable.Range(101,7).SelectMany(id=>new[]{EntryPath(id),RuntimePath(id),$"Assets/_Game/Scenes/Maps/Map_{id}_;.unity"});
                EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(paths.Where(p=>!EditorBuildSettings.scenes.Any(s=>s.path==p))
                    .Select(p=>new EditorBuildSettingsScene(p,true))).ToArray();
                AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static void ConvertSheet(int sheet)
        {
            string output=ArtPath(sheet);
            if(File.Exists(output))return;
            string root="분석/Saver_1.2/decoded/all_arc/gameres";
            var candidates=new[]{root+"/char1/"+sheet+"R.bmp",root+"/char2/"+sheet+"R.bmp"}.Where(File.Exists).ToArray();
            if(candidates.Length!=1)throw new InvalidDataException("Ambiguous/missing monster sheet "+sheet);
            var bytes=File.ReadAllBytes(candidates[0]);
            if(bytes.Length<1078 || bytes[0]!='B' || bytes[1]!='M' || BitConverter.ToInt32(bytes,14)!=40
                || BitConverter.ToInt32(bytes,18)!=96 || BitConverter.ToInt32(bytes,22)!=128
                || BitConverter.ToInt16(bytes,26)!=1 || BitConverter.ToInt16(bytes,28)!=8 || BitConverter.ToInt32(bytes,30)!=0)
                throw new InvalidDataException("Expected native 96x128 indexed monster sheet.");
            int offset=BitConverter.ToInt32(bytes,10);
            if(offset<1078 || offset>bytes.Length-96*128)throw new InvalidDataException("Invalid monster pixels.");
            var texture=new Texture2D(96,128,TextureFormat.RGBA32,false);
            try
            {
                var pixels=new Color32[96*128];
                for(int y=0;y<128;y++)for(int x=0;x<96;x++)
                {
                    int index=bytes[offset+y*96+x],p=54+index*4;
                    byte r=bytes[p+2],g=bytes[p+1],b=bytes[p];
                    pixels[y*96+x]=new Color32(r,g,b,(byte)((r|g|b)==0?0:255));
                }
                texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(output,texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(output);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            var names=new[]{"Up","Down","Left","Right"};var slices=new SpriteMetaData[12];
            for(int row=0;row<4;row++)for(int frame=0;frame<3;frame++)slices[row*3+frame]=new SpriteMetaData {
                name=names[row]+"_"+frame,rect=new Rect(frame*32,128-(row+1)*32,32,32),
                alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(.5f,.1f)};
#pragma warning disable CS0618
            importer.spritesheet=slices;
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }
    }
}
