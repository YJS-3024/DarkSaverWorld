using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public Func<TrainingActionRequest,TrainingView,NativeBattleResponse> NativeBattleAuthority {get;set;}
        public Func<TrainingActionRequest,TrainingView,NativeBattleResponse> NativeTrainingAuthority {get;set;}
        public WorldReply Notify_BattleResponse(NativeBattleResponse response)=>BattleResult(s=>s.ReceiveNativeBattleResponse(response));
        public WorldReply Notify_TrainingResponse(NativeBattleResponse response)=>TrainingResult(s=>s.ReceiveNativeTrainingResponse(response));
        public bool HasBattleAuthorityFor(bool training,TrainingAction action)=>HasBattleAuthority(training)
            ||OriginalBattleResponses.Supports(action)&&(training?NativeTrainingAuthority??NativeBattleAuthority:NativeBattleAuthority)!=null;
        private Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> ActionAuthority(bool training,TrainingAction action)
        {
            var typed=training?TrainingAuthority??BattleAuthority:BattleAuthority;if(typed!=null)return typed;
            var native=training?NativeTrainingAuthority??NativeBattleAuthority:NativeBattleAuthority;
            if(native==null||!OriginalBattleResponses.Supports(action))return null;
            return (r,v)=>OriginalBattleResponses.Prepare(r,v,native(r.Copy(),v));
        }
        private Func<TrainingActionRequest,TrainingView,TrainingUnitsNotice> AutomationAuthority(bool training)
        {
            var typed=training?TrainingAuthority??BattleAuthority:BattleAuthority;if(typed!=null)return typed;
            if((training?NativeTrainingAuthority??NativeBattleAuthority:NativeBattleAuthority)==null)return null;
            return (r,v)=>ActionAuthority(training,r.action)?.Invoke(r,v);
        }
    }
}
