using System;
namespace DarkSaver.Original.World
{
    // Exact client relation helpers. Unknown received teams/current-player
    // alliance slots stay null; ID ranges and job/palette never supply a team.
    public static class OriginalNativeRelations
    {
        private static int? Relation(int[] relations,int team)=>relations?.Length==21&&team>=0&&team<21?(int?)relations[team]:null;
        public static bool? Friendly(int first,int second,Func<int,int?> team)
        {
            if(first<0||second<0)return false;int? a=team?.Invoke(first),b=team?.Invoke(second);
            if(!a.HasValue||!b.HasValue)return null;
            return a==b||a>15||b>15||a>7&&a<16&&b>7&&b<16;
        }
        public static int? Ally3(int first,int second,Func<int,int?> team,int? localTeam=null,int[] relations=null)
        {
            if(first<0||second<0)return 0;int? a=team?.Invoke(first),b=team?.Invoke(second);
            if(!a.HasValue||!b.HasValue)return null;if(a==b)return 1;
            if(a<16&&b<16)
            {
                if(a>=0&&a<8&&b>=0&&b<8)
                {
                    if(!localTeam.HasValue)return null;
                    if(a==localTeam||b==localTeam)
                    {
                        int? value=Relation(relations,a==localTeam?b.Value:a.Value);
                        return value.HasValue?(int?)(value==1||value==2?1:0):null;
                    }
                }
                if(a<8||a>15||b<8||b>15)return 0;
            }
            return 1;
        }
        public static int? Ally4(int first,int second,Func<int,int?> team,int? localTeam=null,int[] relations=null)
        {
            if(first<0||second<0)return 0;int? a=team?.Invoke(first),b=team?.Invoke(second);
            if(!a.HasValue||!b.HasValue)return null;
            if(a!=b&&a<16&&b<16&&(a<8||a>15||b<8||b>15))
            {
                if(a<0||a>7||b<0||b>7)return 1;
                if(!localTeam.HasValue)return null;
                if(a!=localTeam&&b!=localTeam)return 1;
                int? value=Relation(relations,a==localTeam?b.Value:a.Value);
                return value.HasValue?(int?)(value==1||value==4?1:0):null;
            }
            return 0;
        }
        public static bool? MagicEligible(int teamFlags,int first,int second,Func<int,int?> team,int? localTeam=null,int[] relations=null)
        {
            int? ally=(teamFlags&1)!=0?Ally3(first,second,team,localTeam,relations):0;
            int? enemy=(teamFlags&2)!=0?Ally4(first,second,team,localTeam,relations):0;
            if(ally==1||enemy==1)return true;
            return ally.HasValue&&enemy.HasValue?(bool?)false:null;
        }
    }
}
