using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DarkSaver.Events;

namespace DarkSaver.Editor
{
    // Editor-only interpretation. Never rewrites ownership, spawns, or positions on load.
    internal static class ScenarioPlacementTroops
    {
        internal readonly struct Offset
        {
            public readonly int x,y;
            public Offset(int x,int y){this.x=x;this.y=y;}
        }
        internal sealed class AiRule
        {
            public int unit,x,y,radius,target;
            public bool followsUnit;
        }
        public static readonly string[] FormationNames={"대각 네 귀퉁이","상하좌우","좌우 + 아래 2칸","뒤쪽 V형"};
        public static int CommanderId(int id)=>id/10*10;
        public static int Slot(int id)=>id%10;
        public static int StackedCells(IEnumerable<Scenario01Unit> units)=>units.GroupBy(u=>(u.x,u.y)).Count(g=>g.Skip(1).Any());
        public static Scenario01Unit Commander(IEnumerable<Scenario01Unit> units,Scenario01Unit member)
            =>units.FirstOrDefault(u=>u.id==CommanderId(member.id));
        public static Scenario01Unit[] Members(IEnumerable<Scenario01Unit> units,Scenario01Unit member)
            =>units.Where(u=>CommanderId(u.id)==CommanderId(member.id)).OrderBy(u=>u.id).ToArray();
        public static Offset[] Formation(int index)
        {
            switch(index)
            {
                case 0:return new[]{new Offset(-1,-1),new Offset(-1,1),new Offset(1,-1),new Offset(1,1)};
                case 1:return new[]{new Offset(0,-1),new Offset(-1,0),new Offset(1,0),new Offset(0,1)};
                case 2:return new[]{new Offset(-1,0),new Offset(1,0),new Offset(0,1),new Offset(0,2)};
                case 3:return new[]{new Offset(-1,1),new Offset(1,1),new Offset(-2,2),new Offset(2,2)};
                default:throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
        public static string FormationName(IEnumerable<Scenario01Unit> source)
        {
            var units=source.ToArray();
            if(units.Length==0)return "빈 부대";
            var leader=Commander(units,units[0]);
            if(leader==null)return "지휘관 없음";
            if(units.Length==1)return "단독 지휘관";
            if(units.Length==5)
            {
                var offsets=units.Where(u=>u!=leader).Select(u=>(u.x-leader.x)+":"+(u.y-leader.y)).ToArray();
                for(int i=0;i<FormationNames.Length;i++)
                    if(offsets.OrderBy(v=>v).SequenceEqual(Formation(i).Select(o=>o.x+":"+o.y).OrderBy(v=>v)))
                        return FormationNames[i]+" (형태 일치)";
            }
            return "편집 대형 ("+units.Length+"인)";
        }
        public static AiRule[] ParseAi(string text)
        {
            var rows=new List<AiRule>();
            foreach(string line in text.Split('\n'))
            {
                string code=line.Split(';')[0];
                var match=Regex.Match(code,@"^\s*(\d+)\s+OD\w+\s+(AREA|SOILDER)\s*\(\s*([^)]*)\)");
                if(!match.Success)continue;
                string[] args=match.Groups[3].Value.Split(',');
                bool follows=match.Groups[2].Value=="SOILDER";
                if(args.Length!=(follows?1:3))continue;
                var values=new int[args.Length];bool valid=true;
                for(int i=0;i<args.Length;i++)valid&=int.TryParse(args[i].Trim(),out values[i]);
                if(!valid || !int.TryParse(match.Groups[1].Value,out int unit))continue;
                if(!follows && values[2]<0)continue;
                rows.Add(new AiRule{unit=unit,followsUnit=follows,target=follows?values[0]:-1,
                    x=follows?0:values[0],y=follows?0:values[1],radius=follows?0:values[2]});
            }
            return rows.ToArray();
        }
        public static int NextCommanderId(IEnumerable<Scenario01Unit> units)
        {
            var occupied=new HashSet<int>(units.Select(u=>CommanderId(u.id)));
            for(int id=400;id<800;id+=10)if(!occupied.Contains(id))return id;
            return -1;
        }
        public static int NextMemberId(IEnumerable<Scenario01Unit> source,Scenario01Unit selected)
        {
            var units=source.ToArray();
            if(selected==null || Commander(units,selected)==null)return -1;
            var occupied=new HashSet<int>(units.Select(u=>u.id));int commander=CommanderId(selected.id);
            for(int slot=1;slot<10;slot++)if(!occupied.Contains(commander+slot))return commander+slot;
            return -1;
        }
    }
}
