using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;

namespace DarkSaver.Original.World
{
    // Personal data follows the native general record. Wallet, storage, map and scenario visit are shared.
    [Serializable,DataContract] public sealed class CommanderProfile
    {
        [DataMember] public bool healthKnown,ghost;
        [DataMember] public int hp,maxHp,mp,maxMp,hiredJob=-1,hiredCount;
        [NonSerialized,DataMember(EmitDefaultValue=false)] public int? level;
        [DataMember(EmitDefaultValue=false)] public CharacterGrowthRecord growth;
        [DataMember(EmitDefaultValue=false)] public WorldOwnedItem[] inventory,equipment;
        [DataMember(EmitDefaultValue=false)] public RevivalReceipt revival;
        [DataMember(EmitDefaultValue=false)] public MercenaryReceipt mercenary;
        [DataMember(EmitDefaultValue=false)] public ShopReceipt shop;
        [DataMember(EmitDefaultValue=false)] public ItemUseReceipt itemUse;
        [DataMember(EmitDefaultValue=false)] public ClassChangeReceipt classChange;
        [DataMember(EmitDefaultValue=false)] public StatAllocationRecord statAllocation;
        [DataMember(EmitDefaultValue=false)] public MagicLoadoutRecord magicLoadout;
        public static CommanderProfile Capture(WorldRecord r)=>new CommanderProfile{healthKnown=r.healthKnown,ghost=r.ghost,hp=r.hp,maxHp=r.maxHp,mp=r.mp,maxMp=r.maxMp,
            hiredJob=r.hiredJob,hiredCount=r.hiredCount,level=r.level,growth=r.growth?.Copy(),inventory=r.inventory?.Select(i=>i.Copy()).ToArray(),equipment=r.equipment?.Select(i=>i.Copy()).ToArray(),
            revival=r.revival?.Copy(),mercenary=r.mercenary?.Copy(),shop=r.shop?.Copy(),itemUse=r.itemUse?.Copy(),classChange=r.classChange?.Copy(),statAllocation=r.statAllocation?.Copy(),magicLoadout=r.magicLoadout?.Copy()};
        public CommanderProfile Copy()
        {var r=new WorldRecord();Apply(r);return Capture(r);}
        public void Apply(WorldRecord r)
        {
            r.healthKnown=healthKnown;r.ghost=ghost;r.hp=hp;r.maxHp=maxHp;r.mp=mp;r.maxMp=maxMp;r.hiredJob=hiredJob;r.hiredCount=hiredCount;r.level=level;
            r.growth=growth?.Copy();r.inventory=inventory?.Select(i=>i.Copy()).ToArray();r.equipment=equipment?.Select(i=>i.Copy()).ToArray();
            r.revival=revival?.Copy();r.mercenary=mercenary?.Copy();r.shop=shop?.Copy();r.itemUse=itemUse?.Copy();r.classChange=classChange?.Copy();r.statAllocation=statAllocation?.Copy();r.magicLoadout=magicLoadout?.Copy();
        }
        public static bool Valid(CommanderProfile p)
        {
            if(p==null)return true;
            if(!CharacterGrowthRecord.Valid(p.growth)||!RevivalReceipt.Valid(p.revival)||!MercenaryReceipt.Valid(p.mercenary)||!ShopReceipt.Valid(p.shop)||!ItemUseReceipt.Valid(p.itemUse)
                ||!ClassChangeReceipt.Valid(p.classChange)||!StatAllocationRecord.Valid(p.statAllocation)||!MagicLoadoutRecord.Valid(p.magicLoadout))return false;
            if(p.inventory?.Any(i=>i==null)==true||p.equipment?.Any(i=>i==null)==true)return false;
            var r=new WorldRecord();p.Apply(r);if(r.inventory==null)r.inventory=Array.Empty<WorldOwnedItem>();return WorldRecord.Valid(r);
        }
        public string Fingerprint()
        {using(var data=new MemoryStream()){new DataContractJsonSerializer(typeof(CommanderProfile)).WriteObject(data,this);using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data.ToArray())).Replace("-","");}}
        public static bool Same(CommanderProfile a,CommanderProfile b)=>a==null?b==null:b!=null&&a.Fingerprint()==b.Fingerprint();
    }
    [Serializable,DataContract] public sealed class OwnedCommander
    {
        [DataMember] public string id;
        [DataMember(EmitDefaultValue=false)] public string name;
        [DataMember] public int job;
        [NonSerialized,DataMember(EmitDefaultValue=false)] public int? nativeRecord,deployment,nativeStatus;
        [DataMember(EmitDefaultValue=false)] public CommanderProfile profile;
        public OwnedCommander Copy()=>new OwnedCommander{id=id,name=name,job=job,nativeRecord=nativeRecord,deployment=deployment,nativeStatus=nativeStatus,profile=profile?.Copy()};
        public static bool Valid(OwnedCommander r)=>r!=null&&!string.IsNullOrWhiteSpace(r.id)&&r.id.Length<=128&&r.id.All(c=>!char.IsControl(c))&&r.job>=0
            &&(r.name==null||r.name.Length<=128&&r.name.All(c=>!char.IsControl(c)))
            &&(!r.nativeRecord.HasValue||r.nativeRecord>=0&&r.nativeRecord<65535)&&(!r.deployment.HasValue||r.deployment==1||r.deployment==2)
            &&(!r.nativeStatus.HasValue||r.nativeStatus>=0&&r.nativeStatus<=65535)
            &&CommanderProfile.Valid(r.profile)&&(r.profile?.growth==null||r.profile.growth.job==r.job);
    }
    [Serializable,DataContract] public sealed class CommanderRoster
    {
        [DataMember] public long revision;
        [DataMember] public string lastNotice;
        // Raw presence gate at native info-manager+18, not an inferred server scenario rule.
        [NonSerialized,DataMember(EmitDefaultValue=false)] public bool? alternateGate;
        [DataMember] public OwnedCommander[] rows;
        public bool Extended=>alternateGate==true&&rows?.Length==3;
        public CommanderRoster Copy()=>new CommanderRoster{revision=revision,lastNotice=lastNotice,alternateGate=alternateGate,rows=rows?.Select(r=>r?.Copy()).ToArray()};
        public static bool Valid(CommanderRoster r)=>r==null||r.revision>0&&Guid.TryParseExact(r.lastNotice,"N",out _)&&r.rows!=null&&r.rows.Length>0&&r.rows.Length<=10
            &&r.rows.All(OwnedCommander.Valid)&&r.rows.Select(g=>g.id).Distinct().Count()==r.rows.Length&&r.rows[0].profile==null
            &&r.rows.Where(g=>g.nativeRecord.HasValue).Select(g=>g.nativeRecord.Value).Distinct().Count()==r.rows.Count(g=>g.nativeRecord.HasValue);
    }
    public enum CommanderOperation { State,ToggleDeployment,Swap }
    public sealed class CommanderRequest
    {
        public string id,character,session,baseline;public CommanderOperation operation;public long revision;
        public int map,x,y,source,target;
        public CommanderRoster state;
        public byte[] NativePacket=>operation==CommanderOperation.ToggleDeployment&&state?.rows!=null&&source>=0&&source<state.rows.Length&&state.rows[source].nativeRecord.HasValue&&state.rows[source].deployment.HasValue
            ?OriginalCommanderPackets.Toggle(state.rows[source].nativeRecord.Value,state.rows[source].deployment==1?2:1)
            :operation==CommanderOperation.Swap?OriginalCommanderPackets.Swap(target,source):null;
        public CommanderRequest Copy(){var r=(CommanderRequest)MemberwiseClone();r.state=state?.Copy();return r;}
        public static bool Same(CommanderRequest a,CommanderRequest b)=>a!=null&&b!=null&&a.id==b.id&&a.character==b.character&&a.session==b.session&&a.baseline==b.baseline&&a.operation==b.operation
            &&a.revision==b.revision&&a.map==b.map&&a.x==b.x&&a.y==b.y&&a.source==b.source&&a.target==b.target;
    }
    public sealed class CommanderNotice
    {public string id;public bool approved;public CommanderRequest request;public CommanderRoster state;}
    public sealed class CommanderRosterPush
    {public string id,character,session;public long expectedRevision;public bool approved;public CommanderRoster state;}
    public static class OriginalCommanderPackets
    {
        public static byte[] Toggle(int record,int value)=>record<0||record>65535||value<1||value>2?null:new[]{(byte)0x42,(byte)record,(byte)(record>>8),(byte)value,(byte)(value>>8)};
        public static byte[] Swap(int destination,int source)=>destination<0||destination>=10||source<0||source>=10||source==destination?null:new[]{(byte)0x3b,(byte)destination,(byte)(destination>>8),(byte)source,(byte)(source>>8)};
        public static CommanderRoster Edit(CommanderRequest r)
        {
            if(r?.state?.rows==null||r.NativePacket==null)return null;
            var result=r.state.Copy();
            if(r.operation==CommanderOperation.ToggleDeployment)result.rows[r.source].deployment=result.rows[r.source].deployment==1?2:1;
            else if(r.operation==CommanderOperation.Swap)
            {
                var row=result.rows[r.source];result.rows[r.source]=result.rows[r.target];result.rows[r.target]=row;
                result.rows[r.source].nativeRecord=r.source;result.rows[r.target].nativeRecord=r.target;
            }
            else return null;
            return result;
        }
    }
}
