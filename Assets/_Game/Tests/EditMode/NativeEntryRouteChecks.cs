using System;
using DarkSaver.Core;
namespace DarkSaver.Tests
{
    public static class NativeEntryRouteChecks
    {
        public static void Run(Action<bool,string> check)
        {
            var occupied=new bool[10];var readable=new bool[10];
            check(NativeEntryCharacterRoute.TryResolve(occupied,readable,-1,out var slot,out var create,out _) && slot==0 && create,"empty account enters native creation");
            occupied[0]=true;
            check(!NativeEntryCharacterRoute.TryResolve(occupied,readable,-1,out _,out _,out _),"corrupt primary cannot be replaced with fresh character");
            readable[0]=true;
            check(NativeEntryCharacterRoute.TryResolve(occupied,readable,-1,out slot,out create,out _) && slot==0 && !create,"existing account resumes primary");
            occupied[4]=readable[4]=true;
            check(NativeEntryCharacterRoute.TryResolve(occupied,readable,4,out slot,out create,out _) && slot==4 && !create,"account history preserves legacy character");
            check(!NativeEntryCharacterRoute.TryResolve(occupied,readable,3,out _,out _,out _),"invalid history fails closed");
            occupied[0]=readable[0]=false;
            check(NativeEntryCharacterRoute.TryResolve(occupied,readable,-1,out slot,out create,out _) && slot==4 && !create,"single legacy character resumes without picker");
            occupied[5]=readable[5]=true;
            check(!NativeEntryCharacterRoute.TryResolve(occupied,readable,-1,out _,out _,out _),"ambiguous legacy data is preserved");
            check(occupied[4] && occupied[5] && readable[4] && readable[5],"routing never mutates slot data");
        }
    }
}
