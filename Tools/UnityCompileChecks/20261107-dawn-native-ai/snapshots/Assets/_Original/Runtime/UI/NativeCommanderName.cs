using System;
using DarkSaver.Original.World;
namespace DarkSaver.Original.UI
{
    public sealed class CommanderNameRequest
    {
        public string id,character,session,commander,name;public int slot,nativeRecord;public long rosterRevision;public byte[] packet;
        public CommanderNameRequest Copy(){var result=(CommanderNameRequest)MemberwiseClone();result.packet=packet?.Clone()as byte[];return result;}
    }
    // A client edit captures the received general identity, rather than guessing a native record from its slot.
    public sealed class NativeCommanderNameEdit
    {
        public string Character{get;}public string Session{get;}public string Commander{get;}public string OriginalName{get;}
        public int Slot{get;}public int NativeRecord{get;}public long RosterRevision{get;}
        public bool Closed{get;private set;}
        private NativeCommanderNameEdit(WorldView view,OwnedCommander row)
        {Character=view.characterId;Session=view.commanderSession;Commander=row.id;OriginalName=row.name??"";Slot=view.inspectedCommander;NativeRecord=row.nativeRecord.Value;RosterRevision=view.commanders.revision;}
        private static bool Available(WorldView view)=>view?.record!=null&&!view.moving&&view.facility==null&&view.pendingPortal==null
            &&view.commanderRequest==null&&view.training==null&&view.battle==null&&(view.presentation==null||view.presentation.completed);
        public static NativeCommanderNameEdit Begin(WorldView view)
        {
            if(!Available(view)||string.IsNullOrEmpty(view.characterId)||string.IsNullOrEmpty(view.commanderSession)||view.commanders?.rows==null
                ||view.commanders.revision<=0||view.inspectedCommander<0||view.inspectedCommander>=view.commanders.rows.Length)return null;
            var row=view.commanders.rows[view.inspectedCommander];return !string.IsNullOrEmpty(row?.id)&&row.nativeRecord>=0&&row.nativeRecord<65535?new NativeCommanderNameEdit(view,row):null;
        }
        public bool Matches(WorldView view)=>!Closed&&Available(view)&&view.characterId==Character&&view.commanderSession==Session
            &&view.inspectedCommander==Slot&&view.commanders?.revision==RosterRevision&&view.commanders.rows!=null&&view.commanders.rows.Length>Slot
            &&view.commanders.rows[Slot]?.id==Commander&&view.commanders.rows[Slot].nativeRecord==NativeRecord
            &&(view.commanders.rows[Slot].name??"")==OriginalName;
        public void Cancel(){Closed=true;}
        public CommanderNameRequest Submit(WorldView view,string name,byte[] encoded)
        {
            if(!Matches(view)||name==null||encoded==null)return null;
            Closed=true;return new CommanderNameRequest{id=Guid.NewGuid().ToString("N"),character=Character,session=Session,commander=Commander,
                slot=Slot,nativeRecord=NativeRecord,rosterRevision=RosterRevision,name=name,packet=NativeCommanderNamePackets.Build(NativeRecord,encoded)};
        }
    }
    public static class NativeCommanderNamePackets
    {
        // Saver4587C0 writes a BYTE length, then copies the full CString, unlike429F70 chat truncation.
        public static byte[] Build(int record,byte[] encoded)
        {
            if(record<0||record>65535||encoded==null)return null;
            var result=new byte[encoded.Length+4];result[0]=0x3e;result[1]=(byte)record;result[2]=(byte)(record>>8);result[3]=unchecked((byte)encoded.Length);
            Buffer.BlockCopy(encoded,0,result,4,encoded.Length);return result;
        }
    }
}
