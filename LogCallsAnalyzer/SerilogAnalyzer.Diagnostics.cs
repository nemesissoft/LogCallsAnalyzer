using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using System;
using LogCallsAnalyzer.Parser;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace LogCallsAnalyzer
{
    public partial class SerilogAnalyzer
    {
        internal static readonly DiagnosticDescriptor TemplateRule, PropertyBindingRule,
            ConstantMessageTemplateRule, UniquePropertyNameRule, PascalPropertyNameRule,
            DestructureAnonymousObjectsRule, NonFormatNoTemplateRule, NonFormatComplexMessageRule;

        static SerilogAnalyzer()
        {
            byte id = 0;
            var descriptors = new List<DiagnosticDescriptor>(16);

            TemplateRule = GetDiagnostic("MessageTemplate verifier", "Error while parsing MessageTemplate: {0}", "Checks for errors in the MessageTemplate");

            PropertyBindingRule = GetDiagnostic("Property binding verifier", "Error while binding properties: {0}", "Checks whether properties and arguments match up");

            ConstantMessageTemplateRule = GetDiagnostic("Constant MessageTemplate verifier", "MessageTemplate argument {0} is not constant", "Checks that MessageTemplates are constant values which is recommended practice");

            UniquePropertyNameRule = GetDiagnostic("Unique Property name verifier", "Property name '{0}' is not unique in this MessageTemplate", "Checks that all property names in a MessageTemplates are unique");

            PascalPropertyNameRule = GetDiagnostic("Pascal Property name verifier", "Property name '{0}' should be pascal case", "Checks that all property names in a MessageTemplates are Pascal Case", DiagnosticSeverity.Warning);

            DestructureAnonymousObjectsRule = GetDiagnostic("Anonymous objects use destructuring hint verifier", "Property '{0}' should use destructuring because the argument is an anonymous object", "Checks that properties that are passed anonymous objects use the destructuring hint", DiagnosticSeverity.Warning);

            NonFormatNoTemplateRule = GetDiagnostic("Non *Format method template verifier", "Method '{0}' does not support MessageTemplate. Use '{0}Format' method", "Checks if non-format methods (i.e. Debug as opposed to DebugFormat) do not use message templates");

            NonFormatComplexMessageRule = GetDiagnostic("Non *Format method complex message verifier", "Method '{0}' does not support complex messages - only string literals. Use '{0}Format' method with appropriate template", "Checks if non-format methods (i.e. Debug as opposed to DebugFormat) are using complex messages");

            DiagnosticDescriptor GetDiagnostic(string title, string messageFormat, string description, DiagnosticSeverity severity = DiagnosticSeverity.Error)
            {
                var diagnostic = new DiagnosticDescriptor($"SerilogAnalyzer{++id:000}", title, messageFormat, "CodeQuality", severity, true, description);
                descriptors.Add(diagnostic);
                return diagnostic;
            }

            _supportedDiagnostics = ImmutableArray.Create(descriptors.ToArray());
        }
        private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics;
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            //TODO add internal logging for exceptions
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }

        private static Diagnostic MapDiagnostic(SyntaxNode contextNode, ref TextSpan literalSpan, string stringText, bool exactPositions, DiagnosticDescriptor rule, MessageTemplateDiagnostic diagnostic)
        {
            TextSpan textSpan;
            if (diagnostic.MustBeRemapped)
            {
                if (!exactPositions)
                {
                    textSpan = literalSpan;
                }
                else
                {
                    int remappedStart = GetPositionInLiteral(stringText, diagnostic.StartIndex);
                    int remappedEnd = GetPositionInLiteral(stringText, diagnostic.StartIndex + diagnostic.Length);
                    textSpan = new(literalSpan.Start + remappedStart, remappedEnd - remappedStart);
                }
            }
            else
            {
                textSpan = new(diagnostic.StartIndex, diagnostic.Length);
            }
            var sourceLocation = Location.Create(contextNode.SyntaxTree, textSpan);
            return Diagnostic.Create(rule, sourceLocation, diagnostic.Diagnostic);
        }

        /// <summary>
        /// Remaps a string position into the position in a string literal
        /// </summary>
        /// <param name="literal">The literal string as string</param>
        /// <param name="unescapedPosition">The position in the non literal string</param>
        /// <returns></returns>
        private static int GetPositionInLiteral(string literal, int unescapedPosition)
        {
            if (literal[0] == '@')
            {
                for (int i = 2; i < literal.Length; i++)
                {
                    char c = literal[i];

                    if (c == '"' && i + 1 < literal.Length && literal[i + 1] == '"')
                    {
                        i++;
                    }
                    unescapedPosition--;

                    if (unescapedPosition == -1)
                    {
                        return i;
                    }
                }
            }
            else
            {
                for (int i = 1; i < literal.Length; i++)
                {
                    char c = literal[i];

                    if (c == '\\' && i + 1 < literal.Length)
                    {
                        c = literal[++i];
                        if (c is 'x' or 'u' or 'U')
                        {
                            int max = Math.Min((c == 'U' ? 8 : 4) + i + 1, literal.Length);
                            for (i++; i < max; i++)
                            {
                                c = literal[i];
                                if (!IsHexDigit(c))
                                {
                                    break;
                                }
                            }
                            i--;
                        }
                    }
                    unescapedPosition--;

                    if (unescapedPosition == -1)
                    {
                        return i;
                    }
                }
            }

            return unescapedPosition;
        }

        internal static bool IsHexDigit(char c)
            => c is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';
    }
}
