using DarkSaver.Events;
using NUnit.Framework;

namespace DarkSaver.Tests
{
    public sealed class EventScriptParserTests
    {
        [Test]
        public void SceneMove_PreservesInstanceCoordinatesAndSource()
        {
            var command = EventScriptParser.Parse("; intro\n  CHARMOVE 301 14 28  ")[0];
            Assert.That(command.TryReadSceneMove(out var move), Is.True);
            Assert.That((move.InstanceId, move.X, move.Y), Is.EqualTo(((short)301, 14, 28)));
            Assert.That(command.LineNumber, Is.EqualTo(2));
            Assert.That(command.SourceText, Is.EqualTo("  CHARMOVE 301 14 28  "));
        }

        [Test]
        public void SceneMove_DoesNotMergeReusedInstances()
        {
            var commands = EventScriptParser.Parse("CHARPOS 401 400 12 28\nCHARMOVE 400 13 28\nKILLCHAR2 400\nCHARPOS 301 400 16 7\nCHARMOVE 400 17 7");
            Assert.That(commands[1].TryReadSceneMove(out var first), Is.True);
            Assert.That(commands[4].TryReadSceneMove(out var second), Is.True);
            Assert.That((first.InstanceId, first.X, first.Y), Is.EqualTo(((short)400, 13, 28)));
            Assert.That((second.InstanceId, second.X, second.Y), Is.EqualTo(((short)400, 17, 7)));
            Assert.That(commands[2].Name, Is.EqualTo("KILLCHAR2"));
            Assert.That(commands[3].Arguments[0], Is.EqualTo("301"));
        }

        [TestCase("CHARMOVE 301 14")]
        [TestCase("CHARMOVE 301 14 28 1")]
        [TestCase("CHARMOVE COM0 14 28")]
        [TestCase("CHARMOVE 99999 14 28")]
        [TestCase("CHARMOVE 301 invalid 28")]
        [TestCase("CHARMOVE 301 14 2147483648")]
        [TestCase("1 CHARMOVE 301 14 28")]
        [TestCase("CHARPOS 301 14 28")]
        public void SceneMove_RejectsUnsupportedSyntax(string text)
        {
            Assert.That(EventScriptParser.Parse(text)[0].TryReadSceneMove(out _), Is.False);
        }

        [Test]
        public void SceneMove_PreservesRawNegativeValuesWithoutInterpretingSentinels()
        {
            var command = new EventCommand("CHARMOVE", new[] { "-1", "-2", "-3" }, 1);
            Assert.That(command.TryReadSceneMove(out var move), Is.True);
            Assert.That((move.InstanceId, move.X, move.Y), Is.EqualTo(((short)-1, -2, -3)));
            Assert.That(new EventCommand("CHARMOVE", new[] { "301", "14", "28" }, 1, 1).TryReadSceneMove(out _), Is.False);
            Assert.That(default(EventCommand).TryReadSceneMove(out _), Is.False);
        }

        [Test]
        public void Message_PreservesHeadersSpacingAndLineBreakMarkers()
        {
            var command = EventScriptParser.Parse("  MESSAGE 1 701 700 A 으..  분하다.. | 물러나야겠군..  ")[0];
            Assert.That(command.TryReadMessage(out var message), Is.True);
            Assert.That(message.OptionToken, Is.EqualTo("1"));
            Assert.That(message.DefinitionToken, Is.EqualTo("701"));
            Assert.That(message.SpeakerToken, Is.EqualTo("700"));
            Assert.That(message.VariantToken, Is.EqualTo("A"));
            Assert.That(message.Text, Is.EqualTo("으..  분하다.. | 물러나야겠군..  "));
            Assert.That(message.DisplayText, Is.EqualTo("으..  분하다.. \n 물러나야겠군..  "));
        }

