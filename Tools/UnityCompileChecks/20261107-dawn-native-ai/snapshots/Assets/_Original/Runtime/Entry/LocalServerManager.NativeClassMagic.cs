using System;
using System.Collections.Generic;
using DarkSaver.Original.World;
namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        private readonly HashSet<string> nativeClassMagicRequests=new HashSet<string>();private string nativeClassMagicToken;
        public event Action<NativeClassMagicRequest> Notify_ClassMagicRequested;
        public WorldReply Notify_ClassMagicState(string token,NativeClassMagicState state)
        {
            if(!ClassDisplayCurrent||string.IsNullOrEmpty(token)||token!=nativeClassView.token)return new WorldReply(WorldFailure.StaleRequest,WorldSnapshot);
            if(state?.Valid!=true)return new WorldReply(WorldFailure.InvalidInput,WorldSnapshot);
            if(nativeClassView.magicRevision==long.MaxValue)return new WorldReply(WorldFailure.StaleRequest,WorldSnapshot);
            nativeClassView.magic=state.Copy();nativeClassView.magicRevision++;Notify_ClassClientReceived?.Invoke(nativeClassView.Copy());
            return new WorldReply(ClassDisplayCurrent&&token==nativeClassView.token?WorldFailure.None:WorldFailure.StaleRequest,WorldSnapshot);
        }
        public WorldReply Request_ClassMagicButton(int mode,int list,int index,string token,long revision,string request)=>RequestClassMagic(false,mode,list,index,token,revision,request);
        public WorldReply Request_ClassMagicDrag(int list,int index,string token,long revision,string request)=>RequestClassMagic(true,0,list,index,token,revision,request);
        private WorldReply RequestClassMagic(bool drag,int mode,int list,int index,string token,long revision,string request)
        {
            var view=WorldSnapshot;
            if(!ClassDisplayCurrent||string.IsNullOrEmpty(token)||token!=nativeClassView.token||revision!=nativeClassView.magicRevision)return new WorldReply(WorldFailure.StaleRequest,view);
            if(nativeClassMagicToken!=token){nativeClassMagicToken=token;nativeClassMagicRequests.Clear();}
            if(!Guid.TryParseExact(request,"N",out _)||nativeClassMagicRequests.Contains(request))return new WorldReply(WorldFailure.StaleRequest,view);
            var state=nativeClassView.magic;
            if(state==null||(drag?!state.CanDrag(list,index):(mode!=1&&mode!=2)||!state.CanButton(list,index)))return new WorldReply(WorldFailure.InvalidInput,view);
            int magic=state.Spell(list,index);var packet=drag?NativeClassAdjustmentPackets.DragMagic(nativeClassRecord,magic,list==0):NativeClassAdjustmentPackets.Magic(nativeClassRecord,magic,mode==2);
            if(packet==null||nativeClassView.magicRevision==long.MaxValue)return new WorldReply(WorldFailure.InvalidInput,view);
            var sent=new NativeClassMagicRequest{id=request,character=nativeClassCharacter,session=nativeClassSession,map=nativeClassMap,record=nativeClassRecord,token=token,revision=revision,magic=magic,list=list,index=index,mode=mode,drag=drag,packet=packet};
            if(drag)state.PredictDrag(list,index);else state.PredictButton();nativeClassView.magicRevision++;nativeClassMagicRequests.Add(request);
            // A raw request/temporary source-count prediction does not approve new spell ownership or points.
            var current=nativeClassView;Notify_ClassAdjustmentPacket?.Invoke((byte[])packet.Clone());
            if(!ReferenceEquals(current,nativeClassView)||!ClassDisplayCurrent)return new WorldReply(WorldFailure.StaleRequest,WorldSnapshot);
            Notify_ClassMagicRequested?.Invoke(sent.Copy());
            return new WorldReply(ReferenceEquals(current,nativeClassView)&&ClassDisplayCurrent?WorldFailure.None:WorldFailure.StaleRequest,WorldSnapshot);
        }
    }
}
