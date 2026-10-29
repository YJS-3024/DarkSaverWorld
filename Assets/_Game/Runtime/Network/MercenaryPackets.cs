using System;
using DarkSaver.Core;
namespace DarkSaver.Network
{
    public sealed class Request_OpenMercenary : ClientPacket
    {public string NpcId {get;}public ushort OwnerId {get;}public Request_OpenMercenary(string npcId,ushort owner){NpcId=npcId;OwnerId=owner;}}
    // Saver423810:4D + owner + job + offered maximum. Quantity is always one.
    public sealed class Request_HireMercenary : ClientPacket
    {public ushort OwnerId {get;}public ushort JobId {get;}public ushort Maximum {get;}public Request_HireMercenary(ushort owner,ushort job,ushort maximum){OwnerId=owner;JobId=job;Maximum=maximum;}}
    // Saver423940:4E + owner + existing job. This is dismissal, not an item sale.
    public sealed class Request_DismissMercenary : ClientPacket
    {public ushort OwnerId {get;}public ushort JobId {get;}public Request_DismissMercenary(ushort owner,ushort job){OwnerId=owner;JobId=job;}}
    public sealed class MercenaryOffer
    {
        public int JobId,Maximum,Hp,Mp,Attack,Defense;public long Price;public string Name,Type;
        public MercenaryOffer Clone()=>(MercenaryOffer)MemberwiseClone();
    }
    public sealed class Response_OpenMercenary : LocalCommandResponse
    {
        public string NpcId {get;}public ushort OwnerId {get;}private readonly MercenaryOffer[] offers;
        public MercenaryOffer[] CaptureOffers()=>Array.ConvertAll(offers,o=>o.Clone());
        public Response_OpenMercenary(bool accepted,string npcId,ushort owner,MercenaryOffer[] offers=null,string reason=null):base(accepted,reason)
        {NpcId=npcId;OwnerId=owner;this.offers=accepted && offers!=null?Array.ConvertAll(offers,o=>o.Clone()):Array.Empty<MercenaryOffer>();}
    }
    public sealed class Notify_Mercenaries : ServerPacket
    {
        public ushort OwnerId {get;}public int JobId {get;}public int Count {get;}public long Gold {get;}
        public Notify_Mercenaries(ushort owner,int job,int count,long gold){OwnerId=owner;JobId=job;Count=count;Gold=gold;}
    }
}
