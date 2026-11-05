using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace DarkSaver.Original.Campaign
{
    // GameCtl5C54 display data. These values never award XP, money, items or classes.
    [Serializable,DataContract] public sealed class NativeResultRow
    {
        [DataMember] public short[] words;
        [DataMember] public uint experience;
        public bool Valid=>words?.Length==14;
        public int Job=>words[1];public int State=>words[2];public int Growth=>words[3];public int CompanionJob=>words[4];
        public int CompanionCount=>words[5];public int CrossCount=>words[6];
        public NativeResultRow Copy()=>new NativeResultRow{words=words?.ToArray(),experience=experience};
    }
    [Serializable,DataContract] public sealed class NativeResultReport
    {
        [DataMember] public int outcome,eventTotal,eventCompleted;
        [DataMember] public uint spoils;
        [DataMember] public NativeResultRow[] rows;
        public bool Valid=>outcome>=short.MinValue&&outcome<=short.MaxValue&&eventTotal>=short.MinValue&&eventTotal<=short.MaxValue&&eventCompleted>=short.MinValue&&eventCompleted<=short.MaxValue
            &&rows!=null&&rows.Length<=10&&rows.All(r=>r?.Valid==true);
        public bool SafeDisplay=>Valid&&rows.All(r=>r.CompanionCount>=0&&r.CompanionCount<=50&&r.CrossCount>=0&&r.CrossCount<=50);
        public NativeResultReport Copy()=>new NativeResultReport{outcome=outcome,eventTotal=eventTotal,eventCompleted=eventCompleted,spoils=spoils,rows=rows?.Select(r=>r?.Copy()).ToArray()};
    }
    public static class OriginalResultPackets
    {
        private static int Word(byte[] b,int at)=>(short)(b[at]|b[at+1]<<8);
        private static uint Dword(byte[] b,int at)=>(uint)(b[at]|b[at+1]<<8|b[at+2]<<16|b[at+3]<<24);
        public static bool TryDecode(byte[] bytes,out NativeResultReport report)
        {
            report=null;if(bytes==null||bytes.Length<14||bytes[0]!=0x5c||bytes[1]!=0x54)return false;
            int count=Word(bytes,12);if(count<0||count>10||bytes.Length!=14+32*count)return false;
            var rows=new NativeResultRow[count];
            for(int i=0;i<count;i++){int at=14+i*32;var words=new short[14];for(int j=0;j<14;j++)words[j]=(short)Word(bytes,at+2*j);rows[i]=new NativeResultRow{words=words,experience=Dword(bytes,at+28)};}
            report=new NativeResultReport{outcome=Word(bytes,2),eventTotal=Word(bytes,4),eventCompleted=Word(bytes,6),spoils=Dword(bytes,8),rows=rows};return true;
        }
        public static byte[] Packet(NativeResultReport report)
        {
            if(report?.Valid!=true)return null;var bytes=new byte[14+report.rows.Length*32];bytes[0]=0x5c;bytes[1]=0x54;
            Action<int,int> word=(at,value)=>{bytes[at]=(byte)value;bytes[at+1]=(byte)(value>>8);};
            Action<int,uint> dword=(at,value)=>{for(int j=0;j<4;j++)bytes[at+j]=(byte)(value>>(j*8));};
            word(2,report.outcome);word(4,report.eventTotal);word(6,report.eventCompleted);dword(8,report.spoils);word(12,report.rows.Length);
            for(int i=0;i<report.rows.Length;i++){int at=14+i*32;for(int j=0;j<14;j++)word(at+j*2,report.rows[i].words[j]);dword(at+28,report.rows[i].experience);}return bytes;
        }
        public static string Outcome(int code)=>code==1?"승리":code==2?"도망":code==3?"패배":code==4?"종료":"";
        public static string Growth(int code,uint xp)=>code==1?"레벨업":code==2?"클레스체인지":"경험치 획득 : "+unchecked((int)xp).ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static int RowY(int count,int index)=>((8-count)*index-count)*4+86;
        public static int CompanionX(int count,int index)=>60+(15-count)*2*index;
        public static int DropX(int visibleIndex)=>334+12*visibleIndex;
        public static NativeResultIcon[] Icons(NativeResultReport report,Func<int,int?> itemPicture)
        {
            var icons=new List<NativeResultIcon>();if(report?.SafeDisplay!=true)return icons.ToArray();
            void Add(string kind,int id,int x,int y,int sx=32,int sy=32)=>icons.Add(new NativeResultIcon{kind=kind,id=id,x=x,y=y,sourceX=sx,sourceY=sy,width=32,height=32});
            for(int i=0;i<report.rows.Length;i++)
            {
                var row=report.rows[i];int y=RowY(report.rows.Length,i);Add("body",row.Job,25,y);
                if(row.State==2)
                {
                    Add("marker",0,25,y,224,0);int visible=0;
                    for(int slot=7;slot<14;slot++)
                    {
                        int item=row.words[slot];if(item<0)continue;int x=DropX(visible++);int? picture=itemPicture?.Invoke(item);
                        if(picture>=0)Add("item",item,x,y,picture.Value%100*32,picture.Value/100*32);
                        Add("marker",0,x,y,224,0);
                    }
                }
                if(row.CompanionJob>0)
                {
                    for(int n=0;n<row.CompanionCount;n++)Add("body",row.CompanionJob,CompanionX(row.CompanionCount,n),y);
                    for(int n=0;n<row.CrossCount;n++)Add("marker",0,CompanionX(row.CompanionCount,n),y,224,0);
                }
            }
            return icons.ToArray();
        }
        public static NativeResultText[] Texts(NativeResultReport report)
        {
            if(report?.SafeDisplay!=true)return Array.Empty<NativeResultText>();var texts=new List<NativeResultText>();
            string outcome=Outcome(report.outcome);if(outcome.Length>0)texts.Add(new NativeResultText{x=81,y=274,text=outcome});
            texts.Add(new NativeResultText{x=220,y=274,text=report.spoils.ToString(System.Globalization.CultureInfo.InvariantCulture)});
            texts.Add(new NativeResultText{x=363,y=274,text=report.eventCompleted+"/"+report.eventTotal});
            for(int i=0;i<report.rows.Length;i++)texts.Add(new NativeResultText{x=205,y=RowY(report.rows.Length,i)+3,text=Growth(report.rows[i].Growth,report.rows[i].experience)});
            return texts.ToArray();
        }
    }
    public sealed class NativeResultIcon {public string kind;public int id,x,y,sourceX,sourceY,width,height;}
    public sealed class NativeResultText {public int x,y;public string text;public uint color=0x101010;}
    [Serializable,DataContract] public sealed class NativeResultKills
    {
        [DataMember] public int[] jobs;
        // Local delivery revision, absent from the original31 bytes.
        [DataMember] public long revision;
        public bool Valid=>revision>=0&&jobs!=null&&jobs.Length<=1000&&jobs.All(j=>j>=short.MinValue&&j<=short.MaxValue);
        public NativeResultKills Copy()=>new NativeResultKills{jobs=jobs?.ToArray(),revision=revision};
    }
    public static class OriginalResultKillPackets
    {
        public static bool TryDecode(byte[] bytes,out NativeResultKills report)
        {
            report=null;if(bytes==null||bytes.Length<4||bytes[0]!=0x5c||bytes[1]!=0x31)return false;
            int count=(short)(bytes[2]|bytes[3]<<8);if(count<0||count>1000||bytes.Length!=4+2*count)return false;
            var jobs=new int[count];for(int i=0;i<count;i++)jobs[i]=(short)(bytes[4+2*i]|bytes[5+2*i]<<8);
            report=new NativeResultKills{jobs=jobs};return true;
        }
        public static byte[] Packet(NativeResultKills report)
        {
            if(report?.Valid!=true)return null;var bytes=new byte[4+2*report.jobs.Length];bytes[0]=0x5c;bytes[1]=0x31;
            bytes[2]=(byte)report.jobs.Length;bytes[3]=(byte)(report.jobs.Length>>8);
            for(int i=0;i<report.jobs.Length;i++){bytes[4+2*i]=(byte)report.jobs[i];bytes[5+2*i]=(byte)(report.jobs[i]>>8);}return bytes;
        }
    }
    // One Step represents one original10007350 draw invocation, not an inferred
    // wall-clock interval or a server death/reward calculation.
    public sealed class NativeResultKillAnimation
    {
        private int[] jobs=Array.Empty<int>();
        public int Remaining {get;private set;}
        public int PageStart {get;private set;}
        public int Counter {get;private set;}
        public string Caption {get;private set;}="죽인 몬스터: 없음";
        public void Receive(NativeResultKills report)
        {if(report?.Valid!=true)throw new ArgumentException("Invalid result kill list.");jobs=report.jobs.ToArray();Remaining=jobs.Length;}
        public NativeResultKillFrame Step(Func<int,int?> picture)
        {
            if(Remaining==0)return new NativeResultKillFrame{icons=Array.Empty<NativeResultIcon>(),text=Caption};
            var icons=new List<NativeResultIcon>();
            for(int i=0;i<=10;i++)
            {
                int at=PageStart+i;
                if(at>=Remaining)
                {
                    Caption="죽인 몬스터 : 총 "+Remaining.ToString(System.Globalization.CultureInfo.InvariantCulture)+"명 격퇴";
                    if(Counter>i+2)Remaining=0;
                    break;
                }
                int x=36+34*(at%11),y=215;
                if(i>Counter)
                {
                    int? key=picture?.Invoke(jobs[at]);
                    if(key.HasValue&&key.Value>=0)
                    {
                        bool large=key.Value/100==7;
                        icons.Add(new NativeResultIcon{kind="body",id=jobs[at],x=x-(large?16:0),y=y-(large?16:0),sourceX=large?0:32,
                            sourceY=large?64:32,width=large?64:32,height=large?64:32});
                    }
                }
                int offset=i-Counter;
                if(offset>=0&&offset<=3)icons.Add(new NativeResultIcon{kind="effects",x=x,y=y,sourceX=(3-offset)*32,sourceY=0,width=32,height=32});
            }
            if(Counter>11){PageStart+=11;Counter=0;}Counter++;
            return new NativeResultKillFrame{icons=icons.ToArray()};
        }
    }
    public sealed class NativeResultKillFrame {public NativeResultIcon[] icons;public string text;}
    // Display records, not reward instructions or original wire opcodes.
    [Serializable,DataContract] public sealed class CampaignResultUnit
    {
        [DataMember] public int unit,job;
        [DataMember] public string name;
        [NonSerialized,DataMember(EmitDefaultValue=false)] public int? level,classJob;
        [NonSerialized,DataMember(EmitDefaultValue=false)] public ulong? experience;
        public CampaignResultUnit Copy()=>(CampaignResultUnit)MemberwiseClone();
        public bool Valid=>unit>=0&&job>=0&&!string.IsNullOrWhiteSpace(name)&&name.Length<=128&&(!level.HasValue||level>=0)&&(!classJob.HasValue||classJob>=0);
    }
    [Serializable,DataContract] public sealed class CampaignResultRecord
    {
        [DataMember] public string id,visit;
        [DataMember] public int map;
        [DataMember] public bool victory;
        [DataMember(EmitDefaultValue=false)] public string outcome;
        public bool IsDefeat=>!victory&&outcome=="defeat";
        public bool IsRetreat=>!victory&&outcome=="retreat";
        public bool IsTerminated=>!victory&&outcome=="terminated";
        [DataMember(EmitDefaultValue=false)] public NativeResultReport native;
        [DataMember(EmitDefaultValue=false)] public NativeResultKills nativeKills;
        [NonSerialized,DataMember(EmitDefaultValue=false)] public ulong? experience,spoils;
        [NonSerialized,DataMember(EmitDefaultValue=false)] public int? kills,eventCompleted,eventTotal;
        [DataMember] public CampaignResultUnit[] allies=Array.Empty<CampaignResultUnit>(),enemies=Array.Empty<CampaignResultUnit>();
        public CampaignResultRecord Copy()
        {var r=(CampaignResultRecord)MemberwiseClone();r.native=native?.Copy();r.nativeKills=nativeKills?.Copy();r.allies=allies?.Select(u=>u?.Copy()).ToArray();r.enemies=enemies?.Select(u=>u?.Copy()).ToArray();return r;}
        public static bool Valid(CampaignResultRecord r)
        {
            if(r==null)return true;
            if(!Guid.TryParseExact(r.id,"N",out _)||!Guid.TryParseExact(r.visit,"N",out _)||r.map<0
                ||!(r.victory&&(r.outcome==null||r.outcome=="victory")||r.IsDefeat||r.IsRetreat||r.IsTerminated)
                ||r.kills<0||r.eventCompleted.HasValue!=r.eventTotal.HasValue||r.eventCompleted<0||r.eventTotal<r.eventCompleted
                ||r.allies==null||r.enemies==null||r.allies.Length+r.enemies.Length>1000)return false;
            if(r.native!=null&&(!r.native.SafeDisplay||r.native.outcome!=(r.victory?1:r.IsRetreat?2:r.IsDefeat?3:4)||r.native.spoils!=r.spoils
                ||r.native.eventCompleted!=r.eventCompleted||r.native.eventTotal!=r.eventTotal||r.native.rows.Length!=r.allies.Length))return false;
            if(r.nativeKills!=null&&(!r.nativeKills.Valid||r.nativeKills.revision<=0||r.kills!=r.nativeKills.jobs.Length||r.nativeKills.jobs.Any(j=>j<0)))return false;
            var all=r.allies.Concat(r.enemies).ToArray();if(!all.All(u=>u?.Valid==true)||all.Select(u=>u.unit).Distinct().Count()!=all.Length)return false;
            return r.native==null||r.allies.Select((u,i)=>u.unit==i&&u.job==r.native.rows[i].Job&&u.experience==r.native.rows[i].experience&&!u.level.HasValue&&!u.classJob.HasValue).All(v=>v);
        }
    }
    [Serializable,DataContract] public sealed class CampaignVisitRecord
    {
        [DataMember] public string id;
        [DataMember] public int map,sourceMap,x,y,direction;
        [DataMember(EmitDefaultValue=false)] public CampaignResultRecord result;
        public CampaignVisitRecord Copy(){var r=(CampaignVisitRecord)MemberwiseClone();r.result=result?.Copy();return r;}
        public static bool Valid(CampaignVisitRecord r)=>r==null||Guid.TryParseExact(r.id,"N",out _)&&r.map>=0&&r.sourceMap>=0&&r.sourceMap!=r.map&&r.x>=0&&r.y>=0&&r.direction>=0&&r.direction<4
            &&CampaignResultRecord.Valid(r.result)&&(r.result==null||r.result.visit==r.id&&r.result.map==r.map);
    }
    public sealed class CampaignResultNotice
    {
        public string character,session;
        public bool approved;
        public CampaignResultRecord result;
    }
}