        [TestCase("MESSAGE 0 0 ME 0 %s가 상자를 열었습니다.", "0", "ME", "0", "%s가 상자를 열었습니다.")]
        [TestCase("MESSAGE 1 COM0 0 COM0 이런!", "COM0", "0", "COM0", "이런!")]
        public void Message_PreservesDynamicReferencesAndPlaceholders(string text, string definition, string speaker,
            string variant, string body)
        {
            Assert.That(EventScriptParser.Parse(text)[0].TryReadMessage(out var message), Is.True);
            Assert.That(message.DefinitionToken, Is.EqualTo(definition));
            Assert.That(message.SpeakerToken, Is.EqualTo(speaker));
            Assert.That(message.VariantToken, Is.EqualTo(variant));
            Assert.That(message.Text, Is.EqualTo(body));
        }

        [TestCase("MESSAGE 1 701 700 A")]
        [TestCase("MESSAGE 1 701 700")]
        [TestCase("1 MESSAGE 1 701 700 A 대사")]
        [TestCase("UNKNOWN 1 701 700 A 대사")]
        public void Message_RejectsMissingBodyOrUnconfirmedSyntax(string text)
        {
            Assert.That(EventScriptParser.Parse(text)[0].TryReadMessage(out _), Is.False);
        }

        [Test]
        public void ScenePlacement_SeparatesDefinitionInstanceAndSourceCoordinates()
        {
            var command = EventScriptParser.Parse("CHARPOS 701 700 19 10")[0];
            Assert.That(command.TryReadScenePlacement(out var placement), Is.True);
            Assert.That(placement.DefinitionToken, Is.EqualTo("701"));
            Assert.That(placement.InstanceId, Is.EqualTo(700));
            Assert.That((placement.X, placement.Y), Is.EqualTo((19, 10)));
            Assert.That(command.Arguments, Is.EqualTo(new[] { "701", "700", "19", "10" }));
        }

        [Test]
        public void ScenePlacement_PreservesDynamicCommanderReference()
        {
            var command = EventScriptParser.Parse("CHARPOS COM0 301 14 32")[0];
            Assert.That(command.TryReadScenePlacement(out var placement), Is.True);
            Assert.That(placement.DefinitionToken, Is.EqualTo("COM0"));
            Assert.That(placement.InstanceId, Is.EqualTo(301));
        }

        [TestCase("CHARPOS 800 15 5")]
        [TestCase("1 CHARPOS 6 15")]
        [TestCase("CHARPOS 701 99999 19 10")]
        [TestCase("CHARPOS 701 700 invalid 10")]
        [TestCase("CHARPOS3 701 700 19 10")]
        public void ScenePlacement_RejectsOtherSchemasAndInvalidFields(string text)
        {
            Assert.That(EventScriptParser.Parse(text)[0].TryReadScenePlacement(out _), Is.False);
        }

        [Test]
        public void CharacterDeath_PreservesPrefixAndInstanceId()
        {
            var commands = EventScriptParser.Parse("EVENT 99\r\nCHAR\r\n1 CHARDEAD 700\r\nSCENECLEAR\r\nEND");
            Assert.That(commands[2].Name, Is.EqualTo("CHARDEAD"));
            Assert.That(commands[2].NumericPrefix, Is.EqualTo(1));
            Assert.That(commands[2].Arguments, Is.EqualTo(new[] { "700" }));
            Assert.That(commands[2].LineNumber, Is.EqualTo(3));
        }

        [Test]
        public void UnknownNumericCommand_RemainsUninterpreted()
        {
            var command = EventScriptParser.Parse("7 UNKNOWN 42")[0];
            Assert.That(command.Name, Is.EqualTo("7"));
            Assert.That(command.NumericPrefix, Is.Null);
            Assert.That(command.Arguments, Is.EqualTo(new[] { "UNKNOWN", "42" }));
        }

        [Test]
        public void ChangeSprite_UsesYThenXWhileCharacterPositionUsesXThenY()
        {
            var commands = EventScriptParser.Parse("CHANGESPRITE 5 15 0 1 0 22\nCHARPOS 800 15 5");
            Assert.That(commands[0].ReadCoordinates(0), Is.EqualTo((15, 5)));
            Assert.That(commands[1].ReadCoordinates(1), Is.EqualTo((15, 5)));
        }
    }
}
