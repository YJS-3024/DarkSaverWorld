using UnityEngine;

namespace DarkSaver.Combat
{
    public sealed partial class BattleMapPresentation
    {
        // The confirmed grid position, not the interpolated sprite position.
        // A known terrain attribute does not imply that the unit's troop column is known.
        public bool TryGetSelectedTerrain(out short attribute, out int? movement, out Sprite image)
        {
            attribute = -1; movement = null; image = null;
            if (!isActiveAndEnabled || !IsBound || battle == null || ground == null
                || !battle.TryGetSelectedUnit(out var unit)) return false;
            var cell = new CombatGridCell(unit.X, unit.Y);
            var native = new Vector3Int(cell.X, bounds.yMax - 1 - cell.Y, 0);
            if (!bounds.Contains(native) || !ground.HasTile(native)
                || !TryGetAttribute(unit, out var value) || value >= CombatTerrainTraversal.TerrainCount) return false;
            attribute = value;
            image = ground.GetSprite(native);
            if (TryGetMovementAtCell(unit.Id, cell, out var cost)) movement = cost;
            return true;
        }

        private void DrawSelectedTerrainInfo(Rect rect)
        {
            TryGetSelectedTerrain(out _, out var movement, out var image);
            ui.Portrait("terrain-cell-preview", new Rect(rect.x + 10, rect.y + 35, 32, 32), image);
            string text = !movement.HasValue ? string.Empty : movement.Value == CombatTerrainTraversal.ImpassableValue
                ? "불가" : movement.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ui.BattleLabel("terrain-move-value", new Rect(rect.x + 80, rect.y + 68, 29, 16), text, 12, singleLine: true);
            ui.Raise("terrain-cell-preview"); ui.Raise("terrain-move-value");
        }

        private string SelectedTerrainNote => TryGetSelectedTerrain(out var attribute, out _, out _)
            ? $"현재 칸 지형 #{attribute} · 이동값은 AP 비용 아님 · 공격/방어 미확정"
            : "현재 칸 지형 정보 없음 · 공격/방어 미확정";
    }
}
