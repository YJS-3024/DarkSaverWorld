param([ValidateRange(1,180)][int]$TimeoutSeconds=180)
# Bounded direct NUnit invocation, not Unity Test Runner. TestCase arguments are included.
$code=@'
using UnityEditor;
internal class CommandScript:IRunCommand {
    public void Execute(ExecutionResult r) {
        if(EditorApplication.isPlaying || EditorApplication.isCompiling) throw new System.Exception("Editor busy");
        if(EditorUtility.scriptCompilationFailed) throw new System.Exception("Script compilation failed; refusing to test stale assemblies");
        foreach(string file in new[]{"TownServicesPanel.cs","TownServicesPanel.Inventory.cs","TownServicesPanel.Shop.cs","TownServicesPanel.Mercenary.cs","TownServicesPanel.CharacterInfo.cs","TownServicesPanel.CharacterValues.cs","TownServicesPanel.CharacterMagic.cs","TownServicesPanel.CharacterStats.cs","TownServicesPanel.CharacterOptions.cs","ClassMenuIconCatalog.cs","CharacterPortraitCatalog.cs","WorldSessionHud.cs","BattleFormationPanel.cs","InGameUi.cs","InGameUi.ItemDrag.cs"})
            if(System.IO.File.GetLastWriteTimeUtc(typeof(DarkSaver.UI.TownServicesPanel).Assembly.Location)<System.IO.File.GetLastWriteTimeUtc("Assets/_Game/Runtime/UI/"+file))
                throw new System.Exception("Runtime source not compiled: "+file);
        foreach(string file in new[]{"Items/ItemUsageConditions.cs","Network/EquipmentRequest.cs","Network/LocalPacketPlaceholders.cs","Network/OriginalCharacterInfoValues.cs","Network/CharacterAdjustmentPackets.cs"})
            if(System.IO.File.GetLastWriteTimeUtc(typeof(DarkSaver.Network.EquipmentRequest).Assembly.Location)<System.IO.File.GetLastWriteTimeUtc("Assets/_Game/Runtime/"+file))
                throw new System.Exception("Runtime source not compiled: "+file);
        string[] names={"BattleMapPresentationTests","BattleRequestSenderTests","BattleMagicRequestTests",
            "BattleSpecialMenuTests","BattlePacketReceiverTests","BattleEffectPacketTests","BattleFeedbackTests",
            "BattleSystemMenuTests","InGameUiTests","CombatMovementRouteTests","CombatMagicAreaTests",
            "CombatMagicTargetsTests","BattleFormationReturnTests","BattleVictoryRulesTests","BattleDefeatExperienceTests",
            "JobCombatConfigurationTests","ExperienceProgressionTests","ClassAbilityProgressionTests",
            "NativeCharacterTests","NativeSaveStoreTests","TownInteractionTests","TrainingTravelTests","ScenarioTravelTests","TownShopTests","MercenaryHiringTests","MercenaryArtworkTests","LocalPacketTests","ClassChangeExecutionTests","WorkBatchMemoryTests","BattleFormationPanelTests","ItemCatalogTests","ItemDefinitionTests","ItemDefinitionPublisherTests","ItemIconTests","ItemUsageConditionsTests","InventoryGridTests"};
        int total=0;
        try {
        foreach(string name in names) {
            System.Type type=null;
            foreach(var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
                type=assembly.GetType("DarkSaver.Tests."+name); if(type!=null)break;
            }
            if(type==null)throw new System.Exception("Missing tests: "+name);
            if(System.IO.File.GetLastWriteTimeUtc(type.Assembly.Location)<System.IO.File.GetLastWriteTimeUtc("Assets/_Game/Tests/EditMode/"+name+".cs"))
                throw new System.Exception("Test source not compiled: "+name);
            int count=0;
            foreach(var method in type.GetMethods()) {
                if(method.ReturnType!=typeof(void))continue;
                var cases=new System.Collections.Generic.List<object[]>(); bool plain=false;
                foreach(var attribute in method.GetCustomAttributes(true)) {
                    if(attribute.GetType().Name=="TestAttribute")plain=true;
                    if(attribute.GetType().Name=="TestCaseAttribute")
                        cases.Add((object[])attribute.GetType().GetProperty("Arguments").GetValue(attribute));
                }
                if(cases.Count==0 && plain && method.GetParameters().Length==0)cases.Add(new object[0]);
                foreach(var args in cases) {
                    try{method.Invoke(System.Activator.CreateInstance(type),args);count++;}
                    catch(System.Exception e){throw new System.Exception(name+"."+method.Name,e.InnerException??e);}
                }
            }
            total+=count; r.Log("PASS "+name+" cases="+count);
        }
        r.Log("PASS total="+total+" allocatedMB="+(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576));
        UnityEngine.Debug.Log("DarkSaver direct NUnit batch PASS total="+total);
        } finally {
            DarkSaver.EditorTools.WorkBatchMemoryMaintenance.CollectIfIdle();
            r.Log("Cleanup: "+DarkSaver.EditorTools.WorkBatchMemoryMaintenance.LastReport);
        }
    }
}
'@
& (Join-Path $PSScriptRoot 'Invoke-UnityMcpBounded.ps1') -Code $code -TimeoutSeconds $TimeoutSeconds
