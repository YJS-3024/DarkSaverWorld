using System;
using DarkSaver.Events;
namespace DarkSaver.Tests
{
    // Explicit empty received-state fixture for serialization/boundary tests only.
    // Does not execute scripts, invent enemies, grant items or initialize production state.
    internal static class ReceivedScenarioFixture
    {
        internal static Scenario01Progress Empty(Scenario01Definition source)=>new Scenario01Progress
        {
            version=source.version,scenarioKey=source.Key,sourceHash=source.sourceHash,profileHash=source.profileHash,
            actorGuid=Guid.NewGuid().ToString(),hasBattleExperienceLedger=true
        };
    }
}
