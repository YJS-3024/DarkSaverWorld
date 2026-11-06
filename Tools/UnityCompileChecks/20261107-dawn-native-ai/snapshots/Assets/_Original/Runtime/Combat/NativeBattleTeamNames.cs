using System;
using System.Linq;

namespace DarkSaver.Original.World
{
    public static class OriginalVictoryAnnouncement
    {
        // Incoming20: ignored BYTE, name byte length, ANSI bytes. This is not
        // the outgoing three-byte SCENECLEAR request with the same subtype.
        public static bool TryDecode(byte[] bytes,out byte ignored,out byte[] name)
        {
            ignored=0;name=null;
            if(bytes==null||bytes.Length<4||bytes[0]!=0x5c||bytes[1]!=0x20||bytes.Length!=4+bytes[3])return false;
            ignored=bytes[2];name=bytes.Skip(4).ToArray();return true;
        }
        public static byte[] Packet(byte ignored,byte[] name)
        {
            if(name==null||name.Length>255)return null;
            return new[]{(byte)0x5c,(byte)0x20,ignored,(byte)name.Length}.Concat(name).ToArray();
        }
        public static byte[] CString(byte[] name)
        {if(name==null)return null;int end=Array.IndexOf(name,(byte)0);return name.Take(end<0?name.Length:end).ToArray();}
        public static bool SameName(byte[] left,byte[] right)=>left!=null&&right!=null&&CString(left).SequenceEqual(CString(right));
    }
    public sealed class NativeBattleTeamName
    {
        public int team;public byte[] name;
        public NativeBattleTeamName Copy()=>new NativeBattleTeamName{team=team,name=name?.ToArray()};
    }
    public static class OriginalBattleTeamNames
    {
        // D550 copies a884-byte local table: count WORD and21 records of42 bytes.
        // Reject unsafe lengths instead of reproducing its unchecked stack writes.
        public const int MaximumEntries=21,MaximumNameBytes=39,StatusSearchEntries=9;
        public static bool TryDecode(byte[] bytes,out NativeBattleTeamName[] rows)
        {
            rows=null;if(bytes==null||bytes.Length<4||bytes[0]!=0x5c||bytes[1]!=0x60)return false;
            int count=(short)(bytes[2]|bytes[3]<<8);if(count<0||count>MaximumEntries)return false;
            int at=4;var result=new NativeBattleTeamName[count];
            for(int i=0;i<count;i++)
            {
                if(bytes.Length-at<3)return false;int team=(short)(bytes[at]|bytes[at+1]<<8),length=bytes[at+2];at+=3;
                if(length>MaximumNameBytes||bytes.Length-at<length)return false;
                result[i]=new NativeBattleTeamName{team=team,name=bytes.Skip(at).Take(length).ToArray()};at+=length;
            }
            if(at!=bytes.Length)return false;rows=result;return true;
        }
        public static byte[] Packet(NativeBattleTeamName[] rows)
        {
            if(rows==null||rows.Length>MaximumEntries||rows.Any(r=>r?.name==null||r.name.Length>MaximumNameBytes||r.team<short.MinValue||r.team>short.MaxValue))return null;
            var bytes=new byte[4+rows.Sum(r=>3+r.name.Length)];bytes[0]=0x5c;bytes[1]=0x60;bytes[2]=(byte)rows.Length;bytes[3]=(byte)(rows.Length>>8);int at=4;
            foreach(var row in rows){bytes[at++]=(byte)row.team;bytes[at++]=(byte)(row.team>>8);bytes[at++]=(byte)row.name.Length;Array.Copy(row.name,0,bytes,at,row.name.Length);at+=row.name.Length;}return bytes;
        }
        // GameCtl100054C0 searches only the first9 team rows, first match wins.
        // Absent rows are not read as uninitialized stack bytes by this adapter.
        public static byte[] DisplayName(NativeUnitState unit,NativeBattleTeamName[] teams)
        {
            if(unit?.Valid!=true)return Array.Empty<byte>();
            var row=teams?.Take(StatusSearchEntries).FirstOrDefault(r=>r!=null&&r.team==unit.fields[24]);
            return (row?.name??unit.name).ToArray();
        }
    }
    internal sealed partial class WorldSession
    {
        public string NativeTeamSession;
        private byte[] nativeWinner=Array.Empty<byte>();
        private bool nativeWinnerKnown=true;
        public void ReceiveNativeWinner(NativeBattleTeamName[] rows,int? localTeam)
        {
            if(rows.Length==0)return;
            if(!localTeam.HasValue){nativeWinnerKnown=false;return;}
            var match=rows.LastOrDefault(r=>r.team==localTeam.Value);
            if(match!=null){nativeWinner=match.name.ToArray();nativeWinnerKnown=true;}
        }
        public bool TryNativeWinner(NativeBattleNotice notice,out byte[] winner)
        {
            winner=nativeWinner.ToArray();
            if(notice.localTeam.HasValue&&notice.expectedTeamRevision==CurrentNativeTeamRevision)
            {
                var match=NativeTeamView().LastOrDefault(r=>r.team==notice.localTeam.Value);
                if(match!=null){winner=match.name.ToArray();return true;}
            }
            return nativeWinnerKnown;
        }
        public long NativeTeamRevision;
        public NativeBattleTeamName[] NativeTeams;
        public readonly System.Collections.Generic.HashSet<string> NativeTeamNotices=new System.Collections.Generic.HashSet<string>();
        public long CurrentNativeTeamRevision=>NativeTeamSession==CampaignSession?NativeTeamRevision:0;
        public NativeBattleTeamName[] NativeTeamView()=>NativeTeamSession==CampaignSession?NativeTeams.Select(r=>r.Copy()).ToArray():Array.Empty<NativeBattleTeamName>();
    }
}
