using System;
using System.Collections.Generic;

namespace DarkSaver.Events
{
    // GameCtl 0x10008780 / 0x10008930 / 0x10008BE0. Formatting is a separate
    // boundary: do not guess MFC's unresolved %S conversion with .NET formatting.
    public static class ScenarioDialogueLayout
    {
        public const int LinesPerPage = 3;
        public const int Width = 561;
        public static int Top(int mode) => mode == 1 ? 50 : mode == 2 ? 300 : 130;
        public static int Height(int mode) => mode == 1 || mode == 2 ? 104 : 65;
        public static bool IsPortrait(int mode) => mode == 1 || mode == 2;

        public static string[] SplitLines(string text)
        {
            var lines = new List<string>();
            text ??= string.Empty;
            var first = 0;
            while (first < text.Length)
            {
                var end = text.IndexOf('|', first);
                if (end < 0) end = text.Length;
                // Native CString trimming in this path strips ASCII spaces, not
                // arbitrary Unicode whitespace. Interior empty rows are significant.
                lines.Add(text.Substring(first, end - first).Trim(' '));
                first = end + 1;
            }
            return lines.ToArray();
        }

        public static string DisplayText(string modeToken, string text)
        {
            return int.TryParse(modeToken, out var mode) && IsPortrait(mode)
                ? string.Join("\n", SplitLines(text)) : text ?? string.Empty;
        }

        public static string Page(int mode, string text, int firstLine)
        {
            if (firstLine < 0) throw new ArgumentOutOfRangeException(nameof(firstLine));
            if (!IsPortrait(mode)) return text ?? string.Empty;
            var lines = SplitLines(text);
            return firstLine >= lines.Length ? string.Empty
                : string.Join("\n", lines, firstLine, Math.Min(LinesPerPage, lines.Length - firstLine));
        }

        // Unsigned subtraction preserves GetTickCount wraparound, and the native
        // notice stays visible at exactly 10,000 ms.
        public static bool NoticeExpired(uint started, uint now) => unchecked(now - started) > 10000u;
    }
}
