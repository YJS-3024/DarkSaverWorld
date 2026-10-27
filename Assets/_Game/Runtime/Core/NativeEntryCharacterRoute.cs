using System;
namespace DarkSaver.Core
{
    // Account storage slots are compatibility data, not an original ten-save-slot selection screen.
    public static class NativeEntryCharacterRoute
    {
        public static bool TryResolve(bool[] occupied,bool[] readable,int preferred,out int slot,out bool create,out string reason)
        {
            slot=-1;create=false;reason=null;
            if(occupied==null || readable==null || occupied.Length!=NativeCharacterSlots.Count || readable.Length!=occupied.Length
                || preferred < -1 || preferred>=occupied.Length)
            {reason="캐릭터 응답을 확인할 수 없습니다.";return false;}
            for(int i=0;i<occupied.Length;i++)if(readable[i] && !occupied[i])
            {reason="캐릭터 저장 정보가 일치하지 않습니다.";return false;}
            if(preferred>=0)
            {
                if(!readable[preferred]){reason="마지막 캐릭터 저장을 확인해 주세요.";return false;}
                slot=preferred;return true;
            }
            // The primary account character remains slot0; extra legacy files are never deleted or merged.
            if(readable[0]){slot=0;return true;}
            int count=0,valid=0,only=-1;
            for(int i=0;i<occupied.Length;i++){if(occupied[i])count++;if(readable[i]){valid++;only=i;}}
            if(count==0){slot=0;create=true;return true;}
            if(count==1 && valid==1){slot=only;return true;}
            reason="기존 캐릭터 저장을 확인해 주세요. 손상되거나 여러 개의 이전 저장이 있습니다.";return false;
        }
    }
}
