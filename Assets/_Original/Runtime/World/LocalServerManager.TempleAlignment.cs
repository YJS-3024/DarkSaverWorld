using System;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public event Action<WorldView> Notify_TempleAlignmentRequested;
        public event Action<byte[]> Notify_TempleAlignmentPacket;
        private TempleAlignmentOffer templeAlignment;private WorldRecord templeAlignmentState;
        private readonly System.Collections.Generic.HashSet<string> alignmentSent=new System.Collections.Generic.HashSet<string>();
        private WorldView AlignmentContext(string token)
        {
            var v=WorldSnapshot;
            return isActiveAndEnabled&&v?.facility?.kind==FacilityKind.Temple&&v.facility.token==token&&!v.record.ghost&&!v.moving&&v.pendingPortal==null?v:null;
        }
        public bool Request_TempleAlignment(string token)
        {
            var v=AlignmentContext(token);if(v==null)return false;
            templeAlignment=null;templeAlignmentState=null;
            Notify_TempleAlignmentRequested?.Invoke(v);return true;
        }
        public bool Notify_TempleAlignment(TempleAlignmentOffer offer)
        {
            if(offer==null||!offer.fee.HasValue||!offer.target.HasValue||alignmentSent.Contains(offer.id)||!Guid.TryParseExact(offer.id,"N",out _)||offer.target<0||offer.target>1)return false;
            var v=AlignmentContext(offer.token);
            if(v==null||offer.character!=v.characterId||offer.session!=v.commanderSession)return false;
            templeAlignment=offer.Copy();templeAlignmentState=v.record.Copy();return true;
        }
        public TempleAlignmentOffer TempleAlignment(string token)
        {
            var v=AlignmentContext(token);var q=templeAlignment;
            if(q==null||v==null||q.token!=token||q.character!=v.characterId||q.session!=v.commanderSession||!TempleRevivalOffer.SameState(v.record,templeAlignmentState))return null;
            return q.Copy();
        }
        public bool Request_ChangeTempleAlignment(string token,string quote)
        {
            var offer=TempleAlignment(token);if(offer==null||offer.id!=quote)return false;
            templeAlignment=null;templeAlignmentState=null;
            alignmentSent.Add(offer.id);
            Notify_TempleAlignmentPacket?.Invoke(offer.Packet());return true;
        }
        public void Request_CancelTempleAlignment(string token)
        {if(templeAlignment!=null&&templeAlignment.token==token){alignmentSent.Add(templeAlignment.id);templeAlignment=null;templeAlignmentState=null;}}
    }
}
