using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Security.Cryptography;
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
        // JsonUtility materializes null inline classes. Preserve optional tutorial presence explicitly.
        public int tutorialPresenceVersion;
        public bool hasTutorial;
        // v2 only; null is retained for v1, never synthesized from a catalog.
        public NativeOwnedRoster ownedRoster;
        public bool hasOwnedRoster;
        public int[] scenarioGeneralReceipts;
        public bool hasInventory;
        public NativeInventory inventory;
        public bool hasScenario01;
        public DarkSaver.Events.Scenario01Progress scenario01;
        public DarkSaver.Events.Scenario01Progress[] campaign;
    }

    // Native single-player state only; no original file references or server formulas.
    public sealed class NativeSaveStore
    {
        public const string Format = "DarkSaverWorld.NativeSave";
        public const int Version = 1;
        public const int RosterVersion = 2;
        public const int ScenarioVersion = 3;
        public const int CampaignVersion = 4;
        private const string CheckedFormat = "DarkSaverWorld.NativeSave.Checked";
        [Serializable]
        private sealed class SaveEnvelope
        {
            public string format;
            public int version;
            public string payload;
            public string sha256;
        }
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "native-save-v1.json");
        public string FilePath { get; }
        private static NativeSaveData pending;
        private static string pendingPath;

        public NativeSaveStore(string filePath) { FilePath = Path.GetFullPath(filePath); }

        public static bool IsValid(NativeSaveData data)
        {
            if (data == null || data.format != Format || (data.version != Version && data.version != RosterVersion && data.version != ScenarioVersion && data.version != CampaignVersion) ||
                !IsSupportedMap(data.mapScene, data.runtimeScene) ||
                float.IsNaN(data.worldX) || float.IsInfinity(data.worldX) ||
                float.IsNaN(data.worldY) || float.IsInfinity(data.worldY) ||
                data.clearedScenarios == null || data.clearedScenarios.Length > 10000) return false;
            if(data.scenarioGeneralReceipts!=null && (data.scenarioGeneralReceipts.Length>70
                || Array.Exists(data.scenarioGeneralReceipts,id=>id<100 || id>169)
                || new System.Collections.Generic.HashSet<int>(data.scenarioGeneralReceipts).Count!=data.scenarioGeneralReceipts.Length
                || !data.hasCharacter && data.scenarioGeneralReceipts.Length>0))return false;
            // Unity JSON may materialize a null inline class as an empty object.
            if (data.hasOwnedRoster ? data.version == Version || data.ownedRoster == null || !data.ownedRoster.IsValid
                : data.version == RosterVersion || (data.ownedRoster?.units?.Length ?? 0) != 0) return false;
            if (data.hasVitals && (data.maxHp <= 0 || data.hp < 0 || data.hp > data.maxHp ||
                data.maxMp < 0 || data.mp < 0 || data.mp > data.maxMp || data.lifeState < 0 || data.lifeState > 2 ||
                (data.lifeState != 0 && data.hp > 0))) return false;
            if (data.hasCharacter && (data.character == null || !data.character.IsValid)) return false;
            if (data.hasInventory ? !data.hasCharacter || data.inventory == null || !data.inventory.IsValid
                : data.inventory != null && (data.inventory.gold != 0 || (data.inventory.items?.Length ?? 0) != 0)) return false;
            if(data.hasCharacter && !data.character.IsEquipmentOwned(data.hasInventory ? data.inventory : null))return false;
            if (data.hasScenario01 && (!data.hasCharacter || !data.hasInventory || data.scenario01 == null || !data.scenario01.IsValid)) return false;
            if (data.hasScenario01 && data.version < ScenarioVersion || data.version == ScenarioVersion && !data.hasScenario01) return false;
            if (data.version == CampaignVersion)
            {
                if (!data.hasCharacter || !data.hasInventory || data.campaign == null || data.campaign.Length == 0 || data.campaign.Length > ScenarioCatalog.All.Count-1) return false;
                var scenarioKeys = new HashSet<string>();
                foreach (var state in data.campaign)
                    if (state == null || state.version != 2 || !state.IsValid || !ScenarioCatalog.TryGet(state.scenarioKey, out var definition) || definition.Key == "01"
                        || !scenarioKeys.Add(state.scenarioKey) || (state.phase == DarkSaver.Events.Scenario01Progress.Complete) != Array.Exists(data.clearedScenarios, k => k == state.scenarioKey)) return false;
            }
            else if ((data.campaign?.Length ?? 0) != 0) return false;
            if (data.hasScenario01 && (data.scenario01.phase == DarkSaver.Events.Scenario01Progress.Complete)
                != Array.Exists(data.clearedScenarios, key => key == "01")) return false;
            if (!data.hasScenario01 && data.scenario01 != null && !string.IsNullOrEmpty(data.scenario01.sourceHash)) return false;
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
            return KaosiaFacilityCatalog.IsPair(map,runtime) || NativeInteriorCatalog.IsPair(map,runtime) || ShopInteriorCatalog.IsPair(map,runtime) || ScenarioCatalog.IsPair(map, runtime) || TownCatalog.IsPair(map, runtime) ||
            FieldCatalog.IsPair(map, runtime) ||
            (map == "Brugos01_Map" && runtime == "Brugos01_Runtime");
        }

        public bool TryLoad(out NativeSaveData data, out string message)
        {
            if (TryRead(FilePath, out data, out var future)) { message = string.Empty; return true; }
            // An unknown schema must not silently resume an older campaign.
            if (future) { message = "새로운 버전의 저장 파일입니다."; return false; }
            if (TryRead(FilePath + ".bak", out data, out _))
            { message = "이전 정상 백업을 불러왔습니다."; return true; }
            message = File.Exists(FilePath) ? "저장 데이터가 손상되었거나 형식이 맞지 않습니다." : "저장된 게임이 없습니다.";
            return false;
        }

        private static string Checksum(string payload)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var hex = new StringBuilder(64);
            foreach (var value in bytes) hex.Append(value.ToString("x2"));
            return hex.ToString();
        }

        private static bool TryRead(string path, out NativeSaveData data, out bool future)
        {
            data = null; future = false;
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length > 1024 * 1024) return false;
                var json = File.ReadAllText(path, Encoding.UTF8);
                var header = JsonUtility.FromJson<SaveEnvelope>(json);
                if (header == null) return false;
                if (header.format == CheckedFormat)
                {
                    if (header.version != 1) { future = true; return false; }
                    if (header.payload == null || header.sha256 != Checksum(header.payload)) return false;
                    json = header.payload;
                }
                var candidate = JsonUtility.FromJson<NativeSaveData>(json);
                RestoreTutorialPresence(candidate, json);
                if (candidate != null && candidate.format == Format && candidate.version > CampaignVersion)
                { future = true; return false; }
                if (!IsValid(candidate)) return false;
                data = candidate; return true;
            }
            catch (Exception) { return false; }
        }

        public bool TrySave(NativeSaveData data, out string message)
        {
            message = "저장할 상태가 올바르지 않습니다.";
            if (!IsValid(data)) return false;
            string temporary = null;
            try
            {
                // Preserve an unreadable/unknown-version slot rather than overwriting it.
                var primaryValid = TryRead(FilePath, out var previous, out var future);
                var recovered = false;
                if (File.Exists(FilePath) && !primaryValid)
                {
                    recovered = !future && TryRead(FilePath + ".bak", out previous, out _);
                    if (!recovered)
                    { message = "기존 저장 파일에 오류가 있어 덮어쓰지 않았습니다."; return false; }
                }
                else if (!File.Exists(FilePath))
                {
                    // A missing primary can still have a newer valid roster backup.
                    var backupValid = TryRead(FilePath + ".bak", out previous, out var backupFuture);
                    if (backupFuture || (File.Exists(FilePath + ".bak") && !backupValid))
                    { message = "기존 백업의 오류 또는 새로운 버전을 확인해야 합니다."; return false; }
                }
                // Never silently drop a newer roster through a legacy writer.
                if (previous != null && previous.version > data.version)
                { message = "보유 목록이 있는 저장을 이전 버전으로 덮어쓰지 않았습니다."; return false; }
                var payload = SerializePayload(data);
                var json = JsonUtility.ToJson(new SaveEnvelope
                { format = CheckedFormat, version = 1, payload = payload, sha256 = Checksum(payload) }, true);
                if (Encoding.UTF8.GetByteCount(json) > 1024 * 1024)
                { message = "저장 데이터가 너무 큽니다."; return false; }
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                temporary = FilePath + ".tmp-" + Guid.NewGuid().ToString("N");
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var bytes = new UTF8Encoding(false).GetBytes(json);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                // Never replace a good backup with the corrupt primary during recovery.
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, recovered ? null : FilePath + ".bak");
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
            string payload = SerializePayload(data);
            pending = JsonUtility.FromJson<NativeSaveData>(payload);
            RestoreTutorialPresence(pending, payload);
            pendingPath = fullPath;
            return true;
        }

        public static bool TakeResume(out NativeSaveData data, out string filePath)
        {
            data = pending; filePath = pendingPath; ClearResume();
            return data != null;
        }

        private static string SerializePayload(NativeSaveData data)
        {
            data.tutorialPresenceVersion = 1;
            data.hasTutorial = data.hasCharacter && data.character?.tutorial != null;
            return JsonUtility.ToJson(data);
        }

        private static void RestoreTutorialPresence(NativeSaveData data, string json)
        {
            if (data?.character == null) return;
            // Older explicit tutorial objects retain their progress; absent fields never start onboarding.
            bool present = data.tutorialPresenceVersion == 1 ? data.hasTutorial
                : System.Text.RegularExpressions.Regex.IsMatch(json, @"(?<!\\)""tutorial""\s*:\s*\{");
            if (!present) data.character.tutorial = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ClearResume() { pending = null; pendingPath = null; }
    }
}
