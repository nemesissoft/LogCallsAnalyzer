using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using LogCallsAnalyzer.Helpers;
using LogCallsAnalyzer.Parser;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LogCallsAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed partial class SerilogAnalyzer : DiagnosticAnalyzer
    {
        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var diagnostics = GetDiagnostics(context.Options.AnalyzerConfigOptionsProvider, context.Node, context.SemanticModel, context.CancellationToken);

            foreach (var diagnostic in diagnostics)
                context.ReportDiagnostic(diagnostic);
        }

        public const string LOGGER_ABSTRACTION_OPTION = "dotnet_diagnostic.SerilogAnalyzer.LoggerAbstraction";
        private static readonly string _loggerAbstractionOption = LOGGER_ABSTRACTION_OPTION.ToLower();

        public static IEnumerable<Diagnostic> GetDiagnostics(AnalyzerConfigOptionsProvider configOptionsProvider, SyntaxNode contextNode, SemanticModel semanticModel, CancellationToken cancellationToken = default)
        {
            var loggerTypeName = configOptionsProvider.GetOptions(contextNode.SyntaxTree) is { } config &&
                                 config.TryGetValue(_loggerAbstractionOption, out var loggerAbstraction)
                ? loggerAbstraction : null;

            if (!LoggingMethodMeta.TryBuildMeta(loggerTypeName, contextNode, semanticModel, out var invocation,
                    out var compilation, out var meta, cancellationToken))
            {
                yield break;
            }

            // check for errors in the MessageTemplate
            var arguments = new List<SourceArgument>();
            var properties = new List<PropertyToken>();
            bool hasErrors = false, isConstant = false;
            var literalSpan = default(TextSpan);
            var exactPositions = true;
            var stringText = default(string);
            var invocationArguments = invocation.ArgumentList.Arguments;
            int messageTemplateArgumentIndex = -1;
            foreach (var argument in invocationArguments)
            {
                messageTemplateArgumentIndex++;
                var parameter = RoslynHelper.DetermineParameter(argument, semanticModel, true, cancellationToken);
                if (parameter?.Name == meta.MessageTemplateName)
                {
                    string messageTemplate;

                    // is it a simple string literal?
                    if (argument.Expression is LiteralExpressionSyntax literal)
                    {
                        stringText = literal.Token.Text;
                        exactPositions = true;
                        isConstant = true;

                        messageTemplate = literal.Token.ValueText;
                    }
                    else
                    {
                        // can we at least get a computed constant value for it?
                        var constantValue = semanticModel.GetConstantValue(argument.Expression, cancellationToken);
                        if (!constantValue.HasValue || constantValue.Value is not string constString)
                        {
                            if (semanticModel.GetSymbolInfo(argument.Expression, cancellationToken).Symbol
                                    is IFieldSymbol { Name: "Empty" } field &&
                                compilation.GetSpecialType(SpecialType.System_String).Equals(field.Type, SymbolEqualityComparer.Default))
                            {
                                constString = "";
                                isConstant = true;
                            }
                            else
                            {
                                hasErrors = true;
                                yield return Diagnostic.Create(ConstantMessageTemplateRule, argument.Expression.GetLocation(), argument.Expression.ToString());
                                break;
                            }
                        }

                        stringText = argument.Expression.ToString();
                        exactPositions = false;// we can't map positions back from the computed string into the real positions
                        messageTemplate = constString;
                    }

                    literalSpan = argument.Expression.GetLocation().SourceSpan;

                    var messageTemplateTokens = AnalyzingMessageTemplateParser.Analyze(messageTemplate);
                    foreach (var token in messageTemplateTokens)
                        switch (token)
                        {
                            case PropertyToken property:
                                properties.Add(property);
                                continue;
                            case MessageTemplateDiagnostic diagnostic:
                                hasErrors = true;
                                yield return MapDiagnostic(contextNode, ref literalSpan, stringText, exactPositions, TemplateRule, diagnostic);
                                break;
                        }

                    foreach (var x in invocationArguments.Skip(messageTemplateArgumentIndex + 1))
                    {
                        var location = x.GetLocation().SourceSpan;
                        arguments.Add(new(x, location.Start, location.Length));
                    }

                    break;
                }
            }

            if (hasErrors || literalSpan == default || stringText == null) yield break;

            if (meta.IsFormatMethod)
            {
                // do properties match up?
                if (arguments.Count > 0 || properties.Count > 0)
                {
                    var diagnostics = PropertyBindingAnalyzer.AnalyzeProperties(properties, arguments);
                    foreach (var d in diagnostics)
                        yield return MapDiagnostic(contextNode, ref literalSpan, stringText, exactPositions, PropertyBindingRule, d);

                    foreach (var d in CheckDestructureAnonymousObjectsRule(arguments, properties, literalSpan, stringText, exactPositions, contextNode, semanticModel, cancellationToken))
                        yield return d;

                    foreach (var d in CheckPropertyNameRules(properties, literalSpan, stringText, exactPositions, contextNode))
                        yield return d;
                }
            }
            else
            {
                Location GetMethodLocation() => invocation.Expression is MemberAccessExpressionSyntax access
                    ? access.Name.GetLocation()
                    : invocation.GetLocation();

                if (properties.Count > 0)
                    yield return Diagnostic.Create(NonFormatNoTemplateRule, GetMethodLocation(), meta.Method.Name);
                if (!isConstant)
                    yield return Diagnostic.Create(NonFormatComplexMessageRule, GetMethodLocation(), meta.Method.Name);
            }
        }

        private static IEnumerable<Diagnostic> CheckDestructureAnonymousObjectsRule(IReadOnlyList<SourceArgument> arguments, IReadOnlyList<PropertyToken> properties, TextSpan literalSpan, string stringText, bool exactPositions, SyntaxNode contextNode, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // check that all anonymous objects have destructuring hints in the message template
            if (arguments.Count == properties.Count)
            {
                for (int i = 0; i < arguments.Count; i++)
                {
                    var argument = arguments[i];
                    var argumentInfo = semanticModel.GetTypeInfo(argument.Argument.Expression, cancellationToken);
                    if (argumentInfo.Type?.IsAnonymousType ?? false)
                    {
                        var property = properties[i];
                        if (!property.RawText.StartsWith("{@", StringComparison.Ordinal))
                            yield return MapDiagnostic(contextNode, ref literalSpan, stringText, exactPositions, DestructureAnonymousObjectsRule, property.ToDiagnostic(property.PropertyName));
                    }
                }
            }
        }

        private static IEnumerable<Diagnostic> CheckPropertyNameRules(List<PropertyToken> properties, TextSpan literalSpan, string stringText, bool exactPositions, SyntaxNode contextNode)
        {
            // are there duplicate property names?
            var usedNames = new HashSet<string>();
            foreach (var property in properties)
            {
                var propName = property.PropertyName;
                if (!property.IsPositional && !usedNames.Add(propName))
                    yield return MapDiagnostic(contextNode, ref literalSpan, stringText, exactPositions, UniquePropertyNameRule, property.ToDiagnostic(property.PropertyName));


                if (propName.Length > 0 && propName[0] is var firstCharacter && !char.IsDigit(firstCharacter) && !char.IsUpper(firstCharacter))
                    yield return MapDiagnostic(contextNode, ref literalSpan, stringText, exactPositions, PascalPropertyNameRule, property.ToDiagnostic(property.PropertyName));

            }
        }
    }
}
