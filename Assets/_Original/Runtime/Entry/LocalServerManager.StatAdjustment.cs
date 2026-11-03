using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public Func<StatAdjustmentRequest,WorldView,StatAdjustmentNotice> StatAdjustmentAuthority { get; set; }
        public event Action<WorldReply> Notify_StatAdjustmentChanged;
        public WorldReply Request_StatAdjustmentState()=>StatAdjustmentResult(s=>s.ExecuteStatAdjustment(StatAdjustmentOperation.State,-1,0,Guid.NewGuid().ToString("N"),ClassStatAuthority()));
        public WorldReply Request_AdjustStat(int stat,int direction,string request)=>StatAdjustmentResult(s=>s.ExecuteStatAdjustment(StatAdjustmentOperation.Step,stat,direction,request,ClassStatAuthority()));
        public WorldReply Request_ConfirmStatAllocation(string request)=>StatAdjustmentResult(s=>s.ExecuteStatAdjustment(StatAdjustmentOperation.Confirm,-1,0,request,ClassStatAuthority()));
        public WorldReply Request_CancelStatAllocation()=>WorldRequest(s=>s.CancelStatAdjustment());
        public WorldReply Notify_StatAdjustment(StatAdjustmentNotice notice)=>StatAdjustmentResult(s=>s.ReceiveStatAdjustment(notice));
        private WorldReply StatAdjustmentResult(Func<EntryAccountService,WorldReply> apply)
        {
            var reply=WorldRequest(apply);if(reply.Accepted){Notify_StatAdjustmentChanged?.Invoke(reply);if(reply.View?.record.statAllocation!=null&&reply.View.statAdjustment==null)Notify_GrowthChanged?.Invoke(reply);}return reply;
        }
    }
}
