using UnityEngine;
using DarkSaver.Original.Tutorial;
namespace DarkSaver.Original.UI
{
    public static class OriginalWindowPreferences
    {
        private static string Key(int id)=>"DarkSaver.Original.Client.Dlg Information."+NativeWindowPlacement.ProfileKey(id);
        private const string ChatSizeKey="DarkSaver.Original.Client.Dlg Information.ChatWindowSize";
        public static bool ChatExpanded=>PlayerPrefs.GetInt(ChatSizeKey,1)==2;
        public static NativeWindowState LoadState(WaitingWindowDefinition spec)=>NativeWindowPlacement.Restore(spec,PlayerPrefs.GetString(Key(spec.id),NativeWindowPlacement.DefaultProfile(spec.id)),PlayerPrefs.GetInt(ChatSizeKey,1));
        public static NativeWindowPosition Load(WaitingWindowDefinition spec)=>LoadState(spec).Position;
        public static void Save(WaitingWindowDefinition spec,int x,int y,bool expanded=false,bool minimized=false)
        {PlayerPrefs.SetString(Key(spec.id),NativeWindowPlacement.Save(spec,x,y,minimized,expanded));if(spec.id==19)PlayerPrefs.SetInt(ChatSizeKey,expanded&&!minimized?2:1);PlayerPrefs.Save();}
    }
}
