using System.Collections.Generic;
using DarkSaver.Magic;
using DarkSaver.Network;
using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        private bool specialMenuActive;
        private readonly Dictionary<short, MagicChoices> specialChoices = new();
        // GameCtl 0x100096AF..0x100096CF. This classifies menu entries, not ownership.
        public static bool IsNativeSpecialMenuId(int id) => id >= 10000 && id < 20000 || id >= 30000 && id < 32000;
        // Visual evidence only. A mapped renderer is not approval, a learned skill, or a damage/status formula.
        public bool TryGetSpecialVisualMapping(MagicCatalog catalog, int id, out long patternId, out bool mapped)
        {
            patternId = 0; mapped = false;
            if (!isActiveAndEnabled || !IsBound || catalog == null || !IsNativeSpecialMenuId(id)
                || !catalog.TryGet(id, out var entry)) return false;
            patternId = entry.PatternId;
            projectileCatalog ??= Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeProjectileEffects");
            thunderCatalog ??= Resources.Load<NativeThunderEffects>("Effects/NativeThunderEffectsV2");
            linearProjectileCatalog ??= Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeLinearProjectileEffects");
            curvedDirectionalCatalog ??= Resources.Load<NativeProjectileEffectsCatalog>("Effects/NativeCurvedDirectionalEffects");
            mapped = projectileCatalog != null && projectileCatalog.TryGet(id, out _)
                || thunderCatalog != null && thunderCatalog.Supports(id) || SupportsTornado(id) || SupportsNativeTargetEffect(id)
                || linearProjectileCatalog != null && linearProjectileCatalog.TryGet(id, out _)
                || curvedDirectionalCatalog != null && curvedDirectionalCatalog.TryGet(id, out _) || SupportsCrossTrust(id);
            return true;
        }
        public int SpecialChoiceCount => battle != null && battle.TryGetSelectedUnit(out var unit)
            ? learnedMagic.TryGetValue(unit.Id, out var known) ? known.SpecialIds.Length
                : specialChoices.TryGetValue(unit.Id, out var choices) ? choices.Commands.Length : 0
            : 0;

        // Explicit, confirmed per-caster commands only. Never populate from the whole catalog.
        // Existing learned-ability validation remains authoritative at selection and casting.
        public bool ConfigureSpecialChoices(MagicCatalog catalog, short casterId, IReadOnlyList<MagicRequest> commands,
            System.Func<short, bool?> eligible, System.Func<short, int?> teamId, int? currentTeam, IReadOnlyList<int> relations)
        {
            if (!IsBound || catalog == null || commands == null || eligible == null
                || relations != null && relations.Count != 8 || !battle.TryGetUnit(casterId, out var caster) || caster.Enemy) return false;
            var copy = new MagicRequest[commands.Count]; var ids = new HashSet<short>();
            for (int i = 0; i < copy.Length; i++)
            {
                var command = commands[i];
                if (command == null || command.ActingUnitId != casterId || command.TargetCount != 0
                    || !IsNativeSpecialMenuId(command.MagicId) || !catalog.TryGet(command.MagicId, out _)
                    || !ids.Add(command.MagicId) || !battle.IsLearnedMagicAllowed(casterId, command.MagicId)) return false;
                copy[i] = command;
            }
            int[] relationCopy = null;
            if (relations != null) { relationCopy = new int[8]; for (int i = 0; i < 8; i++) relationCopy[i] = relations[i]; }
            if ((specialMenuActive && magicChoiceCaster == casterId) || pendingMagic?.ActingUnitId == casterId)
                CancelMagicTargeting();
            specialChoices[casterId] = new MagicChoices { Catalog = catalog, Commands = copy, Eligible = eligible,
                TeamId = teamId, CurrentTeam = currentTeam, Relations = relationCopy };
            return true;
        }

        private bool TryGetActiveMagicChoices(short casterId, out MagicChoices choices)
            => (specialMenuActive ? specialChoices : magicChoices).TryGetValue(casterId, out choices);
    }
}
