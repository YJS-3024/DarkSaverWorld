using System.Globalization;
namespace DarkSaver.Original.UI
{
    public static class NativeItemCaption
    {
        // Saver46BF20/490E04. The native value is remaining uses, not the table's
        // durability/price or an invented initial quantity. Mode4 is the lesson.
        public static string Format(string name,int? remainingUses,int mode=1)
        {
            if(name==null)return "";if(name.Length==0)name="이름 없음";
            return mode==1?name+", 사용회수: "+(remainingUses?.ToString(CultureInfo.InvariantCulture)??"—"):name;
        }
    }
}
