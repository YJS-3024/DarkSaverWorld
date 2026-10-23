using UnityEngine;

namespace DarkSaver.Combat
{
    // Native view data, not the original state-bit layout or a character FSM.
    public readonly struct BattleAttackPresentation
    {
        public BattleAttackPresentation(short attackerId, short targetId, short effect,
            Vector2Int attackerFacing, bool isAnticipation)
        {
            AttackerId = attackerId; TargetId = targetId; Effect = effect;
            AttackerFacing = attackerFacing; IsAnticipation = isAnticipation;
        }

        public short AttackerId { get; }
        public short TargetId { get; }
        public short Effect { get; } // Raw, not damage.
        public Vector2Int AttackerFacing { get; } // Unity screen-facing axes, not original row IDs.
        public Vector2Int TargetFacing => -AttackerFacing;
        public bool HasDirection => AttackerFacing != Vector2Int.zero;
        public bool IsAnticipation { get; }
    }
}
