namespace DarkSaver.Core
{
    public static class OriginalRestorationPolicy
    {
        // No editor, fixture or player-build switch can re-enable removed rules.
        public static bool AllowSinglePlayerRules=>false;
        public static bool AllowsStaffEvidence(string evidence)=>evidence=="screenshot" || evidence=="user-confirmed";
        public const string PendingServerRule="원작 서버 응답이 필요한 기능입니다.";
    }
}
