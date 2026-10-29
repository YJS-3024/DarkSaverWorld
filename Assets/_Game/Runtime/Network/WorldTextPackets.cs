using System;
using System.Text;
namespace DarkSaver.Network
{
    // Saver1.2 0x42DA30: opcode7A, mode u8, byte-count u8, raw CP949 bytes.
    // F12 passes /공간이동 through the exact same single-string sender as Enter.
    public sealed class Request_WorldText : ClientPacket
    {
        public string Text {get;}
        public byte Mode {get;}
        public Request_WorldText(string text,byte mode=1){Text=text;Mode=mode;}
    }
    public enum WorldTextChannel { System, General, Whisper }
    // Local presentation notification. No original server opcode is assigned.
    public sealed class Notify_WorldText : ServerPacket
    {
        public string Text {get;}
        public WorldTextChannel Channel {get;}
        public Notify_WorldText(string text,WorldTextChannel channel=WorldTextChannel.System){Text=text;Channel=channel;}
    }
    public sealed class Response_WorldText : LocalCommandResponse
    {
        public Notify_WorldText Message {get;}
        public Response_WorldText(bool accepted,string reason=null,Notify_WorldText message=null):base(accepted,reason){Message=message;}
    }
    public static class OriginalWorldTextCodec
    {
        private static Encoding encoding;
        private static Encoding Encoding949
        {
            get
            {
                if(encoding!=null)return encoding;
                try{return encoding=Encoding.GetEncoding(949,EncoderFallback.ExceptionFallback,DecoderFallback.ExceptionFallback);}
                catch(NotSupportedException)
                {
                    // Unity Mono supplies CP949 directly. .NET offline hosts may need the optional provider.
                    var type=Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                    if(type?.GetProperty("Instance")?.GetValue(null) is EncodingProvider provider)Encoding.RegisterProvider(provider);
                    return encoding=Encoding.GetEncoding(949,EncoderFallback.ExceptionFallback,DecoderFallback.ExceptionFallback);
                }
            }
        }
        public static bool TryEncode(Request_WorldText request,out byte[] payload)
        {
            payload=null;if(request==null || string.IsNullOrWhiteSpace(request.Text))return false;
            foreach(char c in request.Text)if(char.IsControl(c) || char.IsSurrogate(c))return false;
            byte[] text;
            try{text=Encoding949.GetBytes(request.Text);}catch(EncoderFallbackException){return false;}
            if(text.Length==0 || text.Length>255)return false;
            payload=new byte[text.Length+3];payload[0]=0x7a;payload[1]=request.Mode;payload[2]=(byte)text.Length;
            text.CopyTo(payload,3);return true;
        }
        public static bool TryDecode(ReadOnlySpan<byte> bytes,out Request_WorldText request)
        {
            request=null;if(bytes.Length<4 || bytes[0]!=0x7a || bytes.Length!=3+bytes[2])return false;
            string text;try{text=Encoding949.GetString(bytes.Slice(3).ToArray());}catch(DecoderFallbackException){return false;}
            var candidate=new Request_WorldText(text,bytes[1]);
            if(!TryEncode(candidate,out var encoded) || !bytes.SequenceEqual(encoded))return false;
            request=candidate;return true;
        }
    }
}
namespace DarkSaver.Network
{
    // Original 0x414970:7D,u16; receive-whispers on=>1, off=>2.
    public sealed class Request_WorldWhisperSetting : ClientPacket
    {
        public bool Receive {get;}
        public ushort OriginalValue=>(ushort)(Receive?1:2);
        public Request_WorldWhisperSetting(bool receive){Receive=receive;}
    }
}
