using UnityEngine;

namespace DarkSaver.Prototype
{
    public sealed class BattleUnit
    {
        public string Name;
        public Vector2Int Cell;
        public int Hp;
        public int MaxHp;
        public int Mp;
        public int MaxMp;
        public int Ap;
        public int MaxAp;
        public int Attack;
        public int Defense;
        public bool Enemy;
        public bool CanMagic;
        public string SkillName;
        public int SkillPower;
        public string Mark;
        public Color Color;
        public int Level = 1;
        public int Experience;
        public float ActionCharge;

        public bool Alive => Hp > 0;
        public bool CanUseSkill => !string.IsNullOrEmpty(SkillName) && SkillPower > 0;
        public int NextLevelExperience => BattleRules.GetNextLevelExperience(Level);
    }
}
