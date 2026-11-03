using System.Globalization;
namespace DarkSaver.Original.UI
{
    //44A790/4453B0/46F7F0 draw wallet with %lu. The bank's94 balance is
    //drawn with %I64u. These are display projections, never accounting casts.
    public static class NativeCurrencyText
    {
        public static uint WalletValue(ulong value)=>unchecked((uint)value);
        public static string Wallet(ulong value)=>WalletValue(value).ToString(CultureInfo.InvariantCulture);
        public static string Savings(ulong value)=>value.ToString(CultureInfo.InvariantCulture);
    }
}
