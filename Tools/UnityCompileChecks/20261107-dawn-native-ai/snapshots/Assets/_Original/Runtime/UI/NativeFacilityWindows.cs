using System;
using System.Linq;
using DarkSaver.Original.Tutorial;
namespace DarkSaver.Original.UI
{
    [Serializable] public sealed class NativeFacilityWindows
    {
        public WaitingWindowDefinition[] windows;
        public WaitingWindowDefinition Find(int id,int scene,int width,int height)=>windows?.FirstOrDefault(window=>window.id==id&&window.scene==scene&&window.width==width&&window.height==height);
        public static int DefaultId(int scene)=>scene==9?24:scene==18?34:scene==20?37:0;
        public static int ShopId(int? nativeBase)=>!nativeBase.HasValue?48:nativeBase==0?29:nativeBase==100?30:nativeBase==200?31:nativeBase==300?32:0;
    }
}
