using System;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        private string nativeClassCostQuery,nativeClassCharacter,nativeClassSession;private int nativeClassMap,nativeClassRecord;
        private EntryAccountService nativeClassOwner;private CommanderAdjustmentTarget nativeClassTarget;private NativeClassClientView nativeClassView;
        public event Action<NativeClassClientView> Notify_ClassClientReceived;
        private bool ClassDisplayCurrent
        {
            get
            {
                var view=WorldSnapshot;
                if(nativeClassView==null||view==null||(nativeClassTarget?.index??0)!=(view.adjustmentTarget?.index??0)||(nativeClassTarget?.identity??nativeClassCharacter)!=(view.adjustmentTarget?.identity??view.characterId))return false;
                return ClassPacketContext(nativeClassOwner,nativeClassCharacter,nativeClassSession,nativeClassMap,view.adjustmentTarget,nativeClassRecord);
            }
        }
        public NativeClassClientView ClassClientView=>ClassDisplayCurrent?nativeClassView.Copy():null;
        public WorldReply Notify_ClassClientPacket(string token,byte[] packet)
        {
            if(!ClassDisplayCurrent||string.IsNullOrEmpty(token)||token!=nativeClassView.token)return new WorldReply(WorldFailure.StaleRequest,WorldSnapshot);
            if(NativeClassAdjustmentPackets.TryReadCosts(packet,out var costs))
            {if(nativeClassView.costRevision==long.MaxValue)return new WorldReply(WorldFailure.StaleRequest,WorldSnapshot);nativeClassView.costs=costs;nativeClassView.costRevision++;}
            else if(NativeClassAdjustmentPackets.TryReadOptions(packet,out var options))
            {if(nativeClassView.optionsRevision==long.MaxValue)return new WorldReply(WorldFailure.StaleRequest,WorldSnapshot);nativeClassView.options=options;nativeClassView.optionsRevision++;}
            else return new WorldReply(WorldFailure.InvalidInput,WorldSnapshot);
            Notify_ClassClientReceived?.Invoke(nativeClassView.Copy());return new WorldReply(ClassDisplayCurrent&&token==nativeClassView.token?WorldFailure.None:WorldFailure.StaleRequest,WorldSnapshot);
        }
        public event Action<byte[]> Notify_ClassAdjustmentPacket;
        private bool ClassPacketContext(EntryAccountService owner,string character,string session,int map,CommanderAdjustmentTarget target,int? record)
        {
            var view=WorldSnapshot;
            if(!isActiveAndEnabled||!ReferenceEquals(service,owner)||service?.TryWorldClientSession(out _,out _)!=true||view==null||view.characterId!=character||view.growthSession!=session||view.record.map!=map||view.moving||view.pendingPortal!=null||view.facility!=null
                ||!CommanderAdjustmentTarget.Same(view.adjustmentTarget,target))return false;
            int index=target?.index??0;
            return !record.HasValue||view.commanders?.rows!=null&&index>=0&&index<view.commanders.rows.Length&&view.commanders.rows[index].nativeRecord==record;
        }
        private bool EmitClassPacket(EntryAccountService owner,string character,string session,int map,CommanderAdjustmentTarget target,int? record,byte[] packet)
        {
            if(!ClassPacketContext(owner,character,session,map,target,record))return false;
            if(packet!=null)Notify_ClassAdjustmentPacket?.Invoke((byte[])packet.Clone());
            return ClassPacketContext(owner,character,session,map,target,record);
        }
        public WorldReply Request_OpenClassClient()
        {
            var view=WorldSnapshot;
            if(!isActiveAndEnabled||service?.TryNativeClassRecord(out int record)!=true)return new WorldReply(WorldFailure.RulesUnavailable,view);
            string key=view.growthSession+":"+view.record.map+":"+(view.adjustmentTarget?.identity??view.characterId)+":"+record;
            if(nativeClassCostQuery==key&&ClassDisplayCurrent)return new WorldReply(WorldFailure.None,view);
            nativeClassCostQuery=key;nativeClassOwner=service;nativeClassCharacter=view.characterId;nativeClassSession=view.growthSession;nativeClassMap=view.record.map;nativeClassRecord=record;nativeClassTarget=view.adjustmentTarget?.Copy();nativeClassView=new NativeClassClientView{token=Guid.NewGuid().ToString("N")};
            var opened=nativeClassView;
            bool current=EmitClassPacket(service,view.characterId,view.growthSession,view.record.map,view.adjustmentTarget,record,NativeClassAdjustmentPackets.Costs(record))&&ReferenceEquals(opened,nativeClassView)&&ClassDisplayCurrent;
            return new WorldReply(current?WorldFailure.None:WorldFailure.StaleRequest,WorldSnapshot);
        }
        public WorldReply Request_CloseClassClient(bool confirm)
        {
            var view=WorldSnapshot;if(!isActiveAndEnabled||service?.TryNativeClassRecord(out int record)!=true)return new WorldReply(WorldFailure.RulesUnavailable,view);
            Request_CancelClassClient();
            bool current=EmitClassPacket(service,view.characterId,view.growthSession,view.record.map,view.adjustmentTarget,record,NativeClassAdjustmentPackets.Close(record,confirm));
            return new WorldReply(current?WorldFailure.None:WorldFailure.StaleRequest,WorldSnapshot);
        }
        public void Request_CancelClassClient(){nativeClassCostQuery=null;nativeClassView=null;nativeClassOwner=null;nativeClassTarget=null;}
        private bool BeginClassPacketContext(int? record,out string token)
        {token=null;if(!record.HasValue)return true;if(!Request_OpenClassClient().Accepted)return false;token=nativeClassView?.token;return token!=null;}
        private bool SameClassPacketWindow(string token)=>token==null||ClassDisplayCurrent&&nativeClassView.token==token;
        private Func<StatAdjustmentRequest,WorldView,StatAdjustmentNotice> ClassStatAuthority()
        {
            var authority=StatAdjustmentAuthority;var owner=service;if(authority==null)return null;
            return (request,view)=>
            {
                if(!StatAdjustmentRequest.Same(WorldSnapshot?.statAdjustmentRequest,request)||!BeginClassPacketContext(request.nativeRecord,out string token))return null;
                if(request.operation!=StatAdjustmentOperation.State&&!EmitClassPacket(owner,request.character,request.session,request.map,request.target,request.nativeRecord,request.NativePacket))return null;
                if(!SameClassPacketWindow(token)||!ClassPacketContext(owner,request.character,request.session,request.map,request.target,request.nativeRecord)||!StatAdjustmentRequest.Same(WorldSnapshot?.statAdjustmentRequest,request))return null;
                return authority(request,view);
            };
        }
        private Func<MagicLoadoutRequest,WorldView,MagicLoadoutNotice> ClassMagicAuthority()
        {
            var authority=MagicLoadoutAuthority;var owner=service;if(authority==null)return null;
            // Legacy selection APIs have no original learning-packet counterpart.
            return (request,view)=>MagicLoadoutRequest.Same(WorldSnapshot?.magicLoadoutRequest,request)&&ClassPacketContext(owner,request.character,request.session,request.map,request.target,request.nativeRecord)?authority(request,view):null;
        }
        private Func<ClassChangeRequest,WorldView,ClassChangeNotice> ClassChangeAuthorityWithPacket()
        {
            var authority=ClassChangeAuthority;var owner=service;if(authority==null)return null;
            return (request,view)=>ClassChangeRequest.Same(WorldSnapshot?.classChangeRequest,request)&&BeginClassPacketContext(request.nativeRecord,out string token)&&EmitClassPacket(owner,request.character,request.session,request.map,request.target,request.nativeRecord,request.NativePacket)&&SameClassPacketWindow(token)&&ClassChangeRequest.Same(WorldSnapshot?.classChangeRequest,request)?authority(request,view):null;
        }
        private Func<ClassOptionsRequest,WorldView,ClassOptionsNotice> ClassOptionsAuthorityWithPacket()
        {
            var authority=ClassOptionsAuthority;var owner=service;if(authority==null)return null;
            return (request,view)=>
            {
                if(!ClassOptionsRequest.Same(WorldSnapshot?.classOptionsRequest,request)||!BeginClassPacketContext(request.nativeRecord,out string token))return null;
                var notice=authority(request,view);if(notice?.approved!=true)return notice;
                return EmitClassPacket(owner,request.character,request.session,request.map,request.target,request.nativeRecord,NativeClassAdjustmentPackets.Options(request.nativeRecord,notice.classPoints))&&SameClassPacketWindow(token)&&ClassOptionsRequest.Same(WorldSnapshot?.classOptionsRequest,request)?notice:null;
            };
        }
    }
}
