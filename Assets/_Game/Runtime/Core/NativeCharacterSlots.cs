using System;
using System.IO;

namespace DarkSaver.Core
{
    // Ten local character slots per user correction; legacy slots1-3 keep their paths.
    public static class NativeCharacterSlots
    {
        public const int Count = 10;
        public static string PathFor(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            return index == 0 ? NativeSaveStore.DefaultPath
                : Path.Combine(Path.GetDirectoryName(NativeSaveStore.DefaultPath), "native-character-" + (index + 1) + ".json");
        }
        public static string[] AllPaths()
        {
            var paths=new string[Count];for(int i=0;i<paths.Length;i++)paths[i]=PathFor(i);return paths;
        }
        public static bool IsOccupied(string path) => File.Exists(path) || File.Exists(path + ".bak");
    }
}
