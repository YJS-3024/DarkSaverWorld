using System;
using System.IO;
using DarkSaver.Editor;
using DarkSaver.Events;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class ScenarioIntroTests
    {
        [Test]
        public void ParserKeepsUnsupportedBoundariesAndReusedInstances()
        {
            var rows = ScenarioIntroConverter.Parse("DEO", "; intro\nCHARPOS 401 400 12 28\nCHARMOVE 400 13 28\nATTACK 301 400 1\nDO 1000\nKILLCHAR2 400\nCHARPOS 301 400 16 7\nCHARMOVE 400 17 7\nEND");
            Assert.That(rows.Length, Is.EqualTo(8));
            Assert.That(rows[0].sourceLine, Is.EqualTo(2));
            Assert.That(rows[0].definitionToken, Is.EqualTo("401"));
            Assert.That(rows[1].kind, Is.EqualTo(ScenarioIntroStepKind.Move));
            Assert.That(rows[1].y, Is.EqualTo(28));
            Assert.That(rows[2].commandName, Is.EqualTo("ATTACK"));
            Assert.That(rows[3].commandName, Is.EqualTo("DO"));
            Assert.That(rows[4].commandName, Is.EqualTo("KILLCHAR2"));
            for (var i = 2; i <= 4; i++) Assert.That(rows[i].kind, Is.EqualTo(ScenarioIntroStepKind.Unsupported));
            Assert.That(rows[5].definitionToken, Is.EqualTo("301"));
            Assert.That(rows[6].instanceId, Is.EqualTo(400));
            Assert.That(rows[6].y, Is.EqualTo(7));
            Assert.That(rows[7].kind, Is.EqualTo(ScenarioIntroStepKind.Unsupported));
        }

        [Test]
        public void CatalogCopiesInputAndBindsNativeDialogueByLine()
        {
            const string text = "CHARPOS COM0 301 14 32\nCHARMOVE 301 14 28\nMESSAGE 1 COM0 0 COM0 이런!  | 다음  \nDO2 301";
            var rows = ScenarioIntroConverter.Parse("DEO", text);
            var messages = ScriptableObject.CreateInstance<ScenarioDialogueCatalog>();
            var catalog = ScriptableObject.CreateInstance<ScenarioIntroCatalog>();
            try
            {
                messages.Initialize(ScenarioDialogueConverter.Parse("DEO", text));
                catalog.Initialize("DEO", rows, messages);
                rows[0].x = 999;
                Assert.That(catalog.GetAt(0).x, Is.EqualTo(14));
                Assert.That(catalog.GetAt(0).definitionToken, Is.EqualTo("COM0"));
                Assert.That(catalog.TryGetDialogueAt(2, out var entry), Is.True);
                Assert.That(entry.text, Is.EqualTo("이런!  | 다음  "));
                Assert.That(catalog.TryGetDialogueAt(-1, out _), Is.False);
                Assert.That(catalog.TryGetDialogueAt(4, out _), Is.False);
                Assert.That(catalog.TryGetDialogueAt(0, out _), Is.False);
                Assert.That(catalog.TryGetDialogueAt(3, out _), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); UnityEngine.Object.DestroyImmediate(messages); }
        }

        [TestCase("CHARPOS 701 700 19")]
        [TestCase("CHARMOVE 301 14 28 extra")]
        [TestCase("CHARMOVE 99999 14 28")]
        [TestCase("MESSAGE 1 COM0 0 COM0")]
        [TestCase("; comments only")]
        public void ParserRejectsMalformedKnownCommands(string text)
        {
            Assert.Throws<InvalidDataException>(() => ScenarioIntroConverter.Parse("DEO", text));
        }

        [Test]
        public void CatalogRejectsInvalidMetadataWithoutReplacingPriorData()
        {
            var catalog = ScriptableObject.CreateInstance<ScenarioIntroCatalog>();
            try
            {
                var valid = ScenarioIntroConverter.Parse("DEO", "DO 1000\nEND");
                catalog.Initialize("DEO", valid, null);
                Assert.Throws<ArgumentException>(() => catalog.Initialize("EVT", valid, null));
                Assert.Throws<ArgumentException>(() => ScenarioIntroConverter.Parse("EVT", "CHARMOVE 301 14 28"));
                Assert.Throws<ArgumentException>(() => catalog.Initialize("DEO", Array.Empty<ScenarioIntroStep>(), null));
                Assert.Throws<ArgumentException>(() => catalog.Initialize("DEO", new[] { valid[1], valid[0] }, null));
                Assert.Throws<ArgumentException>(() => catalog.Initialize("DEO", new[] { valid[0], valid[0] }, null));
                var missingMessage = ScenarioIntroConverter.Parse("DEO", "MESSAGE 1 COM0 0 COM0 이런!");
                Assert.Throws<ArgumentException>(() => catalog.Initialize("DEO", missingMessage, null));
                var invalid = valid[0]; invalid.kind = (ScenarioIntroStepKind)999;
                Assert.Throws<ArgumentException>(() => catalog.Initialize("DEO", new[] { invalid }, null));
                invalid = valid[0]; invalid.x = 14;
                Assert.Throws<ArgumentException>(() => catalog.Initialize("DEO", new[] { invalid }, null));
                invalid = valid[0]; invalid.commandName = "CHARMOVE";
                Assert.Throws<ArgumentException>(() => catalog.Initialize("DEO", new[] { invalid }, null));
                Assert.That(catalog.Count, Is.EqualTo(2));
                Assert.That(catalog.GetAt(0).commandName, Is.EqualTo("DO"));
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void VerifyDoesNotOverwriteDifferentNativeData()
        {
            var rows = ScenarioIntroConverter.Parse("DEO", "CHARMOVE 301 14 28");
            var catalog = ScriptableObject.CreateInstance<ScenarioIntroCatalog>();
            try
            {
                catalog.Initialize("DEO", rows, null);
                rows[0].x = 99;
                Assert.Throws<InvalidDataException>(() => ScenarioIntroConverter.Verify(catalog, "DEO", rows, null));
                Assert.That(catalog.GetAt(0).x, Is.EqualTo(14));
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void NativeAssetKeepsAllStepKindsWithoutOriginalFileDependencies()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScenarioIntroCatalog>(ScenarioIntroConverter.Output);
            Assert.That(catalog, Is.Not.Null);
            var placements = 0; var moves = 0; var messages = 0; var unsupported = 0; var previous = 0;
            for (var i = 0; i < catalog.Count; i++)
            {
                var row = catalog.GetAt(i);
                Assert.That(row.sourceLine, Is.GreaterThan(previous)); previous = row.sourceLine;
                switch (row.kind)
                {
                    case ScenarioIntroStepKind.Placement: placements++; break;
                    case ScenarioIntroStepKind.Move: moves++; break;
                    case ScenarioIntroStepKind.Dialogue: messages++; Assert.That(catalog.TryGetDialogueAt(i, out _), Is.True); break;
                    default: unsupported++; Assert.That(catalog.TryGetDialogueAt(i, out _), Is.False); break;
                }
            }
            Assert.That((placements, moves, messages), Is.EqualTo((15, 5, 7)));
            Assert.That(unsupported, Is.GreaterThan(0));
            Assert.That(catalog.GetAt(0).commandName, Is.EqualTo("LOADSCENE"));
            Assert.That(catalog.GetAt(catalog.Count - 1).commandName, Is.EqualTo("END"));
            foreach (var path in AssetDatabase.GetDependencies(ScenarioIntroConverter.Output, true))
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                Assert.That(extension == ".asset" || extension == ".cs", Is.True, path);
            }
        }
    }
}
