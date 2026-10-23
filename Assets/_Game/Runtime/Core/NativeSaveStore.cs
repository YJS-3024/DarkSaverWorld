using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace DarkSaver.Core
{
    [Serializable]
    public sealed class NativeSaveData
    {
        public string format;
        public int version;
        public string mapScene, runtimeScene;
        public float worldX, worldY;
        public uint experience;
        public bool hasVitals;
        public int hp, maxHp, mp, maxMp;
        public int lifeState;
        public string[] clearedScenarios;
        // Optional additive v1 field. Legacy snapshots have no created character.
        public NativeCharacterProfile character;
        public bool hasCharacter;
        // v2 only; null is retained for v1, never synthesized from a catalog.
        public NativeOwnedRoster ownedRoster;
        public bool hasOwnedRoster;
        public bool hasInventory;
        public NativeInventory inventory;
    }

    // Native single-player state only; no original file references or server formulas.
    public sealed class NativeSaveStore
    {
        public const string Format = "DarkSaverWorld.NativeSave";
        public const int Version = 1;
        public const int RosterVersion = 2;
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "native-save-v1.json");
        public string FilePath { get; }
        private static NativeSaveData pending;
        private static string pendingPath;

        public NativeSaveStore(string filePath) { FilePath = Path.GetFullPath(filePath); }

        public static bool IsValid(NativeSaveData data)
        {
            if (data == null || data.format != Format || (data.version != Version && data.version != RosterVersion) ||
                !IsSupportedMap(data.mapScene, data.runtimeScene) ||
                float.IsNaN(data.worldX) || float.IsInfinity(data.worldX) ||
                float.IsNaN(data.worldY) || float.IsInfinity(data.worldY) ||
                data.clearedScenarios == null || data.clearedScenarios.Length > 10000) return false;
            // Unity JSON may materialize a null inline class as an empty object.
            if (data.version == Version ? data.hasOwnedRoster || (data.ownedRoster?.units?.Length ?? 0) != 0
                : !data.hasOwnedRoster || data.ownedRoster == null || !data.ownedRoster.IsValid) return false;
            if (data.hasVitals && (data.maxHp <= 0 || data.hp < 0 || data.hp > data.maxHp ||
                data.maxMp < 0 || data.mp < 0 || data.mp > data.maxMp || data.lifeState < 0 || data.lifeState > 2 ||
                (data.lifeState != 0 && data.hp > 0))) return false;
            if (data.hasCharacter && (data.character == null || !data.character.IsValid)) return false;
            if (data.hasInventory ? !data.hasCharacter || data.inventory == null || !data.inventory.IsValid
                : data.inventory != null && (data.inventory.gold != 0 || (data.inventory.items?.Length ?? 0) != 0)) return false;
            if(data.hasCharacter && !data.character.IsEquipmentOwned(data.hasInventory ? data.inventory : null))return false;
            if (!data.hasCharacter && data.character != null &&
                (!string.IsNullOrEmpty(data.character.name) || data.character.jobId != 0)) return false;
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var key in data.clearedScenarios)
                if (string.IsNullOrWhiteSpace(key) || !keys.Add(key)) return false;
            return true;
        }

        public static bool IsSupportedMap(string map, string runtime)
        {
            for (int id = 101; id <= 107; id++)
                if (map == "Map_" + id + "_;" && runtime == "Training" + id + "_Runtime") return true;
            return (map == "Kaosia_Map" && runtime == "Town_Runtime") ||
            (map == "World1010_Map" && runtime == "Field_Runtime") ||
            (map == "Brugos01_Map" && runtime == "Brugos01_Runtime");
        }

        public bool TryLoad(out NativeSaveData data, out string message)
        {
            data = null;
            message = "저장된 게임이 없습니다.";
            try
            {
                if (!File.Exists(FilePath)) return false;
                if (new FileInfo(FilePath).Length > 1024 * 1024) { message = "저장 파일이 너무 큽니다."; return false; }
                var candidate = JsonUtility.FromJson<NativeSaveData>(File.ReadAllText(FilePath, Encoding.UTF8));
                if (!IsValid(candidate)) { message = "저장 데이터의 형식 또는 값이 맞지 않습니다."; return false; }
                data = candidate; message = string.Empty; return true;
            }
            catch (Exception) { message = "저장 파일을 읽을 수 없습니다."; return false; }
        }

        public bool TrySave(NativeSaveData data, out string message)
        {
            message = "저장할 상태가 올바르지 않습니다.";
            if (!IsValid(data)) return false;
            string temporary = null;
            try
            {
                // Preserve an unreadable/unknown-version slot rather than overwriting it.
                if (File.Exists(FilePath) && !TryLoad(out _, out _))
                { message = "기존 저장 파일에 오류가 있어 덮어쓰지 않았습니다."; return false; }
                // Never silently drop a newer roster through a legacy writer.
                if (File.Exists(FilePath) && TryLoad(out var previous, out _) && previous.version > data.version)
                { message = "보유 목록이 있는 저장을 이전 버전으로 덮어쓰지 않았습니다."; return false; }
                var json = JsonUtility.ToJson(data, true);
                if (Encoding.UTF8.GetByteCount(json) > 1024 * 1024)
                { message = "저장 데이터가 너무 큽니다."; return false; }
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                temporary = FilePath + ".tmp-" + Guid.NewGuid().ToString("N");
                File.WriteAllText(temporary, json, new UTF8Encoding(false));
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, FilePath + ".bak");
                else File.Move(temporary, FilePath);
                message = "저장했습니다."; return true;
            }
            catch (Exception) { message = "저장 파일을 기록할 수 없습니다."; return false; }
            finally
            {
                if (temporary != null && File.Exists(temporary))
                    try { File.Delete(temporary); } catch (Exception) { }
            }
        }

        public static bool QueueResume(NativeSaveData data, string filePath)
        {
            ClearResume();
            NativeCharacterProfile.ClearPending();
            if (!IsValid(data)) return false;
            string fullPath;
            try { fullPath = Path.GetFullPath(filePath); }
            catch (ArgumentException) { return false; }
            pending = JsonUtility.FromJson<NativeSaveData>(JsonUtility.ToJson(data));
            pendingPath = fullPath;
            return true;
        }

        public static bool TakeResume(out NativeSaveData data, out string filePath)
        {
            data = pending; filePath = pendingPath; ClearResume();
            return data != null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearResume() { pending = null; pendingPath = null; }
    }
}
