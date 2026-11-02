using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        public WorldReply ReceiveNativeBattleResponse(NativeBattleResponse response)
        {
            var ready=ReadyBattle(true);if(ready!=WorldFailure.None)return WorldResult(ready);
            var combat=world.Combat;if(combat?.Pending==null)return WorldResult(WorldFailure.StaleRequest);
            var notice=OriginalBattleResponses.Prepare(combat.Pending.Copy(),combat.Snapshot(),response);
            return notice==null?WorldResult(WorldFailure.InvalidInput):ReceiveBattleUnits(notice);
        }
        public WorldReply ReceiveNativeTrainingResponse(NativeBattleResponse response)
        {var ready=ReadyTraining(true);return ready==WorldFailure.None?ReceiveNativeBattleResponse(response):WorldResult(ready);}
        private static WorldFailure ValidateNativeBattleResponse(TrainingCombatSession combat,TrainingUnitsNotice notice,TrainingActionRequest request)
        {
            if(notice.nativeResponse==null)return WorldFailure.None;
            if(request==null||string.IsNullOrEmpty(notice.request))return WorldFailure.StaleRequest;
            var expected=OriginalBattleResponses.Prepare(request.Copy(),combat.Snapshot(),notice.nativeResponse);
            return OriginalBattleResponses.Matches(notice,expected)?WorldFailure.None:WorldFailure.InvalidInput;
        }
    }
}
