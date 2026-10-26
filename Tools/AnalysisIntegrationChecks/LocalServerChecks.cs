using DarkSaver.Network;
using DarkSaver.Core;
using DarkSaver.Events;

internal static class LocalServerChecks
{
    public static void Run(Action<bool,string> check)
    {
        var dispatcher=new LocalServerDispatcher();int calls=0;
        var request=new AttackRequest(700,100,-1,9);
        check(!dispatcher.TryDispatch(request,out _),"unregistered request is not acknowledged");
        var registration=dispatcher.Register<AttackRequest>(p=>{calls++;return new LocalCommandResponse(p.TargetUnitId==700);});
        try{dispatcher.Register<AttackRequest>(p=>new LocalCommandResponse(true));throw new Exception("duplicate authority accepted");}catch(InvalidOperationException){check(true,"duplicate authority rejected");}
        check(dispatcher.TryDispatch(request,out var response) && response.Accepted && calls==1,"one request has one authoritative execution");
        registration.Dispose();registration.Dispose();check(!dispatcher.TryDispatch(request,out _),"disposed authority cannot process requests");
        using(var binding=dispatcher.Register<AttackRequest>(p=>{check(dispatcher.TryDispatch(p,out var nested) && !nested.Accepted,"reentrant command is rejected");return new LocalCommandResponse(true);})){dispatcher.TryDispatch(request,out _);}
        using(var binding=dispatcher.Register<AttackRequest>(p=>throw new InvalidOperationException("fixture")))
        {try{dispatcher.TryDispatch(request,out _);}catch(InvalidOperationException){check(true,"handler error propagates once");}}
        using(var binding=dispatcher.Register<AttackRequest>(p=>new LocalCommandResponse(true)))check(dispatcher.TryDispatch(request,out _),"handler exception releases dispatch guard");
        for(int i=0;i<300;i++)dispatcher.Record(request);
        var history=dispatcher.CaptureTrace();check(history.Length==128 && history[0].Sequence==173 && history[127].Sequence==300,"trace is ordered and bounded");
        history[0]=default;check(dispatcher.CaptureTrace()[0].Sequence==173,"trace snapshots are detached");
        var random=new Random(20261024);
        foreach(int length in new[]{0,1,9,71,255,256,511,1024,2047,2048})
        {
            var bytes=new byte[length];random.NextBytes(bytes);var framed=OriginalLocalPacketCodec.EncodeFrame(bytes);
            check(OriginalLocalPacketCodec.TryDecodeFrame(framed,out var decoded) && decoded.SequenceEqual(bytes),"original framing random roundtrip "+length);
            for(int size=0;size<framed.Length;size++)check(!OriginalLocalPacketCodec.TryDecodeFrame(framed.AsSpan(0,size),out _),"truncated frame rejected");
        }
        check(Convert.ToHexString(OriginalLocalPacketCodec.EncodeFrame(new byte[]{9,0x47}))=="AA55010200475047475355AA","original escape/checksum known vector");
        var corrupt=OriginalLocalPacketCodec.EncodeFrame(new byte[]{1,2,3});corrupt[5]^=1;check(!OriginalLocalPacketCodec.TryDecodeFrame(corrupt,out _),"bad checksum rejected");
        try{OriginalLocalPacketCodec.EncodeFrame(new byte[2049]);throw new Exception("oversize accepted");}catch(ArgumentOutOfRangeException){check(true,"2049-byte payload rejected");}
        var attack=new AttackRequest(-1,short.MinValue,0x4709,short.MaxValue);
        check(OriginalLocalPacketCodec.TryEncodePayload(attack,out var payload) && Convert.ToHexString(payload)=="5C82FFFF00800947FF7F","native attack words preserve raw bits");
        check(OriginalLocalPacketCodec.TryDecodePayload(LocalPacketDirection.ClientToServer,payload,out var copy)
            && copy is AttackRequest a && a.TargetUnitId==attack.TargetUnitId && a.AttackerUnitId==attack.AttackerUnitId && a.TargetTileAttribute==attack.TargetTileAttribute,"native attack decodes detached values");
        check(!OriginalLocalPacketCodec.TryDecodePayload(LocalPacketDirection.ServerToClient,payload,out _),"direction disambiguates shared82 opcode");
        foreach(byte opcode in new byte[]{0x89,0x92})foreach(int count in new[]{0,1,255})
        {
            var targets=Enumerable.Range(0,count).Select(i=>(short)(i-100)).ToArray();
            var magic=new MagicRequest(101,-1,-2,33,int.MinValue,targets,opcode,2,100);
            check(OriginalLocalPacketCodec.TryEncodePayload(magic,out payload) && payload.Length==16+count*2,"native magic variable payload length");
            check(OriginalLocalPacketCodec.TryDecodePayload(LocalPacketDirection.ClientToServer,payload,out copy,magic.ActingUnitId)
                && copy is MagicRequest m && m.ActingUnitId==100 && m.CasterUnitId==-1 && m.ComputedCost==int.MinValue && m.TargetUnitIds.SequenceEqual(targets),"native magic preserves local actor separately from wire caster");
        }
        check(!OriginalLocalPacketCodec.TryEncodePayload(new MagicRequest(101,100,0,0,0,Array.Empty<short>()),out _),"unknown magic opcode/mode is not fabricated");
        foreach(LocalPacket p in new LocalPacket[]{new HpSetNotification(700,short.MaxValue),new MpSetNotification(100,9),new AttackEffectNotification(700,100,-1)})
        {check(OriginalLocalPacketCodec.TryEncodePayload(p,out payload) && OriginalLocalPacketCodec.TryDecodePayload(p.Direction,payload,out copy) && copy.GetType()==p.GetType(),"confirmed response payload roundtrip");}
        var nativeVectors=File.ReadAllLines("닥세_리소스/분석결과/패킷_원본송신_대조.csv").Skip(1).Take(261).ToArray();
        check(nativeVectors.Length==261,"stored original x86 vector fixture count");
        for(int i=0;i<nativeVectors.Length;i++)
        {
            byte[] bytes=i<256?new[]{(byte)i}:i==256?Array.Empty<byte>():i==257?Enumerable.Range(0,256).Select(v=>(byte)v).ToArray():i==258?Enumerable.Range(0,2048).Select(v=>(byte)v).ToArray():Enumerable.Repeat(i==259?(byte)9:(byte)0x47,2048).ToArray();
            string expected=nativeVectors[i].Split(',')[3];
            string actual=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(OriginalLocalPacketCodec.EncodeFrame(bytes))).ToLowerInvariant();
            check(actual==expected,"C# frame matches stored original encoder digest "+i);
        }
        for(int n=1;n<=17;n++)CheckScenario(n,check);
    }
    private static void CheckScenario(int number,Action<bool,string> check)
    {
        string key=number.ToString("00");var definition=UnityEngine.JsonUtility.FromJson<Scenario01Definition>(File.ReadAllText("Assets/_Game/Resources/Scenarios/"+key+"/Content.json"));
        Scenario01Progress saved=null;var items=new NativeInventory();int revision=0,saves=0;bool fail=false,throwSave=false;
        var server=new LocalScenarioServer(definition,()=>saved?.Clone(),()=>items.Clone(),()=>revision,()=>100,()=>false,
            (Scenario01Progress state,NativeInventory inventory,out string reason)=>
            {
                if(throwSave)throw new IOException("private fixture /accounts/secret/native-save.json");
                if(fail){reason="disk fixture";return false;}saved=state.Clone();items=inventory.Clone();saves++;revision++;reason="";return true;
            },()=>fail);
        var bus=new LocalServerDispatcher();using var binding=bus.Register<ScenarioCommandRequest>(server.Handle);
        ScenarioStateNotification Send(ScenarioOperation operation,int x=0,int y=0,double seconds=0)
        {
            check(bus.TryDispatch(new ScenarioCommandRequest(key,revision,operation,x,y,seconds),out var raw),"scenario request dispatched "+key);
            var packet=(ScenarioStateNotification)raw;check(packet.Accepted,"scenario response accepted "+key+": "+packet.Reason);return packet;
        }
        throwSave=true;
        var exception=(ScenarioStateNotification)server.Handle(new ScenarioCommandRequest(key,revision,ScenarioOperation.Initialize));
        check(!exception.Accepted && !exception.RetryableSaveFailure && !exception.Reason.Contains("secret") && !exception.Reason.Contains("accounts"),"unexpected save exception never publishes a private path "+key);
        check(saved==null && revision==0 && saves==0 && server.LastFailureDiagnostic.StartsWith("IOException/0x"),"unexpected save exception preserves state and sanitized diagnostic "+key);
        throwSave=false;fail=true;
        var firstAttempt=(ScenarioStateNotification)server.Handle(new ScenarioCommandRequest(key,revision,ScenarioOperation.Initialize));
        check(server.LastFailureDiagnostic==null,"diagnostic is reset for the next request "+key);
        check(!firstAttempt.Accepted && firstAttempt.RetryableSaveFailure && saved==null && revision==0,"initial save failure retains an uninitialized session for retry "+key);
        var staleDuringFailure=(ScenarioStateNotification)server.Handle(new ScenarioCommandRequest(key,-1,ScenarioOperation.Initialize));
        check(!staleDuringFailure.Accepted && !staleDuringFailure.RetryableSaveFailure,"stale request is not mislabeled as retryable I/O "+key);
        fail=false;
        var initialized=Send(ScenarioOperation.Initialize);var original=initialized.CaptureState();original.phase=Scenario01Progress.Complete;
        check(saved.phase==Scenario01Progress.Intro,"client cannot mutate server snapshot "+key);
        var bad=(ScenarioStateNotification)server.Handle(new ScenarioCommandRequest(key,revision-1,ScenarioOperation.Step));check(!bad.Accepted && !bad.RetryableSaveFailure,"stale request rejected without offering save retry "+key);
        fail=true;int before=saves;var rejected=(ScenarioStateNotification)server.Handle(new ScenarioCommandRequest(key,revision,ScenarioOperation.Step));
        check(!rejected.Accepted && rejected.RetryableSaveFailure && saves==before && saved.cursor==0,"save failure preserves script cursor and offers explicit retry "+key);
        var repeated=(ScenarioStateNotification)server.Handle(new ScenarioCommandRequest(key,revision,ScenarioOperation.Step));
        check(!repeated.Accepted && repeated.RetryableSaveFailure && saves==before && saved.cursor==0,"repeated failed save remains atomic "+key);fail=false;
        var retry=(ScenarioStateNotification)server.Handle(new ScenarioCommandRequest(key,revision,ScenarioOperation.Step));
        check(retry.Accepted && !retry.RetryableSaveFailure && saves==before+1,"same command retry commits once after storage recovers "+key);
        var state=retry.CaptureState();int guard=0;
        while(state.phase==Scenario01Progress.Intro && guard++<10000)
        {
            var packet=Send(ScenarioOperation.Step);state=packet.CaptureState();
            if(packet.StepResult==Scenario01Rules.StepResult.Waiting)
            {
                packet=Send(ScenarioOperation.Dialogue);state=packet.CaptureState();
                if(!packet.Changed)state=Send(ScenarioOperation.Clock,seconds:1).CaptureState();
            }
        }
        check(guard<10000 && state.phase==Scenario01Progress.Playing,"intro reaches gameplay through server responses "+key);
        var snapshot=Send(ScenarioOperation.Clock,seconds:0).CaptureState();int oldRevision=revision;
        var invalid=(ScenarioStateNotification)server.Handle(new ScenarioCommandRequest(key,revision,ScenarioOperation.Clock,seconds:double.NaN));
        check(!invalid.Accepted && revision==oldRevision,"invalid clock has no checkpoint side effect "+key);
        var unit=definition.units.First(u=>u.wave==snapshot.wave);
        var encounter=Send(ScenarioOperation.BeginEncounter,unit.id).CaptureState();check(encounter.pendingBattle==unit.id,"encounter reservation persisted by server "+key);
        var duplicate=(ScenarioStateNotification)server.Handle(new ScenarioCommandRequest(key,revision,ScenarioOperation.BeginEncounter,unit.id));check(!duplicate.Accepted,"duplicate encounter cannot reserve twice "+key);
        check(Send(ScenarioOperation.CancelEncounter).CaptureState().pendingBattle==-1,"failed battle load cancels reservation "+key);
    }
}
