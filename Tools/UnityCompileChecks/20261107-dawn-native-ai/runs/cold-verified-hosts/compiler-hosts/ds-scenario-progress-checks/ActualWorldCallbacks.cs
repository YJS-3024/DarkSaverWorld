using System;using DarkSaver.Original.World;namespace DarkSaver.Original.Entry {public sealed partial class LocalServerManager {private WorldReply WorldRequest(Func<EntryAccountService, WorldReply> operation, bool entered = false)
        {
            var reply = isActiveAndEnabled && service != null ? operation(service) : new WorldReply(WorldFailure.Unauthenticated);
            Notify_World?.Invoke(reply); if (entered && IsCurrentWorldReply(reply)) Notify_MapEntered?.Invoke(reply); return reply;
        }private bool IsCurrentWorldReply(WorldReply reply)
        {
            var current=WorldSnapshot;var expected=reply?.View;
            return reply?.Accepted==true&&current!=null&&expected!=null&&current.characterId==expected.characterId
                &&current.commanderSession==expected.commanderSession&&current.campaignSession==expected.campaignSession&&current.record.map==expected.record.map;
        }private void PublishNativeScenarioReturn(NativeScenarioReturn delivery,bool requestLeave=false)
        {
            // Saver414640 writes root5B before restoring the saved outside map.
            // This is its host leave notification, not a guessed server exit result.
            if(requestLeave&&delivery.Matches(WorldSnapshot))Notify_BattlePacketRequested?.Invoke(new byte[]{0x5b});
            if(delivery.Matches(WorldSnapshot))Notify_MapEntered?.Invoke(new WorldReply(WorldFailure.None,WorldSnapshot));
            if(delivery.Matches(WorldSnapshot))Notify_ScenarioExited?.Invoke(new WorldReply(WorldFailure.None,WorldSnapshot));
        }}}