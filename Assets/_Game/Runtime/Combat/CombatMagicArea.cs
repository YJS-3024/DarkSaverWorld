using System;
using DarkSaver.Magic;

namespace DarkSaver.Combat
{
    // userunit.dll CheckMagicTarget(0x10001D80), normal SetArea(0x10004BA0).
    // Geometry only: no team/validity filtering, spell approval, AP/MP or damage.
    public static class CombatMagicArea
    {
        // CheckMagicTarget: setarea6 (0x100016D0), setarea8 (0x10001930).
        // Negative weights are intentional: only positive cells become targets.
        public static bool TryGetWeight(MagicCatalogEntry magic, CombatGridCell caster,
            CombatGridCell clicked, CombatGridCell target, int width, int height, out int weight)
        {
            weight = 0;
            if (magic == null) return false;
            if (magic.Range < -6 || magic.Range > -1)
                return TryGetStandardWeight(magic, caster, clicked, target, width, height, out weight);
            if (magic.Unit == 8 || magic.Area < int.MinValue || magic.Area >= int.MaxValue
                || width <= 0 || height <= 0 || !InBounds(caster, width, height)
                || !InBounds(target, width, height)) return false;
            var dx = Math.Abs((long)target.X - caster.X);
            var dy = Math.Abs((long)target.Y - caster.Y);
            if (magic.Range == -6)
            {
                // setareac (0x10001C80): integer fixed-point DDA, not Bresenham.
                if (!InBounds(clicked, width, height)) return false;
                var deltaX = (long)clicked.X - caster.X; var deltaY = (long)clicked.Y - caster.Y;
                var steps = Math.Max(Math.Abs(deltaX), Math.Abs(deltaY));
                if (steps == 0) return true;
                var incrementX = deltaX * 100 / steps; var incrementY = deltaY * 100 / steps;
                long fixedX = -50, fixedY = -50;
                for (long i = 0; i <= steps; i++, fixedX += incrementX, fixedY += incrementY)
                {
                    var x = caster.X + (fixedX + (incrementX > 0 ? 100 : 0)) / 100;
                    var y = caster.Y + (fixedY + (incrementY > 0 ? 100 : 0)) / 100;
                    if (target.X == x && target.Y == y) { weight = 1; break; }
                }
                return true;
            }
            if (magic.Range == -1 || magic.Range == -3 || magic.Range == -4)
            {
                if (magic.Area < 0) return true;
                // Direction is the original clicked-X sentinel, not a vector.
                var horizontal = clicked.X != -4 && clicked.X != -3;
                var positive = clicked.X != -3 && clicked.X != -2;
                var step = horizontal ? (long)target.X - caster.X : (long)target.Y - caster.Y;
                if (!positive) step = -step;
                var across = horizontal ? (long)target.Y - caster.Y : (long)target.X - caster.X;
                if (step < 1 || step > magic.Area + 1) return true;
                var remaining = magic.Area - step + 1;
                if (magic.Range == -1) { if (across == 0) weight = (int)remaining; return true; }
                var perpendicular = horizontal ? caster.Y : caster.X;
                // Native negative side is written only when perpendicular > 1.
                if (across == 1 || (across == -1 && perpendicular > 1))
                    weight = (int)(remaining - (magic.Range == -4 ? 1 : 0));
                else if (across == 0 && magic.Range == -4) weight = (int)remaining;
                return true;
            }
            if (magic.Range == -2)
            {
                if (dx == 0 || dy == 0) weight = (int)Math.Max(0, magic.Area - dx - dy);
                return true;
            }
            var radius = magic.Area + 1;
            var distance = dx + dy;
            if (radius < 0 || distance > radius) return true;
            weight = distance == radius || (distance & 1) == (radius & 1) ? -1 : 1;
            return true;
        }

        public static bool TryGetStandardWeight(MagicCatalogEntry magic, CombatGridCell caster,
            CombatGridCell clicked, CombatGridCell target, int width, int height, out int weight)
        {
            weight = 0;
            if (magic == null || magic.Unit == 8 || magic.Range < int.MinValue || magic.Range > int.MaxValue
                || (magic.Range >= -6 && magic.Range <= -1)
                || magic.Area < int.MinValue || magic.Area >= int.MaxValue
                || width <= 0 || height <= 0 || !InBounds(target, width, height)) return false;
            // Positive Range uses the supplied point; other normal cases use caster.
            // This is not a check that the clicked point lies within casting range.
            var center = magic.Range > 0 ? clicked : caster;
            if (!InBounds(center, width, height)) return false;
            var radius = Math.Max(0L, magic.Area);
            var distance = Math.Abs((long)target.X - center.X) + Math.Abs((long)target.Y - center.Y);
            weight = (int)Math.Max(0L, radius + 1 - distance);
            return true;
        }
        private static bool InBounds(CombatGridCell cell, int width, int height) =>
            cell.X >= 0 && cell.Y >= 0 && cell.X < width && cell.Y < height;
    }
}
