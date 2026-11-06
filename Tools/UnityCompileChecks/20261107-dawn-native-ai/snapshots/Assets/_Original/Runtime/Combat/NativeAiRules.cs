using System;
using System.IO;
using System.Linq;

namespace DarkSaver.Original.World
{
    [Serializable] public sealed class NativeAiTextEntry { public string key,text,sha256; }
    [Serializable] public sealed class NativeAiTextCatalog
    {
        public NativeAiTextEntry[] entries;
        public string Source(string key)=>entries?.FirstOrDefault(e=>e.key==key)?.text;
    }
    // UserUnit readai:100 records,68 DWORDs each. Keep unused/stale fields:
    // SOILDER does not write AREA coordinates, REST/ATTACK do not write magic.
    public sealed class NativeAiRuleTable
    {
        public const int MaximumRows=100,Fields=68,MaximumActions=20;
        private readonly int[] values;
        public NativeAiRuleTable(){values=new int[MaximumRows*Fields];ResetKeys();}
        public NativeAiRuleTable(int[] seed)
        {if(seed==null||seed.Length!=MaximumRows*Fields)throw new ArgumentException(nameof(seed));values=(int[])seed.Clone();}
        public int[] Snapshot()=>(int[])values.Clone();
        public int[] Row(int index)=>index<0||index>=MaximumRows?null:values.Skip(index*Fields).Take(Fields).ToArray();
        public int Find(int unit){for(int i=0;i<MaximumRows;i++)if(values[i*Fields]==unit)return i;return -1;}
        private void ResetKeys(){for(int i=0;i<MaximumRows;i++)values[i*Fields]=999;}
        private static bool Space(char c)=>c==' '||c=='\t'||c=='\r'||c=='\n'||c=='\v'||c=='\f';
        private static string Trim(string value)
        {int a=0,b=value.Length;while(a<b&&Space(value[a]))a++;while(b>a&&Space(value[b-1]))b--;return value.Substring(a,b-a);}
        private static string LeftTrim(string value)
        {int a=0;while(a<value.Length&&Space(value[a]))a++;return value.Substring(a);}
        // Actual setunit GetStrTail: literal-space split; missing entries survive.
        // Leading space stops; callers decide when to Trim. No arbitrary Unicode whitespace.
        public static void Split(string value,string[] output)
        {
            value=value??"";int nul=value.IndexOf('\0');if(nul>=0)value=value.Substring(0,nul);
            for(int i=0;i<output.Length;i++)
            {
                int at=value.IndexOf(' ');if(at==0)return;
                if(at<0){output[i]=value;return;}
                output[i]=value.Substring(0,at);value=LeftTrim(value.Substring(at+1));
            }
        }
        private static int Number(string value,int fallback)
        {
            if(string.IsNullOrEmpty(value)||value.Any(c=>c<'0'||c>'9'))return fallback;
            long result=0;foreach(char c in value){result=result*10+c-'0';if(result>int.MaxValue)throw new InvalidDataException("AI decimal overflows signed32.");}return (int)result;
        }
        private static int Atoi(string value)
        {
            value=LeftTrim(value??"");int sign=1,at=0;if(value.Length>0&&(value[0]=='+'||value[0]=='-')){if(value[0]=='-')sign=-1;at++;}
            long result=0;for(;at<value.Length&&value[at]>='0'&&value[at]<='9';at++)
            {result=result*10+value[at]-'0';if(result>(sign<0?2147483648L:int.MaxValue))throw new InvalidDataException("AI weight overflows signed32.");}
            return unchecked((int)(sign*result));
        }
        private static string Head(string value){int at=value.IndexOf(' ');return at<0?value:value.Substring(0,at);}
        private static string Tail(string value){int at=value.IndexOf(' ');return at<0?"":LeftTrim(value.Substring(at+1));}
        public static void ParseMovement(string source,int[] row)
        {
            if(row==null||row.Length!=Fields)throw new ArgumentException(nameof(row));
            source=source??"";var words=Enumerable.Repeat("",10).ToArray();int open=source.Length<2?-1:source.IndexOf('(',1);
            if(open>=0)
            {
                words[0]=source.Substring(0,open);
                string body=source.Substring(open+1,Math.Max(0,source.Length-open-2)).Replace(',',' ');
                var tail=Enumerable.Repeat("",9).ToArray();Split(body,tail);Array.Copy(tail,0,words,1,9);
            }
            if(words[0]=="AREA"){row[2]=0;row[3]=Number(words[1],-1);row[4]=Number(words[2],-1);row[5]=Number(words[3],-1);}
            else if(words[0]=="SOILDER"){row[2]=1;row[6]=Number(words[1],700);}
            else{row[2]=row[3]=row[4]=row[5]=-1;row[6]=700;}
        }
        public static void ParseActions(string source,int[] row)
        {
            if(row==null||row.Length!=Fields)throw new ArgumentException(nameof(row));
            var actions=Enumerable.Repeat("",MaximumActions).ToArray();Split((source??"").Replace(',',' '),actions);row[7]=MaximumActions;
            for(int i=0;i<MaximumActions;i++)
            {
                if(actions[i].Length==0){row[7]=i;break;}
                string item=Trim(actions[i].Replace('(',' ').Replace(')',' ')),name=Head(item);int at=8+i*3;row[at]=Atoi(Tail(item));
                if(name=="REST")row[at+1]=0;
                else if(name=="ATTACK")row[at+1]=1;
                else
                {
                    name=name.Replace('_',' ');string magic=Tail(name);name=Head(name);
                    if(name=="MAGIC"||name=="MAGIC2"){row[at+1]=2;row[at+2]=Number(magic,-1);}
                }
            }
        }
        public static bool TryLoad(string source,NativeAiRuleTable previous,out NativeAiRuleTable result,out string error)
        {
            result=null;error="";var table=previous==null?new NativeAiRuleTable():new NativeAiRuleTable(previous.values);table.ResetKeys();
            if(source==null){result=table;return true;} // Failed/missing archive leaves reset keys, no fabricated rules.
            var tokens=Enumerable.Repeat("",4).ToArray();var lines=source.Split('\n');int count=lines.Length-(source.EndsWith("\n",StringComparison.Ordinal)?1:0);
            if(source.Length==0)count=0;
            try
            {
                for(int i=0;i<count;i++)
                {
                    if(i>=MaximumRows){error="AI source exceeds100 records.";return false;}
                    Split(Trim(lines[i]),tokens);var row=table.Row(i);row[0]=Number(tokens[0],999);
                    if(tokens[1]=="ODNORMAL")row[1]=2;else if(tokens[1]=="ODATTACK")row[1]=4;else if(tokens[1]=="ODDEFENCE")row[1]=8;else row[0]=-1;
                    ParseMovement(tokens[2],row);ParseActions(tokens[3],row);Array.Copy(row,0,table.values,i*Fields,Fields);
                    if(row[0]==999)break;
                }
            }
            catch(InvalidDataException failure){error=failure.Message;return false;}
            result=table;return true;
        }
    }
}
