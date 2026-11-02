using UnityEngine;
namespace DarkSaver.Original.UI
{
    public sealed class NativeBitmapFont : ScriptableObject
    {
        public Texture2D normal,alternate;
        public TextAsset mapping;
        private NativeTextMapping decoded;
        public bool Ready=>normal!=null&&alternate!=null&&mapping!=null;
        public NativeTextMapping Mapping=>decoded??(decoded=JsonUtility.FromJson<NativeTextMapping>(mapping.text));
        public Texture2D Texture(int variant)=>variant==1?alternate:normal;
    }
}
