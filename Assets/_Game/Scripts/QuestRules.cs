namespace DarkSaver.Prototype
{
    public enum QuestStage
    {
        NotAccepted,
        Active,
        ReadyToDeliver,
        Completed
    }

    public static class QuestRules
    {
        public const int RequiredMonsterKills = 2;
        public const int MonsterDropChancePercent = 40;
        public const int DeliveryGoldReward = 100;
        public const int DeliveryPotionReward = 2;

        public static bool IsReadyToDeliver(
            int monsterKills, bool fieldItemCollected, int monsterDropCount)
        {
            return monsterKills >= RequiredMonsterKills &&
                   fieldItemCollected && monsterDropCount > 0;
        }

        public static bool IsMonsterDrop(int roll, int chancePercent = MonsterDropChancePercent)
        {
            if (chancePercent <= 0) return false;
            if (chancePercent >= 100) return true;
            return roll >= 0 && roll < chancePercent;
        }
    }
}
