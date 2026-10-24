using DarkSaver.Events;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class ScenarioDialogueLayoutTests
    {
        [TestCase("", new string[0])]
        [TestCase("a|", new[] { "a" })]
        [TestCase("a||", new[] { "a", "" })]
        [TestCase("|", new[] { "" })]
        [TestCase(" 안녕 | 용사여 ", new[] { "안녕", "용사여" })]
        [TestCase("a\tb", new[] { "a\tb" })]
        public void SplitMatchesOriginalEmptyRowsAndSpaceTrimming(string source, string[] expected)
            => Assert.That(ScenarioDialogueLayout.SplitLines(source), Is.EqualTo(expected));

        [Test]
        public void PortraitPagesAndNoticeUseSeparatePaths()
        {
            const string text = " a |b||d|";
            Assert.That(ScenarioDialogueLayout.Page(1, text, 0), Is.EqualTo("a\nb\n"));
            Assert.That(ScenarioDialogueLayout.Page(2, text, 3), Is.EqualTo("d"));
            Assert.That(ScenarioDialogueLayout.Page(2, text, 6), Is.Empty);
            Assert.That(ScenarioDialogueLayout.Page(0, text, 0), Is.EqualTo(text));
            Assert.That(ScenarioDialogueLayout.DisplayText("0", text), Is.EqualTo(text));
        }

        [Test]
        public void NoticeExpiresStrictlyAfterTenSecondsIncludingClockWrap()
        {
            uint start = uint.MaxValue - 500;
            Assert.That(ScenarioDialogueLayout.NoticeExpired(start, unchecked(start + 10000)), Is.False);
            Assert.That(ScenarioDialogueLayout.NoticeExpired(start, unchecked(start + 10001)), Is.True);
        }

        [TestCase("1 CHARPOS 6 15", "CHARPOS", 2)]
        [TestCase("2 ITEM 399", "ITEM", 1)]
        [TestCase("1 ALL_MOB_DEAD", "ALL_MOB_DEAD", 0)]
        [TestCase("3 TICK 20", "TICK", 1)]
        public void KnownNumberedConditionsCannotBeMisreadAsActions(string text, string name, int arguments)
        {
            var command = EventScriptParser.Parse(text)[0];
            Assert.That(command.Name, Is.EqualTo(name));
            Assert.That(command.NumericPrefix.HasValue, Is.True);
            Assert.That(command.Arguments.Length, Is.EqualTo(arguments));
            Assert.That(command.TryReadScenePlacement(out _), Is.False);
        }
    }
}
