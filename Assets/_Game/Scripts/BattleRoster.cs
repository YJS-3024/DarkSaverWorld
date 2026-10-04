using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.Prototype
{
    public static class BattleRoster
    {
        public static List<BattleUnit> CreateParty()
        {
            return new List<BattleUnit>
            {
                new BattleUnit
                {
                    Name = "아켄 용병대장", Mark = "대장", Color = new Color(.18f, .50f, .92f),
                    Cell = new Vector2Int(1, 4), Hp = 120, MaxHp = 120,
                    Mp = 30, MaxMp = 30, Ap = 10, MaxAp = 10, Attack = 26, Defense = 7,
                    SkillName = "지휘관의 일격", SkillPower = 12, SpriteColumn = 7, SpriteRow = 0,
                    SwordEquipped = true, ArmorEquipped = true
                },
                new BattleUnit
                {
                    Name = "브란 전사", Mark = "전", Color = new Color(.28f, .70f, .45f),
                    Cell = new Vector2Int(1, 5), Hp = 145, MaxHp = 145,
                    Mp = 15, MaxMp = 15, Ap = 10, MaxAp = 10, Attack = 31, Defense = 11,
                    SkillName = "강타", SkillPower = 16, SpriteColumn = 7, SpriteRow = 1
                },
                new BattleUnit
                {
                    Name = "세라 마법사", Mark = "법", Color = new Color(.62f, .35f, .92f),
                    Cell = new Vector2Int(2, 4), Hp = 85, MaxHp = 85,
                    Mp = 70, MaxMp = 70, Ap = 10, MaxAp = 10, Attack = 17, Defense = 3,
                    CanMagic = true, SpriteColumn = 7, SpriteRow = 2
                }
            };
        }

        public static List<BattleUnit> CreateEnemies()
        {
            return new List<BattleUnit>
            {
                new BattleUnit { Name = "황야 고블린", Cell = new Vector2Int(7, 2), Hp = 55, MaxHp = 55, Ap = 6, MaxAp = 10, Attack = 18, Defense = 3, Enemy = true, SpriteColumn = 6, SpriteRow = 0 },
                new BattleUnit { Name = "황야 고블린", Cell = new Vector2Int(8, 5), Hp = 55, MaxHp = 55, Ap = 4, MaxAp = 10, Attack = 18, Defense = 3, Enemy = true, SpriteColumn = 9, SpriteRow = 0 },
                new BattleUnit { Name = "오크 척후병", Cell = new Vector2Int(6, 6), Hp = 80, MaxHp = 80, Ap = 2, MaxAp = 10, Attack = 23, Defense = 6, Enemy = true, SpriteColumn = 12, SpriteRow = 1 }
            };
        }
    }
}
