using System;
using DarkSaver.Original.World;
namespace DarkSaver.Original.World
{
    public static class OriginalScenarioScriptSuffix
    {
        public static bool Valid(string suffix)=>suffix==null||suffix.Length==0||suffix.Length==1&&suffix[0]<=255;
        // E060 assigns a char, not a number or a player-name suffix. The char
        // wraps at8 bits. Input160 stores one embedded NUL; input0 clears
        // the CString. These must not choose the same DEE filename.
        public static string Decode(byte value)=>value==0?"":new string((char)unchecked((byte)(value+0x60)),1);
        public static bool TryDecode(byte[] bytes,out string suffix)
        {suffix=null;if(bytes==null||bytes.Length!=3||bytes[0]!=0x5c||bytes[1]!=0x66)return false;suffix=Decode(bytes[2]);return true;}
        public static byte[] Packet(byte value)=>new byte[]{0x5c,0x66,value};
    }
    internal sealed partial class WorldSession
    {
        private string scenarioSuffixSession,scenarioSuffix="";
        public string CurrentScenarioScriptSuffix=>scenarioSuffixSession==CampaignSession?scenarioSuffix:"";
        public void SetScenarioScriptSuffix(string value){scenarioSuffix=value;scenarioSuffixSession=CampaignSession;}
        public string ScenarioOutro()=>Definition.campaign?.Map(Record.map)?.Outro(Record.scenarioVisit?.nativeOutroSuffix??CurrentScenarioScriptSuffix)??"";
    }
}
namespace DarkSaver.Original.Entry
{
    public sealed partial class EntryAccountService
    {
        private WorldReply ReceiveScenarioScriptSuffix(NativeBattleNotice notice)
        {
            var ready=ReadyWorld();if(ready!=WorldFailure.None)return WorldResult(ready);
            if(!world.Map(world.Record.map).training&&world.Definition.campaign?.Map(world.Record.map)?.kind!="scenario")return WorldResult(WorldFailure.InvalidStage);
            var input=notice?.Copy();
            if(input?.approved!=true||!OriginalScenarioScriptSuffix.TryDecode(input.packet,out var suffix))return WorldResult(WorldFailure.InvalidInput);
            if(input.character!=worldCharacter||input.session!=world.CampaignSession||!Guid.TryParseExact(input.id,"N",out _)
                ||input.expectedRevision!=(world.Combat?.Revision??0)||world.CampaignNotices.Contains(input.id))return WorldResult(WorldFailure.StaleRequest);
            // No immediate DEE load, clear, reward, health mutation or account write.
            world.SetScenarioScriptSuffix(suffix);world.CampaignNotices.Add(input.id);return WorldResult(WorldFailure.None);
        }
    }
}
