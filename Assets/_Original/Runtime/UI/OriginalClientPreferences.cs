using UnityEngine;
namespace DarkSaver.Original.UI
{
    public static class OriginalClientPreferences
    {
        private static OriginalClientSettings current;
        private const string Prefix="DarkSaver.Original.Client.";
        public static OriginalClientSettings Current
        {
            get
            {
                if(current!=null)return current;
                current=new OriginalClientSettings{Music=PlayerPrefs.GetInt(Prefix+"MusicSelect.Background",1)!=0,Sound=PlayerPrefs.GetInt(Prefix+"MusicSelect.Effect",1)!=0,
                    GlobalChat=PlayerPrefs.GetInt(Prefix+"Environment.GlobalChat",1)!=0,ViewMenu=PlayerPrefs.GetInt(Prefix+"Environment.View Menu",1)!=0};
                int wanted=PlayerPrefs.GetInt(Prefix+"Environment.Font Number",0);if(wanted>=0&&wanted<=1&&NativeBitmapText.SetVariant(wanted))current.Font=wanted;
                return current;
            }
        }
        public static void Save()
        {
            var s=Current;PlayerPrefs.SetInt(Prefix+"MusicSelect.Background",s.Music?1:0);PlayerPrefs.SetInt(Prefix+"MusicSelect.Effect",s.Sound?1:0);
            PlayerPrefs.SetInt(Prefix+"Environment.GlobalChat",s.GlobalChat?1:0);PlayerPrefs.SetInt(Prefix+"Environment.View Menu",s.ViewMenu?1:0);PlayerPrefs.SetInt(Prefix+"Environment.Font Number",s.Font);
            PlayerPrefs.Save();
        }
    }
}
