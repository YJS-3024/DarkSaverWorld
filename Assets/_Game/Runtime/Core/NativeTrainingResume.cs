namespace DarkSaver.Core
{
    // User policy: training disconnect resumes outside the Kaosia entrance.
    // Location-only transformation; never grants healing, XP, money or items.
    public static class NativeTrainingResume
    {
        public static bool IsTraining(NativeSaveData state)
        {
            if(state==null)return false;
            for(int id=101;id<=107;id++)
                if(state.mapScene=="Map_"+id+"_;" && state.runtimeScene=="Training"+id+"_Runtime")return true;
            return false;
        }
        public static bool TryRedirect(NativeSaveData source,float x,float y,out NativeSaveData redirected)
        {
            redirected=null;
            if(!NativeSaveStore.IsValid(source) || !source.hasCharacter || !IsTraining(source)
                || float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y)
                || x<0 || x>=43 || y<0 || y>=43)return false;
            var copy=NativeSaveStore.CopyForTransfer(source);
            copy.mapScene="Kaosia_Map";copy.runtimeScene="Town_Runtime";copy.worldX=x;copy.worldY=y;
            if(!NativeSaveStore.IsValid(copy))return false;
            redirected=copy;return true;
        }
    }
}
