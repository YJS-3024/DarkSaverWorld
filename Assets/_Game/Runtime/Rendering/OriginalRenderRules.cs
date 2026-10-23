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
