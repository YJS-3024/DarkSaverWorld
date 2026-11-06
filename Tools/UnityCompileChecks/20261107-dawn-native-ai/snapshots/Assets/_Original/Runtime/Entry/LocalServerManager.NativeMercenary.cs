using System;
using System.Collections.Generic;
using DarkSaver.Original.UI;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        private NativeMercenaryWindow nativeMercenary;private string nativeMercenarySession;private readonly HashSet<string> nativeMercenaryRequests=new HashSet<string>();
        public event Action<byte[]> Notify_MercenaryPacket;
        public event Action<NativeMercenaryRequest> Notify_MercenaryItemRequested;
        public WorldReply Request_OpenMercenaryClient(string token)
        {
            var view=WorldSnapshot;if(!isActiveAndEnabled||service?.TryWorldClientSession(out _,out _)!=true)return new WorldReply(WorldFailure.Unauthenticated,view);
            if(!NativeMercenaryWindow.Available(view,token))return new WorldReply(WorldFailure.StaleRequest,view);
            if(nativeMercenarySession!=view.currencySession){nativeMercenarySession=view.currencySession;nativeMercenary=null;nativeMercenaryRequests.Clear();}
            if(nativeMercenary?.Matches(view)==true&&nativeMercenary.Token==token)return new WorldReply(WorldFailure.None,view);
            nativeMercenary=NativeMercenaryWindow.Begin(view,service.WorldMercenaryOffers(),token);
            return new WorldReply(nativeMercenary==null?WorldFailure.RulesUnavailable:WorldFailure.None,view);
        }
        public NativeMercenaryWindow MercenaryClient(string token)=>nativeMercenary?.Token==token&&nativeMercenary.Matches(WorldSnapshot)?nativeMercenary:null;
        public WorldReply Request_MercenaryItem(bool hire,int job,string token,string request)
        {
            var opened=Request_OpenMercenaryClient(token);if(!opened.Accepted)return opened;
            var current=nativeMercenary;
            if(!Guid.TryParseExact(request,"N",out _)||nativeMercenaryRequests.Contains(request))return new WorldReply(WorldFailure.StaleRequest,opened.View);
            var sent=current.Act(hire,job,request);if(sent==null)return new WorldReply(WorldFailure.InvalidInput,opened.View);nativeMercenaryRequests.Add(request);
            Notify_MercenaryPacket?.Invoke((byte[])sent.packet.Clone());var view=WorldSnapshot;
            if(!isActiveAndEnabled||!current.SameOwner(view))return new WorldReply(WorldFailure.StaleRequest,view);
            if(!TempleRevivalOffer.SameState(opened.View.record,view.record))return new WorldReply(WorldFailure.None,view);
            if(!ReferenceEquals(current,nativeMercenary))return new WorldReply(WorldFailure.StaleRequest,view);
            Notify_MercenaryItemRequested?.Invoke(sent.Copy());view=WorldSnapshot;
            if(!isActiveAndEnabled||!current.SameOwner(view))return new WorldReply(WorldFailure.StaleRequest,view);
            if(!TempleRevivalOffer.SameState(opened.View.record,view.record))return new WorldReply(WorldFailure.None,view);
            if(!ReferenceEquals(current,nativeMercenary))return new WorldReply(WorldFailure.StaleRequest,view);
            //The native window predicts only its own count. Real ownership/wallet
            //remain unchanged until an explicitly supplied approval is persisted.
            //A synchronous raw response must not be applied a second time here.
            if(!ReferenceEquals(current,nativeMercenary)||!current.Matches(view)||!TempleRevivalOffer.SameState(opened.View.record,view.record)||MercenaryAuthority==null)return new WorldReply(WorldFailure.None,view);
            var applied=hire?Request_HireMercenary(sent.job,token,request):Request_DismissMercenary(token,request);
            if(!applied.Accepted&&ReferenceEquals(current,nativeMercenary)&&current.SameOwner(WorldSnapshot))nativeMercenary=NativeMercenaryWindow.Begin(WorldSnapshot,service.WorldMercenaryOffers(),token);
            return applied;
        }
        public bool Request_CancelMercenaryClient(string token)
        {if(nativeMercenary==null||token!=null&&nativeMercenary.Token!=token)return false;nativeMercenary=null;return true;}
    }
}
