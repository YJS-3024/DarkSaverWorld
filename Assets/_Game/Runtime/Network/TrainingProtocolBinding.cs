using System;
using System.Collections.Generic;

namespace DarkSaver.Network
{
    // Authority owns authentication, character/location, session ownership,
    // loading state, revision ordering, spawn rules and retreat approval.
    // Binding these contracts does not supply any of those missing rules.
    public interface ITrainingServerAuthority
    {
        Response_EnterTraining Enter(Request_EnterTraining request);
        Response_ReadyTraining Ready(Request_ReadyTraining request);
        Response_GetTrainingState State(Request_GetTrainingState request);
        Response_RetreatTraining Retreat(Request_RetreatTraining request);
    }
    public sealed class TrainingProtocolBinding : IDisposable
    {
        private const string Missing = "훈련장 서버 규칙이 연결되지 않았습니다.";
        private readonly List<IDisposable> registrations = new();
        public ITrainingServerAuthority Authority { get; }
        public TrainingProtocolBinding(LocalServerDispatcher dispatcher, ITrainingServerAuthority authority = null)
        {
            if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));
            Authority = authority;
            try
            {
                registrations.Add(dispatcher.Register<Request_EnterTraining>(p => authority?.Enter(p) ?? new Response_EnterTraining(Missing)));
                registrations.Add(dispatcher.Register<Request_ReadyTraining>(p => p.SessionId > 0
                    ? authority?.Ready(p) ?? new Response_ReadyTraining(p.SessionId, false, Missing)
                    : new Response_ReadyTraining(p.SessionId, false, "유효한 훈련 세션이 필요합니다.")));
                registrations.Add(dispatcher.Register<Request_GetTrainingState>(p => p.SessionId > 0
                    ? authority?.State(p) ?? new Response_GetTrainingState(Missing)
                    : new Response_GetTrainingState("유효한 훈련 세션이 필요합니다.")));
                registrations.Add(dispatcher.Register<Request_RetreatTraining>(p => p.SessionId > 0
                    ? authority?.Retreat(p) ?? new Response_RetreatTraining(Missing)
                    : new Response_RetreatTraining("유효한 훈련 세션이 필요합니다.")));
            }
            catch { Dispose(); throw; }
        }
        public void Dispose()
        {
            for (int i = registrations.Count - 1; i >= 0; i--) registrations[i].Dispose();
            registrations.Clear();
        }
    }
}
