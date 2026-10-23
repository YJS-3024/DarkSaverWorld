using System.IO;
using System.Text;
using UnityEditor;
namespace DarkSaver.Editor
{
    public static class NativeBattleHelpBuilder
    {
        [MenuItem("DarkSaver/UI/Build Native Battle Help Text")]
        public static void Build()
        {
            var text=Encoding.GetEncoding(949).GetString(File.ReadAllBytes("\uBD84\uC11D/Saver_1.2/decoded/gameres_set/help.txt"));
            const string output="Assets/_Game/Resources/UI/BattleWindows/NativeBattleHelp.txt";
            File.WriteAllText(output,text.Replace("\r\r\n","\n").Replace("\r\n","\n"),new UTF8Encoding(false));
            AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
