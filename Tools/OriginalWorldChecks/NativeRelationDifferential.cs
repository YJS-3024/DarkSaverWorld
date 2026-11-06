using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using DarkSaver.Original.World;
internal sealed class RelationCase
{
    public int first,second;public int? teamA,teamB,localTeam;public int[] relations;
}
internal static class NativeRelationDifferential
{
    static void Assert(bool value,string text){if(!value)throw new Exception(text);}
    public static void Main(string[] args)
    {
        var json=new JsonSerializerOptions{IncludeFields=true};var cases=JsonSerializer.Deserialize<RelationCase[]>(File.ReadAllText(args[0]),json);
        int? Team(RelationCase c,int id)=>id==c.first?c.teamA:id==c.second?c.teamB:null;
        var result=cases.Select(c=>new{friendly=OriginalNativeRelations.Friendly(c.first,c.second,id=>Team(c,id)),
            ally3=OriginalNativeRelations.Ally3(c.first,c.second,id=>Team(c,id),c.localTeam,c.relations),
            ally4=OriginalNativeRelations.Ally4(c.first,c.second,id=>Team(c,id),c.localTeam,c.relations),
            magic=new[]{0,1,2,3,34,35,-1,128}.Select(flags=>OriginalNativeRelations.MagicEligible(flags,c.first,c.second,id=>Team(c,id),c.localTeam,c.relations)).ToArray()}).ToArray();
        Assert(OriginalNativeRelations.Ally3(-1,400,null)==0&&OriginalNativeRelations.Friendly(-1,400,null)==false,"negative units return before missing context");
        Assert(OriginalNativeRelations.Ally3(0,400,id=>id==0?0:1,0)==null&&OriginalNativeRelations.Ally4(0,400,id=>id==0?0:1,0)==null,"missing local-player alliance slot stays unknown");
        Assert(OriginalNativeRelations.Ally3(0,400,id=>id==0?0:1)==null,"missing local team is not invented from unit0");
        Assert(OriginalNativeRelations.Ally3(0,400,id=>id==0?0:1,7)==0&&OriginalNativeRelations.Ally4(0,400,id=>id==0?0:1,7)==1,"nonlocal different-player relation does not require alliance slots");
        Assert(OriginalNativeRelations.Ally3(0,400,id=>id==0?0:8)==0&&OriginalNativeRelations.Ally4(0,400,id=>id==0?0:8)==1,"player/monster team case needs no guessed B2");
        Assert(OriginalNativeRelations.Ally3(0,400,id=>null)==null&&OriginalNativeRelations.MagicEligible(0,0,400,id=>null)==false,"unknown teams are not zero; disabled team bits need no team lookup");
        File.WriteAllText(args[1],JsonSerializer.Serialize(result,json));
    }
}
