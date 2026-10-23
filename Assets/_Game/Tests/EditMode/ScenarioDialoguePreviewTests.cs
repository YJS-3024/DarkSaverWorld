using DarkSaver.Editor;
using DarkSaver.Events;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DarkSaver.Tests
{
    public sealed class ScenarioDialoguePreviewTests
    {
        [Test]
        public void IntroViewPreservesNativeIndicesAndUnsupportedBoundaries()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScenarioIntroCatalog>(ScenarioIntroConverter.Output);
            var dirty = EditorUtility.IsDirty(catalog);
            var rows = ScenarioDialoguePreviewWindow.GetIntroMatches(catalog, false, null);
            Assert.That(rows.Count, Is.EqualTo(72));
            for (var i = 0; i < rows.Count; i++) Assert.That(rows[i], Is.EqualTo(i));
            var unsupported = ScenarioDialoguePreviewWindow.GetIntroMatches(catalog, true, null);
            Assert.That(unsupported.Count, Is.EqualTo(45));
            foreach (var index in unsupported) Assert.That(catalog.GetAt(index).kind, Is.EqualTo(ScenarioIntroStepKind.Unsupported));
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroMatches(catalog, false, "  charmOVe  ").Count, Is.EqualTo(5));
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroMatches(catalog, false, "CHARPOS").Count, Is.EqualTo(15));
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroMatches(catalog, false, "MESSAGE").Count, Is.EqualTo(7));
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroMatches(catalog, true, "CHARMOVE"), Is.Empty);
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroMatches(catalog, false, "missing"), Is.Empty);
            rows.Clear();
            Assert.That(catalog.Count, Is.EqualTo(72));
            Assert.That(EditorUtility.IsDirty(catalog), Is.EqualTo(dirty));
        }

        [Test]
        public void IntroDescriptionsSeparateRawSpatialDataDialogueAndUnsupportedSteps()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScenarioIntroCatalog>(ScenarioIntroConverter.Output);
            var placement = ScenarioDialoguePreviewWindow.GetIntroMatches(catalog, false, "CHARPOS")[0];
            var move = ScenarioDialoguePreviewWindow.GetIntroMatches(catalog, false, "CHARMOVE")[0];
            var message = ScenarioDialoguePreviewWindow.GetIntroMatches(catalog, false, "MESSAGE")[0];
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroDescription(catalog, placement), Does.Contain("COM0"));
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroDescription(catalog, placement), Does.Contain("(14, 32)"));
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroDescription(catalog, move), Does.Contain("301"));
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroDescription(catalog, move), Does.Contain("(14, 28)"));
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroDescription(catalog, move), Does.Contain("미연결"));
            Assert.That(catalog.TryGetDialogueAt(message, out var nativeMessage), Is.True);
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroDescription(catalog, message), Does.Contain(nativeMessage.Message.DisplayText));
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroDescription(catalog, 0), Does.Contain("미지원 경계"));
        }

        [Test]
        public void MissingIntroAndInvalidIndicesAreEmptyReadOnlyViews()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScenarioIntroCatalog>(ScenarioIntroConverter.Output);
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroMatches(null, false, null), Is.Empty);
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroMatches(null, true, "DO"), Is.Empty);
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroDescription(null, 0), Is.Empty);
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroDescription(catalog, -1), Is.Empty);
            Assert.That(ScenarioDialoguePreviewWindow.GetIntroDescription(catalog, catalog.Count), Is.Empty);
        }

        [Test]
        public void NativeFiltersSeparateIntroEventsAndCompletionWithoutDirtyingAsset()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ScenarioDialogueCatalog>(ScenarioDialogueConverter.Output);
            var dirty = EditorUtility.IsDirty(catalog);
            var intro = ScenarioDialoguePreviewWindow.GetMatches(catalog, "DEO", "99");
            Assert.That(intro.Count, Is.EqualTo(7));
            for (var i = 1; i < intro.Count; i++) Assert.That(intro[i].sourceLine, Is.GreaterThan(intro[i - 1].sourceLine));
            Assert.That(intro[0].text, Does.Contain("왕궁"));
            Assert.That(intro[6].text, Does.Contain("제삿날"));
            Assert.That(ScenarioDialoguePreviewWindow.GetMatches(catalog, "EVT", "").Count, Is.EqualTo(58));
            var ending = ScenarioDialoguePreviewWindow.GetMatches(catalog, "EVT", "99");
            Assert.That(ending.Count, Is.EqualTo(1));
            Assert.That(ending[0].sourceLine, Is.EqualTo(325));
            Assert.That(ending[0].speakerToken, Is.EqualTo("700"));
            Assert.That(ScenarioDialoguePreviewWindow.GetMatches(catalog, "EVT", "missing"), Is.Empty);
            Assert.That(EditorUtility.IsDirty(catalog), Is.EqualTo(dirty));
        }

        [Test]
        public void ViewSortingPreservesAuthoredOrderAndAllBranchMessages()
        {
            var catalog = ScriptableObject.CreateInstance<ScenarioDialogueCatalog>();
            try
            {
                var rows = ScenarioDialogueConverter.Parse("EVT", "EVENT 01\nTEST\nMESSAGE 0 0 ME 0 성공 %s\nTESTNOT\nMESSAGE 0 0 ME 0 빈 상자\nEND");
                catalog.Initialize(new[] { rows[1], rows[0] });
                var matches = ScenarioDialoguePreviewWindow.GetMatches(catalog, "EVT", "01");
                Assert.That(matches.Count, Is.EqualTo(2));
                Assert.That(matches[0].sourceLine, Is.EqualTo(3));
                Assert.That(matches[1].sourceLine, Is.EqualTo(5));
                matches.Clear();
                Assert.That(catalog.GetAt(0), Is.EqualTo(rows[1]));
                Assert.That(catalog.Count, Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(catalog); }
        }

        [TestCase("DEO")]
        [TestCase("EVT")]
        [TestCase("unknown")]
        public void MissingCatalogProducesEmptyReadOnlyView(string key)
        {
            Assert.That(ScenarioDialoguePreviewWindow.GetMatches(null, key, null), Is.Empty);
        }
    }
}
