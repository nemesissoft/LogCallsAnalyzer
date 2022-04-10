using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LogCallsAnalyzer.Parser
{
    internal static class AnalyzingMessageTemplateParser
    {
        public static IEnumerable<MessageTemplateToken> Analyze(string messageTemplate)
        {
            if (messageTemplate == null) throw new ArgumentNullException(nameof(messageTemplate));

            if (messageTemplate == "") yield break;

            var nextIndex = 0;
            while (true)
            {
                ParseTextToken(nextIndex, messageTemplate, out nextIndex);

                if (nextIndex == messageTemplate.Length)
                    yield break;

                var beforeProp = nextIndex;
                var pt = ParsePropertyToken(nextIndex, messageTemplate, out nextIndex);
                if (beforeProp < nextIndex)
                    yield return pt;

                if (nextIndex == messageTemplate.Length)
                    yield break;
            }
        }

        private static MessageTemplateToken ParsePropertyToken(int startAt, string messageTemplate, out int next)
        {
            var first = startAt;
            startAt++;
            while (startAt < messageTemplate.Length && IsValidInPropertyTag(messageTemplate[startAt]))
                startAt++;

            if (startAt == messageTemplate.Length)
            {
                next = startAt;
                return new MessageTemplateDiagnostic(first, next - first, "Encountered end of messageTemplate while parsing property");
            }

            if (messageTemplate[startAt] != '}')
            {
                next = startAt;
                return new MessageTemplateDiagnostic(startAt, 1, $"Found invalid character '{messageTemplate[startAt]}' in property");
            }

            next = startAt + 1;

            var rawText = messageTemplate.Substring(first, next - first);
            var tagContent = messageTemplate.Substring(first + 1, next - (first + 2));
            if (tagContent.Length == 0)
                return new MessageTemplateDiagnostic(first, rawText.Length, "Found property without name");

            if (!TrySplitTagContent(tagContent, out string propertyNameAndDestructuring,
                out string? format, out string? alignment, out MessageTemplateDiagnostic? tagContentDiagnostic))
            {
                return new MessageTemplateDiagnostic(first + 1, tagContentDiagnostic.Length,
                    tagContentDiagnostic.Diagnostic, tagContentDiagnostic.MustBeRemapped);
            }

            var propertyName = propertyNameAndDestructuring;
            bool hasDestructuring = IsValidInDestructuringHint(propertyName[0]);
            if (hasDestructuring)
                propertyName = propertyName.Substring(1);

            if (propertyName == "")
                return new MessageTemplateDiagnostic(first, rawText.Length, "Found property with destructuring hint but without name");

            for (var i = 0; i < propertyName.Length; ++i)
            {
                var c = propertyName[i];
                if (!IsValidInPropertyName(c))
                    return new MessageTemplateDiagnostic(first + (hasDestructuring ? 1 : 0) + 1 + i, 1,
                        $"Found invalid character '{c}' in property name");
            }

            if (format != null)
            {
                for (var i = 0; i < format.Length; ++i)
                {
                    var c = format[i];
                    if (!IsValidInFormat(c))
                        return new MessageTemplateDiagnostic(first + propertyNameAndDestructuring.Length + (alignment?.Length + 1 ?? 0) + 2 + i, 1,
                            $"Found invalid character '{c}' in property format");
                }
            }

            if (alignment != null)
            {
                for (var i = 0; i < alignment.Length; ++i)
                {
                    var c = alignment[i];
                    if (!IsValidInAlignment(c))
                        return new MessageTemplateDiagnostic(first + propertyNameAndDestructuring.Length + 2 + i, 1,
                            $"Found invalid character '{c}' in property alignment");
                }

                var lastDash = alignment.LastIndexOf('-');
                if (lastDash > 0)
                    return new MessageTemplateDiagnostic(first + propertyNameAndDestructuring.Length + 2 + lastDash, 1, "'-' character must be the first in alignment");

                var width = lastDash == -1 ?
                    int.Parse(alignment) :
                    int.Parse(alignment.Substring(1));

                if (width == 0)
                    return new MessageTemplateDiagnostic(first + propertyNameAndDestructuring.Length + 2, alignment.Length, "Found zero size alignment");
            }

            return new PropertyToken(first, propertyName, rawText);
        }

        private static bool TrySplitTagContent(string tagContent, out string propertyNameAndDestructuring,
            out string? format, out string? alignment, [NotNullWhen(returnValue: false)] out MessageTemplateDiagnostic? diagnostic)
        {
            int formatDelimiter = tagContent.IndexOf(':');
            int alignmentDelimiter = tagContent.IndexOf(',');
            if (formatDelimiter == -1 && alignmentDelimiter == -1)
            {
                propertyNameAndDestructuring = tagContent;
                format = null;
                alignment = null;
            }
            else
            {
                if (alignmentDelimiter == -1 || (formatDelimiter != -1 && alignmentDelimiter > formatDelimiter))
                {
                    propertyNameAndDestructuring = tagContent.Substring(0, formatDelimiter);
                    format = formatDelimiter == tagContent.Length - 1
                        ? null
                        : tagContent.Substring(formatDelimiter + 1);
                    alignment = null;
                }
                else
                {
                    propertyNameAndDestructuring = tagContent.Substring(0, alignmentDelimiter);
                    if (formatDelimiter == -1)
                    {
                        if (alignmentDelimiter == tagContent.Length - 1)
                        {
                            alignment = format = null;
                            diagnostic = new(alignmentDelimiter, 1, "Found alignment specifier without alignment");
                            return false;
                        }

                        format = null;
                        alignment = tagContent.Substring(alignmentDelimiter + 1);
                    }
                    else
                    {
                        if (alignmentDelimiter == formatDelimiter - 1)
                        {
                            alignment = format = null;
                            diagnostic = new(alignmentDelimiter, 1, "Found alignment specifier without alignment");
                            return false;
                        }

                        alignment = tagContent.Substring(alignmentDelimiter + 1, formatDelimiter - alignmentDelimiter - 1);
                        format = formatDelimiter == tagContent.Length - 1
                            ? null
                            : tagContent.Substring(formatDelimiter + 1);
                    }
                }
            }

            diagnostic = null;
            return true;
        }

        private static bool IsValidInPropertyTag(char c) =>
            IsValidInDestructuringHint(c) || IsValidInPropertyName(c) || IsValidInFormat(c) || c == ':';

        private static bool IsValidInPropertyName(char c) => char.IsLetterOrDigit(c) || c == '_';

        private static bool IsValidInDestructuringHint(char c) => c is '@' or '$';

        private static bool IsValidInAlignment(char c) => char.IsDigit(c) || c == '-';

        private static bool IsValidInFormat(char c) =>
            c != '}' && (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || c == ' ');

        private static void ParseTextToken(int startAt, string messageTemplate, out int next)
        {
            do
            {
                var nc = messageTemplate[startAt];
                if (nc == '{')
                {
                    if (startAt + 1 < messageTemplate.Length && messageTemplate[startAt + 1] == '{')
                        startAt++;
                    else
                        break;
                }
                else
                {
                    if (nc == '}')
                    {
                        if (startAt + 1 < messageTemplate.Length && messageTemplate[startAt + 1] == '}')
                            startAt++;
                    }
                }

                startAt++;
            } while (startAt < messageTemplate.Length);

            next = startAt;
        }
    }
}
