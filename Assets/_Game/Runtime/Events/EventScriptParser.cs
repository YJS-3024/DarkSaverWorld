using System;
using System.Collections.Generic;

namespace DarkSaver.Events
{
    public readonly struct EventMessage
    {
        public EventMessage(string optionToken, string definitionToken, string speakerToken, string variantToken, string text)
        {
            OptionToken = optionToken;
            DefinitionToken = definitionToken;
            SpeakerToken = speakerToken;
            VariantToken = variantToken;
            Text = text;
        }

        // Keep the four original header tokens. ME/COM0 and mode-specific roles are unresolved.
        public string OptionToken { get; }
        public string DefinitionToken { get; }
        public string SpeakerToken { get; }
        public string VariantToken { get; }
        public string Text { get; }
        public string DisplayText => Text?.Replace("|", "\n") ?? string.Empty;
    }

    public readonly struct SceneCharacterPlacement
    {
        public SceneCharacterPlacement(string definitionToken, short instanceId, int x, int y)
        {
            DefinitionToken = definitionToken;
            InstanceId = instanceId;
            X = x;
            Y = y;
        }

        // Keep dynamic references such as COM0 instead of inventing a numeric job ID.
        public string DefinitionToken { get; }
        public short InstanceId { get; }
        public int X { get; }
        public int Y { get; }
    }

    public readonly struct SceneCharacterMove
    {
        public SceneCharacterMove(short instanceId, int x, int y)
        {
            InstanceId = instanceId;
            X = x;
            Y = y;
        }

        public short InstanceId { get; }
        public int X { get; }
        public int Y { get; }
    }

    public readonly struct EventCommand
    {
        public EventCommand(string name, string[] arguments, int lineNumber, int? numericPrefix = null,
            string sourceText = null)
        {
            Name = name;
            Arguments = arguments;
            LineNumber = lineNumber;
            NumericPrefix = numericPrefix;
            SourceText = sourceText;
        }
        public string Name { get; }
        public string[] Arguments { get; }
        public int LineNumber { get; }
        public string SourceText { get; }
        // Preserved syntax value, not an inferred condition-group or logical operator.
        public int? NumericPrefix { get; }
        public bool UsesReversedCoordinates => Name == "CHANGETILE" || Name == "CHANGETILE2" || Name == "CHANGESPRITE";

        public bool TryReadMessage(out EventMessage message)
        {
            message = default;
            if (Name != "MESSAGE" || NumericPrefix.HasValue || Arguments == null || Arguments.Length < 5)
                return false;
            for (var i = 0; i < 4; i++)
                if (string.IsNullOrWhiteSpace(Arguments[i])) return false;
            var text = SourceText == null
                ? string.Join(" ", Arguments, 4, Arguments.Length - 4)
                : TextAfterTokens(SourceText, 5);
            if (string.IsNullOrWhiteSpace(text)) return false;
            message = new EventMessage(Arguments[0], Arguments[1], Arguments[2], Arguments[3], text);
            return true;
        }

        private static string TextAfterTokens(string source, int tokenCount)
        {
            var index = 0;
            for (var token = 0; token < tokenCount; token++)
            {
                while (index < source.Length && char.IsWhiteSpace(source[index])) index++;
                while (index < source.Length && !char.IsWhiteSpace(source[index])) index++;
            }
            while (index < source.Length && char.IsWhiteSpace(source[index])) index++;
            return source.Substring(index);
        }

        // DEO/DEE form only: CHARPOS definitionToken instanceId x y.
        // EVT CHARPOS position conditions have a different schema and must not spawn units.
        public bool TryReadScenePlacement(out SceneCharacterPlacement placement)
        {
            placement = default;
            if (Name != "CHARPOS" || NumericPrefix.HasValue || Arguments == null || Arguments.Length != 4 ||
                string.IsNullOrWhiteSpace(Arguments[0]) ||
                !short.TryParse(Arguments[1], out var instanceId) ||
                !int.TryParse(Arguments[2], out var x) || !int.TryParse(Arguments[3], out var y)) return false;
            placement = new SceneCharacterPlacement(Arguments[0], instanceId, x, y);
            return true;
        }

        public (int x, int y) ReadCoordinates(int firstArgument)
        {
            var a = int.Parse(Arguments[firstArgument]);
            var b = int.Parse(Arguments[firstArgument + 1]);
            return UsesReversedCoordinates ? (b, a) : (a, b);
        }

        // Confirmed DEO/DEE syntax only. This does not resolve the active instance,
        // convert map coordinates, choose a path/speed, or execute movement.
        public bool TryReadSceneMove(out SceneCharacterMove move)
        {
            move = default;
            if (Name != "CHARMOVE" || NumericPrefix.HasValue || Arguments == null || Arguments.Length != 3 ||
                !short.TryParse(Arguments[0], out var instanceId) ||
                !int.TryParse(Arguments[1], out var x) || !int.TryParse(Arguments[2], out var y)) return false;
            move = new SceneCharacterMove(instanceId, x, y);
            return true;
        }
    }

    public static class EventScriptParser
    {
        public static IReadOnlyList<EventCommand> Parse(string text)
        {
            var result = new List<EventCommand>();
            if (string.IsNullOrEmpty(text)) return result;
            var lines = text.Replace("\r", string.Empty).Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line[0] == ';') continue;
                var tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0) continue;
                var commandIndex = 0;
                int? numericPrefix = null;
                // Confirmed EVT form: "1 CHARDEAD 700". Other numeric tokens stay raw
                // until their command-specific schema is verified.
                if (tokens.Length >= 3 && tokens[1].Equals("CHARDEAD", StringComparison.OrdinalIgnoreCase) &&
                    int.TryParse(tokens[0], out var prefix))
                {
                    numericPrefix = prefix;
                    commandIndex = 1;
                }
                var args = new string[tokens.Length - commandIndex - 1];
                Array.Copy(tokens, commandIndex + 1, args, 0, args.Length);
                result.Add(new EventCommand(tokens[commandIndex].ToUpperInvariant(), args, i + 1, numericPrefix, lines[i]));
            }
            return result;
        }
    }
}
