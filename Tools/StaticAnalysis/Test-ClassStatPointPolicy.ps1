$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$settings=[IO.File]::ReadAllText((Join-Path $root 'Assets/_Game/Runtime/GameConstants.cs'))
$constants=[regex]::Matches($settings,'public const int (?:ClassChangeAdditionalStatPoints|ClassStat\w+PointCost)\s*=\s*\d+;')
$method=[regex]::Match($settings,'public static int GetClassStatPointCost\(int index\)=>index switch \{[\s\S]*?\};')
if($constants.Count -ne 5 -or -not $method.Success){throw 'Configured source layout changed; refusing to test a guessed policy'}
# Compile the exact policy members, excluding unrelated Unity Color/KeyCode fields.
$policy='namespace DarkSaver { public static partial class GameConstants {'+($constants.Value -join "`n")+$method.Value+'}}'
$packets=[IO.File]::ReadAllText((Join-Path $root 'Assets/_Game/Runtime/Network/LocalPackets.cs'))
$end=$packets.IndexOf('// normal_attack:')
if($end -lt 0){throw 'Packet base layout changed'}
$sources=@(($packets.Substring(0,$end)+'}'),$policy,
    [IO.File]::ReadAllText((Join-Path $root 'Assets/_Game/Runtime/Network/CharacterAdjustmentPackets.cs')),
    [IO.File]::ReadAllText((Join-Path $root 'Assets/_Game/Runtime/Network/OriginalCharacterInfoValues.cs')))
$sources=@($sources|ForEach-Object {[regex]::Replace($_,'(?m)^using [^;]+;\s*','')})
$harness=@'
public static class ClassStatPolicyChecks {
    static int count;
    static void Check(bool condition) { if(!condition)throw new System.Exception("Policy check failed #"+count);count++; }
    static void Word(byte[] data,int offset,short value) {data[offset]=(byte)value;data[offset+1]=(byte)(value>>8);}
    public static int Run() {
        count=0;var expected=new[]{-1,-1,2,1,2,1};
        Check(DarkSaver.GameConstants.ClassChangeAdditionalStatPoints==20);
        for(int stat=0;stat<6;stat++) {
            int cost=DarkSaver.GameConstants.GetClassStatPointCost(stat);Check(cost==expected[stat]);
            var current=new byte[724];int offset=0x16+stat*6;Word(current,offset,10);Word(current,0x38,20);
            var baseline=(byte[])current.Clone();short value,points;
            if(stat<2) {Check(!DarkSaver.Network.OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,cost,stat,DarkSaver.Network.CharacterStatAdjustmentDirection.Increase,out value,out points));continue;}
            int units=20/cost;
            for(int i=0;i<units;i++) {
                Check(DarkSaver.Network.OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,cost,stat,DarkSaver.Network.CharacterStatAdjustmentDirection.Increase,out value,out points));
                Word(current,offset,value);Word(current,0x38,points);Check(points==20-(i+1)*cost);
            }
            Check(!DarkSaver.Network.OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,cost,stat,DarkSaver.Network.CharacterStatAdjustmentDirection.Increase,out value,out points));
            for(int i=0;i<units;i++) {
                Check(DarkSaver.Network.OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,cost,stat,DarkSaver.Network.CharacterStatAdjustmentDirection.Decrease,out value,out points));
                Word(current,offset,value);Word(current,0x38,points);
            }
            Check(current[offset]==10 && current[0x38]==20);
            Check(!DarkSaver.Network.OriginalCharacterInfoValues.TryPreviewStatAdjustment(current,baseline,cost,stat,DarkSaver.Network.CharacterStatAdjustmentDirection.Decrease,out value,out points));
        }
        Check(DarkSaver.GameConstants.GetClassStatPointCost(-1)==-1);Check(DarkSaver.GameConstants.GetClassStatPointCost(6)==-1);
        return count;
    }
}
'@
Add-Type -TypeDefinition ('using System;'+($sources -join "`n")+$harness)
[pscustomobject]@{Passed=[ClassStatPolicyChecks]::Run();Scope='Exact policy members and original pure calculator only; not Unity UI or save integration'}|ConvertTo-Json -Compress
