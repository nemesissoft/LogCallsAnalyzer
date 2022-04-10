using System;
using System.Collections.Generic;
using System.Text;

namespace LogCallsAnalyzer.Parser
{
    static class PropertyBindingAnalyzer
    {
        private static readonly IReadOnlyCollection<MessageTemplateDiagnostic> _noDiagnostics = new List<MessageTemplateDiagnostic>(0);

        public static IReadOnlyCollection<MessageTemplateDiagnostic> AnalyzeProperties(IReadOnlyList<PropertyToken> propertyTokens, List<SourceArgument> arguments)
        {
            if (propertyTokens.Count > 0)
            {
                bool allPositional = true, anyPositional = false;
                foreach (var propertyToken in propertyTokens)
                {
                    if (propertyToken.IsPositional) anyPositional = true;
                    else allPositional = false;
                }

                var diagnostics = new List<MessageTemplateDiagnostic>();
                if (allPositional)
                    AnalyzePositionalProperties(diagnostics, propertyTokens, arguments);
                else
                {
                    if (anyPositional)
                    {
                        var sb = new StringBuilder("When named properties are being used, positional properties are not allowed: ");
                        PropertyToken? firstToken = null;

                        foreach (var pt in propertyTokens)
                            if (pt.IsPositional)
                            {
                                firstToken ??= pt;
                                sb.Append("'").Append(pt.PropertyName).Append("', ");
                            }
                        if (firstToken != null)
                            diagnostics.Add(firstToken.ToDiagnostic(sb.Remove(sb.Length - 2, 2).ToString()));
                    }

                    AnalyzeNamedProperties(diagnostics, propertyTokens, arguments);
                }

                return diagnostics;
            }
            else if (arguments.Count > 0)
            {
                var argument = arguments[0];
                return new List<MessageTemplateDiagnostic> {
                    argument.ToDiagnostic($"There is no property but {arguments.Count} arguments are left to match", false)
                };
            }

            return _noDiagnostics;
        }

        static void AnalyzePositionalProperties(ICollection<MessageTemplateDiagnostic> diagnostics, IEnumerable<PropertyToken> positionalProperties, IReadOnlyList<SourceArgument> arguments)
        {
            var mapped = new List<KeyValuePair<int, PropertyToken>>();
            foreach (PropertyToken property in positionalProperties)
            {
                void Add(string message) => diagnostics.Add(property.ToDiagnostic(message));

                if (property.TryGetPositionalValue(out int position))
                {
                    if (position < 0)
                        Add("Positional index cannot be negative");

                    if (position >= arguments.Count)
                        Add($"There is no argument that corresponds to the positional property '{position}'");

                    mapped.Add(new(position, property));
                }
                else
                {
                    Add("Couldn't get the position of this property while analyzing");
                }
            }

            //Serilog error check
            if (HasDuplicates(mapped))
            {
                var positionsBag = new HashSet<int>();
                foreach (var m in mapped)
                    if (!positionsBag.Add(m.Key))
                        diagnostics.Add(new(m.Value.StartIndex, m.Value.Length,
                            $"Serilog bug - repeated positional properties are not supported properly. Property: {{{m.Value.PropertyName}}}"));
            }

            static bool HasDuplicates(IReadOnlyList<KeyValuePair<int, PropertyToken>> list)
            {
                for (int i = 0; i < list.Count; i++)
                    for (int j = i + 1; j < list.Count; j++)
                        if (list[i].Key == list[j].Key)
                            return true;

                return false;
            }
            //Serilog error check


            for (var i = 0; i < arguments.Count; ++i)
            {
                bool indexMatched = false;
                for (int m = 0; m < mapped.Count; m++)
                {
                    if (mapped[m].Key == i)
                    {
                        indexMatched = true;
                        break;
                    }
                }

                if (!indexMatched)
                {
                    var arg = arguments[i];
                    diagnostics.Add(arg.ToDiagnostic(
                        $"There is no positional property that corresponds to argument {arg.Argument}", false));
                }
            }
        }

        static void AnalyzeNamedProperties(ICollection<MessageTemplateDiagnostic> diagnostics, IReadOnlyList<PropertyToken> namedProperties, IReadOnlyList<SourceArgument> arguments)
        {
            var matchedRun = Math.Min(namedProperties.Count, arguments.Count);

            // could still possibly work when it hits a name of a contextual property but it's better practice to be explicit at the callsite
            for (int i = matchedRun; i < namedProperties.Count; i++)
            {
                var namedProperty = namedProperties[i];
                diagnostics.Add(namedProperty.ToDiagnostic(
                    $"There is no argument that corresponds to the named property '{namedProperty.PropertyName}'"));
            }

            for (int i = matchedRun; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                diagnostics.Add(argument.ToDiagnostic(
                    $"There is no named property that corresponds to argument '{argument.Argument}'", false));
            }
        }
    }
}
