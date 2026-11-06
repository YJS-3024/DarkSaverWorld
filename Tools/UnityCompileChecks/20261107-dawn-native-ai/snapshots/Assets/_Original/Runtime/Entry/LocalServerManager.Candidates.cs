using System;
using System.Collections.Generic;
using System.Linq;
using DarkSaver.Original.UI;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        private byte[] candidateCache;private Func<byte[],bool> writeCandidateCache;
        private NativeCandidateData candidateData;private string candidateSession;
        private readonly HashSet<string> candidateNotices=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public void ConfigureCandidateCacheWriter(Func<byte[],bool> write){writeCandidateCache=write;}
        public bool ConfigureCandidateCache(byte[] bytes,Func<byte[],bool> write)
        {
            if(!NativeCandidateData.TryCompressed(bytes,memoCodec,out _))return false;
            candidateCache=(byte[])bytes.Clone();writeCandidateCache=write;return true;
        }
        public NativeCandidateData CandidateSnapshot=>ChatContext(out _,out var session)&&candidateSession==session?candidateData?.Copy():null;
        public bool Notify_CandidateMetadata(NativeCandidateNotice notice)
        {
            if(!ChatContext(out var character,out var session)||notice==null||!notice.approved||notice.character!=character||notice.session!=session||!Guid.TryParseExact(notice.id,"N",out _))return false;
            if(candidateSession!=session){candidateSession=session;candidateData=null;candidateNotices.Clear();}
            if(candidateNotices.Contains(notice.id)||notice.packet==null||notice.packet.Length<2||notice.packet[0]!=0x11)return false;
            bool transfer=notice.packet[1]==0x13;byte[] compressed;
            if(transfer){if(writeCandidateCache==null||notice.packet.Length>10+NativeMemoTransfer.MaximumCompressed)return false;compressed=notice.packet.Skip(2).ToArray();}
            else if(notice.packet[1]==0x12&&notice.packet.Length==2)compressed=candidateCache;
            else return false;
            if(!NativeCandidateData.TryCompressed(compressed,memoCodec,out var data))return false;
            var before=candidateData;var cached=candidateCache;var writer=writeCandidateCache;
            var combined=new NativeCandidateData{header=(byte[])data.header.Clone(),terms=(candidateData?.terms??Array.Empty<string>()).Concat(data.terms).ToArray(),windowClasses=(candidateData?.windowClasses??Array.Empty<string>()).Concat(data.windowClasses).ToArray()};
            var filter=new NativeWorldChatFilter();if(!filter.SetTerms(combined.terms,memoCodec))return false;
            bool Current()=>isActiveAndEnabled&&service.IsWorldClientSession(character,session)&&candidateSession==session&&ReferenceEquals(before,candidateData)&&ReferenceEquals(cached,candidateCache)&&ReferenceEquals(writer,writeCandidateCache);
            if(transfer)
            {
                bool saved;try{saved=writer((byte[])compressed.Clone());}catch{saved=false;}
                if(!saved||!Current())return false;
            }
            if(!Current())return false;if(transfer)candidateCache=(byte[])compressed.Clone();candidateData=combined;chatFilter=filter;candidateNotices.Add(notice.id);return true;
        }
    }
}
