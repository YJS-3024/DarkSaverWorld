using System;
using DarkSaver.Combat;

namespace DarkSaver.Network
{
    [Serializable] public sealed class TrainingTravelSettings
    {
        public int version, mapId;
        public string provenance;
        public float doorX, doorY;
        public int returnX, returnY;
        public TrainingTravelSettings Clone() => (TrainingTravelSettings)MemberwiseClone();
        public bool IsValid => version == 1 && OriginalTrainingMaps.Contains(mapId)
            && !float.IsNaN(doorX) && !float.IsInfinity(doorX) && !float.IsNaN(doorY) && !float.IsInfinity(doorY)
            && doorX >= 0 && doorX < 43 && doorY >= 0 && doorY < 43
            && returnX >= 0 && returnX < 43 && returnY >= 0 && returnY < 43;
    }
    public readonly struct TrainingTravelContext
    {
        public readonly string Region;
        public readonly float X, Y;
        public readonly bool Authenticated, Blocked;
        public TrainingTravelContext(string region, float x, float y, bool authenticated, bool blocked)
        { Region=region; X=x; Y=y; Authenticated=authenticated; Blocked=blocked; }
    }
    // User-authorized local map visit only. No enemy, combat, reward or respawn policy.
    public sealed class TrainingTravelService : ITrainingServerAuthority
    {
        private enum Phase { None, Entering, Active, Retreating }
        private Phase phase;
        private int sequence, sessionId;
        private readonly TrainingTravelSettings settings;
        private readonly Func<TrainingTravelContext> context;
        public int SessionId => sessionId;
        public int MapId => settings.mapId;
        public TrainingTravelService(TrainingTravelSettings settings, Func<TrainingTravelContext> context)
        {
            if(settings==null || !settings.IsValid)throw new ArgumentException("Invalid training travel settings.");
            this.settings=settings.Clone();this.context=context ?? throw new ArgumentNullException(nameof(context));
        }
        internal bool TryGetResumePosition(out float x,out float y)
        {
            x=settings.returnX+.5f;y=43-1-settings.returnY+.5f;
            return sessionId>0 && (phase==Phase.Active || phase==Phase.Retreating) && context().Authenticated;
        }
        internal void AbortUncommittedEntry(int id)
        {if(id==sessionId && phase==Phase.Active){phase=Phase.None;sessionId=0;}}
        public Response_EnterTraining Enter(Request_EnterTraining request)
        {
            var state=context();float dx=state.X-settings.doorX,dy=state.Y-settings.doorY;
            if(request==null || phase!=Phase.None || !state.Authenticated || state.Blocked || state.Region!="Kaosia"
                || float.IsNaN(dx) || float.IsInfinity(dx) || float.IsNaN(dy) || float.IsInfinity(dy)
                || dx*dx+dy*dy>1.5f*1.5f || sequence==int.MaxValue)
                return new Response_EnterTraining("카오시아 훈련장 입구에서 요청해 주세요.");
            sessionId=++sequence;phase=Phase.Entering;
            // Central grid cell, not an inferred original server spawn.
            return new Response_EnterTraining(sessionId,settings.mapId,20/2,15/2);
        }
        public Response_ReadyTraining Ready(Request_ReadyTraining request)
        {
            if(request==null || request.SessionId!=sessionId || sessionId<=0 || (phase!=Phase.Entering && phase!=Phase.Retreating))
                return new Response_ReadyTraining(request?.SessionId ?? 0,false,"만료되었거나 준비 중이 아닌 훈련 세션입니다.");
            bool entering=phase==Phase.Entering;
            if(!request.Loaded || !context().Authenticated)
            {
                phase=entering?Phase.None:Phase.Active;
                if(entering)sessionId=0;
                return new Response_ReadyTraining(request.SessionId,false,"목적지 준비 실패로 현재 맵을 유지합니다.");
            }
            phase=entering?Phase.Active:Phase.None;
            if(!entering)sessionId=0;
            return new Response_ReadyTraining(request.SessionId,true);
        }
        public Response_GetTrainingState State(Request_GetTrainingState request)
        {
            var state=context();
            return request!=null && request.SessionId==sessionId && sessionId>0 && phase==Phase.Active && state.Authenticated
                && state.Region=="Training"+settings.mapId
                ? new Response_GetTrainingState(sessionId,settings.mapId,1,Array.Empty<CombatUnitSnapshot>())
                : new Response_GetTrainingState("활성 훈련 세션이 없습니다.");
        }
        public Response_RetreatTraining Retreat(Request_RetreatTraining request)
        {
            var state=context();
            if(request==null || request.SessionId!=sessionId || sessionId<=0 || phase!=Phase.Active
                || !state.Authenticated || state.Blocked || state.Region!="Training"+settings.mapId)
                return new Response_RetreatTraining("현재 훈련장에서 도망 요청을 처리할 수 없습니다.");
            phase=Phase.Retreating;
            return new Response_RetreatTraining(sessionId,"Kaosia_Map","Town_Runtime",settings.returnX,settings.returnY);
        }
    }
}
