using System;
using System.Collections.Generic;
using DarkSaver.Magic;
using DarkSaver.Network;

namespace DarkSaver.Combat
{
    // CheckMagicTarget / ally3 / ally4. Pure assembly, never approval or damage.
    public static class CombatMagicTargets
    {
        public static bool TryAllowsTeam(long flags, int sourceTeam, int targetTeam, int? currentTeam,
            IReadOnlyList<int> relations, out bool allowed)
        {
            allowed = false;
            var same = sourceTeam == targetTeam;
            var bothNpc = sourceTeam >= 8 && sourceTeam <= 15 && targetTeam >= 8 && targetTeam <= 15;
            var high = sourceTeam > 15 || targetTeam > 15;
            var ally = same || high || bothNpc;
            var opponent = !same && !high && !bothNpc;
            if (!same && !high && !bothNpc && sourceTeam >= 0 && sourceTeam < 8 && targetTeam >= 0 && targetTeam < 8)
            {
                if (!currentTeam.HasValue && (flags & 3) != 0) return false;
                var other = sourceTeam == currentTeam ? targetTeam : targetTeam == currentTeam ? sourceTeam : -1;
                if (other >= 0 && (flags & 3) != 0)
                {
                    if (relations == null || relations.Count != 8) return false;
                    var state = relations[other];
                    ally = state == 1 || state == 2;
                    opponent = state == 1 || state == 4;
                }
            }
            allowed = ((flags & 1) != 0 && ally) || ((flags & 2) != 0 && opponent);
            return true;
        }

        // null callback values mean unknown, not absent/hostile/dead.
        // recordEligible must explicitly supply the original +0/+4 nonzero check;
        // it must not silently substitute CombatUnit.Alive.
        public static bool TryBuildRequest(MagicCatalogEntry magic, MagicRequest command,
            CombatGridCell caster, CombatGridCell clicked, int width, int height,
            Func<CombatGridCell, short?> occupant, Func<short, bool?> recordEligible,
            Func<short, int?> teamId, int? currentTeam, IReadOnlyList<int> relations,
            out MagicRequest packet, out string reason)
        {
            packet = null; reason = "마법 대상 정보를 확정할 수 없습니다.";
            if (magic == null || command == null || magic.Id != command.MagicId || command.TargetCount != 0
                || width <= 0 || height <= 0 || occupant == null || recordEligible == null) return false;
            var targets = new List<short>(); var reference = (int)command.CasterUnitId;
            short x, y;
            if (magic.Unit == 8)
            {
                if (!InBounds(clicked, width, height)) return false;
                var member = occupant(clicked);
                if (!member.HasValue || member < 0) return false;
                var commander = member.Value / 10 * 10;
                if (commander + 8 > short.MaxValue) return false;
                for (var i = 0; i < 9; i++)
                {
                    var id = (short)(commander + i); var eligible = recordEligible(id);
                    if (!eligible.HasValue) return false;
                    if (eligible.Value) targets.Add(id);
                }
                x = -1; y = -1;
            }
            else
            {
                if (clicked.X < short.MinValue || clicked.X > short.MaxValue
                    || clicked.Y < short.MinValue || clicked.Y > short.MaxValue) return false;
                if (reference == -3) reference = 700;
                else if (reference == -2)
                {
                    if (!currentTeam.HasValue || (long)currentTeam.Value * 50 < 0
                        || (long)currentTeam.Value * 50 > short.MaxValue) return false;
                    reference = currentTeam.Value * 50;
                }
                if (reference < -1) return false;
                for (var ix = 0; ix < width; ix++)
                    for (var iy = 0; iy < height; iy++)
                    {
                        var cell = new CombatGridCell(ix, iy);
                        if (!CombatMagicArea.TryGetWeight(magic, caster, clicked, cell, width, height, out var weight)) return false;
                        if (weight <= 0) continue;
                        var id = occupant(cell);
                        if (!id.HasValue || id < 0) continue;
                        if (reference != -1)
                        {
                            // With neither bit set, the original clears this cell without reading teams.
                            if ((magic.Team & 3) == 0) continue;
                            var sourceTeam = teamId?.Invoke((short)reference); var targetTeam = teamId?.Invoke(id.Value);
                            if (!sourceTeam.HasValue || !targetTeam.HasValue
                                || !TryAllowsTeam(magic.Team, sourceTeam.Value, targetTeam.Value, currentTeam, relations, out var allowed)) return false;
                            if (!allowed) continue;
                        }
                        var eligible = recordEligible(id.Value);
                        if (!eligible.HasValue) return false;
                        if (!eligible.Value) continue;
                        if (targets.Count == byte.MaxValue) { reason = "마법 대상 수가 로컬 패킷 한도를 초과했습니다."; return false; }
                        targets.Add(id.Value); // Original cell order, no sorting/deduplication.
                    }
                if (targets.Count == 0) { reason = "일반 마법에 유효한 대상이 없습니다."; return false; }
                x = (short)clicked.X; y = (short)clicked.Y;
            }
            packet = new MagicRequest(command.MagicId, (short)reference, x, y, command.ComputedCost,
                targets.ToArray(), command.OriginalOpcode, command.Mode, command.ActingUnitId);
            reason = ""; return true;
        }

        private static bool InBounds(CombatGridCell cell, int width, int height) =>
            cell.X >= 0 && cell.Y >= 0 && cell.X < width && cell.Y < height;
    }
}
