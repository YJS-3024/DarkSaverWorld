// Offline boundary adapters only. These checks exercise linked production C#,
// not Unity asset import, Unity JsonUtility, profiles, components or PlayMode.
using System.Text.Json;
using System.Text.Json.Serialization;
namespace UnityEngine
{
    public class Object { public static void DestroyImmediate(Object value) { } }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : new() => new T(); }
    public class SerializeField : Attribute { }
    public class CreateAssetMenuAttribute : Attribute { public string menuName; }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType kind) { } }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public static class Application
    { public static string dataPath => Path.Combine(Directory.GetCurrentDirectory(), "Assets"); public static string persistentDataPath => Path.GetTempPath(); }
    public static class Debug { public static void Log(string value) { } }
    public static class JsonUtility
    {
        // Property getters must not leak into the field-only Unity wire shape.
        private static JsonSerializerOptions Options(bool pretty) => new()
        {
            IncludeFields = true, WriteIndented = pretty,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver
            { Modifiers = { info => { for (int i = info.Properties.Count - 1; i >= 0; i--)
                if (info.Properties[i].AttributeProvider is System.Reflection.PropertyInfo) info.Properties.RemoveAt(i); } } }
        };
        public static string ToJson<T>(T value, bool pretty = false) => JsonSerializer.Serialize(value, Options(pretty));
        public static T FromJson<T>(string value) => JsonSerializer.Deserialize<T>(value, Options(false));
    }
}
namespace UnityEditor
{
    public class MenuItem : Attribute { public MenuItem(string name) { } }
    public static class EditorApplication { public static bool isPlayingOrWillChangePlaymode => false; }
    public static class AssetDatabase
    {
        public static T LoadAssetAtPath<T>(string path) where T : class => throw new NotSupportedException("Unity import required");
        public static void CreateAsset(object value, string path) => throw new NotSupportedException("Unity import required");
        public static void SaveAssetIfDirty(object value) { }
        public static bool Contains(object value) => false;
    }
}
namespace DarkSaver.Core
{
    // Scenario disk tests use a minimal no-equipment character fixture. Actual
    // profile/roster validation is covered by Unity tests with the real classes.
    [Serializable] public class NativeCharacterProfile
    {
        public string name; public int jobId; public NativeTutorialProgress tutorial;
        public bool IsValid => !string.IsNullOrWhiteSpace(name) && jobId > 0 && (tutorial == null || tutorial.IsValid);
        public bool IsEquipmentOwned(NativeInventory inventory) => true;
        public static void ClearPending() { }
        public NativeCharacterProfile Clone()=>new NativeCharacterProfile {name=name,jobId=jobId,tutorial=tutorial?.Clone()};
        public static bool TryQueue(string name,int jobId,string path,out string reason){throw new NotSupportedException("Offline host does not implement character creation; use Unity tests.");}
    }
    [Serializable] public class NativeOwnedRoster
    { public int[] units; public bool IsValid => false; }
}
namespace DarkSaver
{
    // Dependency adapter for the render ordering checks; no water playback test.
    public static class GameConstants
    { public const int PlayerMoveSpeed = 6; public const int WaterSurfaceYOffsetPixels = 5; public const int WaterSurfaceFrameMilliseconds = 250; }
}
