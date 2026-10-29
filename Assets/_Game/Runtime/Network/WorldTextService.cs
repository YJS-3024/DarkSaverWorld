using System;
namespace DarkSaver.Network
{
    public readonly struct WorldTextContext
    {
        public readonly string CharacterName;
        public readonly bool Ready;
        public WorldTextContext(string name,bool ready){CharacterName=name;Ready=ready;}
    }
    // Local server owns validation and delivery. Only the actually connected local
    // character exists; other players, command destinations and permissions are not fabricated.
    public sealed class WorldTextService
    {
        private readonly Func<WorldTextContext> context;
        private bool receiveWhispers=true;
        public LocalCommandResponse SetWhispers(Request_WorldWhisperSetting request)
        {if(request==null || !context().Ready)return new LocalCommandResponse(false,"현재는 설정을 변경할 수 없습니다.");receiveWhispers=request.Receive;return new LocalCommandResponse(true);}
        private Func<Request_WorldText,WorldTextContext,Response_WorldText> command;
        public WorldTextService(Func<WorldTextContext> context){this.context=context ?? throw new ArgumentNullException(nameof(context));}
        public void ConfigureCommandHandler(Func<Request_WorldText,WorldTextContext,Response_WorldText> handler)=>command=handler;
        public Response_WorldText Handle(Request_WorldText request)
        {
            var current=context();
            if(!current.Ready || string.IsNullOrWhiteSpace(current.CharacterName))return new Response_WorldText(false,"현재는 대화할 수 없습니다.");
            if(!OriginalWorldTextCodec.TryEncode(request,out _))return new Response_WorldText(false,"입력한 글자를 확인해 주세요. 최대 255바이트까지 보낼 수 있습니다.");
            if(request.Mode!=1)return new Response_WorldText(false,"지원되지 않는 대화 방식입니다.");
            string text=request.Text;
            if(text.StartsWith("/to ",StringComparison.Ordinal) || text.StartsWith("/새 ",StringComparison.Ordinal))
            {
                int start=text.IndexOf(' ')+1,end=text.IndexOf(' ',start);
                if(end<=start || string.IsNullOrWhiteSpace(text.Substring(end+1)))return new Response_WorldText(false,"받는 사람과 내용을 입력해 주세요.");
                string recipient=text.Substring(start,end-start),body=text.Substring(end+1);
                if(!receiveWhispers)return new Response_WorldText(false,"귓속말 수신이 꺼져 있습니다.");
                if(recipient!=current.CharacterName)return new Response_WorldText(false,"접속 중인 사용자를 찾을 수 없습니다.");
                return new Response_WorldText(true,message:new Notify_WorldText(current.CharacterName+" > "+recipient+": "+body,WorldTextChannel.Whisper));
            }
            if(text.StartsWith("/",StringComparison.Ordinal))
                return command?.Invoke(request,current) ?? new Response_WorldText(false,"이 명령을 현재 사용할 수 없습니다.");
            return new Response_WorldText(true,message:new Notify_WorldText(current.CharacterName+": "+text,WorldTextChannel.General));
        }
    }
}
