namespace DarkSaver.Original.World
{
    public static class NativeBodyPalette
    {
        // Late DLL defaults at16110/16114/16118; display preferences, not server rules.
        public const int OriginalOwn=0,OriginalAllied=3,OriginalOther=2;
        public static int? Resolve(int appearance,bool connected,int? team,bool? sameForce,bool? allied,int? offlineRole,
            int own=OriginalOwn,int friend=OriginalAllied,int other=OriginalOther)
        {
            int Clamp(int value)=>value>=0&&value<4?value:0;
            if(appearance>299)return 0;
            if(!connected)
            {
                if(!offlineRole.HasValue)return null;
                return Clamp(offlineRole==-3?friend:offlineRole==-2?other:own);
            }
            if(!team.HasValue)return null;
            if(team>7)return 0;
            if(!sameForce.HasValue)return null;
            if(sameForce.Value)return Clamp(own);
            return allied.HasValue?(int?)Clamp(allied.Value?friend:other):null;
        }
    }
}
