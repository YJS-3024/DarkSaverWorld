using System;
namespace DarkSaver.Core
{
    [Serializable] public sealed class WorldAudioRegion {public string map,clip,midi,inheritedFrom;public bool authored;}
    [Serializable] public sealed class WorldAudioSound {public int id;public string clip;}
    [Serializable] public sealed class WorldAudioJob {public int job,appearance;}
    [Serializable] public sealed class WorldAudioData
    {
        public int version;public WorldAudioRegion[] regions;public WorldAudioSound[] sounds;public WorldAudioJob[] jobs;
        public WorldAudioRegion FindRegion(string map){if(regions!=null)foreach(var row in regions)if(row!=null && row.map==map)return row;return null;}
        public string FindSound(int id){if(sounds!=null)foreach(var row in sounds)if(row!=null && row.id==id)return row.clip;return null;}
        public bool HasDeathSound(int job)
        {if(jobs!=null)foreach(var row in jobs)if(row!=null && row.job==job)return row.appearance<700 || row.appearance>799;return false;}
    }
}
