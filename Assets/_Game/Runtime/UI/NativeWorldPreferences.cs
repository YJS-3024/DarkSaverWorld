using UnityEngine;
namespace DarkSaver.UI
{
    // Client presentation preferences, separate from accounts and game saves.
    public static class NativeWorldPreferences
    {
        private static readonly string[] Keys={"music","effects","all-chat","whisper","menu","fullscreen"};
        public static bool Get(int index)=>PlayerPrefs.GetInt("DarkSaver.WorldUI."+Keys[index],index==5?(Screen.fullScreen?1:0):1)!=0;
        public static void Set(int index,bool value)
        {
            if(index<0 || index>=Keys.Length)throw new System.ArgumentOutOfRangeException(nameof(index));
            PlayerPrefs.SetInt("DarkSaver.WorldUI."+Keys[index],value?1:0);PlayerPrefs.Save();

        }
        // Saver reads45067C only at display creation; changing the checkbox is a pending startup preference.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyStartupDisplayMode()
        {if(!Application.isEditor && PlayerPrefs.HasKey("DarkSaver.WorldUI.fullscreen"))Screen.fullScreen=Get(5);}
        public static int FontMode=>Mathf.Clamp(PlayerPrefs.GetInt("DarkSaver.WorldUI.font",0),0,3);
        public static string FontName=>FontNames[FontMode];
        private static readonly string[] FontNames={"dark14","dark14-1","굴림","솔체"};
        public static void CycleFont(){PlayerPrefs.SetInt("DarkSaver.WorldUI.font",(FontMode+1)%4);PlayerPrefs.Save();}
        public static bool MenuDescriptionsEnabled=>Get(4);
        public static bool MusicEnabled=>Get(0);
        public static bool EffectsEnabled=>Get(1);
    }
}
