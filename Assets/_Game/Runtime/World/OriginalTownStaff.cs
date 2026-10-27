using UnityEngine;
namespace DarkSaver.World
{
    // Native artwork with per-entry evidence; includes explicitly user-authored placements.
    public sealed class OriginalTownStaff:MonoBehaviour
    {
        public OriginalInteriorStaffEntry Source { get; private set; }
        public void Configure(OriginalInteriorStaffEntry source)=>Source=source;
    }
}
