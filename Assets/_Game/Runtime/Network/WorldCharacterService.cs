using System;
using System.Collections.Generic;
namespace DarkSaver.Network
{
    // Authority-owned records. Costs/candidates are supplied from verified server data,
    // never inferred from a job description or a client allocation preview.
    [Serializable] public sealed class WorldCharacterData
    {
        public int RecordId, Flag; // 0 = not supplied; original wire values are 1/2.
        public byte[] Info, ClassOptions;
        public short[] StatCosts;
        public WorldCharacterData Clone()=>new WorldCharacterData{RecordId=RecordId,Flag=Flag,Info=Info==null?null:(byte[])Info.Clone(),ClassOptions=ClassOptions==null?null:(byte[])ClassOptions.Clone(),StatCosts=StatCosts==null?null:(short[])StatCosts.Clone()};
        public bool IsValid
        {
            get
            {
                if(RecordId<0 || RecordId>ushort.MaxValue || Flag<0 || Flag>2 || Info!=null && Info.Length!=OriginalCharacterInfoValues.PayloadSize || StatCosts!=null && StatCosts.Length!=6)return false;
                return ClassOptions==null || CharacterAdjustmentOptionsNotification.TryReadOriginalPrefix(ClassOptions,out _,out int used) && used==ClassOptions.Length;
            }
        }
        public static bool Validate(WorldCharacterData[] source)
        {
            if(source==null)return true;if(source.Length>10)return false;var ids=new HashSet<int>();
            foreach(var item in source)if(item==null || !item.IsValid || !ids.Add(item.RecordId))return false;return true;
        }
        public static WorldCharacterData[] Copy(WorldCharacterData[] source)=>source==null?null:Array.ConvertAll(source,i=>i.Clone());
    }
    public sealed class Request_WorldCharacters : ClientPacket {} // Local snapshot query; no invented original opcode.
    public sealed class WorldCharacterMutation
    {
        public WorldCharacterData Data {get;}
        public LocalCommandResponse Result {get;}
        public WorldCharacterMutation(LocalCommandResponse result,WorldCharacterData data=null){Result=result??throw new ArgumentNullException(nameof(result));Data=data?.Clone();}
    }
    public delegate bool SaveWorldCharacters(WorldCharacterData[] data,out string reason);
    public sealed class WorldCharacterService
    {
        private readonly Func<bool> ready;
        private readonly Func<WorldRosterEntry[]> roster;
        private readonly Func<WorldCharacterData[]> capture;
        private readonly SaveWorldCharacters save;
        private readonly Action<ServerPacket> notify;
        // Verified policies must be pure candidate transforms; apply effective-profile changes
        // only in an atomic persistence backend, never as pre-save side effects.
        // Returning a success without a replacement record is rejected.
        public Func<ClientPacket,WorldRosterEntry,WorldCharacterData,WorldCharacterMutation> AdjustmentPolicy {get;set;}
        private ushort? editing;
        private WorldCharacterData adjustmentBaseline,adjustmentDraft;
        public WorldCharacterService(Func<bool> ready,Func<WorldRosterEntry[]> roster,Func<WorldCharacterData[]> capture,SaveWorldCharacters save,Action<ServerPacket> notify)
        {this.ready=ready??throw new ArgumentNullException(nameof(ready));this.roster=roster??throw new ArgumentNullException(nameof(roster));this.capture=capture??throw new ArgumentNullException(nameof(capture));this.save=save??throw new ArgumentNullException(nameof(save));this.notify=notify??throw new ArgumentNullException(nameof(notify));}
        private LocalCommandResponse Reject(string text)=>new LocalCommandResponse(false,text);
        private bool Read(out WorldRosterEntry[] rows,out WorldCharacterData[] data)
        {
            rows=null;data=null;if(!ready())return false;
            rows=roster();data=capture();
            try{rows=new Notify_WorldRoster(rows).Capture();}catch(ArgumentException){return false;}
            if(!WorldCharacterData.Validate(data))return false;data=WorldCharacterData.Copy(data)??Array.Empty<WorldCharacterData>();
            var ordered=new List<WorldRosterEntry>();
            foreach(var d in data){var row=Array.Find(rows,r=>r.RecordId==d.RecordId);if(row!=null){Apply(row,d);ordered.Add(row);}}
            foreach(var row in rows)if(!ordered.Exists(r=>r.RecordId==row.RecordId))ordered.Add(row);
            rows=ordered.ToArray();return true;
        }
        private static void Apply(WorldRosterEntry row,WorldCharacterData d)
        {row.OriginalFlag=d.Flag==0?(ushort?)null:(ushort)d.Flag;row.CharacterInfoPayload=d.Info==null?row.CharacterInfoPayload:(byte[])d.Info.Clone();}
        private static WorldCharacterData[] Materialize(WorldRosterEntry[] rows,WorldCharacterData[] data)
            =>Array.ConvertAll(rows,row=>Array.Find(data,d=>d.RecordId==row.RecordId)?.Clone() ?? new WorldCharacterData{RecordId=row.RecordId,Flag=row.OriginalFlag??0,Info=row.CharacterInfoPayload==null?null:(byte[])row.CharacterInfoPayload.Clone()});
        private LocalCommandResponse Commit(WorldRosterEntry[] rows,WorldCharacterData[] data)
        {
            if(!WorldCharacterData.Validate(data))return Reject("잘못된 캐릭터 서버 상태입니다.");
            if(!save(WorldCharacterData.Copy(data),out string reason))return Reject(reason);
            foreach(var row in rows){var d=Array.Find(data,x=>x.RecordId==row.RecordId);if(d!=null)Apply(row,d);}
            notify(new Notify_WorldRoster(rows));return new LocalCommandResponse(true);
        }
        public LocalCommandResponse Query(Request_WorldCharacters request)
        {if(!Read(out var rows,out _))return Reject("캐릭터 정보를 조회할 수 없습니다.");notify(new Notify_WorldRoster(rows));return new LocalCommandResponse(true);}
        public LocalCommandResponse Flag(Request_WorldRosterFlag request)
        {
            if(!Read(out var rows,out var data))return Reject("출전 설정을 변경할 수 없습니다.");
            int slot=Array.FindIndex(rows,r=>r.RecordId==request.RecordId);
            // Native0x41C960 excludes slot0; do not confuse UI order with record identity.
            if(slot<=0)return Reject("출전 설정을 변경할 캐릭터가 없습니다.");
            data=Materialize(rows,data);data[slot].Flag=request.Flag;return Commit(rows,data);
        }
        public LocalCommandResponse Order(Request_WorldRosterOrder request)
        {
            if(!Read(out var rows,out var data) || request.Source>=rows.Length || request.Destination>=rows.Length || request.Source==request.Destination)return Reject("변경할 캐릭터 순서가 유효하지 않습니다.");
            var row=rows[request.Source];rows[request.Source]=rows[request.Destination];rows[request.Destination]=row;
            return Commit(rows,Materialize(rows,data));
        }
        public LocalCommandResponse QueryAdjustment(CharacterAdjustmentQueryRequest request)
        {
            if(!Read(out var rows,out var data) || !Array.Exists(rows,r=>r.RecordId==request.UnitRecordId))return Reject("보유 캐릭터가 아닙니다.");
            var d=Array.Find(data,x=>x.RecordId==request.UnitRecordId);
            if(d?.Info==null)return Reject("원본 캐릭터 능력 데이터가 등록되지 않았습니다.");
            editing=request.UnitRecordId;adjustmentBaseline=d.Clone();adjustmentDraft=d.Clone();notify(new Notify_WorldRoster(rows));PublishOptions(d);return new LocalCommandResponse(true);
        }
        private void PublishOptions(WorldCharacterData data)
        {
            if(data.StatCosts!=null)
            {
                var b=new byte[13];b[0]=0x8b;
                for(int i=0;i<6;i++){b[1+i*2]=(byte)data.StatCosts[i];b[2+i*2]=(byte)(data.StatCosts[i]>>8);}
                CharacterAdjustmentValuesNotification.TryReadOriginalPrefix(b,out var costs,out _);notify(costs);
            }
            if(data.ClassOptions!=null && CharacterAdjustmentOptionsNotification.TryReadOriginalPrefix(data.ClassOptions,out var options,out _))notify(options);
        }
        public LocalCommandResponse Adjust(ClientPacket request,ushort id)
        {
            if(!Read(out var rows,out var data))return Reject("현재는 캐릭터 정보를 변경할 수 없습니다.");
            var row=Array.Find(rows,r=>r.RecordId==id);var d=Array.Find(data,x=>x.RecordId==id);
            if(row==null || d?.Info==null || editing!=id)return Reject("캐릭터 능력창을 다시 열어 주세요.");
            if(request is CharacterAdjustmentCommandRequest cmd && cmd.Command==CharacterAdjustmentCommand.QueryValues){PublishOptions(d);return new LocalCommandResponse(true);}
            if(AdjustmentPolicy==null)
            {
                if(adjustmentBaseline?.Info==null || adjustmentDraft?.Info==null || !SameBytes(adjustmentBaseline.Info,d.Info))
                {EndEditing();return Reject("서버 캐릭터 정보가 변경되었습니다. 능력창을 다시 열어 주세요.");}
                if(request is CharacterStatAdjustmentRequest stat)
                {
                    if(adjustmentDraft.StatCosts==null || !OriginalCharacterInfoValues.TryPreviewStatAdjustment(adjustmentDraft.Info,adjustmentBaseline.Info,adjustmentDraft.StatCosts[stat.StatIndex],stat.StatIndex,stat.Adjustment,out short value,out short points))return Reject("능력치 비용·잔여 포인트·변경 범위를 확인해 주세요.");
                    int offset=0x16+stat.StatIndex*6;
                    WriteWord(adjustmentDraft.Info,offset,value);WriteWord(adjustmentDraft.Info,0x38,points);
                    Apply(row,adjustmentDraft);notify(new Notify_WorldRoster(rows));return new LocalCommandResponse(true);
                }
                if(request is CharacterAdjustmentCommandRequest command)
                {
                    if(command.Command==CharacterAdjustmentCommand.Cancel){EndEditing();notify(new Notify_WorldRoster(rows));return new LocalCommandResponse(true);}
                    if(command.Command==CharacterAdjustmentCommand.Confirm)
                    {
                        var candidate=Materialize(rows,data);var committed=d.Clone();committed.Info=(byte[])adjustmentDraft.Info.Clone();candidate[Array.FindIndex(candidate,x=>x.RecordId==id)]=committed;
                        var confirmed=Commit(rows,candidate);if(confirmed.Accepted)EndEditing();return confirmed;
                    }
                }
                return Reject("마법·전직의 원작 서버 규칙이 등록되지 않았습니다.");
            }
            var result=AdjustmentPolicy(request,row.Clone(),d.Clone());
            if(result?.Result.Accepted!=true)return result?.Result??Reject("캐릭터 변경 응답이 없습니다.");
            if(result.Data==null || !result.Data.IsValid || result.Data.RecordId!=id)return Reject("캐릭터 변경 결과가 유효하지 않습니다.");
            data=Materialize(rows,data);data[Array.FindIndex(data,x=>x.RecordId==id)]=result.Data.Clone();
            var response=Commit(rows,data);
            if(response.Accepted && request is CharacterAdjustmentCommandRequest completed && (completed.Command==CharacterAdjustmentCommand.Confirm || completed.Command==CharacterAdjustmentCommand.Cancel))EndEditing();
            return response;
        }
        public LocalCommandResponse InstallVerifiedData(WorldCharacterData[] source)
        {
            if(!Read(out var rows,out _) || source==null || !WorldCharacterData.Validate(source))return Reject("원본 서버 데이터가 유효하지 않습니다.");
            foreach(var d in source)if(!Array.Exists(rows,r=>r.RecordId==d.RecordId))return Reject("보유하지 않은 캐릭터의 원본 데이터입니다.");
            var result=Commit(rows,Materialize(rows,source));if(result.Accepted)EndEditing();return result;
        }
        private static void WriteWord(byte[] bytes,int offset,short value){bytes[offset]=(byte)value;bytes[offset+1]=(byte)(value>>8);}
        private static bool SameBytes(byte[] a,byte[] b)
        {if(a==null || b==null || a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
        public void EndEditing(){editing=null;adjustmentBaseline=adjustmentDraft=null;}
    }
}
