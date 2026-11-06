using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Security.Cryptography;
using DarkSaver.Original.World;

namespace DarkSaver.Original.Campaign
{
    // Local-server grant receipt; never appended to the original3C request.
    [Serializable,DataContract] public sealed class CampaignRecruitReceipt
    {
        [DataMember] public int map,job;
        [DataMember] public string request,notice,owned;
        [DataMember] public bool created;
        public CampaignRecruitReceipt Copy()=>(CampaignRecruitReceipt)MemberwiseClone();
        public static bool Valid(CampaignRecruitReceipt[] rows)=>rows==null||rows.All(r=>r!=null&&r.map>=0&&r.job>=0
            &&!string.IsNullOrEmpty(r.request)&&r.request.Length<=1024&&Guid.TryParseExact(r.notice,"N",out _)
            &&!string.IsNullOrWhiteSpace(r.owned)&&r.owned.Length<=128&&r.owned.All(c=>!char.IsControl(c)))
            &&rows.Select(r=>(r.map,r.job)).Distinct().Count()==rows.Length;
    }
    public sealed class OriginalMapMusic
    {
        private readonly (int map,string path)[] rows;
        public int Count=>rows.Length;
        private OriginalMapMusic((int,string)[] rows){this.rows=rows;}
        public static OriginalMapMusic Parse(string source)
        {
            var lines=(source??"").Replace("\r","").Split('\n');
            if(lines.Length==0||!int.TryParse(lines[0],out int count)||count<0||count>10000||lines.Length<count+1)throw new System.IO.InvalidDataException("Invalid native MIDI table.");
            var rows=new (int,string)[count];
            for(int i=0;i<count;i++)
            {
                string line=lines[i+1];int split=line.LastIndexOf(' ');
                if(split<0||!int.TryParse(line.Substring(0,split).Trim(),out int id)||split==line.Length-1)throw new System.IO.InvalidDataException("Invalid native MIDI row.");
                rows[i]=(id,"BGM\\"+line.Substring(split+1));
            }
            return new OriginalMapMusic(rows);
        }
        // Saver0041F820: first matching row, no inferred fallback or scenario-id expansion.
        public string Lookup(int map)
        {foreach(var row in rows)if(row.map==map)return row.path;return "";}
    }
    // Unity audio adapter: count completed playbacks, never retries while an async start is pending.
    public sealed class NativeScoreRepeat
    {
        public int Starts {get;private set;}
        private bool observedPlaying;
        public void Begin(){Starts=1;observedPlaying=false;}
        public void Stop(){Starts=0;observedPlaying=false;}
        public bool Observe(bool playing)
        {
            if(Starts==0)return false;
            if(playing){observedPlaying=true;return false;}
            if(!observedPlaying||Starts>NativeMapMusicState.RepeatCount)return false;
            observedPlaying=false;Starts++;return true;
        }
    }
    public sealed class NativeMapMusicState
    {
        public const int RepeatCount=1000;
        public int Map {get;private set;}=1000;
        public bool Accepted {get;private set;}
        public bool Enabled {get;private set;}=true;
        public string ActivePath {get;private set;}
        private readonly Func<int,string> lookup;
        private readonly Func<string,bool> play;
        private readonly Action stop;
        public NativeMapMusicState(Func<int,string> lookup,Func<string,bool> play,Action stop)
        {this.lookup=lookup??throw new ArgumentNullException(nameof(lookup));this.play=play??throw new ArgumentNullException(nameof(play));this.stop=stop??throw new ArgumentNullException(nameof(stop));}
        // Saver0041F310: a missing table row clears the accepted flag without stopping old audio.
        public void EnterMap(int map)
        {
            if(!Enabled){StopAccepted();Map=map;return;}
            if(Map==map&&Accepted)return;
            string path=lookup(map);
            if(string.IsNullOrEmpty(path)){Map=map;Accepted=false;return;}
            bool accepted=Play(path);Map=map;Accepted=accepted;
        }
        // Saver00412ED0/0041F8F0: direct script play does not change the cached map/result fields.
        public bool Play(string path)
        {
            if(!Enabled){StopAccepted();return false;}
            bool accepted=play(path);ActivePath=accepted?path:null;return accepted;
        }
        // Saver message590 calls0041F980 directly; it does not clear the map acceptance flag.
        public void Stop(){stop();ActivePath=null;}
        private void StopAccepted(){if(Accepted)Stop();Accepted=false;}
        // Saver0041F2A0: enabling refreshes the current map; repeated equal settings do nothing.
        public void SetEnabled(bool enabled)
        {if(Enabled==enabled)return;Enabled=enabled;if(enabled)EnterMap(Map);else StopAccepted();}
    }
    [Serializable] public sealed class CampaignUnit
    { public int unit, job, x, y, direction = 1; public string name; public CampaignUnit Copy() => (CampaignUnit)MemberwiseClone(); }
    [Serializable] public sealed class CampaignEntry { public int unit, x, y; }
    [Serializable] public sealed class CampaignOutroVariant { public string suffix,script; }
    [Serializable] public sealed class CampaignMap
    {
        public int id; public string key, kind, hash, rosterSource;
        public int nativeIndex, introIndex = -1, outroIndex = -1, eventIndex = -1;
        public string intro, outro, events;
        [NonSerialized] public CampaignOutroVariant[] outroVariants;
        public string Outro(string suffix)=>string.IsNullOrEmpty(suffix)?outro:outroVariants?.FirstOrDefault(v=>v.suffix==suffix)?.script??"";
        public WorldMapDefinition world;
        public CampaignUnit[] units;
        public CampaignEntry[] entries;
    }
    public sealed class CampaignChangedMap {public byte[] bytes;public int[] attributes,renderAttributes;}
    [Serializable] public sealed class CampaignJob { public int job, picture; public string name; }
    [Serializable] public sealed class CampaignConnection
    { public int fromMap, toMap, x, y; public string evidence; public WorldPoint[] cells; }
    [Serializable] public sealed class CampaignDefinition
    {
        public CampaignMap[] maps; public CampaignJob[] jobs;
        public CampaignConnection[] connections;
        private Func<int,CampaignScriptStep[],CampaignChangedMap> nativeChanges;
        public void ConfigureNativeMaps(Func<int,CampaignScriptStep[],CampaignChangedMap> apply){nativeChanges=apply;}
        public bool HasNativeMaps=>nativeChanges!=null;
        public CampaignChangedMap ApplyChanges(int id,CampaignScriptStep[] steps)=>nativeChanges?.Invoke(id,steps);
        public CampaignMap Map(int id) => maps?.FirstOrDefault(m => m.id == id);
        public WorldDefinition Merge(WorldDefinition world)
        {
            // The original town/interiors remain authoritative for their existing ids.
            var original = new HashSet<int>(world.maps.Select(m => m.id));
            world.maps = world.maps.Concat(maps.Where(m => !original.Contains(m.id)).Select(m => m.world)).ToArray();
            world.campaign = this;
            foreach (var connection in connections)
            {
                if (string.IsNullOrWhiteSpace(connection.evidence)) continue;
                var from = world.Map(connection.fromMap); if (from == null || world.Map(connection.toMap) == null) continue;
                string id = "native-" + connection.fromMap + "-" + connection.toMap;
                if (from.portals.Any(p => p.id == id)) continue;
                from.portals = from.portals.Concat(new[] { new WorldPortal { id = "native-" + connection.fromMap + "-" + connection.toMap,
                    targetMap = connection.toMap, spawnX = connection.x, spawnY = connection.y, cells = connection.cells } }).ToArray();
            }
            return world;
        }
    }
    public sealed class CampaignCommand
    {
        private readonly bool native;
        internal static readonly char[] NativeWhitespace=" \t\r\n\v\f".ToCharArray();
        internal static string NativeToken(ref string value)
        {int space=value.IndexOf(' ');string token=space<0?value:value.Substring(0,space);value=space<0?"":value.Substring(space+1).Trim(NativeWhitespace);return token;}
        public string name, text, raw, context; public string[] args; public int line;
        public CampaignActor[] actors; public int scene;
        public CampaignCommand Capture(CampaignActor[] values,int map,string key)
        { return new CampaignCommand(raw,line,native){actors=values.Select(a=>a.Copy()).ToArray(),scene=map,context=key}; }
        public int Int(int i, int fallback = 0) => i < args.Length && int.TryParse(args[i], out int value) ? value : fallback;
        public CampaignCommand(string value, int sourceLine,bool native=false)
        {
            this.native=native;
            if(native)
            {
                raw=value;line=sourceLine;string remaining=value;name=NativeToken(ref remaining);var tokens=new List<string>();text="";
                while(remaining.Length>0){if(name=="MESSAGE"&&tokens.Count==4)text=remaining;tokens.Add(NativeToken(ref remaining));}
                args=tokens.ToArray();return;
            }
            raw = value; line = sourceLine; var t = value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            name = t[0].ToUpperInvariant(); args = t.Skip(1).ToArray(); text = "";
            if (name == "MESSAGE")
            {
                int at = 0;
                for (int i = 0; i < 5; i++) { while (at < value.Length && char.IsWhiteSpace(value[at])) at++; while (at < value.Length && !char.IsWhiteSpace(value[at])) at++; }
                text = at < value.Length ? value.Substring(at + 1) : "";
            }
        }
        public static CampaignCommand[] Parse(string source,bool native=false)
        {
            var result = new List<CampaignCommand>(); int line = 0;
            if(native)
            {
                using(var reader=new System.IO.StringReader(source??""))
                {string raw;while((raw=reader.ReadLine())!=null){line++;var value=raw.Trim(NativeWhitespace);if(value.Length>0)result.Add(new CampaignCommand(value,line,true));}}
                return result.ToArray();
            }
            foreach (var raw in (source ?? "").Replace("\r", "").Split('\n'))
            { line++; var value = raw.Split(';')[0].Trim(); if (value.Length > 0) result.Add(new CampaignCommand(value, line)); }
            return result.ToArray();
        }
    }
    public sealed class CampaignActor
    {
        public int unit, job, x, y, direction = 1; public double renderX, renderY; public bool moving, dead;
        public string sourceJob;
        internal int targetX, targetY; internal double movement;
        public CampaignActor Copy() => (CampaignActor)MemberwiseClone();
    }
    public sealed class CampaignCombineView
    {
        public int phase,counter,steps,alpha,maskIndex,maskArchive,centerX,centerY,flash;public CampaignActor[] bodies;
    }
    public sealed class CampaignCombineAnimation
    {
        private readonly CampaignActor[] bodies;
        private readonly int x,y,steps,interval;
        private int phase,counter,flash,mask;private double elapsed,maskElapsed;
        public bool Completed=>phase==3;
        public CampaignCombineAnimation(CampaignCommand command,CampaignCommand[] rows)
        {
            x=command.Int(1);y=command.Int(2);steps=command.Int(3);interval=command.Int(4);
            if(steps<=0||interval<0||rows.Length!=command.Int(0)||rows.Length>100)throw new ArgumentException("Invalid native combine command.");
            bodies=rows.Select((r,i)=>new CampaignActor{unit=i,job=int.Parse(r.name),x=r.Int(0),y=r.Int(1),renderX=r.Int(0),renderY=r.Int(1)}).ToArray();
        }
        public void Advance(double ms)
        {
            if(ms<0||double.IsNaN(ms)||double.IsInfinity(ms)||Completed)return;
            elapsed+=ms;maskElapsed+=ms;
            if(maskElapsed>300){maskElapsed=0;mask=(mask+1)%(phase==0?5:3);}
            if(phase==0)
            {
                // Native Draw performs at most one counter step per call and uses strict clock > interval.
                if(elapsed>interval){elapsed=0;counter++;if(counter==steps+1){phase=1;elapsed=0;mask=0;maskElapsed=0;}}
            }
            else if(phase==1){if(elapsed>2999){phase=2;flash=0;}}
            else if(phase==2){if(flash==31)phase=3;else flash++;}
        }
        public CampaignCombineView Snapshot()=>new CampaignCombineView{phase=phase,counter=counter,steps=steps,alpha=phase==2?flash*8:0,maskIndex=mask,maskArchive=phase==0?1:2,centerX=x+16,centerY=y+16,flash=flash,
            bodies=phase==0?bodies.Select(b=>new CampaignActor{unit=b.unit,job=b.job,
                renderX=b.x+(x-b.x)*Math.Min(counter,steps)/steps,renderY=b.y+(y-b.y)*Math.Min(counter,steps)/steps}).ToArray():Array.Empty<CampaignActor>()};
    }
    public sealed class CampaignPresentationView
    {
        public string message, heldCommand; public int face, speaker, line, messageFirstLine, messageMode;
        public int cameraX, cameraY, scene; public bool hasCamera, waitingMessage, completed;
        public string context; public string[] ignoredCommands;public bool nativeEvent,messageResolved,nativeMessageVisible;
        public int nativeLine,nativeExecution,nativeTestResult,nativeSubject=-2;
        public CampaignActor[] actors; public CampaignCombineView combine;
    }
    public sealed class NativeCampaignMessageView
    {
        public bool visible;public string text;public int mode,face,speaker,firstLine;
    }
    // GameCtl's shared08790/089F0 window, separate from a script's +30 tracking flag.
    public sealed class NativeCampaignDialogue
    {
        private bool visible;private string text="";private int mode,face,speaker,firstLine;private double elapsed;
        public bool Visible=>visible;
        public void Show(string value,int kind,int portrait=0,int source=0)
        {text=value??"";mode=kind;face=portrait;speaker=source;firstLine=0;elapsed=0;visible=true;}
        public void Continue()
        {
            if(!visible)return;
            if(mode!=0&&text.Length>0&&firstLine+3<CampaignPresentation.MessageLines(text).Length){firstLine+=3;return;}
            Clear();
        }
        public void Advance(double ms)
        {if(!double.IsNaN(ms)&&!double.IsInfinity(ms)&&ms>=0&&visible&&mode==0&&text.Length>0){elapsed+=ms;if(elapsed>10000)Clear();}}
        public void Clear(){visible=false;text="";firstLine=0;elapsed=0;}
        public NativeCampaignMessageView Snapshot()=>new NativeCampaignMessageView{visible=visible,text=text,mode=mode,face=face,speaker=speaker,firstLine=firstLine};
    }
    public sealed class CampaignNativeExecution
    {
        public int subject,team;
        public Func<int> localTeam;
        public Func<int,bool> moving;
        public Func<bool> resultVisible;
        public Func<CampaignCommand,CampaignMessage> message;
        public NativeCampaignDialogue dialogue;
    }
    public static class OriginalCampaignInput
    {
        // GameCtl OnLButtonDown/RButtonUp block map actions only for mode2.
        // Native0D starts mode1; its message acknowledgement is not a pause.
        public static bool Cutscene(CampaignPresentationView view)=>view!=null&&!view.completed&&!view.nativeEvent;
        public static bool DirectedMessage(CampaignPresentationView view,NativeCampaignMessageView message=null)
            =>message?.visible==true||view!=null&&view.nativeEvent&&view.nativeMessageVisible;
    }
    public sealed class CampaignMessage
    {
        public string text;public int face,speaker;
    }
    public static class OriginalCampaignMessage
    {
        private static int Atoi(string value)
        {
            int at=0;while(at<value.Length&&" \t\r\n\v\f".IndexOf(value[at])>=0)at++;
            int sign=1;if(at<value.Length&&(value[at]=='-'||value[at]=='+')){if(value[at]=='-')sign=-1;at++;}
            int result=0;unchecked{while(at<value.Length&&value[at]>='0'&&value[at]<='9')result=result*10+value[at++]-'0';return result*sign;}
        }
        // GameCtl10810 resolves the first3 bytes: COMn uses one digit after
        // COM; ME/ID use the current team-name global, never the event subject.
        public static string Alias(string token,bool name,Func<int,string> commanderName,Func<int,int?> commanderJob,string ownName)
        {
            token=token??"";
            // The source CString takes three ANSI bytes. An ASCII alias cannot
            // cross a CP949 double-byte character within that prefix.
            int length=0;while(length<token.Length&&length<3&&token[length]<128)length++;
            string prefix=token.Substring(0,length);
            if(prefix=="COM")
            {
                // Mid(3,1) is passed to atoi: empty/nonnumeric suffixes select0.
                int slot=token.Length>3&&token[3]>='0'&&token[3]<='9'?token[3]-'0':0;
                return name?commanderName(slot):commanderJob(slot)?.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            if(prefix.Contains("ME")||prefix.Contains("ID"))return ownName;
            return token;
        }
        public static bool TryResolve(CampaignCommand command,Func<int,string> commanderName,Func<int,int?> commanderJob,string ownName,out CampaignMessage result)
        {
            result=null;if(command?.name!="MESSAGE"||command.args.Length<4||command.Int(0,-1)<0||command.Int(0,-1)>2)return false;
            string a=Alias(command.args[2],true,commanderName,commanderJob,ownName),b=Alias(command.args[3],true,commanderName,commanderJob,ownName);
            string face=command.Int(0)==0?"0":Alias(command.args[1],false,commanderName,commanderJob,ownName);
            if(a==null||b==null||face==null)return false;
            string template=command.text.Trim(' ','\t','\r','\n','\v','\f');
            var text=new StringBuilder();int argument=0;
            for(int i=0;i<template.Length;i++)
            {
                char c=template[i];if(c!='%'){text.Append(c);continue;}
                if(++i==template.Length)return false;
                if(template[i]=='%'){text.Append('%');continue;}
                if((template[i]!='s'&&template[i]!='S')||argument>=2)return false;
                string value=argument++==0?a:b;
                if(template[i]=='s')text.Append(value);
                // Actual supplied MFC/CRT C-locale %S treats an ANSI alias as
                // WCHAR*. With CP949 input, only a single ASCII byte followed
                // by NUL is representable. Otherwise its conversion is empty.
                // Reproduce that bounded result, never read beyond the alias
                // allocation and never silently turn %S into %s.
                else if(value.Length==1&&value[0]<128)text.Append(value);
            }
            result=new CampaignMessage{text=text.ToString(),face=Atoi(face),speaker=Atoi(a)};return true;
        }
    }
    [Serializable] public sealed class CampaignClearNotice
    { public string session, id; public int map; public bool approved; }
    // Presentation only. Conditional outcomes/rewards/scene clear require authority.
    public sealed class CampaignPresentation
    {
        private readonly CampaignCommand[] commands;
        private readonly Dictionary<int, CampaignActor> actors = new Dictionary<int, CampaignActor>();
        private readonly int commanderJob;
        private readonly Func<int,bool> canLoad;
        private readonly Func<CampaignCommand,int?> execute;
        private readonly string context;
        private readonly CampaignNativeExecution native;
        private readonly NativeCampaignDialogue nativeDialogue;
        private readonly bool ownsNativeDialogue;
        private bool tracksNativeDialogue;
        private readonly List<string> ignored = new List<string>();
        private int testResult=1; private bool enabled=true;
        private bool nativeBegun;
        private CampaignCombineAnimation combine;
        private int scene;
        private int at, face, speaker, cameraX, cameraY, messageFirstLine;
        private int messageMode = 1;
        private double delay, messageElapsed; private int waitUnit = -1;
        private string message = "", held = ""; private bool waiting, completed, hasCamera,messageResolved;
        public event Action<CampaignCommand> Effect;
        public CampaignPresentation(string script, int commanderJob, int map, Func<int,bool> canLoad = null,
            Func<CampaignCommand,int?> execute = null, string context = null, CampaignActor[] initialActors = null,CampaignNativeExecution native=null)
        {
            this.native=native;
            if(native!=null){ownsNativeDialogue=native.dialogue==null;nativeDialogue=native.dialogue??new NativeCampaignDialogue();}
            commands = CampaignCommand.Parse(script,native!=null); this.commanderJob = commanderJob; scene = map;
            this.canLoad = canLoad ?? (id => id == map); this.execute=execute; this.context=context??"presentation";
            foreach(var actor in initialActors??Array.Empty<CampaignActor>())actors[actor.unit]=actor.Copy();
        }
        public void Retry() { if(held.Length==0)return;held="";at=Math.Max(0,at-1);if(native==null)Pump(); }
        private CampaignCommand Captured(CampaignCommand c)=>c.Capture(actors.Values.ToArray(),scene,context);
        private void Execute(CampaignCommand c,bool query=false)
        {
            int? result=execute?.Invoke(Captured(c));
            if(!result.HasValue)held=c.raw;else if(query)testResult=result.Value;
        }
        private int Job(string value) => value == "COM0" ? commanderJob : int.TryParse(value, out int job) ? job : -1;
        private void Pump()
        {
            if(completed)return;
            if(native?.subject>=0&&native.moving?.Invoke(native.subject)==true)return;
            while (!completed && !waiting && (combine==null||combine.Completed) && delay <= 0 && held.Length == 0 && (waitUnit < 0 || !actors.TryGetValue(waitUnit, out var moving) || !moving.moving))
            {
                waitUnit = -1; if (at >= commands.Length) { completed = true;if(native!=null)at++;break; }
                var c = commands[at++];
                // GameCtl uses one execution flag; closing brackets reset it even in skipped blocks.
                if(c.name==")"||c.name=="}"){enabled=true;continue;}
                if(c.name=="("||c.name=="{")continue;
                bool preOwner=c.name=="END"||c.name=="CHANGESPRITE"||c.name=="CHANGETILE2";
                if(!enabled&&(native==null||!preOwner))continue;
                if(native!=null&&!preOwner&&(native.subject<0||native.team!=native.localTeam()))continue;
                if(native!=null&&c.name=="MESSAGE"&&native.resultVisible?.Invoke()==true)continue;
                switch (c.name)
                {
                    case "LOADSCENE": if (!canLoad(c.Int(0, -1))) held = c.raw; else { scene = c.Int(0); actors.Clear(); hasCamera = false; } break;
                    case "SETPOSITION": case "MOVEPOSITION": cameraX = c.Int(0); cameraY = c.Int(1); hasCamera = true; break;
                    case "CHARPOS":
                        actors[c.Int(1)] = new CampaignActor { unit = c.Int(1), job = Job(c.args[0]), sourceJob = c.args[0], x = c.Int(2), y = c.Int(3), renderX = c.Int(2), renderY = c.Int(3) }; break;
                    case "CHARMOVE":
                        if (actors.TryGetValue(c.Int(0), out var actor)) { actor.targetX = c.Int(1); actor.targetY = c.Int(2); actor.moving = actor.renderX != actor.targetX || actor.renderY != actor.targetY; actor.movement = 0; } break;
                    case "CHARDIR":
                        if (actors.TryGetValue(c.Int(0), out actor)) actor.direction = c.args.Length > 1 ? c.args[1] == "UP" ? 0 : c.args[1] == "DOWN" ? 1 : c.args[1] == "RIGHT" ? 2 : 3 : 1; break;
                    case "KILLCHAR1": if(native!=null)Execute(c);else actors.Remove(c.Int(0)); break;
                    case "KILLCHAR2": if(native!=null)Execute(c);else if (actors.TryGetValue(c.Int(0), out actor)) { actor.dead = true; actor.moving = false; } break;
                    case "DO": delay = Math.Max(0, c.Int(0)); break;
                    case "DO2": case "WAITMOVEEND": waitUnit = c.Int(0); break;
                    case "MESSAGE":
                        if(c.Int(0)<0||c.Int(0)>2)break;
                        var resolved=native?.message?.Invoke(Captured(c));
                        if(native?.message!=null&&resolved==null){held=c.raw;break;}
                        messageResolved=resolved!=null;message = resolved?.text??c.text; messageMode = c.Int(0); messageFirstLine = 0; messageElapsed = 0;
                        face = resolved?.face??(c.args.Length > 1 ? Job(c.args[1]) : -1); speaker = resolved?.speaker??c.Int(2); waiting = native!=null||!string.IsNullOrWhiteSpace(message);
                        if(nativeDialogue!=null){nativeDialogue.Show(message,messageMode,face,speaker);tracksNativeDialogue=true;}break;
                    case "WAITMOUSEBUTTON": waiting = true; break;
                    case "MAGIC":if(native!=null)Execute(c);else Effect?.Invoke(Captured(c));break;
                    case "PLAYMIDI": case "STOPMIDI": case "ATTACK": Effect?.Invoke(Captured(c)); break;
                    case "FX1":if(execute==null)Effect?.Invoke(Captured(c));else ignored.Add(c.raw);break;
                    case "COMBINE_CLASS":
                        int count=c.Int(0,-1);
                        if(count<=0||count>100||at+count>commands.Length){held=c.raw;break;}
                        try{combine=new CampaignCombineAnimation(c,commands.Skip(at).Take(count).ToArray());at+=count;actors.Clear();}
                        catch(ArgumentException){held=c.raw;}catch(FormatException){held=c.raw;}break;
                    case "TEST": enabled=testResult!=0; break;
                    case "TESTNOT": enabled=testResult==0; break;
                    case "FIRSTCLEAR": case "RANDOM": Execute(c,true); break;
                    case "SAVECLEAR": case "GETGENERAL": case "GOLD": case "USEITEM": case "SCENECLEAR":
                    case "CHANGESPRITE": case "CHANGETILE2": Execute(c); break;
                    // CHANGETILE CLEAR/START are data-authoring markers, absent from the client dispatcher.
                    case "CHANGETILE": if(execute==null)held=c.raw;else ignored.Add(c.raw); break;
                    case "END": completed = true; if(native==null)message = ""; break;
                    default:
                        if(execute==null)held=c.raw;
                        else ignored.Add(c.raw); // Native unknown tokens consume a line without granting anything.
                        break;
                }
                // Actual10F20 return1 ends this Update; these five commands
                // return0 and the host immediately calls the interpreter again.
                if(native!=null&&c.name!="TEST"&&c.name!="TESTNOT"&&c.name!="RANDOM"&&c.name!="SCENECLEAR"&&c.name!="CHANGESPRITE")return;
            }
        }
        public void Continue()
        {
            if(nativeDialogue!=null){nativeDialogue.Continue();waiting=tracksNativeDialogue&&nativeDialogue.Visible;return;}
            if (!waiting) return;
            // GameCtl08CA0 advances three literal pipe-separated lines per click.
            // A page acknowledgement must not execute the next scenario command.
            if (messageMode != 0 && message.Length > 0 && messageFirstLine + 3 < MessageLines(message).Length)
            { messageFirstLine += 3; return; }
            waiting = false; message = ""; messageFirstLine = 0; if(native==null)Pump();
        }
        public static string[] MessageLines(string value)
        {
            var rows = (value ?? "").Split('|');
            //08840 omits an empty final remainder, but preserves interior blank rows.
            int count = rows.Length - (rows[rows.Length - 1].Length == 0 ? 1 : 0);
            return rows.Take(count).Select(row => row.Trim(' ', '\t', '\r', '\n', '\v', '\f')).ToArray();
        }
        public void Advance(double ms)
        {
            if (double.IsNaN(ms) || double.IsInfinity(ms) || ms < 0) return;
            if(native!=null){nativeBegun=true;if(ownsNativeDialogue)nativeDialogue.Advance(ms);waiting=tracksNativeDialogue&&nativeDialogue.Visible;}
            foreach (var actor in actors.Values.Where(a => a.moving))
            {
                actor.movement += ms;
                while (actor.moving && actor.movement >= 80)
                {
                    actor.movement -= 80; double dx = actor.targetX - actor.renderX, dy = actor.targetY - actor.renderY;
                    actor.direction = DarkSaver.Original.Tutorial.TutorialGeometry.FacingRow(Math.Sign(dx), Math.Sign(dy));
                    actor.renderX += Math.Sign(dx) * Math.Min(Math.Abs(dx), .5); actor.renderY += Math.Sign(dy) * Math.Min(Math.Abs(dy), .5);
                    if (actor.renderX == actor.targetX && actor.renderY == actor.targetY) { actor.x = actor.targetX; actor.y = actor.targetY; actor.moving = false; }
                }
            }
            combine?.Advance(ms);
            if (delay > 0) delay = Math.Max(0, delay - ms);
            //08AD0 expires a nonempty plain notice strictly after10000ms.
            // Portrait pages have no automatic timeout.
            if(native==null&&waiting&&messageMode==0&&message.Length>0)
            {
                messageElapsed+=ms;
                if(messageElapsed>10000){waiting=false;message="";messageFirstLine=0;}
            }
            Pump();
        }
        public CampaignPresentationView Snapshot()
        {
            var view=new CampaignPresentationView { message = message, messageFirstLine = messageFirstLine, messageMode = messageMode, heldCommand = held, face = face, speaker = speaker,
            combine=combine?.Snapshot(),context=context,nativeEvent=native!=null,messageResolved=messageResolved,nativeLine=nativeBegun?at+1:0,nativeExecution=enabled?1:0,nativeTestResult=testResult,nativeSubject=native?.subject??-2,
            ignoredCommands=ignored.ToArray(),cameraX = cameraX, cameraY = cameraY, scene = scene, hasCamera = hasCamera, waitingMessage = waiting, completed = completed, line = at == 0||commands.Length==0 ? 0 : commands[Math.Min(at, commands.Length) - 1].line,
            actors = actors.Values.Select(a => a.Copy()).ToArray() };
            if(nativeDialogue!=null)
            {
                var current=nativeDialogue.Snapshot();view.message=current.text;view.messageMode=current.mode;view.messageFirstLine=current.firstLine;view.face=current.face;view.speaker=current.speaker;
                view.nativeMessageVisible=current.visible;view.waitingMessage=tracksNativeDialogue&&current.visible;view.messageResolved=true;
            }
            return view;
        }
    }
    // Pure transient playback; no health, AP, inventory or reward mutations.
    public sealed class CampaignVisualPlayback
    {
        public int scene=-1;public string context;
        public readonly List<BattleMagicEffect> magic=new List<BattleMagicEffect>();
        public readonly Dictionary<int,BattleAttackEffect> attacks=new Dictionary<int,BattleAttackEffect>();
        public readonly Dictionary<int,CampaignActor> attackActors=new Dictionary<int,CampaignActor>();
        public void Clear(){scene=-1;context=null;magic.Clear();attacks.Clear();attackActors.Clear();}
        public void Advance(double ms)
        {
            if(double.IsNaN(ms)||double.IsInfinity(ms)||ms<0)return;
            foreach(var effect in magic)effect.Advance(ms);magic.RemoveAll(e=>e.Expired);
            foreach(var effect in attacks.Values)effect.age+=ms;
            foreach(var id in attacks.Where(e=>e.Value.Expired).Select(e=>e.Key).ToArray()){attacks.Remove(id);attackActors.Remove(id);}
        }
        public bool Accept(CampaignCommand c,MagicMenuCatalog menu,MagicProjectileCatalog patterns,int width,int height)
        {
            if(c==null||c.actors==null)return false;
            if(scene!=c.scene||context!=c.context){Clear();scene=c.scene;context=c.context;}
            CampaignActor Actor(int id)=>c.actors.FirstOrDefault(a=>a.unit==id);
            TrainingUnit Unit(CampaignActor a)=>new TrainingUnit{id=a.unit,job=a.job,x=a.x,y=a.y,direction=a.direction};
            if(c.name=="ATTACK")
            {
                // ATTACK stores target first, attacker second, then the raw effect flag.
                var target=Actor(c.Int(0));var source=Actor(c.Int(1));if(source==null||target==null)return false;
                foreach(var e in BattleAttackEffect.Create(Unit(source),Unit(target),c.Int(2)))
                {attacks[e.unit]=e;attackActors[e.unit]=Actor(e.unit).Copy();}return true;
            }
            if(c.name!="MAGIC"||c.args.Length<4)return false;
            var entry=menu?.Find(c.Int(0));var pattern=patterns?.Find(entry?.pattern??-1);var caster=Actor(c.Int(1));
            if(entry==null||pattern?.Valid!=true||caster==null)return false;
            var targets=c.args.Skip(4).Select(v=>int.TryParse(v,out int id)?Actor(id):null).Where(a=>a!=null).ToArray();
            magic.RemoveAll(e=>e.sourceUnit==caster.unit&&e.role==BattleMagicEffectRole.ChargeOnly||targets.Any(t=>t.unit==e.targetUnit));
            string idPrefix=c.context+":"+c.line;
            BattleMagicEffect Effect(CampaignActor target,BattleMagicEffectRole role)=>new BattleMagicEffect{id=idPrefix+"/"+(target?.unit.ToString()??"caster"),
                magic=entry.id,sourceX=caster.x,sourceY=caster.y,sourceUnit=caster.unit,sourceJob=caster.job,targetX=target?.x??c.Int(2),targetY=target?.y??c.Int(3),targetUnit=target?.unit??-1,
                areaX=c.Int(2),areaY=c.Int(3),radius=entry.range,mapWidth=width,mapHeight=height,pattern=pattern.Copy(),role=role};
            if(pattern.type==0)return true;
            bool multiple=targets.Length!=1;
            if(pattern.HasCharge&&(multiple||pattern.type!=2)||pattern.type==18||BattleSkillFrames.HasCaster(pattern.type,entry.range))magic.Add(Effect(null,BattleMagicEffectRole.ChargeOnly));
            if(pattern.type!=16)foreach(var target in targets)magic.Add(Effect(target,multiple?BattleMagicEffectRole.TargetOnly:BattleMagicEffectRole.TargetWithCharge));
            return true;
        }
    }
    [Serializable,DataContract] public sealed class CampaignScriptStep
    {
        [DataMember] public string key,command;
        [DataMember] public int result,scene;
        [DataMember] public byte[] packet;
        public CampaignScriptStep Copy(){var c=(CampaignScriptStep)MemberwiseClone();c.packet=packet==null?null:(byte[])packet.Clone();return c;}
    }
    [Serializable,DataContract] public sealed class CampaignEventRecord
    {
        [DataMember] public string id;
        [DataMember] public int number;
        [DataMember] public bool completed;
        [DataMember(EmitDefaultValue=false)] public NativeCampaignEvent native;
        public CampaignEventRecord Copy(){var r=(CampaignEventRecord)MemberwiseClone();r.native=native?.Copy();return r;}
    }
    [Serializable,DataContract] public sealed class NativeCampaignEvent
    {
        [DataMember] public int subject,providedTeam,team;
        [DataMember] public long life,revision;
        [DataMember] public bool providedTeamKnown,started;
        [DataMember] public string fingerprint;
        public NativeCampaignEvent Copy()=>(NativeCampaignEvent)MemberwiseClone();
        public bool Valid=>subject>=-1&&subject<3000&&life>=0&&(subject!=-1||life==0)&&revision>=0&&fingerprint!=null;
    }
    public static class OriginalDirectedEvents
    {
        public static bool TryDecode(byte[] bytes,out int number,out int subject)
        {
            number=subject=-1;if(bytes==null||bytes.Length!=6||bytes[0]!=0x5c||bytes[1]!=0x0d)return false;
            number=(short)(bytes[2]|bytes[3]<<8);subject=(short)(bytes[4]|bytes[5]<<8);
            return number>=-1&&number<100&&subject>=-1&&subject<3000;
        }
        public static byte[] Packet(int number,int subject)=>number<-1||number>=100||subject<-1||subject>=3000?null:
            new[]{(byte)0x5c,(byte)0x0d,(byte)number,(byte)(number>>8),(byte)subject,(byte)(subject>>8)};
        private static readonly string[] noops={"GETITEM","REGEN","CLASSUP","SET_DUTY_STEP","ENDING_DEMO","NEXTSCENE","COM_GENERAL","GET_GENERAL","MASSAGE","WARPCHAR","CHANGETILE","CHARONCE","SAVE","FX1","COMMANDER","ONCE","CHAR","ALL","INF"};
        private static readonly string[] commands={"MESSAGE","PLAYMIDI","STOPMIDI","GOLD","USEITEM","GETGENERAL","SCENECLEAR","FIRSTCLEAR","SAVECLEAR","RANDOM","DO","TEST","TESTNOT","(",")","END","CHANGESPRITE","CHANGETILE2","MAGIC","KILLCHAR1","KILLCHAR2"};
        public static bool Supports(CampaignCommand[] body,int subject)=>body!=null&&body.All(c=>
            (noops.Contains(c.name)||commands.Contains(c.name)||c.name.Length>0&&(c.name[0]>='0'&&c.name[0]<='9'||c.name[0]=='+'||c.name[0]=='-'))
            &&(c.name!="DO"||c.args.Length==1&&int.TryParse(c.args[0],out int wait)&&wait>=-1)
            &&(c.name!="MAGIC"||c.args.Length==4&&c.args.All(v=>int.TryParse(v,out int n)&&n>=short.MinValue&&n<=short.MaxValue))
            &&(!c.name.StartsWith("KILLCHAR",StringComparison.Ordinal)||c.args.Length==1&&int.TryParse(c.args[0],out int target)&&target<3000)
            &&(c.name!="MESSAGE"||c.args.Length>=4&&c.Int(0,-1)>=0&&c.Int(0,-1)<=2&&!string.IsNullOrWhiteSpace(c.text)));
    }
    [Serializable,DataContract] public sealed class CampaignProgressRecord
    {
        [DataMember] public string visit;
        [DataMember] public long revision;
        [DataMember] public CampaignScriptStep[] steps=Array.Empty<CampaignScriptStep>();
        [DataMember] public CampaignEventRecord[] events=Array.Empty<CampaignEventRecord>();
        [DataMember(EmitDefaultValue=false)] public string[] nativeNotices;
        [NonSerialized,DataMember(EmitDefaultValue=false)] public int? nativeLocalTeam;
        [DataMember(EmitDefaultValue=false)] public bool nativeBlocked;
        public CampaignProgressRecord Copy()=>new CampaignProgressRecord{visit=visit,revision=revision,steps=steps.Select(s=>s.Copy()).ToArray(),events=events.Select(e=>e.Copy()).ToArray(),nativeNotices=nativeNotices?.ToArray(),nativeLocalTeam=nativeLocalTeam,nativeBlocked=nativeBlocked};
        public static bool Valid(CampaignProgressRecord r)=>r==null||!string.IsNullOrEmpty(r.visit)&&r.revision>0&&r.steps!=null&&r.events!=null
            &&r.steps.All(s=>s!=null&&!string.IsNullOrEmpty(s.key)&&s.command!=null&&(s.packet==null||s.packet.Length<=4096))
            &&r.steps.Select(s=>s.key).Distinct().Count()==r.steps.Length
            &&r.events.All(e=>e!=null&&!string.IsNullOrEmpty(e.id)&&e.number>=0&&e.number<100&&(e.native==null||e.native.Valid))
            &&(!r.nativeLocalTeam.HasValue||r.nativeLocalTeam>=0&&r.nativeLocalTeam<20)
            &&(r.nativeNotices==null||r.nativeNotices.All(id=>Guid.TryParseExact(id,"N",out _))&&r.nativeNotices.Distinct().Count()==r.nativeNotices.Length)
            &&r.events.Select(e=>e.id).Distinct().Count()==r.events.Length&&r.events.Count(e=>!e.completed)<=101
            &&r.events.Count(e=>!e.completed&&e.native?.started==true)<=1
            &&(!r.events.Any(e=>e.native!=null)||r.nativeLocalTeam.HasValue);
    }
    [Serializable,DataContract] public sealed class CampaignDelivery
    {
        [DataMember] public string id,visit,command;
        [DataMember] public int map;
        [DataMember] public byte[] packet;
        [DataMember] public bool acknowledged;
        public CampaignDelivery Copy(){var c=(CampaignDelivery)MemberwiseClone();c.packet=(byte[])packet.Clone();return c;}
    }
    [Serializable,DataContract] public sealed class CampaignDeliveryRecord
    {
        [DataMember] public long revision;
        [DataMember] public CampaignDelivery[] requests=Array.Empty<CampaignDelivery>();
        public CampaignDeliveryRecord Copy()=>new CampaignDeliveryRecord{revision=revision,requests=requests.Select(r=>r.Copy()).ToArray()};
        public static bool Valid(CampaignDeliveryRecord r)=>r==null||r.revision>0&&r.requests!=null&&r.requests.All(d=>d!=null&&!string.IsNullOrEmpty(d.id)
            &&!string.IsNullOrEmpty(d.visit)&&d.map>=0&&d.command!=null&&d.packet!=null&&d.packet.Length>0&&d.packet.Length<=4096)
            &&r.requests.Select(d=>d.id).Distinct().Count()==r.requests.Length;
    }
    public sealed class CampaignScriptRequest
    {
        public string id,character,session,visit,command; public int map; public byte[] packet;
        public CampaignScriptRequest Copy(){var c=(CampaignScriptRequest)MemberwiseClone();c.packet=(byte[])packet.Clone();return c;}
    }
    public sealed class CampaignEventsNotice
    { public string id,character,session,visit; public int map; public bool approved; public byte[] packet; }
    public static class OriginalCampaignScript
    {
        public static string Fingerprint(string script)
        {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(script??""))).Replace("-","").ToLowerInvariant();}
        // Native one-way requests. Payloads contain no local visit/id envelope.
        public static byte[] Packet(CampaignCommand c)
        {
            int v=c.Int(0);
            switch(c.name)
            {
                case "GETGENERAL":return new[]{(byte)0x3c,(byte)v,(byte)(v>>8)};
                case "USEITEM":return new[]{(byte)0x49,(byte)v,(byte)(v>>8)};
                case "GOLD":return new[]{(byte)0x3a,(byte)v,(byte)(v>>8),(byte)(v>>16),(byte)(v>>24)};
                case "SCENECLEAR":return new[]{(byte)0x5c,(byte)0x20,(byte)(c.args.FirstOrDefault()=="ALONE"?2:1)};
                default:return null;
            }
        }
        public static int Random(int percent,uint seed)=>((unchecked(seed*214013+2531011)>>16&32767)%101)<percent?1:0;
        private static int NativeConditionNumber(string text)
        {
            if(text.Length==0||text.Any(c=>c<'0'||c>'9'))return -99;
            int result=0;unchecked{foreach(char c in text)result=result*10+c-'0';}return result;
        }
        // Actual13D60: literal headers; full decimal conditions;99 action slots.
        // A blank/999 before END disables the event. Eligibility remains server-owned.
        public static Dictionary<int,CampaignCommand[]> Events(string source)
        {
            var rows=new Dictionary<int,List<CampaignCommand>>();int number=0,line=0;
            using(var reader=new System.IO.StringReader(source??""))
            {
                string raw;
                while((raw=reader.ReadLine())!=null)
                {
                    line++;string value=raw.Trim(CampaignCommand.NativeWhitespace),remaining=value;
                    string token=CampaignCommand.NativeToken(ref remaining);
                    if(token=="EVENT"&&number<1)
                    {
                        int parsed=NativeConditionNumber(CampaignCommand.NativeToken(ref remaining));
                        // The original has no safe bounds for malformed headers;
                        // do not reproduce its out-of-table memory writes.
                        number=parsed>=0&&parsed<100?parsed:-1;
                        if(number>=0)rows[number]=new List<CampaignCommand>();
                    }
                    else if(token=="END")
                    {
                        if(number>=0&&rows.TryGetValue(number,out var body)&&!body.Any(c=>c.name=="END"))body.Add(new CampaignCommand("END",line,true));
                        number=0;
                    }
                    else if(token.Length==0||token=="999")
                    {if(number>0){rows.Remove(number);number=0;}}
                    else if(number>0&&NativeConditionNumber(token)<1&&rows[number].Count<99)
                        rows[number].Add(new CampaignCommand(value,line,true));
                }
            }
            return rows.ToDictionary(p=>p.Key,p=>p.Value.ToArray());
        }
        // With subject=-1,15 bypasses the post-ownership interactive command block.
        // Only the two recovered map mutations are supported from its pre-gate block.
        public static bool TryImmediateChanges(CampaignCommand[] body,out CampaignCommand[] changes)
        {
            var result=new List<CampaignCommand>();changes=null;if(body==null)return false;
            foreach(var command in body.TakeWhile(c=>c.name!="END"))
            {
                if(command.name=="LOADSCENE"||command.name=="TUTORIAL"||command.name=="DISABLE"||command.name=="ENABLE"||command.name=="ENDGAME")return false;
                if(command.name=="CHANGESPRITE"||command.name=="CHANGETILE2")result.Add(command);
            }
            changes=result.ToArray();return true;
        }
        public static bool DecodeEvents(byte[] packet,out int[] events)
        {
            events=null;if(packet==null||packet.Length<4||packet[0]!=0x5c||packet[1]!=0x15)return false;
            int n=packet[2]|packet[3]<<8;if(n>100||packet.Length!=4+n*2)return false;
            events=new int[n];for(int i=0;i<n;i++){events[i]=packet[4+i*2]|packet[5+i*2]<<8;if(events[i]>=100){events=null;return false;}}return true;
        }
    }

}
