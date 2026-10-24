namespace DarkSaver.Rendering
{
    public enum WorldRenderPass
    {
        UnitOccupancyUpdate = 0,
        CharacterBody = 1,
        MapForegroundSprite = 2,
        CharacterStatusOverlay = 3,
        UserInterface = 4
    }

    public static class OriginalRenderRules
    {
        // Unity implementation bands. Native ordering is draw-pass/record order;
        // these numbers are not values recovered from the original executable.
        public const int GroundSortingOrder = 0;
        public const int DetailSortingOrder = 10;
        public const int ForegroundSortingBase = 30000;
        public const int MaximumForegroundIndex = short.MaxValue - ForegroundSortingBase;

        public static int ForegroundSortingOrder(int sourceIndex)
        {
            if (sourceIndex < 0 || sourceIndex > MaximumForegroundIndex)
                throw new System.ArgumentOutOfRangeException(nameof(sourceIndex));
            return ForegroundSortingBase + sourceIndex;
        }

        public static bool IsNativeMapScene(string name)
        {
            if (DarkSaver.Core.KaosiaFacilityCatalog.IsMap(name) || DarkSaver.Core.NativeInteriorCatalog.IsMap(name) || name=="KaosiaWeaponShop_Map" || name=="KaosiaArmorShop_Map" || DarkSaver.Core.ScenarioCatalog.IsMap(name) || DarkSaver.Core.TownCatalog.IsMap(name) || DarkSaver.Core.FieldCatalog.IsMap(name) || name == "Brugos01_Map") return true;
            if (name == null || !name.StartsWith("Map_", System.StringComparison.Ordinal)) return false;
            int end = name.IndexOf('_', 4);
            return end > 4 && int.TryParse(name.Substring(4, end - 4),
                System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var id) && id > 0;
        }

        public static bool TryGetForegroundSortingOrder(string objectName, out int order)
        {
            order = 0;
            if (objectName == null || !objectName.StartsWith("Object_", System.StringComparison.Ordinal)
                || !int.TryParse(objectName.Substring(7), System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var index)
                || index < 0 || index > MaximumForegroundIndex) return false;
            order = ForegroundSortingOrder(index); return true;
        }

        public const int TileSize = 32;
        public const int WaterOverlayYOffset = GameConstants.WaterSurfaceYOffsetPixels;
        public const int WaterFrameMilliseconds = GameConstants.WaterSurfaceFrameMilliseconds;
        public static int WaterFrame(long elapsedMilliseconds) =>
            (int)(elapsedMilliseconds / System.Math.Max(1, WaterFrameMilliseconds)) & 1;

        public static bool IsWaterSubmergeAttribute(int rawAttribute)
        {
            var attribute = rawAttribute & 0x7F;
            return attribute == 0x06 || attribute == 0x14;
        }

        public static int WaterBodyVisiblePixels(int frameHeight) => frameHeight == 32 ? 25 : frameHeight == 64 ? 48 : 0;

        // These are original rendering metadata, not Combat.UnitLifeState values.
        public static bool ShouldShowWaterSubmerge(bool onWater, int unitId, int originalState) =>
            onWater && unitId != 899 && originalState != 1;
    }
}
