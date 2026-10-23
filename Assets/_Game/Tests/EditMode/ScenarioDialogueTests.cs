using System;
using System.IO;
using DarkSaver.Editor;
using DarkSaver.Events;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class ScenarioDialogueTests
    {
        [Test]
        public void ParserPreservesBranchesAndSourceKeys()
        {
            var rows = ScenarioDialogueConverter.Parse("EVT",
                "EVENT 01\nTEST\n(\nMESSAGE 0 0 ME 0 %s 얻음  | 둘째줄  \n)\nTESTNOT\n(\nMESSAGE 0 0 ME 0 빈 상자\n)\nEND\nEVENT 99\nMESSAGE 1 701 700 A 종료\nEND");
            Assert.That(rows.Length, Is.EqualTo(3));
            Assert.That(rows[0].eventToken, Is.EqualTo("01"));
            Assert.That(rows[1].eventToken, Is.EqualTo("01"));
            Assert.That(rows[2].eventToken, Is.EqualTo("99"));
            Assert.That(rows[0].sourceLine, Is.EqualTo(4));
            Assert.That(rows[1].sourceLine, Is.EqualTo(8));
            Assert.That(rows[0].text, Is.EqualTo("%s 얻음  | 둘째줄  "));
        }

        [Test]
        public void LookupSeparatesScriptsAndCopiesInput()
        {
            var intro = ScenarioDialogueConverter.Parse("DEO", "MESSAGE 1 COM0 0 COM0 이런!");
            var events = ScenarioDialogueConverter.Parse("EVT", "EVENT 99\nMESSAGE 1 701 700 A 종료");
            var rows = new[] { intro[0], events[0] };
            var catalog = ScriptableObject.CreateInstance<ScenarioDialogueCatalog>();
            try
            {
                catalog.Initialize(rows);
                rows[0].text = "변경";
                Assert.That(catalog.TryGet("DEO", 1, out var entry), Is.True);
                Assert.That(entry.text, Is.EqualTo("이런!"));
                Assert.That(entry.eventToken, Is.Empty);
                Assert.That(catalog.TryGet("EVT", 1, out _), Is.False);
                Assert.That(catalog.TryGet("EVT", 2, out var end), Is.True);
                Assert.That(end.speakerToken, Is.EqualTo("700"));
                Assert.Throws<ArgumentException>(() => catalog.Initialize(new[] { end, end }));
                Assert.That(catalog.Count, Is.EqualTo(2));
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [TestCase("MESSAGE 1 701 700 A 본문")]
        [TestCase("EVENT 99\nMESSAGE 1 701 700 A")]
        [TestCase("EVENT 99 extra\nMESSAGE 1 701 700 A 본문")]
        [TestCase("EVENT 99\nEND\nMESSAGE 1 701 700 A 본문")]
        public void ParserRejectsMalformedOrUnscopedMessage(string text)
        {
            Assert.Throws<InvalidDataException>(() => ScenarioDialogueConverter.Parse("EVT", text));
        }

        [Test]
        public void NativeAssetPreserves65RecordsWithoutRawDependencies()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScenarioDialogueCatalog>(ScenarioDialogueConverter.Output);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Count, Is.EqualTo(65));
            var introCount = 0;
            var eventCount = 0;
            var bossCount = 0;
            for (var i = 0; i < catalog.Count; i++)
            {
                var row = catalog.GetAt(i);
                if (row.scriptKey == "DEO") introCount++;
                if (row.scriptKey == "EVT") eventCount++;
                if (row.eventToken == "99" && row.definitionToken == "701" && row.speakerToken == "700")
                {
                    bossCount++;
                    Assert.That(row.text, Is.EqualTo("으.. 분하다.. | 억울하지만.. 여기선 일단 물러나야겠군.."));
                }
            }
            Assert.That(introCount, Is.EqualTo(7));
            Assert.That(eventCount, Is.EqualTo(58));
            Assert.That(bossCount, Is.EqualTo(1));
            foreach (var path in AssetDatabase.GetDependencies(ScenarioDialogueConverter.Output, true))
                Assert.That(path.EndsWith(".cs", StringComparison.Ordinal) || path == ScenarioDialogueConverter.Output,
                    Is.True, "Unexpected dependency: " + path);
        }
    }
}
