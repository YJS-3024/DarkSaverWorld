using System;
using System.IO;
using DarkSaver.Core;
using DarkSaver.Rendering;
using DarkSaver.World;
using NUnit.Framework;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class FourTownTests
    {
        [Test]
        public void EveryTownResolvesAndSavePairsCannotBeMixed()
        {
            Assert.AreEqual(4,TownCatalog.All.Count);
            foreach(var town in TownCatalog.All)
            {
                Assert.IsTrue(WorldSceneCoordinator.TryResolveDestination(town.Key,out var map,out var runtime,out var size));
                Assert.AreEqual(town.MapScene,map);Assert.AreEqual(town.RuntimeScene,runtime);
                Assert.AreEqual(new Vector2(town.Width,town.Height),size);
                Assert.IsTrue(NativeSaveStore.IsSupportedMap(map,runtime));Assert.IsTrue(OriginalRenderRules.IsNativeMapScene(map));
                foreach(var other in TownCatalog.All)
                    Assert.AreEqual(town==other,NativeSaveStore.IsSupportedMap(town.MapScene,other.RuntimeScene));
            }
        }
        [Test]
        public void AllFourTownLocationsRoundTripThroughActualJsonAndDisk()
        {
            var folder=Path.Combine(Path.GetTempPath(),"ds-four-towns-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            try
            {
                foreach(var town in TownCatalog.All)
                {
                    var save=new NativeSaveData {format=NativeSaveStore.Format,version=NativeSaveStore.Version,
                        mapScene=town.MapScene,runtimeScene=town.RuntimeScene,worldX=10.5f,worldY=11.5f,
                        clearedScenarios=Array.Empty<string>(),hasCharacter=true,character=new NativeCharacterProfile {name="마을검증",jobId=100},
                        hasInventory=true,inventory=new NativeInventory {gold=12345}};
                    var store=new NativeSaveStore(Path.Combine(folder,town.Key+".json"));
                    Assert.IsTrue(store.TrySave(save,out _));Assert.IsTrue(store.TryLoad(out var restored,out _));
                    Assert.AreEqual(town.MapScene,restored.mapScene);Assert.AreEqual(town.RuntimeScene,restored.runtimeScene);
                    Assert.AreEqual(10.5f,restored.worldX);Assert.AreEqual(12345,restored.inventory.gold);
                }
            }
            finally {Directory.Delete(folder,true);}
        }
    }
}
