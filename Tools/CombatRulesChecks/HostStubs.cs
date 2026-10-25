// Offline adapters for data declarations only; no Unity rendering or asset import.
namespace UnityEngine
{
    public readonly struct Color
    {
        public Color(float r, float g, float b, float a) { }
    }
    public enum KeyCode { F10 }
    public class ScriptableObject { }
    public sealed class SerializeField : System.Attribute { }
    public sealed class CreateAssetMenuAttribute : System.Attribute
    {
        public string menuName;
    }
}
