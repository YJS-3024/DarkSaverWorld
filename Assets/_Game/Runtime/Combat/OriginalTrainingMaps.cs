using System;

namespace DarkSaver.Combat
{
    // Known native map resources only. Neither class assignment nor spawn policy.
    public static class OriginalTrainingMaps
    {
        public static bool Contains(int mapId) => mapId >= 101 && mapId <= 107;
        public static bool TryGetMapId(string sceneName, out int mapId)
        {
            mapId = 0;
            for (int id = 101; id <= 107; id++)
                if (string.Equals(sceneName, "Map_" + id + "_;", StringComparison.Ordinal))
                { mapId = id; return true; }
            return false;
        }
    }
}
