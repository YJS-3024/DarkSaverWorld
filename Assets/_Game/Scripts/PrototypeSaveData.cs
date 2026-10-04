using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DarkSaver.Prototype
{
    [Serializable]
    public sealed class PrototypeSaveData
    {
        public int version = PrototypeSaveStore.CurrentVersion;
        public int defeated;
        public int gold;
        public int healingPotionCount;
        public int fieldX;
        public int fieldY;
        public int questStage;
        public int questMonsterKills;
        public bool questFieldItemCollected;
        public int questMonsterDropCount;
        public List<PrototypePartySaveData> party = new List<PrototypePartySaveData>();
    }

    [Serializable]
    public sealed class PrototypePartySaveData
    {
        public string name;
        public int hp;
        public int maxHp;
        public int mp;
        public int maxMp;
        public int attack;
        public int defense;
        public int level;
        public int experience;
        public bool swordEquipped;
        public bool armorEquipped;
    }

    public static class PrototypeSaveStore
    {
        public const int CurrentVersion = 3;
        private const string FileName = "darksaver-save.json";

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists(string path = null)
        {
            return File.Exists(path ?? DefaultPath);
        }

        public static string Serialize(PrototypeSaveData data)
        {
            return JsonUtility.ToJson(data, true);
        }

        public static bool TryDeserialize(string json, out PrototypeSaveData data)
        {
            data = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<PrototypeSaveData>(json);
            if (data == null) return false;
            if (data.version >= 1 && data.version < CurrentVersion)
            {
                data.version = CurrentVersion;
                return true;
            }

            return data.version == CurrentVersion;
        }

        public static void Save(PrototypeSaveData data, string path = null)
        {
            var target = path ?? DefaultPath;
            var directory = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(target, Serialize(data));
        }

        public static bool TryLoad(out PrototypeSaveData data, string path = null)
        {
            var target = path ?? DefaultPath;
            if (!File.Exists(target))
            {
                data = null;
                return false;
            }

            return TryDeserialize(File.ReadAllText(target), out data);
        }
    }
}
