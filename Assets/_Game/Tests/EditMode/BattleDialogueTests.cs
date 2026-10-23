using DarkSaver.Combat;
using DarkSaver.Editor;
using DarkSaver.Events;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class BattleDialogueTests
    {
        private const string ObjectivePath = "Assets/_Game/Runtime/Combat/Brugos01Victory.asset";

        [Test]
        public void NativeCompletionOnlyAppearsAfterRequiredTargetDefeat()
        {
            var objective = AssetDatabase.LoadAssetAtPath<BattleObjectiveDefinition>(ObjectivePath);
            var world = new CombatWorld();
            Assert.That(objective.TryGetCompletionMessage(world, out _), Is.False);
            world.Add(new CombatUnit { Id = 701, Enemy = true, Hp = 0 });
            Assert.That(objective.GetCompletionDialogue(world), Is.Empty);
            var boss = new CombatUnit { Id = 700, Enemy = true, Hp = 300 };
            world.Add(boss);
            Assert.That(objective.TryGetCompletionMessage(world, out _), Is.False);
            boss.Hp = 0;
            Assert.That(objective.TryGetCompletionMessage(world, out _), Is.False);
            boss.LifeState = UnitLifeState.Corpse;
            Assert.That(objective.TryGetCompletionMessage(world, out var message), Is.True);
            Assert.That(message.DefinitionToken, Is.EqualTo("701"));
            Assert.That(message.SpeakerToken, Is.EqualTo("700"));
            Assert.That(message.VariantToken, Is.EqualTo("A"));
            Assert.That(message.DisplayText, Does.Contain("\n"));
            Assert.That(objective.GetCompletionDialogue(world), Is.EqualTo(message.Text));
            var serialized = new SerializedObject(objective);
            Assert.That(serialized.FindProperty("completionDialogue").stringValue, Is.Empty);
            var catalog = AssetDatabase.LoadAssetAtPath<ScenarioDialogueCatalog>(ScenarioDialogueConverter.Output);
            Assert.That(serialized.FindProperty("completionDialogueCatalog").objectReferenceValue, Is.SameAs(catalog));
        }

        [Test]
        public void BrokenCatalogKeyDoesNotUseInlineFallback()
        {
            var copy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<BattleObjectiveDefinition>(ObjectivePath));
            try
            {
                var serialized = new SerializedObject(copy);
                serialized.FindProperty("completionDialogueSourceLine").intValue = -1;
                serialized.FindProperty("completionDialogue").stringValue = "stale text";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var world = DefeatedWorld();
                Assert.That(copy.IsSatisfied(world), Is.True);
                Assert.That(copy.TryGetCompletionMessage(world, out _), Is.False);
                Assert.That(copy.GetCompletionDialogue(world), Is.Empty);
            }
            finally { Object.DestroyImmediate(copy); }
        }

        [Test]
        public void UnmigratedInlineObjectiveRemainsSupported()
        {
            var copy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<BattleObjectiveDefinition>(ObjectivePath));
            try
            {
                var serialized = new SerializedObject(copy);
                serialized.FindProperty("completionDialogueCatalog").objectReferenceValue = null;
                serialized.FindProperty("completionDialogue").stringValue = "기존 대사 | 둘째줄";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(copy.TryGetCompletionMessage(DefeatedWorld(), out _), Is.False);
                Assert.That(copy.GetCompletionDialogue(DefeatedWorld()), Is.EqualTo("기존 대사 | 둘째줄"));
            }
            finally { Object.DestroyImmediate(copy); }
        }

        private static CombatWorld DefeatedWorld()
        {
            var world = new CombatWorld();
            world.Add(new CombatUnit { Id = 700, Enemy = true, Hp = 0, LifeState = UnitLifeState.Corpse });
            return world;
        }
    }
}
