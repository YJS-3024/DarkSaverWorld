using DarkSaver;
using DarkSaver.Combat;

int checks = 0;
foreach (int attack in new[] { int.MinValue, -10, 0, 65, 126, int.MaxValue })
foreach (int defense in new[] { int.MinValue, -20, 0, 45, 55, int.MaxValue })
foreach (bool magic in new[] { false, true })
{
    long power = (long)attack + (magic ? GameConstants.BattleTemporaryMagicPowerBonus : 0);
    int mitigation = magic ? defense / Math.Max(1, GameConstants.BattleTemporaryMagicDefenseDivisor) : defense;
    int before = (int)Math.Min(int.MaxValue, Math.Max((long)Math.Max(1, GameConstants.BattleTemporaryMinimumDamage), power - Math.Max(0, mitigation)));
    if (UnresolvedCombatDamage.Calculate(attack, defense, magic) != before)
        throw new Exception($"Damage extraction changed {attack}/{defense}/{magic}");
    checks++;
}
if (Math.Abs(UnresolvedCombatAccuracy.PhysicalHitProbability(80, 15) - .68) > 1e-12)
    throw new Exception("User-approved hit probability changed.");
checks++;
if (GameConstants.GetTemporaryAttackApCost(100, false) != 7
    || GameConstants.GetTemporaryAttackApCost(999, true) != 6)
    throw new Exception("Temporary AP lookup changed.");
checks++;
Console.WriteLine($"PASS: {checks} provisional damage/hit/AP checks. No original-formula equivalence claimed.");
