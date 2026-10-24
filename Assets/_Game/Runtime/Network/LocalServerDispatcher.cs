using System;
using System.Collections.Generic;

namespace DarkSaver.Network
{
    // Local protocol metadata is deliberately separate from recovered original opcodes.
    public class LocalCommandResponse : ServerPacket
    {
        public bool Accepted { get; }
        public string Reason { get; }
        public LocalCommandResponse(bool accepted, string reason = null)
        { Accepted = accepted; Reason = reason ?? string.Empty; }
    }
    public readonly struct LocalPacketTrace
    {
        public readonly long Sequence;
        public readonly LocalPacketDirection Direction;
        public readonly string PacketType;
        public LocalPacketTrace(long sequence, LocalPacket packet)
        { Sequence = sequence; Direction = packet.Direction; PacketType = packet.GetType().Name; }
    }

    // One authority per request type. Observation events cannot become competing servers.
    // Synchronous delivery preserves existing main-thread callers; no socket/thread is needed.
    public sealed class LocalServerDispatcher
    {
        private readonly Dictionary<Type, Func<ClientPacket, LocalCommandResponse>> handlers = new();
        private readonly Queue<LocalPacketTrace> history = new();
        private long sequence;
        private bool executing;
        public const int TraceCapacity = 128;
        public LocalPacketTrace[] CaptureTrace() => history.ToArray();
        public bool HasHandler(ClientPacket packet) => packet != null && handlers.ContainsKey(packet.GetType());
        public IDisposable Register<T>(Func<T, LocalCommandResponse> handler) where T : ClientPacket
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (handlers.ContainsKey(typeof(T))) throw new InvalidOperationException("A local server already owns " + typeof(T).Name);
            Func<ClientPacket, LocalCommandResponse> adapter = p => handler((T)p);
            handlers.Add(typeof(T), adapter);
            return new Registration(() => { if (handlers.TryGetValue(typeof(T), out var existing) && existing == adapter) handlers.Remove(typeof(T)); });
        }
        public bool TryDispatch(ClientPacket request, out LocalCommandResponse response)
        {
            response = null;
            if (request == null || !handlers.TryGetValue(request.GetType(), out var handler)) return false;
            if (executing) { response = new LocalCommandResponse(false, "중첩된 로컬 서버 요청입니다."); return true; }
            executing = true;
            try { response = handler(request) ?? throw new InvalidOperationException("Local server omitted its command response."); return true; }
            finally { executing = false; }
        }
        public void Record(LocalPacket packet)
        {
            if (packet == null) return;
            if (history.Count == TraceCapacity) history.Dequeue();
            history.Enqueue(new LocalPacketTrace(++sequence, packet));
        }
        public void Clear() { handlers.Clear(); history.Clear(); }
        private sealed class Registration : IDisposable
        {
            private Action dispose;
            public Registration(Action action) { dispose = action; }
            public void Dispose() { var action = dispose; dispose = null; action?.Invoke(); }
        }
    }
}
