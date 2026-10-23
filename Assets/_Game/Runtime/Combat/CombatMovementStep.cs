namespace DarkSaver.Combat
{
    // Native internal confirmation, NOT a recovered MoveCharPath packet layout.
    public readonly struct CombatMovementStep
    {
        public CombatMovementStep(short unitId, CombatGridCell from, CombatGridCell to, System.Guid reservationId)
        { UnitId = unitId; From = from; To = to; ReservationId = reservationId; }
        public short UnitId { get; }
        public CombatGridCell From { get; }
        public CombatGridCell To { get; }
        public System.Guid ReservationId { get; } // Native-only token, not an original sequence field.
    }
}
