// Managed host substitutes only Unity attributes/base classes and unused save selection.
// Production profile/table/progression code is compiled unchanged beside this file.
using System;
namespace UnityEngine {
 public class ScriptableObject {}
 [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField:Attribute {}
 [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute:Attribute {public string menuName;}
 public enum RuntimeInitializeLoadType {SubsystemRegistration}
 [AttributeUsage(AttributeTargets.Method)] public sealed class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type){}}
 public struct Color {public Color(float r,float g,float b,float a){}}
 public enum KeyCode {F10}
}
namespace DarkSaver.Core {
 public static class NativeSaveStore {public static string DefaultPath=>throw new NotSupportedException("No host save IO");public static void ClearResume()=>throw new NotSupportedException("No host save IO");}
}
