using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LogCallsAnalyzer.Parser
{
    readonly struct SourceArgument
    {
        public ArgumentSyntax Argument { get; }
        public int StartIndex { get; }
        public int Length { get; }

        public SourceArgument(ArgumentSyntax argument, int startIndex, int length)
        {
            Argument = argument;
            StartIndex = startIndex;
            Length = length;
        }

        public MessageTemplateDiagnostic ToDiagnostic(string diagnostics, bool mustBeRemapped = true)
            => new(StartIndex, Length, diagnostics, mustBeRemapped);
    }
}