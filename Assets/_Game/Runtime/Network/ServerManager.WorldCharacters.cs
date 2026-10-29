using System;
using System.Collections.Generic;
namespace DarkSaver.Network
{
    public sealed partial class ServerManager
    {
        private readonly List<IDisposable> worldCharacterBindings=new();
        private WorldCharacterService worldCharacterService;
        private object worldCharacterOwner;
        public WorldCharacterService LocalWorldCharacters=>worldCharacterService;
        public void BindLocalWorldCharacters(object owner,WorldCharacterService service)
        {
            if(owner==null || service==null)throw new ArgumentNullException();
            if(worldCharacterOwner==owner)return;
            if(worldCharacterOwner!=null)throw new InvalidOperationException("World character authority already installed.");
            try
            {
                worldCharacterBindings.Add(RegisterRequestHandler<Request_WorldCharacters>(service.Query));
                worldCharacterBindings.Add(RegisterRequestHandler<Request_WorldRosterFlag>(service.Flag));
                worldCharacterBindings.Add(RegisterRequestHandler<Request_WorldRosterOrder>(service.Order));
                worldCharacterBindings.Add(RegisterRequestHandler<CharacterAdjustmentQueryRequest>(service.QueryAdjustment));
                worldCharacterBindings.Add(RegisterRequestHandler<CharacterStatAdjustmentRequest>(r=>service.Adjust(r,r.UnitRecordId)));
                worldCharacterBindings.Add(RegisterRequestHandler<CharacterMagicSelectionRequest>(r=>service.Adjust(r,r.UnitRecordId)));
                worldCharacterBindings.Add(RegisterRequestHandler<CharacterMagicListRequest>(r=>service.Adjust(r,r.UnitRecordId)));
                worldCharacterBindings.Add(RegisterRequestHandler<CharacterAdjustmentClassRequest>(r=>service.Adjust(r,r.UnitRecordId)));
                worldCharacterBindings.Add(RegisterRequestHandler<CharacterAdjustmentCommandRequest>(r=>service.Adjust(r,r.UnitRecordId)));
                worldCharacterOwner=owner;worldCharacterService=service;
            }
            catch{ClearWorldCharacterBinding();throw;}
        }
        public LocalCommandResponse InstallVerifiedWorldCharacterData(WorldCharacterData[] records)
            =>worldCharacterService?.InstallVerifiedData(records) ?? new LocalCommandResponse(false,"캐릭터 로컬 서버가 연결되지 않았습니다.");
        public void ConfigureLocalWorldCharacterPolicy(Func<ClientPacket,WorldRosterEntry,WorldCharacterData,WorldCharacterMutation> policy)
        {
            if(worldCharacterService==null)throw new InvalidOperationException("Bind the world character authority first.");
            worldCharacterService.AdjustmentPolicy=policy;
        }
        public void UnbindLocalWorldCharacters(object owner){if(worldCharacterOwner==owner)ClearWorldCharacterBinding();}
        private void ClearWorldCharacterBinding()
        {foreach(var binding in worldCharacterBindings)binding.Dispose();worldCharacterBindings.Clear();worldCharacterService?.EndEditing();worldCharacterService=null;worldCharacterOwner=null;}
    }
}
