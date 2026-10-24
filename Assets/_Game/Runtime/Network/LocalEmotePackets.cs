namespace DarkSaver.Network
{
    // Explicit local cosmetic extension; no guessed original wire opcode.
    public sealed class LocalEmoteRequest : ClientPacket
    {public int Id {get;}public LocalEmoteRequest(int id){Id=id;}}
    public sealed class LocalEmoteResponse : LocalCommandResponse
    {public int Id {get;}public LocalEmoteResponse(int id):base(true,""){Id=id;}}
}
