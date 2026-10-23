using System;
using DarkSaver.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class TrainingBattleTestMenuTests
    {
        [Test]
        public void SelectionOnlyAcceptsExistingTrainingEntriesAndRejectsScenarioMaps()
        {
            int previous = TrainingBattleTestMenu.SelectedMap;
            try
            {
                for (int id = 101; id <= 107; id++)
                {
                    TrainingBattleTestMenu.Select(id);
                    Assert.AreEqual(id, TrainingBattleTestMenu.SelectedMap);
                    Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(TrainingSceneGenerator.EntryPath(id)));
                }
                foreach (int invalid in new[] { 0, 1, 100, 108, 1010 })
                    Assert.Throws<ArgumentOutOfRangeException>(() => TrainingBattleTestMenu.Select(invalid));
                Assert.AreEqual(107, TrainingBattleTestMenu.SelectedMap);
            }
            finally { TrainingBattleTestMenu.Select(previous); }
        }
    }
}
