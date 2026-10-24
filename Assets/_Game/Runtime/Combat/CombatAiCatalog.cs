using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkSaver.Combat
{
    [Serializable]
    public struct CombatAiAction
    {
        [SerializeField] private string kind, token;
        [SerializeField] private bool hasMagic;
        [SerializeField] private int magicId, weight;
        public CombatAiAction(string kind, string token, int? magicId, int weight)
        {
            if (weight < 0) throw new ArgumentOutOfRangeException(nameof(weight));
            this.kind = kind; this.token = token; hasMagic = magicId.HasValue;
            this.magicId = magicId.GetValueOrDefault(); this.weight = weight;
        }
        public string Kind => kind;
        public string Token => token;
        public int? MagicId => hasMagic ? magicId : (int?)null;
        public int Weight => weight;
    }

    [Serializable]
    public sealed class CombatAiRule
    {
        [SerializeField] private int id;
        [SerializeField] private string mapKey, aiType, targetType;
        [SerializeField] private bool backup, hasMode, hasX, hasY, hasRadius, hasTargetUnit;
        [SerializeField] private short unitId, targetUnit;
        [SerializeField] private int mode, x, y, radius;
        [SerializeField] private CombatAiAction[] actions;

        public CombatAiRule(int id, string mapKey, bool backup, short unitId, string aiType, int? mode,
            string targetType, int? x, int? y, int? radius, short? targetUnit, CombatAiAction[] actions)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(mapKey) || actions == null || actions.Length == 0)
                throw new ArgumentException("Invalid AI rule.");
            this.id = id; this.mapKey = mapKey; this.backup = backup; this.unitId = unitId;
            this.aiType = aiType; this.targetType = targetType;
            hasMode = mode.HasValue; this.mode = mode.GetValueOrDefault();
            hasX = x.HasValue; this.x = x.GetValueOrDefault();
            hasY = y.HasValue; this.y = y.GetValueOrDefault();
            hasRadius = radius.HasValue; this.radius = radius.GetValueOrDefault();
            hasTargetUnit = targetUnit.HasValue; this.targetUnit = targetUnit.GetValueOrDefault();
            this.actions = (CombatAiAction[])actions.Clone();
        }
        public int Id => id;
        public string MapKey => mapKey;
        public bool IsBackup => backup;
        public short UnitId => unitId;
        public string AiType => aiType;
        public int? ModeCode => hasMode ? mode : (int?)null;
        public string TargetType => targetType;
        public int? TargetX => hasX ? x : (int?)null;
        public int? TargetY => hasY ? y : (int?)null;
        public int? Radius => hasRadius ? radius : (int?)null;
        public short? TargetUnitId => hasTargetUnit ? targetUnit : (short?)null;
        public int ActionCount => actions.Length;
        public CombatAiAction GetAction(int index) => actions[index];
        // userunit.dll 0x10002D47: native parser, not the separate setsolai troop orders.
        public static int? NativeModeCode(string token) => token switch
        {
            "ODNORMAL" => 2, "ODATTACK" => 4, "ODDEFENCE" => 8, _ => null
        };
        public bool HasSupportedMode => NativeModeCode(aiType).HasValue && ModeCode == NativeModeCode(aiType);

        public bool TrySelectAction(int roll, out CombatAiAction action)
        {
            action = default;
            var weights = new int[actions.Length];
            for (var i = 0; i < actions.Length; i++) weights[i] = actions[i].Weight;
            if (!CombatAiActionSelection.TrySelect(weights, roll, out var index)) return false;
            action = actions[index]; return true;
        }

        public bool HasSameData(CombatAiRule other)
        {
            if (other == null || Id != other.Id || MapKey != other.MapKey || IsBackup != other.IsBackup
                || UnitId != other.UnitId || AiType != other.AiType || ModeCode != other.ModeCode
                || TargetType != other.TargetType || TargetX != other.TargetX || TargetY != other.TargetY
                || Radius != other.Radius || TargetUnitId != other.TargetUnitId || ActionCount != other.ActionCount) return false;
            for (var i = 0; i < ActionCount; i++) if (!actions[i].Equals(other.actions[i])) return false;
            return true;
        }
    }

    [CreateAssetMenu(menuName = "DarkSaver/Combat AI Catalog")]
    public sealed class CombatAiCatalog : ScriptableObject
    {
        [SerializeField] private CombatAiRule[] rules = Array.Empty<CombatAiRule>();
        public int Count => rules.Length;
        public CombatAiRule GetAt(int index) => rules[index];
        public void Initialize(CombatAiRule[] source)
        {
            if (source == null || source.Length == 0) throw new ArgumentException("Empty AI catalog.");
            var ordered = (CombatAiRule[])source.Clone();
            foreach (var rule in ordered) if (rule == null) throw new ArgumentException("Null AI rule.");
            Array.Sort(ordered, (a, b) => a.Id.CompareTo(b.Id));
            for (var i = 1; i < ordered.Length; i++)
                if (ordered[i - 1].Id == ordered[i].Id) throw new ArgumentException("Duplicate AI rule ID.");
            rules = ordered;
        }
        public bool TryGet(int id, out CombatAiRule rule)
        {
            var low = 0; var high = rules.Length - 1;
            while (low <= high)
            {
                var mid = low + (high - low) / 2;
                if (rules[mid].Id == id) { rule = rules[mid]; return true; }
                if (rules[mid].Id < id) low = mid + 1; else high = mid - 1;
            }
            rule = null; return false;
        }
        // Backup rules never silently replace active data; ambiguous active rules
        // require an explicit rule ID instead of selecting the first/last row.
        public bool TryGetUniqueActiveRule(string mapKey, short unitId, out CombatAiRule rule)
        {
            rule = null;
            foreach (var candidate in rules)
            {
                if (candidate.IsBackup || candidate.MapKey != mapKey || candidate.UnitId != unitId) continue;
                if (rule != null) { rule = null; return false; }
                rule = candidate;
            }
            return rule != null;
        }
    }
}
