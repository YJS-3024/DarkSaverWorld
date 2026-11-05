using System;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Entry
{
    public sealed partial class LocalServerManager
    {
        public event Action<WorldReply> Notify_World;
        public event Action<WorldReply> Notify_MapEntered;
        private WorldReply WorldRequest(Func<EntryAccountService, WorldReply> operation, bool entered = false)
        {
            var reply = isActiveAndEnabled && service != null ? operation(service) : new WorldReply(WorldFailure.Unauthenticated);
            Notify_World?.Invoke(reply); if (entered && IsCurrentWorldReply(reply)) Notify_MapEntered?.Invoke(reply); return reply;
        }
        private bool IsCurrentWorldReply(WorldReply reply)
        {
            var current=WorldSnapshot;var expected=reply?.View;
            return reply?.Accepted==true&&current!=null&&expected!=null&&current.characterId==expected.characterId
                &&current.commanderSession==expected.commanderSession&&current.campaignSession==expected.campaignSession&&current.record.map==expected.record.map;
        }
        public WorldView WorldSnapshot => isActiveAndEnabled ? service?.WorldSnapshot() : null;
        public WorldReply Request_BeginWorld(NativeWorldArt art) => WorldRequest(s => art?.IsConfigured == true ? s.BeginWorld(campaignArt != null ? campaignArt.Merge(art.Definition) : art.Definition) : new WorldReply(WorldFailure.InvalidStage), true);
        public WorldReply Request_MoveWorld(int x, int y) => WorldRequest(s => s.MoveWorld(x, y));
        public WorldReply Request_EquipWorldItem(string owned,int slot,string id,int inventorySlot=-1) => WorldRequest(s=>s.EquipWorldItem(owned,slot,id,inventorySlot));
        public WorldReply Request_UseWorldItem(string owned,string id) => SendItemUse(owned,id);
        public WorldReply Request_EnterInterior(string portal) => WorldRequest(s => s.EnterWorldPortal(portal, false), true);
        public WorldReply Request_EnterTraining(string portal)
        {
            var reply=WorldRequest(s=>s.EnterWorldPortal(portal,true),true);
            var current=WorldSnapshot;
            if(reply.Accepted&&current!=null&&current.training==null&&current.characterId==reply.View.characterId
                &&current.commanderSession==reply.View.commanderSession&&current.campaignSession==reply.View.campaignSession&&current.record.map==reply.View.record.map)
            {
                BeginTrainingTest(current);
                var after=WorldSnapshot;
                if(after!=null&&after.training==null&&after.characterId==current.characterId&&after.campaignSession==current.campaignSession
                    &&after.record.map==current.record.map)Notify_TrainingInitialRequested?.Invoke(after);
            }
            return reply;
        }
        public WorldReply Request_EscapeTraining() => WorldRequest(s => s.EscapeTraining(), true);
        public WorldReply Request_InteractNpc(string npc) => WorldRequest(s => s.OpenWorldFacility(npc));
        public WorldReply Request_CloseFacility() => WorldRequest(s => s.CloseWorldFacility());
        public WorldReply Request_BuyItem(int item, string token, string id) => SendShop(true,item,null,token,id);
        public WorldReply Request_SellItem(string item, string token, string id) => SendShop(false,-1,item,token,id);
        public WorldReply Request_BankDeposit(ulong amount, string token, string id) => SendBankTransfer(true,amount,token,id);
        public WorldReply Request_BankWithdraw(ulong amount, string token, string id) => SendBankTransfer(false,amount,token,id);
        public WorldReply Request_StoreItem(string item, string token, string id) => SendWarehouseMove(true,item,token,id);
        public WorldReply Request_RetrieveItem(string item, string token, string id) => SendWarehouseMove(false,item,token,id);
        public WorldReply Request_ConfirmStorage(string token, string id) => SendWarehouseConfirm(token,id);
        public WorldReply Request_Revive(string token, string id) => SendRevival(token,id);
    }
}
